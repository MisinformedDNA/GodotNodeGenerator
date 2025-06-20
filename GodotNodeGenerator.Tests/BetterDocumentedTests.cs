using GodotNodeGenerator.Tests.TestHelpers;

namespace GodotNodeGenerator.Tests
{
    /// <summary>
    /// This test class demonstrates better organization and documentation practices for tests
    /// </summary>
    [Collection("NodeGenerator Tests")]
    public class BetterDocumentedTests : NodeGeneratorTestBase
    {
        /// <summary>
        /// Ensures that the source generator correctly produces code with explicitly-documented nullability behavior 
        /// </summary>
        [Fact]
        public void Generated_Code_Has_Explicit_Nullability_Documentation()
        {
            // Arrange: Create a source file that we'll use to generate node accessors
            var sourceCode = CreateSourceTemplate("NullabilityTest", scenePath: "NullabilityScene.tscn");

            // Create a simple test scene to parse
            var scenePath = "NullabilityScene.tscn";
            var sceneContent = @"
[gd_scene format=3]

[node name=""Root"" type=""Node2D""]

[node name=""MaybeNull"" type=""Sprite2D"" parent=""Root""]
";

            // Act: Run the generator to produce the output
            var outputs = RunSourceGenerator(sourceCode, [(scenePath, sceneContent)]);
            var generatedFile = outputs.FirstOrDefault(f => f.HintName == "NullabilityTest.g.cs");
            var generatedCode = generatedFile.SourceText.ToString();

            // Assert: Verify nullability documentation is included
            // Should document null behavior in exceptions
            Assert.Contains("/// <exception cref=\"NullReferenceException\">Thrown when the node is not found in the scene tree.</exception>", generatedCode);
            
            // Should document the TryGet pattern null behavior
            Assert.Contains("/// without throwing exceptions if the node doesn't exist or is of wrong type", generatedCode);
            
            // Should have NotNullWhen attribute on out parameter
            Assert.Contains("[NotNullWhen(true)] out Sprite2D? node", generatedCode);
            
            // Should have NotNull attribute on return value
            Assert.Contains("[return: NotNull]", generatedCode);
        }

        /// <summary>
        /// Verifies that property return types match the node types declared in the scene
        /// </summary>
        [Fact]
        public void Generated_Properties_Match_Node_Types()
        {
            // Arrange: Set up test with specific node types
            var sourceCode = CreateSourceTemplate("TypeMatchTest", scenePath: "TypeMatch.tscn");

            // Create scene with various node types
            var scenePath = "TypeMatch.tscn";
            var sceneContent = @"
[gd_scene format=3]

[node name=""NodeTest"" type=""Node""]
[node name=""Node2DTest"" type=""Node2D""]
[node name=""SpriteTest"" type=""Sprite2D""]
[node name=""LabelTest"" type=""Label""]
[node name=""ButtonTest"" type=""Button""]
";

            // Act: Generate code from the scene
            var outputs = RunSourceGenerator(sourceCode, [(scenePath, sceneContent)]);
            var generatedFile = outputs.FirstOrDefault(f => f.HintName == "TypeMatchTest.g.cs");
            var generatedCode = generatedFile.SourceText.ToString();

            // Assert: Verify type matching
            // Check field declarations
            Assert.Contains("private Node? _NodeTest;", generatedCode);
            Assert.Contains("private Node2D? _Node2DTest;", generatedCode);
            Assert.Contains("private Sprite2D? _SpriteTest;", generatedCode);
            Assert.Contains("private Label? _LabelTest;", generatedCode);
            Assert.Contains("private Button? _ButtonTest;", generatedCode);
            
            // Check property declarations
            Assert.Contains("public Node NodeTest", generatedCode);
            Assert.Contains("public Node2D Node2DTest", generatedCode);
            Assert.Contains("public Sprite2D SpriteTest", generatedCode);
            Assert.Contains("public Label LabelTest", generatedCode);
            Assert.Contains("public Button ButtonTest", generatedCode);
            
            // Check for correct type in casting
            Assert.Contains("node as Node", generatedCode);
            Assert.Contains("node as Node2D", generatedCode);
            Assert.Contains("node as Sprite2D", generatedCode);
            Assert.Contains("node as Label", generatedCode);
            Assert.Contains("node as Button", generatedCode);
        }
    }
}
