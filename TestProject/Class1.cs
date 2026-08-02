namespace TestProject;

using SmartEnum;

public class EventState
{
}

public class Scheduled1 : EventState
{
	public const int StatusId = 1;
}

[SmartEnum<string>("StatusSubtype Id")]
[SmartEnum<string>("StatusSu btypeIds")]
public class Scheduled2 : Scheduled1
{
	public const int StatusId = 1;
}

public class Scheduled3 : Scheduled2
{
	public const int StatusId = 1;
}