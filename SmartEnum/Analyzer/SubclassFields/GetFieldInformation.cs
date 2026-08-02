namespace SmartEnum.Analyzer;

using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

public static partial class SubclassFieldAnalyzerExtensions
{
	public static 
		(SymbolAnalysisContext Context,
		IFieldSymbol? FieldInfo,
		ITypeSymbol IntendedFieldType,
		string IntendedFieldName)?
			GetFieldInformation(this (SymbolAnalysisContext Context, INamedTypeSymbol SmartEnumAttribute) tuple)
	{
		INamedTypeSymbol type = (INamedTypeSymbol)tuple.Context.Symbol;
		AttributeData? baseTypeEnumAttribute = type
			.BaseType?
			.GetAttributes()
			.FirstOrDefault(attrib => SymbolEqualityComparer.Default.Equals(attrib.AttributeClass?.OriginalDefinition, tuple.SmartEnumAttribute));

		if (baseTypeEnumAttribute is null)
		{
			return null;
		}

		object? keyName = baseTypeEnumAttribute.ConstructorArguments.FirstOrDefault().Value;
		if (keyName is not string name)
		{
			return null;
		}

		IFieldSymbol? fieldInfo = type.GetMembers(name).FirstOrDefault(member => member.Kind is SymbolKind.Field) as IFieldSymbol;
		ITypeSymbol? intendedKeyType = baseTypeEnumAttribute.AttributeClass?.TypeArguments.FirstOrDefault();
		if (intendedKeyType is null)
		{
			return null;
		}

		return (tuple.Context, fieldInfo, intendedKeyType, name);
	}
}