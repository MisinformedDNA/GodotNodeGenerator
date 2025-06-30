using System;
using System.Diagnostics;
using System.IO;

namespace GodotNodeGenerator
{
    /// <summary>
    /// Helper class to facilitate source generator debugging
    /// </summary>
    public static class SourceGeneratorDebugger
    {
        // Call this from key points in your code where you want to attach the debugger
        public static void WaitForDebugger(string message = "")
        {
            //if (!string.IsNullOrEmpty(message))
            //{
            //    Console.WriteLine($"DEBUG BREAKPOINT: {message}");
            //}
            
            //Console.WriteLine($"Process ID: {Process.GetCurrentProcess().Id}");
            //Console.WriteLine("Waiting for debugger to attach...");
            //Console.WriteLine("Press any key to continue...");
            
            //// Wait for the debugger
            //while (!Debugger.IsAttached)
            //{
            //    System.Threading.Thread.Sleep(100);
            //}
            
            //// Break into the debugger
            //Debugger.Break();
        }
    }
}
