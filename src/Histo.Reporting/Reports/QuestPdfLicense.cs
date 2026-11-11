using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using QuestPDF.Infrastructure;

namespace Histo.Reporting.Reports;

internal static class QuestPdfLicense
{
    // Runs once on assembly load, before any Document.Create() call; avoids a per-renderer static constructor.
    [SuppressMessage("Usage", "CA2255", Justification = "Licence must be set before the first QuestPDF call from any renderer.")]
    [ModuleInitializer]
    internal static void Initialise() => QuestPDF.Settings.License = LicenseType.Professional;
}
