internal static class ExternalBaseCases
{
	public static IEnumerable<RegressionCase> All
	{
		get
		{
			foreach (string strategy in new[] { "Flat", "Nested", "Separate" })
			{
				string model = strategy == "Separate" ? "LeafFlattened" : "RootFlattened";
				yield return new($"{strategy} ordinary generic ancestor properties round trip", $$"""
                    using SmartEnum;
                    public class Ancestor<T> { public T Value { get; } protected Ancestor(T value) { Value = value; } }
                    public class Ordinary<T> : Ancestor<T> {
                        public string Name { get; protected set; } = "initial";
                        public int Count { get; init; }
                        private readonly int _secret = 9;
                        public int ReadSecret() => _secret;
                        protected Ordinary(T value) : base(value) { }
                    }
                    [SmartEnum<int>("Kind", FlatteningStrategy.{{strategy}}, KeyValueOnBranchMissing.FirstBranch)] public abstract partial class Root<T> : Ordinary<T> { }
                    public sealed partial class Leaf<U> : Root<U> { public const int Kind = 1; }
                    public static class RegressionProbe { public static void Verify() {
                        var leaf = Leaf<string>.ConstructUnvalidated(new {{model}}<string> { Kind = 1, Value = "value", Name = "name", Count = 7 });
                        var dto = leaf.Flatten();
                        if (dto.Value != "value" || dto.Name != "name" || dto.Count != 7 || leaf.ReadSecret() != 9) throw new System.Exception();
                        if (Root<string>.ConstructUnvalidated(dto) is not Leaf<string> { Value: "value", Name: "name", Count: 7 }) throw new System.Exception();
                        if (typeof(RootFlattened<string>).GetProperty("Secret") is not null) throw new System.Exception();
                    } }
                    """);
			}

			yield return new("ordinary base with inaccessible constructor diagnosed", """
                using SmartEnum;
                public class Ordinary { private Ordinary() { } }
                [SmartEnum<int>("Kind", FlatteningStrategy.Flat, KeyValueOnBranchMissing.FirstBranch)] public abstract partial class Root : Ordinary { }
                public sealed partial class Leaf : Root { public const int Kind = 1; }
                """, "SMARTENUM019");
			yield return new("ordinary required properties and optional constructor arguments", """
                using SmartEnum;
                public class Ordinary {
                    public required string Name { get; init; }
                    public required int Count;
                    [SmartEnumIgnore] public int Computed => 7;
                    protected Ordinary(int unused = 3) { }
                }
                [SmartEnum<int>("Kind", FlatteningStrategy.Flat, KeyValueOnBranchMissing.FirstBranch)] public abstract partial class Root : Ordinary { }
                public sealed partial class Leaf : Root { public const int Kind = 1; }
                public static class RegressionProbe { public static void Verify() {
                    var dto = Leaf.ConstructUnvalidated(new RootFlattened { Kind = 1, Name = "ok", Count = 6 }).Flatten();
                    if (dto.Name != "ok" || dto.Count != 6) throw new System.Exception();
                    if (typeof(RootFlattened).GetProperty("Computed") is not null) throw new System.Exception();
                } }
                """);
			yield return new("ordinary base private getter diagnosed", """
                using SmartEnum;
                public class Ordinary { public int Value { private get; set; } }
                [SmartEnum<int>("Kind", FlatteningStrategy.Flat, KeyValueOnBranchMissing.FirstBranch)] public abstract partial class Root : Ordinary { }
                public sealed partial class Leaf : Root { public const int Kind = 1; }
                """, "SMARTENUM027");
			yield return new("ordinary base reserved API name diagnosed", """
                using SmartEnum;
                public class Ordinary { public int Flatten() => 1; }
                [SmartEnum<int>("Kind", FlatteningStrategy.Flat, KeyValueOnBranchMissing.FirstBranch)] public abstract partial class Root : Ordinary { }
                public sealed partial class Leaf : Root { public const int Kind = 1; }
                """, "SMARTENUM032");
			yield return new("ordinary base property normalized collision diagnosed", """
                using SmartEnum;
                public class Ordinary { public int _value { get; set; } }
                [SmartEnum<int>("Kind", FlatteningStrategy.Flat, KeyValueOnBranchMissing.FirstBranch)] public abstract partial class Root : Ordinary { public int Value { get; } }
                public sealed partial class Leaf : Root { public const int Kind = 1; }
                """, "SMARTENUM011");
			yield return new("ordinary base property survives restarted subgroup model", """
                using SmartEnum;
                public class Ordinary { public int Id { get; } protected Ordinary(int id) { Id = id; } }
                [SmartEnum<int>("Kind", FlatteningStrategy.Nested, KeyValueOnBranchMissing.FirstBranch)] public abstract partial class Root : Ordinary { }
                [SmartEnum<int>("Subkind", FlatteningStrategy.Nested, KeyValueOnBranchMissing.FirstBranch)] public abstract partial class Group : Root { public const int Kind = 1; }
                public sealed partial class Leaf : Group { public const int Subkind = 2; }
                public static class RegressionProbe { public static void Verify() {
                    var leaf = Group.ConstructUnvalidated(new GroupFlattened { Id = 9, Subkind = 2 })!;
                    if (leaf.Id != 9 || leaf.FlattenHierarchy().Id != 9 || Root.ConstructUnvalidated(leaf.Flatten())?.Id != 9) throw new System.Exception();
                } }
                """);
			yield return new("reusable generic parent initializes ordinary base", """
                using SmartEnum;
                public class Ordinary<T> { public T Identity { get; } protected Ordinary(T identity) { Identity = identity; } }
                [SmartEnumParent(FlatteningStrategy.Separate)] public sealed partial class Owner<T> : Ordinary<T> { public Root Status { get; } }
                [SmartEnum<int>("Kind", FlatteningStrategy.Flat, KeyValueOnBranchMissing.FirstBranch)] public abstract partial class Root { }
                public sealed partial class Leaf : Root { public const int Kind = 1; }
                public static class RegressionProbe { public static void Verify() {
                    var owner = Owner<string>.ConstructUnvalidated(new OwnerFlattened<string> { Identity = "id", Status = new() { Kind = 1 } }, () => throw new System.Exception());
                    if (owner.Identity != "id" || owner.Flatten(value => value.Flatten()).Identity != "id") throw new System.Exception();
                } }
                """);
			yield return new("ordinary read-only property without restoration diagnosed", """
                using SmartEnum;
                public class Ordinary { public int Value { get; } }
                [SmartEnum<int>("Kind", FlatteningStrategy.Flat, KeyValueOnBranchMissing.FirstBranch)] public abstract partial class Root : Ordinary { }
                public sealed partial class Leaf : Root { public const int Kind = 1; }
                """, "SMARTENUM019");
			yield return new("ordinary base custom root constructor restores differently named argument", """
                using SmartEnum;
                public class Ordinary { public int Value { get; } protected Ordinary(int initial) { Value = initial; } }
                [SmartEnum<int>("Kind", FlatteningStrategy.Flat, KeyValueOnBranchMissing.FirstBranch)] public abstract partial class Root : Ordinary { protected Root(int value) : base(value) { } }
                public sealed partial class Leaf : Root { public const int Kind = 1; }
                public static class RegressionProbe { public static void Verify() {
                    if (Leaf.ConstructUnvalidated(new RootFlattened { Kind = 1, Value = 5 }).Flatten().Value != 5) throw new System.Exception();
                } }
                """);
			foreach (string strategy in new[] { "Flat", "Nested", "Separate" })
			{
				yield return new($"{strategy} parent includes ordinary base properties", $$"""
                    using SmartEnum;
                    public class Ordinary { public int Id { get; } protected Ordinary(int id) { Id = id; } }
                    [SmartEnumParent(FlatteningStrategy.{{strategy}})] public sealed partial class Owner : Ordinary { public Root Status { get; } }
                    [SmartEnum<int>("Kind", FlatteningStrategy.Separate, KeyValueOnBranchMissing.FirstBranch)] public abstract partial class Root { }
                    public sealed partial class Leaf : Root { public const int Kind = 1; }
                    public static class RegressionProbe { public static void Verify() {
                        var owner = Owner.ConstructUnvalidated(new OwnerFlattened { Id = 7, Status = new LeafFlattened { Kind = 1 } }, () => throw new System.Exception());
                        if (owner.Id != 7 || owner.Flatten().Id != 7) throw new System.Exception();
                    } }
                    """);
			}
		}
	}
}