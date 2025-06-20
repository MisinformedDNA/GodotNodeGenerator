using GodotNodeGenerator.Tests.TestHelpers;

namespace GodotNodeGenerator.Tests
{
    /// <summary>
    /// Tests that verify code generation works with complex scene setups
    /// Uses the new test helper classes for clearer assertions
    /// </summary>
    public class ComplexSceneTests : NodeGeneratorTestBase
    {
        [Fact]
        public void Generated_Code_For_Complex_Scene_Structure()
        {
            // Arrange: Create source with NodeGenerator attribute
            var sourceCode = CreateSourceTemplate("ComplexSceneTest", scenePath: "ComplexScene.tscn");

            // Create a complex scene with multiple node types and relationships
            var scenePath = "ComplexScene.tscn";
            var sceneContent = @"
[gd_scene format=3]

[ext_resource type=""Script"" path=""res://scripts/Player.cs"" id=""1_player""]
[ext_resource type=""Script"" path=""res://scripts/Enemy.cs"" id=""2_enemy""]

[node name=""World"" type=""Node2D""]

[node name=""Player"" type=""CharacterBody2D"" parent=""World""]
script = ExtResource(""1_player"")
speed = 300.0

[node name=""Sprite"" type=""Sprite2D"" parent=""World/Player""]

[node name=""Camera"" type=""Camera2D"" parent=""World/Player""]
current = true

[node name=""Enemies"" type=""Node2D"" parent=""World""]

[node name=""Enemy1"" type=""CharacterBody2D"" parent=""World/Enemies""]
script = ExtResource(""2_enemy"")

[node name=""Enemy2"" type=""CharacterBody2D"" parent=""World/Enemies""]
script = ExtResource(""2_enemy"")

[node name=""UI"" type=""CanvasLayer"" parent="".""]

[node name=""HealthBar"" type=""ProgressBar"" parent=""UI""]
";

            // Act: Run the generator
            var outputs = RunSourceGenerator(sourceCode, [(scenePath, sceneContent)]);
            var generatedFile = outputs.FirstOrDefault(f => f.HintName == "ComplexSceneTest.g.cs");
            var generatedCode = generatedFile.SourceText.ToString();

            // Assert: Use the new helper methods for clearer assertions
            // Check general structure
            Assert.Contains("namespace TestNamespace", generatedCode);
            Assert.Contains("// Generated node accessors for ComplexSceneTest", generatedCode);
            
            // Check each node using our helper methods
            NodeGeneratorAssert.ContainsNodeProperty(generatedCode, "World", "Node2D");
            NodeGeneratorAssert.ContainsNodeDocumentation(generatedCode, "World", "World");
            
            NodeGeneratorAssert.ContainsNodeProperty(generatedCode, "Player", "CharacterBody2D");
            NodeGeneratorAssert.ContainsNodeDocumentation(generatedCode, "Player", "World/Player", "res://scripts/Player.cs");
            
            NodeGeneratorAssert.ContainsNodeProperty(generatedCode, "Sprite", "Sprite2D");
            NodeGeneratorAssert.ContainsNodeDocumentation(generatedCode, "Sprite", "World/Player/Sprite");
            
            NodeGeneratorAssert.ContainsNodeProperty(generatedCode, "Camera", "Camera2D");
            NodeGeneratorAssert.ContainsNodeDocumentation(generatedCode, "Camera", "World/Player/Camera");
            
            NodeGeneratorAssert.ContainsNodeProperty(generatedCode, "Enemies", "Node2D");
            NodeGeneratorAssert.ContainsNodeDocumentation(generatedCode, "Enemies", "World/Enemies");
            
            NodeGeneratorAssert.ContainsNodeProperty(generatedCode, "Enemy1", "CharacterBody2D");
            NodeGeneratorAssert.ContainsNodeDocumentation(generatedCode, "Enemy1", "World/Enemies/Enemy1", "res://scripts/Enemy.cs");
            
            NodeGeneratorAssert.ContainsNodeProperty(generatedCode, "Enemy2", "CharacterBody2D");
            NodeGeneratorAssert.ContainsNodeDocumentation(generatedCode, "Enemy2", "World/Enemies/Enemy2", "res://scripts/Enemy.cs");
            
            NodeGeneratorAssert.ContainsNodeProperty(generatedCode, "UI", "CanvasLayer");
            NodeGeneratorAssert.ContainsNodeDocumentation(generatedCode, "UI", "UI");
            
            NodeGeneratorAssert.ContainsNodeProperty(generatedCode, "HealthBar", "ProgressBar");
            NodeGeneratorAssert.ContainsNodeDocumentation(generatedCode, "HealthBar", "UI/HealthBar");
            
            // Verify null safety features are present
            NodeGeneratorAssert.ContainsNullSafetyFeatures(generatedCode);
        }
    }
}
