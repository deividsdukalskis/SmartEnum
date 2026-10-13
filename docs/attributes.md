# Attribute reference

[Back to README](../README.md)

All attributes are generated in the `SmartEnum` namespace. Add `using SmartEnum;` to your source file.

**API modes:** the three-argument `SmartEnum<TKey>` constructor selects Flat, Nested or Separate. The one-argument constructor selects the [legacy API](legacy-api.md).

## SmartEnum<TKey>

**Apply to:** an abstract partial hierarchy root. Flat/Nested hierarchies can also start a new discriminator group on an intermediate abstract class.

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

The shorter form, `[SmartEnum<int>("Kind")]`, uses the legacy API and its different mapping/nullability rules.

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

The model gets `List<string> Attendees`: the property's name and the field's type. The backing field is not added a second time.

## SmartEnumNullableProperty

**Apply to:** a property using the three-argument API, including parent properties. Supported with every strategy.

**Purpose:** makes the flattened property nullable while keeping the domain property's original type.

```csharp
[SmartEnumNullableProperty]
public DateTime CancelledAt { get; }
```

The model gets `DateTime? CancelledAt`. Reconstruction accepts `Func<DateTime> mapCancelledAt` and calls it only when the transfer value is null. For containers, the mapping delegate receives the nullable transfer container and decides how to restore it.

## SmartEnumIgnore

**Apply to:** a field or property. Supported by both APIs.

**Purpose:** excludes state from generated construction and flattening.

```csharp
[SmartEnumIgnore]
private string? _cachedDescription;
```

Initialize ignored state yourself. Required members cannot be ignored.

## SmartEnumSnapshot — legacy only

**Apply to:** a computed property in the one-argument API.

**Purpose:** copies its current value into the flattened model for information only. Reconstruction ignores the supplied snapshot and recomputes the property from domain state.

```csharp
[SmartEnumSnapshot]
public string DisplayName => $"{FirstName} {LastName}";
```

## SmartEnumParentData — legacy only

**Apply to:** the root class in the one-argument API.

**Purpose:** connects an external base-constructor parameter to a readable field or property when their names differ.

```csharp
[SmartEnumParentData("identifier", "StoredIdentifier")]
```

`identifier` names the constructor parameter; `StoredIdentifier` names the readable member. Names are case-sensitive and the types must match. This attribute concerns an inherited base constructor; `SmartEnumParent` concerns a class that contains SmartEnum values.
