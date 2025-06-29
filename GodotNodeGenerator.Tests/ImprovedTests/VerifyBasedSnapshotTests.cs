using FluentAssertions;
using GodotNodeGenerator.Tests.TestHelpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;

namespace GodotNodeGenerator.Tests.ImprovedTests
{    /// <summary>
     /// This test class demonstrates using Verify for snapshot testing and FluentAssertions for more readable assertions
     /// </summary>    
    [UsesVerify]
    public class VerifyBasedSnapshotTests : NodeGeneratorTestBase
    {
        [Fact]
        public Task Generated_Code_Should_Match_Verified_Snapshot()
        {
            // Arrange: Create a simple source file and scene content
            var sourceCode = @"
using Godot;
using GodotNodeGenerator;

namespace TestNamespace
{
    [NodeGenerator(""SimpleScene.tscn"")]
    public partial class TestClass : Node
    {
    }
}";

            var scenePath = "SimpleScene.tscn";
            var sceneContent = @"
[gd_scene format=3]

[node name=""Root"" type=""Node2D""]

[node name=""Child"" type=""Sprite2D"" parent=""Root""]
";

            // Act: Generate the code using the helper method
            var generatedCode = RunGeneratorForSnapshot(sourceCode, [(scenePath, sceneContent)], "TestClass.g.cs");

            // Assert: Verify the snapshot - this will automatically compare against approved snapshots
            var settings = new VerifySettings();
            settings.UseDirectory("Snapshots");

            return Verifier.Verify(generatedCode, "cs", settings);
        }

        [Fact]
        public void Generated_Code_Should_Have_Expected_Structure_Using_FluentAssertions()
        {
            // Arrange: Create source and scene
            var sourceCode = @"
using Godot;
using GodotNodeGenerator;

namespace TestNamespace
{
    [NodeGenerator(""FluentScene.tscn"")]
    public partial class FluentTest : Node
    {
    }
}";

            var scenePath = "FluentScene.tscn";
            var sceneContent = @"
[gd_scene format=3]

[node name=""Root"" type=""Node2D""]

[node name=""Child"" type=""Sprite2D"" parent=""Root""]

[node name=""GrandChild"" type=""Label"" parent=""Root/Child""]
";

            // Act: Generate the code
            var generatedCode = RunSourceGenerator(sourceCode, [(scenePath, sceneContent)]);
            var result = generatedCode.FirstOrDefault(f => f.HintName.Contains("FluentTest.g.cs")).SourceText.ToString();

            // Assert: Using FluentAssertions for more readable assertions
            result.Should().Contain("namespace TestNamespace")
                .And.Contain("public partial class FluentTest")
                .And.Contain("// Generated node accessors for FluentTest");

            // Check for property declarations with fluent assertions
            result.Should().Contain("private Node2D? _Root;")
                .And.Contain("public Node2D Root")
                .And.Contain("public bool TryGetRoot");

            // Fluent assertions for child node
            result.Should().Contain("private Sprite2D? _Child;")
                .And.Contain("public Sprite2D Child")
                .And.Contain("public bool TryGetChild");

            // Fluent assertions for grandchild node
            result.Should().Contain("private Label? _GrandChild;")
                .And.Contain("public Label GrandChild")
                .And.Contain("public bool TryGetGrandChild");

            // Verify node paths are included correctly
            result.Should().Contain("\"Root\"")
                .And.Contain("\"Root/Child\"")
                .And.Contain("\"Root/Child/GrandChild\"");
        }

        [Fact]
        public Task Should_Verify_Diagnostics_From_Source_Generator()
        {
            // Arrange: Source with a missing scene file
            var sourceCode = @"
using Godot;
using GodotNodeGenerator;

namespace TestNamespace
{
    [NodeGenerator(""MissingFile.tscn"")]
    public partial class DiagnosticsTest : Node
    {
    }
}";

            // Act: Run the generator without providing the scene file
            var (outputs, diagnostics) = RunSourceGeneratorWithDiagnostics(sourceCode, []);

            // Assert: Create a snapshot of both the diagnostics and the generated code
            var diagnosticsVerifySettings = new VerifySettings();
            diagnosticsVerifySettings.UseDirectory("Snapshots");

            // Verify the diagnostics - focus on what matters
            var diagnosticsToVerify = diagnostics.Select(d => new
            {
                Id = d.Id,
                Severity = d.Severity,
                Message = d.GetMessage(null),
                Location = d.Location.GetLineSpan().ToString()
            }).ToList();

            // Also verify the generated code for the missing scene case
            var outputFile = outputs.FirstOrDefault(f => f.HintName.Contains("DiagnosticsTest.g.cs"));
            var generatedCode = outputFile.SourceText != null
                              ? outputFile.SourceText.ToString()
                              : "No code was generated.";

            var codeVerifySettings = new VerifySettings();
            codeVerifySettings.UseDirectory("Snapshots");

            return Task.WhenAll(
                Verifier.Verify(diagnosticsToVerify, diagnosticsVerifySettings),
                Verifier.Verify(generatedCode, "cs", codeVerifySettings)
            );
        }

        /// <summary>
        /// Runs the source generator and returns both outputs and diagnostics
        /// </summary>
        private static (List<(string HintName, SourceText SourceText)> Outputs, IEnumerable<Diagnostic> Diagnostics)
            RunSourceGeneratorWithDiagnostics(string sourceCode, IEnumerable<(string Path, string Content)> additionalFiles)
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
            var driver = CSharpGeneratorDriver.Create(
                generators: ImmutableArray.Create(generator.AsSourceGenerator()),
                additionalTexts: additionalTexts);

            driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation);

            // Get the results
            var runResult = driver.GetRunResult();

            // Extract and return both the generated sources and diagnostics
            var outputs = runResult.GeneratedTrees
                .Select(t =>
                {
                    var sourceText = SourceText.From(t.GetText().ToString());
                    return (Path.GetFileName(t.FilePath), sourceText);
                })
                .ToList();

            return (outputs, runResult.Diagnostics);
        }
    }
}
