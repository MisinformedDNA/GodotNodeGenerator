using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Immutable;
using GodotNodeGenerator.Tests.TestHelpers;

namespace GodotNodeGenerator.Tests.ImprovedTests
{
    /// <summary>
    /// Example of parameterized tests with Theory attribute for more DRY tests
    /// </summary>
    public class ParameterizedNodeTests
    {
        // Define test data for node types
        public static IEnumerable<object[]> NodeTypeTestData =>
            new List<object[]>
            {
                new object[] { "Node2D", "A simple 2D node" },
                new object[] { "Sprite2D", "A 2D sprite with texture" },
                new object[] { "Label", "A UI label for text" },
                new object[] { "Button", "A UI button control" },
                new object[] { "RichTextLabel", "A rich text UI component" },
                new object[] { "Area2D", "A 2D area for collision detection" },
                new object[] { "CharacterBody2D", "A character controller" },
                new object[] { "StaticBody2D", "A static collision object" }
            };

        [Theory]
        [MemberData(nameof(NodeTypeTestData))]
        public void GeneratedCode_MatchesNodeType(string nodeType, string description)
        {
            // Arrange: Create a source file with the NodeGenerator attribute
            var sourceCode = $@"
using Godot;
using GodotNodeGenerator;

namespace TestNamespace
{{
    [NodeGenerator(""NodeTypeTest.tscn"")]
    public partial class {nodeType}Test : Node
    {{
    }}
}}";

            // Create a simple test scene with the specified node type
            var scenePath = "NodeTypeTest.tscn";
            var sceneContent = $@"
[gd_scene format=3]

[node name=""MyNode"" type=""{nodeType}""]
";

            // Act: Run the generator with our test data
            var outputs = RunSourceGenerator(sourceCode, [(scenePath, sceneContent)]);
            
            // Find the generated file
            var generatedFile = outputs.FirstOrDefault(f => f.HintName == $"{nodeType}Test.g.cs");
            generatedFile.Should().NotBeNull($"Expected to find generated file for {nodeType}");
            
            var generatedCode = generatedFile.SourceText.ToString();

            // Assert: Check that the generated code contains the correct type
            generatedCode.Should().Contain($"private {nodeType}? _MyNode;", 
                $"Should contain a field for the {nodeType} node");
            
            generatedCode.Should().Contain($"public {nodeType} MyNode", 
                $"Should contain a property for the {nodeType} node");
            
            generatedCode.Should().Contain($"public bool TryGetMyNode([NotNullWhen(true)] out {nodeType}? node)", 
                $"Should contain a TryGet method for the {nodeType} node");
            
            generatedCode.Should().Contain($"if (tempNode is {nodeType} typedNode)", 
                $"Should contain a type check for the {nodeType} node");
        }

        // Define test data for node relationships
        public static IEnumerable<object[]> NodeRelationshipTestData =>
            new List<object[]>
            {
                // Format: RootNodeName, RootNodeType, ChildNodeName, ChildNodeType
                new object[] { "Root", "Node2D", "Child", "Sprite2D" }, 
                new object[] { "UI", "Control", "Button", "Button" },
                new object[] { "Player", "CharacterBody2D", "Sprite", "Sprite2D" },
                new object[] { "Game", "Node", "Level", "Node2D" }
            };

        [Theory]
        [MemberData(nameof(NodeRelationshipTestData))]
        public void GeneratedCode_HandlesNodeRelationships(string rootName, string rootType, string childName, string childType)
        {
            // Arrange: Create a source file
            var sourceCode = @"
using Godot;
using GodotNodeGenerator;

namespace TestNamespace
{
    [NodeGenerator(""RelationshipTest.tscn"")]
    public partial class RelationshipTest : Node
    {
    }
}";

            // Create a scene with parent-child relationship
            var scenePath = "RelationshipTest.tscn";
            var sceneContent = $@"
[gd_scene format=3]

[node name=""{rootName}"" type=""{rootType}""]

[node name=""{childName}"" type=""{childType}"" parent=""{rootName}""]
";

            // Act: Run the generator
            var outputs = RunSourceGenerator(sourceCode, [(scenePath, sceneContent)]);
            var generatedFile = outputs.FirstOrDefault(f => f.HintName == "RelationshipTest.g.cs");
            var generatedCode = generatedFile.SourceText.ToString();

            // Assert: Check for root node properties
            generatedCode.Should().Contain($"private {rootType}? _{rootName};");
            generatedCode.Should().Contain($"public {rootType} {rootName}");
            
            // Check for child node properties
            generatedCode.Should().Contain($"private {childType}? _{childName};");
            generatedCode.Should().Contain($"public {childType} {childName}");
            
            // Check for node paths
            generatedCode.Should().Contain($"\"{rootName}\"");
            generatedCode.Should().Contain($"\"{rootName}/{childName}\"");
        }

        private static List<(string HintName, Microsoft.CodeAnalysis.Text.SourceText SourceText)> RunSourceGenerator(
            string sourceCode,
            IEnumerable<(string Path, string Content)> additionalFiles)
        {
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
            var generator = new NodeGenerator();
            ImmutableArray<ISourceGenerator> generators = [generator.AsSourceGenerator()];
            var driver = CSharpGeneratorDriver.Create(
                generators: generators,
                additionalTexts: additionalTexts);
            driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation);

            // Get the results
            var runResult = driver.GetRunResult();
            // Get all generated sources
            return [.. runResult.GeneratedTrees
                .Select(t => {
                    var sourceText = Microsoft.CodeAnalysis.Text.SourceText.From(t.GetText().ToString());
                    return (Path.GetFileName(t.FilePath), sourceText);
                })];
        }
    }
}
