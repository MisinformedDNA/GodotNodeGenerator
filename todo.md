# GodotNodeGenerator - Todo List

## High Priority Tasks

1. **Improve Scene File Parsing**
   - [ ] Implement proper TSCN parser instead of regex-based approach
   - [ ] Add support for binary SCN files
   - [ ] Handle escaped characters and special syntax in TSCN files

2. **Use AdditionalFiles for Scene Access**
   - [ ] Replace direct file I/O with proper use of AdditionalFiles
   - [ ] Create a configuration system for mapping scene files
   - [ ] Add documentation for setting up AdditionalFiles in .csproj

3. **Enhance Error Handling and Diagnostics**
   - [ ] Add more descriptive error messages for scene parsing issues
   - [ ] Create diagnostics for node type mismatches
   - [ ] Add warnings for outdated node references when scene structure changes

## Medium Priority Tasks

4. **Improve Node Type Handling**
   - [ ] Create a comprehensive mapping of Godot node types to C# types
   - [ ] Add support for custom node types
   - [ ] Handle generic nodes and inheritance

5. **Support for Advanced Node Access Patterns**
   - [ ] Add support for groups (GetNodesInGroup)
   - [ ] Generate methods for signal connections
   - [ ] Support for typed arrays of similar nodes (e.g., waypoints, spawn points)

6. **Code Quality and Performance**
   - [ ] Add unit tests for all components
   - [ ] Optimize performance for large scenes
   - [ ] Implement incremental processing for modified scenes only

## Low Priority Tasks

7. **Additional Features**
   - [ ] Generate editor tools for debugging generated code
   - [ ] Add Visual Studio/Godot integration for better tooling
   - [ ] Support for conditional node generation based on build configurations

8. **Documentation and Examples**
   - [ ] Create comprehensive API documentation
   - [ ] Build example projects demonstrating different use cases
   - [ ] Add troubleshooting guide for common issues

9. **Distribution and Packaging**
   - [ ] Create NuGet package for easy installation
   - [ ] Set up CI/CD pipeline for automatic builds
   - [ ] Add versioning and release notes

## Completed Tasks

- [x] Create basic project structure
- [x] Implement NodeGeneratorAttribute
- [x] Create incremental source generator skeleton
- [x] Implement basic scene parsing
- [x] Generate node accessor properties
