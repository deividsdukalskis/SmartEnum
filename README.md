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
- The root receives public static `MapDataToTypeUnvalidated`. It checks **every**
  discriminator in a concrete type's path and calls that type's factory.
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
in constructors, `ConstructUnvalidated` and `MapDataToTypeUnvalidated`. Inherited
private fields are initialized by the constructor of their declaring class.
Field types must be accessible to the generated public factories, and duplicate
data names within an inheritance path are diagnosed. This adds required parameters
for existing private fields, so update factory calls when upgrading.

If a writable property and its explicit backing field are both present, both
become parameters. Constructors assign properties first and explicit fields last
within each class, preserving the supplied field values even when a setter writes
to those fields. Compiler-generated auto-property backing fields are never exposed
as separate parameters.

The mapper accepts keys in attribute order, followed by the union of hierarchy
data. **Use named arguments**: ordering across branches is deterministic, but
adding a branch can change the positional signature. Every argument is required,
including data used by other branches; only the selected branch's data is
forwarded. Conflicting key parameter names receive repeated `key_` prefixes.

No domain validation is added. Custom setters and reused constructors execute
normally and can perform their own validation or throw exceptions.

```csharp
var state = EventState.MapDataToTypeUnvalidated(
    EventStatusId: "cancelled",
    EventStatusSubtypeId: "rescheduled",
    CreatedName: "Carol",
    CancellationReason: "Weather",
    ScheduledAt: default,
    RescheduledAt: default);
// CancelledRescheduledEvent, even though RescheduledEvent shares the subtype key.
```

## Generics, records and parent constructors

Non-positional record classes and ordinary classes are supported, including
nested types inside partial classes, structs, records and interfaces. Generic
parameters may be renamed or reordered down the hierarchy; the mapper infers the
concrete type arguments from the root. Generic containers and matching generic
constraints are supported. Example: `Leaf<U> : Root<U>` generates factories for
`Root<T>` that construct `Leaf<T>`.

For a parent outside the SmartEnum hierarchy, generation prefers an accessible
parameterless constructor. Otherwise it forwards arguments to the sole accessible
constructor. These arguments appear before root data and receive `base_` prefixes.
External parent state remains that parent's responsibility. Required external
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

Hierarchies must be declared in the consumer compilation. A generator cannot
add a mapper to a root already compiled into another assembly. The mapper is a
closed view of the concrete types present in the compilation; there is no runtime
registration of additional types. Persist the explicit key values, and treat
changes to them as domain schema changes.

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
