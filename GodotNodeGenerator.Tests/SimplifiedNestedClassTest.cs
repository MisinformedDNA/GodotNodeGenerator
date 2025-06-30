using GodotNodeGenerator.Tests.TestHelpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;

namespace GodotNodeGenerator.Tests
{
    public class SimplifiedNestedClassTest : NodeGeneratorTestBase
    {
        [Fact]
        public void Simple_Test_For_Nested_Access()
        {
            // Arrange: Create a simple source file and scene 
            var sourceCode = @"
using Godot;
using GodotNodeGenerator;

namespace TestNamespace
{
    [NodeGenerator(""Simple.tscn"")]
    public partial class SimpleTest : Control
    {
        public override void _Ready()
        {
            MainPanel.Label.Text = ""Test"";
        }
    }
}";

            var scenePath = "Simple.tscn";
            var sceneContent = @"
[gd_scene format=3]

[node name=""SimpleTest"" type=""Control""]

[node name=""MainPanel"" type=""PanelContainer"" parent=""""]

[node name=""Label"" type=""Label"" parent=""MainPanel""]
";

            // Run the generator using the base class helper
            var outputs = RunSourceGenerator(sourceCode, [(scenePath, sceneContent)]);
            
            // Debug output
            Console.WriteLine("Generated files for simple nested class test:");
            foreach (var file in outputs)
            {
                Console.WriteLine($" - {file.HintName}");
            }

            // Get the generated code
            var generatedFile = outputs.FirstOrDefault(f => f.HintName.Contains("SimpleTest.g.cs"));
            
            // Check if we found the file
            Assert.NotNull(generatedFile.SourceText);
            var generatedCode = generatedFile.SourceText.ToString();

            // Output the generated code to make debugging easier
            Console.WriteLine("Generated code for simple nested class:");
            Console.WriteLine(generatedCode);

            // Basic assertions
            Assert.Contains("public class MainPanelWrapper", generatedCode);
            Assert.Contains("public Label Label", generatedCode);
        }
    }
}
