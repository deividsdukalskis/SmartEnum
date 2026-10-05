namespace TestProject.EventStates.EventSubtypes;

public partial class RescheduledEvent : ScheduledEvent
{
	public const string EventStatusSubtypeId = "rescheduled";
	public required DateTime RescheduledAt { get; init; }
}