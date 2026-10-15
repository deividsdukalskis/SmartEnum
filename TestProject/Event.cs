namespace TestProject;

using SmartEnum;

[SmartEnumParent(FlatteningStrategy.Flat)]
public sealed partial class Event : AggregateRoot
{
	[SmartEnumIdentity("EventId")]
	public string Id { get; set; }

	public EventState State { get; set; }
}