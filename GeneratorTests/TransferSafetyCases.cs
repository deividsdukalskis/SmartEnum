internal static class TransferSafetyCases
{
	public static IEnumerable<RegressionCase> All
		=> [
			new("transfer distinguishes omitted keys and values from explicit defaults", """
				using SmartEnum;
				[SmartEnum<int>("Id")] public abstract partial class Root { public int Count { get; } public string Name { get; } }
				public partial class Leaf : Root { public const int Id = 0; }
				public static class RegressionProbe { public static void Verify() {
				 foreach (var flat in new[] { new RootFlattened { Count = 0, Name = "name" }, new RootFlattened { Id = 0, Name = "name" }, new RootFlattened { Id = 0, Count = 0 }, new RootFlattened { Id = 0, Count = 0, Name = null! } }) {
				  try { Root.MapFromFlattenedDataUnvalidated(flat); throw new System.Exception(); } catch (System.ArgumentException e) { if (e.ParamName != "data") throw; }
				 }
				 if (Root.MapFromFlattenedDataUnvalidated(new RootFlattened { Id = 0, Count = 0, Name = "" }) is not Leaf { Count: 0, Name: "" }) throw new System.Exception();
				} }
				"""),
			new("transfer rejects unused branch values unless explicitly permitted", """
				using SmartEnum;
				[SmartEnum<int>("Id")] public abstract partial class Root { }
				public partial class Leaf : Root { public const int Id = 1; public int Count { get; } }
				public partial class Other : Root { public const int Id = 2; public string Text { get; } }
				public static class RegressionProbe { public static void Verify() {
				 var flat = new RootFlattened { Id = 1, Count = 0, Text = "would be lost" };
				 try { Root.MapFromFlattenedDataUnvalidated(flat); throw new System.Exception(); } catch (System.ArgumentException) { }
				 if (Root.MapFromFlattenedDataUnvalidated(flat, rejectUnusedData: false) is not Leaf { Count: 0 }) throw new System.Exception();
				 if (Root.MapDataToTypeUnvalidated(1, 0, "legacy") is not Leaf { Count: 0 }) throw new System.Exception();
				} }
				"""),
			new("ignored private state never enters the transfer contract", """
				using SmartEnum;
				[SmartEnum<int>("Id")] public abstract partial class Root {
				 [SmartEnumIgnore] private readonly string _secret = "local";
				 public string Secret() => _secret;
				 public string Name { get; }
				}
				public partial class Leaf : Root { public const int Id = 1; }
				public static class RegressionProbe { public static void Verify() {
				 var leaf = Leaf.ConstructUnvalidated("name");
				 if (leaf.Secret() != "local" || typeof(RootFlattened).GetProperty("_secret") is not null) throw new System.Exception();
				 if (Root.MapFromFlattenedDataUnvalidated(leaf.Flatten()).Secret() != "local") throw new System.Exception();
				} }
				"""),
			new("computed snapshot is copied but never restored as source state", """
				using SmartEnum;
				[SmartEnum<int>("Id")] public abstract partial class Root { public int Count { get; } [SmartEnumSnapshot] public int Double => Count * 2; }
				public partial class Leaf : Root { public const int Id = 1; [SmartEnumSnapshot] private string Label => "leaf"; }
				public partial class Other : Root { public const int Id = 2; }
				public static class RegressionProbe { public static void Verify() {
				 var flat = Leaf.ConstructUnvalidated(3).Flatten();
				 if (flat.Double != 6 || flat.Label != "leaf" || Other.ConstructUnvalidated(3).Flatten().Label is not null) throw new System.Exception();
				 flat.Double = 1000;
				 if (Root.MapFromFlattenedDataUnvalidated(flat).Double != 6) throw new System.Exception();
				} }
				"""),
			new("partial model and hooks support deep copies in both directions", """
				using SmartEnum;
				public class Payload { public int Number { get; set; } }
				[SmartEnum<int>("Id")] public abstract partial class Root {
				 public Payload Value { get; }
				 partial void CustomizeFlattenedData(RootFlattened data) { data.Value = new Payload { Number = data.Value.Number }; data.Description = "custom"; }
				 static partial void CustomizeFlattenedInput(ref RootFlattened data) { data = new RootFlattened { Id = data.Id, Value = new Payload { Number = data.Value.Number } }; }
				}
				public partial class RootFlattened { public string? Description { get; set; } }
				public partial class Leaf : Root { public const int Id = 1; }
				public static class RegressionProbe { public static void Verify() {
				 var original = Leaf.ConstructUnvalidated(new Payload { Number = 1 });
				 var flat = original.Flatten();
				 flat.Value.Number = 2;
				 if (original.Value.Number != 1 || flat.Description != "custom") throw new System.Exception();
				 var restored = Root.MapFromFlattenedDataUnvalidated(flat); restored.Value.Number = 3;
				 if (flat.Value.Number != 2 || original.Value.Number != 1) throw new System.Exception();
				} }
				"""),
			new("explicit external parent mapping reads different named private projection", """
				using SmartEnum;
				public class Entity { protected string Stored { get; } protected Entity(string token) { Stored = token; } }
				[SmartEnum<int>("Id")][SmartEnumParentData("token", "Projection")]
				public abstract partial class Root : Entity { private string Projection => Stored; }
				public partial class Leaf : Root { public const int Id = 1; }
				public static class RegressionProbe { public static void Verify() {
				 var flat = Leaf.ConstructUnvalidated("state").Flatten();
				 if (flat.base_token != "state" || Root.MapFromFlattenedDataUnvalidated(flat).Flatten().base_token != "state") throw new System.Exception();
				} }
				"""),
			new("unconstrained optional reference distinguishes present null from absence", """
				using SmartEnum;
				[SmartEnum<int>("Id")] public abstract partial class Root<T> { }
				public partial class Leaf<T> : Root<T> { public const int Id = 1; public T? Value { get; } }
				public partial class Other<T> : Root<T> { public const int Id = 2; }
				public static class RegressionProbe { public static void Verify() {
				 var flat = Leaf<string>.ConstructUnvalidated(null).Flatten();
				 if (!flat.Value.HasValue || flat.Value.Value.Value is not null) throw new System.Exception();
				 if (Root<string>.MapFromFlattenedDataUnvalidated(flat) is not Leaf<string> { Value: null }) throw new System.Exception();
				} }
				"""),
			new("ignored required state diagnosed", """
				using SmartEnum;
				[SmartEnum<int>("Id")] public abstract partial class Root { [SmartEnumIgnore] public required string Name { get; init; } }
				public partial class Leaf : Root { public const int Id = 1; }
				""", "SMARTENUM012"),
			new("partial flattened generated member collision diagnosed", """
				using SmartEnum;
				[SmartEnum<int>("Id")] public abstract partial class Root { }
				public partial class RootFlattened { public int Id { get; set; } }
				public partial class Leaf : Root { public const int Id = 1; }
				""", "SMARTENUM012"),
			new("duplicate external parent mapping diagnosed", """
				using SmartEnum;
				public class Entity { public string Token { get; } protected Entity(string token) { Token = token; } }
				[SmartEnum<int>("Id")][SmartEnumParentData("token", "Token")][SmartEnumParentData("token", "Token")]
				public abstract partial class Root : Entity { }
				public partial class Leaf : Root { public const int Id = 1; }
				""", "SMARTENUM012"),
			new("generic optional values and presence survive JSON transport", """
				using SmartEnum;
				using System.Text.Json;
				[SmartEnum<int>("Id")] public abstract partial class Root<TValue> { }
				public partial class Leaf<TValue> : Root<TValue> { public const int Id = 1; public TValue Value { get; } }
				public partial class Other<TValue> : Root<TValue> { public const int Id = 2; }
				public static class RegressionProbe { public static void Verify() {
				 var json = JsonSerializer.Serialize(Leaf<int>.ConstructUnvalidated(42).Flatten());
				 var flat = JsonSerializer.Deserialize<RootFlattened<int>>(json)!;
				 if (Root<int>.MapFromFlattenedDataUnvalidated(flat) is not Leaf<int> { Value: 42 }) throw new System.Exception();
				 try { Root<int>.MapFromFlattenedDataUnvalidated(JsonSerializer.Deserialize<RootFlattened<int>>("{}")!); throw new System.Exception(); } catch (System.ArgumentException) { }
				} }
				"""),
			new("unknown runtime subclasses cannot silently discard data", """
				using SmartEnum;
				using System.Reflection;
				using System.Reflection.Emit;
				[SmartEnum<int>("Id")] public abstract partial class Root { }
				public partial class Leaf : Root { public const int Id = 1; }
				public static class RegressionProbe { public static void Verify() {
				 var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("DynamicHierarchy"), AssemblyBuilderAccess.RunAndCollect);
				 var builder = assembly.DefineDynamicModule("Main").DefineType("RuntimeOnly", TypeAttributes.Public, typeof(Root));
				 builder.DefineDefaultConstructor(MethodAttributes.Public);
				 var instance = (Root)System.Activator.CreateInstance(builder.CreateType()!)!;
				 try { instance.Flatten(); throw new System.Exception(); } catch (System.InvalidOperationException) { }
				} }
				""")
		];
}