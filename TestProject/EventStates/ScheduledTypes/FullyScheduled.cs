namespace TestProject.EventStates.ScheduledTypes;

using SmartEnum;

public partial class FullyScheduled : ScheduledEvent
{
	public const string ScheduledTypeId = "fully_scheduled";

	[SmartEnumPrivateAccessor(nameof(_attendees))]
	public IReadOnlyList<string> Attendees => _attendees.AsReadOnly();

	private readonly List<string> _attendees = [];
}