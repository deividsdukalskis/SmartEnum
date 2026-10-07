internal static class PrivateFieldCases
{
	public static IEnumerable<RegressionCase> All
	{
		get
		{
			yield return new("private mutable readonly and nullable fields", """
				using SmartEnum;
				[SmartEnum<int>("Id")] public abstract partial class Root { }
				public partial class Leaf : Root {
				 public const int Id = 1;
				 private const int DefaultCount = 7;
				 private static string shared = "unchanged";
				 private int _count = DefaultCount;
				 private readonly string _name;
				 private string? _note;
				 public string Label { get; }
				 public bool Matches(string name, int count, string? note) => _name == name && _count == count && _note == note && shared == "unchanged";
				}
				public static class RegressionProbe { public static void Verify() {
				 Leaf direct = Leaf.ConstructUnvalidated(Label: "direct", _count: 42, _name: "Alice", _note: null);
				 if (!direct.Matches("Alice", 42, null) || direct.Label != "direct") throw new System.Exception();
				 Root mapped = Root.MapDataToTypeUnvalidated(Id: 1, Label: "mapped", _count: 10, _name: "Bob", _note: "note");
				 if (mapped is not Leaf leaf || !leaf.Matches("Bob", 10, "note")) throw new System.Exception();
				 if (!typeof(Leaf).GetField("_name", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.IsPrivate) throw new System.Exception();
				} }
				""");
			yield return new("private fields across three inheritance levels", """
				using SmartEnum;
				[SmartEnum<int>("Id")][SmartEnum<int>("SubtypeId")]
				public abstract partial class Root {
				 private readonly string _rootName;
				 public string RootName => _rootName;
				}
				public abstract partial class Middle : Root {
				 public const int Id = 1;
				 private readonly int _middleNumber;
				 public int MiddleNumber => _middleNumber;
				}
				public partial class Leaf : Middle {
				 public const int SubtypeId = 2;
				 private bool _leafFlag;
				 public bool LeafFlag => _leafFlag;
				}
				public static class RegressionProbe { public static void Verify() {
				 Leaf direct = Leaf.ConstructUnvalidated(_rootName: "direct", _middleNumber: 3, _leafFlag: true);
				 if (direct.RootName != "direct" || direct.MiddleNumber != 3 || !direct.LeafFlag) throw new System.Exception();
				 if (Root.MapDataToTypeUnvalidated(Id: 1, SubtypeId: 2, _rootName: "mapped", _middleNumber: 4, _leafFlag: true)
				  is not Leaf { RootName: "mapped", MiddleNumber: 4, LeafFlag: true }) throw new System.Exception();
				} }
				""");
			yield return new("generic private fields", """
				using SmartEnum;
				[SmartEnum<int>("Id")] public abstract partial class Root<T> {
				 private readonly T _rootValue;
				 public T RootValue => _rootValue;
				}
				public partial class Leaf<U> : Root<U> {
				 public const int Id = 1;
				 private readonly U _leafValue;
				 public U LeafValue => _leafValue;
				}
				public static class RegressionProbe { public static void Verify() {
				 if (Root<string>.MapDataToTypeUnvalidated(Id: 1, _rootValue: "root", _leafValue: "leaf")
				  is not Leaf<string> { RootValue: "root", LeafValue: "leaf" }) throw new System.Exception();
				} }
				""");
			yield return new("private fields assigned after setters regardless of name order", """
				using SmartEnum;
				[SmartEnum<int>("Id")] public abstract partial class Root { }
				public partial class Leaf : Root {
				 public const int Id = 1;
				 private string A = "";
				 public string Name { get => A; private set => A = value.ToUpperInvariant(); }
				}
				public static class RegressionProbe { public static void Verify() {
				 if (Leaf.ConstructUnvalidated(A: "raw", Name: "domain").Name != "raw") throw new System.Exception();
				} }
				""");
			yield return new("private field escaped identifier", """
				using SmartEnum;
				[SmartEnum<int>("Id")] public abstract partial class Root { }
				public partial class Leaf : Root {
				 public const int Id = 1;
				 private readonly string? @event;
				 public string? Value => @event;
				}
				public static class RegressionProbe { public static void Verify() {
				 if (Leaf.ConstructUnvalidated(@event: "value").Value != "value") throw new System.Exception();
				 if (Root.MapDataToTypeUnvalidated(Id: 1, @event: null) is not Leaf { Value: null }) throw new System.Exception();
				} }
				""");
			yield return new("existing private field constructor reused", """
				using SmartEnum;
				[SmartEnum<int>("Id")] public abstract partial class Root { }
				public partial class Leaf : Root {
				 public const int Id = 1;
				 private readonly string _value;
				 private Leaf(string value) { _value = value.ToUpperInvariant(); }
				 public string Value => _value;
				}
				public static class RegressionProbe { public static void Verify() {
				 if (Leaf.ConstructUnvalidated(_value: "stored").Value != "STORED") throw new System.Exception();
				} }
				""");
			yield return new("duplicate inherited private field names diagnosed", """
				using SmartEnum;
				[SmartEnum<int>("Id")] public abstract partial class Root { private int _value; public int RootValue => _value; }
				public partial class Leaf : Root { public const int Id = 1; private int _value; public int LeafValue => _value; }
				""", "SMARTENUM012");
			yield return new("private field types must be accessible to factories", """
				using SmartEnum;
				[SmartEnum<int>("Id")] public abstract partial class Root { }
				public partial class Leaf : Root {
				 public const int Id = 1;
				 private class Secret { }
				 private Secret _secret;
				}
				""", "SMARTENUM012");
			yield return new("incompatible private field types across branches diagnosed", """
				using SmartEnum;
				[SmartEnum<int>("Id")] public abstract partial class Root { }
				public partial class First : Root { public const int Id = 1; private int _value; public int FirstValue => _value; }
				public partial class Second : Root { public const int Id = 2; private string _value; public string SecondValue => _value; }
				""", "SMARTENUM012");
		}
	}
}