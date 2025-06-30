using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using FluentAssertions;
using System.Collections.Immutable;
using GodotNodeGenerator.Tests.TestHelpers;

namespace GodotNodeGenerator.Tests
{
    public class NodeNameConflictTests : NodeGeneratorTestBase
    {
        [Fact]
        public void Player_NodeInMainScriptPath_ShouldAppendNodeSuffix()
        {
            // Arrange
            var sourceCode = CreateSourceTemplate(
                className: "MainScriptPath", 
                namespaceName: "TestNamespace",
                scenePath: "Main.tscn");

            var sceneContent = @"
[gd_scene format=3]

[node name=""Main"" type=""Node2D""]

[node name=""Player"" type=""CharacterBody2D"" parent="".""]
";

            var additionalFiles = new[] { 
                ("Main.tscn", sceneContent)
            };

            // Act
            var generatedCode = RunGeneratorForSnapshot(sourceCode, additionalFiles, "MainScriptPath.g.cs");

            // Assert
            // The Player node should have properties generated
            generatedCode.Should().Contain("private CharacterBody2D? _Player;");
            generatedCode.Should().Contain("public CharacterBody2D Player");
            generatedCode.Should().Contain("public bool TryGetPlayer(");
        }
        
        [Fact]
        public void Player_NodeInPlayerClass_ShouldAppendNodeSuffix()
        {
            // This test verifies that a node named "player" (case insensitive) 
            // in a class named "Player" gets a "Node" suffix appended
            
            // Arrange
            var sourceCode = CreateSourceTemplate(
                className: "Player", 
                namespaceName: "TestNamespace",
                scenePath: "Player.tscn");

            var sceneContent = @"
[gd_scene format=3]

[node name=""Main"" type=""Node2D""]

[node name=""player"" type=""CharacterBody2D"" parent="".""]
";

            var additionalFiles = new[] { 
                ("Player.tscn", sceneContent)
            };

            // Act
            var generatedCode = RunGeneratorForSnapshot(sourceCode, additionalFiles, "Player.g.cs");

            // Assert
            // The player node should have a Node suffix to avoid conflict with the class name
            generatedCode.Should().Contain("private CharacterBody2D? _playerNode;");
            generatedCode.Should().Contain("public CharacterBody2D playerNode");
            generatedCode.Should().Contain("public bool TryGetplayerNode(");
        }
    }
}
