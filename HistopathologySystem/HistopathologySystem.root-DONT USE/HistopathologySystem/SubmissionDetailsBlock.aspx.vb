Imports System.Text.RegularExpressions

Partial Class SubmissionDetailsBlock
    Inherits System.Web.UI.Page
    Protected WithEvents VLAHeader1 As VLAHeader
    Protected WithEvents SenderRef1 As SenderRef
    Protected WithEvents HistologyRef1 As HistologyRef
    Protected WithEvents ctlPMDate As CalendarDate


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
        VLAHeader1.PageTitle = "Sample Blocks"
        SetCalendarDateHandler(Me.Page)

        If Not IsPostBack Then
            InitialiseHistoRefsTable()
            LoadLookupLists()
            InitialiseSummaryGrid()
            InitialiseScreenWithDetails()
            DisableEnableControls(False)
            SetToolTips()
            'Remember this so we can use it at a later point
            Session.Item(SessionVars.SV_HistologyRef) = HistologyRef1.Text
            PromptBeforeSaveScript("Are you sure you want to delete the selected blocks?", btnDeleteBlock)
            SetEnterKeyPress()
        End If

        
        ' Allow HP numbers
        If CStr(Session.Item(SessionVars.SV_HeaderUserArea)) = "Histopath" Then
            HistologyRef1.AllowHPNumbers = True
        End If
    End Sub



#Region "Grid Handling"

    Private Sub InitialiseSummaryGrid()
        Try
            Dim dsDataSet As DataSet = CType(Session.Item(SessionVars.SV_BatchDetails), DataSet)
            Dim objSummary As New HistopathologyLib.clsBatchSummary
            Dim dtSummary As New DataTable
            Dim sFilter As String = ""
            Dim dtTissuesList As DataTable = Common.GetLookupTypeList(LOOKUP_TISSUE_CODE)

            Dim iAnimalID As Integer = CType(Session.Item(SessionVars.SV_AnimalID), Integer)
            sFilter = "AnimalID=" & Convert.ToString(iAnimalID)

            If Not objSummary.CreateAnimalSummaryData(dsDataSet, dtSummary, dtTissuesList, sFilter) Then
                Throw New Exception("BatchSummary.CreateAnimalSummaryData return false")
            End If

            ' create a dataview for filtering and sorting
            Dim dv As DataView = dtSummary.DefaultView

            Session.Item(SessionVars.SV_BlockSummaryTable) = dtSummary
            Session.Item(SessionVars.SV_BlockSummaryView) = dv

            grdBlockSummary.DataSource = dtSummary
            grdBlockSummary.DataKeyField = "ID"
            grdBlockSummary.DataBind()
            grdBlockSummary.Enabled = True

            HideColumns(dsDataSet)

            SetHierarchical(False)

        Catch ex As Exception
            clsAppError.DisplayError("Error initialising the Summary Grid, BatchBlocks page.", ex)
        End Try
    End Sub

#End Region

#Region "Private Functions"

    Private Sub SetEnterKeyPress()
        If HistologyRef1.IsEnabled() Then
            HistologyRef1.SetFocus()
            HistologyRef1.SetEnterKeyPress(btnAddBlock)
            ctlPMDate.SetHistologyRefOnEnter(HistologyRef1.GetTextBoxClientID)
        Else
            If ddlHistologyType.Visible = True AndAlso ddlHistologyType.Enabled = True Then
                SetFocus(ddlHistologyType)
                ctlPMDate.SetControlOnEnter(ddlHistologyType.ClientID)
            Else
                If ctlPMDate.Enabled Then
                    ctlPMDate.SetCalendarFocus()
                    ctlPMDate.SetControlOnEnter(btnAddBlock.ClientID)
                End If
            End If
        End If
    End Sub

    Private Function ValidateMandatoryFields() As Boolean
        Try
            Dim dNow As Date
            If ctlPMDate.Validate(dNow) Then
                If HistologyRef1.IsMandatory Then
                    If Not HistologyRef1.IsComplete() Or Not HistologyRef1.IsValid() Then
                        ctlDiv.InnerHtml = "<p><font color=""Red"">Not all fields have been completed correctly, hover the mouse pointer over the red stars for details.</font></p>"
                        Return False
                    End If
                Else
                    If HistologyRef1.Text <> "" Then
                        If Not HistologyRef1.IsValid() Then
                            ctlDiv.InnerHtml = "<p><font color=""Red"">Not all fields have been completed correctly, hover the mouse pointer over the red stars for details.</font></p>"
                            Return False
                        End If
                    End If
                End If
            Else
                ctlDiv.InnerHtml = "<p><font color=""Red"">Not all fields have been completed correctly, hover the mouse pointer over the red stars for details.</font></p>"
                Return False
            End If
            Return True
        Catch ex As Exception
            clsAppError.DisplayError("Failed to validate Mandatory fields.", ex)
        End Try
    End Function

    Private Sub CheckPGNumber()
        Try
            If SenderRef1.CheckPGNumber(False) Then
                Dim strSender As String = SenderRef1.Text
                Dim strYear As String
                Dim strID As String

                strSender = strSender.Substring(2)
                strID = Left$(strSender, 4)
                strYear = Right$(strSender, 2)

                'If year is less than or equal to one, remove the reverse PG option
                ' from the histo type list. Also dont limit the format of the
                'histo ref
                If IsPreEqual01(strYear) Then
                    'Remove the reverse PG option from the dropdown
                    Dim li As ListItem
                    For Each li In ddlHistologyType.Items
                        If li.Text = "use pg number" Then
                            ddlHistologyType.Items.Remove(li)
                            Exit For
                        End If
                    Next
                    'Also dont validate the histology ref
                    HistologyRef1.SetValidate(False)
                Else
                    If HistologyRef1.Text = "" Then
                        DefaultHistoRefPGReverse(False)
                    End If
                    HistologyRef1.SetEnabled(False)
                End If
            End If

        Catch ex As Exception
            clsAppError.DisplayError("Failed to check PG Number.", ex)
        End Try
    End Sub
    Private Function DefaultHistoRefPGReverse(ByVal bDisplayError As Boolean) As Boolean
        Try

            Dim bValid As Boolean = False
            bValid = SenderRef1.CheckPGNumber(bDisplayError)

            If bValid Then
                Dim strSender As String = SenderRef1.Text
                Dim strYear As String
                Dim strID As String

                strSender = strSender.Substring(2)
                strID = Left$(strSender, 4)
                strYear = Right$(strSender, 2)

                'only reverse the PG number automatically if the year > 01
                If Not IsAfter01(strYear) Then
                    HistologyRef1.Text = strYear + "/" + "0" + strID
                End If
            End If

            Return bValid
        Catch ex As Exception
            clsAppError.DisplayError("Failed to Initialise the Histology Ref field.", ex)
        End Try
    End Function
    Private Sub SetHierarchical(ByVal bExpanded As Boolean)
        Dim iCount As Int32

        If Not bExpanded Then
            For iCount = 0 To grdBlockSummary.Items.Count - 1
                If grdBlockSummary.Items(iCount).Cells(1).Text = "&nbsp;" Then
                    grdBlockSummary.Items(iCount).Visible = False
                End If

                grdBlockSummary.Items(iCount).Cells(2).Controls(0).Visible = False

                If iCount + 1 <= grdBlockSummary.Items.Count - 1 Then
                    If Not grdBlockSummary.Items(iCount + 1).Cells(3).Text = "&nbsp;" Then
                        grdBlockSummary.Items(iCount).Cells(2).Controls(0).Visible = True
                        CType(grdBlockSummary.Items(iCount).Cells(2).Controls(0), LinkButton).Text = "<img src=""Images/plus.gif"">"
                    End If
                End If
            Next

        Else
            For iCount = 0 To grdBlockSummary.Items.Count - 1
                'If the sender ref is blank in the datatable it is a expandable tissue row
                If grdBlockSummary.Items(iCount).Cells(1).Text = "&nbsp;" Then
                    grdBlockSummary.Items(iCount).Visible = True
                    grdBlockSummary.Items(iCount).Cells(2).Controls(0).Visible = False
                    HideControls(iCount)
                End If

                If iCount + 1 <= grdBlockSummary.Items.Count - 1 Then
                    'if the next row tissue is blank its a sender ref row
                    If Not grdBlockSummary.Items(iCount + 1).Cells(3).Text = "&nbsp;" Then
                        CType(grdBlockSummary.Items(iCount).Cells(2).Controls(0), LinkButton).Text = "<img src=""Images/minus.gif"">"
                    End If
                End If
            Next
        End If
    End Sub

    Private Sub HideControls(ByVal iCount As Integer)
        Dim strGridPart As String
        Dim strEO As String
        Dim strHE As String
        Dim strHEBse As String
        Dim strSpecialStain As String
        Dim strIHCPrp As String
        Dim strIHCOther As String
        Dim strArchive As String
        Dim strSelected As String
        Dim cbEO As CheckBox
        Dim cbHE As CheckBox
        Dim cbHEBSE As CheckBox
        Dim cbSpecialStain As CheckBox
        Dim cbIHCPrp As CheckBox
        Dim cbIHCOther As CheckBox
        Dim cbArchive As CheckBox
        Dim cbSelected As CheckBox
        Dim litEO As LiteralControl
        Dim litHE As LiteralControl
        Dim liHEBse As LiteralControl
        Dim liSpecialStain As LiteralControl
        Dim liIHCPrp As LiteralControl
        Dim liIHCOther As LiteralControl
        Dim liSelected As LiteralControl

        grdBlockSummary.Items(iCount).Visible = True
        grdBlockSummary.Items(iCount).Cells(2).Controls(0).Visible = False
        'Hide the row selection button
        grdBlockSummary.Items(iCount).Cells(0).Controls(0).Visible = False
        'Hide the combo boxes 

        litEO = CType(grdBlockSummary.Items(iCount).Cells(4).Controls(0), LiteralControl)
        litHE = CType(grdBlockSummary.Items(iCount).Cells(5).Controls(0), LiteralControl)
        liHEBse = CType(grdBlockSummary.Items(iCount).Cells(6).Controls(0), LiteralControl)
        liSpecialStain = CType(grdBlockSummary.Items(iCount).Cells(7).Controls(0), LiteralControl)
        liIHCPrp = CType(grdBlockSummary.Items(iCount).Cells(8).Controls(0), LiteralControl)
        liIHCOther = CType(grdBlockSummary.Items(iCount).Cells(9).Controls(0), LiteralControl)
        liSelected = CType(grdBlockSummary.Items(iCount).Cells(10).Controls(0), LiteralControl)

        strGridPart = GetGridPart(litEO.UniqueID())
        strEO = strGridPart + "cbEODisplay"
        strHE = strGridPart + "cbHAndEDisplay"
        strHEBse = strGridPart + "cbHAndEBseDisplay"
        strSpecialStain = strGridPart + "cbSpecialStainDisplay"
        strIHCPrp = strGridPart + "cbIHCPrpDisplay"
        strIHCOther = strGridPart + "cbIHCOtherDisplay"
        strArchive = strGridPart + "cbArchiveDisplay"
        strSelected = strGridPart + "cbSelected"
        cbEO = Page.FindControl(strEO)
        cbHE = Page.FindControl(strHE)
        cbHEBSE = Page.FindControl(strHEBse)
        cbSpecialStain = Page.FindControl(strSpecialStain)
        cbIHCPrp = Page.FindControl(strIHCPrp)
        cbIHCOther = Page.FindControl(strIHCOther)
        cbArchive = Page.FindControl(strArchive)
        cbSelected = Page.FindControl(strSelected)

        If Not cbSelected Is Nothing Then
            cbSelected.Visible = False
        End If

        If Not cbArchive Is Nothing Then
            cbArchive.Visible = False
        End If

        If Not cbEO Is Nothing Then
            cbEO.Visible = False
        End If

        If Not cbHE Is Nothing Then
            cbHE.Visible = False
        End If

        If Not cbHEBSE Is Nothing Then
            cbHEBSE.Visible = False
        End If

        If Not cbSpecialStain Is Nothing Then
            cbSpecialStain.Visible = False
        End If

        If Not cbIHCPrp Is Nothing Then
            cbIHCPrp.Visible = False
        End If

        If Not cbIHCOther Is Nothing Then
            cbIHCOther.Visible = False
        End If
    End Sub

    Private Sub HideColumns(ByVal dsSubmission As DataSet)
        'Only display the Histology columns which are relevent to the type of submission
        If Session(SessionVars.SV_SubmissionType) = SUBMISSION_NONTSE Then
            grdBlockSummary.Columns(7).Visible = False
            grdBlockSummary.Columns(8).Visible = False
            grdBlockSummary.Columns(9).Visible = True
        Else
            grdBlockSummary.Columns(7).Visible = True
            grdBlockSummary.Columns(8).Visible = True
            grdBlockSummary.Columns(9).Visible = False
        End If
    End Sub

    Private Sub InitialiseScreenWithDetails()
        Try
            Dim objAnimal As New HistopathologyLib.clsAnimal
            Dim objHistology As New HistopathologyLib.clsHistology
            Dim dsBatchDetails As DataSet = CType(Session.Item(SessionVars.SV_BatchDetails), DataSet)
            Dim dtSubmittedAs As DataTable = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_SUBMITTEDAS_TABLE)
            Dim dtBlockDetails As DataTable = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_TABLE)
            Dim iAnimalID As Integer = CInt(Session.Item(SessionVars.SV_AnimalID))
            Dim sHistologyRef As String
            Dim sSenderRef As String
            Dim sNextBlockRef As String
            Dim aRowStamp As System.Array
            Dim bHistologySetInDatabase As Boolean
            Dim bPMDateSet As Boolean
            Dim drFoundRows As DataRow()
            Dim sFilter As String
            Dim sPMDate As String
            Dim bHistoRefLinked As Boolean

            If Not objAnimal.GetAnimalData(dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_ANIMAL), _
                                           iAnimalID, _
                                           sHistologyRef, _
                                           sSenderRef, _
                                           sNextBlockRef, _
                                           aRowStamp, _
                                           bHistologySetInDatabase, _
                                           sPMDate, _
                                           bPMDateSet, _
                                           bHistoRefLinked) Then
                Throw New Exception("Animal.GetAnimalData returned false.")
            End If

            SenderRef1.Text = sSenderRef
            SenderRef1.SetEnabled(False)
            HistologyRef1.Text = sHistologyRef

            'If the user is neuropath, set necessary validation if sender ref is PG number
            If CStr(Session.Item(SessionVars.SV_HeaderUserArea)) = "Neuropath" Then
                CheckPGNumber()
            End If

            'if PMdate has been retrieved from database dont allow it to be edited
            ctlPMDate.DateField = sPMDate
            If bPMDateSet Then
                ctlPMDate.Enabled = False
            End If

            'If the histology ref is retrieved from the database dont allow it to be edited
            If bHistologySetInDatabase Then
                HistologyRef1.SetEnabled(False)
                HistologyRef1.SetValidate(False)
                ddlHistologyType.Enabled = False
            End If

            If Not CStr(Session.Item(SessionVars.SV_HeaderUserArea)) = "Histopath" Then
                lblPick.Visible = False
                ddlHistologyType.Visible = False

                'if pre cassetted make the histology ref mandatory
                sFilter = "Code=" & "'" & "5" & "'"
                drFoundRows = dtSubmittedAs.Select(sFilter)
                If Not drFoundRows Is Nothing And drFoundRows.Length = 1 Then
                    HistologyRef1.SetMandatory(True)
                Else
                    HistologyRef1.SetMandatory(False)
                End If
            End If

            SelectItemInDropDownList(ddlHistologyType, Session.Item(SessionVars.SV_HistologyRefType))

            If Not String.IsNullOrEmpty(HistologyRef1.Text) = True Then
                If Not String.IsNullOrEmpty(objHistology.FindUsedHistologyRef(CType(Session.Item(SessionVars.SV_UsedHistologyRef), DataTable), HistologyRef1.Text)) Then
                    HistologyRef1.SetEnabled(False)
                End If
            End If


            'Display the number of blocks against the current submission
            sFilter = "AnimalID =" & iAnimalID
            drFoundRows = dtBlockDetails.Select(sFilter)
            If Not drFoundRows Is Nothing Then
                lblNumberBlocks.Text = "There are " & drFoundRows.Length _
                                                   & " blocks linked to the current sample."

                If bHistoRefLinked Then
                    If drFoundRows.Length > 0 Then
                        ' And the animal histo ref has been linked
                        HistologyRef1.SetEnabled(False)
                    End If
                End If
            End If

        Catch ex As Exception
            clsAppError.DisplayError("Failed to Initialise 'Submission Details' page.", ex)
        End Try
    End Sub

    Private Sub DisableEnableControls(ByVal bEnable As Boolean)
        btnEditBlock.Enabled = bEnable
        btnDeleteBlock.Enabled = bEnable

        'If not histopath user and tissues are not being assigned to blocks then disabled
        ' the copy blocks button
        If CStr(Session.Item(SessionVars.SV_HeaderUserArea)) = "Histopath" Then
            If CType(Session.Item(SessionVars.SV_AssignBlocks), Boolean) = True Then
                btnCopyBlock.Enabled = bEnable
            Else
                btnCopyBlock.Enabled = False
            End If
        Else
            btnCopyBlock.Enabled = False
        End If

        'If editing the sample disable the back button
        If CType(Session.Item(SessionVars.SV_Editing), Boolean) = True Then
            btnCancel.Enabled = False
        End If

        If CType(Session.Item(SessionVars.SV_ViewSubmission), Boolean) = True Then
            btnAddBlock.Enabled = False
            btnEditBlock.Enabled = False
            btnDeleteBlock.Enabled = False
            btnCopyBlock.Enabled = False
            btnBlockRefSearch.Enabled = False
            btnCancel.Enabled = False
            SenderRef1.SetEnabled(False)
            HistologyRef1.SetEnabled(False)
            ctlPMDate.Enabled = False
            cbSelectAll.Enabled = False
            If ddlHistologyType.Visible Then
                ddlHistologyType.Enabled = False
            End If
        End If

    End Sub

    Private Sub UpdateSessionWithAnimalDetails()
        Try
            Dim dsBatchDetails As DataSet = CType(Session.Item(SessionVars.SV_BatchDetails), DataSet)
            Dim dtAnimal As DataTable = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_ANIMAL)
            Dim iAnimalID As Integer = CInt(Session.Item(SessionVars.SV_AnimalID))
            Dim sFilter As String
            Dim foundRow As DataRow()
            Dim iNextHistoRef As Integer
            Dim objAnimal As New HistopathologyLib.clsAnimal
            Dim dtAnimalData As DataTable
            Dim drAnimal As DataRow

            If Not dtAnimal Is Nothing Then
                sFilter = "ID=" & iAnimalID
                foundRow = dtAnimal.Select(sFilter)
                foundRow(0)("HistologyRef") = FormatEmptyString(HistologyRef1.Text)
                foundRow(0)("PMDate") = FormatEmptyString(ctlPMDate.DateField)

                If ddlHistologyType.Visible AndAlso ddlHistologyType.Enabled Then
                    If ddlHistologyType.SelectedItem.Text = "use pg number" Then
                        foundRow(0)("IsPGNumber") = True
                    Else
                        foundRow(0)("IsPGNumber") = False
                    End If
                End If
            End If

            Dim iCurrentAnimalId As Integer = CInt(Session.Item(SessionVars.SV_AnimalID))
            If Not objAnimal.GetAnimalByHistologyRef(HistologyRef1.Text, dtAnimalData) Then
                Throw New Exception("Animal.GetAnimalbyHistologyRef returned false.")
            Else
                If dtAnimalData.Rows.Count > 0 Then
                    If IsDBNull(dtAnimalData.Rows(0)("SenderRef")) Then
                        Dim iNewAnimalID As Integer = dtAnimalData.Rows(0)("ID")

                        drAnimal = dtAnimal.Rows.Find(iCurrentAnimalId)
                        If drAnimal("BookedHistologyRef") = False Then
                            drAnimal("ID") = iNewAnimalID
                            drAnimal("HistologyRef") = dtAnimalData.Rows(0)("HistologyRef")
                            drAnimal("RowState") = DataRowState.Modified
                            drAnimal("RowStamp") = dtAnimalData.Rows(0)("RowStamp")
                            drAnimal("BookedHistologyRef") = True
                            Session.Item(SessionVars.SV_AnimalID) = iNewAnimalID

                            objAnimal.GetPreBookedBlocks(iNewAnimalID, dsBatchDetails)
                        End If
                    End If
                End If
            End If

        Catch ex As Exception
            clsAppError.DisplayError("Failed to update session with animal details.", ex)
        End Try
    End Sub

    Private Function CheckIfHistoRefIsValid(ByVal iHistologyType As Integer, ByVal iHistoNumber As Integer, ByRef iNextHistoNumber As Integer) As Boolean
        Dim dtData As DataTable
        Dim sFilter As String = "Type=" & iHistologyType
        Dim drFoundRows As DataRow()

        dtData = CType(Session.Item(SessionVars.SV_HistoRefsVersion), DataTable)

        drFoundRows = dtData.Select(sFilter)
        If Not drFoundRows Is Nothing AndAlso drFoundRows.Length > 0 Then
            iNextHistoNumber = CInt(drFoundRows(0)("NextHistologyRef"))
            If CStr(Session.Item(SessionVars.SV_HeaderUserArea)) = "Histopath" Then
                If ddlHistologyType.SelectedItem.Value.ToString = "" Then
                    If iHistoNumber >= iNextHistoNumber Then
                        Return False
                    Else
                        Return True
                    End If
                Else
                    If iHistoNumber >= iNextHistoNumber Then
                        Return False
                    Else
                        Return True
                    End If
                End If
            Else
                If iHistoNumber >= iNextHistoNumber Then
                    Return False
                Else
                    Return True
                End If
            End If
        Else
            Return False
        End If
    End Function

        Private Function ValidateData() As Boolean
        Try
            Dim dsBatchDetails As DataSet = CType(Session.Item(SessionVars.SV_BatchDetails), DataSet)
            Dim dtSubmittedAs As DataTable = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_SUBMITTEDAS_TABLE)
            Dim dvData As DataView
            Dim iNextHistoRef As Integer
            Dim iHistoRefType As String
            Dim bPreCassetted As Boolean
            Dim drFoundRows As DataRow()
            Dim bIsPGNumber As Boolean = False
            Dim iHistoNextNumber As Integer = 0
            Dim dDate As Date
            Dim objAnimal As New HistopathologyLib.clsAnimal
            Dim dtAnimalData As DataTable
            Dim bBeforeCurrentDate As Boolean = False

            dvData = CType(Session.Item(SessionVars.SV_BlockSummaryView), DataView)

            If Not dvData Is Nothing And dvData.Count = 0 Then
                ctlDiv.InnerHtml = "<p><font color=""Green"">Atleast one block must be added against the sample.</font></p>"
                Return False
            End If

            If HistologyRef1.Text <> "" AndAlso HistologyRef1.IsEnabled AndAlso ValidateHistoRef(HistologyRef1.Text, ddlHistologyType.Visible) Then
                If HistologyRef1.Text.IndexOf("HP") = -1 Then

                    iNextHistoRef = Convert.ToInt32(Right$((HistologyRef1.Text), 5))
                    iHistoRefType = CheckRange(iNextHistoRef)

                    If ddlHistologyType.Visible = True And Not ddlHistologyType.SelectedItem.Text = "use pg number" Then
                        'check that the chosen histology type and entered type are the same
                        If CStr(iHistoRefType) <> ddlHistologyType.SelectedItem.Value.ToString AndAlso ddlHistologyType.SelectedItem.Value.ToString <> "" Then
                            ctlDiv.InnerHtml = "<p><font color=""Red"">The Histology Ref entered does not match the range chosen from the drop down list.</font></p>"
                            Return False
                        End If
                        'Check that the Histology Refs do not exceed their range
                        Select Case iHistoRefType
                            Case HistologyRefType.eNeuropath
                                If iNextHistoRef >= 20000 Or iNextHistoRef < 10000 Then
                                    ctlDiv.InnerHtml = "<p><font color=""Red"">Neuropath Histology numbers must be in the range 10000-19999. The range has been exeeded.</font></p>"
                                    Return False
                                End If
                            Case HistologyRefType.eAbattoirSurvey
                                If iNextHistoRef >= 30000 Or iNextHistoRef < 20000 Then
                                    ctlDiv.InnerHtml = "<p><font color=""Red"">Abbattoir Survery Histology numbers must be in the range 20000-29999. The range has been exeeded.<font></p>"
                                    Return False
                                End If
                            Case HistologyRefType.eTBDiag
                                If iNextHistoRef >= 40000 Or iNextHistoRef < 30000 Then
                                    ctlDiv.InnerHtml = "<p><font color=""Red"">TB Diag Histology numbers must be in the range 30000-39000. The range has been exeeded.</font></p>"
                                    Return False
                                End If
                            Case HistologyRefType.eGeneralPool
                                If iNextHistoRef >= 60000 Or iNextHistoRef < 40000 Then
                                    ctlDiv.InnerHtml = "<p><font color=""Red"">General Pool Histology numbers must be in the range 40000-59000. The range has been exeeded.<font></p>"
                                    Return False
                                End If
                            Case HistologyRefType.eMouseProjects
                                If iNextHistoRef >= 90000 Or iNextHistoRef < 60000 Then
                                    ctlDiv.InnerHtml = "<p><font color=""Red"">Mouse Project Histology numbers must be in the range 60000-90000. The range has been exeeded.</font></p>"
                                    Return False
                                End If
                        End Select
                    End If
                Else
                    iHistoRefType = 0
                End If

                If CStr(Session.Item(SessionVars.SV_HeaderUserArea)) = "Neuropath" Then
                    bIsPGNumber = CheckIsPGNumber(SenderRef1.Text.ToString)
                Else
                    bIsPGNumber = False
                End If

                If Not bIsPGNumber And Not IsPreviousYearHistoRef(HistologyRef1.Text) Then
                    If iHistoRefType <> 0 Then
                        If Not CheckIfHistoRefIsValid(iHistoRefType, iNextHistoRef, iHistoNextNumber) Then
                            ctlDiv.InnerHtml = "<p><font color=""Red"">The Histology Ref entered is higher than or equal to the the current next Histology Ref (" & Right$(dDate.Now.Year.ToString, 2) & "/" & iHistoNextNumber.ToString & ") for the selected area.</font></p>"
                            Return False
                        End If
                    End If
                End If

                'Check that the Histology ref doesnt exist in the database already
                If HistologyRef1.Text <> "" Then
                    If Not objAnimal.GetAnimalByHistologyRef(HistologyRef1.Text, dtAnimalData) Then
                        Throw New Exception("Animal.GetAnimalbyHistologyRef returned false.")
                    Else
                        If dtAnimalData.Rows.Count > 0 Then
                            If Not IsDBNull(dtAnimalData.Rows(0)("SenderRef")) Then
                                ctlDiv.InnerHtml = "<p><font color=""Red"">The Histology Ref entered already exists. Please enter an alternative reference.</font></p>"
                                Return False
                            End If
                        End If
                    End If

                    'Check the Histology ref entered does not already exist on the submission.
                    Dim sBeforeHistologyRef = CType(Session.Item(SessionVars.SV_HistologyRef), String)
                    If Not sBeforeHistologyRef Is Nothing Then
                        If sBeforeHistologyRef <> "" And sBeforeHistologyRef <> HistologyRef1.Text Then
                            Dim drCheckAnimalDoesNotExist As DataRow()
                            drCheckAnimalDoesNotExist = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_ANIMAL).Select("HistologyRef='" & HistologyRef1.Text & "'")

                            If Not drCheckAnimalDoesNotExist Is Nothing Then
                                If drCheckAnimalDoesNotExist.Length > 0 Then
                                    ctlDiv.InnerHtml = "<p><font color=""Red"">The Histology Ref " & HistologyRef1.Text & "entered already exists on the submission. Please enter an alternative reference.</font></p>"
                                    Return False
                                End If
                            End If
                        End If
                    End If
                End If
            End If

            Return True
        Catch ex As Exception
            clsAppError.DisplayError("Failed to Validate the data on the 'Sample Details' page.", ex)
        End Try
    End Function

    Private Sub ProcessNextHistologyRef(ByVal iHistoType As Integer)
        Dim dsBatchDetails As DataSet = CType(Session.Item(SessionVars.SV_BatchDetails), DataSet)
        Dim objUsedHistologyNumbers As HistopathologyLib.clsIDPairs()
        Dim iAnimalID As Integer = CInt(Session.Item(SessionVars.SV_AnimalID))
        Dim objAnimal As New HistopathologyLib.clsAnimal
        Dim objHistology As New HistopathologyLib.clsHistology
        Dim foundRows As DataRow()
        Dim sFilter As String
        Dim sNextHistoRef As String
        Dim sLatestHistoRef As String
        Dim dDate As Date
        Dim dtData As DataTable
        Dim iHistoNumber As Integer
        Dim iPreviousHistoType

        If Not HistologyRef1.Text = "" And HistologyRef1.IsValid And Not objAnimal.CheckIfPGAnimal(dsBatchDetails, iAnimalID) And Not IsPreviousYearHistoRef(HistologyRef1.Text) Then

            If HistologyRef1.Text.IndexOf("HP") = -1 Then
                iHistoNumber = Convert.ToInt32(Right$(HistologyRef1.Text, 5))
                iPreviousHistoType = CheckRange(iHistoNumber)
            Else
                iPreviousHistoType = 0
            End If

            Dim dtUnUsedHistologyRefs As DataTable = CType(Session.Item(SessionVars.SV_UnusedHistologyRef), DataTable)
            Dim dtUsedHistologyRefs As DataTable = CType(Session.Item(SessionVars.SV_UsedHistologyRef), DataTable)

            If dtUnUsedHistologyRefs Is Nothing Then
                dtUnUsedHistologyRefs = objHistology.CreateUnusedHistologyRefs()
            End If

            If dtUsedHistologyRefs Is Nothing Then
                dtUsedHistologyRefs = objHistology.CreateUsedHistologyRefs()
            End If

            If iHistoType = 0 Then
                sNextHistoRef = objHistology.FindUnusedHistologyRef(dtUnUsedHistologyRefs, SenderRef1.Text, iPreviousHistoType)
            Else
                sNextHistoRef = objHistology.FindUnusedHistologyRef(dtUnUsedHistologyRefs, SenderRef1.Text, iHistoType)
            End If

            objHistology.AddUnusedHistologyRef(dtUnUsedHistologyRefs, SenderRef1.Text, HistologyRef1.Text, iPreviousHistoType)

            If Not String.IsNullOrEmpty(sNextHistoRef) Then
                HistologyRef1.Text = sNextHistoRef
            Else
                If objHistology.GetNextAvailableHistologyRef(iHistoType, sNextHistoRef) Then
                    HistologyRef1.Text = Right$(dDate.Now().Year(), 2) + "/" + sNextHistoRef
                    InitialiseHistoRefsTable()
                End If
            End If

            If Not String.IsNullOrEmpty(sNextHistoRef) Then
                objHistology.AddUsedHistologyRef(dtUsedHistologyRefs, sNextHistoRef, iHistoType)
            End If

            HistologyRef1.SetEnabled(False)
            Session.Item(SessionVars.SV_UnusedHistologyRef) = dtUnUsedHistologyRefs
        Else
            Dim dtUsedHistologyRefs As DataTable = CType(Session.Item(SessionVars.SV_UsedHistologyRef), DataTable)

            If dtUsedHistologyRefs Is Nothing Then
                dtUsedHistologyRefs = objHistology.CreateUsedHistologyRefs()
            End If

            If objHistology.GetNextAvailableHistologyRef(iHistoType, sNextHistoRef) Then
                HistologyRef1.Text = Right$(dDate.Now().Year(), 2) + "/" + sNextHistoRef
                InitialiseHistoRefsTable()
            End If
            HistologyRef1.SetEnabled(False)
            objHistology.AddUsedHistologyRef(dtUsedHistologyRefs, HistologyRef1.Text, iHistoType)

            Session.Item(SessionVars.SV_UsedHistologyRef) = dtUsedHistologyRefs
        End If
    End Sub

    Private Sub SetToolTips()
        btnAddBlock.ToolTip = ADD_BLOCK_TOOLTIP
        btnEditBlock.ToolTip = EDIT_BLOCK_TOOLTIP
        btnDeleteBlock.ToolTip = DELETE_BLOCK_TOOLTIP
    End Sub

#End Region

#Region "Event Handlers"

    Private Sub btnBlockRefSearch_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnBlockRefSearch.Click
        If ValidateMandatoryFields() Then
            UpdateSessionWithAnimalDetails()
            Session.Item(SessionVars.SV_SearchBlockRefsRedirectPage) = "SubmissionDetailsBlock.aspx"

            Try
                Dim objCrumbArrayList As ArrayList = CType(Session.Item(SessionVars.SV_BreadCrumbs), ArrayList)
                If Not objCrumbArrayList Is Nothing Then
                    objCrumbArrayList(1) = "Submission Samples"
                    objCrumbArrayList(2) = "Blocking"
                    objCrumbArrayList(3) = "Search Block Refs"
                    Session.Item(SessionVars.SV_BreadCrumbs) = objCrumbArrayList
                End If
            Catch ex As Exception
                clsAppError.DisplayError("Bread Crumb Error, SubmissionDetailsBlock.aspx.", ex)
            End Try

            Session.Item(SessionVars.SV_EditingBlock) = False
            Response.Redirect("SearchBlockRefs.aspx?HistologyRef=" & HistologyRef1.Text)
        End If
    End Sub

    Private Sub btnAddBlock_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnAddBlock.Click
        If IsBatchPreCassetted(CType(Session.Item(SessionVars.SV_BatchDetails), DataSet), CInt(Session.Item(SessionVars.SV_BatchID))) Then
            Dim objAnimal As New HistopathologyLib.clsAnimal
            Dim dtPreBookedBlocks As DataTable
            dtPreBookedBlocks = CType(Session.Item(SessionVars.SV_BatchDetails), DataSet).Tables(HistopathologyLib.clsBatch.ANIMAL_PREBOOKED_BLOCKS)

            If Not objAnimal.CheckPreBookedBlocksAvailable(CInt(Session.Item(SessionVars.SV_AnimalID)), dtPreBookedBlocks) Then
                ctlDiv.InnerHtml = "<p><font color=""Red"">There are no pre-booked blocks available for the selected sample.</font></p>"
                Exit Sub
            End If
        End If

        If ValidateMandatoryFields() Then
            UpdateSessionWithAnimalDetails()
            Session.Remove(SessionVars.Sv_BlockID)
            Session.Item(SessionVars.SV_HistologyRefType) = ddlHistologyType.SelectedItem.Value
            Session.Item(SessionVars.SV_EditingBlock) = False

            Try
                Dim objCrumbArrayList As ArrayList = CType(Session.Item(SessionVars.SV_BreadCrumbs), ArrayList)
                If Not objCrumbArrayList Is Nothing Then
                    objCrumbArrayList(1) = "Submission Samples"
                    objCrumbArrayList(2) = "Blocking"
                    objCrumbArrayList(3) = "Block Details"
                    Session.Item(SessionVars.SV_BreadCrumbs) = objCrumbArrayList
                End If
            Catch ex As Exception
                clsAppError.DisplayError("Bread Crumb Error, SubmissionDetailsBlock.aspx.", ex)
            End Try

            Response.Redirect("~/BlockDetails.aspx", True)
        End If
    End Sub

    Private Sub btnLookup_Click(ByVal sender As System.Object, ByVal e As System.EventArgs)
        Response.Redirect("SearchSender.aspx")
    End Sub

    Private Sub AddHistologyRefToUnUsedList()
        Dim dsBatchDetails As DataSet = CType(Session.Item(SessionVars.SV_BatchDetails), DataSet)
        Dim objUsedHistologyNumbers As HistopathologyLib.clsIDPairs()
        Dim iAnimalID As Integer = CInt(Session.Item(SessionVars.SV_AnimalID))
        Dim objAnimal As New HistopathologyLib.clsAnimal
        Dim objHistology As New HistopathologyLib.clsHistology
        Dim foundRows As DataRow()
        Dim sFilter As String
        Dim sNextHistoRef As String
        Dim sLatestHistoRef As String
        Dim dDate As Date
        Dim dtData As DataTable
        Dim iHistoNumber As Integer
        Dim iPreviousHistoType

        If Not HistologyRef1.Text = "" And HistologyRef1.IsValid And Not objAnimal.CheckIfPGAnimal(dsBatchDetails, iAnimalID) And Not IsPreviousYearHistoRef(HistologyRef1.Text) Then

            If HistologyRef1.Text.IndexOf("HP") = -1 Then
                iHistoNumber = Convert.ToInt32(Right$(HistologyRef1.Text, 5))
                iPreviousHistoType = CheckRange(iHistoNumber)
            Else
                iPreviousHistoType = 0
            End If

            Dim dtUnUsedHistologyRefs As DataTable = CType(Session.Item(SessionVars.SV_UnusedHistologyRef), DataTable)
            Dim dtUsedHistologyRefs As DataTable = CType(Session.Item(SessionVars.SV_UsedHistologyRef), DataTable)

            If dtUnUsedHistologyRefs Is Nothing Then
                dtUnUsedHistologyRefs = objHistology.CreateUnusedHistologyRefs()
            End If

            If dtUsedHistologyRefs Is Nothing Then
                dtUsedHistologyRefs = objHistology.CreateUsedHistologyRefs()
            End If

            objHistology.AddUnusedHistologyRef(dtUnUsedHistologyRefs, SenderRef1.Text, HistologyRef1.Text, iPreviousHistoType)

            Session.Item(SessionVars.SV_UnusedHistologyRef) = dtUnUsedHistologyRefs
        End If
    End Sub
    Private Sub btnCancel_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnCancel.Click
        Try
            Dim dsBatchDetails As DataSet = CType(Session.Item(SessionVars.SV_BatchDetails), DataSet)
            Dim dtBatch As DataTable = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_TABLE)
            Dim dtBatchBlocks As DataTable = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_TABLE)
            Dim dtAnimal As DataTable = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_ANIMAL)
            Dim iAnimalID = CInt(Session.Item(SessionVars.SV_AnimalID))
            Dim objAnimal As New HistopathologyLib.clsAnimal
            Dim sFilter As String
            Dim drFoundRows As DataRow()

            sFilter = "AnimalID=" & iAnimalID

            drFoundRows = dtBatchBlocks.Select(sFilter)

            If Not drFoundRows Is Nothing Then
                'If the animal is already in the datatable update its details, otherwise remove it
                If drFoundRows.Length > 0 Then
                    UpdateSessionWithAnimalDetails()
                Else
                    sFilter = "ID=" & iAnimalID

                    drFoundRows = dtAnimal.Select(sFilter)

                    If Not drFoundRows Is Nothing And drFoundRows.Length = 1 Then
                        dtAnimal.Rows.Remove(drFoundRows(0))

                        '---------
                        'neuropath stuff
                        'Only do this if this is the single animal on the submission/batch
                        If CStr(Session.Item(SessionVars.SV_HeaderUserArea)) = "Neuropath" And CType(Session.Item(SessionVars.SV_AssignBlocks), Boolean) = False Then
                            If Not dtAnimal Is Nothing Then
                                If dtAnimal.Rows.Count = 0 Then
                                    If Not dtBatch Is Nothing And dtBatch.Rows.Count > 0 Then
                                        dtBatch.Rows(0)("Species") = ""
                                        dtBatch.Rows(0)("ProjectContractCode") = DBNull.Value
                                        Session.Item(SessionVars.SV_ProjectCode) = ""
                                    End If
                                End If
                            End If
                        End If
                    End If
                End If
            End If

            If CStr(Session.Item(SessionVars.SV_HeaderUserArea)) = "Histopath" Then
                AddHistologyRefToUnUsedList()
            End If

            Session.Item(SessionVars.SV_HistologyRefType) = ""
            Session.Remove(Session.Item(SessionVars.SV_AnimalID))
            Session.Remove(Session.Item(SessionVars.SV_SenderRef))
            Session.Item(SessionVars.SV_OldPGNumber) = ""
            Session.Item(SessionVars.SV_PMDate) = ""
            Session.Item(SessionVars.SV_Species) = ""
            Session.Item(SessionVars.SV_EditingBlock) = False
        Catch ex As Exception
            clsAppError.DisplayError("Failed navigate to previous screen.", ex)
        End Try

        Try
            Dim objArrayList As ArrayList = CType(Session.Item(SessionVars.SV_BreadCrumbs), ArrayList)
            If Not objArrayList Is Nothing Then
                objArrayList(1) = "Submission Samples"
                objArrayList(2) = "Blocking"
                objArrayList(3) = "Add Sample"
                Session.Item(SessionVars.SV_BreadCrumbs) = objArrayList
            End If
        Catch ex As Exception
            clsAppError.DisplayError("Bread Crumb Error, SubmissionDetailsBlock.aspx.", ex)
        End Try

        Response.Redirect("AddSubmission.aspx")
    End Sub

    Private Sub btSubmit_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btSubmit.Click
        Dim sPrevPage As String
        Dim bPGNumber As Boolean
        Try
            Dim dsBatchDetails As DataSet = CType(Session.Item(SessionVars.SV_BatchDetails), DataSet)
            Dim iAnimalID = CInt(Session.Item(SessionVars.SV_AnimalID))
            Dim objAnimal As New HistopathologyLib.clsAnimal

            If CType(Session.Item(SessionVars.SV_ViewSubmission), Boolean) = False Then
                If ValidateMandatoryFields() Then
                    If ValidateData() Then
                        UpdateSessionWithAnimalDetails()
                    Else
                        Exit Sub
                    End If
                Else
                    Exit Sub
                End If
            End If
            Page.SmartNavigation = False
            Session.Item(SessionVars.SV_EditingBlock) = False
        Catch ex As Exception
            clsAppError.DisplayError("Error saving samples.", ex)
        End Try

        Try
            sPrevPage = CStr(Session.Item(SessionVars.SV_AddSamplePrevPage))

            If sPrevPage = "BatchBlockSummary.aspx" Then
                'Bread crumbs
                Dim objArrayList As ArrayList = CType(Session.Item(SessionVars.SV_BreadCrumbs), ArrayList)
                If Not objArrayList Is Nothing Then
                    objArrayList(1) = "Submission Samples"
                    objArrayList(2) = "Sample Summary"
                    objArrayList.RemoveAt(3)
                    Session.Item(SessionVars.SV_BreadCrumbs) = objArrayList
                End If
            End If
        Catch ex As Exception
            clsAppError.DisplayError("Bread Crumb Error, SubmissionDetailsBlock.aspx.", ex)
        End Try

        Response.Redirect(sPrevPage)

    End Sub

    Private Sub grdBlockSummary_ItemCommand(ByVal source As System.Object, ByVal e As System.Web.UI.WebControls.DataGridCommandEventArgs) Handles grdBlockSummary.ItemCommand
        Dim iCount As Int32 = e.Item.ItemIndex + 1

        If e.CommandName = "ExpandTissues" Then
            If CType(e.Item.Cells(2).Controls(0), LinkButton).Text = "<img src=""Images/minus.gif"">" Then
                Do While Not grdBlockSummary.Items(iCount).Cells(3).Text = "&nbsp;"
                    grdBlockSummary.Items(iCount).Visible = False
                    iCount += 1
                    If iCount >= grdBlockSummary.Items.Count Then Exit Do
                Loop
                CType(e.Item.Cells(2).Controls(0), LinkButton).Text = "<img src=""Images/plus.gif"">"
            Else
                'While the sender ref isnt empty show the tissue rows
                Do While Not grdBlockSummary.Items(iCount).Cells(3).Text = "&nbsp;"
                    HideControls(iCount)
                    iCount += 1
                    If iCount >= grdBlockSummary.Items.Count Then Exit Do
                Loop
                CType(e.Item.Cells(2).Controls(0), LinkButton).Text = "<img src=""Images/minus.gif"">"
            End If
        End If
    End Sub

    Private Sub grdBlockSummary_ItemDataBound(ByVal sender As Object, ByVal e As System.Web.UI.WebControls.DataGridItemEventArgs) Handles grdBlockSummary.ItemDataBound
        ' populate template column values here
        Try
            ' set up the checkbox and drop-down columns
            Dim drv As DataRowView = CType(e.Item.DataItem, DataRowView)
            If Not drv Is Nothing Then
                Dim lblFixationCode As Label = Nothing
                Dim cbEO As CheckBox = Nothing
                Dim cbHAndE As CheckBox = Nothing
                Dim cbHAndEBse As CheckBox = Nothing
                Dim cbSpecialStain As CheckBox = Nothing
                Dim cbIHCPrp As CheckBox = Nothing
                Dim cbIHCOther As CheckBox = Nothing
                Dim cbArchive As CheckBox = Nothing
                Dim cbSelect As CheckBox = Nothing

                If e.Item.ItemType = ListItemType.EditItem Then
                ElseIf e.Item.ItemType = ListItemType.Item _
                OrElse e.Item.ItemType = ListItemType.AlternatingItem _
                OrElse e.Item.ItemType = ListItemType.SelectedItem Then
                    ' populate display mode controls
                    lblFixationCode = CType(e.Item.FindControl("lblFixationCodeDisplay"), Label)
                    cbEO = CType(e.Item.FindControl("cbEODisplay"), CheckBox)
                    cbHAndE = CType(e.Item.FindControl("cbHAndEDisplay"), CheckBox)
                    cbHAndEBse = CType(e.Item.FindControl("cbHAndEBseDisplay"), CheckBox)
                    cbSpecialStain = CType(e.Item.FindControl("cbSpecialStainDisplay"), CheckBox)
                    cbIHCPrp = CType(e.Item.FindControl("cbIHCPrpDisplay"), CheckBox)
                    cbIHCOther = CType(e.Item.FindControl("cbIHCOtherDisplay"), CheckBox)
                    cbArchive = CType(e.Item.FindControl("cbArchiveDisplay"), CheckBox)
                    cbSelect = CType(e.Item.FindControl("cbSelected"), CheckBox)
                End If

                If Not lblFixationCode Is Nothing Then
                    If Not IsDBNull(drv("Fixation")) Then
                        lblFixationCode.Text = GetListType(drv("ation"), LOOKUP_FIXATIVE)
                    Else
                        lblFixationCode.Text = ""
                    End If
                End If

                If Not cbArchive Is Nothing Then
                    If Not IsDBNull(drv("Archive")) Then
                        cbArchive.Checked = drv("Archive")
                    Else
                        cbArchive.Checked = False
                    End If
                End If

                If Not cbEO Is Nothing Then
                    If Not IsDBNull(drv("EO")) Then
                        cbEO.Checked = drv("EO")
                    Else
                        cbEO.Checked = False
                    End If
                End If

                If Not cbHAndE Is Nothing Then
                    If Not IsDBNull(drv("HAndE")) Then
                        cbHAndE.Checked = drv("HAndE")
                    Else
                        cbHAndE.Checked = False
                    End If
                End If

                If Not cbHAndEBse Is Nothing Then
                    If Not IsDBNull(drv("HAndEBSE")) Then
                        cbHAndEBse.Checked = drv("HAndEBSE")
                    Else
                        cbHAndEBse.Checked = False
                    End If
                End If

                If Not cbSpecialStain Is Nothing Then
                    If Not IsDBNull(drv("SpecialStain")) Then
                        cbSpecialStain.Checked = drv("SpecialStain")
                    Else
                        cbSpecialStain.Checked = False
                    End If
                End If

                If Not cbIHCPrp Is Nothing Then
                    If Not IsDBNull(drv("IHCPrp")) Then
                        cbIHCPrp.Checked = drv("IHCPrp")
                    Else
                        cbIHCPrp.Checked = False
                    End If
                End If

                If Not cbIHCOther Is Nothing Then
                    If Not IsDBNull(drv("IHCOther")) Then
                        cbIHCOther.Checked = drv("IHCOther")
                    Else
                        cbIHCOther.Checked = False
                    End If
                End If

                If Not cbSelect Is Nothing Then
                    If CType(Session.Item(SessionVars.SV_ViewSubmission), Boolean) = True Then
                        cbSelect.Enabled = False
                    End If
                End If
            End If

        Catch ex As Exception
            clsAppError.DisplayError("Failed to bind template columns in the Batch summary grid", ex)
        End Try
    End Sub

    Private Sub btnEditBlock_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnEditBlock.Click
        If ValidateMandatoryFields() Then
            UpdateSessionWithAnimalDetails()
            Dim iID As Int32
            If grdBlockSummary.SelectedIndex >= 0 Then
                iID = Convert.ToInt32(grdBlockSummary.DataKeys(grdBlockSummary.SelectedIndex))
                Session.Item(SessionVars.Sv_BlockID) = iID
                Session.Item(SessionVars.SV_EditingBlock) = True

                Try
                    Dim objCrumbArrayList As ArrayList = CType(Session.Item(SessionVars.SV_BreadCrumbs), ArrayList)
                    If Not objCrumbArrayList Is Nothing Then
                        objCrumbArrayList(1) = "Submission Samples"
                        objCrumbArrayList(2) = "Blocking"
                        objCrumbArrayList(3) = "Block Details"
                        Session.Item(SessionVars.SV_BreadCrumbs) = objCrumbArrayList
                    End If
                Catch ex As Exception
                    clsAppError.DisplayError("Bread Crumb Error, SubmissionDetailsBlock.aspx.", ex)
                End Try

                Response.Redirect("BlockDetails.aspx")
            End If
        End If
    End Sub

    Private Sub btnDeleteBlock_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnDeleteBlock.Click
        Try
            Dim objBlockIDList As New ArrayList
            Dim dgDataGridItem As DataGridItem
            Dim cbSelected As CheckBox
            Dim iCount As Integer

            For Each dgDataGridItem In grdBlockSummary.Items
                cbSelected = CType(dgDataGridItem.FindControl("cbSelected"), CheckBox)
                If Not cbSelected Is Nothing Then
                    If cbSelected.Visible = True And cbSelected.Checked = True Then
                        objBlockIDList.Add(grdBlockSummary.DataKeys(dgDataGridItem.ItemIndex))
                    End If
                End If
            Next

            If objBlockIDList.Count > 0 Then
                Dim dsBatchDetails As DataSet = CType(Session.Item(SessionVars.SV_BatchDetails), DataSet)
                Dim objBlock As New HistopathologyLib.clsBlock
                iCount = 0
                For iCount = 0 To objBlockIDList.Count - 1
                    If Not objBlock.DeleteBlockData(dsBatchDetails, objBlockIDList(iCount), IsBatchPreCassetted(dsBatchDetails, CInt(Session.Item(SessionVars.SV_BatchID)))) Then
                        Throw New Exception("Block.DeleteBlockData returned false.")
                    End If
                Next

                InitialiseSummaryGrid()

                Session.Item(SessionVars.SV_EditingBlock) = False
            End If
        Catch ex As Exception
            clsAppError.DisplayError("Failed to delete block data.", ex)
        End Try
    End Sub

    Private Sub grdBlockSummary_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles grdBlockSummary.SelectedIndexChanged
        DisableEnableControls(True)

        Dim dgItem As DataGridItem = grdBlockSummary.SelectedItem
        Dim cbSelected As CheckBox = Nothing
        Dim cbOtherSelected As CheckBox = Nothing

        If Not dgItem Is Nothing Then
            cbSelected = dgItem.FindControl("cbSelected")

            If Not cbSelected Is Nothing Then
                If cbSelected.Enabled = False Then
                    Exit Sub
                End If
                For Each dgItem In grdBlockSummary.Items
                    cbOtherSelected = dgItem.FindControl("cbSelected")

                    If Not cbOtherSelected Is Nothing Then
                        cbOtherSelected.Checked = False
                    End If
                Next
                cbSelected.Checked = True
            End If
        End If

    End Sub

    Private Sub VLAHeader1_HomeClick(ByVal sender As Object, ByVal e As HistopathologySystem.HomeLinkEventArgs) Handles VLAHeader1.HomeClick
        If CType(Session.Item(SessionVars.SV_ViewSubmission), Boolean) = False Then
            Dim sMessage As System.Text.StringBuilder = New System.Text.StringBuilder

            If CType(Session.Item(SessionVars.SV_EditingBatch), Boolean) Then
                sMessage.Append("You are currently editing a submission. Any changes that you have made since you last saved the submission will be lost. Are you sure you wish to exit?")
            ElseIf CType(Session.Item(SessionVars.SV_CreatingNewBatch), Boolean) Then
                sMessage.Append("You are currently creating a new submission. If you exit now all the data you have entered will be lost. Are you sure you wish to exit?")
            ElseIf CType(Session.Item(SessionVars.SV_AssignBlocks), Boolean) Then
                sMessage.Append("You are currently assigning tissues to blocks. Any block assignment that you have completed since you last saved will be lost. Are you sure you wish to exit?")
            Else
                sMessage.Append("Any changes that have been made will be discarded, are you sure you wish to exit without saving?")
            End If

            Page.RegisterStartupScript("navigate", PromptBeforeNavigateScript(sMessage.ToString(), "Home.aspx"))
            e.bNavigateHome = False
        Else
            Response.Redirect("Home.aspx")
        End If
    End Sub

    Private Sub CreateUsedHistologyRefs()
        Dim UsedHistologyRefs As DataTable
        UsedHistologyRefs = New DataTable

        UsedHistologyRefs.Columns.Add("HistologyRef", System.Type.GetType("System.String"))
        UsedHistologyRefs.Columns.Add("SenderRef", System.Type.GetType("System.String"))
    End Sub

    Private Sub ddlHistologyType_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ddlHistologyType.SelectedIndexChanged
        Try
            Dim dsBatchDetails As DataSet = CType(Session.Item(SessionVars.SV_BatchDetails), DataSet)
            Dim dtAnimal As DataTable = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_ANIMAL)
            Dim iAnimalID As Integer = CInt(Session.Item(SessionVars.SV_AnimalID))
            Dim sFilter As String
            Dim foundRow As DataRow()

            If Not dtAnimal Is Nothing Then
                sFilter = "ID=" & iAnimalID
                foundRow = dtAnimal.Select(sFilter)

                Select Case ddlHistologyType.SelectedItem.Value
                    Case ""
                        foundRow(0)("IsPGNumber") = False
                        HistologyRef1.SetEnabled(True)
                        HistologyRef1.Text = String.Empty
                        Exit Sub
                    Case HistologyRefType.eNeuropath
                        ProcessNextHistologyRef(HistologyRefType.eNeuropath)
                        SenderRef1.DisplayError(False)
                        foundRow(0)("IsPGNumber") = False
                    Case HistologyRefType.eAbattoirSurvey
                        foundRow(0)("IsPGNumber") = False
                        ProcessNextHistologyRef(HistologyRefType.eAbattoirSurvey)
                        SenderRef1.DisplayError(False)
                    Case HistologyRefType.eTBDiag
                        foundRow(0)("IsPGNumber") = False
                        ProcessNextHistologyRef(HistologyRefType.eTBDiag)
                        SenderRef1.DisplayError(False)
                    Case HistologyRefType.eGeneralPool
                        foundRow(0)("IsPGNumber") = False
                        ProcessNextHistologyRef(HistologyRefType.eGeneralPool)
                        SenderRef1.DisplayError(False)
                    Case HistologyRefType.eMouseProjects
                        foundRow(0)("IsPGNumber") = False
                        ProcessNextHistologyRef(HistologyRefType.eMouseProjects)
                        SenderRef1.DisplayError(False)
                    Case HistologyRefType.eUsePGNumber
                        ProcessNextHistologyRef(0)
                        If DefaultHistoRefPGReverse(True) Then
                            foundRow(0)("IsPGNumber") = True
                        End If
                End Select
            End If
        Catch ex As Exception
            clsAppError.DisplayError("Error setting the Histology Reference after a change" _
                                     & " to the Histology reference drop down list", ex)
        End Try
    End Sub

    Private Sub lbExpandAll_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles lbExpandAll.Click
        SetHierarchical(True)
    End Sub

    Private Sub lbCollapseAll_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles lbCollapseAll.Click
        SetHierarchical(False)
    End Sub

    Public Sub Check_Clicked(ByVal sender As System.Object, ByVal e As System.EventArgs)
        Try
            Dim dgDataGridItem As DataGridItem
            Dim cbSelected As CheckBox
            Dim iCountSelected As Integer = 0

            For Each dgDataGridItem In grdBlockSummary.Items
                cbSelected = CType(dgDataGridItem.FindControl("cbSelected"), CheckBox)
                If Not cbSelected Is Nothing Then
                    If cbSelected.Visible = True And cbSelected.Checked = True Then
                        iCountSelected += 1
                    End If
                End If
            Next

            If iCountSelected > 0 Then
                DisableEnableControls(True)
                btnEditBlock.Enabled = False
            Else
                DisableEnableControls(False)
            End If

        Catch ex As Exception
            clsAppError.DisplayError("Failed to enable to update selected button.", ex)
        End Try
    End Sub

    Private Sub btnCopyBlock_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnCopyBlock.Click
        If ValidateMandatoryFields() Then
            UpdateSessionWithAnimalDetails()
            Dim iID As Int32
            Dim dgDataGridItem As DataGridItem
            Dim cbSelected As CheckBox
            Dim objBlockIDList As New ArrayList

            For Each dgDataGridItem In grdBlockSummary.Items
                cbSelected = CType(dgDataGridItem.FindControl("cbSelected"), CheckBox)
                If Not cbSelected Is Nothing Then
                    If cbSelected.Visible = True And cbSelected.Checked = True Then
                        objBlockIDList.Add(grdBlockSummary.DataKeys(dgDataGridItem.ItemIndex))
                    End If
                End If
            Next

            If objBlockIDList.Count > 0 Then
                Session.Item(SessionVars.SV_EditingBlock) = False
                Session.Item(SessionVars.SV_BlockIDs) = objBlockIDList

                Try
                    Dim objCrumbArrayList As ArrayList = CType(Session.Item(SessionVars.SV_BreadCrumbs), ArrayList)
                    If Not objCrumbArrayList Is Nothing Then
                        objCrumbArrayList(1) = "Submission Samples"
                        objCrumbArrayList(2) = "Blocking"
                        objCrumbArrayList(3) = "Copy Blocks"
                        Session.Item(SessionVars.SV_BreadCrumbs) = objCrumbArrayList
                    End If
                Catch ex As Exception
                    clsAppError.DisplayError("Bread Crumb Error, SubmissionDetailsBlock.aspx.", ex)
                End Try

                Response.Redirect("CopyBlocks.aspx")
            End If
        End If
    End Sub

    Private Sub cbSelectAll_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cbSelectAll.CheckedChanged
        Try
            Dim dgItem As DataGridItem
            Dim cbSelected As CheckBox = Nothing
            Dim bSelectAll As Boolean = cbSelectAll.Checked

            For Each dgItem In grdBlockSummary.Items
                cbSelected = CType(dgItem.FindControl("cbSelected"), CheckBox)

                If Not cbSelected Is Nothing Then
                    cbSelected.Checked = bSelectAll
                End If
            Next

            btnDeleteBlock.Enabled = bSelectAll

            'If not histopath user and tissues are not being assigned to blocks then disabled
            ' the copy blocks button
            If Not CStr(Session.Item(SessionVars.SV_HeaderUserArea)) = "Histopath" Or _
               Not CType(Session.Item(SessionVars.SV_AssignBlocks), Boolean) = True Then
                btnCopyBlock.Enabled = False
            Else
                btnCopyBlock.Enabled = bSelectAll
            End If

        Catch ex As Exception
            clsAppError.DisplayError("Failed to select all checkboxes.", ex)
        End Try
    End Sub

#End Region

#Region "Load Lookup Lists"
    
    Private Sub InitialiseHistoRefsTable()
        Try
            Dim objHistology As New HistopathologyLib.clsHistology
            Dim dtData As DataTable
            Dim dtTest As DataTable

            'Get the latest histo refs from the database
            If Not objHistology.GetHistologyRefsTable(dtData) Then
                Throw New Exception("Histology.GetHistologyRefsTable returned false.")
            End If

            Session.Item(SessionVars.SV_HistoRefsVersion) = dtData

        Catch ex As Exception
            clsAppError.DisplayError("Failed to initialise historefs table in session.", ex)
        End Try
    End Sub

    Private Sub LoadLookupLists()
        Try
            Dim objLookupData As New HistopathologyLib.LookupData
            Dim dtData As DataTable

            dtData = objLookupData.GetHistologyRefLookupData()

            If Not dtData Is Nothing Then
                ddlHistologyType.DataSource = dtData
                ddlHistologyType.DataValueField = "Code"
                ddlHistologyType.DataTextField = "Description"
                ddlHistologyType.DataBind()
                AddItemToDropDownList(ddlHistologyType)
            End If

        Catch ex As Exception
            clsAppError.DisplayError("Failed to retrieve 'Sender Ref' list.", ex)
        End Try
    End Sub

#End Region

    
End Class
