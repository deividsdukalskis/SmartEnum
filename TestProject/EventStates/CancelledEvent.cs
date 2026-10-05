namespace TestProject.EventStates;

public abstract partial class CancelledEvent : EventState
{
	public const string EventStatusId = "cancelled";

	public string? CancellationReason { get; private set; }
}