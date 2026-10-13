namespace EFCoreTests;

using Microsoft.EntityFrameworkCore;
using SmartEnum;

[SmartEnum<string>("Kind", FlatteningStrategy.Separate, KeyValueOnBranchMissing.FirstBranch)]
public abstract partial class Status
{
	[SmartEnumIdentity("StatusId")]
	public string Id { get; }
	public string MeetingId { get; }
}

public sealed partial class Ready : Status
{
	public const string Kind = "ready";
	public int Count { get; }
}

public sealed partial class Cancelled : Status
{
	public const string Kind = "cancelled";
	public string? Reason { get; }
}

public sealed class MeetingRow
{
	public string Id { get; set; } = null!;
}

public sealed class PersistenceContext(DbContextOptions<PersistenceContext> options) : DbContext(options)
{
	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		_ = modelBuilder.Entity<MeetingRow>();
		_ = modelBuilder.Entity<StatusFlattened>().UseTpcMappingStrategy();
		_ = modelBuilder.Entity<StatusFlattened>().HasKey(item => item.Id);
		_ = modelBuilder.Entity<ReadyFlattened>().ToTable("ReadyStatuses");
		_ = modelBuilder.Entity<CancelledFlattened>().ToTable("CancelledStatuses");
		_ = modelBuilder.Entity<StatusFlattened>().HasOne<MeetingRow>().WithMany().HasForeignKey(item => item.MeetingId);
	}
}