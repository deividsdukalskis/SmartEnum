namespace AI.Meetings4;

using AI;

public class FullyScheduled : Scheduled
{
    public const string ScheduledTypeId = "fully_scheduled";

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
    private FullyScheduled(MeetingFlattened flattened, Func<List<string>> mapAttendees) : base(flattened.Status.Scheduled.ScheduledBy, flattened.Status.Scheduled.ScheduledAt)
    {
		/*
         * For AI
         * 
         * Your mapping strategy
         */
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
	public static FullyScheduled ConstructUnvalidated(MeetingFlattened flattened, Func<List<string>> mapAttendees) => new(flattened, mapAttendees);

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