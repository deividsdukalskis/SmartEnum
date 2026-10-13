# SmartEnum

SmartEnum generates flattened data models and mapping methods for C# class hierarchies. It runs at compile time and adds no runtime dependency.

## Documentation

- [Attribute reference](docs/attributes.md) — what each attribute does and where to use it.
- [Flat, Nested and parent models](docs/flat-nested-parents.md) — model shapes, parent classes and collections.
- [Separate models and EF Core](docs/separate-ef-core.md) — concrete variant models and database mapping.
- [Legacy API](docs/legacy-api.md) — the one-argument `[SmartEnum<TKey>("Key")]` API.
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
Status? restored = Status.MapFromFlattenedDataUnvalidated(data);
```

**Unvalidated methods bypass domain validation.** Use them for plain data transfer, such as restoring ORM data. Existing constructors and setters can still execute user code.

## Attributes at a glance

| Attribute | Purpose | API |
| --- | --- | --- |
| `SmartEnum<TKey>` | Defines a discriminator; its three-argument form also selects the model shape and missing-key policy. | Both |
| `SmartEnumParent` | Generates a model for a class containing SmartEnum values or collections. | Three-argument |
| `SmartEnumIdentity` | Identifies a value to copy into related models as a foreign key. | Three-argument |
| `SmartEnumPrivateAccessor` | Reads and initializes a property's private backing field. | Three-argument |
| `SmartEnumNullableProperty` | Makes a transfer property nullable without changing the domain property's type. | Three-argument |
| `SmartEnumIgnore` | Excludes a field or property from mapping. | Both |
| `SmartEnumSnapshot` | Copies a computed property without restoring it. | Legacy |
| `SmartEnumParentData` | Maps an external base-constructor argument to readable state. | Legacy |

See the [attribute reference](docs/attributes.md) for parameters, examples and API availability.

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
| `ConstructUnvalidated(...)` | Creates a known concrete domain type from transfer data. |
| `MapFromFlattenedDataUnvalidated(...)` | Selects a concrete type using the discriminator values. The three-argument API returns `null` for an unknown combination. |
| `Flatten(...)` | Copies domain data into its flattened representation. |

Additional callback parameters supply missing values or map collections. Generated XML comments describe each callback.

## Data and declaration rules

- Roots and intermediate hierarchy classes are abstract; domain declarations are partial. A `SmartEnumParent` class must also be sealed.
- Each discriminator level has a matching `public const` field of the declared key type.
- Auto-getter properties and explicit public/private instance fields participate in mapping. An implemented getter without a setter needs `SmartEnumPrivateAccessor` in the three-argument API.
- Flattened member names use PascalCase without underscores: `_first_name` becomes `FirstName`. Conflicting names are diagnosed.
- The three-argument API preserves declared property nullability unless `SmartEnumNullableProperty` is applied. The legacy API automatically makes branch-specific data nullable.
- Included private fields become public transfer properties. Use `SmartEnumIgnore` for state that must not be transferred.
- Reference values are copied shallowly.

Supported declaration shapes differ between APIs. See the limitations in the [Flat/Nested guide](docs/flat-nested-parents.md#limitations), [Separate guide](docs/separate-ef-core.md#limitations) and [legacy guide](docs/legacy-api.md#limitations).

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
