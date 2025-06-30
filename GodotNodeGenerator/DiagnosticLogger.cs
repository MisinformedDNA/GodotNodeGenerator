using System;
using System.IO;
using System.Text;

namespace GodotNodeGenerator
{
    /// <summary>
    /// Helper class for diagnostic logging
    /// </summary>
    public static class DiagnosticLogger
    {
        private static readonly StringBuilder _logBuffer = new StringBuilder();
        private static bool _isEnabled = true;

        // Set to false to disable logging
        public static bool IsEnabled
        {
            get => _isEnabled;
            set => _isEnabled = value;
        }

        // Log a message with a timestamp
        public static void Log(string message)
        {
            if (!_isEnabled) return;

            var formattedMessage = $"[{DateTime.Now:HH:mm:ss.fff}] {message}";
            Console.WriteLine(formattedMessage);
            _logBuffer.AppendLine(formattedMessage);
        }

        // Log an exception
        public static void LogError(Exception ex, string context = "")
        {
            if (!_isEnabled) return;

            Log($"ERROR in {context}: {ex.GetType().Name}: {ex.Message}");
            Log(ex.StackTrace ?? "No stack trace available");
        }
    }
}
