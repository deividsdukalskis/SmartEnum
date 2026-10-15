internal static class ReusableParentCases
{
	public static IEnumerable<RegressionCase> All
	{
		get
		{
			yield return new("inherited parent data and automatic direct nested callbacks", """
                using SmartEnum;
                public class Ordinary { public string Title { get; set; } = ""; public System.Collections.Generic.List<int> Points = new(); }
                [SmartEnumParent(FlatteningStrategy.Separate)] public sealed partial class Owner : Ordinary { public Root<int>? Optional { get; } public Root<string> Value { get; } }
                [SmartEnum<int>("Kind", FlatteningStrategy.Nested, KeyValueOnBranchMissing.FirstBranch)] public abstract partial class Root<T> { }
                public sealed partial class Leaf<T> : Root<T> { public const int Kind = 1; public int Count { get; } }
                public static class RegressionProbe { public static void Verify() {
                    int countCalls = 0;
                    var owner = Owner.ConstructUnvalidated(new OwnerFlattened { Title = "inherited", Points = new() { 7 }, Value = new() { Kind = 1 } },
                        mapValue: () => throw new System.Exception(), mapOptionalCount: () => throw new System.Exception(), mapValueCount: () => { countCalls++; return 9; });
                    var dto = owner.Flatten(mapOptional: value => value?.Flatten(), mapValue: value => value.Flatten());
                    if (countCalls != 1 || dto.Value.Leaf?.Count != 9 || dto.Optional is not null || dto.Title != "inherited" || dto.Points[0] != 7) throw new System.Exception();
                    int fallbackCalls = 0;
                    foreach (var data in new RootFlattened<string>?[] { null, new() { Kind = 99 } }) {
                        var restored = Owner.ConstructUnvalidated(new OwnerFlattened { Value = data! }, mapValue: () => { fallbackCalls++; return owner.Value; },
                            mapOptionalCount: () => throw new System.Exception(), mapValueCount: () => throw new System.Exception());
                        if (restored.Value != owner.Value) throw new System.Exception();
                    }
                    if (fallbackCalls != 2) throw new System.Exception();
                } }
                """);
			foreach (string strategy in new[] { "Flat", "Nested", "Separate" })
			{
				string model = strategy == "Separate" ? "LeafFlattened" : "RootFlattened";
				yield return new($"reusable generic {strategy} hierarchy in multiple owners and properties", $$"""
                    using SmartEnum;
                    using System.Linq;
                    [SmartEnum<int>("Kind", FlatteningStrategy.{{strategy}}, KeyValueOnBranchMissing.FirstBranch)]
                    public abstract partial class Root<T> { public T Value { get; } }
                    public sealed partial class Leaf<U> : Root<U> { public const int Kind = 1; }
                    [SmartEnumParent(FlatteningStrategy.Separate)]
                    public sealed partial class Owner<T> {
                        public Root<T> First { get; }
                        public Root<int> Second { get; }
                        private readonly Root<T>[] _items;
                        [SmartEnumPrivateAccessor(nameof(_items))] public System.Collections.Generic.IReadOnlyList<Root<T>> Items => _items;
                    }
                    [SmartEnumParent(FlatteningStrategy.Separate)]
                    public sealed partial class OtherOwner { public Root<string>? Optional { get; } }
                    public static class RegressionProbe { public static void Verify() {
                        var source = new OwnerFlattened<string> { First = new {{model}}<string> { Kind = 1, Value = "first" }, Second = new {{model}}<int> { Kind = 1, Value = 42 }, Items = new RootFlattened<string>[] { new {{model}}<string> { Kind = 1, Value = "item" } } };
                        var owner = Owner<string>.ConstructUnvalidated(source, mapFirst: () => throw new System.Exception(), mapSecond: () => throw new System.Exception(), mapItems: data => data.Select(item => Root<string>.ConstructUnvalidated(item)!).ToArray());
                        var result = owner.Flatten(mapFirst: value => value.Flatten(), mapSecond: value => value.Flatten(), mapItems: values => values.Select(value => value.Flatten()).ToArray());
                        if (result.First.Value != "first" || result.Second.Value != 42 || result.Items.Single().Value != "item") throw new System.Exception();
                        var other = OtherOwner.ConstructUnvalidated(new OtherOwnerFlattened());
                        if (other.Flatten(mapOptional: value => value?.Flatten()).Optional is not null) throw new System.Exception();
                    } }
                    """);
			}

			yield return new("repeated non-generic hierarchy keeps property conversions independent", """
                using SmartEnum;
                [SmartEnum<int>("Kind", FlatteningStrategy.Flat, KeyValueOnBranchMissing.FirstBranch)] public abstract partial class Root { public int Value { get; } }
                public sealed partial class Leaf : Root { public const int Kind = 1; }
                [SmartEnumParent(FlatteningStrategy.Separate)] public sealed partial class Owner { public Root First { get; } public Root Second { get; } }
                public static class RegressionProbe { public static void Verify() {
                    var owner = Owner.ConstructUnvalidated(new OwnerFlattened { First = new() { Kind = 1, Value = 4 }, Second = new() { Kind = 1, Value = 8 } }, () => throw new System.Exception(), () => throw new System.Exception());
                    var result = owner.Flatten(value => value.Flatten(), value => value.Flatten());
                    if (result.First.Value != 4 || result.Second.Value != 8) throw new System.Exception();
                    int calls = 0; var missing = Owner.ConstructUnvalidated(new OwnerFlattened { First = new() { Kind = 1 }, Second = new() { Kind = 99 } }, () => throw new System.Exception(), () => { calls++; return owner.Second; }); if (calls != 1 || missing.Second != owner.Second) throw new System.Exception();
                } }
                """);
		}
	}
}