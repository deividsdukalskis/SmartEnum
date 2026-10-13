# Separate models and EF Core

[Back to README](../README.md) · [Attribute reference](attributes.md) · [Flat/Nested models](flat-nested-parents.md)

`Separate` generates an abstract `<Root>Flattened` base and a concrete `<Variant>Flattened` model for each concrete domain variant. Each variant contains inherited data and its own branch data; other branches' properties are omitted.

## Example

```csharp
using SmartEnum;

[SmartEnum<string>("Kind", FlatteningStrategy.Separate, KeyValueOnBranchMissing.FirstBranch)]
public abstract partial class Status
{
    public string Id { get; }
    public string MeetingId { get; }
}

public sealed partial class Ready : Status
{
    public const string Kind = "ready";
    public int Count { get; }
}

public sealed partial class Cancelled : Status
{
    public const string Kind = "cancelled";
    public string? Reason { get; }
}
```

| Generated model | Properties |
| --- | --- |
| Abstract `StatusFlattened` | `Id`, `MeetingId`, `Kind` |
| `ReadyFlattened` | Inherits shared properties; adds `Count` |
| `CancelledFlattened` | Inherits shared properties; adds `Reason` |

```csharp
Ready ready = Ready.ConstructUnvalidated(new ReadyFlattened
{
    Id = "ready-1", MeetingId = "meeting-1", Kind = "ready", Count = 42
});

ReadyFlattened concrete = ready.Flatten();
Status domain = ready;
StatusFlattened data = domain.Flatten();
Status? restored = Status.MapFromFlattenedDataUnvalidated(data);
```

The mapper returns null when the concrete model type and discriminator values do not identify the same variant. Unvalidated methods bypass domain validation. Reference values are copied shallowly.

Properties keep their declared nullability. Use `SmartEnumNullableProperty` for nullable transfer values and `SmartEnumPrivateAccessor` for implemented getters backed by fields; see the [attribute reference](attributes.md).

## EF Core: one table per concrete variant

Configure table-per-concrete-type (TPC) mapping on the flattened models:

```csharp
modelBuilder.Entity<StatusFlattened>().UseTpcMappingStrategy();
modelBuilder.Entity<StatusFlattened>().HasKey(x => x.Id);
modelBuilder.Entity<ReadyFlattened>().ToTable("ReadyStatuses");
modelBuilder.Entity<CancelledFlattened>().ToTable("CancelledStatuses");
modelBuilder.Entity<StatusFlattened>()
    .HasOne<MeetingRow>().WithMany().HasForeignKey(x => x.MeetingId);
```

`MeetingRow` is your separately configured meeting entity with a compatible primary key. See [the complete test model](../EFCoreTests/PersistenceModel.cs).

| Concern | Rule |
| --- | --- |
| Tables | The abstract base has no table. Each concrete table includes inherited columns. |
| Registration | Register every concrete variant explicitly. |
| Primary keys | Values must be unique across the hierarchy. Database-generated strategies depend on the provider. |
| Discriminators | `Kind` is ordinary stored data. TPC identifies the concrete type by its table. |
| Foreign keys | Each variant table can reference a meeting table. A single database foreign key cannot target the whole multi-table status hierarchy. |
| Updates | EF tracks the flattened objects. Copy domain changes into tracked models or manage detached updates. |
| Changing variants | Moving to another variant means changing entity/table; handle it in an application transaction. |

The generator has no EF runtime dependency and does not configure persistence automatically. See [Microsoft's TPC documentation](https://learn.microsoft.com/en-us/ef/core/modeling/inheritance#table-per-concrete-type-configuration) for provider details.

## Limitations

- Select Separate at every discriminator level and declare the levels on the root.
- Standalone Separate supports namespace-level ordinary partial classes and compatible generic hierarchies.
- Records, enclosing type declarations, external root base classes and restarted discriminator groups are unsupported in Separate.
- Parent mapping has the additional [composite limitations](flat-nested-parents.md#limitations).
- Generated method names and flattened-data constructor signatures must remain available. Compatible protected data constructors on shared classes can be reused.
