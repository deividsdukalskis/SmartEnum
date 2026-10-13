namespace AI.Meetings1;

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
    protected override MeetingFlattened.MeetingStatusFlattened? FlattenedData { get; init; }

    /*
     * For AI
     * 
     * This method is generated.
     * 
     * For any questions or suggestions consult with me.
     */
    private PartlyScheduled(MeetingFlattened.MeetingStatusFlattened flattened) : base(flattened.Id, flattened.ScheduledBy, flattened.ScheduledAt) => this.FlattenedData = flattened;

    /*
     * For AI
     * 
     * This method is generated.
     * 
     * Please keep or update the summary.
     * 
     * For any questions or suggestions consult with me.
     */
    public static PartlyScheduled ConstructUnvalidated(MeetingFlattened.MeetingStatusFlattened flattened) => new(flattened);

    /*
     * For AI
     * 
     * This method is generated. Apply the same summary from base class.
     * 
     * For any questions or suggestions consult with me.
     */
    public override MeetingFlattened.MeetingStatusFlattened Flatten(
		string meetingId,
		Func<string> mapScheduledBy,
		Func<List<string>> mapAttendees,
		Func<DateTime> mapScheduledAt) => new()
    {
        Id = this.Id,
		MeetingId = meetingId,
		StatusTypeId = StatusTypeId,
        StatusSubtypeId = StatusSubtypeId,
        ScheduledBy = this.ScheduledBy,
        ScheduledAt = this.ScheduledAt,
        Attendees = this.FlattenedData?.Attendees ?? mapAttendees(),
        CancelledAt = this.FlattenedData?.CancelledAt,
        CancellationReason = this.FlattenedData?.CancellationReason,
    };
}