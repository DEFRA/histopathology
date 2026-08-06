Imports HistopathologyLib
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.Text.RegularExpressions.Regex

Partial Class ViewImportedData
    Inherits System.Web.UI.Page
    Protected WithEvents ImportedDataPager As DataGridPager
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
        VLAHeader1.PageTitle = "View Old ICC_Sub data"
        ImportedDataPager.SetGrid(ImportedDataGrid)
        CheckPermissions()
        VLAHeader1.SubmissioNoVisible() = False

        If Not IsPostBack Then
            Dim sSearch As String = ""

            sSearch = Request.QueryString.Get("SearchString")

            SetFocus(ddlTable)
            SetTextboxDefaultButton(txtFilter, btnGo)
            hlExcel.Visible = False
            If Not sSearch = "" Then
                txtFilter.Text = sSearch
                ImportedDataPager.Visible = False
                'InitialiseGrid(True)
                'btnGo_Click(Me, Nothing)
            Else
                InitialiseGrid(False)
            End If
            LoadLookupLists()
        End If

    End Sub

#Region "Lookup Lists"

    Private Sub LoadLookupLists()
        Try
            Dim lookup As New LookupData
            Dim dtImportedTables As DataTable

            dtImportedTables = lookup.GetImportedtables()

            If Not dtImportedTables Is Nothing Then
                ddlTable.DataSource = dtImportedTables
                ddlTable.DataTextField = "Name"
                ddlTable.DataValueField = "ID"
                ddlTable.DataBind()
            End If

            AddItemToDropDownList(ddlTable)

        Catch ex As Exception
            clsAppError.DisplayError("Failed to retrieve ViewImportedData dropdownlists", ex)
        End Try
    End Sub

#End Region

#Region "Grid"

    Private Sub InitialiseGrid(ByVal bAllRecords As Boolean)
        Try
            Dim objAnimal As New clsAnimal
            Dim dtImportedData As DataTable
            Dim dvImportedDataView As DataView
            Dim sSelectedValue As String = ""

            If bAllRecords Then
                sSelectedValue = "All"
            Else
                sSelectedValue = ddlTable.SelectedValue
            End If

            If Not objAnimal.GetImportedData(dtImportedData, sSelectedValue) Then
                Throw New Exception("Animal.GetImportedData returned false.")
            End If

            If Not dtImportedData Is Nothing Then
                dvImportedDataView = dtImportedData.DefaultView
                dvImportedDataView.RowFilter = CreateFilterString(txtFilter.Text)
                Session.Item(SessionVars.SV_ImportedDataTable) = dtImportedData
                Session.Item(SessionVars.SV_ImportedDataView) = dvImportedDataView

                ImportedDataGrid.CurrentPageIndex = 0
                ImportedDataGrid.DataSource = dtImportedData
                ImportedDataGrid.DataBind()


                ImportedDataPager.DataTableSessionID = SessionVars.SV_ImportedDataTable
                ImportedDataPager.DataViewSessionID = SessionVars.SV_ImportedDataView
                ImportedDataPager.PageLinkCount = 50
                ImportedDataPager.AllowAddNew = False
                ImportedDataPager.AllowEdit = False
                ImportedDataPager.AllowDelete = False
                ImportedDataPager.Rebind()
                ImportedDataPager.Refresh()
                ImportedDataPager.Visible = True
                ImportedDataGrid.Visible = True


                hlExcel.Visible = True
                Session.Item(SessionVars.SV_ExcelExport) = dtImportedData
                Session.Item(SessionVars.SV_ExcelExportView) = dtImportedData.DefaultView
            Else
                ImportedDataPager.Visible = False
                ImportedDataGrid.Visible = False
            End If



        Catch ex As Exception
            clsAppError.DisplayError("Failed to bind table data to grid on ViewImportedData page.", ex)
        End Try
    End Sub

#End Region

#Region "Event Handlers"

    Private Sub btnGo_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnGo.Click
        Try
            Dim dtImportedData As DataTable = CType(Session.Item(SessionVars.SV_ImportedDataTable), DataTable)
            Dim dtImportedDataView As DataView = CType(Session.Item(SessionVars.SV_ImportedDataView), DataView)
            Dim objAnimal As New HistopathologyLib.clsAnimal

            Dim sFilter As String = txtFilter.Text

            If Not dtImportedDataView Is Nothing And ddlTable.SelectedIndex <> 0 Then

                dtImportedDataView.RowFilter = CreateFilterString(sFilter)

                ImportedDataGrid.DataBind()
                ImportedDataPager.Rebind()
                ImportedDataPager.Refresh()

                hlExcel.Visible = True

                Session.Item(SessionVars.SV_ExcelExport) = dtImportedData
                dtImportedData.TableName = "OldICC_Subdata"
                Session.Item(SessionVars.SV_ExcelExportView) = dtImportedData.DefaultView
            Else
                InitialiseGrid(True)
            End If

        Catch ex As Exception
            clsAppError.DisplayError("Failed to filter grid results on ViewImportedData page.", ex)
        End Try

    End Sub

    Private Sub btnClear_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnClear.Click
        Try
            Dim dtImportedDataView As DataView = CType(Session.Item(SessionVars.SV_ImportedDataView), DataView)

            txtFilter.Text = ""

            If Not dtImportedDataView Is Nothing Then
                dtImportedDataView.RowFilter = ""

                ImportedDataGrid.DataBind()
                ImportedDataPager.Rebind()
                ImportedDataPager.Refresh()
            End If

        Catch ex As Exception
            clsAppError.DisplayError("Failed to clear filtered results on ViewImportedData page.", ex)
        End Try
    End Sub

    Private Sub ddlTable_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ddlTable.SelectedIndexChanged
        InitialiseGrid(False)
    End Sub

#End Region

#Region "Private functions"
    Private Function CreateFilterString(ByVal description As String) As String
        Dim filterTerms As ArrayList
        Dim filter As New StringBuilder
        Dim count As Integer

        ' Create the string that will be used to filter the records depending on the 
        ' text entered into the description field.

        filterTerms = SplitQuoted(description)

        If filterTerms.Count > 0 Then
            For count = 0 To filterTerms.Count - 1
                If count = 0 Then
                    filter.Append("(Project LIKE '%" & filterTerms(count) & "%'")
                    filter.Append(" OR DateSubmitted LIKE '%" & filterTerms(count) & "%'")
                    filter.Append(" OR Species LIKE '%" & filterTerms(count) & "%'")
                    filter.Append(" OR Tissue LIKE '%" & filterTerms(count) & "%'")
                    filter.Append(" OR SenderRef LIKE '%" & filterTerms(count) & "%'")
                    filter.Append(" OR HistologyRef LIKE '%" & filterTerms(count) & "%'")
                    filter.Append(" OR BlockRef LIKE '%" & filterTerms(count) & "%'")
                    filter.Append(" OR Comments LIKE '%" & filterTerms(count) & "%')")
                Else
                    filter.Append("AND (Project LIKE '%" & filterTerms(count) & "%'")
                    filter.Append(" OR DateSubmitted LIKE '%" & filterTerms(count) & "%'")
                    filter.Append(" OR Species LIKE '%" & filterTerms(count) & "%'")
                    filter.Append(" OR Tissue LIKE '%" & filterTerms(count) & "%'")
                    filter.Append(" OR SenderRef LIKE '%" & filterTerms(count) & "%'")
                    filter.Append(" OR HistologyRef LIKE '%" & filterTerms(count) & "%'")
                    filter.Append(" OR BlockRef LIKE '%" & filterTerms(count) & "%'")
                    filter.Append(" OR Comments LIKE '%" & filterTerms(count) & "%')")
                End If
            Next

            Return filter.ToString
        Else
            Return ""
        End If

    End Function

    Private Function SplitQuoted(ByVal description As String) As ArrayList
        Dim filterTerms As New ArrayList
        Dim patternMatch As Match
        Dim matchOne As String
        Dim matchTwo As String
        Dim pattern As String
        Dim delimiters As String

        description = description.Replace("[", "[[]")
        description = description.Replace("'", "''")
        description = description.Replace("*", "[*]")
        description = description.Replace("%", "[%]")

        ' Split out the search phases that will be used to filter.
        delimiters = " \t"
        pattern = """([^""\\]*[\\.[^""\\]*]*)""" + "|" + "([^" + delimiters + "]+)"

        For Each patternMatch In Matches(description, pattern)
            matchOne = Replace(patternMatch.Value, Chr(34), "")
            filterTerms.Add(matchOne)
        Next

        Return filterTerms
    End Function

    Private Sub CheckPermissions()
        VLAHeader1.getUserDetails()
        Dim sGroupName As String = Session(SessionVars.SV_HeaderGroupName)
        If sGroupName = "Customer" Then
            'Response.Redirect("Home.aspx")
        ElseIf sGroupName = "Histopathology User" Then
            'Nothing
        ElseIf sGroupName = "Maintenance" Then
            'Nothing
        Else
            Response.Redirect("Home.aspx")
        End If
    End Sub

#End Region




End Class
