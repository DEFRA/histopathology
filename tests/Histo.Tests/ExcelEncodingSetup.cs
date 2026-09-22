using System.Runtime.CompilerServices;
using System.Text;

namespace Histo.Tests;

/// <summary>ExcelDataReader needs codepage 1252 registered on .NET, not just Windows-only builds.</summary>
internal static class ExcelEncodingSetup
{
    [ModuleInitializer]
    internal static void Register() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
}
