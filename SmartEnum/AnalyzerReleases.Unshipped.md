; Public diagnostics planned for the next release.
; SMARTENUM008 retired: subclasses may start independent hierarchy groups.

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
SMARTENUM001 | SmartEnum | Error | Hierarchy roots and intermediate classes must be abstract
SMARTENUM002 | SmartEnum | Error | Hierarchy classes must be partial
SMARTENUM003 | SmartEnum | Error | Key names must be valid identifiers
SMARTENUM004 | SmartEnum | Error | Each level must declare its key
SMARTENUM005 | SmartEnum | Error | Key fields must be public
SMARTENUM006 | SmartEnum | Error | Key fields must be constant
SMARTENUM007 | SmartEnum | Error | Key field types must match the attribute
SMARTENUM009 | SmartEnum | Error | Key names must be unique
SMARTENUM010 | SmartEnum | Error | Abstract branches require concrete descendants
SMARTENUM011 | SmartEnum | Error | Normalized transfer property names must be unique
SMARTENUM012 | SmartEnum | Error | Declaration cannot be generated safely
SMARTENUM013 | SmartEnum | Error | Complete key combinations must be unique
SMARTENUM014 | SmartEnum | Error | Hierarchy depth must match the key levels
SMARTENUM015 | SmartEnum | Error | Invalid explicit flattening contract
SMARTENUM016 | SmartEnum | Warning | Ambiguous flattened foreign key name
SMARTENUM017 | SmartEnum | Error | Parent class must be sealed
SMARTENUM018 | SmartEnum | Error | Primary constructor requires explicit integration
SMARTENUM019 | SmartEnum | Error | Ordinary base constructor cannot restore inherited properties
SMARTENUM020 | SmartEnum | Error | Invalid parent strategy
SMARTENUM021 | SmartEnum | Error | Generic variant cannot be inferred
SMARTENUM022 | SmartEnum | Error | Inaccessible hierarchy variant
SMARTENUM023 | SmartEnum | Error | Invalid FirstBranch position
SMARTENUM024 | SmartEnum | Error | Required member cannot be ignored
SMARTENUM025 | SmartEnum | Error | Multiple identity properties
SMARTENUM026 | SmartEnum | Error | Member conflicts with flattened class name
SMARTENUM027 | SmartEnum | Error | Property requires readable storage
SMARTENUM028 | SmartEnum | Error | Invalid private accessor
SMARTENUM029 | SmartEnum | Error | Unsupported transfer member
SMARTENUM030 | SmartEnum | Error | Inaccessible transfer member type
SMARTENUM031 | SmartEnum | Error | Shared private accessor field
SMARTENUM032 | SmartEnum | Error | Reserved generated member name
SMARTENUM033 | SmartEnum | Error | Flattened type already exists
SMARTENUM034 | SmartEnum | Error | Incompatible shared data constructor
SMARTENUM035 | SmartEnum | Error | Conflicting transfer constructor
SMARTENUM036 | SmartEnum | Error | Incompatible flattening strategies
SMARTENUM037 | SmartEnum | Error | Discriminator conflicts with data member
SMARTENUM038 | SmartEnum | Error | Discriminator conflicts with model name
SMARTENUM039 | SmartEnum | Error | Duplicate nested model type
SMARTENUM040 | SmartEnum | Error | Invalid generated property name
SMARTENUM041 | SmartEnum | Error | Inaccessible owner model type
SMARTENUM042 | SmartEnum | Error | Flattened layout name collision
SMARTENUM043 | SmartEnum | Error | Construction callback name collision
SMARTENUM044 | SmartEnum | Error | Flatten callback name collision
SMARTENUM045 | SmartEnum | Error | Generic ownership requires Separate parent
SMARTENUM046 | SmartEnum | Error | Shared hierarchy requires Separate parents
SMARTENUM047 | SmartEnum | Error | Parent has no hierarchy property
