using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace GodotNodeGenerator
{
    [Generator(LanguageNames.CSharp)]
    public class NodeGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // Register the attribute source
            context.RegisterPostInitializationOutput(ctx => ctx.AddSource(
                "NodeGeneratorAttribute.g.cs",
                SourceText.From(SourceGenerationHelper.AttributeText, Encoding.UTF8)));            // Create a pipeline for all C# classes with our attribute
            var classDeclarations = context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: static (s, _) => IsTargetForGeneration(s),
                    transform: static (ctx, _) => GetTargetForGeneration(ctx))
                .Where(static m => m is not null);

            // Get access to AdditionalFiles
            var additionalFiles = context.AdditionalTextsProvider;

            // Combine with compilation and additional files
            var inputs = context.CompilationProvider
                .Combine(classDeclarations.Collect())
                .Combine(additionalFiles.Collect());

            // Register the source output generator
            context.RegisterSourceOutput(inputs, 
                (spc, tuple) => Execute(spc, tuple.Left.Left, tuple.Left.Right, tuple.Right));
        }

        private static bool IsTargetForGeneration(SyntaxNode node)
        {
            // Check if this is a class declaration with attributes
            return node is ClassDeclarationSyntax classDeclaration && classDeclaration.AttributeLists.Count > 0;
        }

        private static ClassDeclarationSyntax? GetTargetForGeneration(GeneratorSyntaxContext context)
        {
            var classDeclarationSyntax = (ClassDeclarationSyntax)context.Node;
            
            // Check if the class has our attribute
            foreach (var attributeList in classDeclarationSyntax.AttributeLists)
            {
                foreach (var attribute in attributeList.Attributes)
                {
                    if (context.SemanticModel.GetSymbolInfo(attribute).Symbol is not IMethodSymbol attributeSymbol)
                    {
                        continue;
                    }

                    var attributeContainingTypeSymbol = attributeSymbol.ContainingType;
                    var fullName = attributeContainingTypeSymbol.ToDisplayString();

                    if (fullName == "GodotNodeGenerator.NodeGeneratorAttribute")
                    {
                        return classDeclarationSyntax;
                    }
                }
            }

            return null;
        }

        private static void Execute(SourceProductionContext context, Compilation compilation,
            ImmutableArray<ClassDeclarationSyntax?> classes, ImmutableArray<AdditionalText> additionalFiles)
        {
            if (classes.IsDefaultOrEmpty)
            {
                return;
            }

            foreach (var classDeclaration in classes)
            {
                if (classDeclaration == null) continue;

                // Get the semantic model and symbol for the class
                var semanticModel = compilation.GetSemanticModel(classDeclaration.SyntaxTree);
                if (semanticModel.GetDeclaredSymbol(classDeclaration) is not INamedTypeSymbol classSymbol)
                {
                    continue;
                }

                // Extract attribute data and scene path
                var scenePath = ExtractScenePathFromAttribute(classSymbol);
                if (string.IsNullOrEmpty(scenePath))
                {
                    // Try to infer scene path from class name
                    scenePath = InferScenePathFromClassName(classSymbol);
                }

                if (string.IsNullOrEmpty(scenePath))
                {
                    // Report diagnostic that scene file wasn't found
                    context.ReportDiagnostic(Diagnostic.Create(
                        new DiagnosticDescriptor(
                            "GNGEN001",
                            "Scene file not found",
                            $"Could not find scene file for class {classSymbol.Name}",
                            "GodotNodeGenerator",
                            DiagnosticSeverity.Warning,
                            isEnabledByDefault: true),
                        classDeclaration.GetLocation()));
                    continue;
                }

                // Parse the scene using AdditionalFiles
                var nodeInfo = SceneParser.ParseScene(scenePath!, additionalFiles, context.ReportDiagnostic);

                // If no nodes are found (scene file missing or empty), skip code generation
                if (nodeInfo == null || nodeInfo.Count == 0)
                {
                    continue;
                }

                // Generate the code
                var generatedCode = SourceGenerationHelper.GenerateNodeAccessors(
                    classSymbol.ContainingNamespace.ToDisplayString(),
                    classSymbol.Name,
                    nodeInfo);

                // Add the generated code to the compilation
                context.AddSource(
                    $"{classSymbol.Name}.g.cs",
                    SourceText.From(generatedCode, Encoding.UTF8));
            }
        }

        private static string? ExtractScenePathFromAttribute(INamedTypeSymbol classSymbol)
        {
            foreach (var attribute in classSymbol.GetAttributes())
            {
                if (attribute.AttributeClass?.ToDisplayString() == "GodotNodeGenerator.NodeGeneratorAttribute")
                {
                    if (attribute.ConstructorArguments.Length > 0 && 
                        !attribute.ConstructorArguments[0].IsNull)
                    {
                        return attribute.ConstructorArguments[0].Value?.ToString();
                    }
                }
            }
            
            return null;
        }

        private static string? InferScenePathFromClassName(INamedTypeSymbol classSymbol)
        {
            // This would search for a scene file with the same name as the class
            // E.g., for class "Player", look for "Player.tscn"
            // For simplicity, we'll just return a fake path for demo purposes
            
            return $"{classSymbol.Name}.tscn";
        }
    }
}
