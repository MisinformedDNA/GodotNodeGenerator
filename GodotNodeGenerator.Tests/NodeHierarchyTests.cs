using GodotNodeGenerator.Tests.TestHelpers;

namespace GodotNodeGenerator.Tests
{
    /// <summary>
    /// Tests that focus on node path handling and complex hierarchy relationships
    /// </summary>
    public class NodeHierarchyTests : NodeGeneratorTestBase
    {
        [Fact]
        public void Generated_Code_Handles_Nested_Node_Paths_Correctly()
        {
            // Arrange: Create source with NodeGenerator attribute
            var sourceCode = CreateSourceTemplate("NestedTest", scenePath: "NestedScene.tscn");

            // Create a scene with a deep node hierarchy
            var scenePath = "NestedScene.tscn";
            var sceneContent = @"
[gd_scene format=3]

[node name=""Root"" type=""Node2D""]

[node name=""Level1"" type=""Node2D"" parent=""Root""]

[node name=""Level2"" type=""Node2D"" parent=""Root/Level1""]

[node name=""Level3"" type=""Sprite2D"" parent=""Root/Level1/Level2""]

[node name=""Sibling"" type=""Label"" parent=""Root""]
";

            // Act: Run the generator
            var outputs = RunSourceGenerator(sourceCode, [(scenePath, sceneContent)]);
            var generatedFile = outputs.FirstOrDefault(f => f.HintName == "NestedTest.g.cs");
            var generatedCode = generatedFile.SourceText.ToString();

            // Assert: Verify node paths are correct
            // Check that node paths are correctly preserved
            Assert.Contains("\"Root\"", generatedCode);
            Assert.Contains("\"Root/Level1\"", generatedCode);
            Assert.Contains("\"Root/Level1/Level2\"", generatedCode);
            Assert.Contains("\"Root/Level1/Level2/Level3\"", generatedCode);
            Assert.Contains("\"Root/Sibling\"", generatedCode);
            
            // Check property generation
            Assert.Contains("public Node2D Root", generatedCode);
            Assert.Contains("public Node2D Level1", generatedCode);
            Assert.Contains("public Node2D Level2", generatedCode);
            Assert.Contains("public Sprite2D Level3", generatedCode);
            Assert.Contains("public Label Sibling", generatedCode);
        }

        [Fact]
        public void Generated_Code_Handles_Special_Characters_In_Node_Names()
        {
            // Arrange: Create source with NodeGenerator attribute
            var sourceCode = CreateSourceTemplate("SpecialCharsTest", scenePath: "SpecialCharsScene.tscn");

            // Create a scene with nodes having special characters in names
            var scenePath = "SpecialCharsScene.tscn";
            var sceneContent = @"
[gd_scene format=3]

[node name=""Node-With-Dashes"" type=""Node2D""]

[node name=""Node With Spaces"" type=""Sprite2D"" parent=""Node-With-Dashes""]

[node name=""123NumberFirst"" type=""Label"" parent=""Node-With-Dashes""]
";

            // Act: Run the generator
            var outputs = RunSourceGenerator(sourceCode, [(scenePath, sceneContent)]);
            var generatedFile = outputs.FirstOrDefault(f => f.HintName == "SpecialCharsTest.g.cs");
            var generatedCode = generatedFile.SourceText.ToString();

            // Assert: Verify special character handling
            // Field name sanitization
            Assert.Contains("private Node2D? _Node_With_Dashes;", generatedCode);
            Assert.Contains("private Sprite2D? _Node_With_Spaces;", generatedCode);
            Assert.Contains("private Label? __123NumberFirst;", generatedCode);
            
            // Property name sanitization
            Assert.Contains("public Node2D Node_With_Dashes", generatedCode);
            Assert.Contains("public Sprite2D Node_With_Spaces", generatedCode);
            Assert.Contains("public Label _123NumberFirst", generatedCode);
            
            // Method name sanitization
            Assert.Contains("TryGetNode_With_Dashes", generatedCode);
            Assert.Contains("TryGetNode_With_Spaces", generatedCode);
            Assert.Contains("TryGet_123NumberFirst", generatedCode);
            
            // Original paths preserved in GetNode calls
            Assert.Contains("\"Node-With-Dashes\"", generatedCode);
            Assert.Contains("\"Node-With-Dashes/Node With Spaces\"", generatedCode);
            Assert.Contains("\"Node-With-Dashes/123NumberFirst\"", generatedCode);
        }

        [Fact]
        public void Generated_Code_Handles_Script_Attachments_Correctly()
        {
            // Arrange: Create source with NodeGenerator attribute
            var sourceCode = CreateSourceTemplate("ScriptTest", scenePath: "ScriptScene.tscn");

            // Create a scene with script references
            var scenePath = "ScriptScene.tscn";
            var sceneContent = @"
[gd_scene format=3]

[ext_resource type=""Script"" path=""res://scripts/CustomNode.cs"" id=""1_cscript""]
[ext_resource type=""Script"" path=""res://scripts/PlayerController.cs"" id=""2_pscript""]

[node name=""CustomNode"" type=""Node2D""]
script = ExtResource(""1_cscript"")

[node name=""Player"" type=""CharacterBody2D"" parent=""CustomNode""]
script = ExtResource(""2_pscript"")
";

            // Act: Run the generator
            var outputs = RunSourceGenerator(sourceCode, [(scenePath, sceneContent)]);
            var generatedFile = outputs.FirstOrDefault(f => f.HintName == "ScriptTest.g.cs");
            var generatedCode = generatedFile.SourceText.ToString();

            // Assert: Verify script associations are documented
            // Documentation includes script path information
            Assert.Contains("/// Gets the CustomNode node (path: \"CustomNode\") (script: \"", generatedCode);
            Assert.Contains("res://scripts/CustomNode.cs", generatedCode);
            
            Assert.Contains("/// Gets the Player node (path: \"CustomNode/Player\") (script: \"", generatedCode);
            Assert.Contains("res://scripts/PlayerController.cs", generatedCode);
        }
    }
}
