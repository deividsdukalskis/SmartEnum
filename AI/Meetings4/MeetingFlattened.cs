namespace AI.Meetings4;

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
		public class CancelledFlattened
		{
			public string MeetingId { get; set; } = null!;
			public string? CancellationReason { get; set; }
			public DateTime? CancelledAt { get; set; }
		}

		public class ScheduledFlattened
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

			public PartlyScheduledFlattened? PartlyScheduled { get; set; }
			public FullyScheduledFlattened? FullyScheduled { get; set; }
			public string MeetingId { get; set; } = null!;
			public string ScheduledTypeId { get; set; } = null!;
			public string ScheduledBy { get; set; } = null!;
			public DateTime ScheduledAt { get; set; }
		}

		public string MeetingId { get; set; } = null!;
		public ScheduledFlattened? Scheduled { get; set; }
		public CancelledFlattened? Cancelled { get; set; }
	}

	public string Id { get; set; } = null!;
	public MeetingStatusFlattened Status { get; set; } = null!;
	public string Title { get; set; } = null!;
	public string StatusTypeId { get; set; } = null!;
}