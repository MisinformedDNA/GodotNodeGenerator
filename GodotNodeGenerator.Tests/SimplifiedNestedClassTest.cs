using GodotNodeGenerator.Tests.TestHelpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;

namespace GodotNodeGenerator.Tests
{
    public class SimplifiedNestedClassTest
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

            // Run the generator
            var outputs = RunSourceGenerator(sourceCode, [(scenePath, sceneContent)]);

            // Get the generated code
            var generatedFile = outputs.FirstOrDefault(f => f.HintName == "SimpleTest.g.cs");
            var generatedCode = generatedFile.SourceText.ToString();

            // Output the generated code to make debugging easier
            Console.WriteLine(generatedCode);

            // Basic assertions
            Assert.Contains("public class MainPanelWrapper", generatedCode);
            Assert.Contains("public Label Label", generatedCode);
        }

        private static List<(string HintName, SourceText SourceText)> RunSourceGenerator(
            string sourceCode,
            IEnumerable<(string Path, string Content)> additionalFiles)
        {
            // Create a collection of additional files
            var additionalTexts = additionalFiles.Select(
                file => new MockAdditionalText(file.Path, file.Content))
                .ToImmutableArray<AdditionalText>();

            // Create compilation for the source code
            var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
            var references = new List<MetadataReference>
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
            };

            var compilation = CSharpCompilation.Create(
                "TestCompilation",
                [syntaxTree],
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            var generator = new NodeGenerator();
            ImmutableArray<ISourceGenerator> generators = [generator.AsSourceGenerator()];
            var driver = CSharpGeneratorDriver.Create(
                generators: generators,
                additionalTexts: additionalTexts);
            driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation);

            // Get the results
            var runResult = driver.GetRunResult();
            // Get all generated sources
            return [.. runResult.GeneratedTrees
                .Select(t => {
                    var sourceText = SourceText.From(t.GetText().ToString());
                    return (Path.GetFileName(t.FilePath), sourceText);
                })];
        }
    }
}
