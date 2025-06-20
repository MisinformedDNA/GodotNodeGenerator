using GodotNodeGenerator.Tests.TestHelpers;

namespace GodotNodeGenerator.Tests
{
    /// <summary>
    /// Tests different node types and configurations using parameterized tests
    /// </summary>
    public class NodeTypeTests : NodeGeneratorTestBase
    {
        public static IEnumerable<object[]> NodeTypeTestData =>
            new List<object[]>
            {
                // Format: [Node Type, Property Type Check]
                new object[] { "Node2D", "Node2D" },
                new object[] { "Sprite2D", "Sprite2D" },
                new object[] { "Camera2D", "Camera2D" },
                new object[] { "Label", "Label" },
                new object[] { "Button", "Button" },
                new object[] { "Area2D", "Area2D" }
            };

        [Theory]
        [MemberData(nameof(NodeTypeTestData))]
        public void Generated_Code_For_Different_Node_Types(string nodeType, string expectedPropertyType)
        {
            // Arrange: Create source with NodeGenerator attribute
            var sourceCode = CreateSourceTemplate("NodeTypeTest", scenePath: "NodeTypeScene.tscn");

            // Create a scene with the specified node type
            var scenePath = "NodeTypeScene.tscn";
            var sceneContent = $@"
[gd_scene format=3]

[node name=""TestNode"" type=""{nodeType}""]
";

            // Act: Run the generator
            var outputs = RunSourceGenerator(sourceCode, [(scenePath, sceneContent)]);
            var generatedFile = outputs.FirstOrDefault(f => f.HintName == "NodeTypeTest.g.cs");
            var generatedCode = generatedFile.SourceText.ToString();

            // Assert: Verify correct type handling
            Assert.Contains($"private {expectedPropertyType}? _TestNode;", generatedCode);
            Assert.Contains($"public {expectedPropertyType} TestNode", generatedCode);
            Assert.Contains($"public bool TryGetTestNode([NotNullWhen(true)] out {expectedPropertyType}? node)", generatedCode);
            Assert.Contains($"if (tempNode is {expectedPropertyType} typedNode)", generatedCode);
        }

        public static IEnumerable<object[]> NodeScriptTestData =>
            new List<object[]>
            {
                // Format: [Node Type, Script Path]
                new object[] { "Node2D", "res://scripts/CustomScript.cs" },
                new object[] { "CharacterBody2D", "res://scripts/Player.cs" },
                new object[] { "Control", "res://scripts/UI/Widget.cs" }
            };

        [Theory]
        [MemberData(nameof(NodeScriptTestData))]
        public void Generated_Code_For_Script_Attachments(string nodeType, string scriptPath)
        {
            // Arrange: Create source with NodeGenerator attribute
            var sourceCode = CreateSourceTemplate("ScriptAttachTest", scenePath: "ScriptScene.tscn");

            // Create a scene with a script attachment
            var scenePath = "ScriptScene.tscn";
            var sceneContent = $@"
[gd_scene format=3]

[ext_resource type=""Script"" path=""{scriptPath}"" id=""1_script""]

[node name=""TestNode"" type=""{nodeType}""]
script = ExtResource(""1_script"")
";

            // Act: Run the generator
            var outputs = RunSourceGenerator(sourceCode, [(scenePath, sceneContent)]);
            var generatedFile = outputs.FirstOrDefault(f => f.HintName == "ScriptAttachTest.g.cs");
            var generatedCode = generatedFile.SourceText.ToString();

            // Assert: Verify script path is included in documentation
            Assert.Contains($"/// Gets the TestNode node (path: \"TestNode\") (script: \"{scriptPath}\")", generatedCode);
        }
    }
}
