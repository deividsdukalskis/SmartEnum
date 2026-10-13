namespace AI.Meetings2;

/*
 * For AI
 * 
 * This class is generated.
 * 
 * For questions or suggestions consult with me.
 */
public class MeetingFlattened
{
	public class FullyScheduledFlattened
	{
		public string MeetingId { get; set; } = null!;
		public List<string> Attendees { get; set; } = null!;
	}

	public class PartlyScheduledFlattened
	{
		public string MeetingId { get; set; } = null!;
	}

	public FullyScheduledFlattened? FullyScheduled { get; set; }
	public PartlyScheduledFlattened? PartlyScheduled { get; set; }
	public string Id { get; set; } = null!;
	public string Title { get; set; } = null!;
	public string StatusTypeId { get; set; } = null!;
	public string? StatusSubtypeId { get; set; }
	public string? CancellationReason { get; set; }
	public string ScheduledBy { get; set; } = null!;
	public DateTime ScheduledAt { get; set; }
	public DateTime? CancelledAt { get; set; }
}