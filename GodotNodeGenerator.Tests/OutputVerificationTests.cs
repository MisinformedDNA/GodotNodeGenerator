using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;

namespace GodotNodeGenerator.Tests
{
    public class OutputVerificationTests
    {
        [Fact]
        public void Generated_Code_Has_Expected_Structure()
        {
            // Arrange: Create a source file with the NodeGenerator attribute
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

            // Create a simple test scene
            var scenePath = "SimpleScene.tscn";
            var sceneContent = @"
[gd_scene format=3]

[node name=""Root"" type=""Node2D""]

[node name=""Child"" type=""Sprite2D"" parent=""Root""]
";

            // Run the generator
            var outputs = RunSourceGenerator(sourceCode, [(scenePath, sceneContent)]);

            // Get the generated file
            var generatedFile = outputs.FirstOrDefault(f => f.HintName == "TestClass.g.cs");
            var generatedCode = generatedFile.SourceText.ToString();

            // Verify basic structure
            Assert.Contains("namespace TestNamespace", generatedCode);
            Assert.Contains("public partial class TestClass", generatedCode);
            Assert.Contains("// Generated node accessors for TestClass", generatedCode);

            // Verify properties and methods
            Assert.Contains("private Node2D? _Root;", generatedCode);
            Assert.Contains("public Node2D Root", generatedCode);
            Assert.Contains("public bool TryGetRoot(", generatedCode);

            Assert.Contains("private Sprite2D? _Child;", generatedCode);
            Assert.Contains("public Sprite2D Child", generatedCode);
            Assert.Contains("public bool TryGetChild(", generatedCode);
        }

        [Fact]
        public void Generated_Code_Has_Correct_Comments_And_Docs()
        {
            // Arrange: Source with generator attribute
            var sourceCode = @"
using Godot;
using GodotNodeGenerator;

namespace TestNamespace
{
    [NodeGenerator(""DocScene.tscn"")]
    public partial class DocTest : Node
    {
    }
}";

            // Create a scene with scripts and properties
            var scenePath = "DocScene.tscn";
            var sceneContent = @"
[gd_scene format=3]

[ext_resource type=""Script"" path=""res://scripts/PlayerScript.cs"" id=""1_script""]

[node name=""Player"" type=""CharacterBody2D""]
script = ExtResource(""1_script"")
speed = 300.0
";

            // Run the generator
            var outputs = RunSourceGenerator(sourceCode, [(scenePath, sceneContent)]);
            var generatedFile = outputs.FirstOrDefault(f => f.HintName == "DocTest.g.cs");
            var generatedCode = generatedFile.SourceText.ToString();

            // Verify the docs and comments
            Assert.Contains("/// <summary>", generatedCode);
            Assert.Contains("/// Gets the Player node (path: \"Player\") (script: \"", generatedCode);
            Assert.Contains("/// </summary>", generatedCode);
            Assert.Contains("/// <exception cref=\"InvalidCastException\">", generatedCode);
            Assert.Contains("/// <exception cref=\"NullReferenceException\">", generatedCode);

            // Check TryGet docs too
            Assert.Contains("/// Tries to get the Player node", generatedCode);
            Assert.Contains("/// without throwing exceptions", generatedCode);
        }

        [Fact]
        public void Generated_Code_Has_Correct_Type_Safety_Features()
        {
            // Arrange: Source with generator attribute
            var sourceCode = @"
using Godot;
using GodotNodeGenerator;

namespace TestNamespace
{
    [NodeGenerator(""TypeSafetyScene.tscn"")]
    public partial class TypeSafetyTest : Node
    {
    }
}";

            // Create a scene for testing type safety
            var scenePath = "TypeSafetyScene.tscn";
            var sceneContent = @"
[gd_scene format=3]

[node name=""TestNode"" type=""Node2D""]
";

            // Run the generator
            var outputs = RunSourceGenerator(sourceCode, [(scenePath, sceneContent)]);
            var generatedFile = outputs.FirstOrDefault(f => f.HintName == "TypeSafetyTest.g.cs");
            var generatedCode = generatedFile.SourceText.ToString();

            // Verify type safety features
            // Check for nullable types
            Assert.Contains("private Node2D? _TestNode;", generatedCode);

            // Check for NotNull attribute
            Assert.Contains("[return: NotNull]", generatedCode);

            // Check for null checks
            Assert.Contains("if (_TestNode == null)", generatedCode);
            Assert.Contains("if (node == null)", generatedCode);

            // Check for exception throwing
            Assert.Contains("throw new NullReferenceException(", generatedCode);
            Assert.Contains("throw new InvalidCastException(", generatedCode);

            // Check for NotNullWhen attribute
            Assert.Contains("[NotNullWhen(true)]", generatedCode);

            // Check for TryGet pattern
            Assert.Contains("public bool TryGetTestNode([NotNullWhen(true)] out Node2D? node)", generatedCode);
        }

        [Fact]
        public void Generated_Code_Handles_Nested_Node_Paths_Correctly()
        {
            // Arrange: Source with generator attribute
            var sourceCode = @"
using Godot;
using GodotNodeGenerator;

namespace TestNamespace
{
    [NodeGenerator(""NestedScene.tscn"")]
    public partial class NestedTest : Node
    {
    }
}";

            // Create a scene with deeply nested nodes
            var scenePath = "NestedScene.tscn";
            var sceneContent = @"
[gd_scene format=3]

[node name=""Root"" type=""Node2D""]

[node name=""Level1"" type=""Node2D"" parent=""Root""]

[node name=""Level2"" type=""Node2D"" parent=""Root/Level1""]

[node name=""Level3"" type=""Sprite2D"" parent=""Root/Level1/Level2""]

[node name=""Sibling"" type=""Label"" parent=""Root""]
";

            // Run the generator
            var outputs = RunSourceGenerator(sourceCode, [(scenePath, sceneContent)]);
            var generatedFile = outputs.FirstOrDefault(f => f.HintName == "NestedTest.g.cs");
            var generatedCode = generatedFile.SourceText.ToString();

            // Verify node paths are correct
            Assert.Contains("\"Root\"", generatedCode);
            Assert.Contains("\"Root/Level1\"", generatedCode);
            Assert.Contains("\"Root/Level1/Level2\"", generatedCode);
            Assert.Contains("\"Root/Level1/Level2/Level3\"", generatedCode);
            Assert.Contains("\"Root/Sibling\"", generatedCode);
        }

        [Fact]
        public void Generated_Code_Exactly_Matches_Expected_Output_Template()
        {
            // Arrange: Create very simple input for exact output comparison
            var sourceCode = @"
using Godot;
using GodotNodeGenerator;

namespace ExactTest
{
    [NodeGenerator(""TestScene.tscn"")]
    public partial class TestComponent : Node
    {
    }
}";

            // Create a simple scene with just one node
            var scenePath = "TestScene.tscn";
            var sceneContent = @"
[gd_scene format=3]

[node name=""TestNode"" type=""Sprite2D""]
";

            // Run the generator
            var outputs = RunSourceGenerator(sourceCode, [(scenePath, sceneContent)]);
            var generatedFile = outputs.FirstOrDefault(f => f.HintName == "TestComponent.g.cs");
            var generatedCode = generatedFile.SourceText.ToString();

            // Define our exact expected output
            var expectedOutput = @"// <auto-generated/>
using Godot;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;

namespace ExactTest
{
    // Generated node accessors for TestComponent
    public partial class TestComponent
    {

        private Sprite2D? _TestNode;
        
        /// <summary>
        /// Gets the TestNode node (path: ""TestNode"")
        /// </summary>
        /// <exception cref=""InvalidCastException"">Thrown when the node at the specified path is not of type Sprite2D.</exception>
        /// <exception cref=""NullReferenceException"">Thrown when the node is not found in the scene tree.</exception>
        [return: NotNull]
        public Sprite2D TestNode 
        {
            get
            {
                if (_TestNode == null)
                {
                    var node = GetNodeOrNull(""TestNode"");
                    if (node == null)
                    {
                        throw new NullReferenceException($""Node not found: TestNode"");
                    }
                    
                    _TestNode = node as Sprite2D;
                    if (_TestNode == null)
                    {
                        throw new InvalidCastException($""Node at path {node.GetPath()} is of type {node.GetType()}, not Sprite2D"");
                    }
                }
                
                return _TestNode;
            }
        }

        /// <summary>
        /// Tries to get the TestNode node (path: ""TestNode"") 
        /// without throwing exceptions if the node doesn't exist or is of wrong type.
        /// </summary>
        /// <returns>True if the node was found and is of the correct type, otherwise false.</returns>
        public bool TryGetTestNode([NotNullWhen(true)] out Sprite2D? node)
        {
            node = null;
            if (_TestNode != null)
            {
                node = _TestNode;
                return true;
            }
            
            var tempNode = GetNodeOrNull(""TestNode"");
            if (tempNode is Sprite2D typedNode)
            {
                _TestNode = typedNode;
                node = typedNode;
                return true;
            }
            
            return false;
        }

        #region Node Tree Accessors

        #endregion
    }
}
";

            Assert.Equal(expectedOutput, generatedCode);
        }

        [Fact]
        public void Generated_Code_Handles_Special_Characters_In_Node_Names()
        {
            // Arrange: Source with generator attribute
            var sourceCode = @"
using Godot;
using GodotNodeGenerator;

namespace TestNamespace
{
    [NodeGenerator(""SpecialCharsScene.tscn"")]
    public partial class SpecialCharsTest : Node
    {
    }
}";

            // Create a scene with nodes having special characters in names
            var scenePath = "SpecialCharsScene.tscn";
            var sceneContent = @"
[gd_scene format=3]

[node name=""Node-With-Dashes"" type=""Node2D""]

[node name=""Node With Spaces"" type=""Sprite2D"" parent=""Node-With-Dashes""]

[node name=""123NumberFirst"" type=""Label"" parent=""Node-With-Dashes""]
";

            // Run the generator
            var outputs = RunSourceGenerator(sourceCode, [(scenePath, sceneContent)]);
            var generatedFile = outputs.FirstOrDefault(f => f.HintName == "SpecialCharsTest.g.cs");
            var generatedCode = generatedFile.SourceText.ToString();

            // Verify special character handling in identifier names
            Assert.Contains("private Node2D? _Node_With_Dashes;", generatedCode);
            Assert.Contains("public Node2D Node_With_Dashes", generatedCode);
            Assert.Contains("TryGetNode_With_Dashes", generatedCode);

            Assert.Contains("private Sprite2D? _Node_With_Spaces;", generatedCode);
            Assert.Contains("public Sprite2D Node_With_Spaces", generatedCode);
            Assert.Contains("TryGetNode_With_Spaces", generatedCode);

            Assert.Contains("private Label? __123NumberFirst;", generatedCode);
            Assert.Contains("public Label _123NumberFirst", generatedCode);
            Assert.Contains("TryGet_123NumberFirst", generatedCode);

            // But the paths should remain unchanged
            Assert.Contains("\"Node-With-Dashes\"", generatedCode);
            Assert.Contains("\"Node-With-Dashes/Node With Spaces\"", generatedCode);
            Assert.Contains("\"Node-With-Dashes/123NumberFirst\"", generatedCode);
        }

        [Fact]
        public void GeneratedCode_WithCustomScriptAssociation_HandlesScriptCorrectly()
        {
            // Arrange: Source with generator attribute
            var sourceCode = @"
using Godot;
using GodotNodeGenerator;

namespace TestNamespace
{
    [NodeGenerator(""ScriptScene.tscn"")]
    public partial class ScriptTest : Node
    {
    }
}";

            // Create a scene with script associations
            var scenePath = "ScriptScene.tscn";
            var sceneContent = @"
[gd_scene format=3]

[ext_resource type=""Script"" path=""res://scripts/CustomNode.cs"" id=""1_cscript""]
[ext_resource type=""Script"" path=""res://scripts/PlayerController.cs"" id=""2_pscript""]

[node name=""CustomNode"" type=""Node2D""]
script = ExtResource(""1_cscript"")

[node name=""Player"" type=""CharacterBody2D"" parent=""CustomNode""]
script = ExtResource(""2_pscript"")
";

            // Run the generator
            var outputs = RunSourceGenerator(sourceCode, [(scenePath, sceneContent)]);
            var generatedFile = outputs.FirstOrDefault(f => f.HintName == "ScriptTest.g.cs");
            var generatedCode = generatedFile.SourceText.ToString();

            // Verify script information is included in the documentation
            Assert.Contains("/// Gets the CustomNode node (path: \"CustomNode\") (script: \"", generatedCode);
            Assert.Contains("/// Gets the Player node (path: \"CustomNode/Player\") (script: \"", generatedCode);
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
