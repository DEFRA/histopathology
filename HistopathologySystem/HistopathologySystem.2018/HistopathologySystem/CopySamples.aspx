<%@ Register TagPrefix="uc1" TagName="VLAFooter" Src="VLAFooter.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAHeader" Src="VLAHeader.ascx" %>
<%@ Page Language="vb" AutoEventWireup="false" Codebehind="CopySamples.aspx.vb" Inherits="HistopathologySystem.CopySamples" smartNavigation="True" %>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<HTML>
	<HEAD>
		<title>CopySamples</title>
		<meta content="True" name="vs_snapToGrid">
		<meta content="Microsoft Visual Studio .NET 7.1" name="GENERATOR">
		<meta content="Visual Basic .NET 7.1" name="CODE_LANGUAGE">
		<meta content="JavaScript" name="vs_defaultClientScript">
		<meta content="http://schemas.microsoft.com/intellisense/ie5" name="vs_targetSchema">
		<LINK href="Style/vla-ie.css" type="text/css" rel="stylesheet">
	</HEAD>
	<body>
		<form id="Form1" method="post" runat="server">
			<uc1:vlaheader id="VLAHeader1" runat="server"></uc1:vlaheader>
			<DIV style="WIDTH: 760px; POSITION: relative; HEIGHT: 160px" ms_positioning="GridLayout">
				<asp:textbox id="txtSubmissionID" style="Z-INDEX: 103; LEFT: 176px; POSITION: absolute; TOP: 80px"
					runat="server" Width="86px"></asp:textbox>
				<asp:label id="lblEnter" style="Z-INDEX: 101; LEFT: 8px; POSITION: absolute; TOP: 80px" runat="server">Enter Submission Number:</asp:label><asp:regularexpressionvalidator id="revSubmissionID" style="Z-INDEX: 102; LEFT: 264px; POSITION: absolute; TOP: 80px"
					runat="server" CssClass="ValidatorText" ToolTip="Must be numeric" ControlToValidate="txtSubmissionID" ValidationExpression="^[1-9]+[0-9]*$">*</asp:regularexpressionvalidator><asp:requiredfieldvalidator id="rfvSubmissionID" style="Z-INDEX: 104; LEFT: 264px; POSITION: absolute; TOP: 80px"
					runat="server" CssClass="ValidatorText" ToolTip="Required Field" ControlToValidate="txtSubmissionID">*</asp:requiredfieldvalidator><asp:button id="btnGo" style="Z-INDEX: 105; LEFT: 288px; POSITION: absolute; TOP: 80px" runat="server"
					Width="46px" Text="Go" CausesValidation="False"></asp:button>
				<P></P>
				<HR style="Z-INDEX: 106; LEFT: 8px; POSITION: absolute; TOP: 112px; HEIGHT: 1px" width="97%"
					SIZE="1">
				<asp:dropdownlist id="ddlCopySampleFrom" style="Z-INDEX: 107; LEFT: 128px; POSITION: absolute; TOP: 128px"
					runat="server" Width="185"></asp:dropdownlist><asp:dropdownlist id="ddlCopySampleTo" style="Z-INDEX: 108; LEFT: 448px; POSITION: absolute; TOP: 128px"
					runat="server" Width="185px"></asp:dropdownlist><asp:label id="lblCopyFrom" style="Z-INDEX: 109; LEFT: 8px; POSITION: absolute; TOP: 128px"
					runat="server">Copy Blocks from</asp:label><asp:label id="lblCopyTo" style="Z-INDEX: 110; LEFT: 344px; POSITION: absolute; TOP: 128px"
					runat="server">Copy Blocks To</asp:label><asp:requiredfieldvalidator id="rfvCopySampleTo" style="Z-INDEX: 111; LEFT: 636px; POSITION: absolute; TOP: 128px"
					runat="server" CssClass="ValidatorText" ControlToValidate="ddlCopySampleTo">*</asp:requiredfieldvalidator><asp:requiredfieldvalidator id="rfvCopySampleFrom" style="Z-INDEX: 112; LEFT: 316px; POSITION: absolute; TOP: 128px"
					runat="server" CssClass="ValidatorText" ControlToValidate="ddlCopySampleFrom">*</asp:requiredfieldvalidator>
				<asp:Label id="lblDescription" style="Z-INDEX: 113; LEFT: 8px; POSITION: absolute; TOP: 16px"
					runat="server">Enter the Submission that contains the sample you want to copy the previous block assignment from and click Go. This will populate the Copy Blocks from and Copy Block to picklists. Select the sample that contains the block assignment, select the sample to copy the blocks to and Click the Select Blocks button. </asp:Label></DIV>
			<DIV style="WIDTH: 760px; POSITION: relative; HEIGHT: 57px" ms_positioning="GridLayout">
				<HR style="Z-INDEX: 101; LEFT: 8px; POSITION: absolute; TOP: 10px; HEIGHT: 1px" width="97%"
					SIZE="1">
				<asp:button id="btnSummary" style="Z-INDEX: 104; LEFT: 8px; POSITION: absolute; TOP: 16px" runat="server"
					Width="103px" Text="Summary" Height="25px" CausesValidation="False"></asp:button><asp:button id="btnCopyBatch" style="Z-INDEX: 103; LEFT: 638px; POSITION: absolute; TOP: 17px"
					runat="server" Width="102px" Text="Select Blocks" Height="25px" CausesValidation="False"></asp:button><asp:button id="btnCancel" style="Z-INDEX: 102; LEFT: 526px; POSITION: absolute; TOP: 17px"
					runat="server" Width="102px" Text="Cancel" Height="25px" CausesValidation="False"></asp:button></DIV>
			<DIV id="ctlDiv" style="WIDTH: 848px; HEIGHT: 8px" runat="server" ms_positioning="FlowLayout"></DIV>
		</form>
		<P><uc1:vlafooter id="VLAFooter1" runat="server"></uc1:vlafooter></P>
	</body>
</HTML>
