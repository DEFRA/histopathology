
Imports System.Text.RegularExpressions

Module Common

#Region "Public Constants"
    ' Lookup constants
    'Public Const LOOKUP_SPECIES_TYPE As Integer = 1
    Public Const LOOKUP_SUBMISSION_PRIORITY As Integer = 2
    Public Const LOOKUP_TIME_RECEIVED As Integer = 3
    Public Const LOOKUP_TSE_ANTIBODIES As Integer = 4
    Public Const LOOKUP_NONTSE_ANTIBODIES As Integer = 5
    Public Const LOOKUP_SPECIAL_STAIN As Integer = 6
    Public Const LOOKUP_TSE_HISTOLOGY As Integer = 7
    Public Const LOOKUP_NONTSE_HISTOLOGY As Integer = 8
    Public Const LOOKUP_TISSUE_CODE As Integer = 9
    Public Const LOOKUP_FIXATIVE As Integer = 10
    Public Const LOOKUP_SUBMITTEDAS As Integer = 11
    Public Const LOOKUP_POSTFIXATION As Integer = 12
    Public Const LOOKUP_USER_AREA As Integer = 13
    Public Const LOOKUP_QC_CODE As Integer = 14
    Public Const LOOKUP_REMEDIAL_ACTION As Integer = 15
    Public Const LOOKUP_ARCHIVE_LOCATION As Integer = 16
    Public Const LOOKUP_PREMIUM_CHARGES As Integer = 17
    Public Const LOOKUP_CONTACTS As Integer = 18
    Public Const LOOKUP_PROJECTS As Integer = 19

    Public Const SUBMISSION_TSE As Integer = 0
    Public Const SUBMISSION_NONTSE As Integer = 1

    Public Const ADD_SAMPLE_TOOLTIP As String = "Use this button to add a sample to the current submission."
    Public Const EDIT_SAMPLE_TOOLTIP As String = "Use this button to edit the sample that has been selected."
    Public Const DELETE_SAMPLE_TOOLTIP As String = "Use this button to delete the sample that has been selected."
    Public Const COPY_SAMPLE_TOOLTIP As String = "Use this button to copy the sample that has been selected."
    Public Const ADD_BLOCK_TOOLTIP As String = "Use this button to add a block to the sample that has been selected."
    Public Const EDIT_BLOCK_TOOLTIP As String = "Use this button to edit the block that has been selected."
    Public Const DELETE_BLOCK_TOOLTIP As String = "Use this button to delete the block that has been selected."
    Public Const COPY_BLOCK_TOOLTIP As String = "Use this button to copy the block that has been selected."
    Public Const SUBMISSION_SUMMARY_TOOLTIP As String = "Use this button to display the summary of the current submission."
    Public Const COPY_BLOCK_TISSUE_SUBMISSION_TOOLTIP As String = "Use this button to copy blocks from samples in another submission."
    Public Const COPY_SAMPLE_TISSUE_SUBMISSION_TOOLTIP As String = "Use this button to copy sample tissues in another submission."
    Public Const COPY_BATCH_BLOCK_TOOLTIP As String = "Use this button to select the sample you want the blocks to be copied to."
    Public Const COPY_BATCH_SAMPLE_TOOLTIP As String = "Use this button to select the sample you want the tissues to be copied to."
#End Region

#Region "Enums"

    Public Enum HistologyRefType As Integer
        eNeuropath = 1
        eAbattoirSurvey = 2
        eTBDiag = 3
        eGeneralPool = 4
        eMouseProjects = 5
        eUsePGNumber = 6
    End Enum

#End Region

#Region "Public Methods"

    Public Function ValidateMouseNumber(ByVal sValue As String) As Boolean
        Dim sMouseNumber As String = CStr(sValue)
        Dim revMouseNumber As Regex = New Regex("MC[0-9][0-9][0-9][0-9][0-9][0-9]")
        Dim match As Match

        sMouseNumber = sMouseNumber.ToUpper()
        match = revMouseNumber.Match(sMouseNumber)

        If match.Success Then
            Return True
        Else
            Return False
        End If
    End Function

    Public Function ConvertBlockRefToString(ByVal iBlockRef As Integer) As String
        If iBlockRef < 10 Then
            Return "0" & Convert.ToString(iBlockRef)
        Else
            Return Convert.ToString(iBlockRef)
        End If
    End Function

    Public Function IsBatchPreCassetted(ByVal dsBatchDetails As DataSet, ByVal iBatchID As Integer) As Boolean
        If dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_SUBMITTEDAS_TABLE).Rows.Count <> 0 Then
            Dim sSubmittedAs As String
            Dim foundRows As DataRow()

            foundRows = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_SUBMITTEDAS_TABLE).Select("BatchID=" & Convert.ToString(iBatchID))

            If Not foundRows Is Nothing Then
                If foundRows.Length > 0 Then
                    sSubmittedAs = foundRows(0)("Code").ToString()

                    ' If pre cassetted use a precassetted block
                    If sSubmittedAs = "5" Then
                        Return True
                    Else
                        Return False
                    End If
                End If
            End If
        End If
    End Function

    Public Function ValidateHistoRef(ByVal sHistologyRef As String, ByVal bHistologyUser As Boolean) As Boolean
        Dim sHistoYear As String
        Dim dDate As Date
        Dim revHistologyRef As Regex = New Regex("[0-9][0-9](/)[0-9][0-9][0-9][0-9][0-9]")
        Dim revHPRef As Regex = New Regex("[H][P][0-9][0-9][0-9][0-9][/][0-9][0-9]")
        Dim match As Match

        If bHistologyUser Then
            match = revHPRef.Match(sHistologyRef)

            If match.Success Then
                Return False
                Exit Function
            End If
        End If

        If sHistologyRef.IndexOf("-") <> -1 Then
            Return False
            Exit Function
        Else
            If sHistologyRef.Length <> 8 Then
                Return False
            Else
                match = revHistologyRef.Match(sHistologyRef)

                If match.Success Then
                    sHistoYear = Left$(sHistologyRef, 2)
                    Dim sYear As String = Right$(dDate.Now.Year(), 2)

                    If Convert.ToInt32(sHistoYear) > Convert.ToInt32(sYear) And Convert.ToInt32(sHistoYear) < 70 Then
                        Return False
                    Else
                        Return True
                    End If
                    Exit Function
                Else
                    Return False
                    Exit Function
                End If
            End If
        End If
    End Function

    Public Function IsCurrentYearHistoRef(ByVal HistologyRef As String) As Boolean
        'If not in correct format will catch the exception and return false
        Try
            Dim strYear As String
            Dim strNowYear As String
            Dim dDate As Date

            strNowYear = dDate.Year().ToString()
            strNowYear = Right$(strNowYear, 2)
            strYear = Left$(HistologyRef, 2)

            Return strYear = strNowYear

        Catch ex As Exception
            Return False
        End Try
    End Function

    Public Function IsPreviousYearHistoRef(ByVal HistologyRef As String) As Boolean
        'if histology ref is not in correct format an exception will be caught and false returned

        Try
            Dim dDate As Date
            Dim sHistoYear As String = ""
            Dim iHistoYear As Integer = 0
            Dim sActualYear As String = ""
            Dim iActualYear As Integer = 0
            Dim bBeforeCurrentDate As Boolean = False

            sHistoYear = Left$(HistologyRef, 2)
            iHistoYear = CInt(sHistoYear)
            iActualYear = Right$(dDate.Now.Year(), 2)
            sActualYear = Right$(dDate.Now.Year(), 2).ToString

            If iHistoYear < iActualYear Then
                bBeforeCurrentDate = True
            Else
                If IsPre00(iHistoYear) Then
                    bBeforeCurrentDate = True
                End If
            End If

            Return bBeforeCurrentDate

        Catch ex As Exception
            Return False
        End Try
    End Function

    Public Function GenerateHistoFromPG(ByVal sSenderRef As String) As String
        'If not incorrect format will catch the exception and return an empty string
        Try
            Dim strYear As String
            Dim strID As String
            Dim strSender As String = sSenderRef

            strSender = strSender.Substring(2)
            strID = Left$(strSender, 4)
            strYear = Right$(strSender, 2)

            Return strYear + "/" + "0" + strID

        Catch ex As Exception
            Return ""
        End Try
    End Function

    Public Function CheckIsPGNumber(ByVal sSenderRef As String) As Boolean
        Dim sSenderNumber As String = ""
        Dim sSenderCode As String = ""
        Dim bIsPG As Boolean = False

        If sSenderRef.Length >= 2 Then
            sSenderCode = Left$(sSenderRef, 2).ToUpper
            sSenderNumber = Right$(sSenderRef, Len(sSenderRef) - 2)
        Else
            bIsPG = False
        End If

        sSenderCode = sSenderCode.ToUpper()

        If sSenderCode = "PG" Then
            Dim revSenderRef As Regex = New Regex("PG[0-9][0-9][0-9][0-9](/)[0-9][0-9]")
            Dim match As Match

            sSenderRef = sSenderCode + sSenderNumber
            match = revSenderRef.Match(sSenderRef)

            If sSenderRef.Length <> 9 Then
                bIsPG = False
            Else
                If match.Success Then
                    bIsPG = True
                Else
                    bIsPG = False
                End If
            End If
        Else
            bIsPG = False
        End If

        Return bIsPG

    End Function

    Public Function CheckRange(ByVal iVal) As Integer
        If iVal >= 10000 And iVal < 20000 Then
            Return HistologyRefType.eNeuropath
        ElseIf iVal >= 20000 And iVal < 30000 Then
            Return HistologyRefType.eAbattoirSurvey
        ElseIf iVal >= 30000 And iVal < 40000 Then
            Return HistologyRefType.eTBDiag
        ElseIf iVal >= 40000 And iVal < 60000 Then
            Return HistologyRefType.eGeneralPool
        ElseIf iVal >= 60000 And iVal < 90000 Then
            Return HistologyRefType.eMouseProjects
        End If
    End Function

    Public Function IsPre02(ByVal sYearPart As String) As Boolean
        If sYearPart = "01" Then
            Return True
        ElseIf sYearPart = "00" Then
            Return True
        ElseIf sYearPart = "99" Then
            Return True
        ElseIf sYearPart = "98" Then
            Return True
        ElseIf sYearPart = "97" Then
            Return True
        ElseIf sYearPart = "96" Then
            Return True
        ElseIf sYearPart = "95" Then
            Return True
        ElseIf sYearPart = "94" Then
            Return True
        ElseIf sYearPart = "93" Then
            Return True
        ElseIf sYearPart = "92" Then
            Return True
        ElseIf sYearPart = "91" Then
            Return True
        ElseIf sYearPart = "90" Then
            Return True
        ElseIf sYearPart = "89" Then
            Return True
        ElseIf sYearPart = "88" Then
            Return True
        ElseIf sYearPart = "87" Then
            Return True
        ElseIf sYearPart = "86" Then
            Return True
        ElseIf sYearPart = "85" Then
            Return True
        ElseIf sYearPart = "84" Then
            Return True
        ElseIf sYearPart = "83" Then
            Return True
        ElseIf sYearPart = "82" Then
            Return True
        ElseIf sYearPart = "81" Then
            Return True
        ElseIf sYearPart = "80" Then
            Return True
        ElseIf sYearPart = "79" Then
            Return True
        ElseIf sYearPart = "78" Then
            Return True
        ElseIf sYearPart = "77" Then
            Return True
        ElseIf sYearPart = "76" Then
            Return True
        ElseIf sYearPart = "75" Then
            Return True
        ElseIf sYearPart = "74" Then
            Return True
        ElseIf sYearPart = "73" Then
            Return True
        ElseIf sYearPart = "72" Then
            Return True
        ElseIf sYearPart = "71" Then
            Return True
        ElseIf sYearPart = "70" Then
            Return True
        Else
            Return False
        End If
    End Function

    Public Function IsPreEqual01(ByVal sYearPart As String) As Boolean
        If sYearPart = "01" Then
            Return True
        ElseIf sYearPart = "00" Then
            Return True
        ElseIf sYearPart = "99" Then
            Return True
        ElseIf sYearPart = "98" Then
            Return True
        ElseIf sYearPart = "97" Then
            Return True
        ElseIf sYearPart = "96" Then
            Return True
        ElseIf sYearPart = "95" Then
            Return True
        ElseIf sYearPart = "94" Then
            Return True
        ElseIf sYearPart = "93" Then
            Return True
        ElseIf sYearPart = "92" Then
            Return True
        ElseIf sYearPart = "91" Then
            Return True
        ElseIf sYearPart = "90" Then
            Return True
        ElseIf sYearPart = "89" Then
            Return True
        ElseIf sYearPart = "88" Then
            Return True
        ElseIf sYearPart = "87" Then
            Return True
        ElseIf sYearPart = "86" Then
            Return True
        ElseIf sYearPart = "85" Then
            Return True
        ElseIf sYearPart = "84" Then
            Return True
        ElseIf sYearPart = "83" Then
            Return True
        ElseIf sYearPart = "82" Then
            Return True
        ElseIf sYearPart = "81" Then
            Return True
        ElseIf sYearPart = "80" Then
            Return True
        ElseIf sYearPart = "79" Then
            Return True
        ElseIf sYearPart = "78" Then
            Return True
        ElseIf sYearPart = "77" Then
            Return True
        ElseIf sYearPart = "76" Then
            Return True
        ElseIf sYearPart = "75" Then
            Return True
        ElseIf sYearPart = "74" Then
            Return True
        ElseIf sYearPart = "73" Then
            Return True
        ElseIf sYearPart = "72" Then
            Return True
        ElseIf sYearPart = "71" Then
            Return True
        ElseIf sYearPart = "70" Then
            Return True
        Else
            Return False
        End If
    End Function

    Public Function IsAfter01(ByVal sYearPart As String) As Boolean
        If sYearPart = "00" Then
            Return True
        ElseIf sYearPart = "99" Then
            Return True
        ElseIf sYearPart = "98" Then
            Return True
        ElseIf sYearPart = "97" Then
            Return True
        ElseIf sYearPart = "96" Then
            Return True
        ElseIf sYearPart = "95" Then
            Return True
        ElseIf sYearPart = "94" Then
            Return True
        ElseIf sYearPart = "93" Then
            Return True
        ElseIf sYearPart = "92" Then
            Return True
        ElseIf sYearPart = "91" Then
            Return True
        ElseIf sYearPart = "90" Then
            Return True
        ElseIf sYearPart = "89" Then
            Return True
        ElseIf sYearPart = "88" Then
            Return True
        ElseIf sYearPart = "87" Then
            Return True
        ElseIf sYearPart = "86" Then
            Return True
        ElseIf sYearPart = "85" Then
            Return True
        ElseIf sYearPart = "84" Then
            Return True
        ElseIf sYearPart = "83" Then
            Return True
        ElseIf sYearPart = "82" Then
            Return True
        ElseIf sYearPart = "81" Then
            Return True
        ElseIf sYearPart = "80" Then
            Return True
        ElseIf sYearPart = "79" Then
            Return True
        ElseIf sYearPart = "78" Then
            Return True
        ElseIf sYearPart = "77" Then
            Return True
        ElseIf sYearPart = "76" Then
            Return True
        ElseIf sYearPart = "75" Then
            Return True
        ElseIf sYearPart = "74" Then
            Return True
        ElseIf sYearPart = "73" Then
            Return True
        ElseIf sYearPart = "72" Then
            Return True
        ElseIf sYearPart = "71" Then
            Return True
        ElseIf sYearPart = "70" Then
            Return True
        Else
            Return False
        End If
    End Function

    Public Function IsPre00(ByVal sYearPart As String) As Boolean
        If sYearPart = "99" Then
            Return True
        ElseIf sYearPart = "98" Then
            Return True
        ElseIf sYearPart = "97" Then
            Return True
        ElseIf sYearPart = "96" Then
            Return True
        ElseIf sYearPart = "95" Then
            Return True
        ElseIf sYearPart = "94" Then
            Return True
        ElseIf sYearPart = "93" Then
            Return True
        ElseIf sYearPart = "92" Then
            Return True
        ElseIf sYearPart = "91" Then
            Return True
        ElseIf sYearPart = "90" Then
            Return True
        ElseIf sYearPart = "89" Then
            Return True
        ElseIf sYearPart = "88" Then
            Return True
        ElseIf sYearPart = "87" Then
            Return True
        ElseIf sYearPart = "86" Then
            Return True
        ElseIf sYearPart = "85" Then
            Return True
        ElseIf sYearPart = "84" Then
            Return True
        ElseIf sYearPart = "83" Then
            Return True
        ElseIf sYearPart = "82" Then
            Return True
        ElseIf sYearPart = "81" Then
            Return True
        ElseIf sYearPart = "80" Then
            Return True
        ElseIf sYearPart = "79" Then
            Return True
        ElseIf sYearPart = "78" Then
            Return True
        ElseIf sYearPart = "77" Then
            Return True
        ElseIf sYearPart = "76" Then
            Return True
        ElseIf sYearPart = "75" Then
            Return True
        ElseIf sYearPart = "74" Then
            Return True
        ElseIf sYearPart = "73" Then
            Return True
        ElseIf sYearPart = "72" Then
            Return True
        ElseIf sYearPart = "71" Then
            Return True
        ElseIf sYearPart = "70" Then
            Return True
        Else
            Return False
        End If
    End Function

    Public Sub OpenDownloadPopup(ByVal sReport As String, ByRef pPage As Page)
        If Not pPage.IsClientScriptBlockRegistered("ReportRedirect") Then

            If HttpContext.Current.Request.Browser.JavaScript Then
                Dim sJScript As New System.Text.StringBuilder

                sJScript.Append("<script language=""JavaScript"">")
                sJScript.Append("<!-- " + vbCrLf)
                sJScript.Append("var w=window.open (""" & sReport & "")
                sJScript.Append(""",""_blank"",""top=5,left=5,width=800,height=600,buttons=no,scrollbars=yes,location=no,menubar=no,resizable=yes,status=no,directories=no,toolbar=no"");" + vbCrLf)
                sJScript.Append("w.focus()" + vbCrLf)
                sJScript.Append("// -->")
                sJScript.Append("</script>")

                pPage.RegisterClientScriptBlock("ReportRedirect", sJScript.ToString())
            End If
        End If
    End Sub

    Public Sub DontRestoreFocus(ByRef pPage As Page)
        If Not pPage.IsClientScriptBlockRegistered("DontRestoreFocus") Then

            If HttpContext.Current.Request.Browser.JavaScript Then
                Dim sJScript As New System.Text.StringBuilder

                sJScript.Append("<script language=""JavaScript"">")
                sJScript.Append("<!-- " + vbCrLf)
                sJScript.Append("if (window.__smartNav != null)" + vbNewLine)
                sJScript.Append("{ " + vbNewLine)
                sJScript.Append("        window.__smartNav.restoreFocus = function() { }" + vbNewLine)
                sJScript.Append("}" + vbNewLine)
                sJScript.Append("// -->")
                sJScript.Append("</script>")
                pPage.RegisterClientScriptBlock("DontRestoreFocus", sJScript.ToString())
            End If
        End If
    End Sub

    Public Sub UpdateCheckBoxData(ByVal chkblist As CheckBoxList, ByRef dtData As DataTable, ByVal iBatchID As Integer)
        Dim objChkbl As New HistopathologyLib.clsCheckBoxData
        Dim li As ListItem
        Dim sFilter As String
        Dim drFoundRow As DataRow()

        For Each li In chkblist.Items
            sFilter = "Code=" & "'" & li.Value & "'"
            drFoundRow = dtData.Select(sFilter)
            If li.Selected = True Then
                'if its a new item
                If Not drFoundRow Is Nothing And drFoundRow.Length = 0 Then
                    If Not objChkbl.NewItem(dtData, li.Value, iBatchID, "BatchID") Then
                        Throw New Exception("CheckBoxList.NewItem returned false.")
                    End If
                End If
            Else
                'If its been unchecked
                If Not drFoundRow Is Nothing And drFoundRow.Length = 1 Then
                    drFoundRow(0).Delete()
                    'dtData.Rows.Remove(drFoundRow(0))
                End If
            End If
        Next
    End Sub

    Public Function CopyDataTable(ByVal dtDataTable As DataTable, Optional ByVal sFilter As String = "") As DataTable
        Dim dt As New DataTable
        Dim dr As DataRow

        If sFilter <> "" Then
            Dim drFoundRows As DataRow()
            drFoundRows = dtDataTable.Select(sFilter)
            If Not drFoundRows Is Nothing Then
                dt = dtDataTable.Clone()
                For Each dr In drFoundRows
                    dt.ImportRow(dr)
                Next
            End If
            Return dt
        Else
            Return dtDataTable.Copy()
        End If
    End Function

    Public Function GetGridPart(ByVal strUniqueID) As String
        Return strUniqueID.SubString(0, (strUniqueID.LastIndexOf(":") + 1))
    End Function

    Public Function FormatDate(ByVal sDate As String) As String
        Return Trim(Replace(sDate, "00:00:00", ""))
    End Function

    Public Function FormatEmptyString(ByVal sString As String) As Object
        If sString = "" Then
            Return DBNull.Value
        Else
            Return sString
        End If
    End Function

    Public Function GetLoggedOnUser() As String
        Dim strUser As String

        Try
            strUser = HttpContext.Current.User.Identity.Name
            Dim intSlashPos As Integer = strUser.IndexOf("\")
            If intSlashPos >= 0 And strUser.Length > intSlashPos + 1 Then
                strUser = strUser.Substring(intSlashPos + 1)
            End If

            Return strUser
        Catch
            Return Nothing
        End Try
    End Function

    Public Sub AddItemToDropDownList(ByRef ddlList As DropDownList, _
                                     Optional ByVal sText As String = "", _
                                     Optional ByVal sValue As String = "", _
                                     Optional ByVal iIndex As Integer = 0)

        Dim liItem As New System.Web.UI.WebControls.ListItem

        liItem.Text = sText
        liItem.Value = sValue
        ddlList.Items.Insert(iIndex, liItem)

    End Sub

    Public Sub AddItemToEndOfDropDownList(ByRef ddlList As DropDownList, _
                                          ByVal strText As String, _
                                          ByVal strValue As String)

        Dim liItem As New System.Web.UI.WebControls.ListItem

        liItem.Text = strText
        liItem.Value = strValue
        ddlList.Items.Add(liItem)
    End Sub

    Public Sub SelectDescriptionInDropDownList(ByRef ddlList As DropDownList, _
                                               ByVal strDescription As String)
        Dim liItem As System.Web.UI.WebControls.ListItem

        For Each liItem In ddlList.Items
            If (liItem.Text = Trim(strDescription)) Then
                ddlList.SelectedItem.Selected = False
                liItem.Selected = True
                Exit For
            End If
        Next
    End Sub

    Public Sub SelectItemInDropDownList(ByRef ddlList As DropDownList, _
                                        ByVal strValue As String)

        Dim liItem As System.Web.UI.WebControls.ListItem

        For Each liItem In ddlList.Items
            If (liItem.Value = Trim(strValue)) Then
                ddlList.SelectedItem.Selected = False
                liItem.Selected = True
                Exit For
            End If
        Next
    End Sub

    Public Function GetSelectedItemFromDropDownList(ByRef ddlList As DropDownList) As String
        If ddlList.SelectedItem Is Nothing Then
            Return ""
        Else
            Return Trim(ddlList.SelectedItem.Value)
        End If
    End Function

    Public Function GetSelectedTextFromDropDownList(ByRef ddlList As DropDownList) As String
        If ddlList.SelectedItem Is Nothing Then
            Return ""
        Else
            Return ddlList.SelectedItem.Text
        End If
    End Function

    ' Send down Javascript to set focus to a particular control
    Public Function SetFocus(ByVal ctl As System.Web.UI.Control, _
                                    Optional ByVal bSelectAll As Boolean = False) As Boolean
        If HttpContext.Current.Request.Browser.JavaScript Then
            Dim scr As String
            scr = "<script language='javascript'><!--" & vbNewLine
            scr &= "var ctl = document.all(""" & ctl.UniqueID & """);" & vbNewLine
            scr &= "ctl.focus();" & vbNewLine

            If bSelectAll Then
                scr &= "ctl.select();" & vbNewLine
            End If

            scr &= "--></script>"

            ctl.Page.RegisterStartupScript("SetFocus", scr)

            Return True
        Else
            Return False
        End If
    End Function

    ' Sets default button for a textbox
    Public Function SetTextboxDefaultButton(ByRef ctlTextbox As TextBox, _
                                            ByRef ctlButton As Button) As Boolean

        If HttpContext.Current.Request.Browser.JavaScript Then
            Dim scr As New System.Text.StringBuilder

            With scr
                .Append("<SCRIPT language=""javascript"">" & vbNewLine)
                .Append("function " & ctlTextbox.ClientID & "_OnKeyDown(btn)" & vbNewLine + "{" + vbNewLine)
                .Append("   if (event.keyCode == 13) {" & vbNewLine)
                .Append("       event.returnValue=false;" & vbNewLine)
                .Append("       event.cancel = true;" & vbNewLine)
                .Append("       if (!(btn.disabled)) {" & vbNewLine)
                .Append("           btn.click();" & vbNewLine)
                .Append("       }" & vbNewLine)
                .Append("   } " & vbNewLine)
                .Append("}" & vbNewLine)
                .Append("</SCRIPT>" & vbNewLine)
            End With
            ctlTextbox.Attributes.Add("onkeydown", ctlTextbox.ClientID & "_OnKeyDown(document.all(""" & ctlButton.UniqueID & """))")
            ctlButton.Page.RegisterStartupScript(ctlTextbox.ClientID & "DefaultButton", scr.ToString())
            Return True
        Else
            Return False
        End If
    End Function


    Public Sub SetTextboxControlOnEnter(ByRef ctlTextbox As TextBox, _
                                        ByVal sControlClientID As String)

        If HttpContext.Current.Request.Browser.JavaScript Then
            Dim scr As New System.Text.StringBuilder

            With scr
                .Append("<SCRIPT language=""javascript"">" & vbNewLine)
                .Append("function " & ctlTextbox.ClientID & "_OnKeyDown(bIsYear)" & vbNewLine + "{" + vbNewLine)
                .Append("    if (event.keyCode == 13) {" & vbNewLine)
                .Append("        event.returnValue=false;" & vbNewLine)
                .Append("        event.cancel = true;" & vbNewLine)
                .Append("       if (!(Form1." & sControlClientID & ".disabled)) {" & vbNewLine)
                .Append("           Form1." & sControlClientID & ".focus()" & vbNewLine)
                .Append("       }" & vbNewLine)
                .Append("    } " + vbNewLine)
                .Append("}" + vbNewLine)
                .Append("</SCRIPT>" + vbNewLine)
            End With
            ctlTextbox.Attributes.Add("onkeydown", ctlTextbox.ClientID & "_OnKeyDown()")
            ctlTextbox.Page.RegisterStartupScript(ctlTextbox.ClientID & "EnterKey", scr.ToString())
        End If
    End Sub

    Public Sub SetDropDownControlOnEnter(ByRef ctlDropDownList As Object, _
                                        ByVal sControlClientID As String)

        If HttpContext.Current.Request.Browser.JavaScript Then
            Dim scr As New System.Text.StringBuilder

            With scr
                .Append("<SCRIPT language=""javascript"">" & vbNewLine)
                .Append("function " & ctlDropDownList.ClientID & "_OnKeyDown(bIsYear)" & vbNewLine + "{" + vbNewLine)
                .Append("    if (event.keyCode == 13) {" & vbNewLine)
                .Append("        event.returnValue=false;" & vbNewLine)
                .Append("        event.cancel = true;" & vbNewLine)
                .Append("       if (!(document.forms[0]." & sControlClientID & ".disabled)) {" & vbNewLine)
                .Append("           document.forms[0]." & sControlClientID & ".focus()" & vbNewLine)
                .Append("       }" & vbNewLine)
                .Append("    } " + vbNewLine)
                .Append("}" + vbNewLine)
                .Append("</SCRIPT>" + vbNewLine)
            End With
            ctlDropDownList.Attributes.Add("onkeydown", ctlDropDownList.ClientID & "_OnKeyDown()")
            ctlDropDownList.Page.RegisterStartupScript(ctlDropDownList.ClientID & "EnterKey", scr.ToString())
        End If
    End Sub

    ' Sets handler for CalendarDate user control
    Public Function SetCalendarDateHandler(ByRef pge As Page) As Boolean
        If HttpContext.Current.Request.Browser.JavaScript Then
            Dim scr As New System.Text.StringBuilder

            scr.Append("<SCRIPT language=""javascript"">" + vbNewLine)
            scr.Append("function SetDate(strCtrl, strDate)" + vbNewLine + "{" + vbNewLine)
            scr.Append("    var btn = strCtrl + ""_btnCalendar"";" + vbNewLine)
            scr.Append("    var fme = ""fme_"" + strCtrl;" + vbNewLine)
            scr.Append("    var txtDate = strCtrl + ""_txtDate"";" + vbNewLine)
            scr.Append("    var txtClosed = strCtrl + ""_txtClosedCalendar"";" + vbNewLine)
            scr.Append("    var txtClosed = strCtrl + ""_txtClosedCalendar"";" + vbNewLine + vbNewLine)
            scr.Append("    document.all[fme].style.visibility = ""hidden"";" + vbNewLine)
            scr.Append("    document.all[btn].src=""Images/calendarOpenDown.gif"";" + vbNewLine)
            scr.Append("    document.all[txtClosed].value = ""changed"";" + vbNewLine)
            scr.Append("    document.all[txtDate].value = strDate;" + vbNewLine)
            scr.Append("}" + vbNewLine)
            scr.Append("</SCRIPT>" + vbNewLine)

            pge.RegisterClientScriptBlock("SetCalendarDateHandler", scr.ToString())
            Return True
        Else
            Return False
        End If
    End Function

    Public Function IsDateRangeValid(ByVal ctlDateFrom As CalendarDate, ByVal ctlDateTo As CalendarDate, ByVal sName As String) As Boolean

        Dim bDatesValid As Boolean = True
        Dim dDate As Date

        If ctlDateFrom.DateField <> "" AndAlso IsDate(ctlDateTo.DateField) Then
            bDatesValid = ctlDateFrom.Validate(CDate(ctlDateTo.DateField), CalendarDate.ValidationType.eValidateLatest, "Must be earlier than the specified latest " & sName)
        End If
        If ctlDateTo.DateField <> "" AndAlso IsDate(ctlDateFrom.DateField) Then
            bDatesValid = bDatesValid And ctlDateTo.Validate(CDate(ctlDateFrom.DateField), CalendarDate.ValidationType.eValidateEarliest, "Must be later than the specified earliest " & sName)
        End If
        bDatesValid = bDatesValid And ctlDateFrom.Validate(dDate) And ctlDateTo.Validate(dDate)

        Return bDatesValid

    End Function

    Public Function PromptBeforeNavigateScript(ByVal sPrompt As String, ByVal sURL As String) As String

        Dim jScript As System.Text.StringBuilder = New System.Text.StringBuilder
        With jScript
            .Append("<script language=""JavaScript"">")
            .Append("if (confirm(""")
            .Append(sPrompt)
            .Append(""")) { location.href=""")
            .Append(sURL)
            .Append(""" }")
            .Append("</script>")
        End With
        Return jScript.ToString()

    End Function

    Public Function PromptBeforeChangeScript(ByVal sPrompt As String, ByVal ctl As DropDownList, ByVal sVal As String, ByVal ctlTmpTextBox As TextBox) As String

        Dim jScript As System.Text.StringBuilder = New System.Text.StringBuilder
        With jScript
            .Append("<script language=""JavaScript"">")
            .Append("if (confirm(""")
            .Append(sPrompt)
            .Append("""))")
            .Append("{" + vbNewLine)
            .Append("__doPostBack('txtTmpSelectedProject','');" + vbNewLine)
            .Append("}" + vbNewLine)
            .Append("else" + vbNewLine)
            .Append("{" + vbNewLine)
            .Append("document.forms[0]." & ctl.ClientID & ".value = '" & sVal & "';" + vbNewLine)
            .Append("document.forms[0]." & ctlTmpTextBox.ClientID & ".value = '" & sVal & "';" + vbNewLine)
            .Append("__doPostBack('txtTmpSelectedProject','');" + vbNewLine)
            .Append("}" + vbNewLine)

            .Append("</script>")
        End With
        Return jScript.ToString()

    End Function

    Public Function PromptScript(ByVal sPrompt As String) As String

        Dim jScript As System.Text.StringBuilder = New System.Text.StringBuilder
        With jScript
            .Append("<script language=""JavaScript"">")
            .Append("alert(""")
            .Append(sPrompt)
            .Append(""");")
            .Append("</script>")
        End With
        Return jScript.ToString()

    End Function

    Public Sub RemoveSessionVars(ByRef objSession As System.Web.SessionState.HttpSessionState)
        With objSession
            .Remove(SessionVars.SV_HeaderUserName)
            .Remove(SessionVars.SV_HeaderGroupName)
            .Remove(SessionVars.SV_HeaderGroupID)
            .Remove(SessionVars.SV_HeaderUserID)
            .Remove(SessionVars.SV_HeaderUserEmail)
            .Remove(SessionVars.SV_HeaderUserArea)
            .Remove(SessionVars.SV_HeaderUserAreaID)
            .Remove(SessionVars.SV_LookupDataTable)
            .Remove(SessionVars.SV_LookupDataView)
            .Remove(SessionVars.Sv_BlockID)
            .Remove(SessionVars.SV_BatchID)
            .Remove(SessionVars.SV_BatchDetails)
            .Remove(SessionVars.SV_OldBatchDetails)
            .Remove(SessionVars.SV_BatchSubmissionID)
            .Remove(SessionVars.SV_AnimalID)
            .Remove(SessionVars.SV_BatchesNotReceivedTable)
            .Remove(SessionVars.SV_BatchesNotReceivedView)
            .Remove(SessionVars.SV_BatchesWaitingDispatchTable)
            .Remove(SessionVars.SV_BatchesWaitingDispatchView)
            .Remove(SessionVars.SV_BatchesForEditingTable)
            .Remove(SessionVars.SV_BatchesForEditingView)
            .Remove(SessionVars.SV_BatchesForArchivingTable)
            .Remove(SessionVars.SV_BatchesForArchivingView)
            .Remove(SessionVars.SV_BatchQCNotesTable)
            .Remove(SessionVars.SV_BatchQCNotesView)
            .Remove(SessionVars.SV_BatchSummaryTable)
            .Remove(SessionVars.SV_BatchSummaryView)
            .Remove(SessionVars.SV_BlockSummaryTable)
            .Remove(SessionVars.SV_BlockSummaryView)
            .Remove(SessionVars.SV_AnimalTable)
            .Remove(SessionVars.SV_AnimalView)
            .Remove(SessionVars.SV_AnimalTableBackup)
            .Remove(SessionVars.SV_TissuesTable)
            .Remove(SessionVars.SV_TissuesView)
            .Remove(SessionVars.SV_SenderRef)
            .Remove(SessionVars.SV_HistologyRef)
            .Remove(SessionVars.SV_RowStamp)
            .Remove(SessionVars.SV_BlockRef)
            .Remove(SessionVars.SV_SelectedHistologyArray)
            .Remove(SessionVars.SV_SelectedStainArray)
            .Remove(SessionVars.SV_SelectedAntibodiesArray)
            .Remove(SessionVars.SV_SubmissionType)
            .Remove(SessionVars.SV_TissuesBeforeChanges)
            .Remove(SessionVars.SV_Editing)
            .Remove(SessionVars.SV_EditingBatch)
            .Remove(SessionVars.SV_EditingBlock)
            .Remove(SessionVars.SV_ReceiveBatch)
            .Remove(SessionVars.SV_RedirectPage)
            .Remove(SessionVars.SV_RedirectCancelPage)
            .Remove(SessionVars.SV_AddSampleNextPage)
            .Remove(SessionVars.SV_AddSamplePrevPage)
            .Remove(SessionVars.SV_SearchBlockRefsRedirectPage)
            .Remove(SessionVars.Sv_CopySubmission)
            .Remove(SessionVars.SV_CopySample)
            .Remove(SessionVars.SV_CopyBlocks)
            .Remove(SessionVars.SV_TempPickSenderList)
            .Remove(SessionVars.SV_AntibodiesList)
            .Remove(SessionVars.SV_TestTable)
            .Remove(SessionVars.SV_TestView)
            .Remove(SessionVars.SV_UsersTable)
            .Remove(SessionVars.SV_UsersView)
            .Remove(SessionVars.SV_BatchesTable)
            .Remove(SessionVars.SV_BatchesView)
            .Remove(SessionVars.SV_Cassetted)
            .Remove(SessionVars.SV_AssignBlocks)
            .Remove(SessionVars.SV_HistologyRefSet)
            .Remove(SessionVars.SV_HistologyRefType)
            .Remove(SessionVars.SV_HistologyRefTable)
            .Remove(SessionVars.SV_ExcelExport)
            .Remove(SessionVars.SV_ExcelExportView)
            .Remove(SessionVars.SV_SearchBatchDetailsTable)
            .Remove(SessionVars.SV_SearchBatchDetailsView)
            .Remove(SessionVars.SV_ViewSubmission)
            .Remove(SessionVars.SV_SearchSubmission)
            .Remove(SessionVars.SV_AnimalIDs)
            .Remove(SessionVars.SV_SelectedAnimal)
            .Remove(SessionVars.SV_SaveFromBatchDetails)
            .Remove(SessionVars.SV_SubmittedAs)
            .Remove(SessionVars.SV_SubmissionStatus)
            .Remove(SessionVars.SV_PickListTableID)
            .Remove(SessionVars.SV_HistoRefsVersion)
            .Remove(SessionVars.SV_ProjectCode)
            .Remove(SessionVars.SV_BreadCrumbs)
            .Remove(SessionVars.SV_OldPGNumber)
            .Remove(SessionVars.SV_Species)
            .Remove(SessionVars.SV_PMDate)
            .Remove(SessionVars.SV_PassUserArea)
            .Remove(SessionVars.SV_ImportedFromDayBook)
            .Remove(SessionVars.SV_QCNoteNumbers)
            .Remove(SessionVars.SV_UseValidation)
            .Remove(SessionVars.SV_CopySampleBlocksSummaryTable)
            .Remove(SessionVars.SV_CopySampleBlocksSummaryView)
            .Remove(SessionVars.SV_RedirectAfterPrint)
            .Remove(SessionVars.SV_BlockIDs)
            .Remove(SessionVars.SV_SelectedAnimalNumberBlocks)
            .Remove(SessionVars.SV_SearchCriteria)
            .Remove(SessionVars.SV_Sort)
            .Remove(SessionVars.SV_BatchBlockSummaryPage)
            .Remove(SessionVars.SV_CreatingNewBatch)
            .Remove(SessionVars.SV_UnusedHistologyRef)
            .Remove(SessionVars.SV_UsedHistologyRef)
        End With

    End Sub

    Public Function GetListType(ByVal sCode As String, ByVal lookuplist As Integer) As String
        Try
            Dim dt As DataTable = GetLookupTypeList(lookuplist)

            If Not dt Is Nothing Then
                Dim dv As New DataView(dt, "", "Code", DataViewRowState.CurrentRows)
                Dim iRow As Integer = dv.Find(sCode)
                If iRow >= 0 Then
                    Return dv(iRow).Item("Description").ToString()
                Else
                    Return ""
                End If
            Else
                Return ""
            End If
        Catch ex As Exception
            clsAppError.DisplayError("Failed to retrieve the lookup list description.", ex)
        End Try
    End Function

    Public Function GetListTypeID(ByVal sCode As String, ByVal lookuplist As Integer) As String
        Try

            Dim objLookup As New HistopathologyLib.LookupData
            Dim dt As DataTable = objLookup.GetLookupData(lookuplist, True)

            If Not dt Is Nothing Then
                Dim dv As New DataView(dt, "", "ID", DataViewRowState.CurrentRows)
                Dim iRow As Integer = dv.Find(sCode)
                If iRow >= 0 Then
                    Return dv(iRow).Item("Description").ToString()
                Else
                    Return ""
                End If
            Else
                Return ""
            End If
        Catch ex As Exception
            clsAppError.DisplayError("Failed to retrieve the lookup list description.", ex)
        End Try
    End Function

    Public Function GetHistologyListType(ByVal sCode As String) As String
        Try
            Dim objLookup As New HistopathologyLib.LookupData
            Dim dt As DataTable = objLookup.GetHistologyLookupData()

            If Not dt Is Nothing Then
                Dim dv As New DataView(dt, "", "Code", DataViewRowState.CurrentRows)
                Dim iRow As Integer = dv.Find(sCode)
                If iRow >= 0 Then
                    Return dv(iRow).Item("Description").ToString()
                Else
                    Return ""
                End If
            Else
                Return ""
            End If
        Catch ex As Exception
            clsAppError.DisplayError("Failed to retrieve the list of Histology Types", ex)
        End Try
    End Function

    Public Function GetLookupTypeList(ByVal lookuplist As Integer) As DataTable
        Try
            Dim objLookup As New HistopathologyLib.LookupData
            Dim dt As DataTable = objLookup.GetLookupData(lookuplist)

            If dt Is Nothing Then
                Throw New Exception("LookupData.GetLookupData returned Nothing")
            End If

            Return dt

        Catch ex As Exception
            clsAppError.DisplayError("Failed to retrieve the list of lookup codes", ex)
        End Try
    End Function

    Public Function GetRowColumnData(ByRef objValue As Object) As Object
        If (IsDBNull(objValue)) Then
            Return Nothing
        Else
            Return objValue
        End If
    End Function

    Public Function IsBatchBlocked(ByVal dsBatchDetails As DataSet) As Boolean
        Try
            'Dim dsBatchDetails As DataSet = CType(objSession.Item(SessionVars.SV_BatchDetails), DataSet)
            Dim dtBatch As DataTable
            Dim bBlocked As Boolean

            If Not dsBatchDetails Is Nothing Then
                dtBatch = dsBatchDetails.Tables(HistopathologyLib.clsBatch.BATCH_TABLE)
                If Not dtBatch Is Nothing And dtBatch.Rows.Count > 0 Then
                    Return dtBatch.Rows(0)("IsBlocked")
                Else
                    Throw New Exception("Batch table is empty.")
                End If
            Else
                Throw New Exception("Batch details dataset is empty.")
            End If
        Catch ex As Exception
            clsAppError.DisplayError("Failed to verify if Batch has been blocked.", ex)
        End Try
    End Function

    'Public Sub GetSubmissionDetailsFromDatabase(ByVal iSubmissionID As Integer, ByRef objSession As System.Web.SessionState.HttpSessionState)
    '    Dim objSubmission As New HistopathologyLib.clsSubmission()
    '    Dim dsData As DataSet

    '    Try
    '        If iSubmissionID >= 0 Then
    '            If Not (objSubmission.GetSubmissionDetails(iSubmissionID, dsData)) Then
    '                Throw New Exception("Submission.GetSubmissionDetails returned False")
    '            End If

    '            If dsData.Tables.Count <> 0 Then
    '                If dsData.Tables(HistopathologyLib.clsSubmission.SUBMISSION_TABLE).Rows.Count <> 0 Then
    '                    With objSession
    '                        'Create the summary grid as this isnt returned as part of the submission details
    '                        Dim objSummary As New HistopathologyLib.clsSummary()
    '                        objSummary.CreateSummaryData(dsData)
    '                        dsData.Tables(HistopathologyLib.clsSubmission.TISSUES_TABLE).TableName = "Tissues"
    '                        .Item(SessionVars.SV_SubmissionDetails) = dsData
    '                        .Item(SessionVars.SV_SubmissionID) = iSubmissionID
    '                    End With
    '                End If
    '            Else
    '                Throw New ApplicationException("Submission.GetSubmissionDetails returned no tables")
    '            End If
    '        End If
    '    Catch ex As Exception
    '        clsAppError.DisplayError("Failed to Get Submission Details.", ex)
    '    End Try
    'End Sub

    Public Sub GetCommonBatchDetailsFromDatabase(ByVal iBatchID As Integer, ByRef objSession As System.Web.SessionState.HttpSessionState, Optional ByRef sBatchSessionObject As String = "")
        Dim objBatch As New HistopathologyLib.clsBatch
        Dim dsData As DataSet

        If sBatchSessionObject <> "" Then
            dsData = CType(objSession.Item(sBatchSessionObject), DataSet)
        Else
            dsData = CType(objSession.Item(SessionVars.SV_BatchDetails), DataSet)
        End If

        Try
            If Not (objBatch.GetCommonBatchDetails(iBatchID, dsData)) Then
                Throw New Exception("Batch.GetCommonBatchDetails returned False")
            End If

            If dsData.Tables.Count <> 0 Then
                If sBatchSessionObject <> "" Then
                    objSession.Item(sBatchSessionObject) = dsData
                Else
                    objSession.Item(SessionVars.SV_BatchDetails) = dsData
                End If
                If sBatchSessionObject = "" Then
                    objSession.Item(SessionVars.SV_BatchID) = iBatchID
                End If
            Else
                Throw New ApplicationException("Batch.GetBatchDetails returned no tables")
            End If
        Catch ex As Exception
            clsAppError.DisplayError("Failed to Get Batch Details.", ex)
        End Try
    End Sub


    Public Sub GetBatchBlockDetailsFromDatabase(ByVal iBatchID As Integer, ByRef objSession As System.Web.SessionState.HttpSessionState, Optional ByRef sBatchSessionObject As String = "")
        Dim objBatch As New HistopathologyLib.clsBatch
        Dim dsData As DataSet

        If sBatchSessionObject <> "" Then
            dsData = CType(objSession.Item(sBatchSessionObject), DataSet)
        Else
            dsData = CType(objSession.Item(SessionVars.SV_BatchDetails), DataSet)
        End If

        Try
            Dim dr As DataRow
            If Not (objBatch.GetBatchBlockDetails(iBatchID, dsData)) Then
                Throw New Exception("Batch.GetBatchBlockDetails returned False")
            End If

            If dsData.Tables.Count <> 0 Then
                dsData.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_TISSUES).TableName = "BlockTissues"

                dsData.Relations.Add("BATCH_BLOCK", _
                        dsData.Tables(HistopathologyLib.clsBatch.BATCH_TABLE).Columns("ID"), _
                        dsData.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_TABLE).Columns("BatchID"), False)

                dsData.Relations.Add("BLOCK_TISSUES", _
                        dsData.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_TABLE).Columns("ID"), _
                        dsData.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_TISSUES).Columns("BlockID"), False)

                dsData.Relations.Add("BLOCK_ANIMAL", _
                        dsData.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_TABLE).Columns("AnimalID"), _
                        dsData.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_ANIMAL).Columns("ID"), False)

                dsData.Relations.Add("ANIMAL_BLOCK", _
                        dsData.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_ANIMAL).Columns("ID"), _
                        dsData.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_TABLE).Columns("AnimalID"), False)

                dsData.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_ANIMAL).Columns.Add("RowState", System.Type.GetType("System.Int32"))
                dsData.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_ANIMAL).Columns.Add("HistoRefSet", System.Type.GetType("System.Boolean"))
                dsData.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_ANIMAL).Columns.Add("PMDateSet", System.Type.GetType("System.Boolean"))
                dsData.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_ANIMAL).Columns.Add("IsPGNumber", System.Type.GetType("System.Boolean"))
                dsData.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_ANIMAL).Columns.Add("BookedHistologyRef", System.Type.GetType("System.Boolean"))

                For Each dr In dsData.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_ANIMAL).Rows
                    dr("RowState") = DataRowState.Unchanged

                    If Not IsDBNull(dr("HistologyRef")) And Not dr("HistologyRef").ToString() = "" Then
                        dr("HistoRefSet") = True
                    Else
                        dr("HistoRefSet") = False
                    End If
                    If Not IsDBNull(dr("PMDate")) And Not dr("PMDate").ToString() = "" Then
                        dr("PMDateSet") = True
                    Else
                        dr("PMDateSet") = False
                    End If
                    dr("IsPGNumber") = False
                    dr("BookedHistologyRef") = False
                Next

                dsData.Relations.Add("BLOCK_HISTOLOGY", _
                        dsData.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_TABLE).Columns("ID"), _
                        dsData.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_HISTOLOGY).Columns("BlockID"), False)

                dsData.Relations.Add("BLOCK_ANTIBODIES", _
                        dsData.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_TABLE).Columns("ID"), _
                        dsData.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_ANTIBODIES).Columns("BlockID"), False)

                dsData.Relations.Add("BLOCK_STAIN", _
                       dsData.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_TABLE).Columns("ID"), _
                       dsData.Tables(HistopathologyLib.clsBatch.BATCH_BLOCK_STAIN).Columns("BlockID"), False)

                If sBatchSessionObject <> "" Then
                    objSession.Item(sBatchSessionObject) = dsData
                Else
                    objSession.Item(SessionVars.SV_BatchDetails) = dsData
                End If
                If sBatchSessionObject <> "" Then
                    objSession.Item(sBatchSessionObject) = dsData
                Else
                    objSession.Item(SessionVars.SV_BatchDetails) = dsData
                    objSession.Item(SessionVars.SV_BatchID) = iBatchID
                End If

            Else
                Throw New ApplicationException("Batch.GetBatchBlockDetails returned no tables")
            End If
        Catch ex As Exception
            clsAppError.DisplayError("Failed to Get Batch Details.", ex)
        End Try
    End Sub

    Public Sub GetBatchSubmissionDetailsFromDatabase(ByVal iBatchID As Integer, ByRef objSession As System.Web.SessionState.HttpSessionState, Optional ByRef sBatchSessionObject As String = "")
        Dim objBatch As New HistopathologyLib.clsBatch
        Dim dsData As DataSet

        If sBatchSessionObject <> "" Then
            dsData = CType(objSession.Item(sBatchSessionObject), DataSet)
        Else
            dsData = CType(objSession.Item(SessionVars.SV_BatchDetails), DataSet)
        End If

        Try
            Dim dr As DataRow
            If Not (objBatch.GetBatchSubmissionDetails(iBatchID, dsData)) Then
                Throw New Exception("Batch.GetBatchSubmissionDetails returned False")
            End If

            If dsData.Tables.Count <> 0 Then
                dsData.Tables(HistopathologyLib.clsBatch.BATCH_TISSUES_TABLE).TableName = "BatchTissues"

                dsData.Relations.Add("BATCH_BATCHSUBMISSION", _
                                        dsData.Tables(HistopathologyLib.clsBatch.BATCH_TABLE).Columns("ID"), _
                                        dsData.Tables(HistopathologyLib.clsBatch.BATCH_SUBMISSION_TABLE).Columns("BatchID"), False)

                dsData.Relations.Add("BATCHSUBMISSION_BATCHTISSUES", _
                                        dsData.Tables(HistopathologyLib.clsBatch.BATCH_SUBMISSION_TABLE).Columns("ID"), _
                                        dsData.Tables(HistopathologyLib.clsBatch.BATCH_TISSUES_TABLE).Columns("BatchSubmissionID"), False)

                dsData.Relations.Add("BATCHSUBMISSION_ANIMAL", _
                                        dsData.Tables(HistopathologyLib.clsBatch.BATCH_SUBMISSION_TABLE).Columns("AnimalID"), _
                                        dsData.Tables(HistopathologyLib.clsBatch.BATCH_ANIMAL_TABLE).Columns("ID"), False)

                dsData.Relations.Add("ANIMAL_BATCHSUBMISSION", _
                        dsData.Tables(HistopathologyLib.clsBatch.BATCH_ANIMAL_TABLE).Columns("ID"), _
                        dsData.Tables(HistopathologyLib.clsBatch.BATCH_SUBMISSION_TABLE).Columns("AnimalID"), False)

                dsData.Tables(HistopathologyLib.clsBatch.BATCH_ANIMAL_TABLE).Columns.Add("RowState", System.Type.GetType("System.Int32"))
                dsData.Tables(HistopathologyLib.clsBatch.BATCH_ANIMAL_TABLE).Columns.Add("HistoRefSet", System.Type.GetType("System.Boolean"))
                dsData.Tables(HistopathologyLib.clsBatch.BATCH_ANIMAL_TABLE).Columns.Add("PMDateSet", System.Type.GetType("System.Boolean"))
                dsData.Tables(HistopathologyLib.clsBatch.BATCH_ANIMAL_TABLE).Columns.Add("IsPGNumber", System.Type.GetType("System.Boolean"))
                dsData.Tables(HistopathologyLib.clsBatch.BATCH_ANIMAL_TABLE).Columns.Add("BookedHistologyRef", System.Type.GetType("System.Boolean"))

                For Each dr In dsData.Tables(HistopathologyLib.clsBatch.BATCH_ANIMAL_TABLE).Rows
                    dr("RowState") = DataRowState.Unchanged
                    If Not IsDBNull(dr("HistologyRef")) And Not dr("HistologyRef").ToString() = "" Then
                        dr("HistoRefSet") = True
                    Else
                        dr("HistoRefSet") = False
                    End If
                    If Not IsDBNull(dr("PMDate")) And Not dr("PMDate").ToString() = "" Then
                        dr("PMDateSet") = True
                    Else
                        dr("PMDateSet") = False
                    End If
                    dr("IsPGNumber") = False
                    dr("BookedHistologyRef") = False
                Next

                If sBatchSessionObject <> "" Then
                    objSession.Item(sBatchSessionObject) = dsData
                Else
                    objSession.Item(SessionVars.SV_BatchDetails) = dsData
                End If
                If sBatchSessionObject <> "" Then
                    objSession.Item(sBatchSessionObject) = dsData
                Else
                    objSession.Item(SessionVars.SV_BatchDetails) = dsData
                    objSession.Item(SessionVars.SV_BatchID) = iBatchID
                End If
            Else
                Throw New ApplicationException("Batch.GetBatchBlockDetails returned no tables")
            End If
        Catch ex As Exception
            clsAppError.DisplayError("Failed to Get Batch Details.", ex)
        End Try
    End Sub

    Public Sub PromptBeforeSaveScript(ByVal sMessage As String, ByRef ctlButton As System.Web.UI.WebControls.Button)
        ctlButton.Attributes.Add("OnClick", _
                             "javascript:return confirm('" & sMessage & "');")
    End Sub


#End Region

#Region "Sort Classes"

    Public Class CustomBlockRefAscSort : Implements IComparer
        Function Compare(ByVal Obj1 As Object, ByVal Obj2 As Object) As Integer Implements IComparer.Compare
            ' Take the two objects and convert into strings 
            Dim drRow1 As DataRow = CType(Obj1, DataRow)
            Dim drRow2 As DataRow = CType(Obj2, DataRow)

            Dim iBlockRef1 As Integer = 0
            Dim iBlockRef2 As Integer = 0

            If Not IsDBNull(drRow1("BlockRef")) Then
                iBlockRef1 = CInt(drRow1("BlockRef"))
            End If

            If Not IsDBNull(drRow2("BlockRef")) Then
                iBlockRef2 = CInt(drRow2("BlockRef"))
            End If

            If (iBlockRef1 > iBlockRef2) Then
                Return 1
            End If

            If (iBlockRef1 < iBlockRef2) Then
                Return -1
            Else
                Return 0
            End If

        End Function
    End Class

    Public Class CustomTissuesAscSort : Implements IComparer
        Function Compare(ByVal Obj1 As Object, ByVal Obj2 As Object) As Integer Implements IComparer.Compare
            ' Take the two objects and convert into strings 
            Dim drRow1 As DataRow = CType(Obj1, DataRow)
            Dim drRow2 As DataRow = CType(Obj2, DataRow)

            Dim sTissueCode1 As String = drRow1("TissueCode").ToString()
            Dim sTissueCode2 As String = drRow2("TissueCode").ToString()

            Return String.Compare(sTissueCode1, sTissueCode2)

        End Function
    End Class

    Public Class CustomerSenderRefAscSort : Implements IComparer
        Function Compare(ByVal Obj1 As Object, ByVal Obj2 As Object) As Integer Implements IComparer.Compare
            Dim drRow1 As DataRow = CType(Obj1, DataRow)
            Dim drRow2 As DataRow = CType(Obj2, DataRow)

            Dim sSenderRef1 As String = drRow1("SenderRef").ToString()
            Dim sSenderRef2 As String = drRow2("SenderRef").ToString()

            Return String.Compare(sSenderRef1, sSenderRef2)

        End Function
    End Class

    Public Class HistologyRefAscSort : Implements IComparer
        Function Compare(ByVal Obj1 As Object, ByVal Obj2 As Object) As Integer Implements IComparer.Compare
            Dim drRow1 As DataRow = CType(Obj1, DataRow)
            Dim drRow2 As DataRow = CType(Obj2, DataRow)

            Dim sHistologyRef1 As String = drRow1("HistologyRef").ToString()
            Dim sHistologyRef2 As String = drRow2("HistologyRef").ToString()
            Dim sSenderRef1 As String = drRow1("SenderRef").ToString()
            Dim sSenderRef2 As String = drRow2("SenderRef").ToString()
            Dim iCompareResult As Integer
            Dim bBlankvalue As Boolean = False
            iCompareResult = String.Compare(sHistologyRef1, sHistologyRef2)

            If sHistologyRef1 = "" Or sHistologyRef2 = "" Then
                bBlankvalue = True
            End If

            If iCompareResult = -1 Then
                If bBlankvalue Then
                    Return 1
                Else
                    Return -1
                End If
            ElseIf iCompareResult = 1 Then
                If bBlankvalue Then
                    Return -1
                Else
                    Return 1
                End If
            Else
                If sHistologyRef1 = "" And sHistologyRef2 = "" Then
                    iCompareResult = String.Compare(sSenderRef1, sSenderRef2)

                    If iCompareResult = -1 Then
                        Return -1
                    ElseIf iCompareResult = 1 Then
                        Return 1
                    Else
                        Return 0
                    End If
                Else
                    Return 0
                End If
            End If
        End Function
    End Class

#End Region

#Region "BSE System Functions (Unused)"
    'Public Function FormatRBSE(ByVal sRBSE As String) As String
    '    If sRBSE <> "" Then
    '        sRBSE = Replace(sRBSE, "/", "")
    '        sRBSE = Left$(sRBSE, 2) & "/" & Mid$(sRBSE, 3, 2) & "/" & Mid$(sRBSE, 5, 5)
    '    End If
    '    Return sRBSE
    'End Function

    'Public Function FormatDBSE(ByVal sDBSE As String) As String
    '    If sDBSE <> "" Then
    '        sDBSE = Replace(sDBSE, "/", "")
    '        sDBSE = Left$(sDBSE, 2) & "/" & Mid$(sDBSE, 3, 5)
    '    End If
    '    Return sDBSE
    'End Function

    'Public Function FormatCPHH(ByVal sCPHH As String) As String
    '    If Len(sCPHH) > 9 Then
    '        sCPHH = Left$(sCPHH, 2) & "/" & Mid$(sCPHH, 3, 3) & "/" & Mid$(sCPHH, 6, 4) & "/" & Mid$(sCPHH, 10, (Len(sCPHH) - 9))
    '    Else
    '        sCPHH = Left$(sCPHH, 2) & "/" & Mid$(sCPHH, 3, 3) & "/" & Mid$(sCPHH, 6, 4)
    '    End If

    '    Return sCPHH
    'End Function

    'Public Sub GetBatchNumbersFromDatabase(ByVal sRBSE As String, ByRef objSession As System.Web.SessionState.HttpSessionState)
    '    Dim objCase As New HistopathologyLib.clsCase()
    '    Dim dtData As DataTable

    '    sRBSE = Replace(sRBSE, "/", "")

    '    If Not (objCase.GetBatchNumberByRBSE(sRBSE, dtData)) Then
    '        Throw New Exception("Case.GetBatchNumberByRBSE returned False")
    '    End If

    '    objSession.Item(SessionVars.SV_BatchNumbersTable) = dtData
    'End Sub

    'Public Sub GetCaseDetailsFromDatabase(ByVal sRBSE As String, ByRef objSession As System.Web.SessionState.HttpSessionState)
    '    Dim objCase As New BSELib.clsCase()
    '    Dim dsData As DataSet

    '    Try
    '        sRBSE = Replace(sRBSE, "/", "")
    '        If (sRBSE <> "") Then
    '            If Not (objCase.GetCaseDetails(sRBSE, dsData)) Then
    '                Throw New Exception("Case.GetCaseDetails returned False")
    '            End If

    '            If dsData.Tables.Count <> 0 Then
    '                If dsData.Tables(BSELib.clsCase.CASE_TABLE).Rows.Count <> 0 Then
    '                    With objSession
    '                        .Item(SessionVars.SV_CPHHNumber) = dsData.Tables(BSELib.clsCase.CASE_TABLE).Rows(0)("CPHH").ToString()

    '                        .Item(SessionVars.SV_CaseDetails) = dsData
    '                        .Item(SessionVars.SV_FeedTable) = dsData.Tables(BSELib.clsCase.FEED_TABLE)
    '                        .Item(SessionVars.SV_OtherOwnerTable) = dsData.Tables(BSELib.clsCase.OTHER_OWNER_TABLE)
    '                        .Item(SessionVars.SV_ClinicalVisitTable) = dsData.Tables(BSELib.clsCase.CLINICAL_VISIT_TABLE)
    '                    End With
    '                Else
    '                    With objSession
    '                        .Item(SessionVars.SV_CPHHNumber) = ""
    '                        ' Add row to each table
    '                        AddEmptyRow(dsData.Tables(BSELib.clsCase.CASE_TABLE), sRBSE)
    '                        'AddEmptyRow(dsData.Tables(CLINICAL_TABLE), sRBSE)
    '                        'AddEmtpyRow(dsData.Tables(BAB_TABLE), sRBSE)
    '                        'Put the tables into the Session Object
    '                        .Item(SessionVars.SV_CaseDetails) = dsData
    '                        .Item(SessionVars.SV_FeedTable) = dsData.Tables(BSELib.clsCase.FEED_TABLE)
    '                        .Item(SessionVars.SV_OtherOwnerTable) = dsData.Tables(BSELib.clsCase.OTHER_OWNER_TABLE)
    '                        .Item(SessionVars.SV_ClinicalVisitTable) = dsData.Tables(BSELib.clsCase.CLINICAL_VISIT_TABLE)
    '                    End With
    '                End If

    '                If dsData.Tables(BSELib.clsCase.DAM_DETAILS_TABLE).Rows.Count = 0 Then
    '                    AddEmptyRow(dsData.Tables(BSELib.clsCase.DAM_DETAILS_TABLE), "", True)
    '                End If
    '                objSession.Item(SessionVars.SV_DamDetailsTable) = dsData.Tables(BSELib.clsCase.DAM_DETAILS_TABLE)

    '                If dsData.Tables(BSELib.clsCase.SIRE_DETAILS_TABLE).Rows.Count = 0 Then
    '                    AddEmptyRow(dsData.Tables(BSELib.clsCase.SIRE_DETAILS_TABLE), "", True)
    '                End If
    '                objSession.Item(SessionVars.SV_SireDetailsTable) = dsData.Tables(BSELib.clsCase.SIRE_DETAILS_TABLE)

    '                objSession.Item(SessionVars.SV_RelationsTable) = dsData.Tables(BSELib.clsCase.RELATION_TABLE)
    '            Else
    '                Throw New ApplicationException("Case.GetCaseDetails returned no tables")
    '            End If
    '        End If
    '    Catch ex As Exception
    '        clsAppError.DisplayError("Failed to Get Case Details.", ex)
    '    End Try
    'End Sub

    'Public Sub AddEmptyRow(ByRef dtData As DataTable, ByVal sRBSE As String, Optional ByVal bAcceptChanges As Boolean = False)
    '    Dim drRow As DataRow

    '    drRow = dtData.NewRow()
    '    drRow.Item("RBSE") = sRBSE
    '    dtData.Rows.Add(drRow)
    '    If bAcceptChanges Then
    '        dtData.AcceptChanges()
    '    End If
    'End Sub

    'Public Sub RemoveCaseFromSession(ByRef objSession As System.Web.SessionState.HttpSessionState)

    '    With objSession
    '        .Remove(SessionVars.SV_RBSENumber)
    '        .Remove(SessionVars.SV_CPHHNumber)
    '        .Remove(SessionVars.SV_FarmDetails)
    '        .Remove(SessionVars.SV_RelatedFarmsTable)
    '        .Remove(SessionVars.SV_RelatedFarmsView)
    '        .Remove(SessionVars.SV_HerdSizeTable)
    '        .Remove(SessionVars.SV_HerdSizeView)
    '        .Remove(SessionVars.SV_CaseDetails)
    '        .Remove(SessionVars.SV_FeedTable)
    '        .Remove(SessionVars.SV_FeedView)
    '        .Remove(SessionVars.SV_OtherOwnerTable)
    '        .Remove(SessionVars.SV_OtherOwnerView)
    '        .Remove(SessionVars.SV_ClinicalVisitTable)
    '        .Remove(SessionVars.SV_ClinicalVisitView)
    '        .Remove(SessionVars.SV_DamDetailsTable)
    '        .Remove(SessionVars.SV_SireDetailsTable)
    '        .Remove(SessionVars.SV_RelationsTable)
    '        .Remove(SessionVars.SV_RelationsView)
    '    End With

    'End Sub

    'Public Sub GetFarmDetailsFromDatabase(ByVal sCPHH As String, ByRef objSession As System.Web.SessionState.HttpSessionState)
    '    Dim objFarm As New BSELib.clsFarm()
    '    Dim dsData As DataSet

    '    Try
    '        If (sCPHH <> "") Then
    '            If Not (objFarm.GetFarmDetails(Replace(sCPHH, "/", ""), dsData)) Then
    '                Throw New Exception("Case.GetFarmDetails returned False")
    '            End If
    '            objSession.Item(SessionVars.SV_FarmDetails) = dsData
    '            objSession.Item(SessionVars.SV_RelatedFarmsTable) = dsData.Tables(BSELib.clsFarm.RELATED_FARMS_TABLE)
    '            objSession.Item(SessionVars.SV_HerdSizeTable) = dsData.Tables(BSELib.clsFarm.HERDSIZE_TABLE)
    '        End If
    '    Catch ex As Exception
    '        clsAppError.DisplayError("Failed to 'Get Farm Details'.", ex)
    '    End Try
    'End Sub

    'Public Function IsVLAAllowedMainCaseEdit(ByRef objSession As System.Web.SessionState.HttpSessionState) As Boolean

    '    IsVLAAllowedMainCaseEdit = (CStr(objSession(SessionVars.SV_BatchNumber)) <> "" OrElse CType(objSession(SessionVars.SV_BatchNumbersTable), DataTable).Rows.Count > 0)

    'End Function

    'Public Function IsVLAAllowedAdditionalCaseEdit(ByRef objSession As System.Web.SessionState.HttpSessionState) As Boolean

    '    Dim dtBatch As DataTable = CType(objSession(SessionVars.SV_BatchNumbersTable), DataTable)
    '    IsVLAAllowedAdditionalCaseEdit = CStr(objSession(SessionVars.SV_BatchNumber)) = "" OrElse (dtBatch.Rows.Count > 0 AndAlso dtBatch.Select("BatchNumber = '" & CStr(objSession(SessionVars.SV_BatchNumber)) & "'").GetLength(0) = 0)

    'End Function

    'Public Function SplitBatchNumber(ByVal sHoleBatchNumber As String, _
    '                                 ByRef sBatchYear As String, _
    '                                 ByRef sBatchNumber As String)

    '    sHoleBatchNumber = Replace(sHoleBatchNumber, "/", "")
    '    sBatchYear = Left$(sHoleBatchNumber, 4)
    '    sBatchNumber = Mid$(sHoleBatchNumber, 5, (Len(sHoleBatchNumber) - 4))

    'End Function
#End Region

#Region "TB System Functions (unused)"

    ' Send down Javascript to set control button as default
    'Public Sub SetDefaultButton(ByVal ctl As Button)

    '    ctl.Page.RegisterHiddenField("__EVENTTARGET", ctl.UniqueID.ToString())

    'End Sub

    'Public Function ChangeFocusAfterCharacterEntry(ByRef ctlCurrentTextbox As TextBox, ByRef ctlNextTextbox As TextBox, ByRef numberOfChars As Long) As Boolean

    '    If HttpContext.Current.Request.Browser.JavaScript Then
    '        Dim scr As New System.Text.StringBuilder()

    '        scr.Append("<SCRIPT language=""javascript"">" + vbNewLine)
    '        scr.Append("function OnKeyPress(currentControl, nextControl)" + vbNewLine + "{" + vbNewLine)
    '        scr.Append(" if (currentControl.value.length == " + CStr(numberOfChars) + ")" + vbNewLine)
    '        scr.Append(" { " + vbNewLine)
    '        scr.Append(" nextControl.setfocus()" + vbNewLine)
    '        scr.Append(" } " + vbNewLine)
    '        scr.Append("}" + vbNewLine)
    '        scr.Append("</SCRIPT>" + vbNewLine)

    '        ctlCurrentTextbox.Attributes.Add("onkeypress", "OnKeyPress(document.all('" & ctlCurrentTextbox.ClientID & "'), document.all('" & ctlNextTextbox.ClientID & "'))")
    '        'ctlButton.Page.RegisterStartupScript("TextboxDefaultButton", scr.ToString())
    '        Return True
    '    Else
    '        Return False
    '    End If

    'End Function

    ' returns "VL" if the passed object has the value True; "NVL" if
    ' the passed object has the value False; or "" if the passed object
    ' is Null or a type that can't be evaluated as a boolean.
    'Public Function ToVLString(ByVal objValue As Object) As String
    '    Try
    '        If objValue Is Nothing _
    '        OrElse IsDBNull(objValue) Then
    '            Return ""
    '        Else
    '            If CBool(objValue) Then
    '                Return "VL"
    '            Else
    '                Return "NVL"
    '            End If
    '        End If
    '    Catch ex As Exception
    '        Return ""
    '    End Try
    'End Function

#End Region


End Module
