using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using GodotNodeGenerator.Tests.TestHelpers;

namespace GodotNodeGenerator.Tests
{
    public class NestedClassAccessorTests
    {
        [Fact]
        public void Generated_Code_Creates_Wrapper_Classes_For_Nested_Access()
        {
            // Arrange: Create a source file with the NodeGenerator attribute
            var sourceCode = @"
using Godot;
using GodotNodeGenerator;

namespace TestNamespace
{
    [NodeGenerator(""NestedAccessScene.tscn"")]
    public partial class TestClass : Node
    {
    }
}";

            // Create a test scene with nested structure
            var scenePath = "NestedAccessScene.tscn";
            var sceneContent = @"
[gd_scene format=3]

[node name=""PanelContainer"" type=""PanelContainer""]

[node name=""Button"" type=""Button"" parent=""PanelContainer""]

[node name=""Label"" type=""Label"" parent=""PanelContainer""]

[node name=""Container"" type=""Control"" parent=""PanelContainer""]

[node name=""ChildButton"" type=""Button"" parent=""PanelContainer/Container""]
";

            // Run the generator
            var outputs = RunSourceGenerator(sourceCode, [(scenePath, sceneContent)]);

            // Get the generated file
            var generatedFile = outputs.FirstOrDefault(f => f.HintName == "TestClass.g.cs");
            var generatedCode = generatedFile.SourceText.ToString();

            // Verify basic structure
            Assert.Contains("namespace TestNamespace", generatedCode);
            Assert.Contains("public partial class TestClass", generatedCode);

            // Verify the wrapper classes are generated
            Assert.Contains("public class PanelContainerWrapper", generatedCode);
            Assert.Contains("public class ContainerWrapper", generatedCode);

            // Verify the wrapper properties
            Assert.Contains("public PanelContainerWrapper PanelContainer", generatedCode);
            Assert.Contains("public ContainerWrapper Container", generatedCode);

            // Verify direct child node access in the wrapper class
            Assert.Contains("public Button Button => _owner.Button;", generatedCode);
            Assert.Contains("public Label Label => _owner.Label;", generatedCode);

            // Verify nested child access
            Assert.Contains("public Button ChildButton => _owner.ChildButton;", generatedCode);
        }

        [Fact]
        public void Generated_Code_Sets_Up_Nested_Hierarchy_Correctly()
        {
            // Arrange: Create a source file with the NodeGenerator attribute
            var sourceCode = @"
using Godot;
using GodotNodeGenerator;

namespace TestNamespace
{
    [NodeGenerator(""HierarchyScene.tscn"")]
    public partial class HierarchyTest : Node
    {
    }
}";

            // Create a test scene with deep hierarchy
            var scenePath = "HierarchyScene.tscn";
            var sceneContent = @"
[gd_scene format=3]

[node name=""Level1"" type=""Node2D""]

[node name=""Level2A"" type=""Control"" parent=""Level1""]

[node name=""Level3A"" type=""Button"" parent=""Level1/Level2A""]

[node name=""Level2B"" type=""Panel"" parent=""Level1""]

[node name=""Level3B"" type=""Label"" parent=""Level1/Level2B""]

[node name=""Level4"" type=""TextureRect"" parent=""Level1/Level2B/Level3B""]
";

            // Run the generator
            var outputs = RunSourceGenerator(sourceCode, [(scenePath, sceneContent)]);
            var generatedFile = outputs.FirstOrDefault(f => f.HintName == "HierarchyTest.g.cs");
            var generatedCode = generatedFile.SourceText.ToString();

            // Verify wrapper class hierarchy
            Assert.Contains("public class Level1Wrapper", generatedCode);
            Assert.Contains("public class Level2AWrapper", generatedCode);
            Assert.Contains("public class Level2BWrapper", generatedCode);
            Assert.Contains("public class Level3BWrapper", generatedCode);

            // Verify property access chains in wrapper classes
            Assert.Contains("public Level2AWrapper Level2A", generatedCode);
            Assert.Contains("public Level2BWrapper Level2B", generatedCode);
            Assert.Contains("public Level3BWrapper Level3B", generatedCode);

            // Verify leaf node direct access
            Assert.Contains("public Button Level3A", generatedCode);
            Assert.Contains("public TextureRect Level4", generatedCode);

            // Verify top-level wrapper property
            Assert.Contains("public Level1Wrapper Level1", generatedCode);
        }

        #region Test Helpers
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
        #endregion
    }
}
