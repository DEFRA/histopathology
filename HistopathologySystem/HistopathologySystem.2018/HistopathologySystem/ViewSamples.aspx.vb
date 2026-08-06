Partial Class ViewSamples
    Inherits System.Web.UI.Page
    Protected WithEvents ResultsPager As DataGridPager
    Protected WithEvents VLAHeader1 As VLAHeader
    Protected WithEvents TissuesGridPager As DataGridPager

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
        VLAHeader1.PageTitle = "View Samples"
        ResultsPager.SetGrid(grdResults)
        TissuesGridPager.SetGrid(grdTissuesGrid)
        VLAHeader1.SubmissioNoVisible() = False
        SetEnterPresses()

        If Not IsPostBack Then
            ctlAnimalBlockTissuesDiv.Visible = False
            ctlAnimalTissuesDiv.Visible = False
            lblError.Visible = False

            rbWetTissue.Checked = True

            Dim sPGNumber As String = Request.QueryString.Get("PGNumber")
            If sPGNumber <> Nothing Then
                txtSenderRef.Text = sPGNumber
                btnSearch_Click(Me, Nothing)
            End If

            LoadLookupLists()
        End If


    End Sub

    Private Sub LoadLookupLists()
        Try
            Dim objDatatable As DataTable = Nothing
            Dim objLookup As New HistopathologyLib.LookupData

            objDatatable = objLookup.GetLookupData(LOOKUP_TISSUE_CODE)

            If Not (objDatatable Is Nothing) Then
                ddlTissue.DataSource = objDatatable
                ddlTissue.DataValueField = "Code"
                ddlTissue.DataTextField = "Description"
                ddlTissue.DataBind()
                Common.AddItemToDropDownList(ddlTissue)
            End If

            objDatatable = objLookup.GetLookupData(LOOKUP_PROJECTS)
            If Not (objDatatable Is Nothing) Then
                ddlProject.DataSource = objDatatable
                ddlProject.DataValueField = "Description"
                ddlProject.DataTextField = "Description"
                ddlProject.DataBind()
                Common.AddItemToDropDownList(ddlProject)
            End If

        Catch ex As Exception
            clsAppError.DisplayError("Error initialising the View Samples page.", ex)
        End Try
    End Sub

    Sub SetEnterPresses()
        SetFocus(txtSenderRef)
        SetTextboxDefaultButton(txtSenderRef, btnSearch)
        SetTextboxDefaultButton(txtHistRef, btnSearch)
        SetDropDownControlOnEnter(ddlTissue, ddlProject.ClientID)
        SetDropDownControlOnEnter(ddlProject, rbWetTissue.ClientID)
    End Sub

    Private Sub btnSearch_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnSearch.Click
        If txtSenderRef.Text = "" And txtHistRef.Text = "" Then
            ctlDiv.InnerHtml = "<p><font color=""Red"">Not all fields have been completed correctly, hover the mouse pointer over the red stars for details.</font></p>"
            lblError.Visible = True
            lblOtherFieldValue.Text = ""
        ElseIf Not txtSenderRef.Text = "" And Not txtHistRef.Text = "" Then
            ctlDiv.InnerHtml = "<p><font color=""Red"">Not all fields have been completed correctly, hover the mouse pointer over the red stars for details.</font></p>"
            lblError.Visible = True
            lblOtherFieldValue.Text = ""
        Else
            ctlDiv.InnerHtml = ""
            lblError.Visible = False
            FillviewGrid()
        End If

    End Sub

    Private Sub rbWetTissue_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles rbWetTissue.CheckedChanged
        rbBlockInformation.Checked = False
    End Sub

    Private Sub rbBlockInformation_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles rbBlockInformation.CheckedChanged
        rbWetTissue.Checked = False
    End Sub

    Private Sub FillviewGrid()
        Try
            Dim dtAnimalTissues As New DataTable
            Dim dvAnimalTissuesView As DataView
            Dim objAnimal As New HistopathologyLib.clsAnimal

            Dim sSenderRef As String = txtSenderRef.Text
            Dim sHistologyRef As String = txtHistRef.Text

            If rbWetTissue.Checked = True Then
                If Not objAnimal.GetAnimalTissues(dtAnimalTissues, sSenderRef, sHistologyRef, ddlTissue.SelectedValue, ddlProject.SelectedValue) Then
                    Throw New Exception("Animal.GetAnimalTissues returned false.")
                End If
            Else
                If Not objAnimal.GetAnimalBlockTissues(dtAnimalTissues, sSenderRef, sHistologyRef, ddlTissue.SelectedValue, ddlProject.SelectedValue) Then
                    Throw New Exception("Animal.GetAnimalBlockTissues returned false.")
                End If
            End If

            If dtAnimalTissues.Rows.Count = 0 Then
                ctlDiv.InnerHtml = "<p><font color=""Red"">No results were found for the selected criteria</font></p>"
                ctlAnimalBlockTissuesDiv.Visible = False
                ctlAnimalTissuesDiv.Visible = False
                lblOtherFieldValue.Text = ""
            Else
                If txtSenderRef.Text <> "" Then
                    lblOtherFieldValue.Text = "Histology Ref: " & sHistologyRef
                Else
                    lblOtherFieldValue.Text = "Sender Ref: " & sSenderRef
                End If

                Session(SessionVars.SV_SearchBatchDetailsTable) = dtAnimalTissues
                dvAnimalTissuesView = dtAnimalTissues.DefaultView
                Session(SessionVars.SV_SearchBatchDetailsView) = dvAnimalTissuesView

                dtAnimalTissues.TableName = "ViewSamples"
                Session.Item(SessionVars.SV_ExcelExport) = dtAnimalTissues
                Session.Item(SessionVars.SV_ExcelExportView) = dvAnimalTissuesView

                If rbWetTissue.Checked Then
                    ' initialise the grid
                    grdTissuesGrid.DataSource = dtAnimalTissues
                    grdTissuesGrid.DataKeyField = "ID"
                    grdTissuesGrid.CurrentPageIndex = 0
                    grdTissuesGrid.SelectedIndex = -1
                    grdTissuesGrid.EditItemIndex = -1
                    grdTissuesGrid.DataBind()

                    ' initialise the pager
                    TissuesGridPager.DataTableSessionID = SessionVars.SV_SearchBatchDetailsTable
                    TissuesGridPager.DataViewSessionID = SessionVars.SV_SearchBatchDetailsView
                    TissuesGridPager.PageLinkCount = 10
                    TissuesGridPager.AllowAddNew = False
                    TissuesGridPager.AllowEdit = False
                    TissuesGridPager.AllowDelete = False
                    TissuesGridPager.Refresh()

                    ctlAnimalBlockTissuesDiv.Visible = False
                    ctlAnimalTissuesDiv.Visible = True
                Else
                    ' initialise the grid
                    grdResults.DataSource = dtAnimalTissues
                    grdResults.DataKeyField = "ID"
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

                    ctlAnimalBlockTissuesDiv.Visible = True
                    ctlAnimalTissuesDiv.Visible = False
                End If

            End If
        Catch ex As Exception
            clsAppError.DisplayError("Error initialising the View Samples page.", ex)
        End Try
    End Sub

End Class
