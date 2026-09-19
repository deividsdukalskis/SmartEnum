namespace TestProject.EventStates;

public abstract partial class CancelledEvent : EventState
{
	public const string EventStatusId = "cancelled";

	public string CreatedName { get; set; }
}