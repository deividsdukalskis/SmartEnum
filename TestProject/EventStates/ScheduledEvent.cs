namespace TestProject.EventStates;

public abstract partial class ScheduledEvent : EventState
{
	public const string EventStatusId = "scheduled";

	public string CreatedName { get; set; }
}