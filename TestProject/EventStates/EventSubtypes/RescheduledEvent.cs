namespace TestProject.EventStates.EventSubtypes;

public partial class RescheduledEvent : ScheduledEvent
{
	public const string EventStatusSubtypeId = "rescheduled";
	public string CreatedName { get; set; }
}