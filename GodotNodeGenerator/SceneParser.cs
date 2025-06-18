using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace GodotNodeGenerator
{
    /// <summary>
    /// Parses Godot scene files to extract node information.
    /// </summary>
    internal static class SceneParser
    {
        // Regular expressions to extract node information from TSCN files
        private static readonly Regex NodeRegex = new Regex(
            @"\[node\s+name=""(?<name>[^""]+)""\s+type=""(?<type>[^""]+)""(\s+parent=""(?<parent>[^""]*)"")?\]",
            RegexOptions.Compiled);

        /// <summary>
        /// Parse a Godot scene file to extract node information.
        /// </summary>
        /// <param name="scenePath">Path to the scene file</param>
        /// <returns>List of nodes in the scene</returns>
        public static List<NodeInfo> ParseScene(string scenePath)
        {
            try
            {
                // Note: In actual source generators, you should use context.AdditionalFiles
                // instead of direct file IO. This is simplified for demonstration.
                var sceneContent = ReadSceneContent(scenePath);
                return ParseSceneContent(sceneContent);
            }
            catch (Exception ex)
            {
                // In a real generator, you'd report a diagnostic instead
                Console.WriteLine($"Error parsing scene: {ex.Message}");
                return new List<NodeInfo>();
            }
        }

        // This method would be replaced with AdditionalFiles access in a real generator
        private static string ReadSceneContent(string scenePath)
        {
            // WARNING: This is for demonstration only. File I/O operations are not allowed in analyzers.
            // In a real source generator, you should use context.AdditionalFiles instead.
            return "[node name=\"Root\" type=\"Node2D\"]\n" +
                   "[node name=\"Player\" type=\"CharacterBody2D\" parent=\".\"]\n" +
                   "[node name=\"Sprite\" type=\"Sprite2D\" parent=\"Player\"]\n" +
                   "[node name=\"Camera\" type=\"Camera2D\" parent=\"Player\"]\n" +
                   "[node name=\"UI\" type=\"CanvasLayer\"]\n" +
                   "[node name=\"HealthBar\" type=\"ProgressBar\" parent=\"UI\"]\n";
        }

        private static List<NodeInfo> ParseSceneContent(string content)
        {
            var nodes = new List<NodeInfo>();
            var nodeDict = new Dictionary<string, (string Name, string Type, string Parent)>();
            
            // Extract all nodes with their parents
            foreach (Match match in NodeRegex.Matches(content))
            {
                var name = match.Groups["name"].Value;
                var type = match.Groups["type"].Value;
                var parent = match.Groups["parent"].Success ? match.Groups["parent"].Value : ".";
                
                nodeDict[name] = (name, type, parent);
            }
            
            // Compute full paths
            foreach (var kvp in nodeDict)
            {
                var path = CalculateNodePath(kvp.Key, nodeDict);
                nodes.Add(new NodeInfo 
                { 
                    Name = kvp.Value.Name, 
                    Type = kvp.Value.Type,
                    Path = path 
                });
            }
            
            return nodes;
        }

        private static string CalculateNodePath(string nodeName, Dictionary<string, (string Name, string Type, string Parent)> nodeDict)
        {
            if (!nodeDict.TryGetValue(nodeName, out var nodeInfo))
            {
                return nodeName;
            }

            if (nodeInfo.Parent == "." || string.IsNullOrEmpty(nodeInfo.Parent))
            {
                return nodeName;
            }

            var parentPath = CalculateNodePath(nodeInfo.Parent.TrimStart('.').TrimStart('/'), nodeDict);
            return $"{parentPath}/{nodeName}";
        }
    }
}
