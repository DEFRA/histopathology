<%@ Register TagPrefix="uc1" TagName="VLAFooter" Src="VLAFooter.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAHeader" Src="VLAHeader.ascx" %>
<%@ Register TagPrefix="uc1" TagName="DataGridPager" Src="DataGridPager.ascx" %>
<%@ Page Language="vb" AutoEventWireup="false" Codebehind="SubmissionDetails.aspx.vb" Inherits="HistopathologySystem.SubmissionDetails" smartNavigation="False"%>
<%@ Register TagPrefix="uc1" TagName="CalendarDate" Src="CalendarDate.ascx" %>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<HTML>
	<HEAD>
		<title>SubmissionDetails</title>
		<META content="Microsoft Visual Studio.NET 7.0" name="GENERATOR">
		<META content="Visual Basic 7.0" name="CODE_LANGUAGE">
		<META content="JavaScript" name="vs_defaultClientScript">
		<META content="http://schemas.microsoft.com/intellisense/ie5" name="vs_targetSchema">
		<LINK href="Style/vla-ie.css" type="text/css" rel="stylesheet">
	</HEAD>
	<BODY>
		<FORM id="Form1" method="post" runat="server">
			<uc1:vlaheader id="VLAHeader1" runat="server"></uc1:vlaheader>
			<DIV style="Z-INDEX: 120; WIDTH: 763px; POSITION: relative; HEIGHT: 87px" ms_positioning="GridLayout"><asp:textbox id="txtSenderRef" style="Z-INDEX: 101; LEFT: 121px; POSITION: absolute; TOP: 16px" runat="server" Height="23px" Enabled="False" Width="145"></asp:textbox><asp:label id="lblSenderRef" style="Z-INDEX: 102; LEFT: 14px; POSITION: absolute; TOP: 16px" runat="server">Sender Ref</asp:label>
				<HR style="Z-INDEX: 103; LEFT: 13px; POSITION: absolute; TOP: 50px; HEIGHT: 1px" width="97%" SIZE="1">
				<asp:label id="lblExplain" style="Z-INDEX: 104; LEFT: 14px; POSITION: absolute; TOP: 60px" runat="server">The following table displays the tissues for the selected sample within the submission.</asp:label><asp:label id="lblPMDate" style="Z-INDEX: 106; LEFT: 329px; POSITION: absolute; TOP: 16px" runat="server">PM Date</asp:label>
				<DIV style="Z-INDEX: 999; LEFT: 417px; WIDTH: 269px; POSITION: absolute; TOP: 16px; HEIGHT: 41px"><uc1:calendardate id="ctlPMDate" runat="server"></uc1:calendardate></DIV>
			</DIV>
			<DIV id="ctlHistologyDiv" style="WIDTH: 760px; POSITION: relative; HEIGHT: 39px" runat="server" ms_positioning="GridLayout"><asp:dropdownlist id="ddlHistologyType" style="Z-INDEX: 101; LEFT: 368px; POSITION: absolute; TOP: 8px" runat="server" Width="169px"></asp:dropdownlist><asp:label id="lblPick" style="Z-INDEX: 102; LEFT: 312px; POSITION: absolute; TOP: 8px" runat="server">Pick</asp:label><asp:textbox id="txtHistologyRef" style="Z-INDEX: 103; LEFT: 152px; POSITION: absolute; TOP: 8px" runat="server" Height="23" Width="144px" MaxLength="20"></asp:textbox><asp:label id="lblHistoRef" style="Z-INDEX: 104; LEFT: 14px; POSITION: absolute; TOP: 8px" runat="server">Histology Reference</asp:label></DIV>
			<DIV style="WIDTH: 760px"><asp:datagrid id="grdTissues" runat="server" AllowPaging="True" AutoGenerateColumns="False">
					<SelectedItemStyle CssClass="GridSelectedItemSmall"></SelectedItemStyle>
					<EditItemStyle CssClass="GridEditItemSmall"></EditItemStyle>
					<ItemStyle CssClass="GridItemSmall"></ItemStyle>
					<HeaderStyle CssClass="GridHeader"></HeaderStyle>
					<Columns>
						<asp:ButtonColumn Text="&lt;img src=&quot;Images/GridPager/sel.gif&quot;&gt;" CommandName="Select">
							<ItemStyle HorizontalAlign="Left" Width="20px"></ItemStyle>
						</asp:ButtonColumn>
						<asp:TemplateColumn SortExpression="TissueCode" HeaderText="Tissue">
							<ItemStyle HorizontalAlign="Left" Width="380px"></ItemStyle>
							<ItemTemplate>
								<asp:Label id="lblTissueCodeDisplay" runat="server" Width="280px"></asp:Label>
							</ItemTemplate>
							<EditItemTemplate>
								<TABLE cellSpacing="0" cellPadding="0" border="0">
									<TR height="10">
										<td>
											<asp:DropDownList id="ddlTissueCodeEdit" Width="360px" Runat="server"></asp:DropDownList>
										</td>
										<td>
											<asp:RequiredFieldValidator id="rvfTissueCode" runat="server" CssClass="ValidatorText" Height="8px" ToolTip="Required Field" ErrorMessage="*" ControlToValidate="ddlTissueCodeEdit" InitialValue=""></asp:RequiredFieldValidator>
										</td>
									</TR>
								</TABLE>
							</EditItemTemplate>
						</asp:TemplateColumn>
						<asp:TemplateColumn SortExpression="NoPieces" HeaderText="No Pieces">
							<ItemStyle HorizontalAlign="Left" Width="100px"></ItemStyle>
							<ItemTemplate>
								<asp:Label id="lblNoPiecesDisplay" runat="server" Width="100px"></asp:Label>
							</ItemTemplate>
							<EditItemTemplate>
								<TABLE cellSpacing="0" cellPadding="0" border="0">
									<TR height="10">
										<td>
											<asp:TextBox id="txtNoPiecesEdit" Width="80px" MaxLength="3" Runat="server"></asp:TextBox>
										</td>
										<td>
											<asp:RequiredFieldValidator id="rfvNoPieces" runat="server" CssClass="ValidatorText" Height="8px" ToolTip="Required Field" ErrorMessage="*" ControlToValidate="txtNoPiecesEdit" InitialValue=""></asp:RequiredFieldValidator>
										</td>
										<TD>
											<asp:RegularExpressionValidator id="revNoPieces" runat="server" CssClass="ValidatorText" Height="8px" ToolTip="Numeric Value" ErrorMessage="*" ControlToValidate="txtNoPiecesEdit" ValidationExpression="^[1-9]+[0-9]*$"></asp:RegularExpressionValidator></TD>
									</TR>
								</TABLE>
							</EditItemTemplate>
						</asp:TemplateColumn>
						<asp:TemplateColumn SortExpression="Comments" HeaderText="Comments">
							<ItemStyle HorizontalAlign="Left" Width="300px"></ItemStyle>
							<ItemTemplate>
								<asp:Label id="lblCommentsDisplay" runat="server" Width="300px"></asp:Label>
							</ItemTemplate>
							<EditItemTemplate>
								<asp:Textbox id="txtCommentsEdit" runat="server" Width="300px"></asp:Textbox>
							</EditItemTemplate>
						</asp:TemplateColumn>
					</Columns>
					<PagerStyle Visible="False"></PagerStyle>
				</asp:datagrid><uc1:datagridpager id="TissuesPager" runat="server"></uc1:datagridpager></DIV>
			<DIV style="WIDTH: 760px; POSITION: relative; HEIGHT: 55px" ms_positioning="GridLayout">
				<HR style="Z-INDEX: 103; LEFT: 16px; POSITION: absolute; TOP: 11px; HEIGHT: 1px" width="97%" SIZE="1">
				<asp:button id="btnBack" style="Z-INDEX: 1; LEFT: 528px; POSITION: absolute; TOP: 20px" runat="server" Height="22px" Width="106px" Text="Back" CausesValidation="False"></asp:button><asp:button id="btnSave" style="Z-INDEX: 102; LEFT: 648px; POSITION: absolute; TOP: 20px" runat="server" Height="22px" Width="105px" Text="Next"></asp:button><asp:button id="btnCancel" style="Z-INDEX: 101; LEFT: 416px; POSITION: absolute; TOP: 20px" runat="server" Height="22" Width="106" Text="Cancel" CausesValidation="False"></asp:button></DIV>
			<DIV id="ctlDiv" style="WIDTH: 761px; HEIGHT: 5px" runat="server"></DIV>
			<uc1:vlafooter id="VLAFooter1" runat="server"></uc1:vlafooter></FORM>
	</BODY>
</HTML>
