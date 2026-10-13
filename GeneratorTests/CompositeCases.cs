internal static class CompositeCases
{
	public static IEnumerable<RegressionCase> All
		=> [
			new("explicit nullable parent container delegates receive missing data", """
			using SmartEnum;
			using System.Collections.Generic;
			[SmartEnumParent(FlatteningStrategy.Separate)] public sealed partial class Owner { [SmartEnumNullableProperty] public List<Root> Items { get; } }
			[SmartEnum<int>("Kind", FlatteningStrategy.Flat, KeyValueOnBranchMissing.FirstBranch)] public abstract partial class Root { }
			public sealed partial class Leaf : Root { public const int Kind = 1; }
			public static class RegressionProbe { public static void Verify() {
			 int calls = 0;
			 var owner = Owner.ConstructUnvalidated(new OwnerFlattened { Items = null }, items => { if (items is not null) throw new System.Exception(); calls++; return new List<Root>(); });
			 if (calls != 1 || owner.Items.Count != 0 || owner.Flatten(items => null).Items is not null) throw new System.Exception();
			} }
			"""),
			new("malformed private accessor does not crash", """
			using SmartEnum;
			[SmartEnum<int>("Kind", FlatteningStrategy.Flat, KeyValueOnBranchMissing.FirstBranch)] public abstract partial class Root { }
			public partial class Leaf : Root { public const int Kind = 1; [SmartEnumPrivateAccessor] public int Count => 1; }
			""", "SMARTENUM015"),
			new("internal flat model and required members", """
			using SmartEnum;
			internal class Payload { }
			[SmartEnum<int>("Kind", FlatteningStrategy.Flat, KeyValueOnBranchMissing.FirstBranch)] internal abstract partial class Root { public required Payload Data { get; init; } }
			internal sealed partial class Leaf : Root { public const int Kind = 1; public required int Count { get; init; } }
			public static class RegressionProbe { public static void Verify() {
			 var value = new Payload();
			 var leaf = Leaf.ConstructUnvalidated(new RootFlattened { Kind = 1, Data = value, Count = 3 });
			 if (leaf.Count != 3 || !object.ReferenceEquals(leaf.Flatten().Data, value)) throw new System.Exception();
			} }
			"""),
			new("nested uses standalone ancestor identity", """
			using SmartEnum;
			[SmartEnum<int>("Kind", FlatteningStrategy.Nested, KeyValueOnBranchMissing.FirstBranch)] public abstract partial class Root { [SmartEnumIdentity("EntityId")] public int Id { get; } }
			public sealed partial class Leaf : Root { public const int Kind = 1; public int Count { get; } }
			public static class RegressionProbe { public static void Verify() {
			 var leaf = Leaf.ConstructUnvalidated(new RootFlattened { Id = 5, Kind = 1, Leaf = new() { Count = 3 } }, () => 0);
			 if (leaf.Flatten().Leaf?.EntityId != 5) throw new System.Exception();
			} }
			"""),
			new("malformed parent attribute does not crash", """
			using SmartEnum;
			[SmartEnumParent] public sealed partial class Owner { }
			""", "SMARTENUM015"),
			new("repeated owner hierarchy diagnosed", """
			using SmartEnum;
			[SmartEnumParent(FlatteningStrategy.Flat)] public sealed partial class Owner { public Root First { get; } public Root Second { get; } }
			[SmartEnum<int>("Kind", FlatteningStrategy.Flat, KeyValueOnBranchMissing.FirstBranch)] public abstract partial class Root { }
			public partial class Leaf : Root { public const int Kind = 1; }
			""", "SMARTENUM015"),
			new("separate parent arrays retain shape and nullable direct references", """
			using SmartEnum;
			using System.Linq;
			[SmartEnumParent(FlatteningStrategy.Separate)] public sealed partial class Owner { [SmartEnumIdentity("OwnerId")] public int Id { get; } public Root[] Items { get; } }
			[SmartEnum<int>("Kind", FlatteningStrategy.Separate, KeyValueOnBranchMissing.FirstBranch)] public abstract partial class Root { }
			public sealed partial class Leaf : Root { public const int Kind = 1; public int Count { get; } }
			[SmartEnumParent(FlatteningStrategy.Separate)] public sealed partial class OptionalOwner { public Other? Value { get; } }
			[SmartEnum<int>("Kind", FlatteningStrategy.Flat, KeyValueOnBranchMissing.FirstBranch)] public abstract partial class Other { }
			public sealed partial class OtherLeaf : Other { public const int Kind = 1; }
			public static class RegressionProbe { public static void Verify() {
			 var owner = Owner.ConstructUnvalidated(new OwnerFlattened { Id = 9, Items = new RootFlattened[] { new LeafFlattened { Kind = 1, Count = 4 } } }, items => items.Select(item => Root.MapFromFlattenedDataUnvalidated(item)!).ToArray());
			 var result = owner.Flatten(args => args.Items.Select(item => item.Flatten(args.OwnerId)).ToArray());
			 if (result.Items[0] is not LeafFlattened { Count: 4, OwnerId: 9 }) throw new System.Exception();
			 var optional = OptionalOwner.ConstructUnvalidated(new OptionalOwnerFlattened(), () => null);
			 if (optional.Flatten().Value is not null) throw new System.Exception();
			} }
			"""),
			new("parent with two distinct direct hierarchies", """
			using SmartEnum;
			[SmartEnumParent(FlatteningStrategy.Flat)] public sealed partial class Owner { public First A { get; } public Second B { get; } }
			[SmartEnum<int>("FirstKind", FlatteningStrategy.Flat, KeyValueOnBranchMissing.FirstBranch)] public abstract partial class First { }
			public sealed partial class FirstLeaf : First { public const int FirstKind = 1; public string FirstValue { get; } }
			[SmartEnum<int>("SecondKind", FlatteningStrategy.Nested, KeyValueOnBranchMissing.FirstBranch)] public abstract partial class Second { }
			public sealed partial class SecondLeaf : Second { public const int SecondKind = 2; public int SecondValue { get; } }
			public static class RegressionProbe { public static void Verify() {
			 var owner = Owner.ConstructUnvalidated(new OwnerFlattened { FirstKind = 1, FirstValue = "a", SecondKind = 2, SecondLeaf = new() { SecondValue = 7 } }, () => 8, () => throw new System.Exception(), () => throw new System.Exception());
			 var flat = owner.Flatten();
			 if (flat.FirstValue != "a" || flat.SecondLeaf?.SecondValue != 7 || flat.FirstKind != 1 || flat.SecondKind != 2) throw new System.Exception();
			} }
			"""),
			new("parent can reference a concrete variant and recover from unknown keys", """
			using SmartEnum;
			[SmartEnumParent(FlatteningStrategy.Flat)] public sealed partial class Owner { public Leaf Value { get; } }
			[SmartEnum<int>("Kind", FlatteningStrategy.Flat, KeyValueOnBranchMissing.FirstBranch)] public abstract partial class Root { }
			public sealed partial class Leaf : Root { public const int Kind = 1; public int Count { get; } }
			public static class RegressionProbe { public static void Verify() {
			 int calls = 0;
			 var fallback = Leaf.ConstructUnvalidated(new OwnerFlattened { Count = 8 });
			 var owner = Owner.ConstructUnvalidated(new OwnerFlattened { Kind = 99 }, () => { calls++; return fallback; });
			 if (calls != 1 || owner.Flatten().Count != 8) throw new System.Exception();
			} }
			"""),
			new("composite normalizes private data and discriminator names", """
			using SmartEnum;
			[SmartEnum<int>("type_id", FlatteningStrategy.Flat, KeyValueOnBranchMissing.FirstBranch)] public abstract partial class Root { private readonly int _base_count; public int Read() => _base_count; }
			public sealed partial class Leaf : Root { public const int type_id = 1; private readonly string _display_name; public string Name() => _display_name; }
			public static class RegressionProbe { public static void Verify() {
			 var leaf = (Leaf)Root.MapFromFlattenedDataUnvalidated(new RootFlattened { TypeId = 1, BaseCount = 4, DisplayName = "ok" })!;
			 if (leaf.Read() != 4 || leaf.Name() != "ok" || leaf.Flatten().DisplayName != "ok") throw new System.Exception();
			} }
			"""),
			new("nested JSON transfer preserves explicit zero and null", """
			using SmartEnum;
			using System.Text.Json;
			[SmartEnum<int>("Kind", FlatteningStrategy.Nested, KeyValueOnBranchMissing.FirstBranch)] public abstract partial class Root { }
			public sealed partial class Leaf : Root { public const int Kind = 1; public int Number { get; } public string? Text { get; } }
			public static class RegressionProbe { public static void Verify() {
			 var dto = JsonSerializer.Deserialize<RootFlattened>(JsonSerializer.Serialize(new RootFlattened { Kind = 1, Leaf = new() { Number = 0, Text = null } }))!;
			 var leaf = (Leaf)Root.MapFromFlattenedDataUnvalidated(dto, () => throw new System.Exception())!;
			 if (leaf.Number != 0 || leaf.Text is not null || leaf.Flatten().Leaf?.Number != 0) throw new System.Exception();
			 try { Root.MapFromFlattenedDataUnvalidated(null!, () => 0); throw new System.Exception(); } catch (System.ArgumentNullException) { }
			} }
			"""),
			new("flat missing discriminator invokes substitute only on short branch", """
			using SmartEnum;
			[SmartEnum<int>("Kind", FlatteningStrategy.Flat, KeyValueOnBranchMissing.FirstBranch)]
			[SmartEnum<string>("Detail", FlatteningStrategy.Flat, KeyValueOnBranchMissing.PromptSubstitute)] public abstract partial class Root { }
			public sealed partial class Short : Root { public const int Kind = 1; }
			public abstract partial class Long : Root { public const int Kind = 2; }
			public sealed partial class Leaf : Long { public const string Detail = "leaf"; }
			public static class RegressionProbe { public static void Verify() {
			 if (Short.ConstructUnvalidated(new()).Flatten(() => "absent").Detail != "absent") throw new System.Exception();
			 if (Leaf.ConstructUnvalidated(new()).Flatten(() => throw new System.Exception()).Detail != "leaf") throw new System.Exception();
			} }
			"""),
			new("flat normalized data collision diagnosed", """
			using SmartEnum;
			[SmartEnum<int>("Kind", FlatteningStrategy.Flat, KeyValueOnBranchMissing.FirstBranch)] public abstract partial class Root { public int FooBar { get; } }
			public partial class Leaf : Root { public const int Kind = 1; private int _foo_bar; }
			""", "SMARTENUM015"),
			new("nested foreign key collision diagnosed", """
			using SmartEnum;
			[SmartEnumParent(FlatteningStrategy.Nested)] public sealed partial class Owner { [SmartEnumIdentity("OwnerId")] public int Id { get; } public Root Value { get; } }
			[SmartEnum<int>("Kind", FlatteningStrategy.Nested, KeyValueOnBranchMissing.FirstBranch)] public abstract partial class Root { public int OwnerId { get; } }
			public partial class Leaf : Root { public const int Kind = 1; }
			""", "SMARTENUM015"),
			new("parent must be sealed", """
			using SmartEnum;
			[SmartEnumParent(FlatteningStrategy.Flat)] public partial class Owner { public Root Value { get; } }
			[SmartEnum<int>("Kind", FlatteningStrategy.Flat, KeyValueOnBranchMissing.FirstBranch)] public abstract partial class Root { }
			public partial class Leaf : Root { public const int Kind = 1; }
			""", "SMARTENUM015"),
			new("parent without hierarchy diagnosed", """
			using SmartEnum;
			[SmartEnumParent(FlatteningStrategy.Flat)] public sealed partial class Owner { public string Name { get; } }
			""", "SMARTENUM015"),
			new("ambiguous nested sibling keys diagnosed", """
			using SmartEnum;
			[SmartEnum<int>("Kind", FlatteningStrategy.Nested, KeyValueOnBranchMissing.FirstBranch)]
			[SmartEnum<int>("Detail", FlatteningStrategy.Nested, KeyValueOnBranchMissing.SetNull)] public abstract partial class Root { }
			public abstract partial class Branch : Root { public const int Kind = 1; }
			public partial class A : Branch { public const int Detail = 2; }
			public partial class B : Branch { public const int Detail = 2; }
			""", "SMARTENUM015"),
			new("flat preserves unused values and calls required fallbacks only when absent", """
			using SmartEnum;
			[SmartEnum<int>("Kind", FlatteningStrategy.Flat, KeyValueOnBranchMissing.FirstBranch)]
			public abstract partial class Root { public int Id { get; } }
			public sealed partial class A : Root { public const int Kind = 1; public string Name { get; } }
			public sealed partial class B : Root { public const int Kind = 2; [SmartEnumNullableProperty] public int Count { get; } }
			public static class RegressionProbe { public static void Verify() {
			 var original = new RootFlattened { Id = 4, Kind = 2, Name = "retained", Count = null };
			 var b = (B)Root.MapFromFlattenedDataUnvalidated(original, () => 7)!;
			 var result = b.Flatten(() => throw new System.Exception());
			 if (result.Name != "retained" || result.Count != 7 || result.Id != 4) throw new System.Exception();
			 result.Kind = 1;
			 var a = (A)Root.MapFromFlattenedDataUnvalidated(result, () => throw new System.Exception())!;
			 if (a.Flatten(() => "unused").Count != 7) throw new System.Exception();
			 var fresh = B.ConstructUnvalidated(new RootFlattened { Count = 3 }, () => 9);
			 if (fresh.Flatten(() => "fallback").Name != "fallback") throw new System.Exception();
			} }
			"""),
			new("nested standalone restores missing objects and preserves inactive branches", """
			using SmartEnum;
			[SmartEnum<int>("Kind", FlatteningStrategy.Nested, KeyValueOnBranchMissing.FirstBranch)]
			public abstract partial class Root { }
			public sealed partial class A : Root { public const int Kind = 1; public int Number { get; } }
			public sealed partial class B : Root { public const int Kind = 2; public string Name { get; } }
			public static class RegressionProbe { public static void Verify() {
			 var input = new RootFlattened { Kind = 1, B = new() { Name = "keep" } };
			 var a = Root.MapFromFlattenedDataUnvalidated(input, () => 12, () => "unused")!;
			 var result = a.Flatten();
			 if (result.A?.Number != 12 || result.B?.Name != "keep") throw new System.Exception();
			 result.Kind = 99;
			 if (Root.MapFromFlattenedDataUnvalidated(result, () => 0, () => "") is not null) throw new System.Exception();
			} }
			"""),
			new("flat owner mixed hierarchy and generated foreign keys", """
			using SmartEnum;
			[SmartEnumParent(FlatteningStrategy.Flat)] public sealed partial class Meeting {
			 [SmartEnumIdentity("MeetingId")] public string Id { get; } public string Title { get; } public Status Status { get; }
			}
			[SmartEnum<int>("Kind", FlatteningStrategy.Flat, KeyValueOnBranchMissing.FirstBranch)]
			[SmartEnum<int>("Detail", FlatteningStrategy.Nested, KeyValueOnBranchMissing.SetNull)]
			public abstract partial class Status { }
			public abstract partial class Scheduled : Status { public const int Kind = 1; public string By { get; } }
			public sealed partial class Ready : Scheduled { public const int Detail = 1; public int Count { get; } }
			public sealed partial class Cancelled : Status { public const int Kind = 2; [SmartEnumNullableProperty] public int At { get; } }
			public static class RegressionProbe { public static void Verify() {
			 var dto = new MeetingFlattened { Id = "m1", Title = "title", Kind = 1, Detail = 1, By = "Alice", Ready = new() { Count = 8 } };
			 var meeting = Meeting.ConstructUnvalidated(dto, () => 9, () => 10, () => throw new System.Exception());
			 var flat = meeting.Flatten(() => "unused");
			 if (flat.Id != "m1" || flat.Title != "title" || flat.Ready?.Count != 8 || flat.Ready.MeetingId != "m1") throw new System.Exception();
			 flat.Kind = 2;
			 var cancelled = Meeting.ConstructUnvalidated(flat, () => 20, () => 30, () => throw new System.Exception());
			 if (cancelled.Flatten(() => throw new System.Exception()).Ready?.Count != 8) throw new System.Exception();
			} }
			"""),
			new("nested parent with restarted discriminator group", """
			using SmartEnum;
			[SmartEnumParent(FlatteningStrategy.Nested)] public sealed partial class Meeting {
			 [SmartEnumIdentity("MeetingId")] public string Id { get; } public Status Status { get; }
			}
			[SmartEnum<int>("Kind", FlatteningStrategy.Nested, KeyValueOnBranchMissing.FirstBranch)]
			public abstract partial class Status { }
			[SmartEnum<int>("ScheduledKind", FlatteningStrategy.Nested, KeyValueOnBranchMissing.FirstBranch)]
			public abstract partial class Scheduled : Status { public const int Kind = 1; public string By { get; } }
			public sealed partial class Ready : Scheduled { public const int ScheduledKind = 2; public int Count { get; } }
			public sealed partial class Cancelled : Status { public const int Kind = 3; }
			public static class RegressionProbe { public static void Verify() {
			 var dto = new MeetingFlattened { Id = "m", Kind = 1, Status = new() { Scheduled = new() { ScheduledKind = 2, By = "A", Ready = new() { Count = 4 } } } };
			 var meeting = Meeting.ConstructUnvalidated(dto, () => "fallback", () => 5, () => throw new System.Exception());
			 var flat = meeting.Flatten();
			 if (flat.Status?.Scheduled?.Ready?.Count != 4 || flat.Status.MeetingId != "m" || flat.Status.Scheduled.Ready.MeetingId != "m") throw new System.Exception();
			} }
			"""),
			new("parent collection callbacks retain shape and owner identity", """
			using SmartEnum;
			using System.Collections.Generic;
			using System.Linq;
			[SmartEnumParent(FlatteningStrategy.Flat)] public sealed partial class Meeting {
			 [SmartEnumIdentity("MeetingId")] public string Id { get; }
			 private readonly List<Status> _statuses;
			 [SmartEnumPrivateAccessor(nameof(_statuses))] public IReadOnlyList<Status> Statuses => _statuses.AsReadOnly();
			}
			[SmartEnum<int>("Kind", FlatteningStrategy.Flat, KeyValueOnBranchMissing.FirstBranch)] public abstract partial class Status { }
			public sealed partial class Ready : Status { public const int Kind = 1; [SmartEnumNullableProperty] public int Count { get; } }
			public static class RegressionProbe { public static void Verify() {
			 var dto = new MeetingFlattened { Id = "m", Statuses = new() { new() { Kind = 1 } } };
			 var meeting = Meeting.ConstructUnvalidated(dto, () => 6, args => args.Flattened.Select(item => Status.MapFromFlattenedDataUnvalidated(item, args.MapCount)!).ToList());
			 var flat = meeting.Flatten(args => args.Statuses.Select(item => item.Flatten(args.MeetingId)).ToList());
			 if (flat.Statuses[0].Count != 6 || flat.Statuses[0].MeetingId != "m") throw new System.Exception();
			} }
			"""),
			new("separate parent retains independent polymorphic property", """
			using SmartEnum;
			[SmartEnumParent(FlatteningStrategy.Separate)] public sealed partial class Meeting {
			 [SmartEnumIdentity("MeetingId")] public string Id { get; } public Status Status { get; }
			}
			[SmartEnum<int>("Kind", FlatteningStrategy.Separate, KeyValueOnBranchMissing.FirstBranch)] public abstract partial class Status { }
			public sealed partial class Ready : Status { public const int Kind = 1; public int Count { get; } }
			public sealed partial class Cancelled : Status { public const int Kind = 2; public string Reason { get; } }
			public static class RegressionProbe { public static void Verify() {
			 var meeting = Meeting.ConstructUnvalidated(new MeetingFlattened { Id = "m", Status = new ReadyFlattened { Kind = 1, Count = 5 } }, () => throw new System.Exception());
			 if (meeting.Flatten().Status is not ReadyFlattened { Count: 5, MeetingId: "m" }) throw new System.Exception();
			} }
			""")
		];
}