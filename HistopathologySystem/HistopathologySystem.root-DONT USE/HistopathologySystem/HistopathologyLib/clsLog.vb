Public Class clsLog

    Public Enum LogType
        ltInformation = 1
        ltError = 2
        ltWarning = 3
        ltCritical = 4
    End Enum

    Public Enum LogSource
        lsStoredProcedure = 1
        lsLookupData = 2
        lsUserObject = 3
        lsBatchObject = 4
        lsBlockObject = 5
        lsBatchSubmissionObject = 6
        lsBatchSummaryObject = 7
        lsAnimalObject = 8
        lsHistologyObject = 9
        lsCheckBoxObject = 10
        lsTissueObject = 11
        lsQCNoteObject = 12
    End Enum

    Public Shared Sub LogException(ByVal Exc As Exception, _
                                   ByVal iSource As Integer, _
                                   Optional ByVal sUser As String = "")
        Try
            Dim sMsg As String
            sMsg = "Exception of type " & Exc.GetType.ToString & " caught"
            sMsg &= vbNewLine & "Message: " & Exc.Message
            sMsg &= vbNewLine & "Exception Source: " & Exc.Source
            sMsg &= vbNewLine & "Stack trace:" & vbNewLine & Exc.StackTrace

            Dim ExInner As Exception
            Dim iCount As Integer = 1

            ExInner = Exc.InnerException
            While Not ExInner Is Nothing
                Dim sInEx As String = vbNewLine & "Inner exception " & iCount.ToString & " "
                sMsg &= vbNewLine
                sMsg &= sInEx & "message: " & ExInner.Message
                sMsg &= sInEx & "source: " & ExInner.Source
                sMsg &= sInEx & "stack trace:" & vbNewLine & ExInner.StackTrace

                iCount += 1
                ExInner = ExInner.InnerException
            End While

            LogMessage(sMsg, LogType.ltError, iSource, sUser)
        Catch
        End Try
    End Sub

    Public Shared Sub LogMessage(ByVal sMessage As String, _
                          ByVal Type As LogType, _
                          ByVal iSource As Integer, _
                          Optional ByVal sUser As String = "")
        Try
            libDataAccess.libDataAccess.InfoLog.LogToDatabase(Type, sMessage, sUser, iSource)
            'libDataAccess.libDataAccess.InfoLog.LogToEventViewer(CType(Type, libDataAccess.libDataAccess.LogType), CStr(iSource), sMessage)
        Catch ex As Exception
            libDataAccess.libDataAccess.InfoLog.LogToEventViewer(CType(Type, libDataAccess.libDataAccess.LogType), CStr(iSource), sMessage)
        End Try
    End Sub
End Class
