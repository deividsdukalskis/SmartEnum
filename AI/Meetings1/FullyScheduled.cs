namespace AI.Meetings1;

using AI;

public class FullyScheduled : Scheduled
{
    public const string StatusSubtypeId = "fully_scheduled";

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
     * Example of using the attribute. Note that attribute may also apply on non collection properties.
     */
    [SmartEnumPrivateAccessor(nameof(_attendees))]
    public IReadOnlyList<string> Attendees => _attendees.AsReadOnly();

    private readonly List<string> _attendees = new();

    /*
     * For AI
     * 
     * This method is generated.
     * 
     * For any questions or suggestions consult with me.
     */
    private FullyScheduled(MeetingFlattened.MeetingStatusFlattened flattened) : base(flattened.Id, flattened.ScheduledBy, flattened.ScheduledAt)
    {
        this.FlattenedData = flattened;
        _attendees = flattened.Attendees;
    }

    /*
     * For AI
     * 
     * This method is generated.
     * 
     * Please keep or update the summary.
     * 
     * For any questions or suggestions consult with me.
     */
    public static FullyScheduled ConstructUnvalidated(MeetingFlattened.MeetingStatusFlattened flattened) => new(flattened);

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
        Attendees = _attendees,
        CancelledAt = this.FlattenedData?.CancelledAt,
        CancellationReason = this.FlattenedData?.CancellationReason,
    };
}