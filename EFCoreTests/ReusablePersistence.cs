namespace EFCoreTests;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SmartEnum;

[SmartEnum<int>("Kind", FlatteningStrategy.Flat, KeyValueOnBranchMissing.FirstBranch)]
public abstract partial class ReusableState<T> { public T Value { get; } }
public sealed partial class ReusableValue<T> : ReusableState<T> { public const int Kind = 1; }

[SmartEnumParent(FlatteningStrategy.Separate)]
public sealed partial class ReusableOwner<T> : ReusableOwnerBase
{
	public ReusableState<T> First { get; }
	public ReusableState<T> Second { get; }
}

public class ReusableOwnerBase
{
	public int Id { get; }
	protected ReusableOwnerBase(int id) => this.Id = id;
}

internal sealed class ReusableContext(DbContextOptions<ReusableContext> options) : DbContext(options)
{
	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		_ = modelBuilder.Entity<ReusableOwnerFlattened<string>>().HasKey(value => value.Id);
		_ = modelBuilder.Entity<ReusableOwnerFlattened<string>>().OwnsOne(value => value.First);
		_ = modelBuilder.Entity<ReusableOwnerFlattened<string>>().OwnsOne(value => value.Second);
	}
}

internal static class ReusablePersistence
{
	public static void Verify()
	{
		using SqliteConnection connection = new("Data Source=:memory:");
		connection.Open();
		DbContextOptions<ReusableContext> options = new DbContextOptionsBuilder<ReusableContext>().UseSqlite(connection).Options;
		using (ReusableContext context = new(options))
		{
			_ = context.Database.EnsureCreated();
			ReusableOwner<string> owner = ReusableOwner<string>.ConstructUnvalidated(new ReusableOwnerFlattened<string>
			{
				Id = 1,
				First = new() { Kind = 1, Value = "first" },
				Second = new() { Kind = 1, Value = "second" }
			}, () => throw new Exception(), () => throw new Exception());
			_ = context.Add(owner.Flatten(value => value.Flatten(), value => value.Flatten()));
			_ = context.SaveChanges();
		}

		using (ReusableContext context = new(options))
		{
			ReusableOwnerFlattened<string> stored = context.Set<ReusableOwnerFlattened<string>>().Single();
			ReusableOwner<string> owner = ReusableOwner<string>.ConstructUnvalidated(stored, () => throw new Exception(), () => throw new Exception());
			if (owner.First.Value != "first" || owner.Second.Value != "second") throw new Exception("Reusable generic property round trip failed.");
			stored.Second.Value = "updated";
			_ = context.SaveChanges();
		}

		using (ReusableContext context = new(options))
		{
			ReusableOwnerFlattened<string> stored = context.Set<ReusableOwnerFlattened<string>>().Single();
			if (stored.First.Value != "first" || stored.Second.Value != "updated") throw new Exception("Independent property update failed.");
		}

		Console.WriteLine("PASS EF Core SQLite: generic parent, repeated generic hierarchy, independent owned values and updates");
	}
}