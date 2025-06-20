using GodotNodeGenerator.Tests.TestHelpers;
using Microsoft.CodeAnalysis.Text;

namespace GodotNodeGenerator.Tests
{
    /// <summary>
    /// Tests that verify the structure and format of generated code
    /// </summary>
    public class CodeStructureTests : NodeGeneratorTestBase
    {
        [Fact]
        public void Generated_Code_Has_Expected_Basic_Structure()
        {
            // Arrange: Create source with NodeGenerator attribute
            var sourceCode = CreateSourceTemplate("TestClass", scenePath: "SimpleScene.tscn");

            // Create a simple test scene
            var scenePath = "SimpleScene.tscn";
            var sceneContent = @"
[gd_scene format=3]

[node name=""Root"" type=""Node2D""]

[node name=""Child"" type=""Sprite2D"" parent=""Root""]
";

            // Act: Run the generator
            var outputs = RunSourceGenerator(sourceCode, [(scenePath, sceneContent)]);
            var generatedFile = outputs.FirstOrDefault(f => f.HintName == "TestClass.g.cs");
            var generatedCode = generatedFile.SourceText.ToString();

            // Assert: Verify basic structure with clear failure messages
            Assert.Contains("namespace TestNamespace", generatedCode);
            Assert.Contains("public partial class TestClass", generatedCode);
            Assert.Contains("// Generated node accessors for TestClass", generatedCode);

            // Verify property declarations
            Assert.Contains("private Node2D? _Root;", generatedCode);
            Assert.Contains("public Node2D Root", generatedCode);
            
            // Verify Try/Get pattern
            Assert.Contains("public bool TryGetRoot(", generatedCode);

            // Child node checks
            Assert.Contains("private Sprite2D? _Child;", generatedCode);
            Assert.Contains("public Sprite2D Child", generatedCode);
            Assert.Contains("public bool TryGetChild(", generatedCode);
        }

        [Fact]
        public void Generated_Code_Provides_Correct_XML_Documentation()
        {
            // Arrange: Create source with NodeGenerator attribute
            var sourceCode = CreateSourceTemplate("DocTest", scenePath: "DocScene.tscn");

            // Create a scene with scripts and properties
            var scenePath = "DocScene.tscn";
            var sceneContent = @"
[gd_scene format=3]

[ext_resource type=""Script"" path=""res://scripts/PlayerScript.cs"" id=""1_script""]

[node name=""Player"" type=""CharacterBody2D""]
script = ExtResource(""1_script"")
speed = 300.0
";

            // Act: Run the generator
            var outputs = RunSourceGenerator(sourceCode, [(scenePath, sceneContent)]);
            var generatedFile = outputs.FirstOrDefault(f => f.HintName == "DocTest.g.cs");
            var generatedCode = generatedFile.SourceText.ToString();

            // Assert: Verify XML documentation is complete and accurate
            // Summary tags
            Assert.Contains("/// <summary>", generatedCode);
            Assert.Contains("/// Gets the Player node (path: \"Player\") (script: \"", generatedCode);
            Assert.Contains("/// </summary>", generatedCode);
            
            // Exception documentation
            Assert.Contains("/// <exception cref=\"InvalidCastException\">", generatedCode);
            Assert.Contains("/// <exception cref=\"NullReferenceException\">", generatedCode);

            // TryGet documentation
            Assert.Contains("/// Tries to get the Player node", generatedCode);
            Assert.Contains("/// without throwing exceptions", generatedCode);
        }

        [Fact]
        public void Generated_Code_Includes_Appropriate_Type_Safety_Features()
        {
            // Arrange: Create source with NodeGenerator attribute
            var sourceCode = CreateSourceTemplate("TypeSafetyTest", scenePath: "TypeSafetyScene.tscn");

            // Create a simple scene for testing
            var scenePath = "TypeSafetyScene.tscn";
            var sceneContent = @"
[gd_scene format=3]

[node name=""TestNode"" type=""Node2D""]
";

            // Act: Run the generator
            var outputs = RunSourceGenerator(sourceCode, [(scenePath, sceneContent)]);
            var generatedFile = outputs.FirstOrDefault(f => f.HintName == "TypeSafetyTest.g.cs");
            var generatedCode = generatedFile.SourceText.ToString();

            // Assert: Verify type safety features are present
            // Check for null-safety features
            Assert.Contains("private Node2D? _TestNode;", generatedCode);
            Assert.Contains("[return: NotNull]", generatedCode);
            
            // Null checks
            Assert.Contains("if (_TestNode == null)", generatedCode);
            Assert.Contains("if (node == null)", generatedCode);
            
            // Exception handling
            Assert.Contains("throw new NullReferenceException(", generatedCode);
            Assert.Contains("throw new InvalidCastException(", generatedCode);
            
            // NotNullWhen attribute for TryGet pattern
            Assert.Contains("[NotNullWhen(true)]", generatedCode);
            Assert.Contains("public bool TryGetTestNode([NotNullWhen(true)] out Node2D? node)", generatedCode);
        }
    }
}
