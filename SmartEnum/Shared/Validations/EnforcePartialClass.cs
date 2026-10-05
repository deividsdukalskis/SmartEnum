namespace SmartEnum.Shared;

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public abstract partial class ValidatedHierarchyData
{
	private static Result<HierarchyError.ClassNotPartial> EnforcePartialClass(HierarchyData data)
	{
		bool isPartial = data.Type.DeclaringSyntaxReferences
			.Select(r => r.GetSyntax())
			.OfType<TypeDeclarationSyntax>()
			.Any(c => c.Modifiers.Any(SyntaxKind.PartialKeyword));

		return isPartial ? new Success<HierarchyError.ClassNotPartial>() : new Failure<HierarchyError.ClassNotPartial>(new(data.Type));
	}
}