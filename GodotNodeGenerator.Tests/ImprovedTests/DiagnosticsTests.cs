using FluentAssertions;
using GodotNodeGenerator.Tests.TestHelpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using System.Collections.Immutable;

namespace GodotNodeGenerator.Tests.ImprovedTests
{
    /// <summary>
    /// This test class specifically focuses on testing diagnostic messages 
    /// that your source generator produces
    /// </summary>
    public class DiagnosticsTests : NodeGeneratorTestBase
    {
        [Fact]
        public async Task MissingSceneFile_ShouldReportDiagnostic()
        {
            // Arrange: Create a test with source generator
            var test = new CSharpSourceGeneratorTest<NodeGenerator, XUnitVerifier>
            {
                TestCode = @"
using Godot;
using GodotNodeGenerator;

namespace DiagnosticsExample
{
    [NodeGenerator(""NonExistentScene.tscn"")]
    public partial class MissingSceneTest : Node
    {
    }
}"
            };
            
            // Configure what diagnostic to expect - ID, title, message pattern, severity
            test.ExpectedDiagnostics.Add(new DiagnosticResult(
                "GNGEN001", // Your diagnostic ID 
                DiagnosticSeverity.Warning)
                .WithLocation(1, 1)  // Line/column doesn't matter for our test
                .WithMessage("*Scene file not found*")); // Use wildcards for partial message matching
                
            // Act & Assert
            await test.RunAsync();
        }
        
        [Fact]
        public void ErrorDiagnosticsAreReported_WithFluentAssertions()
        {
            // Arrange: Create a source file referencing a non-existent scene
            var sourceCode = @"
using Godot;
using GodotNodeGenerator;

namespace TestNamespace
{
    [NodeGenerator(""NonExistent.tscn"")]
    public partial class ErrorTest : Node
    {
    }
}";

            // Act: Run the generator with no additional files
            var diagnostics = RunGeneratorAndCollectDiagnostics(sourceCode, []);

            // Assert: Verify diagnostics with FluentAssertions
            diagnostics.Should().NotBeEmpty("because a diagnostic should be reported for missing scene file");
              diagnostics.Should().Contain(d => 
                d.Id == "GNGEN001" && 
                d.Severity == DiagnosticSeverity.Warning &&
                d.GetMessage(null).Contains("Scene file not found"), // Pass null for culture
                "because the source generator should report a warning for missing scene files");
        }
        
        [Fact]
        public void InvalidSceneFormat_ShouldReportDiagnostic()
        {
            // Arrange: Create source and scene with invalid format
            var sourceCode = @"
using Godot;
using GodotNodeGenerator;

namespace TestNamespace
{
    [NodeGenerator(""InvalidScene.tscn"")]
    public partial class InvalidFormatTest : Node
    {
    }
}";

            var scenePath = "InvalidScene.tscn";
            var invalidSceneContent = @"
This is not a valid scene file format.
Just some random text that will cause parsing errors.
";

            // Act: Run the generator
            var diagnostics = RunGeneratorAndCollectDiagnostics(
                sourceCode, [(scenePath, invalidSceneContent)]); 

            // Assert: Verify error diagnostics
            diagnostics.Should().Contain(d => d.Severity == DiagnosticSeverity.Warning,
                "because invalid scene format should produce a warning diagnostic");
        }
        
        private static IEnumerable<Diagnostic> RunGeneratorAndCollectDiagnostics(
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
                  // Create a list to collect diagnostics
            var diagnostics = new List<Diagnostic>();
            
            // Configure the generator with our diagnostic collector
            var generator = new NodeGenerator();
            
            // Create driver options without optional parameters
            var driverOptions = new GeneratorDriverOptions(
                IncrementalGeneratorOutputKind.None, // disabledOutputs
                true);                              // trackIncrementalGeneratorSteps
                
            var driver = CSharpGeneratorDriver.Create(
                generators: ImmutableArray.Create(generator.AsSourceGenerator()),
                additionalTexts: additionalTexts,
                driverOptions: driverOptions);

            // Run the generator            
            var finalDriver = driver.RunGenerators(compilation);
            
            // Get the result which includes diagnostics
            var runResult = finalDriver.GetRunResult();
            
            // Return all diagnostics
            return runResult.Diagnostics;
        }
    }
}
