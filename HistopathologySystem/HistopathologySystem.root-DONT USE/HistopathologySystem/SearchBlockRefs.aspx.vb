

Partial Class SearchBlockRefs
    Inherits System.Web.UI.Page
    Protected WithEvents ResultsPager As DataGridPager
    Protected WithEvents DIV1 As System.Web.UI.HtmlControls.HtmlGenericControl
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
        VLAHeader1.PageTitle = "Search Block Refs"
        CheckPermissions()
        ResultsPager.SetGrid(grdResults)
        SetEnterPresses()

        If Not IsPostBack Then
            Dim sHistologyRef As String
            Dim sSenderRef As String

            sHistologyRef = Request.QueryString.Get("HistologyRef")
            sSenderRef = Request.QueryString.Get("SenderRef")

            If Not sHistologyRef = "" Then
                txtHistRef.Text = sHistologyRef
                btnSearch_Click(Me, Nothing)
            Else
                If Not sSenderRef = "" Then
                    txtSenderRef.Text = sSenderRef
                    btnSearch_Click(Me, Nothing)
                Else
                    hlExcelExport.Visible = False
                    grdResults.Visible = False
                    ResultsPager.Visible = False
                    lblError.Visible = False
                End If
            End If

            If CStr(Session.Item(SessionVars.SV_SearchBlockRefsRedirectPage)) <> "SubmissionDetailsBlock.aspx" Then
                VLAHeader1.SubmissioNoVisible() = False
            End If
        End If
    End Sub

#Region "Event Handlers"

    Private Sub lbViewImportedData_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles lbViewImportedData.Click
        Dim sSearch As String = ""

        If txtSenderRef.Text <> "" Then
            sSearch = txtSenderRef.Text.Trim

            If txtHistRef.Text <> "" Then
                sSearch = sSearch & " " & txtHistRef.Text.Trim
            End If
        Else
            sSearch = txtHistRef.Text.Trim
        End If

        OpenDownloadPopup("ViewImportedData.aspx?SearchString=" & sSearch, Me.Page)
    End Sub

    Private Sub btnSearch_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnSearch.Click
        If txtSenderRef.Text = "" And txtHistRef.Text = "" Then
            'lblError.ToolTip = "Must enter either the Sender Ref or HistologyRef"
            ctlDiv.InnerHtml = "<p><font color=""Red"">Not all fields have been completed correctly, hover the mouse pointer over the red stars for details.</font></p>"
            lblError.Visible = True
        ElseIf Not txtSenderRef.Text = "" And Not txtHistRef.Text = "" Then
            ctlDiv.InnerHtml = "<p><font color=""Red"">Not all fields have been completed correctly, hover the mouse pointer over the red stars for details.</font></p>"
            lblError.Visible = True
        Else
            ctlDiv.InnerHtml = ""
            lblError.Visible = False
            grdResults.Visible = True
            ResultsPager.Visible = True
            hlExcelExport.Visible = True
            FillviewGrid()
        End If

    End Sub

    Private Sub btnDone_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnDone.Click
        Dim sPrevPage As String
        sPrevPage = CStr(Session.Item(SessionVars.SV_SearchBlockRefsRedirectPage))

        Try
            If sPrevPage = "SubmissionDetailsBlock.aspx" Then
                'Bread crumbs
                Dim objCrumbArrayList As ArrayList = CType(Session.Item(SessionVars.SV_BreadCrumbs), ArrayList)
                If Not objCrumbArrayList Is Nothing Then
                    objCrumbArrayList(1) = "Submission Samples"
                    objCrumbArrayList(2) = "Blocking"
                    objCrumbArrayList(3) = "Sample Blocks"
                    Session.Item(SessionVars.SV_BreadCrumbs) = objCrumbArrayList
                End If
            End If
        Catch ex As Exception
            clsAppError.DisplayError("Bread Crumb Error, SearchBlockRefs.aspx.", ex)
        End Try

        If sPrevPage = "" Then
            RegisterStartupScript("PageClose", "<script language=""javascript"">self.close();</script>")
        Else
            Response.Redirect(sPrevPage)
        End If

    End Sub

    Private Sub VLAHeader1_HomeClick(ByVal sender As Object, ByVal e As HistopathologySystem.HomeLinkEventArgs) Handles VLAHeader1.HomeClick

        If CType(Session.Item(SessionVars.SV_AssignBlocks), Boolean) Then
            Dim sMessage As System.Text.StringBuilder = New System.Text.StringBuilder
            sMessage.Append("You are currently assigning tissues to blocks. Any block assignment that you have completed since you last saved will be lost. Are you sure you wish to exit?")
            Page.RegisterStartupScript("navigate", PromptBeforeNavigateScript(sMessage.ToString(), "Home.aspx"))
            e.bNavigateHome = False
        End If

    End Sub

#End Region

#Region "Grid Related"

    Private Sub FillviewGrid()
        Try
            Dim dtBlockRefs As New DataTable
            Dim dvBlockRefsView As DataView
            Dim objAnimal As New HistopathologyLib.clsAnimal
            Dim sSearch As String
            Dim dtDisplayGrid As DataTable

            If txtHistRef.Text = "" Then
                If Not objAnimal.GetAnimalBlocksBySenderRefForBlockRefSearch(dtBlockRefs, txtSenderRef.Text) Then
                    Throw New Exception("Animal.GetAnimalBlocksBySenderRefForBlockRefSearch returned false.")
                End If
            Else
                If Not objAnimal.GetAnimalBlocksForBlockRefSearch(dtBlockRefs, txtHistRef.Text) Then
                    Throw New Exception("Animal.GetAnimalBlocksForBlockRefSearch returned false.")
                End If
            End If

            dtDisplayGrid = CreateBlockRefsGrid(dtBlockRefs)

            If Not dtDisplayGrid Is Nothing Then
                Session(SessionVars.SV_SearchBatchDetailsTable) = dtDisplayGrid
                dvBlockRefsView = dtDisplayGrid.DefaultView
                Session(SessionVars.SV_SearchBatchDetailsView) = dvBlockRefsView

                dtBlockRefs.TableName = "SearchBlockRefs"
                Session.Item(SessionVars.SV_ExcelExport) = dtDisplayGrid
                Session.Item(SessionVars.SV_ExcelExportView) = dvBlockRefsView

                ' initialise the grid
                grdResults.DataSource = dtDisplayGrid
                grdResults.DataKeyField = "Used Block Refs"
                grdResults.CurrentPageIndex = 0
                grdResults.SelectedIndex = -1
                grdResults.EditItemIndex = -1
                grdResults.DataBind()

                ' initialise the pager
                ResultsPager.DataTableSessionID = SessionVars.SV_SearchBatchDetailsTable
                ResultsPager.DataViewSessionID = SessionVars.SV_SearchBatchDetailsView
                ResultsPager.PageLinkCount = 10
                ResultsPager.AllowAddNew = False
                ResultsPager.AllowEdit = False
                ResultsPager.AllowDelete = False
                ResultsPager.Refresh()
            End If

        Catch ex As Exception
            clsAppError.DisplayError("Error initialising the Search Block refs page.", ex)
        End Try
    End Sub

#End Region

#Region "Private Functions"

    Sub SetEnterPresses()
        SetFocus(txtSenderRef)
        SetTextboxDefaultButton(txtSenderRef, btnSearch)
        SetTextboxDefaultButton(txtHistRef, btnSearch)
    End Sub

    Private Sub CheckPermissions()
        VLAHeader1.getUserDetails()

        Dim sGroupName As String = Session(SessionVars.SV_HeaderGroupName)

        If sGroupName = "Customer" Then
            'Response.Redirect("Home.aspx")
        ElseIf sGroupName = "Histopathology User" Then
            'nothing
        ElseIf sGroupName = "Maintenance" Then
            'Nothing
        Else
            Response.Redirect("Home.aspx")
        End If
    End Sub

    Private Function CreateBlockRefsGrid(ByVal dtBlockRefs As DataTable) As DataTable
        Dim iCount As Integer = 0
        Dim dtDisplayGrid As New DataTable
        Dim drNewRow As DataRow
        Dim drFoundBlock As DataRow()
        Dim iFirstBlockInRange As Integer = 0
        Dim iLastBlockInRange As Integer = 0
        Dim eBlockStatus As Integer
        Dim bProcess As Boolean = False
        Dim iNumberOfBlocks As Integer = 0
        Dim iMaxValue As Integer = 0

        dtDisplayGrid.TableName = "SearchBlockRefs"
        dtDisplayGrid.Columns.Add("Used Block Refs", System.Type.GetType("System.String"))
        dtDisplayGrid.Columns.Add("Unused Block Refs", System.Type.GetType("System.String"))
        dtDisplayGrid.Columns.Add("Pre Booked Block Refs", System.Type.GetType("System.String"))

        If dtBlockRefs.Rows.Count = 0 Then
            drNewRow = dtDisplayGrid.NewRow()
            drNewRow("Unused Block Refs") = "01+"
            dtDisplayGrid.Rows.Add(drNewRow)
            Return dtDisplayGrid
        End If

        ''The data coming in is a list of used block refs for the histology data. Need to format 
        '' it in a way that the user can see the used and unused ranges.
        iNumberOfBlocks = dtBlockRefs.Rows.Count
        dtBlockRefs.Select("", "BlockRef ASC")
        iMaxValue = dtBlockRefs.Rows(iNumberOfBlocks - 1)("Blockref")

        eBlockStatus = BlockStatus.NotUsed
        For iCount = 1 To iMaxValue + 1
            drFoundBlock = dtBlockRefs.Select("BlockRef=" & iCount)

            If drFoundBlock.Length = 0 Then 'unused block
                If eBlockStatus <> BlockStatus.NotUsed Then
                    drNewRow = dtDisplayGrid.NewRow()

                    Select Case eBlockStatus
                        Case BlockStatus.NotUsed
                            drNewRow("Unused Block Refs") = FormatString(iFirstBlockInRange, iCount)
                        Case BlockStatus.PreBooked
                            drNewRow("Pre Booked Block Refs") = FormatString(iFirstBlockInRange, iCount)
                        Case BlockStatus.Used

                            drNewRow("Used Block Refs") = FormatString(iFirstBlockInRange, iCount)
                        Case BlockStatus.PreBookedButUsed
                            drNewRow("Used Block Refs") = FormatString(iFirstBlockInRange, iCount)
                    End Select

                    iFirstBlockInRange = iCount
                    eBlockStatus = BlockStatus.NotUsed
                    dtDisplayGrid.Rows.Add(drNewRow)
                End If
            Else
                If eBlockStatus <> CInt(drFoundBlock(0)("Status")) Then
                    bProcess = True
                    If eBlockStatus = BlockStatus.Used Or eBlockStatus = BlockStatus.PreBookedButUsed Then
                        If CInt(drFoundBlock(0)("Status")) = BlockStatus.Used Or CInt(drFoundBlock(0)("Status")) = BlockStatus.PreBookedButUsed Then
                            bProcess = False
                        End If
                    End If
                    If bProcess Then
                        Select Case eBlockStatus
                            Case BlockStatus.NotUsed
                                If Not (iFirstBlockInRange = 0 And iCount - 1 = 0) Then
                                    drNewRow = dtDisplayGrid.NewRow()
                                    drNewRow("Unused Block Refs") = FormatString(iFirstBlockInRange, iCount)
                                End If
                            Case BlockStatus.PreBooked

                                drNewRow = dtDisplayGrid.NewRow()
                                drNewRow("Pre Booked Block Refs") = FormatString(iFirstBlockInRange, iCount)
                            Case BlockStatus.Used

                                drNewRow = dtDisplayGrid.NewRow()
                                drNewRow("Used Block Refs") = FormatString(iFirstBlockInRange, iCount)
                            Case BlockStatus.PreBookedButUsed
                                drNewRow = dtDisplayGrid.NewRow()
                                drNewRow("Used Block Refs") = FormatString(iFirstBlockInRange, iCount)
                        End Select

                        iFirstBlockInRange = iCount
                        eBlockStatus = CInt(drFoundBlock(0)("Status"))
                        If Not drNewRow Is Nothing Then
                            dtDisplayGrid.Rows.Add(drNewRow)
                        End If

                    End If
                End If
            End If
        Next

        drNewRow = dtDisplayGrid.NewRow()
        drNewRow("Unused Block Refs") = FormatBlockRef(iCount - 1) & " +"
        dtDisplayGrid.Rows.Add(drNewRow)

        Return dtDisplayGrid

    End Function

    Private Enum BlockStatus
        NotUsed = 0
        Used = 1
        PreBooked = 2
        PreBookedButUsed = 3
    End Enum

    Private Function FormatBlockRef(ByVal iBlockRef As Integer)
        If iBlockRef < 10 Then
            Return "0" & CStr(iBlockRef)
        Else
            Return CStr(iBlockRef)
        End If
    End Function

    Private Function FormatString(ByVal iRangeFrom As Integer, ByVal iRangeTo As Integer)
        If iRangeFrom = iRangeTo - 1 Then
            Return FormatBlockRef(iRangeFrom)
        Else
            If iRangeFrom = 0 Then
                If iRangeTo - 1 = 1 Then
                    Return FormatBlockRef(iRangeTo - 1)
                Else
                    Return "'01 - " & FormatBlockRef(iRangeTo - 1) & "'"
                End If
            Else
                Return "'" & FormatBlockRef(iRangeFrom) & " - " & FormatBlockRef(iRangeTo - 1) & "'"
            End If
        End If
    End Function

#End Region


End Class
