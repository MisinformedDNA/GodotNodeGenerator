using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using FluentAssertions;
using System.Collections.Immutable;
using GodotNodeGenerator.Tests.TestHelpers;

namespace GodotNodeGenerator.Tests
{
    public class NodeConflictTests : NodeGeneratorTestBase
    {
        [Fact]
        public void NodeWithSameNameAsClass_ShouldAppendNodeSuffix()
        {
            // Arrange
            var sourceCode = CreateSourceTemplate(
                className: "MainScriptPath", 
                namespaceName: "TestNamespace",
                scenePath: "Main.tscn");

            var sceneContent = @"
[gd_scene format=3]

[node name=""Main"" type=""Node2D""]

[node name=""MainScriptPath"" type=""CharacterBody2D"" parent="".""]
";

            var additionalFiles = new[] { 
                ("Main.tscn", sceneContent)
            };

            // Act
            var generatedCode = RunGeneratorForSnapshot(sourceCode, additionalFiles, "MainScriptPath.g.cs");

            // Assert
            // The class property should have a suffix to avoid conflict
            generatedCode.Should().Contain("private CharacterBody2D? _MainScriptPathNode;");
            generatedCode.Should().Contain("public CharacterBody2D MainScriptPathNode");
            generatedCode.Should().Contain("public bool TryGetMainScriptPathNode(");
        }

        [Fact]
        public void NodeWithSameNameAsClassInDifferentCase_ShouldAppendNodeSuffix()
        {
            // Arrange
            var sourceCode = CreateSourceTemplate(
                className: "Player", 
                namespaceName: "TestNamespace",
                scenePath: "PlayerScene.tscn");

            var sceneContent = @"
[gd_scene format=3]

[node name=""Main"" type=""Node2D""]

[node name=""player"" type=""CharacterBody2D"" parent="".""]
";

            var additionalFiles = new[] { 
                ("PlayerScene.tscn", sceneContent)
            };

            // Act
            var generatedCode = RunGeneratorForSnapshot(sourceCode, additionalFiles, "Player.g.cs");

            // Assert
            // The class property should have a suffix for case-insensitive match
            // Note: The field preserves the original case but adds Node suffix
            generatedCode.Should().Contain("private CharacterBody2D? _playerNode;");
            generatedCode.Should().Contain("public CharacterBody2D playerNode");
            generatedCode.Should().Contain("public bool TryGetplayerNode(");
        }

        [Fact]
        public void NodesWithSimilarNamesButDifferentCase_ShouldGenerateCorrectAccessors()
        {
            // Arrange
            var sourceCode = CreateSourceTemplate(
                className: "TestClass", 
                namespaceName: "TestNamespace",
                scenePath: "CaseTest.tscn");

            var sceneContent = @"
[gd_scene format=3]

[node name=""Root"" type=""Node2D""]

[node name=""Item"" type=""Sprite2D"" parent=""Root""]

[node name=""item"" type=""Label"" parent=""Root""]
";

            var additionalFiles = new[] { 
                ("CaseTest.tscn", sceneContent)
            };

            // Act
            var generatedCode = RunGeneratorForSnapshot(sourceCode, additionalFiles, "TestClass.g.cs");

            // Assert
            // Both nodes should generate properties with their original case preserved
            generatedCode.Should().Contain("private Sprite2D? _Item;");
            generatedCode.Should().Contain("public Sprite2D Item");
            
            // The second node with same name but different case should get a unique property name
            generatedCode.Should().Contain("private Label? _item;"); 
            generatedCode.Should().Contain("public Label item");
        }
    }
}
