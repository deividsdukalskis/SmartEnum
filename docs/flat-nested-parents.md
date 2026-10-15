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
| Discriminator is unknown | Root construction returns null. Required parent properties invoke their fallback; nullable properties accept null. Container delegates decide their own handling. |
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

## Subclass hierarchies

An abstract subclass can declare its own `SmartEnum<TKey>` group with any strategy. Its children use the new discriminator names, and its first attribute can use `FirstBranch`. New groups replace remaining ancestor discriminator levels below that subclass.

```csharp
[SmartEnum<int>("Kind", FlatteningStrategy.Flat, KeyValueOnBranchMissing.FirstBranch)]
public abstract partial class MeetingStatus { }

[SmartEnum<int>("ScheduledType", FlatteningStrategy.Flat, KeyValueOnBranchMissing.FirstBranch)]
public abstract partial class Scheduled : MeetingStatus
{
    public const int Kind = 1;
}

public sealed partial class Online : Scheduled
{
    public const int ScheduledType = 2;
}

Scheduled scheduled = Scheduled.ConstructUnvalidated(
    new ScheduledFlattened { ScheduledType = 2 })!;
ScheduledFlattened scheduledData = scheduled.FlattenHierarchy();
MeetingStatusFlattened meetingData = scheduled.Flatten();
MeetingStatus? restored = MeetingStatus.ConstructUnvalidated(meetingData);
```

Each new group has a `<Subclass>Flattened` model and its own `ConstructUnvalidated` method. Its model also carries inherited domain data. `FlattenHierarchy()` returns that group's model; inherited `Flatten()` continues to return the outer hierarchy model. Both mapping entry points reach the concrete descendants.

Flat/Nested groups have an independent model and a nested representation in the outer model. Separate groups have an abstract flattened base in the shared flattened inheritance chain. Use Separate throughout that hierarchy.
## Generic and reusable parent properties

Use `SmartEnumParent(Separate)` for a generic parent, a generic hierarchy property, or the same hierarchy in multiple properties or parents. The hierarchy keeps its standalone flattened model. Arrays and generic containers retain their shapes.

```csharp
[SmartEnumParent(FlatteningStrategy.Separate)]
public sealed partial class Comparison<T>
{
    public Status<T> Before { get; }
    public Status<T> After { get; }
}
```

`ComparisonFlattened<T>` contains `StatusFlattened<T> Before` and `After`. Direct properties use automatic construction with a fallback for required values:

```csharp
var comparison = Comparison<string>.ConstructUnvalidated(data,
    mapAfter: () => GetDefaultAfterStatus(),
    mapBefore: () => GetDefaultBeforeStatus());

var flattened = comparison.Flatten(
    mapAfter: value => value.Flatten(),
    mapBefore: value => value.Flatten());
```

`Status<T>` represents your generic annotated hierarchy; `GetDefaultAfterStatus` and `GetDefaultBeforeStatus` are application-provided fallback methods. Direct hierarchy values call `ConstructUnvalidated` on the root automatically. A required-property callback runs only for absent data, unknown discriminators or an incompatible variant. Nullable direct properties need no fallback and accept null. Arrays and generic wrapper/container types retain conversion delegates that receive the complete transfer value. A generic hierarchy such as `Status<T>` is itself a direct value, not a wrapper.

Scalar fallbacks needed by a direct reusable hierarchy are named by both property and member, such as `mapAfterCount`. The separate `mapAfter` fallback supplies the whole required hierarchy value. Generated XML parameter comments explain the distinction. Use named arguments for these callbacks.

All owners of a reused hierarchy must use Separate. Owner identities remain on the owner model; shared hierarchy models do not acquire an owner's foreign-key columns. Configure owner relationships in EF Core explicitly. Existing unique, non-generic ownership retains its generated identity propagation and mapping signatures. Adding a second owner or making a contract generic selects the reusable API; wrapper conversion and flattening delegates remain per property.

## EF Core

Configure EF against the generated models:

- Map a flat owner model to one table.
- Map nested models as owned entities, optionally in separate tables.
- Use `SmartEnumIdentity` to supply generated foreign-key values, then configure EF keys and relationships explicitly.
- Update tracked flattened objects or handle detached updates explicitly; reconstructed domain objects are separate objects.

See [the tested SQLite configuration](../EFCoreTests/CompositePersistence.cs) for an example. The generator does not create tables, migrations or EF configuration.

## Ordinary base classes

Hierarchy roots and parent models may inherit from ordinary classes. Accessible inherited instance properties are included in the flattened model across all strategies, including properties from generic and multi-level base classes.

```csharp
public class Entity
{
    public string Id { get; }
    public string? Label { get; protected set; }
    protected Entity(string id) => Id = id;
}

[SmartEnum<int>("Kind", FlatteningStrategy.Flat, KeyValueOnBranchMissing.FirstBranch)]
public abstract partial class Status : Entity { }

public sealed partial class Ready : Status
{
    public const int Kind = 1;
}

Ready ready = Ready.ConstructUnvalidated(new StatusFlattened
{
    Kind = 1, Id = "status-1", Label = "Ready"
});
```

The generated root constructor calls `Entity(id)` and initializes `Label`. `Flatten()` copies both properties back. No changes or partial declarations are required in `Entity`.

Constructor parameters match transfer properties by normalized name and exact type (`id` matches `Id`). Read-only properties must be supplied through the selected base constructor. Writable properties use accessible setters or init accessors. Unmatched optional constructor parameters keep their defaults. The generator prefers the constructor with the most matched parameters, then the fewest total parameters; tied choices produce SMARTENUM019.

If base parameter names differ, provide a protected constructor on the annotated root that accepts the complete root data in alphabetical normalized-property order and calls the appropriate base constructor. The generator reuses that constructor. SMARTENUM019 now reports an incompatible construction contract, not ordinary inheritance itself.

Public instance fields participate under the same read/write rules. Private base fields and private properties are excluded. Inherited properties without an accessible getter cannot be flattened. Use `SmartEnumIgnore` for computed state that should not be transferred; required properties cannot be ignored. Property name collisions and inherited generated-API names are diagnosed. Base constructors and setters still execute their own code during reconstruction.

This support concerns ordinary base classes. Existing subclass hierarchy groups continue to use their SmartEnum discriminator rules.

## Limitations

- Roots and intermediate branches are abstract; parent classes are sealed. Generated declarations and enclosing types must be partial. Ordinary base classes do not need to be partial.
- Primary constructors and positional records are unsupported. Ordinary classes and non-positional records are supported.
- Generic hierarchies require variant parameters that can be inferred from the root and satisfy its constraints. Generic parents and generic hierarchy properties use `SmartEnumParent(Separate)`.
- A hierarchy can be reused across properties and parents when every owner uses `SmartEnumParent(Separate)`. Flat/Nested ownership still requires a unique non-generic hierarchy; distinct hierarchies can share a parent when generated names and callbacks do not collide.
- A parent property can reference a root or a concrete variant.
- Separate cannot be mixed with Flat/Nested within the same hierarchy.
- References and retained transfer objects are shared, not deep-copied. Collection delegates control element conversion.
- Mappers recognize variants declared in the current compilation. Runtime subclasses and variants in other assemblies require an explicit integration boundary.
