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
    {
        [Fact]
        public void Missing_Scene_File_Reports_Diagnostic()
        {
            // Arrange: Create source with NodeGenerator attribute pointing to non-existent file
            var sourceCode = CreateSourceTemplate("MissingSceneTest", scenePath: "NonExistent.tscn");

            // Act: Run the generator with no scene files
            var outputs = RunSourceGenerator(sourceCode, []);

            // Assert: There should still be output but with proper fallback
            var generatedFile = outputs.FirstOrDefault(f => f.HintName == "MissingSceneTest.g.cs");
            generatedFile.Should().NotBeNull("because the generator should still produce an output file");
            
            var generatedCode = generatedFile.SourceText.ToString();
            // Should contain the empty regions as fallback
            generatedCode.Should().Contain("#region Node Tree Accessors")
                .And.Contain("#endregion")
                .And.NotContain("private Node");
        }

        [Fact]
        public void Invalid_Scene_Format_Falls_Back_To_Empty_Generator()
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
            
            // Assert: Should generate minimum valid code
            generatedFile.Should().NotBeNull();
            var generatedCode = generatedFile.SourceText.ToString();
            
            // Should contain class structure but no node accessors using FluentAssertions
            generatedCode.Should()
                .Contain("namespace TestNamespace").And
                .Contain("public partial class InvalidFormatTest").And
                .Contain("#region Node Tree Accessors").And
                .Contain("#endregion").And
                .NotContain("public Node");
        }

        [Fact] 
        public void Invalid_Node_Type_Defaults_To_Node()
        {
            // Arrange: Create source with NodeGenerator attribute
            var sourceCode = CreateSourceTemplate("InvalidNodeTypeTest", scenePath: "InvalidNodeType.tscn");

            // Create a scene with invalid node type
            var scenePath = "InvalidNodeType.tscn";
            var sceneContent = @"
[gd_scene format=3]

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

        [Fact]
        public async Task MissingScene_UsingSGTestingFramework()
        {
            // Using the Microsoft.CodeAnalysis.Testing framework for source generators
            // Arrange: Create a test with our source generator            // Suppress the obsolete warning for XUnitVerifier
            #pragma warning disable CS0618
            var test = new CSharpSourceGeneratorTest<NodeGenerator, XUnitVerifier>
            #pragma warning restore CS0618
            {
                TestCode = @"
using Godot;
using GodotNodeGenerator;

namespace ErrorTest
{
    [NodeGenerator(""MissingScene.tscn"")]
    public partial class MissingSceneTest : Node
    {
    }
}"
            };
              // Configure what diagnostic we expect
            test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("GNGEN001")
                .WithSpan(7, 6, 7, 41) // Line with attribute, column start and end 
                .WithMessage("*Scene file not found*")); // Partial message matching
                
            // Act & Assert
            await test.RunAsync();
        }
    }
}
