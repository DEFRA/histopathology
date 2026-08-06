Imports System
Imports System.Data.SqlClient
Imports System.Collections
Imports System.Data

Partial Class FixCompletedDates
    Inherits System.Web.UI.Page

    Private connection As SqlConnection

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

        connection = New SqlConnection(System.Configuration.ConfigurationSettings.AppSettings("DBConnectionString"))
        If Not IsPostBack Then

        End If
    End Sub

    Private Sub btnUpdate_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnUpdate.Click

        Dim batchID As Integer
        Dim batchIDs As DataTable
        Dim batchHistology As DataTable
        Dim batchStain As DataTable
        Dim batchAntibodies As DataTable
        Dim row As DataRow
        Dim allTestsDispatched As Boolean
        Dim transaction As SqlTransaction = Nothing
        Dim latestDate As DateTime

        Try
            connection.Open()
            transaction = connection.BeginTransaction

            batchIDs = GetBatchIDs(transaction)

            For Each row In batchIDs.Rows
                allTestsDispatched = True
                latestDate = New DateTime

                CheckDispatchedDate(GetBatchHistology(row("ID"), transaction), latestDate, allTestsDispatched)

                If allTestsDispatched Then
                    CheckDispatchedDate(GetBatchStain(row("ID"), transaction), latestDate, allTestsDispatched)
                End If

                If allTestsDispatched Then
                    CheckDispatchedDate(GetBatchAntibodies(row("ID"), transaction), latestDate, allTestsDispatched)
                End If

                If allTestsDispatched Then
                    UpdateBatchCompletedDate(row("ID"), latestDate, transaction)
                End If
            Next

            transaction.Commit()

        Catch ex As Exception
            If Not transaction Is Nothing Then
                transaction.Rollback()
            End If
        Finally
            If Not connection Is Nothing Then
                connection.Close()
            End If
        End Try
    End Sub

    Private Sub UpdateBatchCompletedDate(ByVal batchID As Integer, ByVal completedDate As DateTime, ByRef transaction As SqlTransaction)
        Dim command As New SqlCommand
        Dim parameterDate As New SqlParameter("@CompletedDate", completedDate)
        Dim parameterBatchId As New SqlParameter("@BatchID", batchID)

        command.CommandType = CommandType.StoredProcedure
        command.CommandText = "EditBatchCompletedDate"
        command.Connection = connection
        command.Transaction = transaction

        command.Parameters.Add(parameterDate)
        command.Parameters.Add(parameterBatchId)

        command.ExecuteNonQuery()

    End Sub

    Private Sub CheckDispatchedDate(ByVal data As DataTable, ByRef latestDate As DateTime, ByRef allTestsDispatched As Boolean)
        Dim row As DataRow

        For Each row In data.Rows
            allTestsDispatched = row("Dispatched")

            If allTestsDispatched = False Then
                Exit For
            Else
                If row("DispatchedDate") > latestDate Then
                    latestDate = row("DispatchedDate")
                End If
            End If
        Next

    End Sub

    Private Function GetBatchIDs(ByRef transaction As SqlTransaction) As DataTable
        Dim command As New SqlCommand
        Dim adapter As New SqlDataAdapter
        Dim batchIDs As New DataTable

        command.CommandType = CommandType.StoredProcedure
        command.CommandText = "GetBatchesLinkedToBlocks"
        command.Connection = connection
        command.Transaction = transaction
        adapter.SelectCommand = command

        adapter.Fill(batchIDs)

        Return batchIDs
    End Function

    Private Function GetBatchHistology(ByVal batchID As Integer, ByRef transaction As SqlTransaction) As DataTable
        Dim command As New SqlCommand
        Dim adapter As New SqlDataAdapter
        Dim batchHistology As New DataTable
        Dim parameter As New SqlParameter("@BatchID", batchID)

        command.CommandType = CommandType.StoredProcedure
        command.CommandText = "GetHistologyDispatched"
        command.Connection = connection
        command.Transaction = transaction
        command.Parameters.Add(parameter)

        adapter.SelectCommand = command

        adapter.Fill(batchHistology)

        Return batchHistology
    End Function

    Private Function GetBatchStain(ByVal batchID As Integer, ByRef transaction As SqlTransaction) As DataTable
        Dim command As New SqlCommand
        Dim adapter As New SqlDataAdapter
        Dim batchStain As New DataTable
        Dim parameter As New SqlParameter("@BatchID", batchID)

        command.CommandType = CommandType.StoredProcedure
        command.CommandText = "GetStainDispatched"
        command.Connection = connection
        command.Transaction = transaction
        command.Parameters.Add(parameter)

        adapter.SelectCommand = command

        adapter.Fill(batchStain)

        Return batchStain
    End Function

    Private Function GetBatchAntibodies(ByVal batchID As Integer, ByRef transaction As SqlTransaction) As DataTable
        Dim command As New SqlCommand
        Dim adapter As New SqlDataAdapter
        Dim batchAntibodies As New DataTable
        Dim parameter As New SqlParameter("@BatchID", batchID)

        command.CommandType = CommandType.StoredProcedure
        command.CommandText = "GetAntibodiesDispatched"
        command.Connection = connection
        command.Transaction = transaction
        command.Parameters.Add(parameter)

        adapter.SelectCommand = command

        adapter.Fill(batchAntibodies)

        Return batchAntibodies
    End Function


End Class
