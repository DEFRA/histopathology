<%@ Control Language="vb" AutoEventWireup="false" Codebehind="Batch.ascx.vb" Inherits="HistopathologySystem.Batch" TargetSchema="http://schemas.microsoft.com/intellisense/ie5" %>
<%@ Register TagPrefix="uc1" TagName="CalendarDate" Src="CalendarDate.ascx" %>
<HEAD>
</HEAD>
<LINK href="Style/vla-ie.css" type="text/css" rel="stylesheet">
<DIV style="WIDTH: 744px; POSITION: relative; HEIGHT: 133px" ms_positioning="GridLayout">
	<asp:label id="lblContactName" style="Z-INDEX: 103; LEFT: 17px; POSITION: absolute; TOP: 98px" Height="18px" runat="server">Pathologist:</asp:label>
	<asp:label id="lblSpecies" style="Z-INDEX: 104; LEFT: 411px; POSITION: absolute; TOP: 98px" Height="18px" runat="server">Species:</asp:label>
	<asp:label id="lblSubmissionDate" style="Z-INDEX: 105; LEFT: 411px; POSITION: absolute; TOP: 69px" Height="18px" runat="server">Submission Date:</asp:label>
	<asp:label id="lblSubmittedBy" style="Z-INDEX: 106; LEFT: 17px; POSITION: absolute; TOP: 41px" runat="server">Submitted By:</asp:label>
	<asp:label id="lblSubmittedArea" style="Z-INDEX: 107; LEFT: 411px; POSITION: absolute; TOP: 41px" runat="server">Submitted Area:</asp:label>
	<HR style="Z-INDEX: 101; LEFT: 18px; POSITION: absolute; TOP: 123px; HEIGHT: 1px" width="720" SIZE="1">
	<asp:label id="lblProjectCode" style="Z-INDEX: 102; LEFT: 17px; POSITION: absolute; TOP: 69px" Height="18px" runat="server">Project or Contract code:</asp:label>
	<asp:Label id="lblEnteredBy" style="Z-INDEX: 108; LEFT: 17px; POSITION: absolute; TOP: 13px" runat="server">Entered By:</asp:Label>
	<asp:Label id="lblEnteredArea" style="Z-INDEX: 109; LEFT: 411px; POSITION: absolute; TOP: 13px" runat="server">Entered Area:</asp:Label>
	<asp:Label id="lblEnteredByVal" style="Z-INDEX: 111; LEFT: 184px; POSITION: absolute; TOP: 13px" runat="server"></asp:Label>
	<asp:Label id="lblSubmittedByVal" style="Z-INDEX: 112; LEFT: 184px; POSITION: absolute; TOP: 41px" runat="server"></asp:Label>
	<asp:Label id="lblProjectCodeVal" style="Z-INDEX: 113; LEFT: 184px; POSITION: absolute; TOP: 69px" runat="server"></asp:Label>
	<asp:Label id="lblContactNameVal" style="Z-INDEX: 114; LEFT: 184px; POSITION: absolute; TOP: 98px" runat="server"></asp:Label>
	<asp:Label id="lblSpeciesVal" style="Z-INDEX: 115; LEFT: 558px; POSITION: absolute; TOP: 98px" runat="server"></asp:Label>
	<asp:Label id="lblEnteredAreaVal" style="Z-INDEX: 116; LEFT: 558px; POSITION: absolute; TOP: 13px" runat="server"></asp:Label>
	<asp:Label id="lblSubmittedAreaVal" style="Z-INDEX: 117; LEFT: 558px; POSITION: absolute; TOP: 41px" runat="server"></asp:Label>
	<asp:Label id="lblSubmissionDateVal" style="Z-INDEX: 118; LEFT: 558px; POSITION: absolute; TOP: 69px" runat="server"></asp:Label></DIV>
