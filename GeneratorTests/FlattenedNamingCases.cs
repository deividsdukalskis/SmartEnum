internal static class FlattenedNamingCases
{
	public static IEnumerable<RegressionCase> All
		=> [
			new("flattened names normalize fields properties snapshots and discriminators", """
				using SmartEnum;
				using System.Text.Json;
				[SmartEnum<int>("_kind_id")] public abstract partial class Root {
				 private readonly string __first_name__;
				 public int itemCount { get; }
				 public string URL_VALUE { get; }
				 [SmartEnumSnapshot] private string display_name => __first_name__;
				}
				public partial class Leaf : Root { public const int _kind_id = 1; }
				public static class RegressionProbe { public static void Verify() {
				 var flat = Leaf.ConstructUnvalidated(URL_VALUE: "url", __first_name__: "Alice", itemCount: 2).Flatten();
				 if (flat.KindId != 1 || flat.FirstName != "Alice" || flat.ItemCount != 2 || flat.UrlValue != "url" || flat.DisplayName != "Alice") throw new System.Exception();
				 foreach (var property in typeof(RootFlattened).GetProperties())
				  if (!char.IsUpper(property.Name[0]) || property.Name.Contains("_")) throw new System.Exception();
				 var json = JsonSerializer.Serialize(flat);
				 if (!json.Contains("\"FirstName\"" ) || json.Contains("first_name")) throw new System.Exception();
				 var restored = Root.MapFromFlattenedDataUnvalidated(JsonSerializer.Deserialize<RootFlattened>(json)!);
				 if (restored.Flatten().FirstName != "Alice" || restored.itemCount != 2) throw new System.Exception();
				 if (Root.MapDataToTypeUnvalidated(_kind_id: 1, URL_VALUE: "url", __first_name__: "legacy", itemCount: 3).Flatten().FirstName != "legacy") throw new System.Exception();
				} }
				"""),
			new("normalized discriminator collision gets PascalCase Key prefix", """
				using SmartEnum;
				[SmartEnum<int>("kind_id")] public abstract partial class Root { private readonly int _kind_id; }
				public partial class Leaf : Root { public const int kind_id = 1; }
				public static class RegressionProbe { public static void Verify() {
				 var flat = Leaf.ConstructUnvalidated(42).Flatten();
				 if (flat.KeyKindId != 1 || flat.KindId != 42 || Root.MapFromFlattenedDataUnvalidated(flat).Flatten().KindId != 42) throw new System.Exception();
				} }
				"""),
			new("backing field and property normalized collision diagnosed", """
				using SmartEnum;
				[SmartEnum<int>("Id")] public abstract partial class Root { private string _name = ""; public string Name { get => _name; set => _name = value; } }
				public partial class Leaf : Root { public const int Id = 1; }
				""", "SMARTENUM012"),
			new("sibling normalized collision diagnosed", """
				using SmartEnum;
				[SmartEnum<int>("Id")] public abstract partial class Root { }
				public partial class Leaf : Root { public const int Id = 1; private int _value; }
				public partial class Other : Root { public const int Id = 2; public int value { get; } }
				""", "SMARTENUM012"),
			new("all underscore field cannot form property name", """
				using SmartEnum;
				[SmartEnum<int>("Id")] public abstract partial class Root { private int ___; }
				public partial class Leaf : Root { public const int Id = 1; }
				""", "SMARTENUM012"),
			new("normalized partial customization collision diagnosed", """
				using SmartEnum;
				[SmartEnum<int>("Id")] public abstract partial class Root { private string _name; }
				public partial class RootFlattened { public string Name { get; set; } = ""; }
				public partial class Leaf : Root { public const int Id = 1; }
				""", "SMARTENUM012")
		];
}