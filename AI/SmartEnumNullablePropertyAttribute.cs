namespace AI;

using System;

/*
 * FOR AI
 * 
 * This attribute is generated.
 * This attribute is primarily made for properties that are not meant to be nullable inside SmartEnum class but are meant to be nullable in flattened structure.
 * If current SmartEnum level has FlatteningStrategy.Nested or FlatteningStrategy.Separate and this attribute is applied to property in that class then
 * this attribute should show an error.
 * 
 * Please ask questions if something is not clear. For any suggestions consult with me.
 */

[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
internal sealed class SmartEnumNullablePropertyAttribute : Attribute
{
    public SmartEnumNullablePropertyAttribute() { }
}