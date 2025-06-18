using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace GodotNodeGenerator.Tests
{
    public class NodeGeneratorTests
    {
        [Fact]
        public void Initialize_RegistersCorrectOutputs()
        {
            // TODO: Implement tests for NodeGenerator
            // This will require setting up a more complex test environment with Roslyn compilation
        }

        [Fact]
        public void Execute_WithValidClass_GeneratesCorrectCode()
        {
            // This test will be implemented in a future PR
            // It requires setting up a real compilation and running the source generator against it
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
    }    // Using MockAdditionalText from TestHelpers namespace
}
