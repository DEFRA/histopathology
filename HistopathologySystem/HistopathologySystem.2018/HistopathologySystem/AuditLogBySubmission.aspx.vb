Partial Class AuditLogBySubmission
    Inherits System.Web.UI.Page

    Protected WithEvents VLAHeader1 As VLAHeader
    Protected WithEvents lblAnd1 As System.Web.UI.WebControls.Label
    Protected WithEvents ResultsPager As DataGridPager
    Protected WithEvents ctlStartDate As CalendarDate
    Protected WithEvents ctlEndDate As CalendarDate

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
        VLAHeader1.PageTitle = "Submission Audit Log Report"
        SetCalendarDateHandler(Me.Page)
        CheckPermissions()
        ctlStartDate.SetCalendarFocus()
        ResultsPager.SetGrid(grdResults)

        SetTextboxDefaultButton(txtSubmissionID, btnSearch)
        ctlStartDate.SetControlOnEnter(ctlEndDate.FirstClientID)
        ctlEndDate.SetControlOnEnter(txtSubmissionID.ClientID)
        VLAHeader1.SubmissioNoVisible() = False

        If Not IsPostBack Then
            grdResults.Visible = False
            ResultsPager.Visible = False
            hlbExcel.Visible = False
        End If
    End Sub

    Private Sub CheckPermissions()
        VLAHeader1.GetUserDetails()

        Dim sGroupName As String = Session(SessionVars.SV_HeaderGroupName)

        If sGroupName = "Customer" Then
            Response.Redirect("Home.aspx")
        ElseIf sGroupName = "Histopathology User" Then
            Response.Redirect("Home.aspx")
        ElseIf sGroupName = "Maintenance" Then
            'Nothing
        Else
            Response.Redirect("Home.aspx")
        End If
    End Sub

    Private Function ValidateMandatoryFields() As Boolean
        Try
            rfvSubmissionID.Validate()
            revSubmissionID.Validate()

            If Not rfvSubmissionID.IsValid Or _
            Not revSubmissionID.IsValid Or _
            Not ctlStartDate.IsComplete Or _
            Not ctlEndDate.IsComplete Then
                ctlDiv.InnerHtml = "<p><font color=""Red"">Not all mandatory fields have been completed, hover the mouse pointer over the red stars for details.</font></p>"
                Return False
            End If

            Return True
        Catch ex As Exception
            clsAppError.DisplayError("Failed to validate Mandatory fields.", ex)
        End Try
    End Function

    Private Sub btnSearch_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnSearch.Click

        ctlDiv.InnerHtml = ""

        If ValidateMandatoryFields() Then
            If Not IsDateRangeValid(ctlStartDate, ctlEndDate, "log entry date") Then
                grdResults.Visible = False
                ResultsPager.Visible = False
                hlbExcel.Visible = False
                Exit Sub
            End If

            Dim objAuditLog As New HistopathologyLib.clsAuditLog
            Dim dtResultsData As DataTable
            Dim dvResultsData As DataView

            If objAuditLog.GetSubmissionAuditLogReport(txtSubmissionID.Text, _
                                                       ctlStartDate.DateField.ToString, _
                                                       ctlEndDate.DateField.ToString, _
                                                       dtResultsData) Then

                dtResultsData.TableName = "SubmissionAuditLogResults"
                dvResultsData = dtResultsData.DefaultView

                Session.Item(SessionVars.SV_ExcelExport) = dtResultsData
                Session.Item(SessionVars.SV_ExcelExportView) = dvResultsData

                With grdResults
                    .Visible = True
                    .DataSource = dtResultsData
                    .DataKeyField = "ID"
                    .CurrentPageIndex = 0
                    .SelectedIndex = -1
                    .EditItemIndex = -1
                    .DataBind()
                End With

                With ResultsPager
                    .Visible = True
                    .DataTableSessionID = SessionVars.SV_ExcelExport
                    .DataViewSessionID = SessionVars.SV_ExcelExportView
                    .AllowAddNew = False
                    .AllowDelete = False
                    .AllowEdit = False
                    .PageLinkCount = 10
                    .Refresh()
                End With

                hlbExcel.Visible = True
            End If
        End If


    End Sub

    Private Sub btnAuditLogMenu_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnAuditLogMenu.Click
        Response.Redirect("AuditLogMenu.aspx")
    End Sub
End Class
