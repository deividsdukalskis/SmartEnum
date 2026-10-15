namespace TestProject.EventStates;

using SmartEnum;

[SmartEnum<string>("ScheduledTypeId", FlatteningStrategy.Flat, KeyValueOnBranchMissing.FirstBranch)]
public abstract partial class ScheduledEvent : EventState
{
	public const string EventStatusId = "scheduled";

	public DateTime ScheduledAt { get; private set; }
}