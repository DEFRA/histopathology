Partial Class CopyBatchBlocksSummary
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
        VLAHeader1.PageTitle = "Submission Summary"

        If Not IsPostBack Then
            InitialiseSummaryGrid()
        End If
    End Sub

#Region "Event Handlers"

    Private Sub grdBatchSummary_ItemCommand(ByVal source As System.Object, ByVal e As System.Web.UI.WebControls.DataGridCommandEventArgs) Handles grdBatchSummary.ItemCommand
        Dim iCount As Int32 = e.Item.ItemIndex + 1

        If e.CommandName = "ExpandTissues" Then
            If CType(e.Item.Cells(3).Controls(0), LinkButton).Text = "<img src=""Images/minus.gif"">" Then
                Do While Not grdBatchSummary.Items(iCount).Cells(4).Text = "&nbsp;"
                    grdBatchSummary.Items(iCount).Visible = False
                    iCount += 1
                    If iCount >= grdBatchSummary.Items.Count Then Exit Do
                Loop
                CType(e.Item.Cells(3).Controls(0), LinkButton).Text = "<img src=""Images/plus.gif"">"
            Else
                'While the sender ref isnt empty show the tissue rows
                Do While Not grdBatchSummary.Items(iCount).Cells(4).Text = "&nbsp;"
                    HideControls(iCount)
                    iCount += 1
                    If iCount >= grdBatchSummary.Items.Count Then Exit Do
                Loop
                CType(e.Item.Cells(3).Controls(0), LinkButton).Text = "<img src=""Images/minus.gif"">"
            End If
        End If
    End Sub

    Private Sub btnCancel_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnCancel.Click
        Response.Redirect("CopyBatchBlocks.aspx")
    End Sub

    Private Sub grdBatchSummary_ItemDataBound(ByVal sender As Object, ByVal e As System.Web.UI.WebControls.DataGridItemEventArgs) Handles grdBatchSummary.ItemDataBound
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
                Dim cbRepeatBlock As CheckBox = Nothing
                Dim cbArchive As CheckBox = Nothing

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
                    cbRepeatBlock = CType(e.Item.FindControl("cbRepeatBlockDisplay"), CheckBox)
                    cbArchive = CType(e.Item.FindControl("cbArchiveDisplay"), CheckBox)
                End If

                If Not cbArchive Is Nothing Then
                    If Not IsDBNull(drv("Archive")) Then
                        cbArchive.Checked = drv("Archive")
                    Else
                        cbArchive.Checked = False
                    End If
                End If

                If Not lblFixationCode Is Nothing Then
                    If Not IsDBNull(drv("Fixation")) Then
                        lblFixationCode.Text = GetListType(drv("Fixation"), LOOKUP_FIXATIVE)
                    Else
                        lblFixationCode.Text = ""
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

                If Not cbRepeatBlock Is Nothing Then
                    If Not IsDBNull(drv("RepeatBlock")) Then
                        cbRepeatBlock.Checked = drv("RepeatBlock")
                    Else
                        cbRepeatBlock.Checked = False
                    End If
                End If
            End If

        Catch ex As Exception
            clsAppError.DisplayError("Failed to bind template columns in the Batch summary grid", ex)
        End Try
    End Sub

    Private Sub VLAHeader1_HomeClick(ByVal sender As Object, ByVal e As HistopathologySystem.HomeLinkEventArgs) Handles VLAHeader1.HomeClick
        Dim sMessage As System.Text.StringBuilder = New System.Text.StringBuilder()

        sMessage.Append("Any changes that have been made will be discarded, are you sure you wish to exit without saving?")
        Page.RegisterStartupScript("navigate", PromptBeforeNavigateScript(sMessage.ToString(), "Home.aspx"))
        e.bNavigateHome = False
    End Sub

    Private Sub lbCollapseAll_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles lbCollapseAll.Click
        SetHierarchical(False)
    End Sub

    Private Sub lbExpandAll_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles lbExpandAll.Click
        SetHierarchical(True)
    End Sub


#End Region

#Region "Grid Related"

    Private Sub InitialiseSummaryGrid()
        Try
            Try
                Dim dsDataSet As DataSet = CType(Session.Item(SessionVars.SV_OldBatchDetails), DataSet)
                Dim objSummary As New HistopathologyLib.clsBatchSummary()
                Dim dtSummary As New DataTable()
                Dim dtTissuesList As DataTable = Common.GetLookupTypeList(LOOKUP_TISSUE_CODE)

                If Not objSummary.CreateBlockSummaryData(dsDataSet, dtSummary, dtTissuesList) Then
                    Throw New Exception("BatchSummary.CreateBlockSummaryData return false")
                End If

                ' create a dataview for filtering and sorting
                Dim dv As DataView = dtSummary.DefaultView

                Session.Item(SessionVars.SV_BlockSummaryTable) = dtSummary
                Session.Item(SessionVars.SV_BlockSummaryView) = dv

                grdBatchSummary.DataSource = dtSummary
                grdBatchSummary.DataKeyField = "ID"
                grdBatchSummary.DataBind()
                grdBatchSummary.Enabled = True

                HideColumns(dsDataSet)

                SetHierarchical(True)

            Catch ex As Exception
                clsAppError.DisplayError("Error initialising the Summary Grid, BatchBlocks page.", ex)
            End Try

        Catch ex As Exception
            clsAppError.DisplayError("Error initialising the Summary Grid", ex)
        End Try
    End Sub

    Private Sub SetHierarchical(ByVal bExpanded As Boolean)
        Dim iCount As Int32

        If Not bExpanded Then
            For iCount = 0 To grdBatchSummary.Items.Count - 1
                If grdBatchSummary.Items(iCount).Cells(1).Text = "&nbsp;" Then
                    grdBatchSummary.Items(iCount).Visible = False
                End If

                grdBatchSummary.Items(iCount).Cells(3).Controls(0).Visible = False

                If iCount + 1 <= grdBatchSummary.Items.Count - 1 Then
                    If Not grdBatchSummary.Items(iCount + 1).Cells(4).Text = "&nbsp;" Then
                        grdBatchSummary.Items(iCount).Cells(3).Controls(0).Visible = True
                        CType(grdBatchSummary.Items(iCount).Cells(3).Controls(0), LinkButton).Text = "<img src=""Images/plus.gif"">"
                        'Else
                        'CType(grdBlockSummary.Items(iCount).Cells(3).Controls(0), LinkButton).Text = "<img src=""Images/plus.gif"">"
                    End If
                End If
            Next

        Else
            For iCount = 0 To grdBatchSummary.Items.Count - 1
                'If the sender ref is blank in the datatable it is a expandable tissue row
                If grdBatchSummary.Items(iCount).Cells(1).Text = "&nbsp;" Then
                    grdBatchSummary.Items(iCount).Visible = True
                    grdBatchSummary.Items(iCount).Cells(3).Controls(0).Visible = False
                    HideControls(iCount)
                End If

                If iCount + 1 <= grdBatchSummary.Items.Count - 1 Then
                    'if the next row tissue is blank its a sender ref row
                    If Not grdBatchSummary.Items(iCount + 1).Cells(4).Text = "&nbsp;" Then
                        CType(grdBatchSummary.Items(iCount).Cells(3).Controls(0), LinkButton).Text = "<img src=""Images/minus.gif"">"
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
        Dim strRepeatBlock As String
        Dim strArchive As String
        Dim cbEO As CheckBox
        Dim cbHE As CheckBox
        Dim cbHEBSE As CheckBox
        Dim cbSpecialStain As CheckBox
        Dim cbIHCPrp As CheckBox
        Dim cbIHCOther As CheckBox
        Dim cbRepeatBlock As CheckBox
        Dim cbArchive As CheckBox
        Dim litEO As LiteralControl
        Dim litHE As LiteralControl
        Dim liHEBse As LiteralControl
        Dim liSpecialStain As LiteralControl
        Dim liIHCPrp As LiteralControl
        Dim liIHCOther As LiteralControl
        Dim liRepeatBlock As LiteralControl

        grdBatchSummary.Items(iCount).Visible = True
        grdBatchSummary.Items(iCount).Cells(3).Controls(0).Visible = False
        'Hide the row selection button
        grdBatchSummary.Items(iCount).Cells(0).Controls(0).Visible = False
        'Hide the check boxes 
        litEO = CType(grdBatchSummary.Items(iCount).Cells(5).Controls(0), LiteralControl)

        strGridPart = GetGridPart(litEO.UniqueID())
        strEO = strGridPart + "cbEODisplay"
        strHE = strGridPart + "cbHAndEDisplay"
        strHEBse = strGridPart + "cbHAndEBseDisplay"
        strSpecialStain = strGridPart + "cbSpecialStainDisplay"
        strIHCPrp = strGridPart + "cbIHCPrpDisplay"
        strIHCOther = strGridPart + "cbIHCOtherDisplay"
        strRepeatBlock = strGridPart + "cbRepeatBlockDisplay"
        strArchive = strGridPart + "cbArchiveDisplay"

        cbEO = Page.FindControl(strEO)
        cbHE = Page.FindControl(strHE)
        cbHEBSE = Page.FindControl(strHEBse)
        cbSpecialStain = Page.FindControl(strSpecialStain)
        cbIHCPrp = Page.FindControl(strIHCPrp)
        cbIHCOther = Page.FindControl(strIHCOther)
        cbRepeatBlock = Page.FindControl(strRepeatBlock)
        cbArchive = Page.FindControl(strArchive)

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

        If Not cbRepeatBlock Is Nothing Then
            cbRepeatBlock.Visible = False
        End If
    End Sub

    Private Sub HideColumns(ByVal dsSubmission As DataSet)
        'Only display the Histology columns which are relevent to the type of submission
        If Session(SessionVars.SV_SubmissionType) = SUBMISSION_NONTSE Then
            grdBatchSummary.Columns(8).Visible = False
            grdBatchSummary.Columns(10).Visible = False
            grdBatchSummary.Columns(11).Visible = True
        Else
            grdBatchSummary.Columns(8).Visible = True
            grdBatchSummary.Columns(10).Visible = True
            grdBatchSummary.Columns(11).Visible = False
        End If
    End Sub

#End Region


End Class
