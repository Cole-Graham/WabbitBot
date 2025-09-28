# WabbitBot Source Generators (THIS README OUT OF DATE, IGNORE)

This project contains source generators for the WabbitBot Discord bot, providing compile-time code generation for commands, embeds, events, and other boilerplate code.

## Project Structure

```
WabbitBot.SourceGenerators/
├── 📁 Generators/
│   ├── 📁 Command/
│   │   └── 📄 CommandGenerator.cs
│   ├── 📁 Embed/
│   │   └── 📄 EmbedFactoryGenerator.cs
│   ├── 📁 Event/
│   │   ├── 📄 EventBusSourceGenerator.cs
│   │   └── 📄 MatchEventGenerator.cs
│   └── 📁 Common/
│       ├── 📄 BaseGenerator.cs
│       ├── 📄 GeneratorHelpers.cs
│       └── 📄 SourceWriter.cs
├── 📁 Attributes/
│   ├── 📄 GenerateEmbedFactoryAttribute.cs
│   ├── 📄 GenerateImplementationAttribute.cs
│   └── 📄 WabbitCommandAttribute.cs
├── 📁 Templates/
│   ├── 📄 CommandTemplate.cs
│   ├── 📄 EmbedTemplate.cs
│   └── 📄 EventTemplate.cs
└── 📄 WabbitBot.SourceGenerators.csproj
```

## Generators

### CommandGenerator
- **Location**: `Generators/Command/CommandGenerator.cs`
- **Purpose**: Generates Discord command registration code
- **Attribute**: `[WabbitCommand]`
- **Output**: `CommandRegistration.g.cs`

### EmbedFactoryGenerator
- **Location**: `Generators/Embed/EmbedFactoryGenerator.cs`
- **Purpose**: Generates embed factory classes for Discord embeds
- **Attribute**: `[GenerateEmbedFactory]`
- **Output**: `EmbedFactories.g.cs`

### EventBusSourceGenerator
- **Location**: `Generators/Event/EventBusSourceGenerator.cs`
- **Purpose**: Generates event handler and publisher code
- **Attribute**: `[GenerateEventHandler]`, `[GenerateEventPublisher]`
- **Output**: `{ClassName}.g.cs`

### MatchEventGenerator
- **Location**: `Generators/Event/MatchEventGenerator.cs`
- **Purpose**: Generates event publishing methods for Match entities
- **Output**: `Match.g.cs`

## Common Utilities

### BaseGenerator
- **Location**: `Generators/Common/BaseGenerator.cs`
- **Purpose**: Base class for all source generators with common functionality

### GeneratorHelpers
- **Location**: `Generators/Common/GeneratorHelpers.cs`
- **Purpose**: Static helper methods for source generation

### SourceWriter
- **Location**: `Generators/Common/SourceWriter.cs`
- **Purpose**: Helper methods for writing generated source code

## Templates

### CommandTemplate
- **Location**: `Templates/CommandTemplate.cs`
- **Purpose**: Templates for command-related code generation

### EmbedTemplate
- **Location**: `Templates/EmbedTemplate.cs`
- **Purpose**: Templates for embed-related code generation

### EventTemplate
- **Location**: `Templates/EventTemplate.cs`
- **Purpose**: Templates for event-related code generation

## Attributes

All generator attributes are now located in the `Attributes/` directory:

- `GenerateEmbedFactoryAttribute`
- `GenerateImplementationAttribute`
- `WabbitCommandAttribute`

## Usage

1. **Commands**: Mark command classes with `[WabbitCommand]` attribute
2. **Embeds**: Mark embed classes with `[GenerateEmbedFactory]` attribute
3. **Events**: Mark handler classes with `[GenerateEventHandler]` attribute

## Generated Files

All generated files are placed in the `Generated/` directory of the consuming project and have the `.g.cs` extension to indicate they are generated code.

## Dependencies

- Microsoft.CodeAnalysis.CSharp (4.8.0)
- Microsoft.CodeAnalysis.Analyzers (3.3.4)

## Target Framework

- .NET Standard 2.0 (for maximum compatibility)
