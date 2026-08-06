Partial Class CopySamples
    Inherits System.Web.UI.Page

    Protected WithEvents VLAHeader1 As VLAHeader
#Region " Web Form Designer Generated Code "

    'This call is required by the Web Form Designer.
    <System.Diagnostics.DebuggerStepThrough()> Private Sub InitializeComponent()

    End Sub

    'NOTE: The following placeholder declaration is required by the Web Form Designer.
    'Do not delete or move it.
    Private designerPlaceholderDeclaration As System.Object

    Private Sub Page_Init(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Init
        'CODEGEN: This method call is required by the Web Form Designer
        'Do not modify it using the code editor.
        InitializeComponent()
    End Sub

#End Region

    Private Sub Page_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load
        VLAHeader1.PageTitle = "Copy Samples"
        CheckPermissions()

        SetTextboxDefaultButton(txtSubmissionID, btnGo)

        If Not IsPostBack Then
            InitialiseDropDownLists()
            InitialiseSubmissionNumber()
            btnSummary.Enabled = False
        End If

        SetFocus(txtSubmissionID)
    End Sub

#Region "Initialise"

    Private Sub InitialiseSubmissionNumber()
        Try
            Dim dsPreviousBatchDetails As DataSet = CType(Session.Item(SessionVars.SV_OldBatchDetails), DataSet)

            If Not dsPreviousBatchDetails Is Nothing AndAlso dsPreviousBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_TABLE).Rows.Count > 0 Then
                txtSubmissionID.Text = dsPreviousBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_TABLE).Rows(0)("ID")
            End If

        Catch ex As Exception
            clsAppError.DisplayError("Error initialising the submission number.", ex)
        End Try
    End Sub

    Private Sub InitialiseDropDownLists()
        Try
            Dim dsPreviousBatchDetails As DataSet = CType(Session.Item(SessionVars.SV_OldBatchDetails), DataSet)
            Dim dsCurrentBatchDetails As DataSet = CType(Session.Item(SessionVars.SV_BatchDetails), DataSet)

            If Not dsPreviousBatchDetails Is Nothing Then
                Dim dtPreviousAnimal As DataTable = Nothing
                Dim dtCurrentAnimal As DataTable = Nothing
                Dim dvPreviousAnimal As DataView
                Dim dvCurrentAnimal As DataView
                Dim objAnimal As New HistopathologyLib.clsAnimal

                dtPreviousAnimal = dsPreviousBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_ANIMAL)

                If Not objAnimal.GetAnimalsForBatch(CInt(Session.Item(SessionVars.SV_BatchID)), dtCurrentAnimal) Then
                    Throw New Exception("Animal.GetAnimalsForBatch returned false.")
                End If

                Session.Item(SessionVars.SV_AnimalTable) = dtCurrentAnimal

                btnSummary.Enabled = True

                dvPreviousAnimal = dtPreviousAnimal.DefaultView
                dvPreviousAnimal.Sort = "SenderRef ASC"

                dvCurrentAnimal = dtCurrentAnimal.DefaultView
                dvCurrentAnimal.Sort = "SenderRef ASC"

                ddlCopySampleFrom.DataSource = dvPreviousAnimal
                ddlCopySampleFrom.DataValueField = "ID"
                ddlCopySampleFrom.DataTextField = "SenderRef"
                ddlCopySampleFrom.DataBind()
                AddItemToDropDownList(ddlCopySampleFrom)

                ddlCopySampleTo.DataSource = dvCurrentAnimal
                ddlCopySampleTo.DataValueField = "ID"
                ddlCopySampleTo.DataTextField = "SenderRef"
                ddlCopySampleTo.DataBind()
                AddItemToDropDownList(ddlCopySampleTo)
            End If

        Catch ex As Exception
            clsAppError.DisplayError("Error initialising the dropdown lists", ex)
        End Try
    End Sub

#End Region

#Region "Private Functions"

    Private Sub CheckPermissions()
        VLAHeader1.getUserDetails()
        Dim sGroupName As String = Session(SessionVars.SV_HeaderGroupName)
        If sGroupName = "Customer" Then
            Response.Redirect("Home.aspx")
        ElseIf sGroupName = "Histopathology User" Then
            'Nothing
        ElseIf sGroupName = "Maintenance" Then
            'Nothing
        Else
            Response.Redirect("Home.aspx")
        End If
    End Sub

    Private Function ValidateSubmissionNumber() As Boolean
        Try
            rfvSubmissionID.Validate()
            revSubmissionID.Validate()

            If Not rfvSubmissionID.IsValid Or _
            Not revSubmissionID.IsValid() Then
                ctlDiv.InnerHtml = "<p><font color=""Red"">Not all mandatory fields have been completed, hover the mouse pointer over the red stars for details.</font></p>"
                Return False
            End If

            Return True
        Catch ex As Exception
            clsAppError.DisplayError("Failed to validate Mandatory fields on CopySamples page.", ex)
        End Try

    End Function

    Private Function ValidateDropDownLists() As Boolean
        Try
            rfvCopySampleFrom.Validate()
            rfvCopySampleTo.Validate()

            If Not rfvCopySampleFrom.IsValid Or _
                Not rfvCopySampleTo.IsValid Then
                ctlDiv.InnerHtml = "<p><font color=""Red"">Not all mandatory fields have been completed, hover the mouse pointer over the red stars for details.</font></p>"
                Return False
            End If

            Return True
        Catch ex As Exception
            clsAppError.DisplayError("Failed to validate Mandatory fields on CopySamples page.", ex)
        End Try
    End Function

    Private Function GetSubmissionData() As Boolean
        Dim dsBatchDetails As DataSet = CType(Session.Item(SessionVars.SV_OldBatchDetails), DataSet)
        GetCommonBatchDetailsFromDatabase(CInt(txtSubmissionID.Text), Session, SessionVars.SV_OldBatchDetails)

        dsBatchDetails = CType(Session.Item(SessionVars.SV_OldBatchDetails), DataSet)
        If dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_TABLE).Rows.Count = 0 Then
            dsBatchDetails = Nothing
            Return False
        Else
            If dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_TABLE).Rows(0)("IsBlocked") = 0 Then
                dsBatchDetails = Nothing
                Return False
            Else
                GetBatchBlockDetailsFromDatabase(CInt(txtSubmissionID.Text), Session, SessionVars.SV_OldBatchDetails)
            End If
        End If

        Return True
    End Function

    Private Function CheckSubmissionType() As Boolean
        Dim dsBatchDetails As DataSet = CType(Session.Item(SessionVars.SV_OldBatchDetails), DataSet)

        If dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_TABLE).Rows.Count = 0 Then
            Return False
        Else
            If CInt(dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_TABLE).Rows(0)("BatchType")) <> CInt(Session.Item(SessionVars.SV_SubmissionType)) Then
                Return False
            End If
        End If

        Return True

    End Function
#End Region

#Region "Event Handlers"

    Private Sub btnCancel_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnCancel.Click
        Session.Item(SessionVars.SV_OldBatchDetails) = Nothing
        Session.Item(SessionVars.SV_CopySampleBlocksSummaryTable) = Nothing
        Session.Item(SessionVars.SV_CopySampleBlocksSummaryView) = Nothing
        Response.Redirect("BatchBlocks.aspx")
    End Sub

    Private Sub btnGo_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnGo.Click
        Dim bRedirect As Boolean = False
        ctlDiv.InnerHtml = ""
        If ValidateSubmissionNumber() Then
            If Not GetSubmissionData() Then
                ctlDiv.InnerHtml = "<p><font color=""Red"">The selected submission could not be found or has not had blocks assigned.</font></p>"
                btnSummary.Enabled = False

                ddlCopySampleFrom.DataSource = New DataTable
                ddlCopySampleFrom.DataBind()

                ddlCopySampleTo.DataSource = New DataTable
                ddlCopySampleTo.DataBind()
            ElseIf Not CheckSubmissionType() Then
                ctlDiv.InnerHtml = "<p><font color=""Red"">The selected submission is not the same type as the current submission. Both must be TSE or Non-TSE.</font></p>"
                btnSummary.Enabled = False

                ddlCopySampleFrom.DataSource = New DataTable
                ddlCopySampleFrom.DataBind()

                ddlCopySampleTo.DataSource = New DataTable
                ddlCopySampleTo.DataBind()
            Else
                InitialiseDropDownLists()
                btnSummary.Enabled = True
            End If
        End If
    End Sub

    Private Sub btnSummary_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnSummary.Click
        Response.Redirect("CopySamplesSummary.aspx")
    End Sub


    Private Sub VLAHeader1_HomeClick(ByVal sender As Object, ByVal e As HistopathologySystem.HomeLinkEventArgs) Handles VLAHeader1.HomeClick
        Dim sMessage As System.Text.StringBuilder = New System.Text.StringBuilder

        sMessage.Append("You are currently assigning tissues to blocks. Any changes you have made since you last saved will be lost. Are you sure you wish to exit without saving?")
        Page.RegisterStartupScript("navigate", PromptBeforeNavigateScript(sMessage.ToString(), "Home.aspx"))
        e.bNavigateHome = False
    End Sub

    Private Sub btnCopyBatch_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnCopyBatch.Click

        ctlDiv.InnerHtml = ""
        If ValidateDropDownLists() Then
            Dim dsBatchDetails As DataSet = CType(Session.Item(SessionVars.SV_BatchDetails), DataSet)
            Dim dtAnimal As DataTable = CType(Session.Item(SessionVars.SV_AnimalTable), DataTable)
            Dim dtBlockAnimals As DataTable = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_ANIMAL)
            Dim objAnimal As New HistopathologyLib.clsAnimal
            Dim sFilter As String
            Dim selectedAnimal As DataRow()
            Dim foundAnimal As DataRow()

            Session.Item(SessionVars.SV_AnimalIDs) = CInt(ddlCopySampleFrom.SelectedValue)
            Session.Item(SessionVars.SV_AnimalID) = CInt(ddlCopySampleTo.SelectedValue)

            '----- Pre Booked Block Functionality -----
            If Not objAnimal.GetPreBookedBlocks(CInt(ddlCopySampleTo.SelectedValue), dsBatchDetails) Then
                Throw New Exception("Animal.GetPreBookedBlocks returned false.")
            End If

            sFilter = "ID=" & CInt(ddlCopySampleTo.SelectedValue)

            selectedAnimal = dtAnimal.Select(sFilter)
            foundAnimal = dtBlockAnimals.Select(sFilter)

            If Not foundAnimal Is Nothing And foundAnimal.Length = 0 Then
                If Not objAnimal.NewExistingRecord(dtBlockAnimals, _
                                                   selectedAnimal(0)("SenderRef").ToString(), _
                                                   selectedAnimal(0)("HistologyRef").ToString(), _
                                                   selectedAnimal(0)("NextBlockRef").ToString(), _
                                                   selectedAnimal(0)("RowStamp"), _
                                                   selectedAnimal(0)("ID"), _
                                                   selectedAnimal(0)("HistoRefSet"), _
                                                   selectedAnimal(0)("OnHold"), _
                                                   selectedAnimal(0)("PMDate").ToString(), _
                                                   selectedAnimal(0)("PMDateSet")) Then
                    Throw New Exception("Animal.NewExistingRecord returned false.")
                End If
            End If

            Response.Redirect("CopySamplesBlocks.aspx")
        End If
    End Sub

#End Region



End Class
