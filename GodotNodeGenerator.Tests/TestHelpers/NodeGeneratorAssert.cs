using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;

namespace GodotNodeGenerator.Tests.TestHelpers
{
    /// <summary>
    /// Provides assertions for NodeGenerator tests
    /// </summary>
    public static class NodeGeneratorAssert
    {
        /// <summary>
        /// Asserts that the NodeGenerator will generate code for the given class and scene content
        /// </summary>        /// <param name="className">Name of the class to generate code for</param>
        /// <param name="sceneContent">Content of the scene file</param>
        /// <param name="namespaceName">Optional namespace name</param>
        /// <returns>The generated code</returns>
        public static string AssertCodeGeneration(string className, string sceneContent, string namespaceName = "TestNamespace")
        {
            // Parse scene content to get the root node type
            var nodes = SceneParser.ParseSceneContent(sceneContent);
            var rootNodeType = nodes.FirstOrDefault(n => !n.Path.Contains("/"))?.Type ?? "Node";
            
            // Setup source code and scene file - use the detected root node type
            var sourceCode = $@"
using Godot;
using GodotNodeGenerator;

namespace {namespaceName}
{{
    [NodeGenerator(""TestScene.tscn"")]
    public partial class {className} : {rootNodeType}
    {{
    }}
}}";
            
            var scenePath = "TestScene.tscn";

            // Run the generator
            var outputs = RunGenerator(sourceCode, [(scenePath, sceneContent)]);
            
            // Verify output file was generated
            var outputFile = $"{className}.g.cs";
            var generatedFile = outputs.FirstOrDefault(f => f.HintName.Contains(outputFile));
            
            // If not found, throw
            if (generatedFile.SourceText == null)
            {
                var availableFiles = string.Join(", ", outputs.Select(o => o.HintName));
                throw new InvalidOperationException(
                    $"Generated file '{outputFile}' not found. Available files: {availableFiles}");
            }

            return generatedFile.SourceText.ToString();
        }        /// <summary>
        /// Verifies that the generated code contains a property for the specified node
        /// </summary>
        public static void ContainsNodeProperty(string generatedCode, string nodeName, string nodeType)
        {
            // Check for the property declaration
            Assert.Contains($"public {nodeType} {nodeName}", generatedCode);
        }

        /// <summary>
        /// Verifies that the generated code contains documentation for the specified node
        /// </summary>
        public static void ContainsNodeDocumentation(string generatedCode, string nodeName, string nodePath, string? scriptPath = null)
        {
            // Check for the XML documentation
            Assert.Contains($"<summary>", generatedCode);
            Assert.Contains($"Gets the {nodeName} node (path: \"{nodePath}\")", generatedCode);
            
            if (scriptPath != null)
            {
                Assert.Contains($"(script: \"{scriptPath}\")", generatedCode);
            }
        }

        /// <summary>
        /// Verifies that the generated code contains null safety features
        /// </summary>
        public static void ContainsNullSafetyFeatures(string generatedCode)
        {
            // Check for null checking and GetNodeOrNull usage
            Assert.Contains("GetNodeOrNull", generatedCode);
            Assert.Contains("if (", generatedCode);
            Assert.Contains(" == null)", generatedCode);
            Assert.Contains("return false;", generatedCode);
            Assert.Contains("out ", generatedCode);
        }
        
        // Helper method to run the generator
        private static List<(string HintName, SourceText SourceText)> RunGenerator(
            string sourceCode, 
            IEnumerable<(string Path, string Content)> additionalFiles)
        {
            // Use a custom implementation instead of trying to access the protected static method
            // Create a collection of additional files
            var additionalTexts = additionalFiles.Select(
                file => new MockAdditionalText(file.Path, file.Content))
                .ToImmutableArray<AdditionalText>();

            // Create compilation for the source code
            var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
            var references = new List<MetadataReference>
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
            };

            var compilation = CSharpCompilation.Create(
                "TestCompilation",
                [syntaxTree],
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
                
            // Use our NodeGeneratorAdapter instead of raw NodeGenerator
            var generator = new NodeGeneratorAdapter(new NodeGenerator());
            ImmutableArray<ISourceGenerator> generators = [generator];
            
            // Set up generator driver with additional texts
            var driver = CSharpGeneratorDriver.Create(
                generators: generators,
                additionalTexts: additionalTexts,
                parseOptions: (CSharpParseOptions)compilation.SyntaxTrees.First().Options,
                optionsProvider: null);
                
            driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation);

            // Get the results
            var runResult = driver.GetRunResult();
            
            // Add debug output
            Console.WriteLine($"Generator run completed with {runResult.GeneratedTrees.Length} generated files.");
            
            return [.. runResult.GeneratedTrees
                .Select(t => {
                    var sourceText = SourceText.From(t.GetText().ToString());
                    var hintName = Path.GetFileName(t.FilePath);
                    Console.WriteLine($"Generated file: {hintName}");
                    return (hintName, sourceText);
                })];
        }
    }
}
