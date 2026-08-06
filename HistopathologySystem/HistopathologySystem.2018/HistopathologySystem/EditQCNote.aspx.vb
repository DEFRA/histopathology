Partial Class EditQCNote
    Inherits System.Web.UI.Page
    Protected WithEvents VLAHeader1 As VLAHeader

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
        VLAHeader1.PageTitle = "Edit QC Note"
        CheckPermissions()
        VLAHeader1.SubmissioNoVisible() = False
        ValidateLength()

        If Not IsPostBack Then
            SetFocus(txtQCNoteText)
            PromptBeforeSaveScript("Are you sure you want to Cancel, any changes that have been made since you last pressed Done will be lost.  Continue?", btnCancel)
            Dim iQCNoteRef As Integer = Request.QueryString("QCNoteRef")
            Dim iSubmissionType As Integer = 0

            If iQCNoteRef <> 0 Then
                InitialiseHeader(iQCNoteRef, iSubmissionType)
                InitialiseMainAndFooterSection(iQCNoteRef, iSubmissionType)
                ctlDivChooseQCNote.Visible = False
            Else
                ctlDivChooseQCNote.Visible = True
                LoadLookupLists()
            End If
        End If

    End Sub

#Region "Lookuplists"

    Private Sub LoadLookupLists()
        Try
            Dim objQCNote As New HistopathologyLib.clsQCNote
            Dim dtData As New DataTable

            If Not objQCNote.GetAllQCNotes(dtData) Then
                Throw New Exception("QCNote.GetAllQCNotes returned false.")
            Else

                ddlQCNotes.DataSource = dtData
                ddlQCNotes.DataTextField = "ID"
                ddlQCNotes.DataValueField = "ID"
                ddlQCNotes.DataBind()
                AddItemToDropDownList(ddlQCNotes)
            End If
        Catch ex As Exception
            clsAppError.DisplayError("Failed to initialise lookup list.", ex)
        End Try
    End Sub

#End Region

#Region "Event Handlers"

    Private Sub btnCancel_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnCancel.Click
        Response.Redirect("QCNotes.aspx")
    End Sub

    Private Sub btnDone_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnDone.Click
        Dim bRedirect As Boolean = False
        Try
            Dim objQCNote As New HistopathologyLib.clsQCNote
            Dim objErrorlist As New ArrayList
            Dim bSuccess As Boolean = False

            bSuccess = objQCNote.UpdateQCNote(CInt(lblQCNoteRef.Text), _
                                              txtQCNoteText.Text, _
                                              Session.Item(SessionVars.SV_RowStamp), _
                                              CInt(Session.Item(SessionVars.SV_HeaderUserID)), _
                                              objErrorlist)

            If bSuccess Then
                If objErrorlist.Count = 0 Then
                    bRedirect = True
                Else
                    ctlDiv.InnerHtml = "<p><font color=""Red"">The database has been updated but some errors were encountered:</font></p><p>&nbsp;</p><p><font color=""Red"">" & Join(objErrorlist.ToArray, "</font></p><p>") & "</p>"
                End If
            Else
                ctlDiv.InnerHtml = "<p><font color=""Red"">The database has not been updated because the following error(s) occurred:</font></p><p>&nbsp;</p><p><font color=""Red"">" & Join(objErrorlist.ToArray, "</font></p><p>") & "</p>"
            End If

        Catch ex As Exception
            clsAppError.DisplayError("Failed to update QC Note " & lblQCNoteRef.Text & ".", ex)
        End Try

        If bRedirect Then
            Response.Redirect("QCNotes.aspx")
        End If
    End Sub

    Private Sub ddlQCNotes_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ddlQCNotes.SelectedIndexChanged
        If ddlQCNotes.SelectedIndex = 0 Then
            ClearFields()
        Else
            Dim iSubmissionType As Integer
            InitialiseHeader(CInt(ddlQCNotes.SelectedItem.Value), iSubmissionType)
            InitialiseMainAndFooterSection(CInt(ddlQCNotes.SelectedItem.Value), iSubmissionType)
        End If
    End Sub

    Private Sub VLAHeader1_HomeClick(ByVal sender As Object, ByVal e As HistopathologySystem.HomeLinkEventArgs) Handles VLAHeader1.HomeClick
        Dim sMessage As System.Text.StringBuilder = New System.Text.StringBuilder

        sMessage.Append("Any changes that have been made will be discarded, are you sure you wish to exit without saving?")
        Page.RegisterStartupScript("navigate", PromptBeforeNavigateScript(sMessage.ToString(), "Home.aspx"))
        e.bNavigateHome = False
    End Sub

#End Region

#Region "Private Functions"

    Private Sub CheckPermissions()
        VLAHeader1.getUserDetails()

        Dim sGroupName As String = Session(SessionVars.SV_HeaderGroupName)

        If sGroupName = "Customer" Then
            Response.Redirect("Home.aspx")
        ElseIf sGroupName = "Histopathology User" Then
            'nothing
        ElseIf sGroupName = "Maintenance" Then
            'Nothing
        Else
            Response.Redirect("Home.aspx")
        End If
    End Sub

    Private Sub InitialiseHeader(ByVal iQCNoteRef As Integer, ByRef iSubmissionType As Integer)
        Try
            Dim objQCNote As New HistopathologyLib.clsQCNote
            Dim dtData As New DataTable
            Dim sSubmittedArea As String

            If Not objQCNote.GetBatchQCNotes(dtData, iQCNoteRef) Then
                Throw New Exception("QCNote.GetBatchQCNotes returned false.")
            End If

            If Not dtData Is Nothing And dtData.Rows.Count > 0 Then
                lblQCNoteRef.Text = dtData.Rows(0)("QCNoteRef")
                lblSubmissionNumber.Text = dtData.Rows(0)("ID")
                lblStainRef.Text = dtData.Rows(0)("StainRef").ToString()
                lblSpecies.Text = dtData.Rows(0)("Species").ToString()
                iSubmissionType = dtData.Rows(0)("BatchType")
                lblProject.Text = GetListTypeID(dtData.Rows(0)("ProjectContractCode").ToString(), LOOKUP_PROJECTS)
            End If

        Catch ex As Exception
            clsAppError.DisplayError("Failed to initialise QC Note header.", ex)
        End Try
    End Sub

    Private Function InsertSpaces(ByVal iNumberSpaces As Integer) As String
        Dim iCount As Integer = 0
        Dim sString As String = ""

        For iCount = 0 To iNumberSpaces - 1
            sString = sString & " "
        Next

        Return sString
    End Function

    Private Sub InitialiseMainAndFooterSection(ByVal iQCNoteRef As Integer, ByVal iSubmissionType As Integer)
        Try
            Dim objQCNote As New HistopathologyLib.clsQCNote
            Dim dtData As New DataTable
            Dim drDataRow As DataRow
            Dim sTextBoxString As String

            If Not objQCNote.GetQCNoteTestInformation(dtData, iQCNoteRef, iSubmissionType) Then
                Throw New Exception("QCNote.GetQCNoteTestInformation returned false.")
            End If


            sTextBoxString = "Sender Ref" & InsertSpaces(22 - ("Sender Ref".Length)) & _
                             "Histo Ref" & InsertSpaces(22 - ("Histo Ref".Length)) & _
                             "Block Ref" & InsertSpaces(6) & _
                             "Test" & vbNewLine

            If Not dtData Is Nothing And dtData.Rows.Count > 0 Then
                If Not dtData.Rows(0)("QCText").ToString() <> "" Then
                    For Each drDataRow In dtData.Rows
                        lblCreatedBy.Text = dtData.Rows(0)("Name").ToString()
                        lblDateCreated.Text = Format(CDate(dtData.Rows(0)("DateCreated").ToString()), "Long Date")
                        sTextBoxString = sTextBoxString & drDataRow("SenderRef").ToString() & InsertSpaces(22 - (drDataRow("SenderRef").ToString.Length))
                        sTextBoxString = sTextBoxString & drDataRow("HistologyRef").ToString() & InsertSpaces(22 - (drDataRow("HistologyRef").ToString.Length))
                        sTextBoxString = sTextBoxString & drDataRow("BlockRef").ToString & InsertSpaces(15 - drDataRow("BlockRef").ToString.Length)
                        sTextBoxString = sTextBoxString & drDataRow("Description").ToString
                        sTextBoxString = sTextBoxString & vbNewLine
                    Next
                    txtQCNoteText.Text = sTextBoxString
                    Session.Item(SessionVars.SV_RowStamp) = drDataRow("RowStamp")
                Else
                    lblCreatedBy.Text = dtData.Rows(0)("Name").ToString()
                    lblDateCreated.Text = Format(CDate(dtData.Rows(0)("DateCreated").ToString()), "Long Date")
                    txtQCNoteText.Text = dtData.Rows(0)("QCText")
                    Session.Item(SessionVars.SV_RowStamp) = dtData.Rows(0)("RowStamp")
                End If
            End If

        Catch ex As Exception
            clsAppError.DisplayError("Failed to initialise QC Note main section.", ex)
        End Try
    End Sub

    Private Sub ClearFields()
        lblQCNoteRef.Text = ""
        lblSubmissionNumber.Text = ""
        lblProject.Text = ""
        lblSpecies.Text = ""
        lblStainRef.Text = ""
        txtQCNoteText.Text = ""
        lblCreatedBy.Text = ""
        lblDateCreated.Text = ""
    End Sub
#End Region

#Region "Validation"

    Private Sub valQCNoteText_ServerValidate(ByVal source As Object, ByVal e As ServerValidateEventArgs) Handles valQCNoteText.ServerValidate
        e.IsValid = txtQCNoteText.Text.Length <= 4000
    End Sub

    Private Sub ValidateLength()
        If HttpContext.Current.Request.Browser.JavaScript Then
            Dim scr As New System.Text.StringBuilder

            scr.Append("<SCRIPT language=""Javascript"">" + vbNewLine)
            scr.Append("function ValidateLength(sender, args)" + vbNewLine)
            scr.Append("{" + vbNewLine)
            scr.Append("   if (args.Value.length <= 4000){" + vbNewLine)
            scr.Append("      args.IsValid = true;" + vbNewLine)
            scr.Append("      return;" + vbNewLine)
            scr.Append("   }" + vbNewLine)
            scr.Append("   else{" + vbNewLine)
            scr.Append("      args.IsValid = false;" + vbNewLine)
            scr.Append("      return;" + vbNewLine)
            scr.Append("   }" + vbNewLine)
            scr.Append("}" + vbNewLine)
            scr.Append("</SCRIPT>")
            Me.Page.RegisterClientScriptBlock("LengthValidation", scr.ToString())
        End If

    End Sub

#End Region
End Class
