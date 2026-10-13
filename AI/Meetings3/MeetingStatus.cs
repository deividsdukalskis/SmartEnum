namespace AI.Meetings3;

using AI;

[SmartEnum<string>("StatusTypeId", FlatteningStrategy.Nested, KeyValueOnBranchMissing.FirstBranch)]
[SmartEnum<string>("StatusSubtypeId", FlatteningStrategy.Nested, KeyValueOnBranchMissing.SetNull)]
public abstract class MeetingStatus
{
	/*
     * For AI
     * 
     * This property is generated.
     * 
     * For any questions or suggestions consult with me.
     */
	protected abstract MeetingFlattened? FlattenedData { get; init; }

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
    public static MeetingStatus? MapFromFlattenedDataUnvalidated(MeetingFlattened flattened, Func<DateTime> mapCancelledAt, Func<List<string>> mapAttendees)
    {
        if (flattened.StatusTypeId == Scheduled.StatusTypeId)
        {
            if (flattened.StatusSubtypeId == PartlyScheduled.StatusSubtypeId)
            {
                return PartlyScheduled.ConstructUnvalidated(flattened);
            }

            if (flattened.StatusSubtypeId == FullyScheduled.StatusSubtypeId)
            {
                return FullyScheduled.ConstructUnvalidated(flattened, mapAttendees);
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
    public abstract MeetingFlattened Flatten(
		string meetingId, // For AI: this comes from [SmartEnumIdentity("MeetingId")] in Meeting class.
		Func<string> mapScheduledBy, 
		Func<DateTime> mapScheduledAt);
}