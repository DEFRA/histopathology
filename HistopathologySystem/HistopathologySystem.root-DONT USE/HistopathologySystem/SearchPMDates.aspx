<%@ Page Language="vb" AutoEventWireup="false" Codebehind="SearchPMDates.aspx.vb" Inherits="HistopathologySystem.SearchPMDates" smartNavigation="True"%>
<%@ Register TagPrefix="uc1" TagName="DataGridPager" Src="DataGridPager.ascx" %>
<%@ Register TagPrefix="uc1" TagName="CalendarDate" Src="CalendarDate.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAHeader" Src="VLAHeader.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAFooter" Src="VLAFooter.ascx" %>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<HTML>
	<HEAD>
		<title>SearchPMDates</title>
		<meta content="Microsoft Visual Studio.NET 7.0" name="GENERATOR">
		<meta content="Visual Basic 7.0" name="CODE_LANGUAGE">
		<meta content="JavaScript" name="vs_defaultClientScript">
		<meta content="http://schemas.microsoft.com/intellisense/ie5" name="vs_targetSchema">
		<LINK href="Style/vla-ie.css" type="text/css" rel="stylesheet">
	</HEAD>
	<body>
		<form id="Form1" method="post" runat="server">
			<uc1:vlaheader id="VLAHeader1" runat="server"></uc1:vlaheader>
			<DIV style="Z-INDEX: 120; WIDTH: 758px; POSITION: relative; HEIGHT: 80px" ms_positioning="GridLayout">
				<DIV style="Z-INDEX: 105; LEFT: 116px; WIDTH: 174px; POSITION: absolute; TOP: 13px; HEIGHT: 40px"><uc1:calendardate id="ctlFromDate" runat="server"></uc1:calendardate></DIV>
				<DIV style="Z-INDEX: 107; LEFT: 330px; WIDTH: 199px; POSITION: absolute; TOP: 13px; HEIGHT: 41px"><uc1:calendardate id="ctlToDate" runat="server"></uc1:calendardate></DIV>
				<asp:label id="lblFromDate" style="Z-INDEX: 102; LEFT: 15px; POSITION: absolute; TOP: 14px" runat="server">PM from date</asp:label><asp:label id="lblPMToDate" style="Z-INDEX: 103; LEFT: 306px; POSITION: absolute; TOP: 17px" runat="server">to</asp:label><asp:button id="btnSearch" style="Z-INDEX: 100; LEFT: 558px; POSITION: absolute; TOP: 13px" runat="server" Text="Search" Width="102px"></asp:button>
				<HR style="Z-INDEX: 104; LEFT: 9px; WIDTH: 87.81%; POSITION: absolute; TOP: 68px; HEIGHT: 1px" width="87.81%" SIZE="1">
			</DIV>
			<DIV id="ctlDiv" style="WIDTH: 751px; HEIGHT: 8px" runat="server"></DIV>
			<DIV style="Z-INDEX: 110; WIDTH: 752px"><asp:datagrid id="grdSearchResults" runat="server" AutoGenerateColumns="False" AllowPaging="True" AllowSorting="True" PageSize="15">
					<SelectedItemStyle CssClass="GridSelectedItem"></SelectedItemStyle>
					<EditItemStyle CssClass="GridEditItem"></EditItemStyle>
					<ItemStyle CssClass="GridItem"></ItemStyle>
					<HeaderStyle CssClass="GridHeader"></HeaderStyle>
					<Columns>
						<asp:BoundColumn DataField="ID" SortExpression="ID" HeaderText="Sub. Number">
							<ItemStyle HorizontalAlign="Left" Width="68px"></ItemStyle>
						</asp:BoundColumn>
						<asp:BoundColumn DataField="SenderRef" SortExpression="SenderRef" HeaderText="Sender Ref">
							<ItemStyle HorizontalAlign="Left" Width="220px"></ItemStyle>
						</asp:BoundColumn>
						<asp:BoundColumn DataField="PMDate" SortExpression="PMDate" HeaderText="PM Date">
							<ItemStyle HorizontalAlign="Left" Width="95px"></ItemStyle>
						</asp:BoundColumn>
						<asp:BoundColumn DataField="BatchDate" SortExpression="BatchDate" HeaderText="Date Submitted">
							<ItemStyle HorizontalAlign="Left" Width="95px"></ItemStyle>
						</asp:BoundColumn>
						<asp:BoundColumn DataField="DateReceived" SortExpression="DateReceived" HeaderText="Date Received / Rejected">
							<ItemStyle HorizontalAlign="Left" Width="95px"></ItemStyle>
						</asp:BoundColumn>
						<asp:BoundColumn DataField="TimeReceived" SortExpression="TimeReceived" HeaderText="Time Received / Rejected">
							<ItemStyle HorizontalAlign="Left" Width="80px"></ItemStyle>
						</asp:BoundColumn>
						<asp:BoundColumn DataField="CompletedDate" SortExpression="CompletedDate" HeaderText="Date Completed">
							<ItemStyle HorizontalAlign="Left" Width="95px"></ItemStyle>
						</asp:BoundColumn>
						<asp:BoundColumn DataField="CustomerReceivedDate" SortExpression="CustomerReceivedDate" HeaderText="Customer Received Date">
							<ItemStyle HorizontalAlign="Left" Width="95px"></ItemStyle>
						</asp:BoundColumn>
					</Columns>
					<PagerStyle Visible="False"></PagerStyle>
				</asp:datagrid>
				<uc1:DataGridPager id="SearchResultsPager" runat="server"></uc1:DataGridPager></DIV>
			<asp:hyperlink id="hlbExcel" style="Z-INDEX: 101; LEFT: 640px; POSITION: relative" runat="server" Width="102px" Visible="False" NavigateUrl="ExcelExport.aspx" Target="_blank">Export to Excel</asp:hyperlink>
			<uc1:vlafooter id="VLAFooter1" runat="server"></uc1:vlafooter>
		</form>
	</body>
</HTML>
