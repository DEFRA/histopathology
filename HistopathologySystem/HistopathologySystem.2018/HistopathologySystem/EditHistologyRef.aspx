<%@ Page Language="vb" AutoEventWireup="false" Codebehind="EditHistologyRef.aspx.vb" Inherits="HistopathologySystem.EditHistologyRef" %>
<%@ Register TagPrefix="uc1" TagName="SenderRef" Src="SenderRef.ascx" %>
<%@ Register TagPrefix="uc1" TagName="HistologyRef" Src="HistologyRef.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAHeader" Src="VLAHeader.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAFooter" Src="VLAFooter.ascx" %>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<HTML>
	<HEAD>
		<title>EditHistologyRef</title>
		<meta name="vs_snapToGrid" content="False">
		<meta content="Microsoft Visual Studio .NET 7.1" name="GENERATOR">
		<meta content="Visual Basic .NET 7.1" name="CODE_LANGUAGE">
		<meta content="JavaScript" name="vs_defaultClientScript">
		<meta content="http://schemas.microsoft.com/intellisense/ie5" name="vs_targetSchema">
		<LINK href="Style/vla-ie.css" type="text/css" rel="stylesheet">
	</HEAD>
	<body>
		<form id="Form1" method="post" runat="server">
			<uc1:vlaheader id="VLAHeader1" runat="server"></uc1:vlaheader>
			<DIV style="WIDTH: 776px; POSITION: relative; HEIGHT: 259px" ms_positioning="GridLayout"><asp:label id="lblSampleSenderRef" style="Z-INDEX: 101; LEFT: 16px; POSITION: absolute; TOP: 72px"
					runat="server" Width="160px"> Sender Ref</asp:label><asp:label id="lblNewSenderRef" style="Z-INDEX: 102; LEFT: 16px; POSITION: absolute; TOP: 132px"
					runat="server" Width="136px">New Sender Ref</asp:label><asp:button id="cmdEditSenderRef" style="Z-INDEX: 103; LEFT: 344px; POSITION: absolute; TOP: 132px"
					runat="server" Width="120px" CausesValidation="False" Text="Save Sender Ref" ToolTip="Save Sample Ref. Button" tabIndex="3"></asp:button><asp:label id="lblHistologyReference" style="Z-INDEX: 104; LEFT: 16px; POSITION: absolute; TOP: 164px"
					runat="server" Width="160px">New Histology Ref</asp:label>
				<asp:button id="cmdSaveHistologyRef" style="Z-INDEX: 105; LEFT: 344px; POSITION: absolute; TOP: 164px"
					runat="server" Width="120px" CausesValidation="False" Text="Save Histology Ref" ToolTip="Save Histology Ref.Button"
					tabIndex="5"></asp:button>
				<asp:label id="lblMessage" style="Z-INDEX: 106; LEFT: 16px; POSITION: absolute; TOP: 228px"
					runat="server" Width="544px" ForeColor="Red"></asp:label><asp:button id="cmdDone" style="Z-INDEX: 107; LEFT: 584px; POSITION: absolute; TOP: 224px" runat="server"
					Width="60px" CausesValidation="False" Text="Done" tabIndex="6"></asp:button>
				<DIV style="Z-INDEX: 108; LEFT: 152px; WIDTH: 176px; POSITION: absolute; TOP: 72px; HEIGHT: 32px"
					ms_positioning="FlowLayout">
					<uc1:senderref id="txtOriginalSenderRef" runat="server" EnableViewState="False" tabIndex="0"></uc1:senderref></DIV>
				<DIV style="Z-INDEX: 109; LEFT: 152px; WIDTH: 176px; POSITION: absolute; TOP: 132px; HEIGHT: 32px"
					ms_positioning="FlowLayout">
					<uc1:senderref id="txtNewSenderRef" runat="server" tabIndex="2"></uc1:senderref></DIV>
				<DIV style="Z-INDEX: 110; LEFT: 152px; WIDTH: 176px; POSITION: absolute; TOP: 164px; HEIGHT: 32px"
					ms_positioning="FlowLayout">
					<uc1:histologyref id="txtNewHistologyRef" runat="server" tabIndex="4"></uc1:histologyref></DIV>
				<HR style="Z-INDEX: 111; LEFT: 8px; WIDTH: 99.07%; POSITION: absolute; TOP: 216px; HEIGHT: 1px"
					width="99.07%" SIZE="1">
				<asp:TextBox id="txtOldHistologyRef" style="Z-INDEX: 112; LEFT: 152px; POSITION: absolute; TOP: 102px"
					runat="server" Width="160px" ReadOnly="True" Enabled="False"></asp:TextBox>
				<asp:Label id="lblOldHistologyRef" style="Z-INDEX: 113; LEFT: 16px; POSITION: absolute; TOP: 102px"
					runat="server">Original Histology Ref</asp:Label>
				<asp:Button id="cmdGetOldHistologyRef" style="Z-INDEX: 114; LEFT: 344px; POSITION: absolute; TOP: 102px"
					runat="server" Text="Get Histology Ref" CausesValidation="False" Width="120px" tabIndex="1"></asp:Button>
				<asp:Label id="Label1" style="Z-INDEX: 115; LEFT: 16px; POSITION: absolute; TOP: 8px" runat="server"
					Width="728px">This page allows you to change existing Sender or Histo refs. Please use this page with extreme care. When you change a Sender or Histo ref, all submissions using that reference will also be changed.</asp:Label>
				<HR style="Z-INDEX: 116; LEFT: 16px; WIDTH: 98.11%; POSITION: absolute; TOP: 48px; HEIGHT: 1px"
					width="98.11%" SIZE="1">
			</DIV>
			<uc1:vlafooter id="VLAFooter1" runat="server"></uc1:vlafooter></form>
	</body>
</HTML>
