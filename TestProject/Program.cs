using System.Reflection;
using TestProject;
using TestProject.EventStates;
using TestProject.EventStates.EventSubtypes;

DateTime scheduledAt = new(2026, 10, 14);
RescheduledEvent direct = RescheduledEvent.ConstructUnvalidated(new RescheduledEventFlattened
{
	EventStatusId = "scheduled",
	EventStatusSubtypeId = "rescheduled",
	CreatedName = "Alice",
	ScheduledAt = scheduledAt,
	RescheduledAt = scheduledAt.AddDays(1)
});
if (direct.CreatedName != "Alice" || direct.ScheduledAt != scheduledAt) throw new Exception("Construction failed.");
EventStateFlattened flattened = direct.Flatten();
if (EventState.MapFromFlattenedDataUnvalidated(flattened) is not RescheduledEvent { CreatedName: "Alice" }) throw new Exception("Round trip failed.");
CancelledRescheduledEvent cancelled = CancelledRescheduledEvent.ConstructUnvalidated(new CancelledRescheduledEventFlattened
{
	EventStatusId = "cancelled",
	EventStatusSubtypeId = "rescheduled",
	CreatedName = "Carol",
	CancellationReason = "weather"
});
if (EventState.MapFromFlattenedDataUnvalidated(cancelled.Flatten()) is not CancelledRescheduledEvent) throw new Exception("Full-key mapping failed.");
flattened.EventStatusId = "unknown";
if (EventState.MapFromFlattenedDataUnvalidated(flattened) is not null) throw new Exception("Unknown keys must return null.");
if (typeof(RescheduledEvent).GetConstructors(BindingFlags.Instance | BindingFlags.Public).Length != 0) throw new Exception("Concrete constructors must be private.");
if (!typeof(ScheduledEvent).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic).Single().IsFamily) throw new Exception("Shared constructors must be protected.");
Console.WriteLine("PASS consumer: typed models, construction, full-key mapping, round trips and constructor visibility");