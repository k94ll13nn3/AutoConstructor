using Microsoft.CodeAnalysis.Text;

namespace AutoConstructor.Generator.Models;

/// <summary>
/// Basic diagnostic description for reporting diagnostic inside the incremental pipeline.
/// </summary>
/// <param name="FilePath">Path of the file</param>
/// <param name="TextSpan">Span of the diagnostic</param>
/// <param name="LineSpan">Line span of the diagnostic</param>
/// <see href="https://github.com/dotnet/roslyn/issues/62269#issuecomment-1170760367" />
internal sealed record ReportedDiagnostic(string FilePath, TextSpan TextSpan, LinePositionSpan LineSpan);
