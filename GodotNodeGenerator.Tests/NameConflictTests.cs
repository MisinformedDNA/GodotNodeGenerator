using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;

namespace GodotNodeGenerator.Tests
{
    public class NameConflictTests
    {        [Fact]
        public void MakeSafeIdentifier_WhenNameMatchesClassName_AppendsNodeSuffix()
        {
            // Instead of using reflection, we'll test this through the GenerateNodeAccessors method
            // which will call MakeSafeIdentifier internally
            var nodeInfos = new List<NodeInfo>
            {
                new() {
                    Name = "TestClass", // Name matches class name
                    Type = "Node",
                    Path = "TestClass"
                }
            };            // Generate code for class with same name as node
            var generatedCode = SourceGenerationHelper.GenerateNodeAccessors(
                "TestNamespace", "TestClass", "Node", nodeInfos);

            // Verify proper suffix was added to avoid conflict
            Assert.Contains("private Node? _TestClassNode;", generatedCode);
            Assert.Contains("public Node TestClassNode", generatedCode);
            Assert.Contains("public bool TryGetTestClassNode", generatedCode);

            // Now try with a different class name (no conflict)
            var generatedCode2 = SourceGenerationHelper.GenerateNodeAccessors(
                "TestNamespace", "DifferentClass", "Node", nodeInfos);

            // Verify no suffix was added when no conflict
            Assert.Contains("private Node? _TestClass;", generatedCode2);
            Assert.Contains("public Node TestClass", generatedCode2);
            Assert.Contains("public bool TryGetTestClass", generatedCode2);
        }

        [Fact]
        public void GenerateNodeAccessors_WhenRootNodeSameAsClass_AppendsNodeSuffix()
        {
            // Arrange
            var nodeInfos = new List<NodeInfo>
            {
                new() {
                    Name = "Player", // Node name same as class name
                    Type = "CharacterBody2D",
                    Path = "Player"
                },
                new() {
                    Name = "Sprite",
                    Type = "Sprite2D",
                    Path = "Player/Sprite"
                }
            };            // Act
            var generatedCode = SourceGenerationHelper.GenerateNodeAccessors("TestNamespace", "Player", "Node2D", nodeInfos);

            // Assert
            // Verify property is renamed to avoid conflict
            Assert.Contains("private CharacterBody2D? _PlayerNode;", generatedCode);
            Assert.Contains("public CharacterBody2D PlayerNode", generatedCode);
            Assert.Contains("public bool TryGetPlayerNode", generatedCode);
            
            // Verify Node Tree Accessors use the modified name
            Assert.Contains("PlayerNodeWrapper", generatedCode);
            
            // Verify other nodes are not modified
            Assert.Contains("private Sprite2D? _Sprite;", generatedCode);
            Assert.Contains("public Sprite2D Sprite", generatedCode);
        }

        [Fact]
        public void GenerateNodeAccessors_WithNestedNodesMatchingClassName_HandlesCorrectly()
        {
            // Arrange
            var nodeInfos = new List<NodeInfo>
            {
                new() {
                    Name = "Root",
                    Type = "Node2D",
                    Path = "Root"
                },
                new() {
                    Name = "Enemy", // This node has same name as class
                    Type = "CharacterBody2D",
                    Path = "Root/Enemy" 
                },
                new() {
                    Name = "Sprite",
                    Type = "Sprite2D",
                    Path = "Root/Enemy/Sprite"
                }
            };            // Act
            var generatedCode = SourceGenerationHelper.GenerateNodeAccessors("TestNamespace", "Enemy", "Node2D", nodeInfos);

            // Assert
            // Verify non-root nodes with name conflicts are also handled
            Assert.Contains("private CharacterBody2D? _EnemyNode;", generatedCode);
            Assert.Contains("public CharacterBody2D EnemyNode", generatedCode);
            
            // Verify regular nodes are unchanged
            Assert.Contains("private Node2D? _Root;", generatedCode);
            Assert.Contains("public Node2D Root", generatedCode);
        }

        [Fact]
        public void GenerateNodeAccessors_WrapperClassesHandleNameConflicts()
        {
            // Test that wrapper classes properly handle name conflicts
            var nodeInfos = new List<NodeInfo>
            {
                new() {
                    Name = "Player", // Root node with same name as class
                    Type = "Node2D",
                    Path = "Player"
                },
                new() {
                    Name = "Sprite", 
                    Type = "Sprite2D",
                    Path = "Player/Sprite" // Child of root node
                }
            };            // Act
            var generatedCode = SourceGenerationHelper.GenerateNodeAccessors("TestNamespace", "Player", "Node2D", nodeInfos);

            // Assert
            // Check wrapper class name uses the modified property name
            Assert.Contains("public class PlayerNodeWrapper", generatedCode);
            
            // Check wrapper class references use the modified property name
            Assert.Contains("private PlayerNodeWrapper? _PlayerNodeWrapper;", generatedCode);
            Assert.Contains("public PlayerNodeWrapper PlayerNode", generatedCode);
            Assert.Contains("_PlayerNodeWrapper = new PlayerNodeWrapper(this, PlayerNode);", generatedCode);
        }
    }
}
