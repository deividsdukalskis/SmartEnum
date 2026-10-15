namespace TestProject.EventStates;

public partial class CancelledEvent : EventState
{
	public const string EventStatusId = "cancelled";

	public string? CancellationReason { get; private set; }

	public DateTime CancelledAt { get; private set; }
}