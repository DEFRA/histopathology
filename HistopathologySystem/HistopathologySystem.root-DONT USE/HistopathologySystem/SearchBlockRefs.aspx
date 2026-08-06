<%@ Register TagPrefix="uc1" TagName="VLAFooter" Src="VLAFooter.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAHeader" Src="VLAHeader.ascx" %>
<%@ Register TagPrefix="uc1" TagName="DataGridPager" Src="DataGridPager.ascx" %>
<%@ Page Language="vb" AutoEventWireup="false" Codebehind="SearchBlockRefs.aspx.vb" Inherits="HistopathologySystem.SearchBlockRefs" smartNavigation="True"%>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<HTML>
	<HEAD>
		<title>Search Block Refs</title>
		<meta content="Microsoft Visual Studio.NET 7.0" name="GENERATOR">
		<meta content="Visual Basic 7.0" name="CODE_LANGUAGE">
		<meta content="JavaScript" name="vs_defaultClientScript">
		<meta content="http://schemas.microsoft.com/intellisense/ie5" name="vs_targetSchema">
		<LINK href="Style/vla-ie.css" type="text/css" rel="stylesheet">
	</HEAD>
	<body>
		<form id="Form1" method="post" runat="server">
			<uc1:vlaheader id="VLAHeader1" runat="server"></uc1:vlaheader>
			<DIV style="WIDTH: 712px; POSITION: relative; HEIGHT: 59px" ms_positioning="GridLayout">
				<HR style="Z-INDEX: 102; LEFT: 10px; WIDTH: 96.86%; POSITION: absolute; TOP: 48px; HEIGHT: 1px"
					width="96.86%" SIZE="1">
				<asp:label id="lblHistRef" style="Z-INDEX: 105; LEFT: 272px; POSITION: absolute; TOP: 8px"
					runat="server">Histology Ref</asp:label>
				<asp:textbox id="txtSenderRef" style="Z-INDEX: 108; LEFT: 96px; POSITION: absolute; TOP: 8px"
					runat="server" Width="152px" Height="21px" MaxLength="20"></asp:textbox>
				<asp:textbox id="txtHistRef" style="Z-INDEX: 106; LEFT: 360px; POSITION: absolute; TOP: 8px"
					runat="server" MaxLength="20" Height="21px" Width="152"></asp:textbox>
				<asp:label id="lblError" style="Z-INDEX: 107; LEFT: 520px; POSITION: absolute; TOP: 8px" runat="server"
					CssClass="ValidatorText" ToolTip="Must enter either the Sender Ref or HistologyRef">*</asp:label><asp:button id="btnSearch" style="Z-INDEX: 101; LEFT: 544px; POSITION: absolute; TOP: 8px" runat="server"
					Width="83px" Text="Search"></asp:button>
				<asp:label id="lblSenderRef" style="Z-INDEX: 109; LEFT: 16px; POSITION: absolute; TOP: 8px"
					runat="server">Sender Ref</asp:label></DIV>
			<DIV id="ctlDiv" style="WIDTH: 711px; HEIGHT: 16px" runat="server"></DIV>
			<DIV style="WIDTH: 712px"><asp:datagrid id="grdResults" runat="server" PageSize="20" AllowSorting="True" AllowPaging="True"
					AutoGenerateColumns="False">
					<SelectedItemStyle CssClass="GridSelectedItemSmall"></SelectedItemStyle>
					<EditItemStyle CssClass="GridEditItemSmall"></EditItemStyle>
					<ItemStyle CssClass="GridItemSmall"></ItemStyle>
					<HeaderStyle CssClass="GridHeader"></HeaderStyle>
					<Columns>
						<asp:BoundColumn DataField="Used Block Refs" SortExpression="Used Block Refs" HeaderText="Used Block Refs">
							<ItemStyle Width="200px"></ItemStyle>
						</asp:BoundColumn>
						<asp:BoundColumn DataField="Unused Block Refs" SortExpression="Unused Block Refs" HeaderText="Unused Block Refs">
							<ItemStyle Width="200px"></ItemStyle>
						</asp:BoundColumn>
						<asp:BoundColumn DataField="Pre Booked Block Refs" SortExpression="Pre Booked Block Refs" HeaderText="Pre Booked Block Refs">
							<ItemStyle Width="200px"></ItemStyle>
						</asp:BoundColumn>
					</Columns>
					<PagerStyle Visible="False"></PagerStyle>
				</asp:datagrid><uc1:datagridpager id="ResultsPager" runat="server"></uc1:datagridpager><asp:hyperlink id="hlExcelExport" style="Z-INDEX: 101; LEFT: 450px; POSITION: relative" runat="server"
					Visible="False" Target="_blank" NavigateUrl="ExcelExport.aspx">Export to Excel</asp:hyperlink></DIV>
			<DIV style="WIDTH: 712px; POSITION: relative; HEIGHT: 54px" ms_positioning="GridLayout">
				<HR style="Z-INDEX: 101; LEFT: 10px; WIDTH: 96.86%; POSITION: absolute; TOP: 7px; HEIGHT: 1px"
					width="96.86%" SIZE="1">
				<asp:Button id="btnDone" style="Z-INDEX: 102; LEFT: 614px; POSITION: absolute; TOP: 12px" runat="server"
					Width="73px" Text="Done"></asp:Button>
				<asp:LinkButton id="lbViewImportedData" style="Z-INDEX: 103; LEFT: 8px; POSITION: absolute; TOP: 16px"
					runat="server">View Old ICC_Sub data</asp:LinkButton>
			</DIV>
			<uc1:vlafooter id="VLAFooter1" runat="server"></uc1:vlafooter>
		</form>
	</body>
</HTML>
