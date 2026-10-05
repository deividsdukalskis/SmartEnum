internal static class EdgeCases
{
	public static IEnumerable<RegressionCase> All
	{
		get
		{
			yield return new("keyword key name", """
				using SmartEnum;
				[SmartEnum<int>("class")] public abstract partial class Root { }
				public partial class Leaf : Root { public const int @class = 1; }
				""");
			yield return new("colliding key parameter prefixes", """
				using SmartEnum;
				[SmartEnum<int>("Id")][SmartEnum<int>("key_Id")] public abstract partial class Root { public int Id { get; } }
				public abstract partial class Middle : Root { public new const int Id = 1; }
				public partial class Leaf : Middle { public const int key_Id = 2; }
				public static class RegressionProbe { public static void Verify() {
				 if (Root.MapDataToTypeUnvalidated(1, 2, 42).Id != 42) throw new System.Exception();
				} }
				""");
			yield return new("private backing fields stay implementation details", """
				using SmartEnum;
				[SmartEnum<int>("Id")] public abstract partial class Root {
				 private string text = "";
				 public string Name { get => text; private set => text = value.ToUpperInvariant(); }
				}
				public partial class Leaf : Root { public const int Id = 1; }
				public static class RegressionProbe { public static void Verify() {
				 if (Leaf.ConstructUnvalidated("text").Name != "TEXT") throw new System.Exception();
				} }
				""");
			yield return new("internal branch cannot expose internal mapper data", """
				using SmartEnum;
				internal class Secret { }
				[SmartEnum<int>("Id")] public abstract partial class Root { }
				internal partial class Leaf : Root { public const int Id = 1; public Secret Value { get; } }
				""", "SMARTENUM012");
			yield return new("same simple name as factory diagnosed", """
				using SmartEnum;
				[SmartEnum<int>("Id")] public abstract partial class Root { }
				public partial class ConstructUnvalidated : Root { public const int Id = 1; }
				""", "SMARTENUM012");
			yield return new("nullable reference constraint strengthening diagnosed", """
				using SmartEnum;
				[SmartEnum<int>("Id")] public abstract partial class Root<T> where T : class? { }
				public partial class Leaf<T> : Root<T> where T : class { public const int Id = 1; }
				""", "SMARTENUM012");
			yield return new("positional record diagnosed", """
				using SmartEnum;
				[SmartEnum<int>("Id")] public abstract partial record Root(string Name);
				public partial record Leaf : Root { public const int Id = 1; }
				""", "SMARTENUM012");
			yield return new("split files partial root and leaf", """
				using SmartEnum;
				namespace Example;
				[SmartEnum<int>("Id")] public abstract partial class Root { public string Name { get; } }
				// ---file---
				namespace Example;
				public abstract partial class Root { public int Number { get; } }
				public partial class Leaf : Root { public const int Id = 1; }
				""");
			yield return new("ref parent constructor diagnosed", """
				using SmartEnum;
				public class Entity { protected Entity(ref int value) { } }
				[SmartEnum<int>("Id")] public abstract partial class Root : Entity { }
				public partial class Leaf : Root { public const int Id = 1; }
				""", "SMARTENUM012");
			yield return new("existing required constructor must carry contract", """
				using SmartEnum;
				[SmartEnum<int>("Id")] public abstract partial class Root { }
				public partial class Leaf : Root { public const int Id = 1; public required string Name { get; init; } private Leaf(string name) { Name = name; } }
				""", "SMARTENUM012");
			yield return new("parent argument and child data collision diagnosed", """
				using SmartEnum;
				public class Entity { protected Entity(string token) { } }
				[SmartEnum<int>("Id")] public abstract partial class Root : Entity { }
				public partial class Leaf : Root { public const int Id = 1; public string base_token { get; } }
				""", "SMARTENUM012");
			foreach ((string type, string literal) in new (string, string)[]
			{
				("byte", "255"), ("sbyte", "-128"), ("short", "-32768"), ("ushort", "65535"),
				("int", "int.MinValue"), ("uint", "uint.MaxValue"), ("long", "long.MinValue"), ("ulong", "ulong.MaxValue"),
				("char", "'\\u263A'"), ("bool", "true"), ("decimal", "decimal.MaxValue"), ("float", "float.PositiveInfinity"),
				("double", "double.NegativeInfinity"), ("string", "\"a\\\"b\\\\c\\n\\u263A\"")
			})
			{
				yield return new($"key boundary {type}", $$"""
					using SmartEnum;
					[SmartEnum<{{type}}>("Id")] public abstract partial class Root { }
					public partial class Leaf : Root { public const {{type}} Id = {{literal}}; }
					public static class RegressionProbe { public static void Verify() {
					 if (Root.MapDataToTypeUnvalidated(Leaf.Id) is not Leaf) throw new System.Exception();
					} }
					""");
			}

			string branches = string.Join("\n", Enumerable.Range(0, 8).Select(parent => $$"""
				public abstract partial class Parent{{parent}} : Root { public const int ParentId = {{parent}}; }
				{{string.Join("\n", Enumerable.Range(0, 8).Select(child => $"public partial class Leaf{parent}_{child} : Parent{parent} {{ public const int ChildId = {child}; }}"))}}
				"""));
			yield return new("64 leaf complete key matrix", $$"""
				using SmartEnum;
				[SmartEnum<int>("ParentId")][SmartEnum<int>("ChildId")] public abstract partial class Root { }
				{{branches}}
				public static class RegressionProbe { public static void Verify() {
				 for (int parent = -1; parent <= 8; parent++) for (int child = -1; child <= 8; child++) {
				  bool valid = parent >= 0 && parent < 8 && child >= 0 && child < 8;
				  try { var result = Root.MapDataToTypeUnvalidated(parent, child);
				   if (!valid || result.GetType().Name != $"Leaf{parent}_{child}") throw new System.Exception();
				  } catch (System.ArgumentException) { if (valid) throw; }
				 }
				} }
				""");
		}
	}
}