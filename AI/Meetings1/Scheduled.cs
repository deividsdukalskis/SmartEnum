namespace AI.Meetings1;

public abstract class Scheduled : MeetingStatus
{
    public const string StatusTypeId = "scheduled";

    public string ScheduledBy { get; }

    public DateTime ScheduledAt { get; }

    /*
     * For AI
     * 
     * This method is generated.
     * 
     * For any questions or suggestions consult with me.
     */
    protected Scheduled(string id, string scheduledBy, DateTime scheduledAt) : base(id)
    {
        this.ScheduledBy = scheduledBy;
        this.ScheduledAt = scheduledAt;
    }
}