# NuGet Publishing Commands for Dapr.Analyzers

# Step 1: Add your NuGet API Key (run this once)
dotnet nuget add source https://api.nuget.org/v3/index.json -n nuget.org
dotnet nuget setapikey YOUR_API_KEY_HERE --source https://api.nuget.org/v3/index.json

# Step 2: Publish the package
dotnet nuget push "bin\Release\Dapr.Analyzers.1.0.0.nupkg" --source https://api.nuget.org/v3/index.json

# Alternative: Publish with explicit API key (if not set globally)
dotnet nuget push "bin\Release\Dapr.Analyzers.1.0.0.nupkg" --api-key YOUR_API_KEY_HERE --source https://api.nuget.org/v3/index.json

# Step 3: For future versions, increment version in .csproj and rebuild
# Update PackageVersion in Dapr.Analyzers.csproj
# Then run:
# dotnet pack -c Release
# dotnet nuget push "bin\Release\Dapr.Analyzers.NEW_VERSION.nupkg" --source https://api.nuget.org/v3/index.json
