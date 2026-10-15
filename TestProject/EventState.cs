namespace TestProject;

using SmartEnum;

[SmartEnum<string>("EventStatusId", FlatteningStrategy.Separate, KeyValueOnBranchMissing.FirstBranch)]
[SmartEnum<string>("EventStatusSubtypeId", FlatteningStrategy.Separate, KeyValueOnBranchMissing.SetNull)]
public abstract partial class EventState
{
	public string CreatedName { get; }
}