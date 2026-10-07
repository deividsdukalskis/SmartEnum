internal static class FlattenedCases
{
	public static IEnumerable<RegressionCase> All
		=> [
			new("flattened multi-level private state and nullable branches round trip", """
				using SmartEnum;
				[SmartEnum<int>("Kind")][SmartEnum<string>("Variant")]
				public abstract partial class Root { private readonly string _name; public string Name => _name; }
				public abstract partial class First : Root { public const int Kind = 1; public int Count { get; } }
				public sealed partial class Leaf : First { public const string Variant = "same"; private readonly bool _flag; public bool Flag => _flag; }
				public abstract partial class Second : Root { public const int Kind = 2; public string? Note { get; } }
				public sealed partial class Other : Second { public const string Variant = "same"; public System.DateTime Time { get; init; } }
				public static class RegressionProbe { public static void Verify() {
				 Root item = Leaf.ConstructUnvalidated("name", 0, false);
				 RootFlattened flat = item.Flatten();
				 if (flat.Kind != 1 || flat.Variant != "same" || flat._name != "name" || flat.Count != 0 || flat._flag != false || flat.Note is not null || flat.Time is not null) throw new System.Exception();
				 if (Root.MapFromFlattenedDataUnvalidated(flat) is not Leaf { Name: "name", Count: 0, Flag: false }) throw new System.Exception();
				 var other = Other.ConstructUnvalidated("other", null, new System.DateTime(2026, 1, 2)).Flatten();
				 if (other.Kind != 2 || other.Count is not null || other._flag is not null || other.Time is null) throw new System.Exception();
				 if (Root.MapFromFlattenedDataUnvalidated(other) is not Other { Name: "other", Note: null }) throw new System.Exception();
				 if (typeof(RootFlattened).GetProperty("Count")!.PropertyType != typeof(int?) || typeof(RootFlattened).GetProperty("_flag")!.PropertyType != typeof(bool?)) throw new System.Exception();
				 if (Root.MapDataToTypeUnvalidated(1, "same", "legacy", 0, false, null, default) is not Leaf) throw new System.Exception();
				 flat.Count = 8;
				 if (((Leaf)item).Count != 0 || item.Flatten().Count != 0) throw new System.Exception();
				} }
				"""),
			new("flattened null input and missing selected value rejected", """
				using SmartEnum;
				[SmartEnum<int>("Id")] public abstract partial class Root { }
				public partial class Leaf : Root { public const int Id = 1; public int Count { get; } }
				public partial class Other : Root { public const int Id = 2; }
				public static class RegressionProbe { public static void Verify() {
				 try { Root.MapFromFlattenedDataUnvalidated(null!); throw new System.Exception(); } catch (System.ArgumentNullException e) { if (e.ParamName != "data") throw; }
				 try { Root.MapFromFlattenedDataUnvalidated(new RootFlattened { Id = 1 }); throw new System.Exception(); } catch (System.ArgumentException e) { if (e.ParamName != "data") throw; }
				 if (Root.MapFromFlattenedDataUnvalidated(new RootFlattened { Id = 2 }) is not Other) throw new System.Exception();
				} }
				"""),
			new("flattened generic value branches preserve null and zero", """
				using SmartEnum;
				[SmartEnum<int>("Id")] public abstract partial class Root<T> where T : struct { }
				public partial class Leaf<U> : Root<U> where U : struct { public const int Id = 1; public U Value { get; } public U? Optional { get; } }
				public partial class Other<T> : Root<T> where T : struct { public const int Id = 2; }
				public static class RegressionProbe { public static void Verify() {
				 RootFlattened<int> flat = Leaf<int>.ConstructUnvalidated(null, 0).Flatten();
				 if (flat.Value != 0 || flat.Optional is not null || Other<int>.ConstructUnvalidated().Flatten().Value is not null) throw new System.Exception();
				 if (Root<int>.MapFromFlattenedDataUnvalidated(flat) is not Leaf<int> { Value: 0, Optional: null }) throw new System.Exception();
				} }
				"""),
			new("flattened generic reference branches and nested containers", """
				using SmartEnum;
				public partial class Container<X> where X : class {
				 [SmartEnum<int>("Id")] public abstract partial class Root<T> where T : class { public X Shared { get; } }
				 public partial class Leaf<U> : Root<U> where U : class { public const int Id = 1; public U Value { get; } }
				 public partial class Other<U> : Root<U> where U : class { public const int Id = 2; }
				}
				public static class RegressionProbe { public static void Verify() {
				 var flat = Container<string>.Leaf<string>.ConstructUnvalidated("shared", "branch").Flatten();
				 if (flat.Value != "branch" || flat.Shared != "shared") throw new System.Exception();
				 if (Container<string>.Root<string>.MapFromFlattenedDataUnvalidated(flat) is not Container<string>.Leaf<string> { Value: "branch" }) throw new System.Exception();
				 if (Container<string>.Other<string>.ConstructUnvalidated("shared").Flatten().Value is not null) throw new System.Exception();
				} }
				"""),
			new("flattened record required data and custom setter state", """
				using SmartEnum;
				[SmartEnum<int>("Id")] public abstract partial record Root { public required string Name { get; init; } }
				public sealed partial record Leaf : Root { public const int Id = 1; private string _text = ""; public string Text { get => _text; set => _text = value.ToUpperInvariant(); } }
				public static class RegressionProbe { public static void Verify() {
				 var leaf = Leaf.ConstructUnvalidated("name", "setter", "raw");
				 var flat = leaf.Flatten();
				 if (flat.Text != "raw" || flat._text != "raw" || Root.MapFromFlattenedDataUnvalidated(flat) != leaf) throw new System.Exception();
				} }
				"""),
			new("flattened virtual property overrides and key collision", """
				using SmartEnum;
				[SmartEnum<int>("Id")] public abstract partial class Root { public virtual string Value { get; } public int Id { get; } }
				public partial class Leaf : Root { public new const int Id = 1; public override string Value { get; } }
				public static class RegressionProbe { public static void Verify() {
				 var flat = Leaf.ConstructUnvalidated(42, "value").Flatten();
				 if (flat.Id != 42 || flat.key_Id != 1 || flat.Value != "value") throw new System.Exception();
				 if (Root.MapFromFlattenedDataUnvalidated(flat).Id != 42) throw new System.Exception();
				} }
				"""),
			new("flattened external parent reads current state", """
				using SmartEnum;
				public class Entity { public string Token { get; set; } protected Entity(string token) { Token = token; } }
				[SmartEnum<int>("Id")] public abstract partial class Root : Entity { }
				public partial class Leaf : Root { public const int Id = 1; }
				public static class RegressionProbe { public static void Verify() {
				 var leaf = Leaf.ConstructUnvalidated("original"); leaf.Token = "current";
				 if (leaf.Flatten().base_token != "current" || Root.MapFromFlattenedDataUnvalidated(leaf.Flatten()).Token != "current") throw new System.Exception();
				} }
				"""),
			new("flattened escaped key and private property", """
				using SmartEnum;
				[SmartEnum<string>("class")] public abstract partial class Root { private string @event { get; set; } }
				public partial class Leaf : Root { public const string @class = "leaf"; }
				public static class RegressionProbe { public static void Verify() {
				 var flat = Leaf.ConstructUnvalidated("value").Flatten();
				 if (flat.@class != "leaf" || flat.@event != "value" || Root.MapFromFlattenedDataUnvalidated(flat).Flatten().@event != "value") throw new System.Exception();
				} }
				"""),
			new("flattened model name collision diagnosed", """
				using SmartEnum;
				[SmartEnum<int>("Id")] public abstract partial class Root { }
				public class RootFlattened { }
				public partial class Leaf : Root { public const int Id = 1; }
				""", "SMARTENUM012"),
			new("flatten method collision diagnosed", """
				using SmartEnum;
				[SmartEnum<int>("Id")] public abstract partial class Root { public object Flatten() => new(); }
				public partial class Leaf : Root { public const int Id = 1; }
				""", "SMARTENUM012"),
			new("flatten writer collision diagnosed", """
				using SmartEnum;
				[SmartEnum<int>("Id")] public abstract partial class Root { }
				public partial class Leaf : Root { public const int Id = 1; private int __SmartEnumWriteFlattenedData; }
				""", "SMARTENUM012"),
			new("flattened property class name collision diagnosed", """
				using SmartEnum;
				[SmartEnum<int>("Id")] public abstract partial class Root { public int RootFlattened { get; } }
				public partial class Leaf : Root { public const int Id = 1; }
				""", "SMARTENUM012"),
			new("flattened write-only data diagnosed", """
				using SmartEnum;
				[SmartEnum<int>("Id")] public abstract partial class Root { public string Value { set { } } }
				public partial class Leaf : Root { public const int Id = 1; }
				""", "SMARTENUM012"),
			new("flattened unreadable external state diagnosed", """
				using SmartEnum;
				public class Entity { protected Entity(string token) { } }
				[SmartEnum<int>("Id")] public abstract partial class Root : Entity { }
				public partial class Leaf : Root { public const int Id = 1; }
				""", "SMARTENUM012"),
			new("flattened unconstrained branch supports reference and value types", """
				using SmartEnum;
				[SmartEnum<int>("Id")] public abstract partial class Root<T> { }
				public partial class Leaf<T> : Root<T> { public const int Id = 1; public T Value { get; } }
				public partial class Other<T> : Root<T> { public const int Id = 2; }
				public static class RegressionProbe { public static void Verify() {
				 var zero = Leaf<int>.ConstructUnvalidated(0).Flatten();
				 if (zero.Value is null || zero.Value.Value.Value != 0 || Other<int>.ConstructUnvalidated().Flatten().Value is not null) throw new System.Exception();
				 if (Root<int>.MapFromFlattenedDataUnvalidated(zero) is not Leaf<int> { Value: 0 }) throw new System.Exception();
				 var text = new RootFlattened<string> { Id = 1, Value = "text" };
				 if (Root<string>.MapFromFlattenedDataUnvalidated(text) is not Leaf<string> { Value: "text" }) throw new System.Exception();
				 try { Root<int>.MapFromFlattenedDataUnvalidated(new RootFlattened<int> { Id = 1 }); throw new System.Exception(); } catch (System.ArgumentException) { }
				} }
				""")
		];
}