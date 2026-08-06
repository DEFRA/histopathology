Imports System.Text.RegularExpressions

Partial Class BatchBlockSummary
    Inherits System.Web.UI.Page
    Protected WithEvents SummaryGridPager As DataGridPager
    Protected WithEvents VLAHeader1 As VLAHeader
    Private m_bContinueEditing As Boolean = Nothing

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
        VLAHeader1.PageTitle = "Sample Summary"
        SummaryGridPager.SetGrid(grdBatchSummary)
        SetClientValidation()

        If Not IsPostBack Then
            DisplayDetails()
            InitialiseSummaryGrid()
            EnableDisableButtons(False)
            SetToolTips()
            DisplayNumberSamples()
            PromptBeforeSaveScript("Are you sure you want to delete the selected sample?", btnDeleteSubmission)
        End If

    End Sub


#Region "Populate Summary Grid"

    Private Sub InitialiseSummaryGrid()
        Try
            Try
                Dim dsDataSet As DataSet = CType(Session.Item(SessionVars.SV_BatchDetails), DataSet)
                Dim objSummary As New HistopathologyLib.clsBatchSummary
                Dim dtSummary As New DataTable
                Dim dtTissuesList As DataTable = Common.GetLookupTypeList(LOOKUP_TISSUE_CODE)
                Dim iCurrentPage As Integer = CInt(Session.Item(SessionVars.SV_BatchBlockSummaryPage))

                If Not objSummary.CreateSenderHistoRefData(dsDataSet, dtSummary) Then
                    Throw New Exception("BatchSummary.CreateSenderHistoRefData return false")
                End If

                Dim dv As DataView = dtSummary.DefaultView

                Session.Item(SessionVars.SV_BlockSummaryTable) = dtSummary
                Session.Item(SessionVars.SV_BlockSummaryView) = dv

                grdBatchSummary.DataSource = dtSummary
                grdBatchSummary.DataKeyField = "NewID"
                grdBatchSummary.DataBind()
                grdBatchSummary.CurrentPageIndex = iCurrentPage
                grdBatchSummary.SelectedIndex = -1
                grdBatchSummary.EditItemIndex = -1
                grdBatchSummary.Enabled = True
                grdBatchSummary.DataBind()

                SummaryGridPager.DataTableSessionID = SessionVars.SV_BlockSummaryTable
                SummaryGridPager.DataViewSessionID = SessionVars.SV_BlockSummaryView
                SummaryGridPager.PageLinkCount = 10
                SummaryGridPager.AllowAddNew = False
                SummaryGridPager.AllowEdit = False
                SummaryGridPager.AllowDelete = False

                SummaryGridPager.AllowHierarchicalEdit = Not CType(Session.Item(SessionVars.SV_ViewSubmission), Boolean)
                SummaryGridPager.ShowPageCount = True

                SummaryGridPager.DisplayPageNavControls(True)
                SummaryGridPager.Refresh()

            Catch ex As Exception
                clsAppError.DisplayError("Error initialising the Summary Grid, BatchBlocks page.", ex)
            End Try

        Catch ex As Exception
            clsAppError.DisplayError("Error initialising the Summary Grid", ex)
        End Try
    End Sub

#End Region

#Region "Validation"

    Private Function SetClientValidation() As Boolean
        If HttpContext.Current.Request.Browser.JavaScript Then
            Dim scr As New System.Text.StringBuilder

            scr.Append("<SCRIPT language=""Javascript"">" + vbNewLine)
            scr.Append("function ClientValidateHistologyRef(sender, args)" + vbNewLine)
            scr.Append("{" + vbNewLine)
            scr.Append("    var sHistologyRef = args.Value;" + vbNewLine)
            scr.Append("    var iIndexof = sHistologyRef.indexOf(""-"");" + vbNewLine)
            scr.Append("    if (iIndexof != -1)" + vbNewLine)
            scr.Append("    { " + vbNewLine)
            scr.Append("        args.IsValid = false;" + vbNewLine)
            scr.Append("        return;" + vbNewLine)
            scr.Append("    }" + vbNewLine)
            scr.Append("    else" + vbNewLine)
            scr.Append("    {" + vbNewLine)

            ' Allow HP numbers
            If CStr(Session.Item(SessionVars.SV_HeaderUserArea)) = "Histopath" Then
                scr.Append("    var expHPRef = /[H][P][0-9][0-9][0-9][0-9][/][0-9][0-9]/;" + vbNewLine)
                scr.Append("    if (expHPRef.test(sHistologyRef))" + vbNewLine)
                scr.Append("    {" + vbNewLine)
                scr.Append("        args.IsValid = true;" + vbNewLine)
                scr.Append("        return;" + vbNewLine)
                scr.Append("    }" + vbNewLine)
            End If

            scr.Append("        if (sHistologyRef.length !=8)" + vbNewLine)
            scr.Append("        {" + vbNewLine)
            scr.Append("            args.IsValid = false;" + vbNewLine)
            scr.Append("            return;" + vbNewLine)
            scr.Append("        }" + vbNewLine)
            scr.Append("        else" + vbNewLine)
            scr.Append("        {" + vbNewLine)
            scr.Append("            var expHistologyRef = /[0-9][0-9][/][0-9][0-9][0-9][0-9][0-9]/;" + vbNewLine)
            scr.Append("            if (expHistologyRef.test(sHistologyRef))" + vbNewLine)
            scr.Append("            {" + vbNewLine)
            scr.Append("                var d = new Date();" + vbNewLine)
            scr.Append("                var sYear = sHistologyRef.substring(0,2);" + vbNewLine)
            scr.Append("                var currentDate = d.getFullYear().toString();" + vbNewLine)
            scr.Append("                var sYearPart = currentDate.substring(2, 4);" + vbNewLine)
            scr.Append("                if (sYear > sYearPart && sYear < 70)" + vbNewLine)
            scr.Append("                {" + vbNewLine)
            scr.Append("                    args.IsValid = false;" + vbNewLine)
            scr.Append("                    return;" + vbNewLine)
            scr.Append("                }" + vbNewLine)
            scr.Append("                args.IsValid = true;" + vbNewLine)
            scr.Append("                return;" + vbNewLine)
            scr.Append("            }" + vbNewLine)
            scr.Append("            else" + vbNewLine)
            scr.Append("            {" + vbNewLine)
            scr.Append("                args.IsValid = false;" + vbNewLine)
            scr.Append("                return;" + vbNewLine)
            scr.Append("            }" + vbNewLine)
            scr.Append("        }" + vbNewLine)
            scr.Append("    }" + vbNewLine)
            scr.Append("}" + vbNewLine)
            scr.Append("</SCRIPT>" + vbNewLine)

            Me.Page.RegisterClientScriptBlock("SetHistologyClientValidation", scr.ToString())
            Return True
        Else
            Return False
        End If
    End Function


    Public Sub ValidateHistologyRef(ByVal sender As Object, ByVal args As ServerValidateEventArgs)
        Dim sHistologyRef As String = CStr(args.Value)
        Dim sHistoYear As String
        Dim dDate As Date
        Dim revHistologyRef As Regex = New Regex("[0-9][0-9](/)[0-9][0-9][0-9][0-9][0-9]")
        Dim revHPRef As Regex = New Regex("[H][P][0-9][0-9][0-9][0-9][/][0-9][0-9]")
        Dim match As Match

        ' Allow HP numbers
        If CStr(Session.Item(SessionVars.SV_HeaderUserArea)) = "Histopath" Then
            match = revHPRef.Match(sHistologyRef)

            If match.Success Then
                args.IsValid = True
                Exit Sub
            End If
        End If

        If sHistologyRef.IndexOf("-") <> -1 Then
            args.IsValid = False
            Exit Sub
        Else
            If sHistologyRef.Length <> 8 Then
                args.IsValid = False
                Exit Sub
            Else
                match = revHistologyRef.Match(sHistologyRef)

                If match.Success Then
                    sHistoYear = Left$(sHistologyRef, 2)
                    Dim sYear As String = Right$(dDate.Now.Year(), 2)

                    If Convert.ToInt32(sHistoYear) > Convert.ToInt32(sYear) And Convert.ToInt32(sHistoYear) < 70 Then
                        args.IsValid = False
                    Else
                        args.IsValid = True
                    End If
                    Exit Sub
                Else
                    args.IsValid = False
                    Exit Sub
                End If
            End If
        End If
    End Sub

#End Region

#Region "Event Handlers"

    Protected Sub chkByPassSort_CheckedChanged(ByVal sender As Object, ByVal e As EventArgs) Handles chkByPassSort.CheckedChanged
        Dim dsBatchDetails As DataSet = CType(Session.Item(SessionVars.SV_BatchDetails), DataSet)
        Dim dtBatch As DataTable

        dtBatch = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_TABLE)
        dtBatch.Rows(0)("ByPassSort") = chkByPassSort.Checked

        InitialiseSummaryGrid()
    End Sub

    Private Sub grdBatchSummary_ItemDataBound(ByVal sender As System.Object, ByVal e As System.Web.UI.WebControls.DataGridItemEventArgs) Handles grdBatchSummary.ItemDataBound
        ' populate template column values here
        Try
            ' set up the checkbox and drop-down columns
            Dim drv As DataRowView = CType(e.Item.DataItem, DataRowView)

            If Not drv Is Nothing Then
                Dim valHistologyRef As CustomValidator = CType(e.Item.FindControl("valHistologyRef"), CustomValidator)
                Dim valreqHistoloygRef As RequiredFieldValidator = CType(e.Item.FindControl("valrequiredHistologyRef"), RequiredFieldValidator)
                Dim sSenderRef As String
                Dim sFilter As String
                Dim drFoundRow As DataRow()
                Dim dsBatchDetails As DataSet = CType(Session.Item(SessionVars.SV_BatchDetails), DataSet)
                Dim dtSubmittedAs As DataTable = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_SUBMITTEDAS_TABLE)
                Dim bDisableHistologyRef As Boolean = False

                If Not drv Is Nothing Then
                    Dim lblHistologyRefDisplay As Label = Nothing
                    Dim txtHistologyRefEdit As TextBox = Nothing
                    Dim lblSenderRefDisplay As Label = Nothing

                    If e.Item.ItemType = ListItemType.EditItem Then
                        txtHistologyRefEdit = CType(e.Item.FindControl("txtHistologyRefEdit"), TextBox)
                        lblSenderRefDisplay = CType(e.Item.FindControl("lblSenderRefEdit"), Label)
                        valreqHistoloygRef = CType(e.Item.FindControl("valRequiredHistologyRef"), RequiredFieldValidator)
                        valHistologyRef = CType(e.Item.FindControl("valHistologyRef"), CustomValidator)
                    ElseIf e.Item.ItemType = ListItemType.Item _
OrElse e.Item.ItemType = ListItemType.AlternatingItem _
OrElse e.Item.ItemType = ListItemType.SelectedItem Then
                        ' populate display mode controls
                        lblHistologyRefDisplay = CType(e.Item.FindControl("lblHistologyRefDisplay"), Label)
                        lblSenderRefDisplay = CType(e.Item.FindControl("lblSenderRefDisplay"), Label)
                    End If

                    If Not lblSenderRefDisplay Is Nothing Then
                        If Not IsDBNull(drv("SenderRef")) Then
                            lblSenderRefDisplay.Text = drv("SenderRef").ToString()
                        Else
                            lblSenderRefDisplay.Text = ""
                        End If
                    End If

                    If Not lblHistologyRefDisplay Is Nothing Then
                        If Not IsDBNull(drv("HistologyRef")) Then
                            lblHistologyRefDisplay.Text = drv("HistologyRef").ToString()
                        Else
                            lblHistologyRefDisplay.Text = ""
                        End If
                    End If

                    If Not txtHistologyRefEdit Is Nothing AndAlso Not valHistologyRef Is Nothing Then
                        If Not IsDBNull(drv("HistologyRef")) Then
                            txtHistologyRefEdit.Text = drv("HistologyRef").ToString()

                            If CStr(Session.Item(SessionVars.SV_HeaderUserArea)) = "Neuropath" Then
                                If Not valHistologyRef Is Nothing Then
                                    If Not lblSenderRefDisplay Is Nothing Then
                                        'Check if the senderref is a pg number
                                        sSenderRef = lblSenderRefDisplay.Text

                                        If CheckPGNumber(sSenderRef) Then
                                            Dim sYear As String
                                            Dim sID As String

                                            sSenderRef = sSenderRef.Substring(2)
                                            sID = Left$(sSenderRef, 4)
                                            sYear = Right$(sSenderRef, 2)

                                            'If year is less than or equal to one dont limit the format of the histo ref
                                            If IsPreEqual01(sYear) Then
                                                valHistologyRef.Enabled = False
                                            Else
                                                txtHistologyRefEdit.Enabled = False
                                            End If
                                        End If
                                    End If
                                End If
                            ElseIf CStr(Session.Item(SessionVars.SV_HeaderUserArea)) = "Histopath" Then
                                valHistologyRef.ToolTip = "Format: NN/NNNNN (Year part must not be greater than current year.) or HPNNNN/NN"
                            End If

                            If Not IsDBNull(drv("BookedHistologyRef")) Then
                                If drv("BookedHistologyRef") = True Then
                                    bDisableHistologyRef = True
                                Else
                                    bDisableHistologyRef = False
                                End If
                            End If

                            If Not IsDBNull(drv("HistoRefSet")) Then
                                If drv("HistoRefSet") = True Then
                                    bDisableHistologyRef = True
                                Else
                                    bDisableHistologyRef = False
                                End If
                            End If

                            txtHistologyRefEdit.Enabled = Not bDisableHistologyRef
                            valHistologyRef.Enabled = Not bDisableHistologyRef
                        Else
                            txtHistologyRefEdit.Text = ""
                        End If
                    End If

                    If Not valreqHistoloygRef Is Nothing Then
                        If Not CStr(Session.Item(SessionVars.SV_HeaderUserArea)) = "Histopath" Then
                            'if pre cassetted make the histology ref mandatory
                            sFilter = "Code=" & "'" & "5" & "'"
                            drFoundRow = dtSubmittedAs.Select(sFilter)
                            If Not drFoundRow Is Nothing Then
                                If drFoundRow.Length = 1 Then
                                    valreqHistoloygRef.Enabled = True
                                Else
                                    valreqHistoloygRef.Enabled = False
                                End If
                            End If
                        End If
                    End If
                End If
            End If
        Catch ex As Exception
            clsAppError.DisplayError("Failed to bind template columns in the Batch summary grid", ex)
        End Try
    End Sub

    Private Sub SummaryGridPager_EditModeStop(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles SummaryGridPager.EditModeStop
        btnAddSubmission.Enabled = True
        EnableDisableButtons(True)
        btnCancel.Enabled = True
    End Sub

    Private Sub SummaryGridPager_EditModeStart(ByVal sender As System.Object, ByVal e As HistopathologySystem.DataGridPagerEventArgs) Handles SummaryGridPager.EditModeStart
        Try

            Dim txtHistologyRefEdit As TextBox = Nothing
            txtHistologyRefEdit = CType(e.GridRow.FindControl("txtHistologyRefEdit"), TextBox)

            ' Disable the selection arrows for the rows that have not been selected.
            Dim dgItem As DataGridItem

            For Each dgItem In grdBatchSummary.Items
                CType(dgItem.Cells(0).Controls(0), LinkButton).Enabled = False
            Next

            EnableDisableButtons(False)
            btnCancel.Enabled = False
            btnAddSubmission.Enabled = False

            If Not txtHistologyRefEdit Is Nothing Then
                If txtHistologyRefEdit.Enabled = True Then
                    SetFocus(txtHistologyRefEdit)
                    Session("BeforeHistologyRef") = txtHistologyRefEdit.Text
                End If
            End If

            ctlDiv.InnerHtml = ""
        Catch ex As Exception
            clsAppError.DisplayError("Editmode start failed.", ex)
        End Try
    End Sub

    Private Sub SummaryGridPager_BeforeDataChanged(ByVal sender As System.Object, ByRef e As HistopathologySystem.DataGridPagerEventArgs) Handles SummaryGridPager.BeforeDataChanged
        e.bCarryOnEditing = m_bContinueEditing
    End Sub

    Private Sub btnEditSubmission_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnEditSubmission.Click
        Try
            Dim iID As Int32
            If grdBatchSummary.SelectedIndex >= 0 Then
                Dim dsBatchDetails As DataSet = CType(Session.Item(SessionVars.SV_BatchDetails), DataSet)
                Dim dtBlocks As DataTable = CType(Session.Item(SessionVars.SV_BlockSummaryTable), DataTable)
                Dim objAnimal As New HistopathologyLib.clsAnimal
                Dim drFoundRow As DataRow()
                Dim sFilter As String

                iID = Convert.ToInt32(grdBatchSummary.DataKeys(grdBatchSummary.SelectedIndex))

                sFilter = "NewID=" & Convert.ToString(iID)
                drFoundRow = dtBlocks.Select(sFilter)

                If Not drFoundRow Is Nothing AndAlso drFoundRow.Length > 0 Then
                    Session.Item(SessionVars.SV_SenderRef) = drFoundRow(0)("SenderRef").ToString()
                    Session.Item(SessionVars.SV_AnimalID) = drFoundRow(0)("AnimalID")
                    objAnimal.GetPreBookedBlocks(drFoundRow(0)("AnimalID"), dsBatchDetails)
                End If

                Session.Item(SessionVars.SV_Editing) = True
                Session.Remove(SessionVars.SV_HistologyRefType)
            End If

        Catch ex As Exception
            clsAppError.DisplayError("Failed to edit submission.", ex)
        End Try

        Try
            Dim objArrayList As ArrayList = CType(Session.Item(SessionVars.SV_BreadCrumbs), ArrayList)
            If Not objArrayList Is Nothing Then
                objArrayList(1) = "Submission Samples"
                objArrayList(2) = "Blocking"
                objArrayList.Insert(3, "Sample Blocks")
                Session.Item(SessionVars.SV_BreadCrumbs) = objArrayList
            End If
        Catch ex As Exception
            clsAppError.DisplayError("Bread Crumb Error, BatchBlockSummary.aspx.", ex)
        End Try

        Session.Item(SessionVars.SV_BatchBlockSummaryPage) = grdBatchSummary.CurrentPageIndex
        Session.Item(SessionVars.SV_AddSamplePrevPage) = "BatchBlockSummary.aspx"
        Response.Redirect("SubmissionDetailsBlock.aspx")
    End Sub

    Private Sub btnDeleteSubmission_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnDeleteSubmission.Click
        Try
            Dim iID As Int32
            If grdBatchSummary.SelectedIndex >= 0 Then
                Dim objAnimal As New HistopathologyLib.clsAnimal
                Dim dtData As DataTable = CType(Session.Item(SessionVars.SV_BlockSummaryTable), DataTable)
                Dim dsBatchDetails As DataSet = CType(Session.Item(SessionVars.SV_BatchDetails), DataSet)
                Dim sFilter As String
                Dim iAnimalID As Integer = 0
                Dim foundRows As DataRow()

                iID = Convert.ToInt32(grdBatchSummary.DataKeys(grdBatchSummary.SelectedIndex))

                sFilter = "NewID=" & iID
                foundRows = dtData.Select(sFilter)

                If Not foundRows Is Nothing And foundRows.Length > 0 Then
                    iAnimalID = foundRows(0)("AnimalID")
                    If Not objAnimal.RemoveSubmission(dsBatchDetails, _
                                                      iAnimalID, _
                                                      "BATCH_BLOCK_TABLE", _
                                                      IsBatchPreCassetted(dsBatchDetails, CInt(Session.Item(SessionVars.SV_BatchID)))) Then
                        Throw New Exception("Animal.RemoveSubmission returned false")
                    End If

                    If CType(Session.Item(SessionVars.SV_CreatingNewBatch), Boolean) And CStr(Session.Item(SessionVars.SV_HeaderUserArea)) = "Histopath" Then
                        AddTounusedHistologyRefTable(foundRows(0)("SenderRef"), foundRows(0)("HistologyRef"))
                    End If
                End If

                InitialiseSummaryGrid()

                If grdBatchSummary.Items.Count = 0 Then
                    Session.Item(SessionVars.SV_ImportedFromDayBook) = False
                    EnableDisableButtons(False)
                End If
            End If

            DisplayNumberSamples()
            SummaryGridPager.EnableDisableHierachicalButton(False)

        Catch ex As Exception
            clsAppError.DisplayError("Failed to edit submission.", ex)
        End Try
    End Sub

    Private Sub AddTounusedHistologyRefTable(ByVal sSenderRef As String, ByVal sHistologyRef As String)
        Dim dtUsedHistologyRefs As DataTable = CType(Session.Item(SessionVars.SV_UnusedHistologyRef), DataTable)
        Dim objHistology As New HistopathologyLib.clsHistology
        Dim iHistoNumber
        Dim iPreviousHistoType

        If sHistologyRef.IndexOf("HP") = -1 Then
            iHistoNumber = Convert.ToInt32(Right$(sHistologyRef, 5))
            iPreviousHistoType = CheckRange(iHistoNumber)
        Else
            iPreviousHistoType = 0
        End If

        If dtUsedHistologyRefs Is Nothing Then
            dtUsedHistologyRefs = objHistology.CreateUnusedHistologyRefs()
            objHistology.AddUnusedHistologyRef(dtUsedHistologyRefs, sSenderRef, sHistologyRef, iPreviousHistoType)
            Session.Item(SessionVars.SV_UnusedHistologyRef) = dtUsedHistologyRefs
        End If

    End Sub

    Private Function CheckIfHistoRefIsValid(ByVal iHistologyType As Integer, ByVal iHistoNumber As Integer, ByRef iHistoNextNumber As Integer) As Boolean
        Dim dtData As DataTable
        Dim sFilter As String = "Type=" & iHistologyType
        Dim drFoundRows As DataRow()
        Dim objHistology As New HistopathologyLib.clsHistology

        'Get the latest histo refs from the database
        If Not objHistology.GetHistologyRefsTable(dtData) Then
            Throw New Exception("Histology.GetHistologyRefsTable returned false.")
        End If

        drFoundRows = dtData.Select(sFilter)
        If Not drFoundRows Is Nothing AndAlso drFoundRows.Length > 0 Then
            iHistoNextNumber = CInt(drFoundRows(0)("NextHistologyRef"))
            If CStr(Session.Item(SessionVars.SV_HeaderUserArea)) = "Histopath" Then
                If iHistoNumber >= iHistoNextNumber Then
                    Return False
                Else
                    Return True
                End If
            Else
                If iHistoNumber >= iHistoNextNumber Then
                    Return False
                Else
                    Return True
                End If
            End If
        Else
            Return True
        End If
    End Function

    Private Sub SummaryGridPager_RowSave(ByVal sender As System.Object, ByVal e As HistopathologySystem.DataGridPagerEventArgs) Handles SummaryGridPager.RowSave
        Try
            ' save template column values to the dataset here
            Dim iID As Int32 = grdBatchSummary.DataKeys(grdBatchSummary.SelectedIndex)
            Dim dtData As DataTable = CType(Session.Item(SessionVars.SV_BlockSummaryTable), DataTable)
            Dim txtHistoRef As TextBox = CType(e.GridRow.FindControl("txtHistologyRefEdit"), TextBox)
            Dim lblSenderRef As Label = CType(e.GridRow.FindControl("lblSenderRefEdit"), Label)
            Dim dsBatchDetails As DataSet = CType(Session.Item(SessionVars.SV_BatchDetails), DataSet)
            Dim dtSubmittedAs As DataTable = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_SUBMITTEDAS_TABLE)
            Dim dtAnimal As DataTable
            Dim drRow As DataRow()
            Dim iAnimalID As Integer
            Dim sFilter As String
            Dim sHistologyRef As String
            Dim drBlockRow As DataRow
            Dim iHistoRefType As Integer = 0
            Dim iNextHistoRef As Integer = 0
            Dim drFoundRow As DataRow()
            Dim iHistoNextNumber As Integer = 0
            Dim dDate As Date
            Dim objAnimal As New HistopathologyLib.clsAnimal
            Dim bHistologyUser As Boolean = False

            ' Allow HP numbers
            If CStr(Session.Item(SessionVars.SV_HeaderUserArea)) = "Histopath" Then
                bHistologyUser = True
            End If

            If Not txtHistoRef Is Nothing Then
                If txtHistoRef.Enabled = True Then
                    sHistologyRef = txtHistoRef.Text
                    If sHistologyRef <> "" And ValidateHistoRef(sHistologyRef, bHistologyUser) And Not IsPreviousYearHistoRef(sHistologyRef) Then
                        If sHistologyRef.IndexOf("HP") = -1 Then
                            iNextHistoRef = Convert.ToInt32(Right$(sHistologyRef, 5))
                            iHistoRefType = CheckRange(iNextHistoRef)

                            If Not CheckIfHistoRefIsValid(iHistoRefType, iNextHistoRef, iHistoNextNumber) Then
                                ctlDiv.InnerHtml = "<p><font color=""Red"">The Histology Ref entered is higher than or equal to the current next Histology Ref (" & Right$(dDate.Now.Year.ToString, 2) & "/" & iHistoNextNumber.ToString & ") for the selected area.</font></p>"
                                m_bContinueEditing = True
                                Exit Sub
                            End If
                        End If
                    End If

                    If Not objAnimal.GetAnimalByHistologyRef(sHistologyRef, dtAnimal) Then
                        Throw New Exception("Animal.GetAnimalbyHistologyRef returned false.")
                    Else
                        If dtAnimal.Rows.Count > 0 Then
                            ctlDiv.InnerHtml = "<p><font color=""Red"">The Histology Ref " & sHistologyRef & " entered already exists. Please enter an alternative reference.</font></p>"
                            m_bContinueEditing = True
                            Exit Sub
                        End If
                    End If

                    'Check the Histology ref entered does not already exist on the submission.
                    Dim sBeforeHistologyRef = CType(Session("BeforeHistologyRef"), String)
                    If sBeforeHistologyRef <> "" And sBeforeHistologyRef <> txtHistoRef.Text Then
                        Dim drCheckAnimalDoesNotExist As DataRow()
                        drCheckAnimalDoesNotExist = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_ANIMAL).Select("HistologyRef='" & txtHistoRef.Text & "'")

                        If Not drCheckAnimalDoesNotExist Is Nothing Then
                            If drCheckAnimalDoesNotExist.Length > 0 Then
                                ctlDiv.InnerHtml = "<p><font color=""Red"">The Histology Ref " & sHistologyRef & " entered already exists on the submission. Please enter an alternative reference.</font></p>"
                                m_bContinueEditing = True
                                Exit Sub
                            End If
                        End If

                        If CType(Session.Item(SessionVars.SV_CreatingNewBatch), Boolean) And bHistologyUser Then
                            If Not lblSenderRef Is Nothing Then
                                AddTounusedHistologyRefTable(lblSenderRef.Text, CType(Session.Item("BeforeHistologyRef"), String))
                            End If
                        End If

                        Session("BeforeHistologyRef") = ""
                    End If

                    sFilter = "NewID=" & iID
                    drRow = dtData.Select(sFilter)

                    If Not drRow Is Nothing Then
                        iAnimalID = drRow(0)("AnimalID")

                        For Each drBlockRow In dtData.Rows
                            If Not IsDBNull(drBlockRow("AnimalID")) Then
                                If drBlockRow("AnimalID") = iAnimalID Then
                                    drBlockRow("HistologyRef") = sHistologyRef
                                End If
                            End If
                        Next

                        If Not dsBatchDetails Is Nothing Then
                            dtAnimal = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_ANIMAL)
                            sFilter = "ID=" & drRow(0)("AnimalID")
                            drRow = dtAnimal.Select(sFilter)

                            If Not drRow Is Nothing Then
                                drRow(0)("HistologyRef") = sHistologyRef
                            End If
                        End If
                    Else
                        Throw New Exception("SummaryGridPager_RowSave No Animal ID found in datatable")
                    End If
                End If
            End If
            ctlDiv.InnerHtml = ""

        Catch ex As Exception
            clsAppError.DisplayError("Failed to save template values to session.", ex)
        End Try
    End Sub


    Private Sub btnCancel_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnCancel.Click
        'Bread crumbs
        Try
            Dim objArrayList As ArrayList = CType(Session.Item(SessionVars.SV_BreadCrumbs), ArrayList)
            If Not objArrayList Is Nothing Then
                objArrayList(1) = "Submission"
                objArrayList(2) = "Submission Details"
                Session.Item(SessionVars.SV_BreadCrumbs) = objArrayList
            End If
        Catch ex As Exception
            clsAppError.DisplayError("Bread Crumb Error, BatchBlockSummary.aspx.", ex)
        End Try

        Response.Redirect("BatchDetails.aspx")
    End Sub

    Private Sub grdBatchSummary_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles grdBatchSummary.SelectedIndexChanged
        EnableDisableButtons(True)
    End Sub

    Private Sub VLAHeader1_HomeClick(ByVal sender As Object, ByVal e As HistopathologySystem.HomeLinkEventArgs) Handles VLAHeader1.HomeClick
        If CType(Session.Item(SessionVars.SV_ViewSubmission), Boolean) = False Then
            Dim sMessage As System.Text.StringBuilder = New System.Text.StringBuilder

            If CType(Session.Item(SessionVars.SV_EditingBatch), Boolean) Then
                sMessage.Append("You are currently editing a submission. Any changes that you have made since you last saved the submission will be lost. Are you sure you wish to exit?")
            ElseIf CType(Session.Item(SessionVars.SV_CreatingNewBatch), Boolean) Then
                sMessage.Append("You are currently creating a new submission. If you exit now all the data you have entered will be lost. Are you sure you wish to exit?")
            Else
                sMessage.Append("Any changes that have been made will be discarded, are you sure you wish to exit without saving?")
            End If

            Page.RegisterStartupScript("navigate", PromptBeforeNavigateScript(sMessage.ToString(), "Home.aspx"))
            e.bNavigateHome = False
        Else
            Response.Redirect("Home.aspx")
        End If
    End Sub

    Private Sub btnCopySubmission_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnCopySubmission.Click
        Try
            If grdBatchSummary.SelectedIndex >= 0 Then
                Dim iID As Int32 = grdBatchSummary.DataKeys(grdBatchSummary.SelectedIndex)
                Dim dtData As DataTable = CType(Session.Item(SessionVars.SV_BlockSummaryTable), DataTable)
                Dim drRow As DataRow()
                Dim iAnimalID As Integer
                Dim sFilter As String
                Dim objAnimal As New HistopathologyLib.clsAnimal
                Dim iNumberOfSamples As Integer = 0

                sFilter = "NewID=" & iID
                drRow = dtData.Select(sFilter)

                If Not drRow(0) Is Nothing Then
                    Session.Item(SessionVars.SV_AnimalID) = drRow(0)("AnimalID")
                    objAnimal.GetNumberOfBlocks(CType(Session.Item(SessionVars.SV_BatchDetails), DataSet), drRow(0)("AnimalID"), iNumberOfSamples)
                    Session.Item(SessionVars.SV_SelectedAnimalNumberBlocks) = iNumberOfSamples
                Else
                    Session.Item(SessionVars.SV_AnimalID) = 0
                End If

                
            End If
        Catch ex As Exception
            clsAppError.DisplayError("Failed to copy sample.", ex)
        End Try

        Try
            Dim objArrayList As ArrayList = CType(Session.Item(SessionVars.SV_BreadCrumbs), ArrayList)
            If Not objArrayList Is Nothing Then
                objArrayList(1) = "Submission Samples"
                objArrayList(2) = "Copy Sample"
                Session.Item(SessionVars.SV_BreadCrumbs) = objArrayList
            End If
        Catch ex As Exception
            clsAppError.DisplayError("Bread Crumb Error, BatchBlockSummary.aspx.", ex)
        End Try

        Session.Item(SessionVars.SV_BatchBlockSummaryPage) = grdBatchSummary.CurrentPageIndex
        Session.Item(SessionVars.SV_OldPGNumber) = ""
        Session.Item(SessionVars.SV_CopySample) = True
        Session.Item(SessionVars.SV_AddSamplePrevPage) = "BatchBlockSummary.aspx"
        Session.Item(SessionVars.SV_AddSampleNextPage) = "BatchBlockSummary.aspx"
        Session.Remove(SessionVars.SV_SenderRef)
        Response.Redirect("AddSample.aspx")
    End Sub

    Private Sub btnAddSubmission_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnAddSubmission.Click
        Session.Item(SessionVars.SV_Editing) = False
        Session.Item(SessionVars.Sv_CopySubmission) = False

        Session.Remove(SessionVars.SV_BatchSubmissionID)
        Session.Remove(SessionVars.SV_AnimalID)
        Session.Remove(SessionVars.SV_SenderRef)
        Session.Remove(SessionVars.SV_HistologyRefType)

        Session.Item(SessionVars.SV_AddSamplePrevPage) = "BatchBlockSummary.aspx"
        Session.Item(SessionVars.SV_AddSampleNextPage) = "SubmissionDetailsBlock.aspx"
        Session.Item(SessionVars.SV_BatchBlockSummaryPage) = grdBatchSummary.CurrentPageIndex
        Session.Item(SessionVars.SV_OldPGNumber) = ""
        Session.Item(SessionVars.SV_PMDate) = ""
        Session.Item(SessionVars.SV_Species) = ""

        Try
            Dim objArrayList As ArrayList = CType(Session.Item(SessionVars.SV_BreadCrumbs), ArrayList)
            If Not objArrayList Is Nothing Then
                objArrayList(1) = "Submission Samples"
                objArrayList(2) = "Blocking"
                objArrayList.Insert(3, "Add Sample")
                Session.Item(SessionVars.SV_BreadCrumbs) = objArrayList
            End If
        Catch ex As Exception
            clsAppError.DisplayError("Bread Crumb Error, BatchBlockSummary.aspx.", ex)
        End Try

        Response.Redirect("AddSubmission.aspx")
    End Sub

#End Region

#Region "private functions"

    Private Sub DisplayDetails()
        Try
            Dim dsBatchDetails As DataSet = CType(Session.Item(SessionVars.SV_BatchDetails), DataSet)
            Dim dtBatch As DataTable

            If Not dsBatchDetails Is Nothing Then
                dtBatch = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_TABLE)

                If Not IsDBNull(dtBatch.Rows(0)("ByPassSort")) Then
                    chkByPassSort.Checked = dtBatch.Rows(0)("ByPassSort")
                Else
                    dtBatch.Rows(0)("ByPassSort") = False
                End If
            End If
        Catch ex As Exception
            clsAppError.DisplayError("Failed to display batch details.", ex)
        End Try
    End Sub

    Private Sub DisplayNumberSamples()
        Try
            Dim dsBatchDetails As DataSet = CType(Session.Item(SessionVars.SV_BatchDetails), DataSet)
            Dim dtBatchBlocks As DataTable
            Dim aArray As New ArrayList
            Dim drRow As DataRow
            Dim iAnimalID As Integer

            If Not dsBatchDetails Is Nothing Then
                'Find the number of samples that have been added against the submission
                dtBatchBlocks = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_TABLE)

                For Each drRow In dtBatchBlocks.Rows
                    If Not drRow.RowState = DataRowState.Deleted Then
                        iAnimalID = drRow("AnimalID")

                        If Not aArray.Contains(iAnimalID) Then
                            aArray.Add(iAnimalID)
                        End If
                    End If
                Next
            End If

            lblNumberSamples.Text = "There are " & aArray.Count _
                                               & " samples on the current submission."
        Catch ex As Exception
            clsAppError.DisplayError("Failed to display number of samples.", ex)
        End Try
    End Sub

    Public Function CheckPGNumber(ByVal sSenderRef As String) As Boolean
        Dim strYear As String
        Dim strID As String

        'check that the sender ref entered is actually a PG number
        If sSenderRef.Length > 2 Then
            sSenderRef = Left$(sSenderRef, 2)
            If sSenderRef = "PG" Or sSenderRef = "pg" Or sSenderRef = "pG" Or sSenderRef = "Pg" Then
                'check that its valid
                Return True
            Else
                Return False
            End If
        Else
            Return False
        End If
    End Function

    Private Sub SetToolTips()
        btnAddSubmission.ToolTip = ADD_SAMPLE_TOOLTIP
        btnEditSubmission.ToolTip = EDIT_SAMPLE_TOOLTIP
        btnDeleteSubmission.ToolTip = DELETE_SAMPLE_TOOLTIP
        btnCopySubmission.ToolTip = COPY_SAMPLE_TOOLTIP
    End Sub

    Private Sub EnableDisableButtons(ByVal bEnabled As Boolean)
        Try
            If CType(Session.Item(SessionVars.SV_ViewSubmission), Boolean) = True Then
                btnDeleteSubmission.Enabled = False
                btnCopySubmission.Enabled = False
                btnEditSubmission.Enabled = bEnabled
                btnAddSubmission.Enabled = False
            ElseIf CType(Session.Item(SessionVars.SV_EditingBatch), Boolean) = True Then
                btnDeleteSubmission.Enabled = bEnabled
                btnCopySubmission.Enabled = bEnabled
                btnEditSubmission.Enabled = bEnabled
                btnAddSubmission.Enabled = True
            Else
                btnDeleteSubmission.Enabled = bEnabled
                btnCopySubmission.Enabled = bEnabled
                btnEditSubmission.Enabled = bEnabled
            End If

        Catch ex As Exception
            clsAppError.DisplayError("Unable to enable or disable controls.", ex)
        End Try
    End Sub

#End Region


End Class
