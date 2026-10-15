using EFCoreTests;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

using SqliteConnection connection = new("Data Source=:memory:");
connection.Open();
DbContextOptions<PersistenceContext> options = new DbContextOptionsBuilder<PersistenceContext>().UseSqlite(connection).Options;
using (PersistenceContext context = new(options))
{
	_ = context.Database.EnsureCreated();
	if (context.Model.GetRelationalModel().Tables.Any(table => table.Name == nameof(StatusFlattened))) throw new Exception("The abstract DTO must not have a table.");
	IEntityType ready = context.Model.FindEntityType(typeof(ReadyFlattened))!;
	StoreObjectIdentifier table = StoreObjectIdentifier.Table(ready.GetTableName()!, ready.GetSchema());
	if (ready.FindProperty(nameof(StatusFlattened.Id))!.GetColumnName(table) is null || ready.FindProperty(nameof(ReadyFlattened.Count))!.IsNullable)
		throw new Exception("TPC must include inherited columns and preserve non-nullable branch values.");
	MeetingRow meeting = new() { Id = "meeting-1" };
	_ = context.Add(meeting);
	_ = context.Add(Ready.ConstructUnvalidated(new ReadyFlattened { Id = "ready-1", MeetingId = meeting.Id, Kind = "ready", Count = 42 }).Flatten());
	_ = context.Add(Cancelled.ConstructUnvalidated(new CancelledFlattened { Id = "cancelled-1", MeetingId = meeting.Id, Kind = "cancelled", Reason = "weather" }).Flatten());
	_ = context.SaveChanges();
}

using (PersistenceContext context = new(options))
{
	List<StatusFlattened> persisted = [.. context.Set<StatusFlattened>().OrderBy(item => item.Id)];
	if (persisted.Count != 2 || Status.ConstructUnvalidated(persisted[0]) is not Cancelled { Reason: "weather" }
		|| Status.ConstructUnvalidated(persisted[1]) is not Ready { Count: 42, MeetingId: "meeting-1" })
	{
		throw new Exception("Polymorphic persistence round trip failed.");
	}

	ReadyFlattened ready = context.Set<ReadyFlattened>().Single();
	ready.Count = 43;
	_ = context.SaveChanges();
}

using (PersistenceContext context = new(options))
{
	if (context.Set<ReadyFlattened>().Single().Count != 43) throw new Exception("Tracked DTO update failed.");
	_ = context.Add(new ReadyFlattened { Id = "orphan", MeetingId = "missing", Kind = "ready", Count = 1 });
	try { _ = context.SaveChanges(); throw new Exception("The concrete table's MeetingId foreign key must be enforced."); }
	catch (DbUpdateException) { }
}

Console.WriteLine("PASS EF Core SQLite TPC: separate tables, inherited data, nullability, polymorphic queries, round trips, updates, foreign keys");
CompositePersistence.Verify(); ReusablePersistence.Verify();