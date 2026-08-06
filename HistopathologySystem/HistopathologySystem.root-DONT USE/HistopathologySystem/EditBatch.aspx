<%@ Register TagPrefix="uc1" TagName="CalendarDate" Src="CalendarDate.ascx" %>
<%@ Register TagPrefix="uc1" TagName="Batch" Src="Batch.ascx" %>
<%@ Page Language="vb" AutoEventWireup="false" Codebehind="EditBatch.aspx.vb" Inherits="HistopathologySystem.EditBatch" smartNavigation="True"%>
<%@ Register TagPrefix="uc1" TagName="DataGridPager" Src="DataGridPager.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAHeader" Src="VLAHeader.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAFooter" Src="VLAFooter.ascx" %>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<HTML>
	<HEAD>
		<title>ReceiveSubmission</title>
		<meta content="Microsoft Visual Studio.NET 7.0" name="GENERATOR">
		<meta content="Visual Basic 7.0" name="CODE_LANGUAGE">
		<meta content="JavaScript" name="vs_defaultClientScript">
		<meta content="http://schemas.microsoft.com/intellisense/ie5" name="vs_targetSchema">
		<LINK href="Style/vla-ie.css" type="text/css" rel="stylesheet">
	</HEAD>
	<body>
		<form id="Form1" method="post" runat="server">
			<uc1:vlaheader id="VLAHeader1" runat="server"></uc1:vlaheader>
			<DIV style="WIDTH: 100px"><uc1:batch id="Batch1" runat="server"></uc1:batch></DIV>
			<DIV style="WIDTH: 744px; POSITION: relative; HEIGHT: 234px" ms_positioning="GridLayout">
				<asp:dropdownlist id="ddlStatus" style="Z-INDEX: 103; LEFT: 184px; POSITION: absolute; TOP: 40px" runat="server" Height="22" Width="160" AutoPostBack="True"></asp:dropdownlist>
				<asp:textbox id="txtReason" style="Z-INDEX: 108; LEFT: 17px; POSITION: absolute; TOP: 94px" runat="server" Height="74px" Width="705px" TextMode="MultiLine"></asp:textbox>
				<asp:button id="btnEditSubmission" style="Z-INDEX: 105; LEFT: 12px; POSITION: absolute; TOP: 199px" runat="server" Height="24" Text="Edit Submission" CausesValidation="False" Width="137"></asp:button>
				<asp:button id="btnSamplesOnHold" style="Z-INDEX: 109; LEFT: 158px; POSITION: absolute; TOP: 199px" runat="server" Height="24px" Text="Put Samples On Hold" Width="155px"></asp:button>
				<asp:button id="btnSave" style="Z-INDEX: 102; LEFT: 605px; POSITION: absolute; TOP: 199px" runat="server" Height="24px" Text="Done" Width="113"></asp:button>
				<asp:button id="btnCancel" style="Z-INDEX: 104; LEFT: 486px; POSITION: absolute; TOP: 199px" runat="server" Height="24px" Text="Cancel" Width="113px" CausesValidation="False"></asp:button>
				<asp:label id="lblBatchStatus" style="Z-INDEX: 101; LEFT: 17px; POSITION: absolute; TOP: 40px" runat="server">Submission status</asp:label>
				<HR style="Z-INDEX: 106; LEFT: 18px; POSITION: absolute; TOP: 182px; HEIGHT: 1px" width="720" SIZE="1">
				<asp:label id="lblComment" style="Z-INDEX: 107; LEFT: 17px; POSITION: absolute; TOP: 70px" runat="server">Reason</asp:label>
				<asp:Label id="lblExplain" style="Z-INDEX: 110; LEFT: 17px; POSITION: absolute; TOP: 9px" runat="server">To receive the submission go to the receive submission option from the home screen.</asp:Label></DIV>
			<DIV id="ctlDIV" style="WIDTH: 744px; HEIGHT: 16px" runat="server" ms_positioning="FlowLayout"></DIV>
			<uc1:vlafooter id="VLAFooter1" runat="server"></uc1:vlafooter></form>
	</body>
</HTML>
