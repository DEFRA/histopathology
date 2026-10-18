-- ============================================================================
-- V20261018_01_Drop_GetOrCreateSubmissionQCNote_Proc
-- ============================================================================
-- Removes the "one QC note per submission" stored procedure. QC notes are no
-- longer reused/locked per submission — each save that raises a QC note now
-- creates an independent note via dbo.AddQCNote directly (Histo.QualityControl
-- QCNoteService.AddAsync), matching legacy's per-save-action note creation.
-- Idempotent — safe to re-run.
-- ============================================================================
IF OBJECT_ID('[dbo].[GetOrCreateSubmissionQCNote]', 'P') IS NOT NULL
        DROP PROCEDURE [dbo].[GetOrCreateSubmissionQCNote];
GO

PRINT 'Dropped stored procedure dbo.GetOrCreateSubmissionQCNote (if it existed)';
