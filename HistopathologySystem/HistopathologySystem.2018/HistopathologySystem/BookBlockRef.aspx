<%@ Register TagPrefix="uc1" TagName="VLAFooter" Src="VLAFooter.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAHeader" Src="VLAHeader.ascx" %>
<%@ Page Language="vb" AutoEventWireup="false" Codebehind="BookBlockRef.aspx.vb" Inherits="HistopathologySystem.BookBlockRef"%>
<%@ Register TagPrefix="uc1" TagName="SenderRef" Src="SenderRef.ascx" %>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<HTML>
	<HEAD>
		<title>BookBlockRef</title>
		<meta content="Microsoft Visual Studio .NET 7.1" name="GENERATOR">
		<meta content="Visual Basic .NET 7.1" name="CODE_LANGUAGE">
		<meta content="JavaScript" name="vs_defaultClientScript">
		<meta content="http://schemas.microsoft.com/intellisense/ie5" name="vs_targetSchema">
		<LINK href="Style/vla-ie.css" type="text/css" rel="stylesheet">
	</HEAD>
	<body>
		<form id="Form1" method="post" runat="server">
			<uc1:vlaheader id="VLAHeader1" runat="server"></uc1:vlaheader>
			<DIV style="WIDTH: 744px; POSITION: relative; HEIGHT: 72px" ms_positioning="GridLayout">
				<HR style="Z-INDEX: 101; LEFT: 8px; WIDTH: 98.04%; POSITION: absolute; TOP: 64px; HEIGHT: 1px"
					width="98.04%" SIZE="1">
				<asp:Label id="lblText" style="Z-INDEX: 102; LEFT: 16px; POSITION: absolute; TOP: 8px" runat="server">Use the following controls to book blocks for Sender Ref's. Note that only PG numbers or MC numbers can be used when booking a range of Sender Ref's i.e. Both Sender Ref from and Sender Ref to must be MC or PG numbers. If an alternative range is used, only the first Sender Ref in the range will have blocks booked.</asp:Label></DIV>
			<DIV style="WIDTH: 728px; POSITION: relative; HEIGHT: 144px" ms_positioning="GridLayout">
				<DIV style="Z-INDEX: 101; LEFT: 120px; WIDTH: 192px; POSITION: absolute; TOP: 8px; HEIGHT: 32px"
					ms_positioning="FlowLayout"><uc1:senderref id="SenderRefFrom" runat="server"></uc1:senderref></DIV>
				<DIV style="Z-INDEX: 102; LEFT: 416px; WIDTH: 176px; POSITION: absolute; TOP: 8px; HEIGHT: 32px"
					ms_positioning="FlowLayout"><uc1:senderref id="SenderRefTo" runat="server"></uc1:senderref></DIV>
				<asp:textbox id="txtBlockRefFrom" style="Z-INDEX: 104; LEFT: 120px; POSITION: absolute; TOP: 48px"
					runat="server" Width="56px" MaxLength="3"></asp:textbox><asp:textbox id="txtBlockRefTo" style="Z-INDEX: 103; LEFT: 416px; POSITION: absolute; TOP: 48px"
					runat="server" Width="56px" MaxLength="3"></asp:textbox><asp:button id="btnOk" style="Z-INDEX: 105; LEFT: 16px; POSITION: absolute; TOP: 88px" runat="server"
					Width="81" Text="Book" Height="22"></asp:button><asp:customvalidator id="revBlockRefFrom" style="Z-INDEX: 106; LEFT: 176px; POSITION: absolute; TOP: 48px"
					runat="server" ClientValidationFunction="ClientValidateBlockRef" OnServerValidate="ValidateBlockRefRef" CssClass="ValidatorText" ControlToValidate="txtBlockRefFrom"
					ToolTip="Enter a value between 1 and 999. The Block Ref must be at least two digits long. Valid ranges are therefore 01-99 and 100-999. Note, for > 01-99 no further leading zero should be entered. For example 001 is invalid, enter it as 01.">*</asp:customvalidator><asp:requiredfieldvalidator id="rfvBlockRef" style="Z-INDEX: 107; LEFT: 176px; POSITION: absolute; TOP: 48px"
					runat="server" CssClass="ValidatorText" ControlToValidate="txtBlockRefFrom" ToolTip="Required Field">*</asp:requiredfieldvalidator><asp:customvalidator id="revBlockRefTo" style="Z-INDEX: 108; LEFT: 472px; POSITION: absolute; TOP: 48px"
					runat="server" ClientValidationFunction="ClientValidateBlockRef" OnServerValidate="ValidateBlockRefRef" CssClass="ValidatorText" ControlToValidate="txtBlockRefTo" ToolTip="Enter a value between 1 and 999. The Block Ref must be at least two digits long. Valid ranges are therefore 01-99 and 100-999. Note, for > 01-99 no further leading zero should be entered. For example 001 is invalid, enter it as 01.">*</asp:customvalidator><asp:label id="lblBlockRefFrom" style="Z-INDEX: 109; LEFT: 16px; POSITION: absolute; TOP: 48px"
					runat="server">Block Ref from</asp:label><asp:label id="lblBlockRefTo" style="Z-INDEX: 110; LEFT: 328px; POSITION: absolute; TOP: 48px"
					runat="server">Block Ref to</asp:label><asp:label id="lblSenderRefTo" style="Z-INDEX: 112; LEFT: 328px; POSITION: absolute; TOP: 8px"
					runat="server">Sender Ref to</asp:label><asp:label id="lblSenderRefFrom" style="Z-INDEX: 113; LEFT: 16px; POSITION: absolute; TOP: 8px"
					runat="server">Sender Ref from</asp:label>
				<HR style="Z-INDEX: 114; LEFT: 16px; WIDTH: 96.84%; POSITION: absolute; TOP: 80px; HEIGHT: 1px"
					width="96.84%" SIZE="1">
				<asp:button id="btnBack" style="Z-INDEX: 115; LEFT: 624px; POSITION: absolute; TOP: 88px" runat="server"
					Width="78px" Text="Done" CausesValidation="False"></asp:button>
				<asp:LinkButton id="lbSearchBlockRefs" style="Z-INDEX: 116; LEFT: 16px; POSITION: absolute; TOP: 120px"
					runat="server" CausesValidation="False">Check Used Block Refs</asp:LinkButton></DIV>
			<DIV id="ctlDiv" style="WIDTH: 672px; HEIGHT: 16px" runat="server" ms_positioning="FlowLayout"></DIV>
			<asp:label id="lblError" runat="server" Width="592px" ForeColor="Red"></asp:label><p>
				<uc1:vlafooter id="VLAFooter1" runat="server"></uc1:vlafooter></p>
		</form>
	</body>
</HTML>
