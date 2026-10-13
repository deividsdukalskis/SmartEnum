namespace AI;

/* 
 * For AI
 * 
 * Attribute could only be applied to classes with properties which type is in SmartEnum hierarchy
 * or properties with generic type which type argument is in SmartEnum hierarchy.
 * If property type is in SmartEnum hierarchy (not generic type which type argument is in SmartEnum hierarchy) then unique property name
 * should also be enforced just like in SmartEnum hierarchy.
 * Class to which this attribute applies, should be sealed.
 * 
 * For any questions or suggestions consult with me.
 */

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
internal sealed class SmartEnumParentAttribute : Attribute
{
	public FlatteningStrategy FlatteningStrategy { get; }

	public SmartEnumParentAttribute(FlatteningStrategy flatteningStrategy) => this.FlatteningStrategy = flatteningStrategy;
}