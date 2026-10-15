namespace SmartEnum.Generators;

using System;
using System.Threading;
using Microsoft.CodeAnalysis;

internal sealed class GenerationContext
{
	private readonly Action<string, string> addSource;
	private readonly Action<Diagnostic> reportDiagnostic;
	public CancellationToken CancellationToken { get; }
	public GenerationContext(CancellationToken cancellationToken, Action<string, string> addSource, Action<Diagnostic> reportDiagnostic)
		=> (this.CancellationToken, this.addSource, this.reportDiagnostic) = (cancellationToken, addSource, reportDiagnostic);
	public void AddSource(string name, string source) => this.addSource(name, source);
	public void ReportDiagnostic(Diagnostic diagnostic) => this.reportDiagnostic(diagnostic);
}