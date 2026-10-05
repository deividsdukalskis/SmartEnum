using System.Reflection;
using TestProject;
using TestProject.EventStates;
using TestProject.EventStates.EventSubtypes;

DateTime scheduledAt = new(2026, 10, 5);
DateTime rescheduledAt = scheduledAt.AddDays(1);
RescheduledEvent direct = RescheduledEvent.ConstructUnvalidated("Alice", scheduledAt, rescheduledAt);
Assert(direct.CreatedName == "Alice" && direct.ScheduledAt == scheduledAt && direct.RescheduledAt == rescheduledAt,
	"ConstructUnvalidated must initialize properties at every inheritance level.");

EventState scheduled = EventState.MapDataToTypeUnvalidated(
	EventStatusId: "scheduled", EventStatusSubtypeId: "rescheduled", CreatedName: "Bob",
	CancellationReason: null, ScheduledAt: scheduledAt, RescheduledAt: rescheduledAt);
Assert(scheduled is RescheduledEvent { CreatedName: "Bob" }, "Scheduled branch mapping failed.");
EventState cancelled = EventState.MapDataToTypeUnvalidated(
	EventStatusId: "cancelled", EventStatusSubtypeId: "rescheduled", CreatedName: "Carol",
	CancellationReason: "Weather", ScheduledAt: default, RescheduledAt: default);
Assert(cancelled is CancelledRescheduledEvent { CreatedName: "Carol", CancellationReason: "Weather" },
	"Mapping must check the parent key even when the subtype keys match.");

foreach ((string, string) keys in new[] { ("unknown", "rescheduled"), ("scheduled", "unknown"), ("cancelled", "unknown") })
{
	try
	{
		_ = EventState.MapDataToTypeUnvalidated(keys.Item1, keys.Item2, "Name", null, default, default);
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
Console.WriteLine("All consumer checks passed: initialization, full-key mapping, unknown keys, constructor visibility.");

static void Assert(bool condition, string message)
{
	if (!condition) throw new Exception(message);
}