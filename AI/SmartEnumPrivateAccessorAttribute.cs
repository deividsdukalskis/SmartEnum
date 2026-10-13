namespace AI;

using System;

/*
 * FOR AI
 * 
 * This attribute is generated.
 * 
 * This attribute is primarily made for properties that are holding a collection and do not want direct outside class mutability but are using
 * private backing collection field to operate on that from the class. Problem is that I do want the flattened data structure to show the property name
 * not the field name and without this attribute I cannot safely and surely correlate the field to the public property.
 * 
 * Attribute must be enforced to be applied to any property inside SmartEnum hierarchy that has no setter for example "public int Weight => 2".
 * That is because those properties cannot be modified elsewhere. Entire property must be underlined and error must be shown if attribute is ommited in this scenario.
 * Error also must be shown and attribute should be underlined if field is not defined by the name specified in "accessorFieldName". For checking if
 * "public int Weight => _weight" correlation is actually being done you can implement some enforcement and also show some error. But the decision is on your side.
 * 
 * On flattened structure the property name should match the property name from SmartEnum but type must be taken from the field. Mapping should be done as in example.
 * 
 * Please ask questions if something is not clear. For any suggestions consult with me.
 */

[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
internal sealed class SmartEnumPrivateAccessorAttribute : Attribute
{
    public string AccessorFieldName { get; }

    public SmartEnumPrivateAccessorAttribute(string accessorFieldName) => this.AccessorFieldName = accessorFieldName;
}