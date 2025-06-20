using System.Runtime.CompilerServices;

namespace GodotNodeGenerator.Tests.TestHelpers
{
    /// <summary>
    /// Initializer for Verify settings across all tests
    /// </summary>
    public static class VerifyInitializer
    {
        [ModuleInitializer]
        public static void Initialize()
        {
            // Configure Verify settings for C# code snapshots
            VerifierSettings.AddScrubber(s => s.Replace("\r\n", "\n")); // Normalize line endings
        }
    }
}
