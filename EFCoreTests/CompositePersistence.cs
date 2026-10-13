namespace EFCoreTests;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SmartEnum;

[SmartEnumParent(FlatteningStrategy.Flat)]
public sealed partial class FlatMeeting
{
	[SmartEnumIdentity("MeetingId")]
	public string Id { get; }
	public FlatStatus Status { get; }
}

[SmartEnum<int>("Kind", FlatteningStrategy.Flat, KeyValueOnBranchMissing.FirstBranch)]
public abstract partial class FlatStatus { }

public sealed partial class FlatReady : FlatStatus
{
	public const int Kind = 1;
	public int Count { get; }
}

public sealed partial class FlatCancelled : FlatStatus
{
	public const int Kind = 2;
	public string? Reason { get; }
}

[SmartEnumParent(FlatteningStrategy.Nested)]
public sealed partial class NestedMeeting
{
	[SmartEnumIdentity("MeetingId")]
	public string Id { get; }
	public NestedStatus Status { get; }
}

[SmartEnum<int>("Kind", FlatteningStrategy.Nested, KeyValueOnBranchMissing.FirstBranch)]
public abstract partial class NestedStatus { }

public sealed partial class NestedReady : NestedStatus
{
	public const int Kind = 1;
	public int Count { get; }
}

public sealed partial class NestedCancelled : NestedStatus
{
	public const int Kind = 2;
	public string? Reason { get; }
}

public sealed class CompositeContext(DbContextOptions<CompositeContext> options) : DbContext(options)
{
	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		_ = modelBuilder.Entity<FlatMeetingFlattened>().HasKey(item => item.Id);
		_ = modelBuilder.Entity<NestedMeetingFlattened>().HasKey(item => item.Id);
		_ = modelBuilder.Entity<NestedMeetingFlattened>().OwnsOne(item => item.Status, status =>
		{
			_ = status.ToTable("NestedStatuses");
			_ = status.WithOwner().HasForeignKey(item => item.MeetingId);
			_ = status.HasKey(item => item.MeetingId);
			_ = status.OwnsOne(item => item.NestedReady, ready =>
			{
				_ = ready.ToTable("NestedReady");
				_ = ready.WithOwner().HasForeignKey(item => item.MeetingId);
				_ = ready.HasKey(item => item.MeetingId);
			});
			_ = status.OwnsOne(item => item.NestedCancelled, cancelled =>
			{
				_ = cancelled.ToTable("NestedCancelled");
				_ = cancelled.WithOwner().HasForeignKey(item => item.MeetingId);
				_ = cancelled.HasKey(item => item.MeetingId);
			});
		});
	}
}

internal static class CompositePersistence
{
	public static void Verify()
	{
		using SqliteConnection connection = new("Data Source=:memory:");
		connection.Open();
		DbContextOptions<CompositeContext> options = new DbContextOptionsBuilder<CompositeContext>().UseSqlite(connection).Options;
		using (CompositeContext context = new(options))
		{
			_ = context.Database.EnsureCreated();
			FlatMeeting flat = FlatMeeting.ConstructUnvalidated(new FlatMeetingFlattened { Id = "flat", Kind = 2, Count = 42, Reason = "weather" }, () => throw new Exception());
			context.Add(flat.Flatten(() => throw new Exception("Retained count must avoid fallback.")));
			NestedMeeting nested = NestedMeeting.ConstructUnvalidated(new NestedMeetingFlattened
			{
				Id = "nested",
				Kind = 1,
				Status = new() { NestedReady = new() { Count = 7 }, NestedCancelled = new() { MeetingId = "nested", Reason = "retained" } }
			}, () => 0, () => throw new Exception());
			context.Add(nested.Flatten());
			_ = context.SaveChanges();
		}

		using (CompositeContext context = new(options))
		{
			FlatMeetingFlattened flat = context.Set<FlatMeetingFlattened>().Single();
			if (flat.Count != 42 || flat.Reason != "weather" || FlatMeeting.ConstructUnvalidated(flat, () => throw new Exception()).Status is not FlatCancelled)
				throw new Exception("Flat persistence round trip failed.");
			NestedMeetingFlattened nested = context.Set<NestedMeetingFlattened>().Single();
			if (nested.Status?.NestedReady?.Count != 7 || nested.Status.NestedCancelled?.Reason != "retained" || nested.Status.MeetingId != "nested")
				throw new Exception("Nested persistence must retain active and inactive branches and their foreign keys.");
			NestedMeeting domain = NestedMeeting.ConstructUnvalidated(nested, () => throw new Exception(), () => throw new Exception());
			if (domain.Status is not NestedReady { Count: 7 }) throw new Exception("Nested domain reconstruction failed.");
			nested.Status.NestedReady.Count = 8;
			_ = context.SaveChanges();
		}

		using (CompositeContext context = new(options))
		{
			if (context.Set<NestedMeetingFlattened>().Single().Status?.NestedReady?.Count != 8) throw new Exception("Nested tracked update failed.");
		}

		Console.WriteLine("PASS EF Core SQLite Flat/Nested: parent round trips, owned tables, retained inactive branches, foreign keys, updates");
	}
}