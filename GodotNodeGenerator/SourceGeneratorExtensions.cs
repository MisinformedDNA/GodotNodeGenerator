using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace GodotNodeGenerator
{
    /// <summary>
    /// Extension methods for source generators
    /// </summary>
    public static class SourceGeneratorExtensions
    {
        /// <summary>
        /// Adapts an IIncrementalGenerator to an ISourceGenerator for compatibility with older APIs
        /// </summary>
        /// <param name="generator">The incremental generator to adapt</param>
        /// <returns>An ISourceGenerator compatible adapter</returns>
        public static ISourceGenerator AsSourceGenerator(this IIncrementalGenerator generator)
        {
            if (generator is NodeGenerator nodeGenerator)
            {
                return new NodeGeneratorAdapter(nodeGenerator);
            }
            throw new ArgumentException("The generator must be of type NodeGenerator", nameof(generator));
        }
    }
    
    /// <summary>
    /// A source generator adapter that can be used with both the old and new source generator APIs
    /// </summary>
    public class NodeGeneratorAdapter : ISourceGenerator
    {
        private readonly NodeGenerator _incrementalGenerator;

        public NodeGeneratorAdapter(NodeGenerator incrementalGenerator)
        {
            _incrementalGenerator = incrementalGenerator;
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            // Register a syntax receiver that will identify class declarations with our attribute
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            // Debug output
            Console.WriteLine($"NodeGeneratorAdapter.Execute called with {context.AdditionalFiles.Length} additional files");
            
            // Cast the syntax receiver to our type
            if (!(context.SyntaxReceiver is SyntaxReceiver receiver))
            {
                Console.WriteLine("No syntax receiver found");
                return;
            }

            // If there are no candidates, return early
            if (receiver.CandidateClasses.Count == 0)
            {
                Console.WriteLine("No candidate classes found with NodeGenerator attribute");
                return;
            }
            
            Console.WriteLine($"Found {receiver.CandidateClasses.Count} candidate classes with NodeGenerator attribute");

            // For each class with our attribute, run the generator logic
            foreach (var classDeclaration in receiver.CandidateClasses)
            {
                Console.WriteLine($"Processing class: {classDeclaration.Identifier.Text}");
                
                // Get the semantic model and symbol for the class
                var semanticModel = context.Compilation.GetSemanticModel(classDeclaration.SyntaxTree);
                if (semanticModel.GetDeclaredSymbol(classDeclaration) is not INamedTypeSymbol classSymbol)
                {
                    Console.WriteLine("Couldn't get class symbol");
                    continue;
                }

                // Extract attribute data and scene path
                var scenePath = ExtractScenePathFromAttribute(classSymbol);
                Console.WriteLine($"Extracted scene path from attribute: {scenePath}");
                
                if (string.IsNullOrEmpty(scenePath))
                {
                    // Try to infer scene path from class name
                    scenePath = InferScenePathFromClassName(classSymbol);
                    Console.WriteLine($"Inferred scene path: {scenePath}");
                }
                else
                {
                    // We found a scene path from the attribute - use that directly
                    Console.WriteLine($"Using scene path from attribute: {scenePath}");
                }

                if (string.IsNullOrEmpty(scenePath))
                {
                    Console.WriteLine("No scene path found, skipping generation");
                    continue;
                }

                // Always report a diagnostic for the specified scene path before attempting to parse
                // This ensures we report diagnostics for missing scene files even in snapshot tests
                var missingFileDiagnostic = Diagnostic.Create(
                    new DiagnosticDescriptor(
                        "GNGEN001",
                        "Scene file not found",
                        $"Could not find scene file: {scenePath}",
                        "GodotNodeGenerator",
                        DiagnosticSeverity.Warning,
                        isEnabledByDefault: true),
                    classDeclaration.GetLocation());
                
                // Parse the scene using AdditionalFiles
                Console.WriteLine($"Parsing scene {scenePath} using {context.AdditionalFiles.Length} additional files");
                
                // Debug output of all additional files
                foreach (var file in context.AdditionalFiles)
                {
                    Console.WriteLine($"AdditionalFile: {file.Path}");
                }
                
                // Pass the diagnostic reporting function to ParseScene
                var nodeInfo = SceneParser.ParseScene(
                    scenePath!, 
                    context.AdditionalFiles, 
                    context.ReportDiagnostic);
                
                // If no nodes are found (scene file missing or empty), report diagnostic and skip code generation
                if (nodeInfo == null || nodeInfo.Count == 0)
                {
                    // Make sure we report a diagnostic for files that were not found or couldn't be parsed
                    Console.WriteLine("No nodes found in scene, reporting diagnostic");
                    context.ReportDiagnostic(missingFileDiagnostic);
                    continue;
                }
                
                Console.WriteLine($"Found {nodeInfo.Count} nodes in scene");

                // Get the root node type for inheritance
                var rootNodeType = nodeInfo.FirstOrDefault(n => !n.Path.Contains("/"))?.Type ?? "Node";
                Console.WriteLine($"Root node type: {rootNodeType}");

                // Generate the code
                var generatedCode = SourceGenerationHelper.GenerateNodeAccessors(
                    classSymbol.ContainingNamespace?.ToDisplayString() ?? "",
                    classSymbol.Name,
                    rootNodeType,
                    nodeInfo);

                // Add the generated code to the compilation
                var fileName = $"{classSymbol.Name}.g.cs";
                Console.WriteLine($"Adding generated source file: {fileName}");
                context.AddSource(
                    fileName,
                    SourceText.From(generatedCode, Encoding.UTF8));
                
                Console.WriteLine("Code generation complete");
            }
        }

        // Helper method to extract scene path from attribute
        private static string? ExtractScenePathFromAttribute(INamedTypeSymbol classSymbol)
        {
            // Debug all attributes to see what's available
            foreach (var attribute in classSymbol.GetAttributes())
            {
                Console.WriteLine($"Found attribute: {attribute.AttributeClass?.ToDisplayString()}");
                
                // Check for all possible attribute name variations
                if (attribute.AttributeClass?.ToDisplayString() == "GodotNodeGenerator.NodeGeneratorAttribute" ||
                    attribute.AttributeClass?.Name == "NodeGeneratorAttribute" ||
                    attribute.AttributeClass?.Name == "NodeGenerator")
                {
                    Console.WriteLine("Found NodeGeneratorAttribute");
                    
                    // Log all constructor arguments
                    Console.WriteLine($"Constructor arguments: {attribute.ConstructorArguments.Length}");
                    foreach (var arg in attribute.ConstructorArguments)
                    {
                        Console.WriteLine($"Arg type: {arg.Type}, Value: {arg.Value}, IsNull: {arg.IsNull}");
                    }
                    
                    // Special handling for test cases:
                    // If this is a test class and attribute has constructor arguments but first arg is null,
                    // try to extract the intended scene path from the attribute application syntax
                    if (attribute.ConstructorArguments.Length > 0)
                    {
                        if (!attribute.ConstructorArguments[0].IsNull)
                        {
                            var value = attribute.ConstructorArguments[0].Value?.ToString();
                            Console.WriteLine($"Extracted scene path from constructor arg: {value}");
                            return value;
                        }
                        else
                        {
                            // Try to extract from ApplicationSyntaxReference if available
                            var syntaxRef = attribute.ApplicationSyntaxReference;
                            if (syntaxRef != null)
                            {
                                var syntax = syntaxRef.GetSyntax();
                                var attributeText = syntax.ToString();
                                Console.WriteLine($"Attribute syntax: {attributeText}");
                                
                                // Extract scene path from something like [NodeGenerator("Scene.tscn")]
                                if (attributeText.Contains("(\"") && attributeText.Contains("\")"))
                                {
                                    int start = attributeText.IndexOf("(\"") + 2;
                                    int end = attributeText.IndexOf("\")", start);
                                    if (end > start)
                                    {
                                        var extractedPath = attributeText.Substring(start, end - start);
                                        Console.WriteLine($"Extracted scene path from syntax: {extractedPath}");
                                        return extractedPath;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            
            Console.WriteLine("No attribute with scene path found");
            return null;
        }

        // Helper method to infer scene path from class name
        private static string? InferScenePathFromClassName(INamedTypeSymbol classSymbol)
        {
            return $"{classSymbol.Name}.tscn";
        }

        // Syntax receiver that finds candidate classes
        private class SyntaxReceiver : ISyntaxReceiver
        {
            public List<ClassDeclarationSyntax> CandidateClasses { get; } = new List<ClassDeclarationSyntax>();

            public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
                // Check if this is a class declaration with attributes
                if (syntaxNode is ClassDeclarationSyntax classDeclaration && classDeclaration.AttributeLists.Count > 0)
                {
                    Console.WriteLine($"Found class with attributes: {classDeclaration.Identifier.Text}");
                    
                    // Look for our attribute
                    foreach (var attributeList in classDeclaration.AttributeLists)
                    {
                        foreach (var attribute in attributeList.Attributes)
                        {
                            string attributeName = attribute.Name.ToString();
                            Console.WriteLine($"Checking attribute: {attributeName}");
                            
                            if (attributeName == "NodeGenerator" ||
                                attributeName == "NodeGeneratorAttribute" ||
                                attributeName == "GodotNodeGenerator.NodeGenerator" || 
                                attributeName == "GodotNodeGenerator.NodeGeneratorAttribute")
                            {
                                Console.WriteLine($"Found matching NodeGenerator attribute on class {classDeclaration.Identifier.Text}");
                                CandidateClasses.Add(classDeclaration);
                                return;
                            }
                        }
                    }
                }
            }
        }
    }
}
