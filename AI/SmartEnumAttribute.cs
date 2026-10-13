namespace AI;

using System;

/*
 * FOR AI
 * 
 * This attribute is generated.
 * Nothing more to add here.
 * 
 * Please ask questions if something is not clear. For any suggestions consult with me.
 */
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
internal sealed class SmartEnumAttribute<TKey> : Attribute
{
    public string KeyName { get; }

    public FlatteningStrategy FlatteningStrategy { get; }

	public KeyValueOnBranchMissing KeyValueOnBranchMissing { get; }

	public SmartEnumAttribute(string keyName, FlatteningStrategy flatteningStrategy, KeyValueOnBranchMissing keyValueOnBranchMissing)
    {
        this.KeyName = keyName;
        this.FlatteningStrategy = flatteningStrategy;
		this.KeyValueOnBranchMissing = keyValueOnBranchMissing;
    }
}