<%@ Page Language="vb" AutoEventWireup="false" Codebehind="SearchUnUsedHistologyRefs.aspx.vb" Inherits="HistopathologySystem.SearchUnUsedHistologyRefs" smartNavigation="True" %>
<%@ Register TagPrefix="uc1" TagName="DataGridPager" Src="DataGridPager.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAHeader" Src="VLAHeader.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAFooter" Src="VLAFooter.ascx" %>
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
			<DIV id="ctlDiv" style="WIDTH: 711px; HEIGHT: 16px" runat="server"></DIV>
			<DIV style="WIDTH: 445px;"><asp:datagrid id="grdResults" runat="server" PageSize="20" AllowSorting="True" AllowPaging="True"
					AutoGenerateColumns="False">
					<SelectedItemStyle CssClass="GridSelectedItemSmall"></SelectedItemStyle>
					<EditItemStyle CssClass="GridEditItemSmall"></EditItemStyle>
					<ItemStyle CssClass="GridItemSmall"></ItemStyle>
					<HeaderStyle CssClass="GridHeader"></HeaderStyle>
					<Columns>
						<asp:BoundColumn DataField="HistologyRef" SortExpression="HistologyRef" HeaderText="Histology Ref">
							<ItemStyle Width="200px"></ItemStyle>
						</asp:BoundColumn>
					</Columns>
					<PagerStyle Visible="False"></PagerStyle>
				</asp:datagrid><uc1:datagridpager id="ResultsPager" runat="server"></uc1:datagridpager>
				<asp:hyperlink id="hlExcelExport" runat="server" NavigateUrl="ExcelExport.aspx" Target="_blank"
					Visible="True">Export to Excel</asp:hyperlink></DIV>
			<DIV></DIV>
			<uc1:vlafooter id="VLAFooter1" runat="server"></uc1:vlafooter>
		</form>
	</body>
</HTML>
