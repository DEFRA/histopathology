<%@ Page Language="vb" AutoEventWireup="false" Codebehind="ViewSamples.aspx.vb" Inherits="HistopathologySystem.ViewSamples"%>
<%@ Register TagPrefix="uc1" TagName="DataGridPager" Src="DataGridPager.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAHeader" Src="VLAHeader.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAFooter" Src="VLAFooter.ascx" %>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<HTML>
	<HEAD>
		<title>ViewSamples</title>
		<meta content="Microsoft Visual Studio.NET 7.0" name="GENERATOR">
		<meta content="Visual Basic 7.0" name="CODE_LANGUAGE">
		<meta content="JavaScript" name="vs_defaultClientScript">
		<meta content="http://schemas.microsoft.com/intellisense/ie5" name="vs_targetSchema">
		<LINK href="Style/vla-ie.css" type="text/css" rel="stylesheet">
	</HEAD>
	<body>
		<form id="Form1" method="post" runat="server">
			<uc1:vlaheader id="VLAHeader1" runat="server"></uc1:vlaheader>
			<DIV style="WIDTH: 760px; POSITION: relative; HEIGHT: 144px" ms_positioning="GridLayout">
				<HR style="Z-INDEX: 102; LEFT: 8px; WIDTH: 96.86%; POSITION: absolute; TOP: 136px; HEIGHT: 1px"
					width="96.86%" SIZE="1">
				<asp:textbox id="txtSenderRef" style="Z-INDEX: 103; LEFT: 88px; POSITION: absolute; TOP: 14px"
					runat="server" MaxLength="20" Height="21px" Width="169"></asp:textbox>
				<asp:textbox id="txtHistRef" style="Z-INDEX: 106; LEFT: 367px; POSITION: absolute; TOP: 14px"
					runat="server" MaxLength="20" Height="21px" Width="170"></asp:textbox>
				<asp:DropDownList id="ddlTissue" style="Z-INDEX: 110; LEFT: 88px; POSITION: absolute; TOP: 48px" runat="server"
					Width="169"></asp:DropDownList>
				<asp:DropDownList id="ddlProject" style="Z-INDEX: 113; LEFT: 367px; POSITION: absolute; TOP: 48px"
					runat="server" Width="170px"></asp:DropDownList>
				<asp:radiobutton id="rbWetTissue" style="Z-INDEX: 108; LEFT: 16px; POSITION: absolute; TOP: 80px"
					runat="server" Text="Tissue Information" AutoPostBack="True"></asp:radiobutton>
				<asp:radiobutton id="rbBlockInformation" style="Z-INDEX: 109; LEFT: 160px; POSITION: absolute; TOP: 80px"
					runat="server" Text="Block Information" AutoPostBack="True"></asp:radiobutton>
				<asp:button id="btnSearch" style="Z-INDEX: 101; LEFT: 368px; POSITION: absolute; TOP: 80px"
					runat="server" Width="83px" Text="Search"></asp:button>
				<asp:label id="lblSenderRef" style="Z-INDEX: 104; LEFT: 14px; POSITION: absolute; TOP: 14px"
					runat="server">Sender Ref</asp:label>
				<asp:label id="lblHistRef" style="Z-INDEX: 105; LEFT: 272px; POSITION: absolute; TOP: 14px"
					runat="server">Histology Ref</asp:label>
				<asp:label id="lblError" style="Z-INDEX: 107; LEFT: 536px; POSITION: absolute; TOP: 14px" runat="server"
					CssClass="ValidatorText" ToolTip="Must enter either the Sender Ref or HistologyRef">*</asp:label>
				<asp:Label id="lblTissue" style="Z-INDEX: 111; LEFT: 16px; POSITION: absolute; TOP: 48px" runat="server">Tissue</asp:Label>
				<asp:Label id="lblProject" style="Z-INDEX: 112; LEFT: 272px; POSITION: absolute; TOP: 48px"
					runat="server">Project</asp:Label>
				<asp:Label id="lblKey" style="Z-INDEX: 114; LEFT: 16px; POSITION: absolute; TOP: 112px" runat="server">Submitted As: WT = Wet Tissue, WB = Wax Block, SS = Stained Section, US = Unstained Section, PC = Pre Cassetted</asp:Label>
				<asp:Label id="lblOtherFieldValue" style="Z-INDEX: 115; LEFT: 560px; POSITION: absolute; TOP: 14px"
					runat="server" Width="184px"></asp:Label>
			</DIV>
			<DIV id="ctlDiv" style="WIDTH: 746px; HEIGHT: 1px" runat="server"></DIV>
			<DIV id="ctlAnimalTissuesDiv" style="WIDTH: 940px" runat="server" ms_positioning="FlowLayout"><asp:datagrid id="grdTissuesGrid" runat="server" PageSize="20" AllowSorting="True" AllowPaging="True"
					AutoGenerateColumns="False">
					<SelectedItemStyle CssClass="GridSelectedItemSmall"></SelectedItemStyle>
					<EditItemStyle CssClass="GridEditItemSmall"></EditItemStyle>
					<ItemStyle CssClass="GridItemSmall"></ItemStyle>
					<HeaderStyle CssClass="GridHeader"></HeaderStyle>
					<Columns>
						<asp:BoundColumn DataField="ID" SortExpression="ID" HeaderText="Sub. Number">
							<ItemStyle HorizontalAlign="Left" Width="66px"></ItemStyle>
						</asp:BoundColumn>
						<asp:BoundColumn DataField="DateSubmitted" SortExpression="DateSubmitted" HeaderText="Date Submitted">
							<ItemStyle HorizontalAlign="Left" Width="85px"></ItemStyle>
						</asp:BoundColumn>
						<asp:BoundColumn DataField="DateReceived" SortExpression="DateReceived" HeaderText="Date Received / Rejected">
							<ItemStyle HorizontalAlign="Left" Width="80px"></ItemStyle>
						</asp:BoundColumn>
						<asp:BoundColumn DataField="TimeReceived" SortExpression="TimeReceived" HeaderText="Time Received / Rejected">
							<ItemStyle HorizontalAlign="Left" Width="80px"></ItemStyle>
						</asp:BoundColumn>
						<asp:BoundColumn DataField="DateCompleted" SortExpression="DateCompleted" HeaderText="Date Completed">
							<ItemStyle HorizontalAlign="Left" Width="85px"></ItemStyle>
						</asp:BoundColumn>
						<asp:BoundColumn DataField="CustomerReceivedDate" SortExpression="CustomerReceivedDate" HeaderText="Customer Received Date">
							<ItemStyle HorizontalAlign="Left" Width="85px"></ItemStyle>
						</asp:BoundColumn>
						<asp:BoundColumn DataField="SubmittedAs" SortExpression="SubmittedAs" HeaderText="Submitted As">
							<ItemStyle HorizontalAlign="Left" Width="50px"></ItemStyle>
						</asp:BoundColumn>
						<asp:BoundColumn DataField="TissueDescription" SortExpression="TissueDescription" HeaderText="Tissue">
							<ItemStyle HorizontalAlign="Left" Width="130px"></ItemStyle>
						</asp:BoundColumn>
						<asp:BoundColumn DataField="NoPieces" SortExpression="NoPieces" HeaderText="No. Pieces">
							<ItemStyle HorizontalAlign="Left" Width="90px"></ItemStyle>
						</asp:BoundColumn>
					</Columns>
					<PagerStyle Visible="False"></PagerStyle>
				</asp:datagrid>
				<DIV style="WIDTH: 792px; POSITION: relative; HEIGHT: 48px" ms_positioning="GridLayout"><uc1:datagridpager id="TissuesGridPager" runat="server"></uc1:datagridpager>
					<asp:HyperLink id="hlTissuesExcelExport" style="Z-INDEX: 100; LEFT: 584px; POSITION: absolute; TOP: 24px"
						runat="server" Visible="True" Target="_blank" NavigateUrl="ExcelExport.aspx">Export to Excel</asp:HyperLink></DIV>
			</DIV>
			<DIV style="WIDTH: 940px" id="ctlAnimalBlockTissuesDiv" runat="server">
				<asp:datagrid id="grdResults" runat="server" AutoGenerateColumns="False" AllowPaging="True" AllowSorting="True"
					PageSize="20">
					<SelectedItemStyle CssClass="GridSelectedItemSmall"></SelectedItemStyle>
					<EditItemStyle CssClass="GridEditItemSmall"></EditItemStyle>
					<ItemStyle CssClass="GridItemSmall"></ItemStyle>
					<HeaderStyle CssClass="GridHeader"></HeaderStyle>
					<Columns>
						<asp:BoundColumn DataField="ID" SortExpression="ID" HeaderText="Sub. Number">
							<ItemStyle HorizontalAlign="Left" Width="66px"></ItemStyle>
						</asp:BoundColumn>
						<asp:BoundColumn DataField="DateSubmitted" SortExpression="DateSubmitted" HeaderText="Date Submitted">
							<ItemStyle HorizontalAlign="Left" Width="85px"></ItemStyle>
						</asp:BoundColumn>
						<asp:BoundColumn DataField="DateReceived" SortExpression="DateReceived" HeaderText="Date Received / Rejected">
							<ItemStyle HorizontalAlign="Left" Width="80px"></ItemStyle>
						</asp:BoundColumn>
						<asp:BoundColumn DataField="TimeReceived" SortExpression="TimeReceived" HeaderText="Time Received / Rejected">
							<ItemStyle HorizontalAlign="Left" Width="80px"></ItemStyle>
						</asp:BoundColumn>
						<asp:BoundColumn DataField="DateCompleted" SortExpression="DateCompleted" HeaderText="Date Completed">
							<ItemStyle HorizontalAlign="Left" Width="85px"></ItemStyle>
						</asp:BoundColumn>
						<asp:BoundColumn DataField="CustomerReceivedDate" SortExpression="CustomerReceivedDate" HeaderText="Customer Received Date">
							<ItemStyle HorizontalAlign="Left" Width="85px"></ItemStyle>
						</asp:BoundColumn>
						<asp:BoundColumn DataField="SubmittedAs" SortExpression="SubmittedAs" HeaderText="Submitted As">
							<ItemStyle HorizontalAlign="Left" Width="50px"></ItemStyle>
						</asp:BoundColumn>
						<asp:BoundColumn DataField="BlockRef" SortExpression="BlockRef" HeaderText="Block Ref">
							<ItemStyle HorizontalAlign="Left" Width="50px"></ItemStyle>
						</asp:BoundColumn>
						<asp:BoundColumn DataField="TissueDescription" SortExpression="TissueDescription" HeaderText="Tissue">
							<ItemStyle HorizontalAlign="Left" Width="130px"></ItemStyle>
						</asp:BoundColumn>
						<asp:BoundColumn DataField="NoPieces" SortExpression="NoPieces" HeaderText="No. Pieces">
							<ItemStyle HorizontalAlign="Left" Width="90px"></ItemStyle>
						</asp:BoundColumn>
					</Columns>
					<PagerStyle Visible="False"></PagerStyle>
				</asp:datagrid>
				<DIV style="WIDTH: 784px; POSITION: relative; HEIGHT: 40px" ms_positioning="GridLayout"><uc1:datagridpager id="ResultsPager" runat="server"></uc1:datagridpager>
					<asp:HyperLink id="hlExcelExport" style="Z-INDEX: 101; LEFT: 568px; POSITION: absolute; TOP: 24px"
						runat="server" Visible="True" Target="_blank" NavigateUrl="ExcelExport.aspx">Export to Excel</asp:HyperLink></DIV>
			</DIV>
			<uc1:vlafooter id="VLAFooter1" runat="server"></uc1:vlafooter></form>
	</body>
</HTML>
