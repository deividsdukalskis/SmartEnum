namespace SmartEnum.Shared;

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

public abstract class HierarchyError
{
	public class DuplicateAttributesFound : HierarchyError
	{
		public INamedTypeSymbol Type { get; }
		public INamedTypeSymbol ParentType { get; }

		public DuplicateAttributesFound(INamedTypeSymbol type, INamedTypeSymbol parentType)
		{
			this.Type = type;
			this.ParentType = parentType;
		}
	}

	public class DuplicatePropertyDefinitionsFound : HierarchyError
	{
		public ImmutableArray<IGrouping<(string CommonParentTypeName, IPropertySymbol CurrentTypeProperty), string>> PropertiesWithSameName { get; }

		public DuplicatePropertyDefinitionsFound(ImmutableArray<IGrouping<(string CommonParentTypeName, IPropertySymbol CurrentTypeProperty), string>> propertiesWithSameName) => this.PropertiesWithSameName = propertiesWithSameName;
	}

	public class InvalidKeyName : HierarchyError
	{
		public INamedTypeSymbol Type { get; }
		public ImmutableArray<(AttributeData Attribute, string ProvidedName)> AttributesWithInvalidKeyNames { get; }

		public InvalidKeyName(INamedTypeSymbol type, ImmutableArray<(AttributeData Attribute, string ProvidedName)> attributesWithInvalidKeyNames)
		{
			this.Type = type;
			this.AttributesWithInvalidKeyNames = attributesWithInvalidKeyNames;
		}
	}

	public class ClassNotAbstract : HierarchyError
	{
		public INamedTypeSymbol Type { get; }

		public ClassNotAbstract(INamedTypeSymbol type) => this.Type = type;
	}

	public class ClassNotPartial : HierarchyError
	{
		public INamedTypeSymbol Type { get; }

		public ClassNotPartial(INamedTypeSymbol type) => this.Type = type;
	}

	public class KeyFieldNotDefined : HierarchyError
	{
		public INamedTypeSymbol Type { get; }
		public string KeyName { get; }
		public string KeyType { get; }

		public KeyFieldNotDefined(INamedTypeSymbol type, string keyName, string keyType)
		{
			this.Type = type;
			this.KeyName = keyName;
			this.KeyType = keyType;
		}
	}

	public class KeyFieldNotPublic : HierarchyError
	{
		public INamedTypeSymbol Type { get; }
		public IFieldSymbol Field { get; }

		public KeyFieldNotPublic(INamedTypeSymbol type, IFieldSymbol field)
		{
			this.Type = type;
			this.Field = field;
		}
	}

	public class KeyFieldNotConst : HierarchyError
	{
		public INamedTypeSymbol Type { get; }
		public IFieldSymbol Field { get; }

		public KeyFieldNotConst(INamedTypeSymbol type, IFieldSymbol field)
		{
			this.Type = type;
			this.Field = field;
		}
	}

	public class KeyFieldInvalidType : HierarchyError
	{
		public INamedTypeSymbol Type { get; }
		public IFieldSymbol Field { get; }
		public ITypeSymbol IntendedType { get; }
		public ITypeSymbol ProvidedType { get; }

		public KeyFieldInvalidType(INamedTypeSymbol type, IFieldSymbol field, ITypeSymbol intendedType, ITypeSymbol providedType)
		{
			this.Type = type;
			this.Field = field;
			this.IntendedType = intendedType;
			this.ProvidedType = providedType;
		}
	}

	public class ClassIsAbstract : HierarchyError
	{
		public INamedTypeSymbol Type { get; }

		public ClassIsAbstract(INamedTypeSymbol type) => this.Type = type;
	}

	public class DuplicateKeyNamesFound : HierarchyError
	{
		public INamedTypeSymbol Type { get; }
		public ImmutableArray<string> DuplicateKeyNames { get; }

		public DuplicateKeyNamesFound(INamedTypeSymbol type, ImmutableArray<string> duplicateKeyNames)
		{
			this.Type = type;
			this.DuplicateKeyNames = duplicateKeyNames;
		}
	}
}