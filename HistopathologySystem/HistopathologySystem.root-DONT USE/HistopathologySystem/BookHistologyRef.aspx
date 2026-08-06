<%@ Page Language="vb" AutoEventWireup="false" Codebehind="BookHistologyRef.aspx.vb" Inherits="HistopathologySystem.BookHistologyRef"%>
<%@ Register TagPrefix="uc1" TagName="VLAHeader" Src="VLAHeader.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAFooter" Src="VLAFooter.ascx" %>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<HTML>
	<HEAD>
		<title>BookHistologyRef</title>
		<meta content="Microsoft Visual Studio.NET 7.0" name="GENERATOR">
		<meta content="Visual Basic 7.0" name="CODE_LANGUAGE">
		<meta content="JavaScript" name="vs_defaultClientScript">
		<meta content="http://schemas.microsoft.com/intellisense/ie5" name="vs_targetSchema">
		<LINK href="Style/vla-ie.css" type="text/css" rel="stylesheet">
	</HEAD>
	<body>
		<form id="Form1" method="post" runat="server">
			<uc1:vlaheader id="VLAHeader1" runat="server"></uc1:vlaheader>
			<DIV style="WIDTH: 749px; POSITION: relative; HEIGHT: 41px" ms_positioning="GridLayout"><asp:label id="lblGridLabel" style="Z-INDEX: 101; LEFT: 2px; POSITION: absolute; TOP: 6px"
					runat="server" Font-Bold="True">Current Database Histology Refs</asp:label></DIV>
			<DIV style="WIDTH: 492px; HEIGHT: 100px"><asp:datagrid id="grdHistologyRefs" runat="server" AutoGenerateColumns="False">
					<ItemStyle CssClass="GridItem"></ItemStyle>
					<HeaderStyle CssClass="GridHeader"></HeaderStyle>
					<Columns>
						<asp:BoundColumn DataField="Description" HeaderText="Description">
							<ItemStyle Width="280px"></ItemStyle>
						</asp:BoundColumn>
						<asp:BoundColumn DataField="NextHistologyRef" HeaderText="Next Histology Ref">
							<ItemStyle Width="180px"></ItemStyle>
						</asp:BoundColumn>
					</Columns>
				</asp:datagrid></DIV>
			<DIV style="WIDTH: 749px; POSITION: relative; HEIGHT: 216px" ms_positioning="GridLayout">
				<asp:dropdownlist id="ddlHistologyType" style="Z-INDEX: 104; LEFT: 214px; POSITION: absolute; TOP: 88px"
					runat="server" Width="240px"></asp:dropdownlist>
				<asp:textbox id="txtNumberToBook" style="Z-INDEX: 106; LEFT: 214px; POSITION: absolute; TOP: 122px"
					runat="server" Width="62"></asp:textbox>
				<asp:button id="btnOK" style="Z-INDEX: 105; LEFT: 8px; POSITION: absolute; TOP: 160px" runat="server"
					Width="99px" CausesValidation="False" Text="Book"></asp:button>
				<asp:button id="btnBack" style="Z-INDEX: 103; LEFT: 648px; POSITION: absolute; TOP: 160px" runat="server"
					Width="78px" CausesValidation="False" Text="Done"></asp:button>
				<asp:regularexpressionvalidator id="revNoToBook" style="Z-INDEX: 102; LEFT: 278px; POSITION: absolute; TOP: 122px"
					runat="server" ValidationExpression="^[1-9]+[0-9]*$" CssClass="ValidatorText" ControlToValidate="txtNumberToBook"
					ToolTip="Numeric Value">*</asp:regularexpressionvalidator>
				<asp:requiredfieldvalidator id="rfvNoToBook" style="Z-INDEX: 101; LEFT: 278px; POSITION: absolute; TOP: 122px"
					runat="server" CssClass="ValidatorText" ControlToValidate="txtNumberToBook" ToolTip="Required Field">*</asp:requiredfieldvalidator>
				<asp:label id="lblHistoType" style="Z-INDEX: 107; LEFT: 10px; POSITION: absolute; TOP: 88px"
					runat="server">Histology Ref Range Type</asp:label>
				<asp:label id="lblNoOf" style="Z-INDEX: 108; LEFT: 10px; POSITION: absolute; TOP: 122px" runat="server">Number Required</asp:label>
				<asp:label id="lblDescription" style="Z-INDEX: 109; LEFT: 10px; POSITION: absolute; TOP: 27px"
					runat="server">Select the type of Histology number required from the picklist, enter the number required in the 'Number Required' text box and Click on the 'Book' button.</asp:label>
				<asp:requiredfieldvalidator id="rfvHistoType" style="Z-INDEX: 110; LEFT: 456px; POSITION: absolute; TOP: 88px"
					runat="server" CssClass="ValidatorText" ControlToValidate="ddlHistologyType" ToolTip="Required Field">*</asp:requiredfieldvalidator>
				<HR style="Z-INDEX: 111; LEFT: 11px; POSITION: absolute; TOP: 12px; HEIGHT: 1px" width="97%"
					SIZE="1">
				<HR style="Z-INDEX: 112; LEFT: 8px; POSITION: absolute; TOP: 152px" width="97%" SIZE="1">
				<asp:LinkButton id="lbCheckBlockRefs" style="Z-INDEX: 119; LEFT: 8px; POSITION: absolute; TOP: 192px"
					runat="server" CausesValidation="False">Check Used Block Refs</asp:LinkButton>
			</DIV>
			<DIV id="ctlDIV" style="WIDTH: 749px; HEIGHT: 3px" runat="server" ms_positioning="FlowLayout"></DIV>
			<DIV style="WIDTH: 744px; HEIGHT: 8px" ms_positioning="FlowLayout" id="ctlBlockBookDiv"
				runat="server"></DIV>
			<uc1:vlafooter id="VLAFooter1" runat="server"></uc1:vlafooter></form>
	</body>
</HTML>
