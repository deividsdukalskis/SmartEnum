namespace TestProject.EventStates.EventSubtypes;

public partial class CancelledRescheduledEvent : CancelledEvent
{
	// The same subtype key in another branch must still map to a different type.
	public const string EventStatusSubtypeId = "rescheduled";
}