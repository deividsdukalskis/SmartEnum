namespace SmartEnum.Shared;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using System.Linq;

public static partial class EnumHierarchy
{
	private static Result<HierarchyError.ClassNotPartial> EnforcePartialClass(HierarchyData data)
	{
		bool isPartial = data.Type.DeclaringSyntaxReferences
			.Select(r => r.GetSyntax())
			.OfType<ClassDeclarationSyntax>()
			.Any(c => c.Modifiers.Any(SyntaxKind.PartialKeyword));

		if (isPartial)
		{
			return new Success<HierarchyError.ClassNotPartial>();
		}

		return new Failure<HierarchyError.ClassNotPartial>(new(data.Type));
	}
}
