# ADR-004: Crystal Reports 13 for PDF Report Generation

**Date:** Discovered 2026-07-27  
**Status:** Implicit — established at application inception  
**Source:** `Web.config` assembly registrations, `HistologyReport.rpt` and 8 other `.rpt` files, companion `.vb` classes

---

## Context

The application must generate formatted PDF reports for submission details, histology records, QC notes, blocks, antibodies, special stains, and tissues.

## Decision

SAP Crystal Reports 13.0.3500.0 is used as the exclusive PDF/report generation library. Reports are defined as `.rpt` files using the Crystal Reports designer. At runtime, the companion `.vb` class loads the `.rpt` file, binds typed XSD datasets or programmatic data, and renders the report via the Crystal HTTP handler (`CrystalImageHandler.aspx`).

Four assemblies are registered in GAC and referenced in `Web.config`:
- `CrystalDecisions.CrystalReports.Engine`
- `CrystalDecisions.ReportSource`
- `CrystalDecisions.Shared`
- `CrystalDecisions.Web`

Three typed XSD datasets are used: `HistologyReportDataset.xsd`, `QCNoteDataset.xsd`, `SubmissionNotesDataset.xsd`.

One sub-report nesting exists: `HistologyReport.rpt` embeds `HistologySubReport.rpt`.

## Consequences

**Positive:**
- Rich designer-based report layout with precise formatting
- Typed datasets provide compile-time schema binding for three reports
- Proven stable for the application's current on-premises IIS environment

**Negative:**
- Crystal Reports 13 is installed via GAC — requires manual installation on every IIS server; no NuGet distribution
- Crystal Reports 13 is incompatible with .NET 5+ and .NET 10 — the DLLs have no .NET Core / .NET 5+ build
- `CrystalImageHandler.aspx` HTTP handler must be registered in `Web.config` and IIS — fails silently if absent
- `.rpt` files are binary format — not diffable in source control; changes require Crystal Reports designer
- SAP Crystal Reports 13 runtime is end-of-life for modern Windows Server versions; continued support risk on new OS versions
- Any Azure App Service migration requires replacing Crystal Reports entirely before the application can be deployed
