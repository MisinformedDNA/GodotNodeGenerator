using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;

namespace GodotNodeGenerator.Tests.TestHelpers
{
    /// <summary>
    /// Base class for NodeGenerator test fixtures that provides common test infrastructure
    /// </summary>
    public abstract class NodeGeneratorTestBase
    {
        /// <summary>
        /// Runs the source generator with the provided source code and additional files
        /// </summary>
        /// <param name="sourceCode">The C# source code to process</param>
        /// <param name="additionalFiles">Collection of additional file paths and their contents</param>
        /// <returns>List of generated source files</returns>
        protected static List<(string HintName, SourceText SourceText)> RunSourceGenerator(
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

        /// <summary>
        /// Creates a standard source code template with NodeGenerator attribute
        /// </summary>
        /// <param name="className">The name of the class to generate</param>
        /// <param name="namespaceName">The namespace for the class</param>
        /// <param name="scenePath">The path to the scene file</param>
        /// <param name="baseType">The base type for the class (defaults to Node)</param>
        /// <returns>C# source code as a string</returns>
        protected static string CreateSourceTemplate(
            string className, 
            string namespaceName = "TestNamespace",
            string scenePath = "TestScene.tscn",
            string baseType = "Node")
        {
            return $@"
using Godot;
using GodotNodeGenerator;

namespace {namespaceName}
{{
    [NodeGenerator(""{scenePath}"")]
    public partial class {className} : {baseType}
    {{
    }}
}}";
        }

        /// <summary>
        /// Runs the source generator and returns the generated code for snapshot verification
        /// </summary>
        /// <param name="sourceCode">The source code to process</param>
        /// <param name="additionalFiles">Additional files required for code generation</param>
        /// <param name="expectedOutputFile">The expected name of the output file</param>
        /// <returns>The generated source code as a string</returns>
        protected static string RunGeneratorForSnapshot(
            string sourceCode, 
            IEnumerable<(string Path, string Content)> additionalFiles,
            string expectedOutputFile)
        {
            var outputs = RunSourceGenerator(sourceCode, additionalFiles);
            var generatedFile = outputs.FirstOrDefault(f => f.HintName.Contains(expectedOutputFile));
            
            // If the file isn't found, throw a descriptive exception
            if (generatedFile.SourceText == null)
            {
                var availableFiles = string.Join(", ", outputs.Select(o => o.HintName));
                throw new InvalidOperationException(
                    $"Generated file '{expectedOutputFile}' not found. Available files: {availableFiles}");
            }
            
            return generatedFile.SourceText.ToString();
        }        /// <summary>
        /// Verify a generated source file using Verify snapshot testing
        /// </summary>
        /// <param name="content">The source file content to verify</param>
        /// <returns>A task representing the verification process</returns>
        protected static Task VerifyGeneratedCode(string content)
        {
            // Simplest form with extension parameter
            return Verifier.Verify(content, "cs");
        }
    }
}
