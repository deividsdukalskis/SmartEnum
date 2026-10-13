# Legacy API

[Back to README](../README.md) · [Attribute reference](attributes.md)

This guide applies only to the one-argument attribute:

```csharp
[SmartEnum<int>("Kind")]
public abstract partial class Status { }
```

Use the three-argument API when you need configurable [Flat/Nested parent models](flat-nested-parents.md) or [Separate variant models](separate-ef-core.md).

## Generated API

| Member | Behavior |
| --- | --- |
| `<Root>Flattened` | A sealed partial model containing all hierarchy data and discriminator properties. |
| `ConstructUnvalidated(...)` | Creates a concrete type from individual data arguments. |
| `MapFromFlattenedDataUnvalidated(model)` | Checks the supplied data and selects a concrete type. Unknown combinations throw `ArgumentException`. |
| `Flatten()` | Copies current domain values into a new model. Data belonging to other branches is null. |
| `MapDataToTypeUnvalidated(...)` | Compatibility adapter with the original individual-argument signature; hidden from IntelliSense. |

Constructors receive inherited data first, then their own members in alphabetical order. Compatible existing data constructors can be reused: protected on abstract types, private on concrete types. Constructors that initialize required members must declare `SetsRequiredMembers`.

**Unvalidated methods bypass domain validation.** Transfer checks still run, and custom constructors or setters may validate or throw.

## Nullability and transfer checks

- Data not present in every concrete branch becomes nullable. Shared data keeps its declared type.
- Null model input throws `ArgumentNullException`.
- Omitted discriminators or required branch values throw `ArgumentException`. Explicit zero and false values are valid.
- A null non-nullable reference is rejected. Nullable data may remain null.
- Populated data belonging to another branch is rejected by default. Pass `rejectUnusedData: false` to permit discarding it.
- Discriminator comparisons use `EqualityComparer<TKey>.Default`, including null strings and floating-point NaN.

Presence is tracked through property setters. Object initializers and ordinary deserialization work; writing directly to backing fields does not. Presence describes assignments to the current model, not the original payload before earlier serialization.

Unconstrained generic branch values use `OptionalValue<T>?`: a null wrapper means absent; a present wrapper can contain zero, false or null. Read the payload with `data.Value?.Value`. Its JSON representation is `{ "Value": ... }`.

## Included data

Auto-getter and writable properties, plus explicit public/private instance fields, participate in mapping. Static members, ordinary constants, indexers, explicit interface properties and compiler-generated backing fields do not.

Computed getter-only properties are excluded unless marked `SmartEnumSnapshot`. Use `SmartEnumIgnore` to omit state entirely. See the [attribute reference](attributes.md).

If both a writable property and its explicit backing field are included, both become data. Constructors assign fields after properties to preserve field values even when setters modify them.

Flattened names use PascalCase without underscores. Conflicting data names produce a diagnostic. Discriminator/data name conflicts use a `Key` prefix for the discriminator.

## Custom copies and additional data

Reference values are copied shallowly. Extend the flattened model with a compatible partial class and implement optional hooks on the root to customize transfers:

```csharp
partial void CustomizeFlattenedData(StatusFlattened data)
{
    // Clone mutable values or populate custom properties after flattening.
}

static partial void CustomizeFlattenedInput(ref StatusFlattened data)
{
    // Replace input with a detached copy before reconstruction.
}
```

The input hook runs after the initial null check and before transfer checks. Replace `data` to avoid mutating the caller's model. Both hooks may throw. Use `SmartEnumIgnore` for state that must never enter the transfer model.

## External base constructors

For a base class outside the hierarchy, generation selects an accessible parameterless constructor, or otherwise the sole accessible constructor. Its arguments precede root data and use `base_` prefixes.

Reverse mapping needs a readable member with the same name, ignoring case, and type. Use `SmartEnumParentData` when the readable member has a different name. Required external state must be initialized by a constructor declaring `SetsRequiredMembers`.

## Limitations

- Ordinary classes and non-positional records are supported, including types nested inside partial declarations.
- Compatible generic hierarchies can rename or reorder parameters. Closed specializations, uninferable parameters and stronger descendant constraints are diagnosed.
- File-local types, positional records and primary constructors are unsupported.
- Ambiguous external constructors, ref/in/out arguments, inaccessible types, ref-like data and conflicting generated signatures require declaration changes.
- Generated members, `OptionalValue` and the `SmartEnum` member prefix are reserved.
- Hierarchies must be in the consumer compilation. Runtime registration and unregistered proxy subclasses are unsupported; `Flatten()` rejects unknown runtime types.
- Snapshots, ignored state and custom constructor/setter behavior can prevent exact equality after a round trip.
