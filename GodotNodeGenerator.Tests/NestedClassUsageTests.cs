using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using GodotNodeGenerator.Tests.TestHelpers;

namespace GodotNodeGenerator.Tests
{
    /// <summary>
    /// Tests that verify the nested class accessors work as expected in code that mimics
    /// how they would be used in a real Godot game script.
    /// </summary>
    public class NestedClassUsageTests
    {
        [Fact]
        public void Generated_Code_Supports_Nested_Class_Access_Pattern()
        {
            // Arrange: Create a source file with the NodeGenerator attribute
            // This represents a UI controller with a nested hierarchy
            var sourceCode = @"
using Godot;
using GodotNodeGenerator;

namespace TestNamespace
{
    // UI controller that manages a form with various nested UI elements
    [NodeGenerator(""UIScene.tscn"")]
    public partial class UIController : Control
    {
        public override void _Ready()
        {
            // This is how the code would be used in a real Godot script
            // We'll verify the generated code supports this pattern
            MainPanel.HeaderLabel.Text = ""Settings"";
            MainPanel.Form.NameInput.PlaceholderText = ""Enter your name"";
            MainPanel.Form.EmailInput.PlaceholderText = ""Enter your email"";
            MainPanel.Form.SubmitButton.Text = ""Save Settings"";
            
            // Deep nested elements
            MainPanel.Form.AdvancedSection.ToggleButton.Text = ""Show Advanced"";
        }
    }
}";

            // Create a test UI scene with a deep hierarchy
            var scenePath = "UIScene.tscn";
            var sceneContent = @"
[gd_scene format=3]

[node name=""UIController"" type=""Control""]

[node name=""MainPanel"" type=""PanelContainer"" parent=""""]

[node name=""HeaderLabel"" type=""Label"" parent=""MainPanel""]

[node name=""Form"" type=""VBoxContainer"" parent=""MainPanel""]

[node name=""NameInput"" type=""LineEdit"" parent=""MainPanel/Form""]

[node name=""EmailInput"" type=""LineEdit"" parent=""MainPanel/Form""]

[node name=""SubmitButton"" type=""Button"" parent=""MainPanel/Form""]

[node name=""AdvancedSection"" type=""VBoxContainer"" parent=""MainPanel/Form""]

[node name=""ToggleButton"" type=""Button"" parent=""MainPanel/Form/AdvancedSection""]

[node name=""AdvancedOptions"" type=""GridContainer"" parent=""MainPanel/Form/AdvancedSection""]

[node name=""Option1"" type=""CheckBox"" parent=""MainPanel/Form/AdvancedSection/AdvancedOptions""]

[node name=""Option2"" type=""CheckBox"" parent=""MainPanel/Form/AdvancedSection/AdvancedOptions""]
";

            // Run the generator
            var outputs = RunSourceGenerator(sourceCode, [(scenePath, sceneContent)]);

            // Get the generated code
            var generatedFile = outputs.FirstOrDefault(f => f.HintName == "UIController.g.cs");
            var generatedCode = generatedFile.SourceText.ToString();            // Verify the nested class structure is generated correctly
            // Main wrapper class
            Assert.Contains("public class MainPanelWrapper", generatedCode);
            Assert.Contains("public PanelContainer Node =>", generatedCode);

            // First level nested wrapper for Form
            Assert.Contains("public class FormWrapper", generatedCode);
            Assert.Contains("FormWrapper Form", generatedCode);

            // Second level nested wrapper for AdvancedSection
            Assert.Contains("public class AdvancedSectionWrapper", generatedCode);
            Assert.Contains("AdvancedSectionWrapper AdvancedSection", generatedCode);

            // Third level nested wrapper for AdvancedOptions
            Assert.Contains("public class AdvancedOptionsWrapper", generatedCode);
            Assert.Contains("AdvancedOptionsWrapper AdvancedOptions", generatedCode);

            // Verify the accessor properties that create the path
            // Root level accessor
            Assert.Contains("public MainPanelWrapper MainPanel", generatedCode);

            // First level accessors
            Assert.Contains("public Label HeaderLabel", generatedCode); // Direct property
            Assert.Contains("public FormWrapper Form", generatedCode);  // Wrapper property

            // Second level accessors
            Assert.Contains("public LineEdit NameInput", generatedCode);
            Assert.Contains("public LineEdit EmailInput", generatedCode);
            Assert.Contains("public Button SubmitButton", generatedCode);
            Assert.Contains("public AdvancedSectionWrapper AdvancedSection", generatedCode);

            // Third level accessor
            Assert.Contains("public Button ToggleButton", generatedCode);
            Assert.Contains("public AdvancedOptionsWrapper AdvancedOptions", generatedCode);

            // Fourth level accessors
            Assert.Contains("public CheckBox Option1", generatedCode);
            Assert.Contains("public CheckBox Option2", generatedCode);
        }
        [Fact]
        public void Generated_Code_Supports_Character_Controller_With_Nested_Nodes()
        {
            // Arrange: Create a source file for a character controller with nested elements
            var sourceCode = @"
using Godot;
using GodotNodeGenerator;

namespace TestNamespace
{
    [NodeGenerator(""CharacterScene.tscn"")]
    public partial class CharacterController : CharacterBody2D
    {
        public override void _Ready()
        {
            // Example usage in a real Godot script
            Visual.AnimationPlayer.Play(""Idle"");
            Visual.Sprite.Texture = null; // Would set a real texture
            
            Collision.CollisionShape.Disabled = false;
            
            InteractionArea.CollisionShape.Scale = new Vector2(1.2f, 1.2f);
        }
    }
}";

            // Create a test character scene
            var scenePath = "CharacterScene.tscn";
            var sceneContent = @"
[gd_scene format=3]

[node name=""CharacterController"" type=""CharacterBody2D""]

[node name=""Visual"" type=""Node2D"" parent=""""]

[node name=""Sprite"" type=""Sprite2D"" parent=""Visual""]

[node name=""AnimationPlayer"" type=""AnimationPlayer"" parent=""Visual""]

[node name=""Collision"" type=""Node2D"" parent=""""]

[node name=""CollisionShape"" type=""CollisionShape2D"" parent=""Collision""]

[node name=""InteractionArea"" type=""Area2D"" parent=""""]

[node name=""CollisionShape"" type=""CollisionShape2D"" parent=""InteractionArea""]
";

            // Run the generator
            var outputs = RunSourceGenerator(sourceCode, [(scenePath, sceneContent)]);            // Get the generated file
            var generatedFile = outputs.FirstOrDefault(f => f.HintName == "CharacterController.g.cs");
            var generatedCode = generatedFile.SourceText.ToString();

            // Output the generated code to make debugging easier
            Console.WriteLine("GENERATED CODE:");
            Console.WriteLine(generatedCode);            // Verify the generated wrapper classes have the expected structure
            Assert.Contains("public class VisualWrapper", generatedCode);
            Assert.Contains("public AnimationPlayer AnimationPlayer", generatedCode);
            Assert.Contains("public Sprite2D Sprite", generatedCode);

            // For Collision node
            Assert.Contains("public Node2D Collision", generatedCode);

            // For InteractionArea
            Assert.Contains("public class InteractionAreaWrapper", generatedCode);
            Assert.Contains("public CollisionShape2D CollisionShape", generatedCode);            // Verify methods and properties in the main class to access the wrappers
            Assert.Contains("public VisualWrapper Visual", generatedCode);
            Assert.Contains("public Node2D Collision", generatedCode);
            Assert.Contains("public InteractionAreaWrapper InteractionArea", generatedCode);
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
