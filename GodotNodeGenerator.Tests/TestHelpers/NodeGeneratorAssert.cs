namespace GodotNodeGenerator.Tests.TestHelpers
{
    /// <summary>
    /// Provides utility methods for common test assertions in the NodeGenerator tests
    /// </summary>
    public static class NodeGeneratorAssert
    {
        /// <summary>
        /// Asserts that the generated code contains the expected property for the specified node
        /// </summary>
        /// <param name="generatedCode">The generated source code</param>
        /// <param name="nodeName">The name of the node</param>
        /// <param name="nodeType">The expected type of the node</param>
        public static void ContainsNodeProperty(string generatedCode, string nodeName, string nodeType)
        {
            // Check for proper field declaration
            Assert.Contains($"private {nodeType}? _{nodeName};", generatedCode);
            
            // Check for property
            Assert.Contains($"public {nodeType} {nodeName}", generatedCode);
            
            // Check for TryGet method
            Assert.Contains($"public bool TryGet{nodeName}([NotNullWhen(true)] out {nodeType}? node)", generatedCode);
        }

        /// <summary>
        /// Asserts that the generated code contains proper XML documentation for the node
        /// </summary>
        /// <param name="generatedCode">The generated source code</param>
        /// <param name="nodeName">The name of the node</param>
        /// <param name="nodePath">The expected path of the node</param>
        /// <param name="scriptPath">Optional script path associated with the node</param>
        public static void ContainsNodeDocumentation(string generatedCode, string nodeName, string nodePath, string? scriptPath = null)
        {
            // Basic summary documentation
            string expectedDoc = $"/// Gets the {nodeName} node (path: \"{nodePath}\")";
            if (!string.IsNullOrEmpty(scriptPath))
            {
                expectedDoc += $" (script: \"{scriptPath}\")";
            }
            
            Assert.Contains(expectedDoc, generatedCode);
            
            // Exception documentation
            Assert.Contains("/// <exception cref=\"InvalidCastException\">", generatedCode);
            Assert.Contains("/// <exception cref=\"NullReferenceException\">", generatedCode);
        }

        /// <summary>
        /// Asserts that the generated code contains proper null-safety features
        /// </summary>
        /// <param name="generatedCode">The generated source code</param>
        public static void ContainsNullSafetyFeatures(string generatedCode)
        {
            // Return attribute
            Assert.Contains("[return: NotNull]", generatedCode);
            
            // NotNullWhen attribute
            Assert.Contains("[NotNullWhen(true)]", generatedCode);
            
            // Null checks
            Assert.Contains("if (node == null)", generatedCode);
            
            // Exception throwing
            Assert.Contains("throw new NullReferenceException(", generatedCode);
            Assert.Contains("throw new InvalidCastException(", generatedCode);
        }
    }
}
