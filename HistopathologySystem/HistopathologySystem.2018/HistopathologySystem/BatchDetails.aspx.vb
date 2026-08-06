Imports System.Data.SqlClient

Partial Class BatchDetails
    Inherits System.Web.UI.Page
    Protected WithEvents lblText As System.Web.UI.WebControls.Label
    Protected WithEvents VLAHeader1 As VLAHeader
    Protected WithEvents Checkboxlist1 As System.Web.UI.WebControls.CheckBoxList
    Protected WithEvents ctlBatchDate As CalendarDate
    Protected WithEvents txtSampleOverride As System.Web.UI.WebControls.TextBox
    Protected WithEvents ctlReceivedDate As CalendarDate

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
        VLAHeader1.PageTitle = "Submission Details"
        SetCalendarDateHandler(Me.Page)
        ctlBatchDate.Mandatory = True

        If Not IsPostBack Then
            LoadLookupLists()
            InitialiseArrays()
            InitialiseScreenWithBatchDetails()
            LoadCheckBoxLists()
            InitialiseCheckBoxLists()
            'The user area should have had a value set in InitialiseScreenWithBatchDetails
            txtTmpSelectedProject.Text = ddlProjectCode.SelectedItem.Value
            txtTmpSelectedSpecies.Text = ddlSpecies.SelectedItem.Value
            EnableDisableControls()
            InitialiseNeuropathImport()
            AddConfirmDialogsToButtons()

            Dim sRedirectCancelPage As String = CStr(Session.Item(SessionVars.SV_RedirectCancelPage))
            If (CType(Session.Item(SessionVars.SV_EditingBatch), Boolean) = True And _
               Not sRedirectCancelPage = "EditBatch.aspx" And _
               Not sRedirectCancelPage = "ReceiveBatch.aspx") Or sRedirectCancelPage = "Home.aspx" Then
                If CType(Session.Item(SessionVars.SV_EditingBatch), Boolean) Then
                    PromptBeforeSaveScript("You are currently editing a submission. Any changes that you have made since you last saved the submission will be lost. Are you sure you wish to cancel?", btnCancel)
                ElseIf CType(Session.Item(SessionVars.SV_CreatingNewBatch), Boolean) Then
                    PromptBeforeSaveScript("You are currently creating a new submission. If you cancel now all the data you have entered will be lost. Are you sure you wish to cancel?", btnCancel)
                Else
                    PromptBeforeSaveScript("Any changes that have been made will be discarded, are you sure you wish to cancel without saving?", btnCancel)
                End If
            End If
        End If

        SetEnterKeyPress()
    End Sub

#Region "Lookup List Population"

    Private Sub LoadLookupLists()
        Dim objDataTable As DataTable

        Dim objProjectDataTable As DataTable
        Dim objContactDataTable As DataTable
        Dim objUsersDataTable As DataTable

        Dim objLookup As New HistopathologyLib.LookupData
        Dim objUsers As New HistopathologyLib.clsUser
        Dim dsBatchDetails As DataSet = CType(Session.Item(SessionVars.SV_BatchDetails), DataSet)

        Try
            'objDataTable = objLookup.GetLookupData(LOOKUP_SPECIES_TYPE)
            objDataTable = objLookup.GetSpeciesLookup()
            If Not (objDataTable Is Nothing) Then
                ddlSpecies.DataSource = objDataTable
                ddlSpecies.DataValueField = "SpeciesID"
                ddlSpecies.DataTextField = "Species"
                ddlSpecies.DataBind()
                Common.AddItemToDropDownList(ddlSpecies)
            End If

            objDataTable = objLookup.GetLookupData(LOOKUP_USER_AREA)
            If Not (objDataTable Is Nothing) Then
                ddlUserArea.DataSource = objDataTable
                ddlUserArea.DataValueField = "Code"
                ddlUserArea.DataTextField = "Description"
                ddlUserArea.DataBind()
                Common.AddItemToDropDownList(ddlUserArea)

                ddlEnteredArea.DataSource = objDataTable
                ddlEnteredArea.DataValueField = "Code"
                ddlEnteredArea.DataTextField = "Description"
                ddlEnteredArea.DataBind()
            End If

            objDataTable = objLookup.GetLookupData(LOOKUP_FIXATIVE)
            If Not (objDataTable Is Nothing) Then
                ddlFixation.DataSource = objDataTable
                ddlFixation.DataValueField = "Code"
                ddlFixation.DataTextField = "Description"
                ddlFixation.DataBind()
                Common.AddItemToDropDownList(ddlFixation)
            End If

            objDataTable = objUsers.GetUsers()
            If Not (objDataTable Is Nothing) Then
                ddlEnteredBy.DataSource = objDataTable
                ddlEnteredBy.DataValueField = "ID"
                ddlEnteredBy.DataTextField = "Name"
                ddlEnteredBy.DataBind()
            End If

            'LoadUserAreaSpecificLists(CStr(Session.Item(SessionVars.SV_HeaderUserAreaID)))

            'Setup the safe to handle drop down list
            AddItemToDropDownList(ddlSafeToHandle, "", "-1", 0)
            AddItemToDropDownList(ddlSafeToHandle, "Yes", True, 1)
            AddItemToDropDownList(ddlSafeToHandle, "No", False, 2)

        Catch ex As Exception
            clsAppError.DisplayError("Failed to retrieve 'Add Submission' drop down lists.", ex)
        End Try

    End Sub

    Private Sub LoadCheckBoxLists()
        Dim objDataTable As DataTable
        Dim objLookup As New HistopathologyLib.LookupData
        Dim dsSubmission As DataSet

        Try
            objDataTable = objLookup.GetHistologyLookupData()

            If Not objDataTable Is Nothing Then
                chkblHistology.DataSource = objDataTable
                chkblHistology.DataValueField = "Code"
                chkblHistology.DataTextField = "Description"
                chkblHistology.DataBind()
            End If

            HideOptions()

            'Check the submission type and load the correct list
            If CType(Session.Item(SessionVars.SV_SubmissionType), Integer) = SUBMISSION_NONTSE Then
                objDataTable = objLookup.GetLookupData(LOOKUP_NONTSE_ANTIBODIES)
            Else
                objDataTable = objLookup.GetLookupData(LOOKUP_TSE_ANTIBODIES)
            End If

            'Session(SessionVars.SV_SelectedItemsListArray) = objDataTable

            If Not objDataTable Is Nothing Then
                chkblAntibodies.DataSource = objDataTable
                chkblAntibodies.DataValueField = "Code"
                chkblAntibodies.DataTextField = "Description"
                chkblAntibodies.DataBind()
                chkblAntibodies.Enabled = False
                'Add "Other" to the chkBoxList
                Dim li As New ListItem
                li.Text = "Other"
                li.Value = "Other"
                chkblAntibodies.Items.Add(li)
            End If

            objDataTable = objLookup.GetLookupData(LOOKUP_SPECIAL_STAIN)

            If Not objDataTable Is Nothing Then
                chkblSpecialStain.DataSource = objDataTable
                chkblSpecialStain.DataValueField = "Code"
                chkblSpecialStain.DataTextField = "Description"
                chkblSpecialStain.DataBind()
                chkblSpecialStain.Enabled = False
                Dim li As New ListItem
                li.Text = "Other"
                li.Value = "Other"
                chkblSpecialStain.Items.Add(li)
            End If

        Catch ex As Exception
            clsAppError.DisplayError("Failed to retrieve 'Histology and Antibodies' lists.", ex)
        End Try
    End Sub

#End Region

#Region "Event Handlers"

    Private Sub cbSampleOverride_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cbSampleOverride.CheckedChanged
        Try
            If cbSampleOverride.Checked = False Then
                ddlProjectCode.Enabled = True
            Else
                ddlProjectCode.Enabled = False
            End If
        Catch ex As Exception
            clsAppError.DisplayError("Failed to change the sample override flag.", ex)
        End Try
    End Sub

    Private Sub ddlUserArea_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ddlUserArea.SelectedIndexChanged
        Try
            If ddlUserArea.Enabled = True Then
                LoadUserAreaSpecificLists(ddlUserArea.SelectedItem.Value)
            End If

        Catch ex As Exception
            clsAppError.DisplayError("Unable to refresh the projects and pathologist drop down lists.", ex)
        End Try
    End Sub

    Private Sub chkblSpecialStain_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles chkblSpecialStain.SelectedIndexChanged
        Try
            If chkblSpecialStain.Enabled = True Then
                Dim dsBatchDetails As DataSet = CType(Session.Item(SessionVars.SV_BatchDetails), DataSet)
                Dim bCassetted As Boolean = CType(Session.Item(SessionVars.SV_Cassetted), Boolean)
                Dim bTestAllocated As Boolean = False
                Dim dtStain As DataTable
                Dim sItemSelected As String
                Dim li As ListItem
                Dim aArray As ArrayList = CType(Session(SessionVars.SV_SelectedStainArray), ArrayList)
                li = GetCheckListSelectedItem(sItemSelected, aArray, chkblSpecialStain)

                If bCassetted And li.Selected = False Then
                    dtStain = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_STAIN)
                    bTestAllocated = HasTestBeenAllocated(dtStain, li.Value)
                End If

                If bTestAllocated Then
                    Page.RegisterStartupScript("StainPrompt", PromptScript("The test cannot be de-selected as it has been assigned to a block."))
                    If Not li Is Nothing Then
                        'The item text would have been removed from the array in GetCheckListSelectedItem.
                        'add it back.
                        li.Selected = True
                        aArray.Add(li.Text)
                    End If
                End If
            End If
        Catch ex As Exception
            clsAppError.DisplayError("Failed to select or deselect Stain.", ex)
        End Try
    End Sub

    Private Sub chkblAntibodies_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles chkblAntibodies.SelectedIndexChanged
        Try
            If chkblAntibodies.Enabled = True Then
                Dim dsBatchDetails As DataSet = CType(Session.Item(SessionVars.SV_BatchDetails), DataSet)
                Dim bCassetted As Boolean = CType(Session.Item(SessionVars.SV_Cassetted), Boolean)
                Dim bTestAllocated As Boolean = False
                Dim dtAntibodies As DataTable
                Dim sItemSelected As String
                Dim li As ListItem
                Dim aArray As ArrayList = CType(Session(SessionVars.SV_SelectedAntibodiesArray), ArrayList)
                li = GetCheckListSelectedItem(sItemSelected, aArray, chkblAntibodies)

                If bCassetted And li.Selected = False Then
                    dtAntibodies = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_ANTIBODIES)
                    bTestAllocated = HasTestBeenAllocated(dtAntibodies, li.Value)
                End If

                If bTestAllocated Then
                    Page.RegisterStartupScript("AntibodiesPrompt", PromptScript("The test cannot be de-selected as it has been assigned to a block."))
                    If Not li Is Nothing Then
                        'The item text would have been removed from the array in GetCheckListSelectedItem.
                        'add it back.
                        li.Selected = True
                        aArray.Add(li.Text)
                    End If
                End If
            End If
        Catch ex As Exception
            clsAppError.DisplayError("Failed to select or deselect Antibodies.", ex)
        End Try
    End Sub

    Private Sub btnCancel_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnCancel.Click
        Response.Redirect(CStr(Session.Item(SessionVars.SV_RedirectCancelPage)))
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

    Private Sub grdSubmissionSummary_ItemDataBound(ByVal sender As System.Object, ByVal e As System.Web.UI.WebControls.DataGridItemEventArgs)
        ' populate template column values here
        Try
            ' set up the checkbox and drop-down columns
            Dim drv As DataRowView = CType(e.Item.DataItem, DataRowView)
            If Not drv Is Nothing Then
                ' Dim lblFixationCode As Label = Nothing
                Dim cbEO As CheckBox = Nothing
                Dim cbHAndE As CheckBox = Nothing
                Dim cbHAndEBse As CheckBox = Nothing
                Dim cbSpecialStain As CheckBox = Nothing
                Dim cbIHCPrp As CheckBox = Nothing
                Dim cbIHCOther As CheckBox = Nothing

                If e.Item.ItemType = ListItemType.EditItem Then
                ElseIf e.Item.ItemType = ListItemType.Item _
                OrElse e.Item.ItemType = ListItemType.AlternatingItem _
                OrElse e.Item.ItemType = ListItemType.SelectedItem Then
                    ' populate display mode controls
                    cbEO = CType(e.Item.FindControl("cbEODisplay"), CheckBox)
                    cbHAndE = CType(e.Item.FindControl("cbHAndEDisplay"), CheckBox)
                    cbHAndEBse = CType(e.Item.FindControl("cbHAndEBseDisplay"), CheckBox)
                    cbSpecialStain = CType(e.Item.FindControl("cbSpecialStainDisplay"), CheckBox)
                    cbIHCPrp = CType(e.Item.FindControl("cbIHCPrpDisplay"), CheckBox)
                    cbIHCOther = CType(e.Item.FindControl("cbIHCOtherDisplay"), CheckBox)
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
            End If

        Catch ex As Exception
            clsAppError.DisplayError("Failed to bind template columns in the Submission summary grid", ex)
        End Try
    End Sub

    Private Function CheckTestsAllocated(ByVal dsBatchDetails As DataSet)
        Dim dtAntibodies As DataTable = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_ANTIBODIES)
        Dim dtStain As DataTable = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_STAIN)
        Dim dtHistology As DataTable = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_HISTOLOGY)

        If dtAntibodies.Rows.Count > 0 Or _
           dtStain.Rows.Count > 0 Or _
           dtHistology.Rows.Count > 0 Then

            Page.RegisterStartupScript("ArchivePrompt", PromptScript("Archive cannot be selected as tests have been assigned to blocks."))
            Return True
        End If

        Return False
    End Function

    Private Sub chkblHistology_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles chkblHistology.SelectedIndexChanged
        Try
            If chkblHistology.Enabled = True Then
                Dim dsBatchDetails As DataSet = CType(Session.Item(SessionVars.SV_BatchDetails), DataSet)
                Dim bCassetted As Boolean = CType(Session.Item(SessionVars.SV_Cassetted), Boolean)
                Dim bTestAllocated As Boolean = False
                Dim dtHistology As DataTable
                Dim sItemSelected As String
                Dim li As ListItem
                Dim aArray As ArrayList = CType(Session(SessionVars.SV_SelectedHistologyArray), ArrayList)
                li = GetCheckListSelectedItem(sItemSelected, aArray, chkblHistology)

                If bCassetted And li.Selected = False Then
                    dtHistology = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_HISTOLOGY)
                    bTestAllocated = HasTestBeenAllocated(dtHistology, li.Value)
                End If

                If Not bTestAllocated Then
                    If Not li Is Nothing Then
                        If li.Text = "Special Stain" Then
                            If li.Selected = True Then
                                chkblSpecialStain.Enabled = True
                                UnCheckArchive(aArray)
                            Else
                                DisableSpecialStains(aArray)
                            End If
                        ElseIf li.Text = "IHC - PrP" Then
                            If li.Selected = True Then
                                chkblAntibodies.Enabled = True
                                UnCheckArchive(aArray)
                            Else
                                DisableAntibodies(aArray)
                            End If
                        ElseIf li.Text = "IHC - Other" Then
                            If li.Selected = True Then
                                chkblAntibodies.Enabled = True
                                UnCheckArchive(aArray)
                            Else
                                DisableAntibodies(aArray)
                            End If
                        ElseIf li.Text = "Archive" Then
                            If li.Selected = True Then
                                If bCassetted Then
                                    bTestAllocated = CheckTestsAllocated(dsBatchDetails)
                                Else
                                    bTestAllocated = False
                                End If

                                'If no tests allocated then disable all options
                                'If they have de-select Archive again.
                                If Not bTestAllocated Then
                                    DisableAllOptions(aArray)
                                Else
                                    li.Selected = False
                                    aArray.Remove(li.Text)
                                End If
                            End If
                        Else
                            UnCheckArchive(aArray)
                        End If
                    End If
                Else
                    Page.RegisterStartupScript("HistologyPrompt", PromptScript("The test cannot be de-selected as it has been assigned to a block."))
                    If Not li Is Nothing Then
                        'The item text would have been removed from the array in GetCheckListSelectedItem.
                        'add it back.
                        li.Selected = True
                        aArray.Add(li.Text)
                    End If
                End If
            End If
        Catch ex As Exception
            clsAppError.DisplayError("Failed to select or deselect HistologyType.", ex)
        End Try
    End Sub

    Private Sub btnBatchSummary_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnBatchSummary.Click
        If CheckHistology() Then
            UpdateSessionWithBatchDetails()
            Session.Item(SessionVars.SV_BatchSubmissionID) = 0

            Try
                Dim objArrayList As ArrayList = CType(Session.Item(SessionVars.SV_BreadCrumbs), ArrayList)
                If Not objArrayList Is Nothing Then
                    objArrayList(1) = "Submission Samples"
                    objArrayList(2) = "Sample Summary"
                    Session.Item(SessionVars.SV_BreadCrumbs) = objArrayList
                End If
            Catch ex As Exception
                clsAppError.DisplayError("Bread Crumb Error, BatchDetails.aspx.", ex)
            End Try

            If CType(Session.Item(SessionVars.SV_Cassetted), Boolean) = True Then
                Response.Redirect("BatchBlockSummary.aspx")
            Else
                Response.Redirect("BatchSummary.aspx")
            End If
        End If
    End Sub

    Private Sub btnSave_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnSave.Click
        Dim dsBatchDetails As DataSet = CType(Session.Item(SessionVars.SV_BatchDetails), DataSet)
        Dim dDate As Date
        Dim bValid As Boolean = True
        Dim dReceivedDate As Date

        If Not dsBatchDetails Is Nothing AndAlso dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_ANIMAL_TABLE).Rows.Count > 0 Then
            If Not IsDBNull(dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_TABLE).Rows(0)("DateReceived")) Then
                dReceivedDate = CType(dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_TABLE).Rows(0)("DateReceived"), Date)
            End If
        End If

        If Not ValidateMandatoryFields() Then
            Exit Sub
        End If

        'Check the received date
        If ctlReceivedDate.Enabled = True Then
            If Not ctlReceivedDate.Validate(dDate, CDate(dReceivedDate.ToShortDateString), ctlReceivedDate.ValidationType.eValidateEarliest, "Must the same or later than the Submission received date of " & dReceivedDate.ToShortDateString) Or _
                Not ctlReceivedDate.Validate(dDate, dDate.Date.Now.ToShortDateString, ctlReceivedDate.ValidationType.eValidateLatest, "Must be today or earlier") Then
                ctlDIV.InnerHtml = "<p><font color=""Red"">Not all mandatory fields have been completed, hover the mouse pointer over the red stars for details.</font></p>"
                Exit Sub
            End If
        End If

        'Dont validate the submitted date if we are editing the batch
        If CType(Session.Item(SessionVars.SV_EditingBatch), Boolean) = False And _
           CType(Session.Item(SessionVars.SV_ReceiveBatch), Boolean) = False Then
            bValid = ctlBatchDate.Validate(dDate, CDate(dDate.Date.Now().ToShortDateString), ctlReceivedDate.ValidationType.eValidateEarliest, "Must be today or later")
        End If

        If ValidateData() Then
            If CheckHistology() Then
                If bValid Then
                    Dim bRedirect As Boolean = False
                    Dim objErrorlist As New ArrayList
                    Dim objBatch As New HistopathologyLib.clsBatch
                    Dim iBatchID As Integer

                    UpdateSessionWithBatchDetails()

                    'If editing the batch go back the the edit screen, save from there
                    If CType(Session.Item(SessionVars.SV_SaveFromBatchDetails), Boolean) = False Then
                        Response.Redirect(CStr(Session.Item(SessionVars.SV_RedirectPage)))
                    End If

                    'Set the batch so we know its been blocked
                    If CType(Session.Item(SessionVars.SV_Cassetted), Boolean) = True Then
                        dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_TABLE).Rows(0)("IsBlocked") = True
                    Else
                        dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_TABLE).Rows(0)("IsBlocked") = False
                    End If

                    Dim bSuccess As Boolean = objBatch.UpdateBatchDetails(CType(Session(SessionVars.SV_HeaderUserID), Integer), dsBatchDetails, objErrorlist, CType(Session.Item(SessionVars.SV_Cassetted), Boolean), iBatchID, Nothing, IsBatchPreCassetted(dsBatchDetails, Session.Item(SessionVars.SV_BatchID)), CType(Session.Item(SessionVars.SV_UnusedHistologyRef), DataTable))
                    If bSuccess Then
                        If objErrorlist.Count = 0 Then
                            bRedirect = True
                        Else
                            ctlDIV.InnerHtml = "<p><font color=""Red"">The database has been updated but some errors were encountered:</font></p><p>&nbsp;</p><p><font color=""Red"">" & Join(objErrorlist.ToArray, "</font></p><p>") & "</p>"
                        End If
                    Else
                        ctlDIV.InnerHtml = "<p><font color=""Red"">The database has not been updated because the following error(s) occurred:</font></p><p>&nbsp;</p><p><font color=""Red"">" & Join(objErrorlist.ToArray, "</font></p><p>") & "</p>"
                    End If

                    Session.Item(SessionVars.SV_BatchDetails) = dsBatchDetails
                    Session.Item(SessionVars.SV_BatchID) = iBatchID
                    Session.Item(SessionVars.SV_UnusedHistologyRef) = Nothing

                    If bRedirect Then
                        Dim sRedirectPage As String
                        Try
                            sRedirectPage = CStr(Session.Item(SessionVars.SV_RedirectPage))
                            If sRedirectPage = "FinalPrintBatch.aspx" Then
                                'Bread crumbs
                                Dim objArrayList As ArrayList = CType(Session.Item(SessionVars.SV_BreadCrumbs), ArrayList)
                                If Not objArrayList Is Nothing Then
                                    objArrayList(1) = "Submission"
                                    objArrayList(2) = "Print Submission"
                                    Session.Item(SessionVars.SV_BreadCrumbs) = objArrayList
                                End If
                            End If
                        Catch ex As Exception
                            clsAppError.DisplayError("Bread Crumb Error, BatchDetails.aspx.", ex)
                        End Try

                        Response.Redirect(sRedirectPage)
                    End If
                End If
            End If
        End If
    End Sub

    Private Sub btnNewSubmittedBy_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnNewSubmittedBy.Click
        UpdateSessionWithBatchDetails()

        Session.Item(SessionVars.SV_PassUserArea) = ddlUserArea.SelectedItem.Value
        Response.Redirect("UserMaintenance.aspx")
    End Sub

    Private Sub btnNewProject_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnNewProject.Click
        UpdateSessionWithBatchDetails()

        Session.Item(SessionVars.SV_PickListTableID) = LOOKUP_PROJECTS
        Session.Item(SessionVars.SV_PassUserArea) = ddlUserArea.SelectedItem.Value
        Response.Redirect("PickListUserArea.aspx")

    End Sub

    Private Sub btnNewContact_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnNewContact.Click
        UpdateSessionWithBatchDetails()

        Session.Item(SessionVars.SV_PickListTableID) = LOOKUP_CONTACTS
        Session.Item(SessionVars.SV_PassUserArea) = ddlUserArea.SelectedItem.Value
        Response.Redirect("PickListUserArea.aspx")
    End Sub

    Private Sub ddlProjectCode_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ddlProjectCode.SelectedIndexChanged
        Try
            Dim dsBatchDetails As DataSet = CType(Session.Item(SessionVars.SV_BatchDetails), DataSet)
            Dim dtSamples As DataTable
            Dim sPreviousSelectedProject As String = txtTmpSelectedProject.Text

            If Not dsBatchDetails Is Nothing Then
                If CType(Session.Item(SessionVars.SV_Cassetted), Boolean) = True Then
                    dtSamples = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_TABLE)
                Else
                    dtSamples = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_SUBMISSION_TABLE)
                End If

                txtTmpSelectedProject.Text = ddlProjectCode.SelectedItem.Value

                'if samples have been added and the project code dropdown is changing, display confirmation dialog
                If Not sPreviousSelectedProject = "" And dtSamples.Rows.Count > 0 And ddlProjectCode.SelectedItem.Value <> sPreviousSelectedProject Then
                    Page.RegisterStartupScript("ProjectChangePrompt", PromptBeforeChangeScript("Are you sure you want to change the project code?", ddlProjectCode, sPreviousSelectedProject, txtTmpSelectedProject))
                End If

            End If
        Catch ex As Exception
            clsAppError.DisplayError("Unable to select new Project.", ex)
        End Try
    End Sub

    Private Sub ddlSpecies_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ddlSpecies.SelectedIndexChanged
        Try

            Dim dsBatchDetails As DataSet = CType(Session.Item(SessionVars.SV_BatchDetails), DataSet)
            Dim dtSamples As DataTable
            Dim sPreviousSelectedSpecies As String = txtTmpSelectedSpecies.Text

            If Not dsBatchDetails Is Nothing Then
                If CType(Session.Item(SessionVars.SV_Cassetted), Boolean) = True Then
                    dtSamples = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_TABLE)
                Else
                    dtSamples = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_SUBMISSION_TABLE)
                End If

                txtTmpSelectedSpecies.Text = ddlSpecies.SelectedItem.Value

                'if samples have been added and the project code dropdown is changing, display confirmation dialog
                If Not sPreviousSelectedSpecies = "" And dtSamples.Rows.Count > 0 And ddlSpecies.SelectedItem.Value <> sPreviousSelectedSpecies Then
                    Page.RegisterStartupScript("SpeciesPrompt", PromptBeforeChangeScript("Are you sure you want to change the species?", ddlSpecies, sPreviousSelectedSpecies, txtTmpSelectedSpecies))
                End If
            End If

        Catch ex As Exception
            clsAppError.DisplayError("Unable to select new Species.", ex)
        End Try

    End Sub

#End Region

#Region "Handle Data"

    Private Function UpdateSessionWithBatchDetails() As Boolean
        Dim dsBatchData As DataSet = Session.Item(SessionVars.SV_BatchDetails)
        Dim iID As Integer = CInt(Session.Item(SessionVars.SV_BatchID))
        Try
            If Not dsBatchData Is Nothing Then
                'Update the batch details
                If dsBatchData.Tables(HistopathologyLib.clsBatch.BATCH_TABLE).Rows.Count > 0 Then
                    With dsBatchData.Tables(HistopathologyLib.clsBatch.BATCH_TABLE).Rows(0)
                        .Item("ProjectContractCode") = FormatEmptyString(ddlProjectCode.SelectedItem.Value())
                        .Item("ContactName") = FormatEmptyString(ddlContactName.SelectedItem.Value())
                        .Item("Species") = FormatEmptyString(ddlSpecies.SelectedItem.Value())
                        .Item("BatchDate") = FormatEmptyString(ctlBatchDate.DateField)
                        If Session(SessionVars.SV_SubmissionType) = SUBMISSION_TSE Then
                            .Item("BatchType") = 0
                        Else
                            .Item("BatchType") = 1
                        End If

                        If ddlSafeToHandle.SelectedItem.Value <> "-1" Then
                            .Item("SafeToHandle") = FormatEmptyString(ddlSafeToHandle.SelectedItem.Value())
                        End If
                        'Set the submission status to not received
                        If CType(Session.Item(SessionVars.SV_EditingBatch), Boolean) = False And _
                            CType(Session.Item(SessionVars.SV_ReceiveBatch), Boolean) = False Then
                            .Item("BatchStatus") = 1
                        End If

                        .Item("SubmittedBy") = FormatEmptyString(ddlEnteredBy.SelectedItem.Value())
                        .Item("SubmittedArea") = FormatEmptyString(ddlEnteredArea.SelectedItem.Value())

                        Dim sBatchS As String = .Item("BatchStatus").ToString()
                        'If editing the batch that has been rejected switch it back to submitted.
                        If CType(Session.Item(SessionVars.SV_EditingBatch), Boolean) = True And _
                           CType(Session.Item(SessionVars.SV_ReceiveBatch), Boolean) = False And _
                           .Item("BatchStatus") = 3 Then
                            .Item("SubmittedBy") = Convert.ToInt32(Session.Item(SessionVars.SV_HeaderUserID))
                            .Item("SubmittedArea") = CStr(Session.Item(SessionVars.SV_HeaderUserAreaID))
                            .Item("BatchStatus") = 1
                        End If

                        'If the customer is viewing the submission and the status of the submission is rejected
                        'set the status back to submitted
                        If CType(Session.Item(SessionVars.SV_ViewSubmission), Boolean) = True And _
                            .Item("BatchStatus").ToString() = HistopathologyLib.clsBatch.STATUS_REJECTED Then
                            .Item("BatchStatus") = 1
                        End If

                        .Item("OtherSubmittedBy") = FormatEmptyString(ddlSubmittedBy.SelectedItem.Value())
                        .Item("OtherSubmittedArea") = FormatEmptyString(ddlUserArea.SelectedItem.Value())
                        .Item("Fixation") = FormatEmptyString(ddlFixation.SelectedItem.Value())
                        'Dont want to change the cassetted field if we are editing the batch
                        If CType(Session.Item(SessionVars.SV_EditingBatch), Boolean) = False Then
                            .Item("Cassetted") = CInt(Session.Item(SessionVars.SV_Cassetted))
                        End If
                        .Item("Comments") = FormatEmptyString(txtComments.Text())
                        .Item("CustomerReceivedDate") = FormatEmptyString(ctlReceivedDate.DateField)
                        .Item("SampleSameProjects") = cbSampleOverride.Checked
                    End With
                End If

                'Check the values in the antibodies, stains checkbox list and add to datatable. Also make a copy datatable just
                'the antibodies, stains for this batch so when a new batch is added we can set the defaults.

                If Not chkblSpecialStain.Enabled = False Then
                    UpdateCheckBoxData(chkblSpecialStain, dsBatchData.Tables(HistopathologyLib.clsBatch.BATCH_STAIN_TABLE), iID)
                End If

                If Not chkblAntibodies.Enabled = False Then
                    UpdateCheckBoxData(chkblAntibodies, dsBatchData.Tables(HistopathologyLib.clsBatch.BATCH_ANTIBODIES_TABLE), iID)
                End If

                If Not chkblHistology.Enabled = False Then
                    UpdateCheckBoxData(chkblHistology, dsBatchData.Tables(HistopathologyLib.clsBatch.BATCH_HISTOLOGY_TABLE), iID)
                End If
            End If

        Catch ex As Exception
            clsAppError.DisplayError("Failed to Save Batch Details.", ex)
            Return False
        End Try
        Return True
    End Function

    Private Sub ProcessProjectCode()
        Try
            Dim dsBatchDetails As DataSet = CType(Session.Item(SessionVars.SV_BatchDetails), DataSet)
            Dim dtBatch As DataTable = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_TABLE)
            Dim sProjectCode As String
            Dim iNewRowID As Integer
            Dim objLookup As New HistopathologyLib.LookupData
            Dim drFoundRows As DataRow()
            Dim drFoundSavedRow As DataRow()
            Dim sFilter As String
            Dim drNew As DataRow
            Dim objProjectDataTable As DataTable
            Dim sSubmittedArea As String = CStr(Session.Item(SessionVars.SV_HeaderUserAreaID))

            sProjectCode = CStr(Session.Item(SessionVars.SV_ProjectCode))
            'Only update the project if the session item is not null and the project download is enabled.
            'if the project download is disabled this means that the project has already been set.
            If Not sProjectCode = "" And ddlProjectCode.Enabled = True Then

                Dim dtProjectsData As DataTable = objLookup.GetLookupData(LOOKUP_PROJECTS, True)

                If dtProjectsData Is Nothing Then Throw New Exception

                'Check if the project code associated with the animal is present in the projects code list
                'if not add it and add it to the drop down
                If Not dtProjectsData Is Nothing Then
                    sFilter = "Description=" & "'" & sProjectCode & "' AND Area =1"

                    drFoundRows = dtProjectsData.Select(sFilter)

                    If Not drFoundRows Is Nothing And drFoundRows.Length = 0 Then
                        drNew = dtProjectsData.NewRow()
                        drNew("Description") = sProjectCode
                        drNew("IsActive") = True
                        drNew("Area") = sSubmittedArea
                        dtProjectsData.Rows.Add(drNew)

                        If Not objLookup.SaveLookupData(LOOKUP_PROJECTS, dtProjectsData, CInt(Session.Item(SessionVars.SV_HeaderUserID))) Then
                            Throw New Exception("Failed to add new neuropath project to the lookup list.")
                        End If

                        sFilter = "Description=" & "'" & sProjectCode & "'"
                        drFoundSavedRow = dtProjectsData.Select(sFilter)

                        If Not drFoundSavedRow Is Nothing And drFoundSavedRow.Length > 0 Then
                            'Update the batch level project code with the new code added
                            If IsDBNull(dtBatch.Rows(0)("ProjectContractCode")) Or dtBatch.Rows(0)("ProjectContractCode").ToString() = "" Then
                                dtBatch.Rows(0)("ProjectContractCode") = drFoundSavedRow(0)("ID")
                            End If

                            objProjectDataTable = objLookup.GetProjectsByArea("1") ' Get the neuropath list
                            If Not (objProjectDataTable Is Nothing) Then
                                ddlProjectCode.DataSource = objProjectDataTable
                                ddlProjectCode.DataValueField = "ID"
                                ddlProjectCode.DataTextField = "Description"
                                ddlProjectCode.DataBind()
                                Common.AddItemToDropDownList(ddlProjectCode)
                            End If
                        End If
                    Else
                        If IsDBNull(dtBatch.Rows(0)("ProjectContractCode")) Or dtBatch.Rows(0)("ProjectContractCode").ToString() = "" Then
                            dtBatch.Rows(0)("ProjectContractCode") = drFoundRows(0)("ID")
                        End If
                    End If
                Else
                    Throw New Exception("ProjectsList is nothing.")
                End If
            End If
            Session.Item(SessionVars.SV_ProjectCode) = ""
        Catch ex As Exception
            clsAppError.DisplayError("Failed to Display Batch Details.", ex)
        End Try
    End Sub

    Private Sub InitialiseScreenWithBatchDetails()
        Try
            Dim dsBatchData As DataSet = Session.Item(SessionVars.SV_BatchDetails)
            Dim cbChecked As Object = Nothing
            Dim foundRows As DataRow()
            Dim iBatchID As Integer = CType(Session.Item(SessionVars.SV_BatchID), Integer)

            If Not dsBatchData Is Nothing Then
                If dsBatchData.Tables(HistopathologyLib.clsBatch.BATCH_TABLE).Rows.Count > 0 Then
                    With dsBatchData.Tables(HistopathologyLib.clsBatch.BATCH_TABLE).Rows(0)
                        SelectItemInDropDownList(ddlSpecies, .Item("Species").ToString())
                        SelectItemInDropDownList(ddlSafeToHandle, .Item("SafeToHandle").ToString())
                        SelectItemInDropDownList(ddlFixation, .Item("Fixation").ToString())

                        If .Item("OtherSubmittedArea").ToString() <> "" Then
                            LoadUserAreaSpecificLists(.Item("OtherSubmittedArea").ToString())
                        Else
                            LoadUserAreaSpecificLists(CStr(Session.Item(SessionVars.SV_HeaderUserAreaID)))
                        End If

                        If CType(Session.Item(SessionVars.SV_EditingBatch), Boolean) = True Or _
                           CType(Session.Item(SessionVars.SV_ViewSubmission), Boolean) = True Then
                            SelectItemInDropDownList(ddlEnteredBy, .Item("SubmittedBy").ToString())
                            SelectItemInDropDownList(ddlEnteredArea, .Item("SubmittedArea").ToString())
                        Else
                            SelectItemInDropDownList(ddlEnteredBy, CStr(Session.Item(SessionVars.SV_HeaderUserID)))
                            SelectItemInDropDownList(ddlEnteredArea, CStr(Session.Item(SessionVars.SV_HeaderUserAreaID)))
                        End If

                        SelectItemInDropDownList(ddlUserArea, .Item("OtherSubmittedArea").ToString())
                        'Default the user area if it doesnt have a value
                        If ddlUserArea.SelectedIndex = 0 Then
                            SelectItemInDropDownList(ddlUserArea, CType(Session.Item(SessionVars.SV_HeaderUserAreaID), String))
                        End If

                        SelectItemInDropDownList(ddlContactName, .Item("ContactName").ToString())

                        If CStr(Session.Item(SessionVars.SV_HeaderUserArea)) = "Neuropath" Then
                            ProcessProjectCode()
                        End If

                        SelectItemInDropDownList(ddlProjectCode, .Item("ProjectContractCode").ToString())
                        'Default the project code, species and fixation if they dont have a value and is mouse
                        If CStr(Session.Item(SessionVars.SV_HeaderUserArea)) = "Mouse Bioassay" Then
                            If ddlProjectCode.SelectedIndex = 0 Then
                                SelectItemInDropDownList(ddlProjectCode, "1")
                            End If

                            If ddlFixation.SelectedIndex = 0 Then
                                SelectItemInDropDownList(ddlFixation, "1")
                            End If

                            If ddlSpecies.SelectedIndex = 0 Then
                                SelectDescriptionInDropDownList(ddlSpecies, "Murine")
                            End If
                        End If

                        SelectItemInDropDownList(ddlSubmittedBy, .Item("OtherSubmittedBy").ToString())
                        'Default the submitted by field if it doesnt have a value
                        If ddlSubmittedBy.SelectedIndex = 0 Then
                            SelectItemInDropDownList(ddlSubmittedBy, CType(Session.Item(SessionVars.SV_HeaderUserID), String))
                        End If

                        ctlBatchDate.DateField = .Item("BatchDate").ToString()

                        'Default the batch date if it doesnt have a value
                        If ctlBatchDate.DateField = "" Then
                            Dim newDate As Date
                            ctlBatchDate.DateField = newDate.Now()
                            ctlBatchDate.Enabled = True
                        End If

                        txtComments.Text = .Item("Comments").ToString()
                        ctlReceivedDate.DateField = .Item("CustomerReceivedDate").ToString()
                        cbChecked = GetRowColumnData(.Item("SampleSameProjects"))
                        If cbChecked Is Nothing Then
                            If CStr(Session.Item(SessionVars.SV_HeaderUserArea)) = "Neuropath" Then
                                cbSampleOverride.Checked = True
                            Else
                                cbSampleOverride.Checked = False
                            End If
                        Else
                            cbSampleOverride.Checked = GetRowColumnData(.Item("SampleSameProjects"))
                        End If
                    End With
                End If

                If dsBatchData.Tables(HistopathologyLib.clsBatch.BATCH_SUBMITTEDAS_TABLE).Rows.Count <> 0 Then
                    Dim sSubmittedAs As String
                    foundRows = dsBatchData.Tables(HistopathologyLib.clsBatch.BATCH_SUBMITTEDAS_TABLE).Select("BatchID=" & Convert.ToString(iBatchID))
                    'Will only ever been one now that only one submitted as can be selected
                    If Not foundRows Is Nothing Then
                        If foundRows.Length > 0 Then
                            sSubmittedAs = foundRows(0)("Code").ToString()
                            txtSubmittedAs.Text = GetListType(sSubmittedAs, LOOKUP_SUBMITTEDAS)
                        End If
                    End If

                End If
            End If

        Catch ex As Exception
            clsAppError.DisplayError("Failed to Display Batch Details.", ex)
        End Try
    End Sub

#End Region

#Region "Checkbox list functions"

    Private Sub HideOptions()
        Dim li As ListItem
        Dim iCount As Integer = 0

        'Only display the Histology columns which are relevent to the type of submission
        If Session(SessionVars.SV_SubmissionType) = SUBMISSION_NONTSE Then
            For iCount = chkblHistology.Items.Count - 1 To 0 Step -1
                'Get rid of the IHC-Prp & H&E(BSE) options for Non TSE
                If chkblHistology.Items(iCount).Value = "4" Or chkblHistology.Items(iCount).Value = "5" Then
                    chkblHistology.Items.RemoveAt(iCount)
                End If
            Next
        Else
            For Each li In chkblHistology.Items
                'Get rid of the IHC-Other option for TSE
                If li.Value = "6" Then
                    chkblHistology.Items.Remove(li)
                    Exit For
                End If
            Next
        End If
    End Sub

#End Region

#Region "Private Functions"
    Private Function SampleOverrideScript() As String

        Dim jScript As System.Text.StringBuilder = New System.Text.StringBuilder
        With jScript
            .Append("var bChecked;" + vbNewLine)
            .Append("bChecked = document.forms[0].cbSampleOverride.checked;" + vbNewLine)
            .Append("if (confirm(""")
            .Append("Are you sure you want to change the project override flag?")
            .Append("""))")
            .Append("{" + vbNewLine)
            .Append("}" + vbNewLine)
            .Append("else" + vbNewLine)
            .Append("{" + vbNewLine)
            .Append("   document.forms[0].cbSampleOverride.checked = !bChecked;" + vbNewLine)
            .Append("}" + vbNewLine)
        End With
        Return jScript.ToString()

    End Function

    Private Sub SetEnterKeyPress()
        SetDropDownControlOnEnter(ddlSubmittedBy, btnNewSubmittedBy.ClientID)
        SetDropDownControlOnEnter(ddlUserArea, ddlProjectCode.ClientID)
        SetDropDownControlOnEnter(ddlProjectCode, btnNewProject.ClientID)
        ctlBatchDate.SetDropDownOnEnter(ddlContactName.ClientID)
        SetDropDownControlOnEnter(ddlContactName, btnNewContact.ClientID)
        SetDropDownControlOnEnter(ddlFixation, ddlSpecies.ClientID)
        SetDropDownControlOnEnter(ddlSpecies, ddlSafeToHandle.ClientID)

        If cbSampleOverride.Visible AndAlso cbSampleOverride.Enabled Then
            SetDropDownControlOnEnter(ddlSafeToHandle, cbSampleOverride.ClientID)
        End If

        ctlReceivedDate.SetDefaultButton(btnSave)
    End Sub

    Private Sub InitialiseArrays()
        Try
            'These arrays keep track of which items are selected in the checkbox lists
            Dim aSelectedHistologyArray As New ArrayList
            Dim aSelectedAntibodiesArray As New ArrayList
            Dim aSelectedStainArray As New ArrayList

            Session.Item(SessionVars.SV_SelectedHistologyArray) = aSelectedHistologyArray
            Session.Item(SessionVars.SV_SelectedStainArray) = aSelectedStainArray
            Session.Item(SessionVars.SV_SelectedAntibodiesArray) = aSelectedAntibodiesArray

        Catch ex As Exception
            clsAppError.DisplayError("Failed to initialise checkbox selection arrays.", ex)
        End Try
    End Sub

    Private Sub InitialiseCheckBoxLists()
        Try
            Dim sRowFilter As String
            Dim iBatchID As Integer = CType(Session.Item(SessionVars.SV_BatchID), Integer)
            Dim dsBatchData As DataSet = CType(Session.Item(SessionVars.SV_BatchDetails), DataSet)
            Dim dtDataTable As DataTable
            Dim foundRows As DataRow()
            Dim dr As DataRow
            Dim li As ListItem
            Dim aHistologyArray As ArrayList = CType(Session.Item(SessionVars.SV_SelectedHistologyArray), ArrayList)
            Dim aStainArray As ArrayList = CType(Session.Item(SessionVars.SV_SelectedStainArray), ArrayList)
            Dim aAntibodiesArray As ArrayList = CType(Session.Item(SessionVars.SV_SelectedAntibodiesArray), ArrayList)

            sRowFilter = "BatchID=" & Convert.ToString(iBatchID)
            'Update the selected histology

            If dsBatchData.Tables(HistopathologyLib.clsBatch.BATCH_STAIN_TABLE).Rows.Count <> 0 Then
                dtDataTable = dsBatchData.Tables(HistopathologyLib.clsBatch.BATCH_STAIN_TABLE)
                foundRows = dtDataTable.Select(sRowFilter)
                For Each dr In foundRows
                    For Each li In chkblSpecialStain.Items
                        If dr("Code") = li.Value Then
                            chkblSpecialStain.Enabled = True
                            li.Selected = True
                            aStainArray.Add(li.Text)
                        End If
                    Next
                Next
            End If

            If dsBatchData.Tables(HistopathologyLib.clsBatch.BATCH_ANTIBODIES_TABLE).Rows.Count <> 0 Then
                dtDataTable = dsBatchData.Tables(HistopathologyLib.clsBatch.BATCH_ANTIBODIES_TABLE)
                foundRows = dtDataTable.Select(sRowFilter)
                For Each dr In foundRows
                    For Each li In chkblAntibodies.Items
                        If dr("Code") = li.Value Then
                            chkblAntibodies.Enabled = True
                            li.Selected = True
                            aAntibodiesArray.Add(li.Text)
                        End If
                    Next
                Next
            End If

            If dsBatchData.Tables(HistopathologyLib.clsBatch.BATCH_HISTOLOGY_TABLE).Rows.Count <> 0 Then
                dtDataTable = dsBatchData.Tables(HistopathologyLib.clsBatch.BATCH_HISTOLOGY_TABLE)
                foundRows = dtDataTable.Select(sRowFilter)
                For Each dr In foundRows
                    For Each li In chkblHistology.Items
                        If dr("Code") = li.Value Then
                            li.Selected = True
                            aHistologyArray.Add(li.Text)
                        End If
                    Next
                Next
            End If

        Catch ex As Exception
            clsAppError.DisplayError("Failed to initialise checkbox lists.", ex)
        End Try
    End Sub

    Private Sub LoadUserAreaSpecificLists(ByVal sUserArea As String)
        Try
            Dim objUsers As New HistopathologyLib.clsUser
            Dim objContactDataTable As DataTable
            Dim objProjectDataTable As DataTable
            Dim objUsersDataTable As DataTable
            Dim objLookup As New HistopathologyLib.LookupData

            objContactDataTable = objLookup.GetContactsByArea(sUserArea)
            If Not (objContactDataTable Is Nothing) Then
                ddlContactName.DataSource = objContactDataTable
                ddlContactName.DataValueField = "ID"
                ddlContactName.DataTextField = "Description"
                ddlContactName.DataBind()
                Common.AddItemToDropDownList(ddlContactName)
            End If

            objProjectDataTable = objLookup.GetProjectsByArea(sUserArea)
            If Not (objProjectDataTable Is Nothing) Then
                ddlProjectCode.DataSource = objProjectDataTable
                ddlProjectCode.DataValueField = "ID"
                ddlProjectCode.DataTextField = "Description"
                ddlProjectCode.DataBind()
                Common.AddItemToDropDownList(ddlProjectCode)
            End If

            objUsersDataTable = objUsers.GetUsers()

            If Not (objUsersDataTable Is Nothing) Then
                ddlSubmittedBy.DataSource = objUsersDataTable
                ddlSubmittedBy.DataValueField = "ID"
                ddlSubmittedBy.DataTextField = "Name"
                ddlSubmittedBy.DataBind()
                Common.AddItemToDropDownList(ddlSubmittedBy)
            End If

        Catch ex As Exception
            clsAppError.DisplayError("Unable to refresh the projects and pathologist drop down lists.", ex)
        End Try

    End Sub

    Private Function HasTestBeenAllocated(ByVal dtData As DataTable, ByVal sCode As String) As Boolean
        Dim bFound As Boolean = False
        Dim drFoundRows As DataRow()

        drFoundRows = dtData.Select("Code=" & "'" & sCode & "'")

        If drFoundRows.Length > 0 Then
            Return True
        Else
            Return False
        End If
    End Function

    Private Function ValidateMandatoryFields() As Boolean
        Try
            rfvSubmittedBy.Validate()
            rvfProjectContract.Validate()
            rvfContact.Validate()
            rvfSpecies.Validate()
            rfvSubmittedArea.Validate()
            rfvSafeToHandle.Validate()

            If Not rfvSubmittedBy.IsValid Or _
               Not rvfProjectContract.IsValid Or _
               Not rvfContact.IsValid Or _
               Not rvfSpecies.IsValid Or _
               Not rfvSubmittedArea.IsValid Or _
               Not rfvSafeToHandle.IsValid Or _
               Not ctlBatchDate.IsComplete Then
                ctlDIV.InnerHtml = "<p><font color=""Red"">Not all mandatory fields have been completed, hover the mouse pointer over the red stars for details.</font></p>"
                Return False
            End If

            Return True
        Catch ex As Exception
            clsAppError.DisplayError("Failed to validate Mandatory fields.", ex)
        End Try
    End Function

    Private Sub CheckImportFromDayBook()
        Try
            If Not CType(Session.Item(SessionVars.SV_ViewSubmission), Boolean) = True Then
                If CType(Session.Item(SessionVars.SV_ImportedFromDayBook), Boolean) = True Or _
                   CType(Session.Item(SessionVars.SV_EditingBatch), Boolean) = True Then
                    If cbSampleOverride.Checked = True Then
                        'Only set the fields to disabled if they have a value set
                        If ddlProjectCode.SelectedIndex > 0 Then
                            ddlProjectCode.Enabled = False
                        End If
                    End If
                    If ddlSpecies.SelectedIndex > 0 Then
                        ddlSpecies.Enabled = False
                    End If
                Else
                    ddlSpecies.Enabled = True
                    ddlProjectCode.Enabled = True
                End If
            End If
        Catch ex As Exception
            clsAppError.DisplayError("Failed to set the Species and Project based on import from daybook.", ex)
        End Try
    End Sub

    Private Sub AddConfirmDialogsToButtons()
        cbSampleOverride.Attributes.Add("onClick", SampleOverrideScript())
    End Sub

    Private Sub InitialiseNeuropathImport()
        Try
            'Hide the daybook integration for non neuropath users
            If Not CStr(Session.Item(SessionVars.SV_HeaderUserArea)) = "Neuropath" Then
                cbSampleOverride.Visible = False
                lblOverride.Visible = False
            Else
                CheckImportFromDayBook()
            End If
        Catch ex As Exception
            clsAppError.DisplayError("Failed to initailise Neuropath controls.", ex)
        End Try
    End Sub

    Private Function ValidateData() As Boolean
        'need to check there is atleast a sample or block on the submission

        Try
            Dim dsBatchDetails As DataSet = CType(Session.Item(SessionVars.SV_BatchDetails), DataSet)
            Dim bCassetted As Boolean = CType(Session.Item(SessionVars.SV_Cassetted), Boolean)
            Dim dtData As DataTable

            If bCassetted Then
                dtData = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_TABLE)

                If dtData.Rows.Count = 0 Then
                    ctlDIV.InnerHtml = "<p><font color=""Red"">Atleast one Block must be added to the Submission.</font></p>"
                    Return False
                End If
            Else
                dtData = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_SUBMISSION_TABLE)

                If dtData.Rows.Count = 0 Then
                    ctlDIV.InnerHtml = "<p><font color=""Red"">Atleast one Sample must be added to the Submission.</font></p>"
                    Return False
                End If
            End If

            Return True
        Catch ex As Exception
            clsAppError.DisplayError("Failed to Validate the batch details.", ex)
        End Try

    End Function

    Private Sub DisableSpecialStains(ByRef aArray As ArrayList)
        Dim li As ListItem

        For Each li In chkblSpecialStain.Items
            If li.Selected = True Then
                aArray.Remove(li.Text.ToString())
                li.Selected = False
            End If
        Next
        chkblSpecialStain.Enabled = False
    End Sub

    Private Sub DisableAntibodies(ByRef aArray As ArrayList)
        Dim li As ListItem

        For Each li In chkblAntibodies.Items
            If li.Selected = True Then
                aArray.Remove(li.Text.ToString)
                li.Selected = False
            End If
        Next
        chkblAntibodies.Enabled = False
    End Sub

    Private Sub DisableAllOptions(ByRef aArray As ArrayList)
        Dim li As ListItem

        For Each li In chkblHistology.Items
            If li.Text = "IHC - Other" Or li.Text = "IHC - PrP" Then
                li.Selected = False
                aArray.Remove(li.Text.ToString())
                DisableAntibodies(aArray)
            ElseIf li.Text = "Special Stain" Then
                li.Selected = False
                aArray.Remove(li.Text.ToString())
                DisableSpecialStains(aArray)
            ElseIf li.Text = "Archive" Then
                'dont do anything
            Else
                li.Selected = False
                aArray.Remove(li.Text.ToString())
            End If
        Next
    End Sub

    Private Sub UnCheckArchive(ByRef aArray As ArrayList)
        Dim li As ListItem
        For Each li In chkblHistology.Items
            If li.Text = "Archive" Then
                li.Selected = False
                aArray.Remove(li.Text.ToString())
            End If
        Next
    End Sub

    Private Sub EnableDisableControls()
        Try
            Dim dsBatchDetails As DataSet = CType(Session.Item(SessionVars.SV_BatchDetails), DataSet)
            Dim dtBatch As DataTable
            Dim sBatchStatus As String

            'If a user is viewing a submission do not allow them to edit it if it has been received
            ', submitted or completed.
            If Not dsBatchDetails Is Nothing Then
                dtBatch = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_TABLE)
                sBatchStatus = dtBatch.Rows(0)("BatchStatus").ToString()

                If CType(Session.Item(SessionVars.SV_ViewSubmission), Boolean) = True Then
                    ddlSubmittedBy.Enabled = False
                    ddlProjectCode.Enabled = False
                    ddlContactName.Enabled = False
                    ddlSpecies.Enabled = False
                    ddlUserArea.Enabled = False
                    ctlBatchDate.Enabled = False
                    ddlFixation.Enabled = False
                    ddlSafeToHandle.Enabled = False
                    chkblHistology.Enabled = False
                    chkblAntibodies.Enabled = False
                    chkblSpecialStain.Enabled = False
                    btnSave.Enabled = False
                    ctlReceivedDate.Enabled = False
                    ctlReceivedDate.Visible = True
                    lblCustomerReceivedDate.Visible = True
                    txtComments.Enabled = False
                    btnBatchSummary.Enabled = True
                    btnNewSubmittedBy.Enabled = False
                    btnNewProject.Enabled = False
                    btnNewContact.Enabled = False
                    cbSampleOverride.Enabled = False
                    txtSubmittedAs.Enabled = False
                    ddlEnteredBy.Enabled = False
                    ddlEnteredArea.Enabled = False
                ElseIf CType(Session.Item(SessionVars.SV_ReceiveBatch), Boolean) = True Then
                    ddlSubmittedBy.Enabled = False
                    ddlProjectCode.Enabled = False
                    ddlContactName.Enabled = False
                    ddlSpecies.Enabled = False
                    ddlUserArea.Enabled = False
                    ctlBatchDate.Enabled = False
                    ddlFixation.Enabled = False
                    ddlSafeToHandle.Enabled = False
                    chkblHistology.Enabled = False
                    chkblAntibodies.Enabled = False
                    chkblSpecialStain.Enabled = False
                    btnSave.Enabled = True
                    ctlReceivedDate.Enabled = True
                    ctlReceivedDate.Visible = True
                    ctlReceivedDate.SetCalendarFocus()
                    lblCustomerReceivedDate.Visible = True
                    txtComments.Enabled = False
                    btnBatchSummary.Enabled = False
                    btnNewSubmittedBy.Enabled = False
                    btnNewProject.Enabled = False
                    btnNewContact.Enabled = False
                    cbSampleOverride.Enabled = False
                    txtSubmittedAs.Enabled = False
                    ddlEnteredBy.Enabled = False
                    ddlEnteredArea.Enabled = False
                ElseIf CType(Session.Item(SessionVars.SV_EditingBatch), Boolean) = True Then
                    ddlSubmittedBy.Enabled = True
                    ddlProjectCode.Enabled = True
                    ddlContactName.Enabled = True
                    ddlSpecies.Enabled = True
                    ctlBatchDate.Enabled = True
                    ddlFixation.Enabled = True
                    ddlSafeToHandle.Enabled = True
                    chkblHistology.Enabled = True
                    btnSave.Enabled = True
                    ctlReceivedDate.Enabled = False
                    ctlReceivedDate.Visible = False
                    lblCustomerReceivedDate.Visible = False
                    txtComments.Enabled = True
                    btnBatchSummary.Enabled = True
                    btnNewSubmittedBy.Enabled = True
                    btnNewProject.Enabled = True
                    btnNewContact.Enabled = True
                    cbSampleOverride.Enabled = True
                    ddlEnteredBy.Enabled = False
                    ddlEnteredArea.Enabled = False
                    If CStr(Session.Item(SessionVars.SV_HeaderUserArea)) = "Histopath" Then
                        ddlUserArea.Enabled = True
                    Else
                        ddlUserArea.Enabled = False
                    End If
                    txtSubmittedAs.Enabled = False
                Else
                    ddlSubmittedBy.Enabled = True
                    ddlProjectCode.Enabled = True
                    ddlContactName.Enabled = True
                    ddlSpecies.Enabled = True
                    ctlBatchDate.Enabled = True
                    ddlFixation.Enabled = True
                    ddlSafeToHandle.Enabled = True
                    chkblHistology.Enabled = True
                    btnSave.Enabled = True
                    ctlReceivedDate.Enabled = False
                    ctlReceivedDate.Visible = False
                    lblCustomerReceivedDate.Visible = False
                    btnBatchSummary.Enabled = True
                    btnNewSubmittedBy.Enabled = True
                    btnNewProject.Enabled = True
                    btnNewContact.Enabled = True
                    cbSampleOverride.Enabled = True
                    ddlEnteredBy.Enabled = False
                    ddlEnteredArea.Enabled = False
                    If CStr(Session.Item(SessionVars.SV_HeaderUserArea)) = "Histopath" Then
                        ddlUserArea.Enabled = True
                    Else
                        ddlUserArea.Enabled = False
                    End If
                    txtSubmittedAs.Enabled = False
                End If
            End If

            'Only allow the finish button to be enabled if either 1 block or 1 sample has been added to 
            ' the submission.
            If btnSave.Enabled = True Then
                If CType(Session.Item(SessionVars.SV_Cassetted), Boolean) = True Then
                    If dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_TABLE).Rows.Count = 0 Then
                        btnSave.Enabled = False
                    End If
                Else
                    If dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_SUBMISSION_TABLE).Rows.Count = 0 Then
                        btnSave.Enabled = False
                    End If
                End If
            End If

        Catch ex As Exception
            clsAppError.DisplayError("Failed to disable controls on the Submission details page.", ex)
        End Try
    End Sub

    Private Function CheckHistology() As Boolean
        Try
            Dim iSelectedIndex As Integer = chkblHistology.SelectedIndex
            'Check atleast one histology has have been selected
            If Not chkblHistology.Enabled = False Then
                If iSelectedIndex = -1 Then
                    lblError.Visible = True
                    lblError.ToolTip = "Must add atleast one histology to the block."
                    ctlDIV.InnerHtml = "<p><font color=""Red"">Not all mandatory fields have been completed, hover the mouse pointer over the red stars for details.</font></p>"
                    Return False
                Else
                    Dim li As ListItem
                    For Each li In chkblHistology.Items
                        'Special Stain selected check atleast 1 stain has been selected
                        If (li.Value = 3 And li.Selected = True) Then
                            If chkblSpecialStain.SelectedIndex = -1 Then
                                lblError.Visible = True
                                lblError.ToolTip = "Special stain selected, atleast one stain must be selected."
                                ctlDIV.InnerHtml = "<p><font color=""Red"">Not all mandatory fields have been completed, hover the mouse pointer over the red stars for details.</font></p>"
                                Return False
                            End If
                        End If

                        'IHC Selected, check atleast 1 test has been selected
                        If (li.Value = 4 And li.Selected) _
                            Or (li.Value = 6 And li.Selected = True) Then
                            If chkblAntibodies.SelectedIndex = -1 Then
                                lblError.Visible = True
                                lblError.ToolTip = "IHC selected, atleast one test must be selected."
                                ctlDIV.InnerHtml = "<p><font color=""Red"">Not all mandatory fields have been completed, hover the mouse pointer over the red stars for details.</font></p>"
                                Return False
                            End If
                        End If
                    Next
                End If
            End If
            Return True
        Catch ex As Exception
            clsAppError.DisplayError("Failed to check if tests are selected.", ex)
        End Try
    End Function

    Private Function GetCheckListSelectedItem(ByRef sText As String, ByVal aArray As ArrayList, ByVal chkbl As CheckBoxList) As ListItem
        'This function is used to get the item in the ComboboxList that has just been selected.
        'Using comboboxList.selectedItem always returns the lowest indexed selected item rather
        'than the item just selected.

        Dim li As ListItem
        For Each li In chkbl.Items
            If li.Selected = True Then
                sText = li.Text
                If Not aArray.Contains(sText) Then
                    aArray.Add(sText)
                    Return li
                End If
            Else
                sText = li.Text
                If aArray.Contains(sText) Then
                    aArray.Remove(sText)
                    Return li
                End If
            End If
        Next
        Return li
    End Function

#End Region


End Class

