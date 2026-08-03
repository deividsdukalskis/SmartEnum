namespace TestProject;

using SmartEnum;

//[SmartEnum<string>("StatusSubtypeId")]
//[SmartEnum<string>("StatusSubtypeIds")]
public class EventState
{
}

[SmartEnum<string>("StatusSubtypeId")]
[SmartEnum<string>("StatusSubtypeIds")]
public abstract partial class Scheduled1 : EventState
{
	public const int StatusId = 1;
}

public abstract class Scheduled2 : Scheduled1
{
	public const int StatusSubtypeId = 1;
}

public partial class Scheduled3 : Scheduled2
{
	public const string StatusSubtypeIds = "";
}