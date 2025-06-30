using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using FluentAssertions;
using System.Collections.Immutable;
using GodotNodeGenerator.Tests.TestHelpers;

namespace GodotNodeGenerator.Tests
{
    public class IntegrationScenarioTests : NodeGeneratorTestBase
    {
        [Fact]
        public void IntegrationTest_ShouldHandlePlayerNodeCorrectly()
        {
            // This test mirrors the actual integration test scenario
            var sourceCode = CreateSourceTemplate(
                className: "Main", 
                namespaceName: "TestNamespace",
                scenePath: "Main.tscn");

            var sceneContent = @"
[gd_scene load_steps=2 format=3]

[node name=""Main"" type=""Node2D""]

[node name=""Player"" type=""CharacterBody2D"" parent="".""]
";

            var additionalFiles = new[] { 
                ("Main.tscn", sceneContent)
            };

            // Act
            var generatedCode = RunGeneratorForSnapshot(sourceCode, additionalFiles, "Main.g.cs");

            // Assert
            // In the integration test scenario, there should be a Player property
            // This is the property that's being accessed with Main.Player
            generatedCode.Should().Contain("private CharacterBody2D? _Player;");
            generatedCode.Should().Contain("public CharacterBody2D Player");
            generatedCode.Should().Contain("public bool TryGetPlayer(");
            
            // Also make sure conflicting case scenarios are handled correctly
            var sourceCode2 = CreateSourceTemplate(
                className: "MainScriptPath", 
                namespaceName: "TestNamespace",
                scenePath: "Main.tscn");

            var generatedCode2 = RunGeneratorForSnapshot(sourceCode2, additionalFiles, "MainScriptPath.g.cs");
            generatedCode2.Should().Contain("public CharacterBody2D Player");
        }
    }
}
