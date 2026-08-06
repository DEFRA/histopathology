<%@ Register TagPrefix="uc1" TagName="VLAFooter" Src="VLAFooter.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAHeader" Src="VLAHeader.ascx" %>
<%@ Register TagPrefix="uc1" TagName="SenderRef" Src="SenderRef.ascx" %>
<%@ Page Language="vb" AutoEventWireup="false" Codebehind="Cassetted.aspx.vb" Inherits="HistopathologySystem.Cassetted"%>
<%@ Register TagPrefix="uc1" TagName="MouseNumber" Src="MouseNumber.ascx" %>
<%@ Register TagPrefix="uc1" TagName="HistologyRef" Src="HistologyRef.ascx" %>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<HTML>
	<HEAD>
		<title>Cassetted</title>
		<meta content="Microsoft Visual Studio.NET 7.0" name="GENERATOR">
		<meta content="Visual Basic 7.0" name="CODE_LANGUAGE">
		<meta content="JavaScript" name="vs_defaultClientScript">
		<meta content="http://schemas.microsoft.com/intellisense/ie5" name="vs_targetSchema">
		<LINK href="Style/vla-ie.css" type="text/css" rel="stylesheet">
	</HEAD>
	<body>
		<form id="Form1" method="post" runat="server">
			<uc1:vlaheader id="VLAHeader1" runat="server"></uc1:vlaheader>
			<DIV style="WIDTH: 710px; POSITION: relative; HEIGHT: 224px" ms_positioning="GridLayout"><asp:label id="lblCassetted" style="Z-INDEX: 101; LEFT: 16px; POSITION: absolute; TOP: 16px" runat="server" Height="24px" Width="624px">Select the submission type from the checkbox list and click ok. Note that only one submission type should be selected.</asp:label>
				<DIV style="Z-INDEX: 105; LEFT: 48px; WIDTH: 214px; POSITION: absolute; TOP: 64px; HEIGHT: 104px">
					<asp:checkboxlist id="chkblSubmittedAs" runat="server" Width="200px" AutoPostBack="True"></asp:checkboxlist></DIV>
				<asp:button id="btnYes" style="Z-INDEX: 104; LEFT: 536px; POSITION: absolute; TOP: 192px" runat="server" Height="22px" Width="90px" Text="Next"></asp:button>
				<asp:button id="btnCancel" style="Z-INDEX: 102; LEFT: 440px; POSITION: absolute; TOP: 192px" runat="server" Height="22px" Width="89px" Text="Cancel"></asp:button>
				<HR style="Z-INDEX: 103; LEFT: 8px; WIDTH: 93.99%; POSITION: absolute; TOP: 184px; HEIGHT: 1px" width="93.99%" SIZE="1">
				<asp:label id="lblError" style="Z-INDEX: 106; LEFT: 272px; POSITION: absolute; TOP: 64px" runat="server" Visible="False" ToolTip="Required Field" CssClass="ValidatorText">*</asp:label>
			</DIV>
				<DIV id="ctlDiv" style="WIDTH: 707px; HEIGHT: 13px" runat="server"></DIV>
				<uc1:vlafooter id="VLAFooter1" runat="server"></uc1:vlafooter>
		</form>
	</body>
</HTML>
