# SmartEnum

SmartEnum generates flattened data models and mapping methods for C# class hierarchies. It runs at compile time and adds no runtime dependency.

## Documentation

- [Attribute reference](docs/attributes.md) — what each attribute does and where to use it.
- [Flat, Nested and parent models](docs/flat-nested-parents.md) — model shapes, parent classes and collections.
- [Separate models and EF Core](docs/separate-ef-core.md) — concrete variant models and database mapping.
- [Diagnostics](docs/diagnostics.md) — analyzer errors, warnings and Visual Studio settings.
- [Private NuGet installation and publishing](docs/github-packages.md).

## Quick start

Install the SmartEnum analyzer package, then declare a partial hierarchy:

```csharp
using SmartEnum;

[SmartEnum<int>("Kind", FlatteningStrategy.Separate, KeyValueOnBranchMissing.FirstBranch)]
public abstract partial class Status
{
    public string Id { get; }
}

public sealed partial class Ready : Status
{
    public const int Kind = 1;
    public int Count { get; }
}

public sealed partial class Cancelled : Status
{
    public const int Kind = 2;
    public string? Reason { get; }
}
```

This generates an abstract `StatusFlattened` model and concrete `ReadyFlattened` and `CancelledFlattened` models.

```csharp
Ready ready = Ready.ConstructUnvalidated(new ReadyFlattened
{
    Id = "status-1", Kind = 1, Count = 42
});

StatusFlattened data = ready.Flatten();
Status? restored = Status.ConstructUnvalidated(data);
```

**Unvalidated methods bypass domain validation.** Use them for plain data transfer, such as restoring ORM data. Existing constructors and setters can still execute user code.

## Attributes at a glance

| Attribute | Purpose |
| --- | --- |
| `SmartEnum<TKey>` | Starts a hierarchy or subclass hierarchy; selects its discriminator, model shape and missing-key policy. |
| `SmartEnumParent` | Generates a model for a class containing SmartEnum values or collections. |
| `SmartEnumIdentity` | Identifies a value to copy into related models as a foreign key. |
| `SmartEnumPrivateAccessor` | Reads and initializes a property's explicitly named backing field. |
| `SmartEnumNullableProperty` | Makes a transfer property nullable without changing the domain property's type. |
| `SmartEnumIgnore` | Excludes a field or property from mapping. |

See the [attribute reference](docs/attributes.md) for parameters and examples.
## Choose a model shape

| Strategy | Result |
| --- | --- |
| `Flat` | Branch data shares one transfer object. |
| `Nested` | Branch data is grouped into nullable nested objects. |
| `Separate` | Each concrete variant has its own model derived from a shared abstract flattened base. |

Flat and Nested can be mixed at different discriminator levels. Separate must be selected for every level of its hierarchy.

## Generated methods

| Method | Purpose |
| --- | --- |
| `ConstructUnvalidated(...)` | On a hierarchy root, selects a concrete variant using discriminators and returns null for unknown values. On a concrete class, constructs that known type. On a parent, restores its data and hierarchy properties. |
| `Flatten(...)` | Copies domain data into its outer hierarchy representation. |
| `FlattenHierarchy(...)` | Copies a subclass hierarchy into its own `<Subclass>Flattened` model. |

Direct hierarchy properties are constructed automatically. Required properties have a fallback callback for missing or unknown values; nullable properties accept null. Containers use conversion delegates. Each generated XML parameter comment explains the target property, why a callback exists and when it runs.

## Data and declaration rules

- Roots and intermediate hierarchy classes are abstract; domain declarations are partial. A `SmartEnumParent` class must also be sealed.
- Each discriminator level has a matching `public const` field of the declared key type.
- Properties and explicit public instance fields participate in mapping. An implemented getter without a setter needs `SmartEnumPrivateAccessor`.
- Private fields are excluded unless a property explicitly names them with `SmartEnumPrivateAccessor`. The transfer member uses the property's name and the backing field's type.
- Flattened member names use PascalCase without underscores: `_first_name` becomes `FirstName`. Conflicting names are diagnosed.
- Properties preserve declared nullability unless `SmartEnumNullableProperty` is applied.
- Standalone hierarchies support compatible generics, non-positional records and types nested in partial declarations.
- Generic or reused hierarchy properties are supported under `SmartEnumParent(Separate)` with independent conversion delegates per property.
- Roots and parent models may inherit from ordinary classes; inherited accessible properties are included. See [ordinary base classes](docs/flat-nested-parents.md#ordinary-base-classes) for constructor rules.
- Reference values are copied shallowly.

The one-argument attribute, `SmartEnumSnapshot` and `SmartEnumParentData` have been removed. Migrate to the three-argument attribute and explicit accessors. SMARTENUM008 is retired: an attribute on an abstract subclass starts a new hierarchy. See the [subclass example](docs/flat-nested-parents.md#subclass-hierarchies) and remaining [limitations](docs/flat-nested-parents.md#limitations).
## Requirements

- C# 11 or newer.
- Roslyn 5.6 or newer; tested with .NET SDK 10.0.401.
- The generator targets .NET Standard 2.0. Consumers using required members need a framework that provides the required-member support attributes.

## Show errors in closed files

In Visual Studio, set **Tools → Options → Text Editor → C# → Advanced → Run background code analysis for → Entire solution** to see diagnostics for closed files.

## Development checks

```powershell
dotnet run --project GeneratorTests -c Release
dotnet run --project EFCoreTests -c Release
```

These run generator/analyzer regression checks and SQLite persistence tests. The [package guide](docs/github-packages.md) covers the publishing workflow.
