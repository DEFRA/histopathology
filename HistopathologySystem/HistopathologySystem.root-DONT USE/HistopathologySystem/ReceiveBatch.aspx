<%@ Register TagPrefix="uc1" TagName="CalendarDate" Src="CalendarDate.ascx" %>
<%@ Register TagPrefix="uc1" TagName="Batch" Src="Batch.ascx" %>
<%@ Page Language="vb" AutoEventWireup="false" Codebehind="ReceiveBatch.aspx.vb" Inherits="HistopathologySystem.ReceiveBatch" smartNavigation="True"%>
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
			<DIV style="WIDTH: 744px; POSITION: relative; HEIGHT: 344px" ms_positioning="GridLayout">
				<asp:dropdownlist id="ddlStatus" style="Z-INDEX: 106; LEFT: 184px; POSITION: absolute; TOP: 8px" runat="server" Height="22" Width="160" AutoPostBack="True"></asp:dropdownlist>
				<DIV style="Z-INDEX: 118; LEFT: 184px; WIDTH: 160px; POSITION: absolute; TOP: 40px; HEIGHT: 54px"><uc1:calendardate id="ctlBatchDate" runat="server"></uc1:calendardate></DIV>
				<asp:dropdownlist id="ddlReceivedBy" style="Z-INDEX: 114; LEFT: 184px; POSITION: absolute; TOP: 72px" runat="server" Width="161px"></asp:dropdownlist>
				<asp:dropdownlist id="ddlTimeReceived" style="Z-INDEX: 107; LEFT: 184px; POSITION: absolute; TOP: 104px" runat="server" Width="161"></asp:dropdownlist>
				<DIV style="Z-INDEX: 112; LEFT: 520px; WIDTH: 202px; POSITION: absolute; TOP: 8px; HEIGHT: 168px"><asp:checkboxlist id="chkblPostFixation" runat="server" Width="152px" AutoPostBack="True"></asp:checkboxlist><asp:textbox id="mtxtPostFixationOther" runat="server" Height="66px" Width="200px" TextMode="MultiLine" Enabled="False"></asp:textbox></DIV>
				<asp:textbox id="txtReason" style="Z-INDEX: 113; LEFT: 17px; POSITION: absolute; TOP: 200px" runat="server" Height="74px" Width="705px" TextMode="MultiLine"></asp:textbox>
				<asp:button id="btnEditSubmission" style="Z-INDEX: 109; LEFT: 21px; POSITION: absolute; TOP: 304px" runat="server" Text="Edit Submission" Height="24px" CausesValidation="False"></asp:button>
				<asp:button id="btnSave" style="Z-INDEX: 103; LEFT: 608px; POSITION: absolute; TOP: 304px" runat="server" Text="Done" Height="24px" Width="113"></asp:button>
				<asp:button id="btnCancel" style="Z-INDEX: 108; LEFT: 490px; POSITION: absolute; TOP: 304px" runat="server" Text="Cancel" Height="24px" Width="113px" CausesValidation="False"></asp:button>
				<asp:label id="lblBatchStatus" style="Z-INDEX: 101; LEFT: 13px; POSITION: absolute; TOP: 8px" runat="server">Submission status</asp:label><asp:label id="lblDateReceived" style="Z-INDEX: 102; LEFT: 13px; POSITION: absolute; TOP: 40px" runat="server">Date received/rejected</asp:label><asp:label id="lblReceivedBy" style="Z-INDEX: 104; LEFT: 13px; POSITION: absolute; TOP: 72px" runat="server">Received/Rejected by</asp:label><asp:label id="lblTimeReceived" style="Z-INDEX: 105; LEFT: 13px; POSITION: absolute; TOP: 104px" runat="server">Time received/rejected</asp:label>
				<HR style="Z-INDEX: 110; LEFT: 18px; POSITION: absolute; TOP: 288px; HEIGHT: 1px" width="720" SIZE="1">
				<asp:label id="lblComment" style="Z-INDEX: 111; LEFT: 17px; POSITION: absolute; TOP: 176px" runat="server">Reason</asp:label>
				<asp:label id="lblPostFixation" style="Z-INDEX: 117; LEFT: 411px; POSITION: absolute; TOP: 8px" runat="server">Post Fixations</asp:label><asp:label id="lblRepeat" style="Z-INDEX: 116; LEFT: 13px; POSITION: absolute; TOP: 138px" runat="server">This submission contains repeat blocks?</asp:label><asp:checkbox id="chkRepeatBlocks" style="Z-INDEX: 115; LEFT: 325px; POSITION: absolute; TOP: 138px" runat="server" tabIndex="-1"></asp:checkbox>
				<asp:Label id="lblErrorReceivedBy" style="Z-INDEX: 119; LEFT: 348px; POSITION: absolute; TOP: 72px" runat="server" ToolTip="Required Field" CssClass="ValidatorText" Visible="False">*</asp:Label>
				<asp:Label id="lblErrorTimeReceived" style="Z-INDEX: 120; LEFT: 348px; POSITION: absolute; TOP: 104px" runat="server" ToolTip="Required Field" CssClass="ValidatorText" Visible="False">*</asp:Label>
				<asp:Label id="lblErrorReason" style="Z-INDEX: 121; LEFT: 728px; POSITION: absolute; TOP: 200px" runat="server" ToolTip="Required Field" CssClass="ValidatorText" Visible="False">*</asp:Label></DIV>
			<DIV id="ctlDIV" style="WIDTH: 744px; HEIGHT: 16px" runat="server" ms_positioning="FlowLayout"></DIV>
			<uc1:vlafooter id="VLAFooter1" runat="server"></uc1:vlafooter></form>
	</body>
</HTML>
