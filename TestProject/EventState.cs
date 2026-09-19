namespace TestProject;

using SmartEnum;

[SmartEnum<string>("EventStatusId")]
[SmartEnum<string>("EventStatusSubtypeId")]
public abstract partial class EventState
{
}