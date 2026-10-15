using System.Reflection;
using TestProject;
using TestProject.EventStates;
using TestProject.EventStates.ScheduledTypes;

DateTime scheduledAt = new(2026, 10, 15);
EventFlattened scheduledData = new()
{
	Id = "event-1",
	Points = [3, 7],
	EventStatusId = ScheduledEvent.EventStatusId,
	CreatedName = "Alice",
	ScheduledEvent = new()
	{
		ScheduledTypeId = FullyScheduled.ScheduledTypeId,
		ScheduledAt = scheduledAt,
		Attendees = ["Alice", "Bob"]
	}
};
Event scheduled = Event.ConstructUnvalidated(scheduledData,
	mapCancelledAt: Unexpected<DateTime>, mapScheduledAt: Unexpected<DateTime>,
	mapAttendees: Unexpected<List<string>>, mapState: Unexpected<EventState>);
if (scheduled.State is not FullyScheduled fully || fully.ScheduledAt != scheduledAt
	|| !fully.Attendees.SequenceEqual(["Alice", "Bob"]) || !scheduled.Points.SequenceEqual([3, 7]))
{
	throw new Exception("Scheduled construction or inherited data mapping failed.");
}

EventFlattened flattened = scheduled.Flatten();
if (flattened.Id != "event-1" || flattened.ScheduledEvent?.EventId != "event-1" || !flattened.Points.SequenceEqual([3, 7]))
	throw new Exception("Owner identity or inherited Points round trip failed.");
if (EventState.ConstructUnvalidated(flattened, Unexpected<DateTime>, Unexpected<DateTime>, Unexpected<List<string>>)
	is not FullyScheduled { CreatedName: "Alice" })
{
	throw new Exception("Hierarchy round trip failed.");
}

ScheduledEventFlattened hierarchyData = fully.FlattenHierarchy(scheduled.Id,
	mapAttendees: Unexpected<List<string>>, mapId: Unexpected<string>, mapPoints: Unexpected<List<int>>);
if (ScheduledEvent.ConstructUnvalidated(hierarchyData, Unexpected<DateTime>, Unexpected<List<string>>)
	is not FullyScheduled { ScheduledAt: var restoredAt } || restoredAt != scheduledAt)
{
	throw new Exception("Scheduled subclass hierarchy round trip failed.");
}

Event partial = Event.ConstructUnvalidated(new EventFlattened
{
	Id = "event-2",
	Points = [5],
	EventStatusId = ScheduledEvent.EventStatusId,
	ScheduledEvent = new() { ScheduledTypeId = PartiallyScheduled.ScheduledTypeId, ScheduledAt = scheduledAt }
}, Unexpected<DateTime>, Unexpected<DateTime>, Unexpected<List<string>>, Unexpected<EventState>);
if (partial.State is not PartiallyScheduled || partial.Flatten().ScheduledEvent?.ScheduledTypeId != PartiallyScheduled.ScheduledTypeId)
	throw new Exception("Partially scheduled mapping failed.");

DateTime cancelledAt = scheduledAt.AddHours(1);
CancelledEvent cancelled = CancelledEvent.ConstructUnvalidated(new EventFlattened
{
	EventStatusId = CancelledEvent.EventStatusId,
	CreatedName = "Carol",
	CancelledEvent = new() { CancellationReason = "weather", CancelledAt = cancelledAt }
}, mapCancelledAt: Unexpected<DateTime>);
if (EventState.ConstructUnvalidated(cancelled.Flatten("event-3"), Unexpected<DateTime>, Unexpected<DateTime>, Unexpected<List<string>>)
	is not CancelledEvent { CancellationReason: "weather", CancelledAt: var restoredCancellation } || restoredCancellation != cancelledAt)
{
	throw new Exception("Cancelled event round trip failed.");
}

EventFlattened unknown = new() { Id = "event-4", Points = [], EventStatusId = "unknown" };
if (EventState.ConstructUnvalidated(unknown, Unexpected<DateTime>, Unexpected<DateTime>, Unexpected<List<string>>) is not null)
	throw new Exception("Unknown discriminators must return null at the hierarchy root.");
int fallbackCalls = 0;
Event recovered = Event.ConstructUnvalidated(unknown, Unexpected<DateTime>, Unexpected<DateTime>, Unexpected<List<string>>,
	mapState: () => { fallbackCalls++; return cancelled; });
if (fallbackCalls != 1 || !ReferenceEquals(recovered.State, cancelled)) throw new Exception("Required-property fallback failed.");

if (typeof(FullyScheduled).GetConstructors(BindingFlags.Instance | BindingFlags.Public).Length != 0)
	throw new Exception("Generated concrete constructors must be private.");
if (!typeof(ScheduledEvent).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic).Single().IsFamily)
	throw new Exception("Generated shared constructors must be protected.");
Console.WriteLine("PASS consumer: current event variants, inherited Points, private accessor, subclass and parent round trips, required fallback and constructor visibility");

static T Unexpected<T>() => throw new InvalidOperationException("A fallback was invoked despite complete valid data.");