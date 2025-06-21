using FluentAssertions;
using GodotNodeGenerator.Tests.TestHelpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using System.Collections.Immutable;

namespace GodotNodeGenerator.Tests.ImprovedTests
{    
    /// <summary>
    /// Tests focusing on diagnostic messages produced by the source generator.
    /// 
    /// These tests verify:
    /// 1. The correct diagnostic ID is reported (GNGEN001 for missing scene files)
    /// 2. The correct diagnostic severity is used (Warning)
    /// 3. The diagnostic message contains the expected information
    /// 
    /// Diagnostics defined in the system:
    /// - GNGEN001: Reported when a scene file is not found
    /// - GNGEN002: Defined but rarely reported - only triggered when an exception occurs in the parser
    ///   (Most invalid scene formats don't cause exceptions, they just result in no nodes)
    /// </summary>
    public class DiagnosticsTests : NodeGeneratorTestBase
    {
        #region Missing Scene File Diagnostics        

        /// <summary>
        /// Tests that a diagnostic is reported when using FluentAssertions
        /// </summary>
        [Fact]
        public void MissingSceneFile_ReportsDiagnostic()
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
            
            // From SceneParser.ReadSceneContent: "Could not find scene file: {0}"
            diagnostics.Should().Contain(d =>
              d.Id == "GNGEN001" &&
              d.Severity == DiagnosticSeverity.Warning &&
              d.GetMessage(null).Contains("Could not find scene file"),
              "because the source generator should report a warning for missing scene files");
        }

        #endregion

        #region Invalid Scene Format Diagnostics        
        
        /// <summary>
        /// Tests that no GNGEN002 diagnostic is reported for a simple invalid scene format.
        /// The SceneParser code handles invalid formats by returning an empty list of nodes        
        /// without throwing exceptions, so no diagnostic is actually generated.
        /// </summary>
        [Fact]
        public void SimpleInvalidSceneFormat_DoesNotReportDiagnostic()
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

            diagnostics.Should().NotContain(d => 
                d.Id == "GNGEN002",
                "because the SceneParser handles invalid formats without throwing exceptions");
        }        
        
        /// <summary>
        /// Tests that GNGEN002 diagnostic is reported when scene parsing throws an exception.
        /// To simulate this, we need to actually cause an exception in the SceneParser.
        /// </summary>
        [Fact(Skip = @"GNGEN002 diagnostic is only reported when an exception occurs during scene parsing.
                
                From the code review in SceneParser.cs, exceptions would only happen for:
                1. Malformed XML scene files that actually throw when parsing
                2. Other low-level file access/parsing exceptions
                
                To properly test this, we would need to:
                1. Either create a custom mock AdditionalText that throws when read
                2. Or modify SceneParser to add testability hooks
                
                Currently most invalid scene formats just result in no nodes found,
                not in an exception, so the diagnostic isn't triggered.")]
        public void SceneParsingException_ReportsDiagnostic()
        {
            // This test requires mocking the internal exception handling of SceneParser
            // Since we can't easily force an exception in the actual parsing code,
            // we'll create a test file that demonstrates how GNGEN002 would be reported

            // If we were to implement this test fully, we would:
            // 1. Create a mock AdditionalText that throws when GetText() is called
            // 2. Run the generator with this mock
            // 3. Check for GNGEN002 diagnostic in the results
        }

        #endregion        
        
        /// <summary>
        /// Runs the NodeGenerator and returns all diagnostics reported during the generation process.
        /// </summary>
        /// <param name="sourceCode">The C# source code to process</param>
        /// <param name="additionalFiles">Additional files (like scene files) to include</param>
        /// <returns>Collection of diagnostics reported during generation</returns>
        private static ImmutableArray<Diagnostic> RunGeneratorAndCollectDiagnostics(
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

            // Configure the generator
            var generator = new NodeGenerator();

            // Create driver options
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
