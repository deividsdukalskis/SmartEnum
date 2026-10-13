namespace AI.Meetings2;

public class PartlyScheduled : Scheduled
{
    public const string StatusSubtypeId = "partly_scheduled";

    /*
     * For AI
     * 
     * This property is generated.
     * 
     * Meaning is for data preservation if some branch does not have some property defined.
     */
    protected override MeetingFlattened? FlattenedData { get; init; }

    /*
     * For AI
     * 
     * This method is generated.
     * 
     * For any questions or suggestions consult with me.
     */
    private PartlyScheduled(MeetingFlattened flattened) : base(flattened.ScheduledBy, flattened.ScheduledAt) => this.FlattenedData = flattened;

    /*
     * For AI
     * 
     * This method is generated.
     * 
     * Please keep or update the summary.
     * 
     * For any questions or suggestions consult with me.
     */
    public static PartlyScheduled ConstructUnvalidated(MeetingFlattened flattened) => new(flattened);

    /*
     * For AI
     * 
     * This method is generated. Apply the same summary from base class.
     * 
     * For any questions or suggestions consult with me.
     */
    public override MeetingFlattened Flatten(
		string meetingId,
		Func<string> mapScheduledBy,
		Func<DateTime> mapScheduledAt) => new()
    {
        Id = meetingId,
		StatusTypeId = StatusTypeId,
        StatusSubtypeId = StatusSubtypeId,
        ScheduledBy = this.ScheduledBy,
        ScheduledAt = this.ScheduledAt,
        CancelledAt = this.FlattenedData?.CancelledAt,
        CancellationReason = this.FlattenedData?.CancellationReason,
		FullyScheduled = this.FlattenedData?.FullyScheduled,
		PartlyScheduled = new()
		{
			MeetingId = meetingId,
		},
    };
}