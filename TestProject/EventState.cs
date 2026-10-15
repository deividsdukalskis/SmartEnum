namespace TestProject;

using SmartEnum;

[SmartEnum<string>("EventStatusId", FlatteningStrategy.Nested, KeyValueOnBranchMissing.FirstBranch)]
public abstract partial class EventState
{
	public string? CreatedName { get; }
}