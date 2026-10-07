internal sealed record RegressionCase(string Name, string Source, string? Diagnostic = null);

internal static class RegressionCases
{
	public static IEnumerable<RegressionCase> All
	=> [
		new("generic properties with renamed parameters", """
			using SmartEnum;
			[SmartEnum<int>("Id")] public abstract partial class Root<T> { public T Value { get; } }
			public partial class Leaf<U> : Root<U> { public const int Id = 1; public U[] Values { get; init; } }
			public static class RegressionProbe { public static void Verify() {
			 var item = Root<string>.MapFromFlattenedDataUnvalidated(new RootFlattened<string> { Id = 1, Value = "one", Values = new[] { "two" } });
			 if (item is not Leaf<string> leaf || leaf.Value != "one" || leaf.Values[0] != "two") throw new System.Exception();
			} }
			"""),
		new("generic parameter permutation", """
			using SmartEnum;
			[SmartEnum<int>("Id")] public abstract partial class Root<T, U> { public T First { get; } public U Second { get; } }
			public partial class Leaf<A, B> : Root<B, A> { public const int Id = 1; public A Third { get; } }
			public static class RegressionProbe { public static void Verify() {
			 var item = Root<string, int>.MapFromFlattenedDataUnvalidated(new RootFlattened<string, int> { Id = 1, First = "one", Second = 2, Third = 3 });
			 if (item is not Leaf<int, string> leaf || leaf.Third != 3) throw new System.Exception();
			} }
			"""),
		new("matching generic constraints", """
			using SmartEnum;
			[SmartEnum<int>("Id")] public abstract partial class Root<T> where T : class, new() { public T Value { get; } }
			public partial class Leaf<T> : Root<T> where T : class, new() { public const int Id = 1; }
			"""),
		new("generic interface constraints", """
			using SmartEnum;
			[SmartEnum<int>("Id")] public abstract partial class Root<T> where T : System.IDisposable { public T Value { get; } }
			public partial class Leaf<U> : Root<U> where U : System.IDisposable { public const int Id = 1; }
			"""),
		new("generic struct constraints", """
			using SmartEnum;
			[SmartEnum<int>("Id")] public abstract partial class Root<T> where T : unmanaged { public T Value { get; } }
			public partial class Leaf<T> : Root<T> where T : unmanaged { public const int Id = 1; }
			"""),
		new("stronger descendant constraints diagnosed", """
			using SmartEnum;
			[SmartEnum<int>("Id")] public abstract partial class Root<T> { }
			public partial class Leaf<T> : Root<T> where T : class { public const int Id = 1; }
			""", "SMARTENUM012"),
		new("closed specialization diagnosed", """
			using SmartEnum;
			[SmartEnum<int>("Id")] public abstract partial class Root<T> { }
			public partial class Leaf : Root<int> { public const int Id = 1; }
			""", "SMARTENUM012"),
		new("uninferable generic parameter diagnosed", """
			using SmartEnum;
			[SmartEnum<int>("Id")] public abstract partial class Root { }
			public partial class Leaf<T> : Root { public const int Id = 1; }
			""", "SMARTENUM012"),
		new("generic enclosing type", """
			using SmartEnum;
			public partial class Container<T> where T : class {
			 [SmartEnum<int>("Id")] public abstract partial class Root { public T Value { get; } }
			 public partial class Leaf : Root { public const int Id = 1; }
			}
			public static class RegressionProbe { public static void Verify() {
			 if (Container<string>.Root.MapFromFlattenedDataUnvalidated(new Container<string>.RootFlattened { Id = 1, Value = "x" }) is not Container<string>.Leaf { Value: "x" }) throw new System.Exception();
			} }
			"""),
		new("record hierarchy runtime", """
			using SmartEnum;
			[SmartEnum<int>("Id")] public abstract partial record Root { public string Name { get; } }
			public sealed partial record Leaf : Root { public const int Id = 1; public int Count { get; init; } }
			public static class RegressionProbe { public static void Verify() {
			 var leaf = Leaf.ConstructUnvalidated("x", 2);
			 if (leaf != Leaf.ConstructUnvalidated("x", 2) || leaf with { Count = 3 } is not { Count: 3 }) throw new System.Exception();
			 if (Root.MapFromFlattenedDataUnvalidated(new RootFlattened { Id = 1, Name = "x", Count = 2 }) != leaf) throw new System.Exception();
			} }
			"""),
		new("nested readonly struct container", """
			using SmartEnum;
			public readonly partial struct Container {
			 [SmartEnum<int>("Id")] public abstract partial class Root { }
			 public partial class Leaf : Root { public const int Id = 1; }
			}
			"""),
		new("required and readonly fields runtime", """
			using SmartEnum;
			[SmartEnum<int>("Id")] public abstract partial class Root { public required string Name; }
			public partial class Leaf : Root { public const int Id = 1; public readonly int Count; }
			public static class RegressionProbe { public static void Verify() {
			 var leaf = Leaf.ConstructUnvalidated("x", 4);
			 if (leaf.Name != "x" || leaf.Count != 4) throw new System.Exception();
			} }
			"""),
		new("abstract property overrides", """
			using SmartEnum;
			[SmartEnum<int>("Id")] public abstract partial class Root { public abstract string Name { get; } }
			public partial class Leaf : Root { public const int Id = 1; public override string Name { get; } }
			public partial class Other : Root { public const int Id = 2; public override string Name { get; } }
			public static class RegressionProbe { public static void Verify() {
			 if (Root.MapFromFlattenedDataUnvalidated(new RootFlattened { Id = 2, Name = "other" }) is not Other { Name: "other" }) throw new System.Exception();
			} }
			"""),
		new("required abstract property override", """
			using SmartEnum;
			[SmartEnum<int>("Id")] public abstract partial class Root { public abstract required string Name { get; init; } }
			public partial class Leaf : Root { public const int Id = 1; public override required string Name { get; init; } }
			"""),
		new("reuse existing data constructors", """
			using SmartEnum;
			[SmartEnum<int>("Id")] public abstract partial class Root {
			 public string Name { get; }
			 protected Root(string name) { Name = name.ToUpperInvariant(); }
			}
			public partial class Leaf : Root {
			 public const int Id = 1;
			 private Leaf(string name) : base(name) { }
			}
			public static class RegressionProbe { public static void Verify() {
			 if (Leaf.ConstructUnvalidated("abc").Name != "ABC") throw new System.Exception();
			} }
			"""),
		new("unrelated factory overload allowed", """
			using SmartEnum;
			[SmartEnum<int>("Id")] public abstract partial class Root { }
			public partial class Leaf : Root {
			 public const int Id = 1;
			 public static Leaf ConstructUnvalidated(string unused) => ConstructUnvalidated();
			}
			"""),
		new("external parent constructor arguments", """
			using SmartEnum;
			public class Entity { public string Token { get; } protected Entity(string token) { Token = token; } }
			[SmartEnum<int>("Id")] public abstract partial class Root : Entity { public int Count { get; } }
			public partial class Leaf : Root { public const int Id = 1; }
			public static class RegressionProbe { public static void Verify() {
			 if (Root.MapFromFlattenedDataUnvalidated(new RootFlattened { Id = 1, base_token = "token", Count = 3 }) is not Leaf { Token: "token", Count: 3 }) throw new System.Exception();
			} }
			"""),
		new("external required initialization", """
			using SmartEnum;
			public class Entity {
			 public required string Token { get; init; }
			 [System.Diagnostics.CodeAnalysis.SetsRequiredMembers] protected Entity(string token) { Token = token; }
			}
			[SmartEnum<int>("Id")] public abstract partial class Root : Entity { }
			public partial class Leaf : Root { public const int Id = 1; }
			"""),
		new("data property key name collision", """
			using SmartEnum;
			[SmartEnum<int>("Id")] public abstract partial class Root { public int Id { get; } }
			public partial class Leaf : Root { public new const int Id = 1; }
			public static class RegressionProbe { public static void Verify() {
			 if (Root.MapFromFlattenedDataUnvalidated(new RootFlattened { key_Id = 1, Id = 42 }).Id != 42) throw new System.Exception();
			} }
			"""),
		new("private data type exposure diagnosed", """
			using SmartEnum;
			[SmartEnum<int>("Id")] public abstract partial class Root { private class Secret { } private Secret Value { get; } }
			public partial class Leaf : Root { public const int Id = 1; }
			""", "SMARTENUM012"),
		new("field/property duplicate diagnosed", """
			using SmartEnum;
			[SmartEnum<int>("Id")] public abstract partial class Root { public int Value; }
			public partial class Leaf : Root { public const int Id = 1; public new int Value { get; } }
			""", "SMARTENUM012"),
		new("file local diagnosed", """
			using SmartEnum;
			[SmartEnum<int>("Id")] file abstract partial class Root { }
			file partial class Leaf : Root { public const int Id = 1; }
			""", "SMARTENUM012"),
		new("exact factory collision diagnosed", """
			using SmartEnum;
			[SmartEnum<int>("Id")] public abstract partial class Root { }
			public partial class Leaf : Root { public const int Id = 1; public static Leaf ConstructUnvalidated() => new(); }
			""", "SMARTENUM012"),
		new("ambiguous external constructors diagnosed", """
			using SmartEnum;
			public class Entity { protected Entity(int x) { } protected Entity(string x) { } }
			[SmartEnum<int>("Id")] public abstract partial class Root : Entity { }
			public partial class Leaf : Root { public const int Id = 1; }
			""", "SMARTENUM012"),
		new("NaN keys use equality comparer", """
			using SmartEnum;
			[SmartEnum<double>("Id")] public abstract partial class Root { }
			public partial class Leaf : Root { public const double Id = double.NaN; }
			public static class RegressionProbe { public static void Verify() {
			 if (Root.MapFromFlattenedDataUnvalidated(new RootFlattened { Id = double.NaN }) is not Leaf) throw new System.Exception();
			} }
			"""),
		new("NaN duplicate key diagnosed", """
			using SmartEnum;
			[SmartEnum<double>("Id")] public abstract partial class Root { }
			public partial class Leaf : Root { public const double Id = double.NaN; }
			public partial class Other : Root { public const double Id = double.NaN; }
			""", "SMARTENUM013"),
		new("null key dispatch runtime", """
			using SmartEnum;
			[SmartEnum<string>("Id")] public abstract partial class Root { }
			public partial class Leaf : Root { public const string Id = null!; }
			public static class RegressionProbe { public static void Verify() {
			 if (Root.MapFromFlattenedDataUnvalidated(new RootFlattened { Id = null! }) is not Leaf) throw new System.Exception();
			} }
			"""),
		new("empty root mapper rejects unknown", """
			using SmartEnum;
			[SmartEnum<int>("Id")] public abstract partial class Root { }
			public static class RegressionProbe { public static void Verify() {
			 try { Root.MapFromFlattenedDataUnvalidated(new RootFlattened { Id = 1 }); } catch (System.ArgumentException) { return; }
			 throw new System.Exception();
			} }
			"""),
		new("malformed source does not crash", """
			using SmartEnum;
			[SmartEnum<int>(null)] public abstract partial class Root { public string Missing { get;
			""", "SMARTENUM003"),
		new("cyclic inheritance does not crash", """
			using SmartEnum;
			[SmartEnum<int>("Id")] public abstract partial class Root : Leaf { }
			public partial class Leaf : Root { public const int Id = 1; }
			""", "SMARTENUM012")
	];
}