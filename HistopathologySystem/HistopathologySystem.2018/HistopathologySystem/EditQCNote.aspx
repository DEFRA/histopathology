<%@ Page Language="vb" AutoEventWireup="false" Codebehind="EditQCNote.aspx.vb" Inherits="HistopathologySystem.EditQCNote" smartNavigation="True"%>
<%@ Register TagPrefix="uc1" TagName="VLAHeader" Src="VLAHeader.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAFooter" Src="VLAFooter.ascx" %>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<HTML>
	<HEAD>
		<title>EditQCNote</title>
		<meta content="Microsoft Visual Studio.NET 7.0" name="GENERATOR">
		<meta content="Visual Basic 7.0" name="CODE_LANGUAGE">
		<meta content="JavaScript" name="vs_defaultClientScript">
		<meta content="http://schemas.microsoft.com/intellisense/ie5" name="vs_targetSchema">
		<LINK href="Style/vla-ie.css" type="text/css" rel="stylesheet">
	</HEAD>
	<body>
		<form id="Form1" method="post" runat="server">
			<uc1:vlaheader id="VLAHeader1" style="POSITION: relative" runat="server"></uc1:vlaheader>
			<DIV id="ctlDivChooseQCNote" style="WIDTH: 752px; POSITION: relative; HEIGHT: 64px" runat="server"
				ms_positioning="GridLayout"><asp:label id="lblChooseQCNote" style="Z-INDEX: 101; LEFT: 16px; POSITION: absolute; TOP: 24px"
					runat="server">Select QC Note Ref:</asp:label><asp:dropdownlist id="ddlQCNotes" style="Z-INDEX: 102; LEFT: 152px; POSITION: absolute; TOP: 24px"
					runat="server" Width="152px" AutoPostBack="True"></asp:dropdownlist></DIV>
			<DIV style="WIDTH: 784px; POSITION: relative; HEIGHT: 504px" ms_positioning="GridLayout">
				<DIV style="BORDER-RIGHT: 1px solid; BORDER-TOP: 1px solid; Z-INDEX: 103; LEFT: 24px; BORDER-LEFT: 1px solid; WIDTH: 736px; BORDER-BOTTOM: 1px solid; POSITION: absolute; TOP: 8px; HEIGHT: 128px"
					ms_positioning="GridLayout"><asp:label id="lblQCNoteRef" style="Z-INDEX: 115; LEFT: 240px; POSITION: absolute; TOP: 8px"
						runat="server" Font-Size="Larger"></asp:label><asp:label id="lblQCNoteLabel" style="Z-INDEX: 115; LEFT: 16px; POSITION: absolute; TOP: 8px"
						runat="server" Font-Size="Larger" Font-Bold="True">QC Note Ref:</asp:label><asp:label id="lblSubmissionNumber" style="Z-INDEX: 115; LEFT: 240px; POSITION: absolute; TOP: 32px"
						runat="server"></asp:label><asp:label id="lblSubmissionNumberLabel" style="Z-INDEX: 115; LEFT: 16px; POSITION: absolute; TOP: 32px"
						runat="server" Font-Bold="True">Submission Number:</asp:label><asp:label id="lblProjectLabel" style="Z-INDEX: 115; LEFT: 16px; POSITION: absolute; TOP: 56px"
						runat="server" Font-Bold="True">Project:</asp:label><asp:label id="lblProject" style="Z-INDEX: 115; LEFT: 240px; POSITION: absolute; TOP: 56px"
						runat="server"></asp:label><asp:label id="lblSpeciesLabel" style="Z-INDEX: 115; LEFT: 16px; POSITION: absolute; TOP: 80px"
						runat="server" Font-Bold="True">Species:</asp:label><asp:label id="lblStainRefLabel" style="Z-INDEX: 115; LEFT: 16px; POSITION: absolute; TOP: 104px"
						runat="server" Font-Bold="True">Stain Ref:</asp:label><asp:label id="lblSpecies" style="Z-INDEX: 115; LEFT: 240px; POSITION: absolute; TOP: 80px"
						runat="server"></asp:label><asp:label id="lblStainRef" style="Z-INDEX: 115; LEFT: 240px; POSITION: absolute; TOP: 104px"
						runat="server"></asp:label></DIV>
				<DIV style="BORDER-RIGHT: 1px solid; BORDER-TOP: 1px solid; Z-INDEX: 104; LEFT: 24px; BORDER-LEFT: 1px solid; WIDTH: 736px; BORDER-BOTTOM: 1px solid; POSITION: absolute; TOP: 424px; HEIGHT: 40px"
					ms_positioning="GridLayout"><asp:label id="lblCreatedBy" style="Z-INDEX: 101; LEFT: 16px; POSITION: absolute; TOP: 8px"
						runat="server"></asp:label><asp:label id="lblDateCreated" style="Z-INDEX: 102; LEFT: 208px; POSITION: absolute; TOP: 8px"
						runat="server"></asp:label></DIV>
				<DIV style="BORDER-RIGHT: 1px solid; BORDER-TOP: 1px solid; Z-INDEX: 105; LEFT: 24px; BORDER-LEFT: 1px solid; WIDTH: 736px; BORDER-BOTTOM: 1px solid; POSITION: absolute; TOP: 144px; HEIGHT: 272px"
					ms_positioning="GridLayout"><asp:textbox id="txtQCNoteText" style="Z-INDEX: 200; LEFT: 8px; POSITION: absolute; TOP: 8px"
						runat="server" Width="704px" MaxLength="7000" TextMode="MultiLine" Height="256px" Font-Names="Courier New"></asp:textbox></DIV>
				<asp:customvalidator id="valQCNoteText" style="Z-INDEX: 108; LEFT: 744px; POSITION: absolute; TOP: 152px"
					runat="server" CssClass="ValidatorText" ToolTip="Comments must be less than or equal to 4000 characters"
					ControlToValidate="txtQCNoteText" ClientValidationFunction="ValidateLength" Runat="server">*</asp:customvalidator><asp:button id="btnDone" style="Z-INDEX: 101; LEFT: 656px; POSITION: absolute; TOP: 472px" runat="server"
					Width="92px" Height="24px" Text="Done"></asp:button><asp:button id="btnCancel" style="Z-INDEX: 102; LEFT: 552px; POSITION: absolute; TOP: 472px"
					runat="server" Width="92" Height="24" Text="Cancel" CausesValidation="False"></asp:button></DIV>
			<DIV id="ctlDiv" style="WIDTH: 776px; HEIGHT: 4px" runat="server" ms_positioning="FlowLayout"></DIV>
			<uc1:vlafooter id="VLAFooter1" runat="server"></uc1:vlafooter></form>
	</body>
</HTML>
