namespace AI.Meetings1;

using AI;

[SmartEnum<string>("StatusTypeId", FlatteningStrategy.Flat, KeyValueOnBranchMissing.FirstBranch)]
[SmartEnum<string>("StatusSubtypeId", FlatteningStrategy.Flat, KeyValueOnBranchMissing.SetNull)]
public abstract class MeetingStatus
{
	[SmartEnumIdentity("MeetingStatusId")]
    public string Id { get; }

	/*
     * For AI
     * 
     * This property is generated.
     * 
     * For any questions or suggestions consult with me.
     */
	protected abstract MeetingFlattened.MeetingStatusFlattened? FlattenedData { get; init; }

    /*
     * For AI
     * 
     * This method is generated.
     * 
     * For any questions or suggestions consult with me.
     */
    protected MeetingStatus(string id) => this.Id = id;

    /*
     * For AI
     * 
     * This method is generated.
     * 
     * Please update with a descriptive summary especially for parameters that map user chosen behavior to the flattened data structure property in case the
     * flattened data structure is null. It should explain to what exact property it will map to and why.
     * 
     * Keep the warning that it will bypass validation.
     * 
     * If any questions ask. For suggestions consult with me.
     */
    public static MeetingStatus? MapFromFlattenedDataUnvalidated(MeetingFlattened.MeetingStatusFlattened flattened, Func<DateTime> mapCancelledAt)
    {
        if (flattened.StatusTypeId == Scheduled.StatusTypeId)
        {
            if (flattened.StatusSubtypeId == PartlyScheduled.StatusSubtypeId)
            {
                return PartlyScheduled.ConstructUnvalidated(flattened);
            }

            if (flattened.StatusSubtypeId == FullyScheduled.StatusSubtypeId)
            {
                return FullyScheduled.ConstructUnvalidated(flattened);
            }
        }

        if (flattened.StatusTypeId == Cancelled.StatusTypeId)
        {
            return Cancelled.ConstructUnvalidated(flattened, mapCancelledAt);
        }

        return null;
    }

    /*
     * For AI
     * 
     * This method is generated.
     * 
     * Please create a descriptive summary especially for parameters that map user chosen behavior to the flattened data structure property in case the
     * flattened data structure is null. It should explain to what exact property it will map to and why.
     * 
     * If any questions ask. For suggestions consult with me.
     */
    public abstract MeetingFlattened.MeetingStatusFlattened Flatten(
		string meetingId, // For AI: this comes from [SmartEnumIdentity("MeetingId")] in Meeting class.
		Func<string> mapScheduledBy,
		Func<List<string>> mapAttendees, 
		Func<DateTime> mapScheduledAt);
}