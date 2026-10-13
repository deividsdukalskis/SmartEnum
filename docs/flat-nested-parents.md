# Flat, Nested and parent models

[Back to README](../README.md) · [Attribute reference](attributes.md) · [Separate models](separate-ef-core.md)

Use the three-argument `SmartEnum<TKey>` attribute to choose how hierarchy data is stored. Add `SmartEnumParent` when another class contains that hierarchy.

## Model shapes

| Setting | Generated shape |
| --- | --- |
| Hierarchy `Flat` | Branch data is added to the current transfer object. |
| Hierarchy `Nested` | Each branch gets a nullable property and a nested `<Branch>Flattened` class. |
| Parent `Flat` | Direct hierarchy data is merged into `<Parent>Flattened`. |
| Parent `Nested` | Direct hierarchy data goes into a nested `<Root>Flattened` object. Root discriminators stay on the parent model. |
| Parent `Separate` | Each hierarchy remains a separate property using its own flattened model. |
| Collection or array | The container shape is retained, with hierarchy types replaced by flattened types. |

Flat and Nested can be mixed across discriminator levels. A Separate hierarchy retains its abstract base and concrete variant models under any parent strategy.

## Example: a flat parent model

```csharp
using SmartEnum;

[SmartEnumParent(FlatteningStrategy.Flat)]
public sealed partial class Meeting
{
    [SmartEnumIdentity("MeetingId")]
    public string Id { get; }
    public Status Status { get; }
}

[SmartEnum<int>("Kind", FlatteningStrategy.Flat, KeyValueOnBranchMissing.FirstBranch)]
public abstract partial class Status { }

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

`MeetingFlattened` contains `Id`, `Kind`, `Count` and `Reason`.

```csharp
var data = new MeetingFlattened
{
    Id = "meeting-1", Kind = 2, Count = 42, Reason = "weather"
};

Meeting meeting = Meeting.ConstructUnvalidated(
    data, mapStatus: () => throw new InvalidOperationException("Unknown status"));

MeetingFlattened result = meeting.Flatten(mapCount: () => 0);
```

`mapStatus` handles unknown discriminators. Although the selected status is Cancelled, the existing `Count = 42` is retained; `mapCount` is not called.

## Missing values and retained data

| Situation | Behavior |
| --- | --- |
| Selected branch has the member | `Flatten` writes its current domain value. |
| Another branch owns a flat member | Its input value is retained. A required value with no retained data uses a fallback delegate. |
| Nested branch is inactive | Its input object is retained, or remains null if absent. |
| Active nested object is missing | Reconstruction requests fallbacks for non-nullable members; nullable members stay null. |
| Discriminator is unknown | Root mapping returns null. The parent fallback or container delegate decides what to do. |
| Nested object containing a restarted discriminator is missing | The concrete variant cannot be identified; mapping returns null. |

Properties keep their declared nullability unless marked `SmartEnumNullableProperty`. Zero and false are valid values. References and inactive nested objects are copied shallowly; keep retained input objects stable.

Unvalidated methods are for plain data transfer. Custom constructors, setters and delegates can still run user code.

## Collections and backing fields

Use `SmartEnumPrivateAccessor` for an implemented getter backed by a private field:

```csharp
private readonly List<Status> _statuses = new();

[SmartEnumPrivateAccessor(nameof(_statuses))]
public IReadOnlyList<Status> Statuses => _statuses.AsReadOnly();
```

The transfer property uses the backing field's container type: `List<StatusFlattened>`. Under a Flat/Nested parent, the element model is nested in the parent model, such as `MeetingFlattened.StatusFlattened`.

The parent's factory and `Flatten` method accept container-mapping delegates. A delegate receives either the container alone or a named tuple with the container, owner identity and required fallback delegates. It controls element order, filtering and unknown-variant handling. Custom generic containers must accept the substituted flattened types.

## Restarting a discriminator group

An intermediate abstract class can declare a new `SmartEnum<TKey>` group. It receives a new nested flattened class containing the new discriminator properties.

The preceding group's levels must end at that class. Its children must declare constants using the new key names. `FirstBranch` is valid on the first attribute of each group.

## EF Core

Configure EF against the generated models:

- Map a flat owner model to one table.
- Map nested models as owned entities, optionally in separate tables.
- Use `SmartEnumIdentity` to supply generated foreign-key values, then configure EF keys and relationships explicitly.
- Update tracked flattened objects or handle detached updates explicitly; reconstructed domain objects are separate objects.

See [the tested SQLite configuration](../EFCoreTests/CompositePersistence.cs) for an example. The generator does not create tables, migrations or EF configuration.

## Limitations

- Composite hierarchies and parents must be namespace-level, non-generic ordinary partial classes. Roots and intermediate branches are abstract; parents are sealed and derive directly from object.
- Generic containers are supported. Generic domain roots/parents, records and enclosing type declarations are not.
- One hierarchy can belong to one parent property. A parent can contain multiple distinct hierarchies when generated names and callbacks do not collide.
- A parent property can reference a root or a concrete variant.
- Separate cannot be mixed with Flat/Nested within the same hierarchy.
