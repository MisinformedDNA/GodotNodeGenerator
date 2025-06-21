using FluentAssertions;
using GodotNodeGenerator.Tests.TestHelpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;

namespace GodotNodeGenerator.Tests
{
    /// <summary>
    /// Tests that verify the source generator's behavior for error cases
    /// </summary>
    public class ErrorHandlingTests : NodeGeneratorTestBase
    {        [Fact]
        public void Missing_Scene_File_Should_Not_Generate_Code()
        {
            // Arrange: Create source with NodeGenerator attribute pointing to non-existent file
            var sourceCode = CreateSourceTemplate("MissingSceneTest", scenePath: "NonExistent.tscn");

            // Act: Run the generator with no scene files
            var outputs = RunSourceGenerator(sourceCode, []);

            // Assert: No code should be generated
            var generatedFile = outputs.FirstOrDefault(f => f.HintName == "MissingSceneTest.g.cs");
            
            // NodeGenerator skips code generation if no nodes are found (as per line 100-104 in NodeGenerator.cs)
            // So we expect generatedFile to be empty or null
            (generatedFile.HintName == null || generatedFile.SourceText == null).Should().BeTrue(
                "because the generator should not produce output when no scene file is found");
        }        [Fact]
        public void Invalid_Scene_Format_Should_Not_Generate_Code()
        {
            // Arrange: Create source with NodeGenerator attribute 
            var sourceCode = CreateSourceTemplate("InvalidFormatTest", scenePath: "InvalidFormat.tscn");

            // Create a scene with invalid format
            var scenePath = "InvalidFormat.tscn";
            var sceneContent = @"
This is not a valid scene file format
Just random text that doesn't match the expected structure
";

            // Act: Run the generator
            var outputs = RunSourceGenerator(sourceCode, [(scenePath, sceneContent)]);
            var generatedFile = outputs.FirstOrDefault(f => f.HintName == "InvalidFormatTest.g.cs");
            
            // Assert: No code should be generated for invalid format
            (generatedFile.HintName == null || generatedFile.SourceText == null).Should().BeTrue(
                "because the generator should not produce output for invalid scene format");
        }

        [Fact] 
        public void Invalid_Node_Type_Defaults_To_Node()
        {
            // Arrange: Create source with NodeGenerator attribute
            var sourceCode = CreateSourceTemplate("InvalidNodeTypeTest", scenePath: "InvalidNodeType.tscn");

            // Create a scene with invalid node type
            var scenePath = "InvalidNodeType.tscn";
            var sceneContent = @"[gd_scene format=3]

[node name=""TestNode"" type=""NonExistentNodeType""]
";

            // Act: Run the generator
            var outputs = RunSourceGenerator(sourceCode, [(scenePath, sceneContent)]);
            var generatedFile = outputs.FirstOrDefault(f => f.HintName == "InvalidNodeTypeTest.g.cs");
            
            // Assert: Should default to Node type using FluentAssertions
            generatedFile.Should().NotBeNull();
            var generatedCode = generatedFile.SourceText.ToString();
            
            // Should contain fallback to Node type
            generatedCode.Should().Contain("private Node? _TestNode;");
            generatedCode.Should().Contain("public Node TestNode");
        }
    }
}
