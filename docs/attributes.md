# Attribute reference

[Back to README](../README.md)

All attributes are generated in the `SmartEnum` namespace. Add `using SmartEnum;` to your source file.

`SmartEnum<TKey>` takes three arguments. The former one-argument API has been removed.

## SmartEnum<TKey>

**Apply to:** an abstract partial hierarchy root. Every strategy supports starting a new hierarchy on an intermediate abstract class. It receives its own mapper and flattened model while remaining reachable through the outer mapper.

**Purpose:** defines how a discriminator identifies a branch and how that level's data is flattened.

```csharp
[SmartEnum<string>("Kind", FlatteningStrategy.Flat, KeyValueOnBranchMissing.FirstBranch)]
public abstract partial class Status { }

public sealed partial class Ready : Status
{
    public const string Kind = "ready";
}
```

| Parameter | Meaning |
| --- | --- |
| `TKey` | Type of the discriminator constant, such as `int` or `string`. |
| `keyName` | Name of the matching `public const` field on each branch. |
| `flatteningStrategy` | Shape of the flattened data at this level. |
| `keyValueOnBranchMissing` | What to do when a shorter branch has no discriminator at this level. |

Apply one attribute per discriminator level, in hierarchy order. Keep a group's attributes together in one declaration.

### FlatteningStrategy

| Value | Effect |
| --- | --- |
| `Flat` | Adds branch data to the current transfer object. |
| `Nested` | Creates nullable branch properties and nested flattened classes. |
| `Separate` | Generates a shared abstract flattened base and a model for each concrete variant. Use Separate at every level of the hierarchy. |

### KeyValueOnBranchMissing

| Value | Effect |
| --- | --- |
| `FirstBranch` | Marks the first discriminator level. Valid only on the first attribute in a group. |
| `SetNull` | Makes the discriminator nullable and writes `null` when that level is absent. |
| `PromptSubstitute` | Adds a `Func<TKey> mapKeyName` parameter to `Flatten`; calls it when that level is absent. |

A subclass group restarts discriminator levels. See [subclass hierarchies](flat-nested-parents.md#subclass-hierarchies) for its generated methods.

## SmartEnumParent

**Apply to:** a sealed partial class containing a SmartEnum property or a container whose type argument is in a SmartEnum hierarchy.

**Purpose:** generates `<ParentName>Flattened`, `ConstructUnvalidated(...)` and `Flatten(...)` for the containing class.

```csharp
[SmartEnumParent(FlatteningStrategy.Nested)]
public sealed partial class Meeting
{
    public Status Status { get; }
}
```

| Parent strategy | Direct hierarchy property |
| --- | --- |
| `Flat` | Merges the hierarchy's data into the parent model. |
| `Nested` | Puts hierarchy data in a nested object; root discriminators stay on the parent model. |
| `Separate` | Keeps a separate property using the hierarchy's own flattened model. |

Collections and arrays retain their container shape and use caller-provided mapping delegates. Separate hierarchies retain their variant models under any parent strategy.

## SmartEnumIdentity

**Apply to:** an identity property in a hierarchy or parent class. Optional; at most one identity per declared transfer level.

**Purpose:** supplies a value and column name for linking nested or independent flattened models.

```csharp
[SmartEnumIdentity("MeetingId")]
public string Id { get; }
```

The value of `Id` is copied into related models as `MeetingId`. If `foreignKeyName` is omitted, the property's name is used. Names such as `Id`, `Guid`, `Uuid` and `Key` produce a warning; conflicting names produce an error.

This attribute does **not** configure EF Core keys or relationships. Configure those explicitly.

## SmartEnumPrivateAccessor

**Apply to:** a property backed by a field declared in the same class.

**Purpose:** tells the generator which field to read and initialize. Required for an implemented getter without a setter; unnecessary for an auto getter.

```csharp
private readonly List<string> _attendees = new();

[SmartEnumPrivateAccessor(nameof(_attendees))]
public IReadOnlyList<string> Attendees => _attendees.AsReadOnly();
```

The model gets `List<string> Attendees`: the property's name and the field's type. The backing field is not added a second time. Other private fields are excluded automatically.

## SmartEnumNullableProperty

**Apply to:** a property, including parent properties. Supported with every strategy.

**Purpose:** makes the flattened property nullable while keeping the domain property's original type.

```csharp
[SmartEnumNullableProperty]
public DateTime CancelledAt { get; }
```

The model gets `DateTime? CancelledAt`. Reconstruction accepts `Func<DateTime> mapCancelledAt` and calls it only when the transfer value is null. For containers, the mapping delegate receives the nullable transfer container and decides how to restore it.

## SmartEnumIgnore

**Apply to:** a public field or a property.

**Purpose:** excludes state from generated construction and flattening.

```csharp
[SmartEnumIgnore]
public string? CachedDescription { get; set; }
```

Initialize ignored state yourself. Required members cannot be ignored.
