using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Reflection;
using System.Text.RegularExpressions;

namespace GodotNodeGenerator.Tests
{
    public partial class NodeGeneratorTests
    {
        [Fact]
        public void TestNodeInfoParsing()
        {
            // Create a mock scene content
            var scenePath = "Player.tscn";
            var sceneContent = @"
[node name=""Root"" type=""Node2D""]
[node name=""Player"" type=""CharacterBody2D"" parent=""Root""]
[node name=""Sprite"" type=""Sprite2D"" parent=""Player""]
[node name=""Camera"" type=""Camera2D"" parent=""Player""]
script = ExtResource(""1_script"")
";

            // Create a mock collection of AdditionalText files
            var additionalFiles = ImmutableArray.Create<AdditionalText>(
                new MockAdditionalText(scenePath, sceneContent)
            );

            // Parse the scene using AdditionalFiles
            var nodeInfo = SceneParser.ParseScene(scenePath, additionalFiles);

            // Verify that the scene was parsed correctly
            Assert.Equal(4, nodeInfo.Count);

            // Verify Root node
            var rootNode = nodeInfo.FirstOrDefault(n => n.Name == "Root");
            Assert.Equal("Node2D", rootNode.Type);
            Assert.Equal("Root", rootNode.Path);

            // Verify Player node
            var playerNode = nodeInfo.FirstOrDefault(n => n.Name == "Player");
            Assert.Equal("CharacterBody2D", playerNode.Type);
            Assert.Equal("Root/Player", playerNode.Path);

            // Verify Sprite node
            var spriteNode = nodeInfo.FirstOrDefault(n => n.Name == "Sprite");
            Assert.Equal("Sprite2D", spriteNode.Type);
            Assert.Equal("Root/Player/Sprite", spriteNode.Path);

            // Verify Camera node
            var cameraNode = nodeInfo.FirstOrDefault(n => n.Name == "Camera");
            Assert.Equal("Camera2D", cameraNode.Type);
            Assert.Equal("Root/Player/Camera", cameraNode.Path);
        }

        [Fact]
        public void Execute_WithValidClass_GeneratesCorrectCode()
        {
            // Arrange: Create a simple C# file with the NodeGenerator attribute
            var sourceCode = @"
using System;
using Godot;
using GodotNodeGenerator;

namespace TestNamespace
{
    [NodeGenerator(""Player.tscn"")]
    public partial class TestPlayer : CharacterBody2D
    {
    }
}";
            // Create a scene file matching the one referenced in the attribute
            var scenePath = "Player.tscn";
            var sceneContent = @"
[gd_scene load_steps=3 format=3 uid=""uid://test""]

[ext_resource type=""Script"" path=""res://scripts/Player.cs"" id=""1_script""]

[node name=""Root"" type=""Node2D""]

[node name=""Player"" type=""CharacterBody2D"" parent=""Root""]
script = ExtResource(""1_script"")

[node name=""Sprite"" type=""Sprite2D"" parent=""Player""]
texture = ExtResource(""2_tex"")

[node name=""Camera"" type=""Camera2D"" parent=""Player""]
current = true
";

            // Act: Run the generator
            var outputs = RunSourceGenerator(sourceCode, [(scenePath, sceneContent)]);

            // Assert: Check that generated code matches our expectations
            var generatedFile = outputs.FirstOrDefault(f => f.HintName == "TestPlayer.g.cs");

            var generatedCode = generatedFile.SourceText.ToString();

            // Verify namespace and class declaration
            Assert.Contains("namespace TestNamespace", generatedCode);
            Assert.Contains("public partial class TestPlayer", generatedCode);

            // Check for Root node accessor
            Assert.Contains("private Node2D? _Root;", generatedCode);
            Assert.Contains("public Node2D Root", generatedCode);
            Assert.Contains("TryGetRoot(", generatedCode);

            // Check for Sprite node accessor
            Assert.Contains("private Sprite2D? _Sprite;", generatedCode);
            Assert.Contains("public Sprite2D Sprite", generatedCode);
            Assert.Contains("TryGetSprite(", generatedCode);

            // Check for Camera node accessor
            Assert.Contains("private Camera2D? _Camera;", generatedCode);
            Assert.Contains("public Camera2D Camera", generatedCode);
            Assert.Contains("TryGetCamera(", generatedCode);

            // Check for improved type safety features
            Assert.Contains("[return: NotNull]", generatedCode);
            Assert.Contains("throw new NullReferenceException", generatedCode);
            Assert.Contains("throw new InvalidCastException", generatedCode);
            Assert.Contains("[NotNullWhen(true)]", generatedCode);

            // Check for proper node paths
            Assert.Contains("\"Root\"", generatedCode);
            Assert.Contains("\"Root/Player/Sprite\"", generatedCode);
            Assert.Contains("\"Root/Player/Camera\"", generatedCode);

            // Check for script detection
            Assert.Contains("(script:", generatedCode);
        }

        [Fact]
        public void GenerateNodeAccessors_FormatTest_MatchesExpectedOutput()
        {
            // Arrange
            var nodeInfos = new List<NodeInfo>
            {
                new() {
                    Name = "TestNode",
                    Type = "Sprite2D",
                    Path = "TestNode",
                    Script = "res://scripts/TestNode.cs"
                }
            };

            // Act
            var generatedCode = SourceGenerationHelper.GenerateNodeAccessors("TestNamespace", "TestClass", nodeInfos);

            // Assert
            // Verify the basic structure is correct
            Assert.Contains("namespace TestNamespace", generatedCode);
            Assert.Contains("public partial class TestClass", generatedCode);
            Assert.Contains("private Sprite2D? _TestNode;", generatedCode);
            Assert.Contains("public Sprite2D TestNode", generatedCode);
            Assert.Contains("public bool TryGetTestNode", generatedCode);

            // Verify type safety features
            Assert.Contains("throw new NullReferenceException", generatedCode);
            Assert.Contains("throw new InvalidCastException", generatedCode);
            Assert.Contains("[return: NotNull]", generatedCode);
            Assert.Contains("[NotNullWhen(true)]", generatedCode);

            // Verify script detection is working
            Assert.Contains("(script: \"res://scripts/TestNode.cs\")", generatedCode);

            // Normalize whitespace for a more reliable comparison
            var normalizedCode = NormalizeWhitespace(generatedCode);

            // Verify specific formatting expectations
            Assert.Contains("if (_TestNode == null)", normalizedCode);
            Assert.Contains("var node = GetNodeOrNull(\"TestNode\");", normalizedCode);
            Assert.Contains("public bool TryGetTestNode([NotNullWhen(true)] out Sprite2D? node)", normalizedCode);
        }

        [Fact]
        public void NodeGenerator_UsesAdditionalFiles_ToAccessSceneFiles()
        {
            // Create a mock scene content
            var scenePath = "Player.tscn";
            var sceneContent = @"
[node name=""Root"" type=""Node2D""]
[node name=""Player"" type=""CharacterBody2D"" parent=""Root""]
[node name=""Sprite"" type=""Sprite2D"" parent=""Player""]
";

            // Create a mock collection of AdditionalText files
            var additionalFiles = ImmutableArray.Create<AdditionalText>(
                new MockAdditionalText(scenePath, sceneContent)
            );

            // Parse the scene using AdditionalFiles
            var nodeInfo = SceneParser.ParseScene(scenePath, additionalFiles);

            // Verify that AdditionalFiles were used correctly
            Assert.Equal(3, nodeInfo.Count);
            Assert.Contains(nodeInfo, n => n.Name == "Root");
            Assert.Contains(nodeInfo, n => n.Name == "Player");
            Assert.Contains(nodeInfo, n => n.Name == "Sprite");
        }

        [Fact]
        public void GeneratedCode_ContainsCorrectNodePaths()
        {
            // Arrange: Create scene with nested structure
            var scenePath = "NestedScene.tscn";
            var sceneContent = @"
[node name=""Root"" type=""Node2D""]

[node name=""UI"" type=""CanvasLayer"" parent="".""]

[node name=""HealthBar"" type=""ProgressBar"" parent=""UI""]

[node name=""Player"" type=""CharacterBody2D"" parent=""Root""]

[node name=""Sprite"" type=""Sprite2D"" parent=""Player""]

[node name=""Camera"" type=""Camera2D"" parent=""Player""]
";

            // Create source code using the scene
            var sourceCode = @"
using Godot;
using GodotNodeGenerator;

namespace Test
{
    [NodeGenerator(""NestedScene.tscn"")]
    public partial class NestedTest : Node
    {
    }
}";

            // Act: Run the generator
            var outputs = RunSourceGenerator(sourceCode, [(scenePath, sceneContent)]);
            var generatedFile = outputs.FirstOrDefault(f => f.HintName == "NestedTest.g.cs");
            var generatedCode = generatedFile.SourceText.ToString();

            // Assert: Check for correct paths
            Assert.Contains("\"Root\"", generatedCode);
            Assert.Contains("\"UI\"", generatedCode);
            Assert.Contains("\"UI/HealthBar\"", generatedCode);
            Assert.Contains("\"Root/Player\"", generatedCode);
            Assert.Contains("\"Root/Player/Sprite\"", generatedCode);
            Assert.Contains("\"Root/Player/Camera\"", generatedCode);
        }

        #region Test Helpers

        private static List<(string HintName, SourceText SourceText)> RunSourceGenerator(
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
                // Add a reference to a fake Godot assembly
                MetadataReference.CreateFromFile(Assembly.GetExecutingAssembly().Location)
            };

            var compilation = CSharpCompilation.Create(
                "TestCompilation",
                [syntaxTree],
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));            // Create the generator and driver
            var generator = new NodeGenerator();
            var driver = CSharpGeneratorDriver.Create(
                generators: [generator.AsSourceGenerator()],
                additionalTexts: additionalTexts);            // Run the generator
            driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation);

            // Get the results
            var runResult = driver.GetRunResult();

            // Get all generated sources
            return [.. runResult.GeneratedTrees
                .Select(t => (t.FilePath, SourceText.From(t.GetText().ToString())))
                .Select(f => (Path.GetFileName(f.FilePath), f.Item2))];
        }

        private static string NormalizeWhitespace(string code)
        {
            // Remove extra whitespace and normalize line endings
            return RemoveWhitespaceRegex().Replace(code, " ").Trim();
        }

        [GeneratedRegex(@"\s+")]
        private static partial Regex RemoveWhitespaceRegex();

        #endregion
    }
}
