internal sealed record RegressionCase(string Name, string Source, string? Diagnostic = null);

internal static class CurrentContractCases
{
	public static IEnumerable<RegressionCase> All => Cases.Concat(Boundaries()).Concat(InvalidDeclarations());
	private static IEnumerable<RegressionCase> InvalidDeclarations()
	{
		const string root = "using SmartEnum; [SmartEnum<int>(\"Kind\", FlatteningStrategy.Separate, KeyValueOnBranchMissing.FirstBranch)] public abstract partial class Root { } ";
		yield return new("non-partial declaration", root + "public sealed class Leaf : Root { public const int Kind = 1; }", "SMARTENUM002");
		yield return new("invalid discriminator identifier", root.Replace("\"Kind\"", "\"bad name\"") + "public sealed partial class Leaf : Root { }", "SMARTENUM003");
		yield return new("missing discriminator", root + "public sealed partial class Leaf : Root { }", "SMARTENUM004");
		yield return new("non-public discriminator", root + "public sealed partial class Leaf : Root { private const int Kind = 1; }", "SMARTENUM005");
		yield return new("non-constant discriminator", root + "public sealed partial class Leaf : Root { public static int Kind = 1; }", "SMARTENUM006");
		yield return new("wrong discriminator type", root + "public sealed partial class Leaf : Root { public const long Kind = 1; }", "SMARTENUM007");
		yield return new("abstract terminal branch", root + "public abstract partial class Leaf : Root { public const int Kind = 1; }", "SMARTENUM010");
		yield return new("uninferable variant parameter", root + "public sealed partial class Leaf<T> : Root { public const int Kind = 1; }", "SMARTENUM021");
		yield return new("missing level declaration", root + "public abstract partial class Branch : Root { public const int Kind = 1; } public sealed partial class Leaf : Branch { }", "SMARTENUM014");
		yield return new("Separate normalized data-key collision", root + "public sealed partial class Leaf : Root { public const int Kind = 1; public int _kind { get; } }", "SMARTENUM037");
		yield return new("Separate normalized key collision", root.Replace("public abstract", "[SmartEnum<int>(\"_kind\", FlatteningStrategy.Separate, KeyValueOnBranchMissing.SetNull)] public abstract") + "public sealed partial class Leaf : Root { public const int Kind = 1; }", "SMARTENUM009");
	}
	private static IEnumerable<RegressionCase> Cases => [
		new("generic restarted hierarchy retains inherited data", """
        using SmartEnum;
        [SmartEnum<int>("Kind", FlatteningStrategy.Flat, KeyValueOnBranchMissing.FirstBranch)] public abstract partial class Root<T> where T : class { public T Value { get; } }
        [SmartEnum<int>("Subkind", FlatteningStrategy.Nested, KeyValueOnBranchMissing.FirstBranch)] public abstract partial class Group<U> : Root<U> where U : class { public const int Kind = 1; }
        public sealed partial class Leaf<V> : Group<V> where V : class { public const int Subkind = 2; public int Count { get; } }
        public static class RegressionProbe { public static void Verify() {
          var leaf = Group<string>.ConstructUnvalidated(new GroupFlattened<string> { Value = "root", Subkind = 2, Leaf = new() { Count = 7 } }, () => 0)!;
          if (leaf.FlattenHierarchy().Leaf?.Count != 7 || leaf.Value != "root" || Root<string>.ConstructUnvalidated(leaf.Flatten(), () => 0) is not Leaf<string>) throw new System.Exception();
        } }
        """),
		new("generic Flat hierarchy with renamed parameters", """
        using SmartEnum;
        [SmartEnum<int>("Kind", FlatteningStrategy.Flat, KeyValueOnBranchMissing.FirstBranch)] public abstract partial class Root<T> where T : class { public T Value { get; } }
        public sealed partial class Leaf<U> : Root<U> where U : class { public const int Kind = 1; public U Extra { get; } }
        public static class RegressionProbe { public static void Verify() {
          var leaf = Leaf<string>.ConstructUnvalidated(new RootFlattened<string> { Kind = 1, Value = "root", Extra = "leaf" });
          if (Root<string>.ConstructUnvalidated(leaf.Flatten()) is not Leaf<string> { Value: "root", Extra: "leaf" }) throw new System.Exception();
        } }
        """),
		new("generic Nested hierarchy with struct constraint", """
        using SmartEnum;
        [SmartEnum<int>("Kind", FlatteningStrategy.Nested, KeyValueOnBranchMissing.FirstBranch)] public abstract partial class Root<T> where T : struct { }
        public sealed partial class Leaf<U> : Root<U> where U : struct { public const int Kind = 1; public U Value { get; } }
        public static class RegressionProbe { public static void Verify() {
          var leaf = Root<int>.ConstructUnvalidated(new RootFlattened<int> { Kind = 1 }, () => 42)!;
          if (leaf.Flatten().Leaf?.Value != 42) throw new System.Exception();
        } }
        """),
		new("nested non-positional record hierarchy", """
        using SmartEnum;
        public static partial class Container {
          [SmartEnum<int>("Kind", FlatteningStrategy.Nested, KeyValueOnBranchMissing.FirstBranch)] public abstract partial record Root { public string Name { get; } }
          public sealed partial record Leaf : Root { public const int Kind = 1; public int Count { get; } }
        }
        public static class RegressionProbe { public static void Verify() {
          var leaf = Container.Leaf.ConstructUnvalidated(new Container.RootFlattened { Kind = 1, Name = "x", Leaf = new() { Count = 3 } }, () => 0);
          if (leaf.Flatten().Leaf?.Count != 3 || leaf.Name != "x") throw new System.Exception();
        } }
        """),
		new("Separate record hierarchy in partial container", """
        using SmartEnum;
        public static partial class Container {
          [SmartEnum<int>("Kind", FlatteningStrategy.Separate, KeyValueOnBranchMissing.FirstBranch)] public abstract partial record Root { public string Name { get; } }
          public sealed partial record Leaf : Root { public const int Kind = 1; public int Count { get; } }
        }
        public static class RegressionProbe { public static void Verify() {
          var leaf = Container.Leaf.ConstructUnvalidated(new Container.LeafFlattened { Kind = 1, Name = "x", Count = 3 });
          if (Container.Root.ConstructUnvalidated(leaf.Flatten()) is not Container.Leaf { Count: 3 }) throw new System.Exception();
        } }
        """),
		new("private fields require accessors while public fields remain data", """
        using SmartEnum;
        [SmartEnum<int>("Kind", FlatteningStrategy.Flat, KeyValueOnBranchMissing.FirstBranch)] public abstract partial class Root { }
        public sealed partial class Leaf : Root {
          public const int Kind = 1;
          private readonly int _secret = 7;
          public int Unmapped = 3;
          private readonly string _storage;
          [SmartEnumPrivateAccessor(nameof(_storage))] public string Name => _storage;
          public int ReadSecret() => _secret;
        }
        public static class RegressionProbe { public static void Verify() {
          var dto = new RootFlattened { Kind = 1, Name = "ok", Unmapped = 8 };
          var leaf = (Leaf)Root.ConstructUnvalidated(dto)!;
          if (leaf.Name != "ok" || leaf.ReadSecret() != 7 || leaf.Unmapped != 8) throw new System.Exception();
          if (typeof(RootFlattened).GetProperty("Secret") is not null || typeof(RootFlattened).GetProperty("Storage") is not null) throw new System.Exception();
          if (typeof(Root).GetMethod("MapDataToTypeUnvalidated") is not null || typeof(Root).Assembly.GetType("SmartEnum.SmartEnumSnapshotAttribute") is not null) throw new System.Exception();
        } }
        """),
		new("one argument attribute is removed", """
        using SmartEnum;
        [SmartEnum<int>("Kind")] public abstract partial class Root { }
        public partial class Leaf : Root { public const int Kind = 1; }
        """, "SMARTENUM015"),
		new("subclass hierarchy has independent mapping view and inherited context", """
        using SmartEnum;
        [SmartEnum<int>("Kind", FlatteningStrategy.Nested, KeyValueOnBranchMissing.FirstBranch)] public abstract partial class Status { public string Id { get; } }
        [SmartEnum<string>("ScheduledType", FlatteningStrategy.Nested, KeyValueOnBranchMissing.FirstBranch)] public abstract partial class Scheduled : Status { public const int Kind = 1; public string By { get; } }
        public sealed partial class Ready : Scheduled { public const string ScheduledType = "ready"; public int Count { get; } }
        public sealed partial class Cancelled : Status { public const int Kind = 2; public string? Reason { get; } }
        public static class RegressionProbe { public static void Verify() {
          var dto = new ScheduledFlattened { Id = "s", By = "Alice", ScheduledType = "ready", Ready = new() { Count = 9 } };
          var scheduled = Scheduled.ConstructUnvalidated(dto, () => "fallback", () => 0)!;
          if (scheduled is not Ready { Id: "s", By: "Alice", Count: 9 }) throw new System.Exception();
          var local = scheduled.FlattenHierarchy();
          if (local.ScheduledType != "ready" || local.Ready?.Count != 9 || local.Id != "s") throw new System.Exception();
          var outer = scheduled.Flatten();
          if (Status.ConstructUnvalidated(outer, () => "unused", () => 0) is not Ready { Count: 9 }) throw new System.Exception();
        } }
        """),
		new("Separate subclass hierarchy has typed flattened base", """
        using SmartEnum;
        [SmartEnum<int>("Kind", FlatteningStrategy.Separate, KeyValueOnBranchMissing.FirstBranch)] public abstract partial class Status { public int Id { get; } }
        [SmartEnum<string>("ScheduledType", FlatteningStrategy.Separate, KeyValueOnBranchMissing.FirstBranch)] public abstract partial class Scheduled : Status { public const int Kind = 1; }
        public sealed partial class Ready : Scheduled { public const string ScheduledType = "ready"; public int Count { get; } }
        public sealed partial class Cancelled : Status { public const int Kind = 2; }
        public static class RegressionProbe { public static void Verify() {
          Scheduled scheduled = Ready.ConstructUnvalidated(new ReadyFlattened { Id = 7, Kind = 1, ScheduledType = "ready", Count = 9 });
          ScheduledFlattened dto = scheduled.FlattenHierarchy();
          if (Scheduled.ConstructUnvalidated(dto) is not Ready { Count: 9 }) throw new System.Exception();
          if (Status.ConstructUnvalidated(dto) is not Ready { Id: 7 }) throw new System.Exception();
        } }
        """)
	];

	private static IEnumerable<RegressionCase> Boundaries()
	{
		foreach ((string type, string constant) in new[] { ("byte", "255"), ("sbyte", "-128"), ("short", "-32768"), ("ushort", "65535"), ("int", "int.MinValue"), ("uint", "uint.MaxValue"), ("long", "long.MinValue"), ("ulong", "ulong.MaxValue"), ("char", "'x'"), ("bool", "true"), ("float", "float.NaN"), ("double", "double.NaN"), ("string", "null!") })
		{
			foreach (string strategy in new[] { "Flat", "Nested", "Separate" })
			{
				string model = strategy == "Separate" ? "LeafFlattened" : "RootFlattened";
				yield return new($"{strategy} discriminator boundary {type}", $$"""
                using SmartEnum;
                [SmartEnum<{{type}}>("Kind", FlatteningStrategy.{{strategy}}, KeyValueOnBranchMissing.FirstBranch)] public abstract partial class Root { }
                public sealed partial class Leaf : Root { public const {{type}} Kind = {{constant}}; }
                public static class RegressionProbe { public static void Verify() {
                  var leaf = Leaf.ConstructUnvalidated(new {{model}} { Kind = {{constant}} });
                  if (Root.ConstructUnvalidated(leaf.Flatten()) is not Leaf) throw new System.Exception();
                } }
                """);
			}
		}
	}
}