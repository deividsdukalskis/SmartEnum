internal static class SeparateCases
{
	public static IEnumerable<RegressionCase> All
		=> [
			new("separate variants share abstract DTO and preserve complete typed data", """
				using SmartEnum;
				[SmartEnum<string>("Kind", FlatteningStrategy.Separate, KeyValueOnBranchMissing.FirstBranch)]
				public abstract partial class Root { [SmartEnumIdentity("EntityId")] public string Id { get; } }
				public sealed partial class First : Root { public const string Kind = "first"; public int Count { get; } }
				public sealed partial class Second : Root { public const string Kind = "second"; public string Name { get; } }
				public static class RegressionProbe { public static void Verify() {
				 Root entity = First.ConstructUnvalidated(new FirstFlattened { Id = "id", Kind = "first", Count = 4 });
				 RootFlattened flattened = entity.Flatten();
				 FirstFlattened concrete = ((First)entity).Flatten();
				 if (!typeof(RootFlattened).IsAbstract || flattened is not FirstFlattened { Id: "id", Count: 4, Kind: "first" }) throw new System.Exception();
				 if (typeof(FirstFlattened).GetProperty("Name") is not null || typeof(FirstFlattened).GetProperty("Count")!.PropertyType != typeof(int)) throw new System.Exception();
				 if (Root.ConstructUnvalidated(flattened) is not First { Id: "id", Count: 4 }) throw new System.Exception();
				 concrete.Kind = "second";
				 if (Root.ConstructUnvalidated(concrete) is not null) throw new System.Exception();
				} }
				"""),
			new("separate ragged branches and explicit nullable fallback", """
				using SmartEnum;
				[SmartEnum<string>("Kind", FlatteningStrategy.Separate, KeyValueOnBranchMissing.FirstBranch)]
				[SmartEnum<int>("Subkind", FlatteningStrategy.Separate, KeyValueOnBranchMissing.SetNull)]
				public abstract partial class Root { public string Id { get; } }
				public abstract partial class Scheduled : Root { public const string Kind = "scheduled"; public string By { get; } }
				public partial class Ready : Scheduled { public const int Subkind = 1; public int Count { get; } }
				public partial class Cancelled : Root { public const string Kind = "cancelled"; [SmartEnumNullableProperty] public System.DateTime At { get; } }
				public static class RegressionProbe { public static void Verify() {
				 int calls = 0;
				 var expected = new System.DateTime(2026, 10, 13);
				 var cancelled = Cancelled.ConstructUnvalidated(new CancelledFlattened { Id = "id", Kind = "cancelled" }, () => { calls++; return expected; });
				 if (calls != 1 || cancelled.At != expected || cancelled.Flatten().Subkind is not null) throw new System.Exception();
				 if (Root.ConstructUnvalidated(cancelled.Flatten(), () => throw new System.Exception()) is not Cancelled { At: var at } || at != expected) throw new System.Exception();
				 var ready = Ready.ConstructUnvalidated(new ReadyFlattened { Id = "ready", Kind = "scheduled", Subkind = 1, By = "Alice", Count = 2 });
				 if (Root.ConstructUnvalidated(ready.Flatten(), () => throw new System.Exception()) is not Ready { By: "Alice", Count: 2 }) throw new System.Exception();
				} }
				"""),
			new("separate private accessor keeps property name and storage type", """
				using SmartEnum;
				using System.Collections.Generic;
				[SmartEnum<int>("Kind", FlatteningStrategy.Separate, KeyValueOnBranchMissing.FirstBranch)]
				public abstract partial class Root { }
				public sealed partial class Leaf : Root {
				 public const int Kind = 1;
				 private readonly List<string> _attendees = new();
				 [SmartEnumPrivateAccessor(nameof(_attendees))] public IReadOnlyList<string> Attendees => _attendees.AsReadOnly();
				}
				public static class RegressionProbe { public static void Verify() {
				 var leaf = Leaf.ConstructUnvalidated(new LeafFlattened { Kind = 1, Attendees = new() { "Alice" } });
				 if (leaf.Attendees[0] != "Alice" || leaf.Flatten().Attendees[0] != "Alice" || typeof(LeafFlattened).GetProperty("Attendees")!.PropertyType != typeof(List<string>)) throw new System.Exception();
				} }
				"""),
			new("separate generic renamed parameters", """
				using SmartEnum;
				[SmartEnum<int>("Kind", FlatteningStrategy.Separate, KeyValueOnBranchMissing.FirstBranch)]
				public abstract partial class Root<T> where T : class { public T Value { get; } }
				public partial class Leaf<U> : Root<U> where U : class { public const int Kind = 1; public U Extra { get; } }
				public static class RegressionProbe { public static void Verify() {
				 var leaf = Leaf<string>.ConstructUnvalidated(new LeafFlattened<string> { Kind = 1, Value = "root", Extra = "leaf" });
				 if (Root<string>.ConstructUnvalidated(leaf.Flatten()) is not Leaf<string> { Value: "root", Extra: "leaf" }) throw new System.Exception();
				} }
				"""),
			new("separate missing key substitute called only for short branch", """
				using SmartEnum;
				[SmartEnum<int>("Kind", FlatteningStrategy.Separate, KeyValueOnBranchMissing.FirstBranch)]
				[SmartEnum<string>("Subkind", FlatteningStrategy.Separate, KeyValueOnBranchMissing.PromptSubstitute)]
				public abstract partial class Root { }
				public partial class Short : Root { public const int Kind = 1; }
				public abstract partial class Long : Root { public const int Kind = 2; }
				public partial class Leaf : Long { public const string Subkind = "leaf"; }
				public static class RegressionProbe { public static void Verify() {
				 if (Short.ConstructUnvalidated(new ShortFlattened { Kind = 1 }).Flatten(() => "missing").Subkind != "missing") throw new System.Exception();
				 if (Leaf.ConstructUnvalidated(new LeafFlattened { Kind = 2, Subkind = "leaf" }).Flatten(() => throw new System.Exception()).Subkind != "leaf") throw new System.Exception();
				} }
				"""),
			new("separate implemented getter requires accessor", """
				using SmartEnum;
				[SmartEnum<int>("Kind", FlatteningStrategy.Separate, KeyValueOnBranchMissing.FirstBranch)] public abstract partial class Root { }
				public partial class Leaf : Root { public const int Kind = 1; public int Weight => 2; }
				""", "SMARTENUM027"),
			new("separate invalid private accessor diagnosed", """
				using SmartEnum;
				[SmartEnum<int>("Kind", FlatteningStrategy.Separate, KeyValueOnBranchMissing.FirstBranch)] public abstract partial class Root { }
				public partial class Leaf : Root { public const int Kind = 1; [SmartEnumPrivateAccessor("missing")] public int Weight => 2; }
				""", "SMARTENUM028"),
			new("separate FirstBranch on later discriminator diagnosed", """
				using SmartEnum;
				[SmartEnum<int>("Kind", FlatteningStrategy.Separate, KeyValueOnBranchMissing.FirstBranch)]
				[SmartEnum<int>("Subkind", FlatteningStrategy.Separate, KeyValueOnBranchMissing.FirstBranch)] public abstract partial class Root { }
				public partial class Leaf : Root { public const int Kind = 1; }
				""", "SMARTENUM023")
		];
}