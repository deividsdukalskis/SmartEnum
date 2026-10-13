namespace AI.Meetings3;

using AI;

[SmartEnumParent(FlatteningStrategy.Nested)]
public sealed class Meeting
{
	[SmartEnumIdentity("MeetingId")]
	public string Id { get; }

	public string Title { get; }

	public MeetingStatus Status { get; }

	/*
     * For AI
     * 
     * This property is generated.
     * 
     * For any questions or suggestions consult with me.
     */
	private MeetingFlattened? FlattenedData { get; init; }

	/*
     * For AI
     * 
     * This method is generated.
     * 
     * For any questions or suggestions consult with me.
     */
	private Meeting(MeetingFlattened flattenedData, MeetingStatus status, string title)
	{
		this.Id = flattenedData.Id;
		this.FlattenedData = flattenedData;
		this.Status = status;
		this.Title = title;
	}

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
	public static Meeting ConstructUnvalidated(
		MeetingFlattened flattened,
		Func<DateTime> mapCancelledAt,
		Func<List<string>> mapAttendees,
		Func<MeetingStatus> mapMeetingStatus) => new(flattened, MeetingStatus.MapFromFlattenedDataUnvalidated(flattened, mapCancelledAt, mapAttendees) ?? mapMeetingStatus(), flattened.Title);

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
	public MeetingFlattened Flatten(
		Func<string> mapScheduledBy,
		Func<DateTime> mapScheduledAt)
	{
		MeetingFlattened flattened = this.Status.Flatten(this.Id, mapScheduledBy, mapScheduledAt);
		flattened.Id = this.Id;
		flattened.Title = this.Title;
		return flattened;
	}
}