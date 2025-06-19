# Clean any previous builds
Write-Host "Cleaning previous builds..." -ForegroundColor Blue
dotnet clean -c Release

# Build the project in Release mode
Write-Host "Building project in Release configuration..." -ForegroundColor Blue
dotnet build -c Release

# Run all tests to ensure quality
Write-Host "Running all tests..." -ForegroundColor Blue
dotnet test -c Release --no-build
if ($LASTEXITCODE -ne 0) {
    Write-Host "Tests failed! Please fix the issues before packaging." -ForegroundColor Red
    exit 1
}

# Create the NuGet package
Write-Host "Creating NuGet package..." -ForegroundColor Blue
dotnet pack -c Release --no-build ./GodotNodeGenerator/GodotNodeGenerator.csproj -o ./nupkg

Write-Host "Package created successfully in the ./nupkg directory!" -ForegroundColor Green
