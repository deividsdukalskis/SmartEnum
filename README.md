# SmartEnum

SmartEnum generates constructors and data-mapping factories for rich class
hierarchies. The analyzer and generator run at compile time; generated consumer
code has no SmartEnum runtime dependency.

## Toolchain

- Compiler host: Roslyn 5.6 or newer, tested with .NET SDK 10.0.401.
- Consumer language: C# 11 or newer (generic attributes are required).
- The generator targets .NET Standard 2.0. Required members additionally need a
  target framework providing the C# required-member support attributes.

## Declare a hierarchy

Apply one `SmartEnum<TKey>("KeyName")` attribute to the abstract root for each
inheritance level below it. Attribute order defines the key order. Keep these
attributes together in one declaration so their order is explicit. Each class
below the root declares its level's key as a `public const` field of the exact
attribute type. Roots and intermediate classes are abstract; the final level is
concrete. All hierarchy types and enclosing types must be partial.

The same subtype value may occur below different parent keys. Complete key
combinations must be unique. Names are unescaped symbol names: use `"class"`
for a discriminator declared as `@class`.

## Generated API

- Roots and intermediate types receive protected constructors.
- Concrete types receive private constructors and public static
  `ConstructUnvalidated` factories.
- A sealed partial `<RootName>Flattened` transfer class contains public get/set properties
  for hierarchy data and discriminator constants. It shares the root's namespace,
  enclosing type, accessibility and generic parameters/constraints.
- The root receives public static `MapFromFlattenedDataUnvalidated`, accepting
  one flattened model. It checks **every** discriminator in a concrete type's path
  and calls that type's factory.
- The root's public instance `Flatten()` returns a new flattened model containing
  the instance's current data and discriminator values, including private fields.
  Copies are shallow by default; partial customization hooks can clone values in
  either direction without adding a runtime serialization dependency.
- Unknown combinations throw `ArgumentException`. Comparisons use
  `EqualityComparer<TKey>.Default`, including null strings and floating-point NaN.
- Compatible explicitly written data constructors are reused. They must be
  protected on abstract types or private on concrete types. Their implementation
  remains responsible for initialization. Constructors handling required members
  must declare `SetsRequiredMembers`.
- Unrelated factory overloads are allowed; duplicate generated signatures are
  diagnosed.

Constructors accept data from the root down to the concrete type, alphabetized
within each declaring type. Each generated constructor initializes its own data
and forwards inherited data to its parent. Data consists of concrete instance
properties with a setter/init accessor or an auto getter, plus explicit public
and private instance fields. Static members, constants, compiler-generated backing
fields, computed getter-only properties, indexers and explicit interface properties
are not data.

Supported data includes private setters, getter-only auto properties, `init`,
nullable annotations, public/private readonly fields and required properties/fields.
Abstract property contracts are initialized by their concrete overrides. Property
names must be unique throughout the hierarchy except for overrides of the same
contract. Multiple incompatible data types under one name are diagnosed.

Private fields retain their visibility and use their declared names as parameters
in constructors and `ConstructUnvalidated`, and as flattened properties. Inherited
private fields are initialized by the constructor of their declaring class.
Field types must be accessible to the generated public factories, and duplicate
data names within an inheritance path are diagnosed. This adds required parameters
for existing private fields, so update factory calls when upgrading.

Apply `[SmartEnumIgnore]` to a field or property to exclude it from constructors,
factories and flattened data. This is useful for secrets, caches and transient
state. Initialize ignored members in your own code or with member initializers.
Required members cannot be ignored. Private fields that are included become public
transfer properties and may be serialized; private visibility is not a redaction rule.

If a writable property and its explicit backing field are both present, both
become parameters. Constructors assign properties first and explicit fields last
within each class, preserving the supplied field values even when a setter writes
to those fields. Compiler-generated auto-property backing fields are never exposed
as separate parameters.

The flattened model contains the union of hierarchy data. Properties not present
in every concrete branch are nullable and remain null when `Flatten()` is called
on another branch. Shared properties retain their declared types. Discriminator
properties retain their key types; conflicting names receive repeated `key_`
prefixes. Ordinary constants are excluded.

Null input throws `ArgumentNullException`. The model tracks property assignments,
so omitted discriminators and omitted non-nullable data required by the selected
branch throw `ArgumentException`, even when their default is zero or false.
Explicit zero/false values remain valid. Null non-nullable references are rejected;
nullable data may remain null. These are transfer-shape checks, not domain rules.
Discriminator nulls are valid when explicitly supplied and matched by a null key.

Populated properties from another branch are rejected to prevent silent data loss.
Pass `rejectUnusedData: false` to explicitly allow discarding them. Computed snapshot
values are informational and are not restored. Property setters track assignments
when using object initializers or ordinary deserialization; transport that fills
backing fields directly is unsupported. Presence reflects assignments to the current
model, not whether a value was originally supplied before an earlier serialization.

Computed getters remain excluded by default because they may have side effects.
Apply `[SmartEnumSnapshot]` to copy a computed property's current value into the
flattened model. Restoration recomputes it from actual constructor data and ignores
the supplied snapshot. Write-only data still cannot be flattened.

`MapFromFlattenedDataUnvalidated` is the preferred API. A hidden-from-IntelliSense
`MapDataToTypeUnvalidated` compatibility adapter retains the original argument
list and permits unused branch data. It delegates to the new mapper, including its
null checks. An existing user-defined method with that signature is preserved.
`ConstructUnvalidated` retains its parameters except for explicitly ignored data.

No domain validation is added. Custom setters and reused constructors execute
normally and can perform their own validation or throw exceptions.

```csharp
var data = new EventStateFlattened
{
    EventStatusId = "cancelled",
    EventStatusSubtypeId = "rescheduled",
    CreatedName = "Carol",
    CancellationReason = "Weather"
};
EventState state = EventState.MapFromFlattenedDataUnvalidated(data);
// CancelledRescheduledEvent, even though RescheduledEvent shares the subtype key.
EventStateFlattened flattened = state.Flatten();
// ScheduledAt and RescheduledAt are null for this branch.
EventState restored = EventState.MapFromFlattenedDataUnvalidated(flattened);
```

## Copy and transfer customization

Extend the generated flattened class with a compatible partial class to add
application-specific properties or methods. Generated property names are reserved.
Implement either optional hook in the root's partial declaration:

```csharp
partial void CustomizeFlattenedData(EventStateFlattened data)
{
    // Clone mutable values, populate custom properties, or redact optional data.
}

static partial void CustomizeFlattenedInput(ref EventStateFlattened data)
{
    // Replace data with a detached copy before restoration, if needed.
}
```

The output hook runs after the hierarchy is read. The input hook runs after the
initial null check and before transfer checks and construction. To avoid mutating
the caller's transfer object, replace `data` with a new model in the input hook.
Both hooks may throw. They let applications define deep-copy behavior for cycles,
shared references and custom object types; no automatic universal clone is attempted.
Use `[SmartEnumIgnore]` rather than redaction when state must never enter the model.

## Generics, records and parent constructors

Non-positional record classes and ordinary classes are supported, including
nested types inside partial classes, structs, records and interfaces. Generic
parameters may be renamed or reordered down the hierarchy; the mapper infers the
concrete type arguments from the root. Generic containers and matching generic
constraints are supported. Example: `Leaf<U> : Root<U>` generates factories for
`Root<T>` that construct `Leaf<T>`.

Unconstrained generic branch values are supported using the nested nullable
`OptionalValue<T>?` transfer type. A null wrapper means the branch value is absent;
a present wrapper can hold zero, false, or a nullable reference. Values convert
implicitly when assigning the property (`data.Value = 42`); read the payload with
`data.Value?.Value`. Supply a wrapper explicitly to represent present-null data.
Constrained reference/value types keep their ordinary nullable property types.
This changes the JSON shape of unconstrained branch values to `{ "Value": ... }`.

For a parent outside the SmartEnum hierarchy, generation prefers an accessible
parameterless constructor. Otherwise it forwards arguments to the sole accessible
constructor. These arguments appear before root data and receive `base_` prefixes.
For reverse mapping, each argument needs an accessible readable instance field
or property of the same name (ignoring case) and type on the external parent.
`Flatten()` reads its current value, not the original constructor input. Arguments
without readable state are diagnosed. Add
`[SmartEnumParentData("constructorParameter", "ReadableMember")]` to the root to
use a differently named member, including a private computed projection on the
root. Explicit names are case-sensitive and their types must match the constructor
parameter. Required external
state must be initialized by a constructor declaring `SetsRequiredMembers`.

## Remaining boundaries

`SMARTENUM012` explains declarations that cannot be generated safely:

- File-local types cannot be extended from another generated source file.
- Positional records and primary constructors conflict with the generated
  constructor contract; use ordinary partial declarations and explicit data.
- Closed generic specializations (for example `Leaf : Root<int>`), additional
  uninferable type parameters, and stronger descendant constraints cannot be
  constructed for every instantiation of an open root. Use a uniform generic
  hierarchy or separate non-generic roots.
- Ambiguous external constructor overloads, ref/in/out parent arguments,
  inaccessible data types, unsafe/ref-like data and conflicting generated
  signatures require explicit model changes.
- Custom flattened declarations must be compatible partial classes and cannot
  redefine generated properties. `OptionalValue` and the `__SmartEnum` prefix
  are reserved for generated state.

Hierarchies must be declared in the consumer compilation. A generator cannot
add a mapper to a root already compiled into another assembly. The mapper is a
closed view of the concrete types present in the compilation; there is no runtime
registration of additional types. Persist the explicit key values, and treat
changes to them as domain schema changes.

`Flatten()` rejects runtime types outside that generated set, including unregistered
ORM proxy subclasses, instead of silently dropping their additional state. Custom
getters, setters and constructors still execute. Snapshot values, ignored state and
constructor transformations mean that exact object identity or equality after a
round trip is not guaranteed.

## Diagnostics

To see all SmartEnum errors and warnings in Visual Studio, including those in
closed files, set the solution/background analysis scope to **Entire solution**.
Open **Tools → Options → Text Editor → C# → Advanced** and set
**Run background code analysis for** to **Entire solution**.
This enables live diagnostics across the solution without opening each file.
See [Visual Studio live code analysis settings](https://learn.microsoft.com/en-us/visualstudio/code-quality/configure-live-code-analysis-scope-managed-code).

| ID | Meaning |
| --- | --- |
| SMARTENUM001–002 | Incorrect abstract/partial declaration |
| SMARTENUM003–007 | Invalid key name, missing field, visibility, constness or type |
| SMARTENUM008–009 | Repeated attributes or discriminator names |
| SMARTENUM010 | Final type must be concrete |
| SMARTENUM011 | Duplicate data property names |
| SMARTENUM012 | Unsupported or ambiguous generation contract |
| SMARTENUM013 | Duplicate full key combination |
| SMARTENUM014 | Inheritance exceeds the declared key levels |

## Verify and package

For private publishing and installation, see [GitHub Packages setup](docs/github-packages.md).

```powershell
./Verify.ps1
```

The script restores, checks `.editorconfig`, builds Release with warnings treated
as errors, runs the regression and consumer executables, packs the analyzer, and
builds/runs an isolated consumer of the resulting NuGet package. Nothing is
published. Temporary output stays under `bin/verification`.

The regression runner compiles and emits valid cases, rejects warnings in
generated code, executes runtime probes, checks diagnostics for invalid cases,
and tests repeat generation, cancellation, source removal, incremental edits,
reverts and concurrent compilations. It includes key type boundaries and a
64-leaf hierarchy exercising all 100 combinations of valid/invalid parent and
child inputs. Tests are executable checks, not a `dotnet test` adapter.

Verification is evidence for the supported contracts, not a proof covering every
possible C# program. Review remaining boundaries and your own domain/ORM behavior
before release; no package has been published by this repository workflow.
