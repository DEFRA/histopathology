<%@ Register TagPrefix="uc1" TagName="DataGridPager" Src="DataGridPager.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAFooter" Src="VLAFooter.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAHeader" Src="VLAHeader.ascx" %>
<%@ Page Language="vb" AutoEventWireup="false" Codebehind="ViewImportedData.aspx.vb" Inherits="HistopathologySystem.ViewImportedData" smartNavigation="True"%>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<HTML>
	<HEAD>
		<title>ViewImportedData</title>
		<meta content="Microsoft Visual Studio .NET 7.1" name="GENERATOR">
		<meta content="Visual Basic .NET 7.1" name="CODE_LANGUAGE">
		<meta content="JavaScript" name="vs_defaultClientScript">
		<meta content="http://schemas.microsoft.com/intellisense/ie5" name="vs_targetSchema">
		<LINK href="Style/vla-ie.css" type="text/css" rel="stylesheet">
	</HEAD>
	<body>
		<form id="Form1" method="post" runat="server">
			<uc1:vlaheader id="VLAHeader1" runat="server"></uc1:vlaheader>
			<DIV style="WIDTH: 760px; POSITION: relative; HEIGHT: 73px" ms_positioning="GridLayout">
				<asp:dropdownlist id="ddlTable" style="Z-INDEX: 107; LEFT: 56px; POSITION: absolute; TOP: 8px" runat="server"
					Width="344px" AutoPostBack="True"></asp:dropdownlist>
				<asp:textbox id="txtFilter" style="Z-INDEX: 101; LEFT: 56px; POSITION: absolute; TOP: 40px" runat="server"
					MaxLength="50" Width="344px"></asp:textbox>
				<asp:label id="lblFilter" style="Z-INDEX: 102; LEFT: 16px; POSITION: absolute; TOP: 40px" runat="server">Filter</asp:label>
				<HR style="Z-INDEX: 103; LEFT: 0px; WIDTH: 90.18%; POSITION: absolute; TOP: 72px; HEIGHT: 1px"
					width="90.18%" SIZE="1">
				<asp:button id="btnGo" style="Z-INDEX: 104; LEFT: 424px; POSITION: absolute; TOP: 40px" runat="server"
					Width="73" Text="Go"></asp:button>
				<asp:requiredfieldvalidator id="RequiredFieldValidator1" style="Z-INDEX: 105; LEFT: 408px; POSITION: absolute; TOP: 40px"
					runat="server" ControlToValidate="txtFilter" ErrorMessage="*" CssClass="ValidatorText" ToolTip="Enter filter criteria"></asp:requiredfieldvalidator>
				<asp:button id="btnClear" style="Z-INDEX: 106; LEFT: 504px; POSITION: absolute; TOP: 40px" runat="server"
					Width="73px" Text="Clear" CausesValidation="False"></asp:button>
				<asp:label id="lbltable" style="Z-INDEX: 108; LEFT: 16px; POSITION: absolute; TOP: 8px" runat="server">Table</asp:label></DIV>
			<DIV style="WIDTH: 750px" ms_positioning="FlowLayout"><asp:datagrid id="ImportedDataGrid" runat="server" Width="688px" AllowSorting="True" AllowPaging="True"
					PageSize="30" AutoGenerateColumns="False">
					<SelectedItemStyle CssClass="GridSelectedItemSmall"></SelectedItemStyle>
					<EditItemStyle CssClass="GridEditItemSmall"></EditItemStyle>
					<ItemStyle CssClass="GridItemSmall"></ItemStyle>
					<HeaderStyle CssClass="GridHeader"></HeaderStyle>
					<Columns>
						<asp:BoundColumn DataField="SenderRef" SortExpression="SenderRef" HeaderText="Sender Ref"></asp:BoundColumn>
						<asp:BoundColumn DataField="HistologyRef" SortExpression="HistologyRef" HeaderText="Histology Ref"></asp:BoundColumn>
						<asp:BoundColumn DataField="BlockRef" SortExpression="BlockRef" HeaderText="Block Ref"></asp:BoundColumn>
						<asp:BoundColumn DataField="Project" SortExpression="Project" HeaderText="Project"></asp:BoundColumn>
						<asp:BoundColumn DataField="DateSubmitted" SortExpression="DateSubmitted" HeaderText="Date Submitted"
							DataFormatString="{0:d}"></asp:BoundColumn>
						<asp:BoundColumn DataField="Species" SortExpression="Species" HeaderText="Species"></asp:BoundColumn>
						<asp:BoundColumn DataField="Tissue" SortExpression="Tissue" HeaderText="Tissue"></asp:BoundColumn>
						<asp:BoundColumn DataField="Comments" SortExpression="Comments" HeaderText="Comments"></asp:BoundColumn>
					</Columns>
					<PagerStyle Visible="False"></PagerStyle>
				</asp:datagrid></DIV>
			<DIV style="WIDTH: 720px; POSITION: relative; HEIGHT: 62px" ms_positioning="GridLayout">
				<uc1:DataGridPager id="ImportedDataPager" runat="server"></uc1:DataGridPager>
				<asp:HyperLink id="hlExcel" style="Z-INDEX: 101; LEFT: 504px; POSITION: absolute; TOP: 32px" runat="server"
					Visible="False" Target="_blank" NavigateUrl="ExcelExport.aspx">Export To Excel</asp:HyperLink></DIV>
			<uc1:VLAFooter id="VLAFooter1" runat="server"></uc1:VLAFooter></form>
	</body>
</HTML>
