namespace AI.Meetings2;

using AI;

public class Cancelled : MeetingStatus
{
    public const string StatusTypeId = "cancelled";

    public string? CancellationReason { get; }

    [SmartEnumNullableProperty]
    public DateTime CancelledAt { get; }

	/*
     * For AI
     * 
     * This property is generated.
     * 
     * For any questions or suggestions consult with me.
     */
	protected override MeetingFlattened? FlattenedData { get; init; }

    /*
     * For AI
     * 
     * This method is generated.
     * 
     * For any questions or suggestions consult with me.
     */
    private Cancelled(MeetingFlattened flattened, Func<DateTime> mapCancelledAt)
    {
        this.FlattenedData = flattened;
        this.CancellationReason = flattened.CancellationReason;
        this.CancelledAt = flattened.CancelledAt ?? mapCancelledAt();
    }

    /*
     * For AI
     * 
     * This method is generated.
     * 
     * Please keep or update the summary. Also do the same for explaining what mapping the property does and for what property.
     * 
     * For any questions or suggestions consult with me.
     */
    public static Cancelled ConstructUnvalidated(MeetingFlattened flattened, Func<DateTime> mapCancelledAt) => new(flattened, mapCancelledAt);

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
        CancelledAt = this.CancelledAt,
        CancellationReason = this.CancellationReason,
		StatusTypeId = StatusTypeId,
        StatusSubtypeId = null,
        ScheduledAt = this.FlattenedData?.ScheduledAt ?? mapScheduledAt(),
        ScheduledBy = this.FlattenedData?.ScheduledBy ?? mapScheduledBy(),
		FullyScheduled = this.FlattenedData?.FullyScheduled,
		PartlyScheduled = this.FlattenedData?.PartlyScheduled,
    };
}