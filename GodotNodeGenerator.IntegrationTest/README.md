# GodotNodeGenerator Integration Test

This directory contains a minimal Godot C# project for integration testing the GodotNodeGenerator source generator.

## Structure
- Minimal Godot project with a scene and C# script using the generator.
- The integration test project references the generator via ProjectReference.

## How to use
1. Open project in Godot (with .NET support enabled).
2. Build the C# project (from Godot or CLI).
3. Ensure the generated code appears and works as expected (e.g., Player node accessor is available).

You can automate build/run checks by adding a script or test runner in the future.
