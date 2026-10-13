namespace AI.Meetings4;

public class PartlyScheduled : Scheduled
{
    public const string ScheduledTypeId = "partly_scheduled";

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
    private PartlyScheduled(MeetingFlattened flattened) : base(flattened.Status.Scheduled.ScheduledBy, flattened.Status.Scheduled.ScheduledAt) => this.FlattenedData = flattened;

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
			/*
			 * For AI
			 * 
			 * Your mapping strategy
			 */
		};
}