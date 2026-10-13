namespace AI;

/*
 * FOR AI
 * 
 * This enum is generated.
 * Please include detailed summary why this enum is neccessary and how it works.
 * If branch is missing then what to do.
 * 
 * Please ask questions if something is not clear. For any suggestions consult with me.
 */
internal enum KeyValueOnBranchMissing
{
	SetNull, // For AI: This will set the key nullable and be null when missing
	PromptSubstitute, // For AI: This will prompt a Func<"key_type"> map"key_name" when flattening from user to provide a value.
	FirstBranch // For AI: this option can only be used for first SmartEnum attribute and will show a clear error underlining the attribute if rule gets violated.
}