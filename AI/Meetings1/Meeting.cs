namespace AI.Meetings1;

using AI;

[SmartEnumParent(FlatteningStrategy.Flat)]
public sealed class Meeting
{
	[SmartEnumIdentity("MeetingId")]
	public string Id { get; }

	[SmartEnumPrivateAccessor(nameof(_statuses))]
	public IReadOnlyList<MeetingStatus> Statuses => _statuses.AsReadOnly();

	private readonly List<MeetingStatus> _statuses = new();

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
	private Meeting(MeetingFlattened flattenedData, List<MeetingStatus> statuses)
	{
		this.Id = flattenedData.Id;
		this.FlattenedData = flattenedData;
		_statuses = statuses;
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
		Func<(List<MeetingFlattened.MeetingStatusFlattened> Flattened,
			Func<DateTime> MapCancelledAt), List<MeetingStatus>> mapStatuses) => new(flattened, mapStatuses((flattened.Statuses, mapCancelledAt)));

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
		Func<List<string>> mapAttendees,
		Func<DateTime> mapScheduledAt,
		Func<(string MeetingId, // For AI: This comes from [SmartEnumIdentity("MeetingId")]
			List<MeetingStatus> Statuses,
			Func<string> MapScheduledBy,
			Func<List<string>> MapAttendees,
			Func<DateTime> MapScheduledAt), List<MeetingFlattened.MeetingStatusFlattened>> mapStatuses) => new()
			{
				Id = this.Id,
				Statuses = mapStatuses((this.Id, _statuses, mapScheduledBy, mapAttendees, mapScheduledAt)),
			};
}