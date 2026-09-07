namespace TestProject;

using SmartEnum;

[SmartEnum<string>("StatusId")]
[SmartEnum<string>("StatusDuId")]
public partial class EventState
{
	public string Test { get; private init; }
}

public abstract partial class Scheduled1 : EventState
{
	public const string StatusId = "Tets";
}

public partial class Scheduled2 : Scheduled1
{
	public const string StatusDuId = "d";
}