# Analyzers.Dapr

![CI](https://github.com/moonolgerd/Analyzers.Dapr/workflows/CI/badge.svg)
![NuGet](https://img.shields.io/nuget/v/Analyzers.Dapr.svg)

A .NET analyzer that validates classes implementing the Dapr Actor base class to ensure they follow proper serialization rules for strongly-typed >.NET Dapr Actor clients.

## Overview

This analyzer helps developers follow the serialization guidelines outlined in the [Dapr Actor Serialization documentation](https://docs.dapr.io/developing-applications/sdks/dotnet/dotnet-actors/dotnet-actors-serialization/) by providing compile-time validation and code fixes.

## Diagnostic Rules

### DAPR001 - Actor interface should inherit from IActor
**Severity:** Error

Interfaces implemented by Actor classes should inherit from `IActor` interface.

**Bad:**
```csharp
public interface IWeatherActor
{
    Task<string> GetWeatherAsync();
}

public class WeatherActor : Actor, IWeatherActor
{
    public WeatherActor(ActorHost host) : base(host) { }
    public Task<string> GetWeatherAsync() => Task.FromResult("Sunny");
}
```

**Good:**
```csharp
public interface IWeatherActor : IActor
{
    Task<string> GetWeatherAsync();
}

public class WeatherActor : Actor, IWeatherActor
{
    public WeatherActor(ActorHost host) : base(host) { }
    public Task<string> GetWeatherAsync() => Task.FromResult("Sunny");
}
```

### DAPR002 - Enum members should use EnumMember attribute
**Severity:** Warning

Enum members used in Actor types should use `[EnumMember]` attribute for consistent serialization.

**Bad:**
```csharp
public enum Season
{
    Spring,
    Summer,
    Fall,
    Winter
}
```

**Good:**
```csharp
public enum Season
{
    [EnumMember]
    Spring,
    [EnumMember]
    Summer,
    [EnumMember]
    Fall,
    [EnumMember]
    Winter
}
```

### DAPR003 - Consider using JsonPropertyName for property name consistency
**Severity:** Information

Properties in Actor classes used with weakly-typed clients should consider `[JsonPropertyName]` attribute for consistent property naming.

**Example:**
```csharp
public class WeatherData
{
    [JsonPropertyName("temperature")]
    public double Temperature { get; set; }
    
    [JsonPropertyName("humidity")]
    public int Humidity { get; set; }
}
```

### DAPR004 - Complex types used in Actor methods need serialization attributes
**Severity:** Warning

Complex types used as parameters or return types in Actor methods should have proper serialization attributes.

**Bad:**
```csharp
public class WeatherData
{
    public double Temperature { get; set; }
    public int Humidity { get; set; }
}

public interface IWeatherActor : IActor
{
    Task<WeatherData> GetWeatherAsync(); // WeatherData lacks serialization attributes
}
```

**Good:**
```csharp
[DataContract]
public class WeatherData
{
    [DataMember]
    public double Temperature { get; set; }
    
    [DataMember]
    public int Humidity { get; set; }
}

public interface IWeatherActor : IActor
{
    Task<WeatherData> GetWeatherAsync();
}
```

### DAPR005 - Actor method parameter needs proper serialization attributes
**Severity:** Warning

Parameters in Actor methods should use types with proper serialization attributes for reliable data transfer.

**Bad:**
```csharp
public class LocationData
{
    public string City { get; set; }
    public string Country { get; set; }
}

public interface IWeatherActor : IActor
{
    Task<string> GetWeatherAsync(LocationData location); // LocationData lacks serialization attributes
}
```

**Good:**
```csharp
[DataContract]
public class LocationData
{
    [DataMember]
    public string City { get; set; }
    
    [DataMember]
    public string Country { get; set; }
}

public interface IWeatherActor : IActor
{
    Task<string> GetWeatherAsync(LocationData location);
}
```

### DAPR006 - Actor method return type needs proper serialization attributes
**Severity:** Warning

Return types in Actor methods should have proper serialization attributes for reliable data transfer.

### DAPR007 - Collection types in Actor methods need element type validation
**Severity:** Warning

Collection types used in Actor methods should contain elements with proper serialization attributes.

**Bad:**
```csharp
public class WeatherReading
{
    public DateTime Timestamp { get; set; }
    public double Value { get; set; }
}

public interface IWeatherActor : IActor
{
    Task<List<WeatherReading>> GetHistoryAsync(); // WeatherReading lacks serialization attributes
}
```

**Good:**
```csharp
[DataContract]
public class WeatherReading
{
    [DataMember]
    public DateTime Timestamp { get; set; }
    
    [DataMember]
    public double Value { get; set; }
}

public interface IWeatherActor : IActor
{
    Task<List<WeatherReading>> GetHistoryAsync();
}
```

### DAPR008 - Record types should use DataContract and DataMember attributes
**Severity:** Warning

Record types used in Actor methods should have `[DataContract]` attribute and `[DataMember]` attributes on all properties for reliable serialization.

**Bad:**
```csharp
public record WeatherData(double Temperature, int Humidity);

public interface IWeatherActor : IActor
{
    Task<WeatherData> GetWeatherAsync();
}
```

**Good:**
```csharp
[DataContract]
public record WeatherData([DataMember] double Temperature, [DataMember] int Humidity);

public interface IWeatherActor : IActor
{
    Task<WeatherData> GetWeatherAsync();
}
```

### DAPR009 - Actor class should implement an interface that inherits from IActor
**Severity:** Error

Actor class implementations should implement an interface that inherits from `IActor` for proper Actor pattern implementation.

**Bad:**
```csharp
public class WeatherActor : Actor
{
    public WeatherActor(ActorHost host) : base(host) { }
    
    public Task<string> GetWeatherAsync() => Task.FromResult("Sunny");
}
```

**Good:**
```csharp
public interface IWeatherActor : IActor
{
    Task<string> GetWeatherAsync();
}

public class WeatherActor : Actor, IWeatherActor
{
    public WeatherActor(ActorHost host) : base(host) { }
    
    public Task<string> GetWeatherAsync() => Task.FromResult("Sunny");
}
```

### DAPR010 - Types must have parameterless constructor or DataContract attribute
**Severity:** Error

All types used in Actor methods must either expose a public parameterless constructor or be decorated with the `[DataContract]` attribute for reliable serialization.

**Bad:**
```csharp
public class WeatherData
{
    public WeatherData(double temperature)
    {
        Temperature = temperature;
    }
    
    public double Temperature { get; }
}
```

**Good (Option 1 - Parameterless Constructor):**
```csharp
public class WeatherData
{
    public WeatherData() { }
    
    public WeatherData(double temperature)
    {
        Temperature = temperature;
    }
    
    public double Temperature { get; set; }
}
```

**Good (Option 2 - DataContract):**
```csharp
[DataContract]
public class WeatherData
{
    public WeatherData(double temperature)
    {
        Temperature = temperature;
    }
    
    [DataMember]
    public double Temperature { get; }
}
```

## Installation

Install the analyzer via NuGet:

```xml
<PackageReference Include="Analyzers.Dapr" Version="1.0.0">
  <PrivateAssets>all</PrivateAssets>
  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
</PackageReference>
```

## Configuration

You can configure the severity of each rule in your `.editorconfig` file:

```ini
[*.cs]
# Dapr Actor Analyzer Rules
dotnet_diagnostic.DAPR001.severity = error
dotnet_diagnostic.DAPR002.severity = warning
dotnet_diagnostic.DAPR003.severity = suggestion
dotnet_diagnostic.DAPR004.severity = warning
dotnet_diagnostic.DAPR005.severity = warning
dotnet_diagnostic.DAPR006.severity = warning
dotnet_diagnostic.DAPR007.severity = warning
dotnet_diagnostic.DAPR008.severity = warning
dotnet_diagnostic.DAPR009.severity = error
dotnet_diagnostic.DAPR010.severity = error
```

## Code Fixes

The analyzer provides automatic code fixes for:
- Adding inheritance from `IActor` to Actor interfaces (DAPR001)
- Adding `[EnumMember]` attribute to enum members (DAPR002)
- Adding `[DataContract]` attribute to complex types (DAPR004, DAPR005, DAPR006)
- Adding `[DataMember]` attribute to properties (DAPR004, DAPR005, DAPR006)
- Adding `[DataContract]` and `[DataMember]` attributes to record types (DAPR008)
- Adding parameterless constructors to types (DAPR010)

## Building

To build the analyzer:

```bash
dotnet build Analyzers.Dapr.csproj
```

To run tests:

```bash
dotnet test Analyzers.Dapr.Tests.csproj
```

## CI/CD

This project uses GitHub Actions for continuous integration and deployment:

### Workflows

- **CI (`ci.yml`)**: Runs on every push and pull request
  - Builds the project on multiple platforms (Ubuntu, Windows, macOS)
  - Runs all tests with code coverage
  - Performs code quality checks with `dotnet format`
  - Validates NuGet packages
  - Uploads test results and coverage reports

- **Release (`release.yml`)**: Triggered by version tags or manual dispatch
  - Creates GitHub releases with release notes
  - Publishes packages to NuGet.org
  - Requires `NUGET_API_KEY` secret to be configured

- **Security (`security.yml`)**: Runs security scans
  - CodeQL analysis for security vulnerabilities
  - .NET security audit for package vulnerabilities
  - Dependency review for pull requests

- **Documentation (`docs.yml`)**: Validates and builds documentation
  - Lints markdown files
  - Checks for broken links
  - Generates API documentation with DocFX
  - Deploys to GitHub Pages

- **Performance (`performance.yml`)**: Monitors analyzer performance
  - Tracks build times and memory usage
  - Monitors package size
  - Runs weekly performance regression tests

- **Dependency Updates (`dependency-updates.yml`)**: Automated dependency management
  - Checks for outdated packages weekly
  - Creates pull requests for updates
  - Works alongside Dependabot configuration

### Secrets Required

To fully utilize the CI/CD pipeline, configure these repository secrets:

- `NUGET_API_KEY`: API key for publishing to NuGet.org

## Contributing

When contributing to this analyzer, please ensure:
1. All new rules have corresponding tests
2. Code fixes are provided where applicable
3. Documentation is updated for new rules
4. Follow the existing code style and patterns

## License

This project is licensed under the MIT License.
