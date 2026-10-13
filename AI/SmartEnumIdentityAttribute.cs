namespace AI;

/*
 * For AI
 * 
 * This attribute is meant to be used to identify the primary key of a table on flattening. It is optional.
 * If this attribute is applied to property named "Id", "id", "Guid", "guid", "Uuid", "uuid" or another non-unique name and no "foreignKeyName" is specified
 * or "Id", "id", "Guid", "guid", "Uuid", "uuid" or another non-unique name is passed to "foreignKeyName" then warning should be shown for attribute saying that
 * the foreign key name may conflict with other table primary key name or something similar.
 * 
 * If FlatteningStrategy.Nested is defined in that level only then on flattening the key gets created on that flattened class. Or when SmartEnumParent attribute is applied
 * then only when FlatteningStrategy.Nested is applied or SmartEnum hierarchy is referenced in generic type argument.
 * 
 * If "foreignKeyName" is null then property name to which this attribute is applied to should be used.
 * If SmartEnum hierarchy already defines a property with name same as resolved from "foreignKeyName" or SmartEnumParent has properties that reference the SmartEnum hierarchy
 * that has property same as resolved from "foreignKeyName", then error should state a conflict on flattening.
 * 
 * For any questions or suggestions consult with me.
 */
[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
internal sealed class SmartEnumIdentityAttribute : Attribute
{
	public string? ForeignKeyName { get; }

	public SmartEnumIdentityAttribute(string? foreignKeyName = null) => this.ForeignKeyName = foreignKeyName;
}