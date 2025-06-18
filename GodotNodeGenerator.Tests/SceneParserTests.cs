using Microsoft.CodeAnalysis;

namespace GodotNodeGenerator.Tests
{
    public class SceneParserTests
    {
        [Fact]
        public void ParseScene_ValidSceneContent_ReturnsCorrectNodes()
        {
            // Arrange
            var scenePath = "TestScene.tscn";
            var sceneContent = @"[gd_scene load_steps=4 format=3 uid=""uid://c1j5hxnhctilu""]

[ext_resource type=""Script"" path=""res://scripts/Player.cs"" id=""1_kqm1w""]
[ext_resource type=""Texture2D"" uid=""uid://c0ogahvvkpi8g"" path=""res://assets/player.png"" id=""2_5scav""]

[node name=""Root"" type=""Node2D""]

[node name=""Player"" type=""CharacterBody2D"" parent=""Root""]
script = ExtResource(""1_kqm1w"")

[node name=""Sprite"" type=""Sprite2D"" parent=""Player""]
texture = ExtResource(""2_5scav"")

[node name=""Camera"" type=""Camera2D"" parent=""Player""]
current = true";

            var additionalFiles = new List<AdditionalText>
            {
                new MockAdditionalText(scenePath, sceneContent)
            };

            // Act
            var nodes = SceneParser.ParseScene(scenePath, additionalFiles);

            // Assert
            Assert.Equal(4, nodes.Count);
            
            Assert.Contains(nodes, n => n.Name == "Root" && n.Type == "Node2D" && n.Path == "Root");
            Assert.Contains(nodes, n => n.Name == "Player" && n.Type == "CharacterBody2D" && n.Path == "Root/Player");
            Assert.Contains(nodes, n => n.Name == "Sprite" && n.Type == "Sprite2D" && n.Path == "Root/Player/Sprite");
            Assert.Contains(nodes, n => n.Name == "Camera" && n.Type == "Camera2D" && n.Path == "Root/Player/Camera");
        }

        [Fact]
        public void ParseScene_FileNotFound_ReturnsNodesFromDummyContent()
        {
            // Arrange
            var scenePath = "NonExistentScene.tscn";
            var additionalFiles = new List<AdditionalText>();

            // Act
            var nodes = SceneParser.ParseScene(scenePath, additionalFiles);

            // Assert - Should not be empty, as it returns dummy content
            Assert.NotEmpty(nodes);
            // The dummy content contains a Root node
            Assert.Contains(nodes, n => n.Name == "Root" && n.Type == "Node2D");
        }

        [Fact]
        public void ParseScene_NullAdditionalFiles_ReturnsDummyContent()
        {
            // Arrange
            var scenePath = "TestScene.tscn";

            // Act - Pass null for additionalFiles to test fallback behavior
            var nodes = SceneParser.ParseScene(scenePath, null);

            // Assert - Should return nodes from dummy content
            Assert.NotEmpty(nodes);
            // The dummy content contains a Root node
            Assert.Contains(nodes, n => n.Name == "Root" && n.Type == "Node2D");
        }

        [Fact]
        public void ParseScene_SceneFileLookup_FindsByName()
        {
            // Arrange
            var scenePath = "Player"; // No extension
            var sceneContent = @"[node name=""Player"" type=""CharacterBody2D""]";
            
            var additionalFiles = new List<AdditionalText>
            {
                new MockAdditionalText(@"C:\Project\Player.tscn", sceneContent)
            };

            // Act
            var nodes = SceneParser.ParseScene(scenePath, additionalFiles);

            // Assert
            Assert.Single(nodes);
            Assert.Equal("Player", nodes[0].Name);
        }

        [Fact]
        public void ParseScene_SceneFileLookup_FindsByPath()
        {
            // Arrange
            var scenePath = @"res://scenes/Player.tscn";
            var sceneContent = @"[node name=""Player"" type=""CharacterBody2D""]";
            
            var additionalFiles = new List<AdditionalText>
            {
                new MockAdditionalText(scenePath, sceneContent)
            };

            // Act
            var nodes = SceneParser.ParseScene(scenePath, additionalFiles);

            // Assert
            Assert.Single(nodes);
            Assert.Equal("Player", nodes[0].Name);
        }

        [Fact]
        public void ParseScene_WithDiagnostics_ReportsWarnings()
        {
            // Arrange
            var scenePath = "NonExistentScene.tscn";
            var additionalFiles = new List<AdditionalText>();
            var diagnosticReported = false;

            // Act
            var nodes = SceneParser.ParseScene(
                scenePath, 
                additionalFiles,
                diagnostic => {
                    diagnosticReported = true;
                    Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
                });

            // Assert
            Assert.True(diagnosticReported, "Diagnostic should be reported for missing scene file");
            // Note: The implementation returns dummy content, not an empty list
            Assert.NotEmpty(nodes);
        }
    }
}
