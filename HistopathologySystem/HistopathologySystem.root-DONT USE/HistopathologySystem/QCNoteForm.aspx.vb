Partial Class QCNoteForm
    Inherits System.Web.UI.Page

#Region " Web Form Designer Generated Code "

    'This call is required by the Web Form Designer.
    <System.Diagnostics.DebuggerStepThrough()> Private Sub InitializeComponent()

    End Sub

    Private Sub Page_Init(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Init
        'CODEGEN: This method call is required by the Web Form Designer
        'Do not modify it using the code editor.
        InitializeComponent()
    End Sub

#End Region

    Private Sub Page_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load

        If Not IsPostBack Then
            Dim iQCNoteRef As Integer = Request.QueryString("QCNoteRef")

            If iQCNoteRef <> 0 Then
                CreateReport(iQCNoteRef)
            End If
        End If
    End Sub

    Private Sub CreateReport(ByVal iQCNoteRef As Integer)
        Try
            Dim rptQCNote As New QCNote()
            Dim dsReportDataset As New DataSet()

            Dim objQCNote As New HistopathologyLib.clsQCNote()
            If Not objQCNote.CreateReportDataset(iQCNoteRef, dsReportDataset) Then
                Throw New Exception("QCNote.CreateReportDataset returned false.")
            End If

            rptQCNote.SetDataSource(dsReportDataset)

            'Printing code (export to PDF)
            Dim crExportOptions As CrystalDecisions.Shared.ExportOptions
            Dim crDiskFileDestinationOptions As CrystalDecisions.Shared.DiskFileDestinationOptions
            Dim Fname As String

            Fname = System.Configuration.ConfigurationSettings.AppSettings("Exports") & Session.SessionID.ToString & "QCNote.pdf"
            crDiskFileDestinationOptions = New CrystalDecisions.Shared.DiskFileDestinationOptions()
            crDiskFileDestinationOptions.DiskFileName = Fname
            crExportOptions = rptQCNote.ExportOptions
            With crExportOptions
                .DestinationOptions = crDiskFileDestinationOptions
                .ExportDestinationType = CrystalDecisions.[Shared].ExportDestinationType.DiskFile
                .ExportFormatType = CrystalDecisions.[Shared].ExportFormatType.PortableDocFormat
            End With
            rptQCNote.Export()

            'Display report in browser
            With Response
                .ClearContent()
                .ClearHeaders()
                .ContentType = "application/pdf"
                .WriteFile(Fname)
                .Flush()
                .Close()
            End With

            'Delete the report once it is displayed in the browser
            System.IO.File.Delete(Fname)
        Catch ex As Exception
            clsAppError.DisplayError("Unable to create QC Note report " & CStr(iQCNoteRef) & ".", ex)
        End Try
    End Sub
End Class

