namespace SmartEnum.Shared;

using System.Linq;
using Microsoft.CodeAnalysis;

public static partial class EnumHierarchy
{
	private static Result<(IFieldSymbol Field, ITypeSymbol IntendedType), HierarchyError.KeyFieldNotDefined> EnforceKeyFieldDefined(HierarchyData.DerivedTypeData data)
	{
		string? keyName = data.RelevantAttribute.ConstructorArguments.FirstOrDefault().Value as string;
		ITypeSymbol? intendedKeyType = data.RelevantAttribute.AttributeClass?.TypeArguments.FirstOrDefault();
		IFieldSymbol? field = data.Type.GetMembers(keyName ?? string.Empty).OfType<IFieldSymbol>().FirstOrDefault();

		if (intendedKeyType is null || field is null)
		{
			return new Failure<(IFieldSymbol Field, ITypeSymbol IntendedType), HierarchyError.KeyFieldNotDefined>(new(data.Type, keyName ?? string.Empty, intendedKeyType?.ToDisplayString() ?? string.Empty));
		}

		return new Success<(IFieldSymbol Field, ITypeSymbol IntendedType), HierarchyError.KeyFieldNotDefined>((field, intendedKeyType));
	}
}