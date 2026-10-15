# Diagnostics

[Back to README](../README.md)

All active rules are registered with the analyzer so Visual Studio can list and configure them. Errors point to user declarations, including errors discovered while validating generated layouts.

| ID | Meaning |
| --- | --- |
| SMARTENUM001 | A root or intermediate class must be abstract. |
| SMARTENUM002 | A declaration or enclosing type must be partial. |
| SMARTENUM003 | Invalid discriminator name. |
| SMARTENUM004 | Missing discriminator field. |
| SMARTENUM005 | Discriminator field must be public. |
| SMARTENUM006 | Discriminator field must be constant. |
| SMARTENUM007 | Discriminator type differs from the attribute's key type. |
| SMARTENUM009 | Duplicate discriminator names. |
| SMARTENUM010 | Abstract branch has no concrete descendant. |
| SMARTENUM011 | Transfer member names collide after normalization. |
| SMARTENUM012 | Static or file-local hierarchy declaration. |
| SMARTENUM013 | Sibling branches have the same discriminator. |
| SMARTENUM014 | A class has no unique discriminator level. |
| SMARTENUM015 | Invalid SmartEnum attribute arguments. |
| SMARTENUM016 | Warning: choose a more descriptive foreign-key name. |
| SMARTENUM017 | Parent class must be sealed. |
| SMARTENUM018 | Primary constructor requires explicit integration. |
| SMARTENUM019 | No unambiguous accessible ordinary-base constructor can restore inherited properties. Supply a compatible constructor or a protected root data constructor. |
| SMARTENUM020 | Invalid parent strategy. |
| SMARTENUM021 | Generic variant cannot be inferred. |
| SMARTENUM022 | Inaccessible hierarchy variant. |
| SMARTENUM023 | Invalid FirstBranch position. |
| SMARTENUM024 | Required member cannot be ignored. |
| SMARTENUM025 | Multiple identity properties. |
| SMARTENUM026 | Member conflicts with flattened class name. |
| SMARTENUM027 | Property requires readable storage. |
| SMARTENUM028 | Invalid private accessor. |
| SMARTENUM029 | Unsupported transfer member. |
| SMARTENUM030 | Inaccessible transfer member type. |
| SMARTENUM031 | Shared private accessor field. |
| SMARTENUM032 | Reserved generated member name. |
| SMARTENUM033 | Flattened type already exists. |
| SMARTENUM034 | Incompatible shared data constructor. |
| SMARTENUM035 | Conflicting transfer constructor. |
| SMARTENUM036 | Incompatible flattening strategies. |
| SMARTENUM037 | Discriminator conflicts with data member. |
| SMARTENUM038 | Discriminator conflicts with model name. |
| SMARTENUM039 | Duplicate nested model type. |
| SMARTENUM040 | Invalid generated property name. |
| SMARTENUM041 | Inaccessible owner model type. |
| SMARTENUM042 | Flattened layout name collision. |
| SMARTENUM043 | Construction callback name collision. |
| SMARTENUM044 | Flatten callback name collision. |
| SMARTENUM045 | Generic ownership requires Separate parent. |
| SMARTENUM046 | Shared hierarchy requires Separate parents. |
| SMARTENUM047 | Parent has no hierarchy property. |

SMARTENUM008 is retired. Applying `SmartEnum` to an abstract subclass now starts a new hierarchy.

## Visual Studio

Set **Tools → Options → Text Editor → C# → Advanced → Run background code analysis for → Entire solution** to include closed files. In Error List, include both **Build** and **IntelliSense** sources. After upgrading the package, rebuild the solution to refresh generated sources and analyzer diagnostics.

Rules support normal `.editorconfig` severity settings. Suppressing a rule does not make an invalid hierarchy generate successfully; fix the reported contract before relying on generated methods.
