using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GodotNodeGenerator
{
    /// <summary>
    /// Parses Godot scene files to extract node information.
    /// </summary>
    public static class SceneParser
    {
        /// <summary>
        /// Parse a Godot scene file to extract node information.
        /// </summary>
        /// <param name="scenePath">Path or identifier for the scene file</param>
        /// <param name="additionalFiles">The collection of additional files to search for the scene file</param>
        /// <param name="reportDiagnostic">Optional action to report diagnostics</param>
        /// <returns>List of nodes in the scene</returns>
        public static List<NodeInfo> ParseScene(
            string scenePath,
            IEnumerable<AdditionalText>? additionalFiles = null,
            Action<Diagnostic>? reportDiagnostic = null)
        {
            try
            {
                var sceneContent = ReadSceneContent(scenePath, additionalFiles, reportDiagnostic);
                if (string.IsNullOrEmpty(sceneContent))
                {
                    return [];
                }

                return ParseSceneContent(sceneContent);
            }
            catch (Exception ex)
            {
                reportDiagnostic?.Invoke(Diagnostic.Create(
                    new DiagnosticDescriptor(
                        id: "GNGEN002",
                        title: "Error parsing scene file",
                        messageFormat: "Error parsing scene file {0}: {1}",
                        category: "SceneParser",
                        DiagnosticSeverity.Warning,
                        isEnabledByDefault: true),
                    location: null,
                    scenePath,
                    ex.Message));

                return [];
            }
        }

        // Use AdditionalFiles to get scene content
        public static string ReadSceneContent(
            string scenePath,
            IEnumerable<AdditionalText>? additionalFiles,
            Action<Diagnostic>? reportDiagnostic = null)
        {
            // If no additional files provided, return empty string
            if (additionalFiles == null)
            {
                return string.Empty;
            }

            // Try to find the scene file in the additional files
            var sceneFile = FindSceneFile(scenePath, additionalFiles);
            if (sceneFile == null)
            {
                // Report diagnostic that scene file wasn't found
                reportDiagnostic?.Invoke(Diagnostic.Create(
                    new DiagnosticDescriptor(
                        id: "GNGEN001",
                        title: "Scene file not found",
                        messageFormat: "Could not find scene file: {0}",
                        category: "SceneParser",
                        DiagnosticSeverity.Warning,
                        isEnabledByDefault: true),
                    location: null,
                    scenePath));

                return string.Empty;
            }

            // Read the content of the scene file
            var sceneContent = sceneFile.GetText()?.ToString();
            return sceneContent ?? string.Empty;
        }

        // Helper method to find the scene file in the additional files
        private static AdditionalText? FindSceneFile(string scenePath, IEnumerable<AdditionalText> additionalFiles)
        {
            // Try direct path match first
            var result = additionalFiles.FirstOrDefault(file =>
                string.Equals(file.Path, scenePath, StringComparison.OrdinalIgnoreCase));

            // If not found, try matching by file name
            if (result == null)
            {
                var fileName = Path.GetFileName(scenePath);
                result = additionalFiles.FirstOrDefault(file =>
                    string.Equals(Path.GetFileName(file.Path), fileName, StringComparison.OrdinalIgnoreCase));
            }

            // If still not found, try looking for *.tscn files
            if (result == null && !scenePath.EndsWith(".tscn", StringComparison.OrdinalIgnoreCase))
            {
                var nameWithExtension = $"{Path.GetFileNameWithoutExtension(scenePath)}.tscn";
                result = additionalFiles.FirstOrDefault(file =>
                    string.Equals(Path.GetFileName(file.Path), nameWithExtension, StringComparison.OrdinalIgnoreCase));
            }

            return result;
        }

        public static List<NodeInfo> ParseSceneContent(string content)
        {
            var nodes = new List<NodeInfo>();
            var nodeDict = new Dictionary<string, (string Name, string Type, string Parent)>();

            // Track scripts and properties for each node
            var nodeScripts = new Dictionary<string, string>();
            var nodeProperties = new Dictionary<string, Dictionary<string, string>>();

            var extResourceMap = new Dictionary<string, string>(); // id -> path
            var lines = content.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("[ext_resource "))
                {
                    // Example: [ext_resource type="Script" path="res://scripts/Player.cs" id="1_player"]
                    var path = ExtractAttributeValue(trimmed, "path");
                    var id = ExtractAttributeValue(trimmed, "id");
                    if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(path))
                        extResourceMap[id] = path;
                }
                // Stop parsing ext_resources when we hit the first [node ...]
                if (trimmed.StartsWith("[node ")) break;
            }

            string currentNode = "";
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();

                // Check if this is a node declaration
                if (line.StartsWith("[node "))
                {
                    var nodeInfo = ParseNodeDeclaration(line);
                    if (nodeInfo.HasValue)
                    {
                        currentNode = nodeInfo.Value.Name;
                        nodeDict[currentNode] = nodeInfo.Value;
                        nodeProperties[currentNode] = [];
                    }
                }
                // Check for script assignment
                else if (!string.IsNullOrEmpty(currentNode) && line.StartsWith("script = "))
                {
                    var scriptPath = ExtractResourcePath(line, extResourceMap);
                    if (!string.IsNullOrEmpty(scriptPath))
                    {
                        nodeScripts[currentNode] = scriptPath;
                    }
                }
                // Check for other property assignments
                else if (!string.IsNullOrEmpty(currentNode) && line.Contains(" = "))
                {
                    var parts = line.Split([" = "], 2, StringSplitOptions.None);
                    if (parts.Length == 2 && nodeProperties.ContainsKey(currentNode))
                    {
                        nodeProperties[currentNode][parts[0].Trim()] = parts[1].Trim();
                    }
                }
            }

            // Build the list of NodeInfo objects
            foreach (var kvp in nodeDict)
            {
                var path = CalculateNodePath(kvp.Key, nodeDict);
                var nodeInfo = new NodeInfo
                {
                    Name = kvp.Value.Name,
                    Type = kvp.Value.Type,
                    Path = path
                };

                // Add script info if available
                if (nodeScripts.TryGetValue(kvp.Key, out var script))
                {
                    nodeInfo.Script = script;
                }

                // Add properties if available
                if (nodeProperties.TryGetValue(kvp.Key, out var props))
                {
                    nodeInfo.Properties = props;
                }

                nodes.Add(nodeInfo);
            }

            return nodes;
        }

        // Helper to extract attribute value from a line like [ext_resource ...]
        private static string? ExtractAttributeValue(string line, string attribute)
        {
            var search = attribute + "=\"";
            var start = line.IndexOf(search);
            if (start == -1) return null;
            start += search.Length;
            var end = line.IndexOf('"', start);
            if (end == -1) return null;
            return line[start..end];
        }

        public static (string Name, string Type, string Parent)? ParseNodeDeclaration(string line)
        {
            // Parse a line like: [node name="Player" type="CharacterBody2D" parent="."]

            // Remove the brackets
            line = line.Trim('[', ']');

            if (!line.StartsWith("node "))
                return null;

            string? name = null;
            string? type = null;
            string parent = ".";

            // Parse attributes
            int position = 4; // Start after "node"
            while (position < line.Length)
            {
                // Skip whitespace
                while (position < line.Length && char.IsWhiteSpace(line[position]))
                    position++;

                if (position >= line.Length)
                    break;

                // Find attribute name
                int attrNameStart = position;
                while (position < line.Length && line[position] != '=')
                    position++;

                if (position >= line.Length)
                    break;

                string attrName = line[attrNameStart..position].Trim();
                position++; // Skip '='

                // Extract quoted value
                if (position < line.Length && line[position] == '"')
                {
                    position++; // Skip opening quote
                    int valueStart = position;

                    // Find closing quote
                    while (position < line.Length && line[position] != '"')
                        position++;

                    if (position < line.Length)
                    {
                        string value = line[valueStart..position];
                        position++; // Skip closing quote

                        // Store the attribute
                        switch (attrName)
                        {
                            case "name":
                                name = value;
                                break;
                            case "type":
                                type = value;
                                break;
                            case "parent":
                                parent = value;
                                break;
                        }
                    }
                }
            }

            // Return the node info if we have at least a name and type
            if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(type))
            {
                return (name, type, parent);
            }

            return null;
        }

        public static string CalculateNodePath(string nodeName, Dictionary<string, (string Name, string Type, string Parent)> nodeDict)
        {
            if (!nodeDict.TryGetValue(nodeName, out var nodeInfo))
            {
                return nodeName;
            }

            if (nodeInfo.Parent == "." || string.IsNullOrEmpty(nodeInfo.Parent))
            {
                return nodeName;
            }

            // Handle direct parent references
            if (nodeInfo.Parent.StartsWith("."))
            {
                return nodeName;
            }

            // Handle relative parent paths
            if (nodeInfo.Parent.StartsWith("../"))
            {
                // For simplicity, we're returning just the node name in this case
                // In a full implementation, you'd handle these paths properly
                return nodeName;
            }

            // Handle absolute paths
            if (nodeInfo.Parent.StartsWith("/"))
            {
                return $"{nodeInfo.Parent.TrimStart('/')}/{nodeName}";
            }

            // Handle normal paths - recursive lookup
            var parentPath = CalculateNodePath(nodeInfo.Parent, nodeDict);
            return $"{parentPath}/{nodeName}";
        }

        /// <summary>
        /// Extracts a resource path from a line like "script = ExtResource("1_kqm1w")" or "script = Resource("res://path/to/script.cs")"
        /// </summary>
        public static string ExtractResourcePath(string line, Dictionary<string, string>? extResourceMap = null)
        {
            // Handle direct resource path
            if (line.Contains("\"res://") || line.Contains("\"user://"))
            {
                int startIndex = line.IndexOf('"');
                int endIndex = line.LastIndexOf('"');

                if (startIndex >= 0 && endIndex > startIndex)
                {
                    return line.Substring(startIndex + 1, endIndex - startIndex - 1);
                }
            }

            // Handle ExtResource reference
            if (line.Contains("ExtResource(") && extResourceMap != null)
            {
                // Extract the ID from ExtResource("1_player")
                var idStart = line.IndexOf('"');
                var idEnd = line.LastIndexOf('"');
                if (idStart >= 0 && idEnd > idStart)
                {
                    var id = line.Substring(idStart + 1, idEnd - idStart - 1);
                    if (extResourceMap.TryGetValue(id, out var path))
                        return path;
                }
                return "ExtResourceReference";
            }

            return string.Empty;
        }
    }
}
