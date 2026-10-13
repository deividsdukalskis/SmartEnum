namespace AI.Meetings1;

/*
 * For AI
 * 
 * This class is generated.
 * 
 * For questions or suggestions consult with me.
 */
public class MeetingFlattened
{
	public class MeetingStatusFlattened
	{
		public string Id { get; set; } = null!;
		public string MeetingId { get; set; } = null!;
		public string StatusTypeId { get; set; } = null!;
		public string? StatusSubtypeId { get; set; }
		public string? CancellationReason { get; set; }
		public string ScheduledBy { get; set; } = null!;
		public DateTime ScheduledAt { get; set; }
		public DateTime? CancelledAt { get; set; }
		public List<string> Attendees { get; set; } = null!;
	}

	public string Id { get; set; } = null!;
	public List<MeetingStatusFlattened> Statuses { get; set; } = null!;
}