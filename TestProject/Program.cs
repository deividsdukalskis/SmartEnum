using System.Reflection;
using TestProject;
using TestProject.EventStates;
using TestProject.EventStates.EventSubtypes;

DateTime scheduledAt = new(2026, 10, 5);
DateTime rescheduledAt = scheduledAt.AddDays(1);
RescheduledEvent direct = RescheduledEvent.ConstructUnvalidated("Alice", scheduledAt, rescheduledAt);
Assert(direct.CreatedName == "Alice" && direct.ScheduledAt == scheduledAt && direct.RescheduledAt == rescheduledAt,
	"ConstructUnvalidated must initialize properties at every inheritance level.");

EventState scheduled = EventState.MapFromFlattenedDataUnvalidated(new EventStateFlattened
{
	EventStatusId = "scheduled",
	EventStatusSubtypeId = "rescheduled",
	CreatedName = "Bob",
	ScheduledAt = scheduledAt,
	RescheduledAt = rescheduledAt
});
Assert(scheduled is RescheduledEvent { CreatedName: "Bob" }, "Scheduled branch mapping failed.");
EventState cancelled = EventState.MapFromFlattenedDataUnvalidated(new EventStateFlattened
{
	EventStatusId = "cancelled",
	EventStatusSubtypeId = "rescheduled",
	CreatedName = "Carol",
	CancellationReason = "Weather"
});
Assert(cancelled is CancelledRescheduledEvent { CreatedName: "Carol", CancellationReason: "Weather" },
	"Mapping must check the parent key even when the subtype keys match.");

foreach ((string, string) keys in new[] { ("unknown", "rescheduled"), ("scheduled", "unknown"), ("cancelled", "unknown") })
{
	try
	{
		_ = EventState.MapFromFlattenedDataUnvalidated(new EventStateFlattened { EventStatusId = keys.Item1, EventStatusSubtypeId = keys.Item2, CreatedName = "Name" });
		throw new Exception("Unknown keys must be rejected.");
	}
	catch (ArgumentException) { }
}

Assert(typeof(RescheduledEvent).GetConstructors(BindingFlags.Instance | BindingFlags.Public).Length == 0,
	"Concrete constructors must be private.");
Assert(typeof(ScheduledEvent).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic).Single().IsFamily,
	"Intermediate constructors must be protected.");
Assert(typeof(EventState).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic).Single().IsFamily,
	"Base constructors must be protected.");
EventStateFlattened flattened = scheduled.Flatten();
Assert(flattened.EventStatusId == "scheduled" && flattened.EventStatusSubtypeId == "rescheduled"
	&& flattened.CreatedName == "Bob" && flattened.ScheduledAt == scheduledAt && flattened.RescheduledAt == rescheduledAt
	&& flattened.CancellationReason is null, "Flatten must include the selected branch's state and keys.");
Assert(EventState.MapFromFlattenedDataUnvalidated(flattened) is RescheduledEvent { CreatedName: "Bob" }, "Round trip failed.");
Assert(cancelled.Flatten().ScheduledAt is null && cancelled.Flatten().RescheduledAt is null, "Other branch data must be null.");
Console.WriteLine("All consumer checks passed: initialization, flattened round trips, full-key mapping, unknown keys, constructor visibility.");

static void Assert(bool condition, string message)
{
	if (!condition) throw new Exception(message);
}