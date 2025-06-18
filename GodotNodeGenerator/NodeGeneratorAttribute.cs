using System;

namespace GodotNodeGenerator
{
    /// <summary>
    /// Attribute to mark a class for node access code generation.
    /// Apply this to Godot node classes to generate strongly-typed accessors.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class NodeGeneratorAttribute : Attribute
    {
        /// <summary>
        /// Path to the scene file (*.tscn) to generate accessors for.
        /// If not specified, the generator will look for a scene file with the same name as the class.
        /// </summary>
        public string? ScenePath { get; }

        public NodeGeneratorAttribute(string? scenePath = null)
        {
            ScenePath = scenePath;
        }
    }
}
