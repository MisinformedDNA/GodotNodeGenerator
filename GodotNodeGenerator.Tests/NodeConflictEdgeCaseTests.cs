using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using FluentAssertions;
using System.Collections.Immutable;
using GodotNodeGenerator.Tests.TestHelpers;

namespace GodotNodeGenerator.Tests
{
    public class NodeConflictEdgeCaseTests : NodeGeneratorTestBase
    {
        [Fact]
        public void NodeWithSameNameAsClassDifferentCase_ShouldGenerateDistinctProperty()
        {
            // Arrange
            var sourceCode = CreateSourceTemplate(
                className: "MainScriptPath", 
                namespaceName: "TestNamespace",
                scenePath: "Main.tscn");

            var sceneContent = @"
[gd_scene format=3]

[node name=""Main"" type=""Node2D""]

[node name=""mainScriptPath"" type=""CharacterBody2D"" parent="".""]
";

            var additionalFiles = new[] { 
                ("Main.tscn", sceneContent)
            };

            // Act
            var generatedCode = RunGeneratorForSnapshot(sourceCode, additionalFiles, "MainScriptPath.g.cs");

            // Assert
            // The property should have Node suffix to avoid conflict with class
            generatedCode.Should().Contain("private CharacterBody2D? _mainScriptPathNode;");
            generatedCode.Should().Contain("public CharacterBody2D mainScriptPathNode");
            generatedCode.Should().Contain("public bool TryGetmainScriptPathNode(");
            
            // Verify the GetNodeOrNull call uses correct original path
            generatedCode.Should().Contain("this.GetNodeOrNull(\"mainScriptPath\");");
        }
        
        [Fact]
        public void NodeWithPartialCaseMatchToClass_ShouldNotHaveSuffix()
        {
            // Arrange
            var sourceCode = CreateSourceTemplate(
                className: "PlayerController", 
                namespaceName: "TestNamespace",
                scenePath: "Player.tscn");

            var sceneContent = @"
[gd_scene format=3]

[node name=""Root"" type=""Node2D""]

[node name=""Player"" type=""CharacterBody2D"" parent=""Root""]
";

            var additionalFiles = new[] { 
                ("Player.tscn", sceneContent)
            };

            // Act
            var generatedCode = RunGeneratorForSnapshot(sourceCode, additionalFiles, "PlayerController.g.cs");

            // Assert
            // The property should NOT have Node suffix as it doesn't match the class name
            generatedCode.Should().Contain("private CharacterBody2D? _Player;");
            generatedCode.Should().Contain("public CharacterBody2D Player");
            generatedCode.Should().Contain("public bool TryGetPlayer(");
            
            // Verify the GetNodeOrNull call uses correct original path
            generatedCode.Should().Contain("this.GetNodeOrNull(\"Root/Player\");");
        }
    }
}
