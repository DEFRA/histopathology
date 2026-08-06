<%@ Register TagPrefix="uc1" TagName="VLAFooter" Src="VLAFooter.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAHeader" Src="VLAHeader.ascx" %>
<%@ Register TagPrefix="uc1" TagName="DataGridPager" Src="DataGridPager.ascx" %>
<%@ Register TagPrefix="uc1" TagName="Batch" Src="Batch.ascx" %>
<%@ Page Language="vb" AutoEventWireup="false" Codebehind="ArchiveBlocks.aspx.vb" Inherits="HistopathologySystem.ArchiveBlocks" smartNavigation="True"%>
<%@ Register TagPrefix="uc1" TagName="CalendarDate" Src="CalendarDate.ascx" %>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<HTML>
	<HEAD>
		<title>ArchiveBlocks</title>
		<meta name="vs_snapToGrid" content="False">
		<meta content="Microsoft Visual Studio.NET 7.0" name="GENERATOR">
		<meta content="Visual Basic 7.0" name="CODE_LANGUAGE">
		<meta content="JavaScript" name="vs_defaultClientScript">
		<meta content="http://schemas.microsoft.com/intellisense/ie5" name="vs_targetSchema">
		<LINK href="Style/vla-ie.css" type="text/css" rel="stylesheet">
	</HEAD>
	<body>
		<form id="Form1" method="post" runat="server">
			<uc1:vlaheader id="VLAHeader1" runat="server"></uc1:vlaheader>
			<DIV style="WIDTH: 198px"><uc1:batch id="Batch1" runat="server"></uc1:batch></DIV>
			<DIV style="WIDTH: 768px; POSITION: relative; HEIGHT: 75px" ms_positioning="GridLayout">
				<asp:radiobuttonlist id="rblFilter" style="Z-INDEX: 103; LEFT: 8px; POSITION: absolute; TOP: 1px" runat="server"
					AutoPostBack="True" CssClass="Body" RepeatDirection="Horizontal"></asp:radiobuttonlist>
				<asp:dropdownlist id="ddlHistologyRefList" style="Z-INDEX: 105; LEFT: 227px; POSITION: absolute; TOP: 8px"
					runat="server" Width="156px"></asp:dropdownlist>
				<asp:dropdownlist id="ddlBlockRefList" style="Z-INDEX: 106; LEFT: 474px; POSITION: absolute; TOP: 8px"
					runat="server" Width="156"></asp:dropdownlist>
				<asp:button id="btnFilter" style="Z-INDEX: 100; LEFT: 641px; POSITION: absolute; TOP: 8px" runat="server"
					Text="Go" CausesValidation="False" Width="56px"></asp:button>
				<asp:TextBox id="txtPage" style="Z-INDEX: 107; LEFT: 119px; POSITION: absolute; TOP: 37px" runat="server"
					Width="75px" MaxLength="5"></asp:TextBox>
				<asp:Button id="btnGoToPage" style="Z-INDEX: 108; LEFT: 227px; POSITION: absolute; TOP: 37px"
					runat="server" CausesValidation="False" Text="Go to Page"></asp:Button>
				<asp:Label id="lblPage" style="Z-INDEX: 110; LEFT: 16px; POSITION: absolute; TOP: 37px" runat="server">Go To Page</asp:Label>
				<asp:label id="lblHistoFilter" style="Z-INDEX: 101; LEFT: 119px; POSITION: absolute; TOP: 8px"
					runat="server">Histology Ref</asp:label>
				<asp:label id="lblBlockRef" style="Z-INDEX: 102; LEFT: 394px; POSITION: absolute; TOP: 8px"
					runat="server">Block Ref</asp:label>
				<asp:checkbox id="cbSelectAll" style="Z-INDEX: 104; LEFT: 656px; POSITION: absolute; TOP: 39px"
					runat="server" AutoPostBack="True" Text="All"></asp:checkbox>
				<asp:customvalidator id="revPageNumber" style="Z-INDEX: 111; LEFT: 197px; POSITION: absolute; TOP: 37px"
					runat="server" CssClass="ValidatorText" ToolTip="Must be numeric" ControlToValidate="txtPage" OnServerValidate="ValidatePageNumber"
					ClientValidationFunction="ClientValidatePageNumber">*</asp:customvalidator>
				<asp:requiredfieldvalidator id="rfvPageNumber" style="Z-INDEX: 112; LEFT: 197px; POSITION: absolute; TOP: 37px"
					runat="server" CssClass="ValidatorText" ToolTip="Required Field" ControlToValidate="txtPage">*</asp:requiredfieldvalidator>
			</DIV>
			<DIV style="WIDTH: 696px"><asp:datagrid id="grdBlocks" runat="server" AutoGenerateColumns="False" PageSize="12" AllowPaging="True"
					AllowSorting="True">
					<SelectedItemStyle CssClass="GridSelectedItem"></SelectedItemStyle>
					<EditItemStyle CssClass="GridEditItem"></EditItemStyle>
					<ItemStyle CssClass="GridItem"></ItemStyle>
					<HeaderStyle CssClass="GridHeader"></HeaderStyle>
					<Columns>
						<asp:ButtonColumn Text="&lt;img src=&quot;Images/GridPager/sel.gif&quot;&gt;" CommandName="Select">
							<ItemStyle HorizontalAlign="Left" Width="20px"></ItemStyle>
						</asp:ButtonColumn>
						<asp:BoundColumn Visible="False" DataField="ID" SortExpression="ID" HeaderText="ID">
							<ItemStyle HorizontalAlign="Left" Width="100px"></ItemStyle>
						</asp:BoundColumn>
						<asp:BoundColumn DataField="SenderRef" SortExpression="SenderRef" HeaderText="Sender Ref">
							<ItemStyle HorizontalAlign="Left" Width="150px"></ItemStyle>
						</asp:BoundColumn>
						<asp:BoundColumn DataField="HistologyRef" SortExpression="Histology Ref" HeaderText="Histology Ref">
							<ItemStyle HorizontalAlign="Left" Width="150px"></ItemStyle>
						</asp:BoundColumn>
						<asp:BoundColumn Visible="False" DataField="AnimalID" SortExpression="AnimalID" HeaderText="AnimalID">
							<ItemStyle HorizontalAlign="Left" Width="100px"></ItemStyle>
						</asp:BoundColumn>
						<asp:BoundColumn DataField="BlockRef" SortExpression="BlockRef" HeaderText="Block Ref">
							<ItemStyle HorizontalAlign="Left" Width="100px"></ItemStyle>
						</asp:BoundColumn>
						<asp:BoundColumn Visible="False" DataField="ArchiveComment" SortExpression="Archive Comment" HeaderText="Archive Comment"></asp:BoundColumn>
						<asp:BoundColumn DataField="ArchivedDate" SortExpression="ArchivedDate" HeaderText="Archived Date"
							DataFormatString="{0:d}">
							<ItemStyle HorizontalAlign="Left" Width="100px"></ItemStyle>
						</asp:BoundColumn>
						<asp:TemplateColumn SortExpression="ArchiveLocation" HeaderText="Archive Location">
							<ItemStyle HorizontalAlign="Left" Width="100px"></ItemStyle>
							<ItemTemplate>
								<asp:Label ID="lblArchiveLocationDisplay" Runat="server" Enabled="true"></asp:Label>
							</ItemTemplate>
						</asp:TemplateColumn>
						<asp:TemplateColumn HeaderText="Select">
							<ItemStyle HorizontalAlign="Center" Width="50px"></ItemStyle>
							<ItemTemplate>
								<asp:CheckBox ID="cbSelected" Runat="server" Enabled="true" OnCheckedChanged="Check_Clicked" AutoPostBack="True"></asp:CheckBox>
							</ItemTemplate>
						</asp:TemplateColumn>
					</Columns>
					<PagerStyle Visible="False"></PagerStyle>
				</asp:datagrid><uc1:datagridpager id="BlocksPager" runat="server"></uc1:datagridpager></DIV>
			<DIV style="WIDTH: 752px; POSITION: relative; HEIGHT: 220px" ms_positioning="GridLayout">
				<HR style="Z-INDEX: 101; LEFT: 7px; POSITION: absolute; TOP: 6px; HEIGHT: 1px" width="98%"
					SIZE="1">
				<asp:dropdownlist id="ddlArchiveLocation" style="Z-INDEX: 106; LEFT: 184px; POSITION: absolute; TOP: 29px"
					runat="server" Width="180px"></asp:dropdownlist>
				<asp:label id="lblArchivedDate" style="Z-INDEX: 110; LEFT: 411px; POSITION: absolute; TOP: 32px"
					runat="server">Archived Date</asp:label>
				<DIV style="Z-INDEX: 111; LEFT: 558px; WIDTH: 177px; POSITION: absolute; TOP: 32px; HEIGHT: 58px"><uc1:calendardate id="ctlArchivedDate" runat="server"></uc1:calendardate></DIV>
				<asp:textbox id="txtComment" style="Z-INDEX: 107; LEFT: 17px; POSITION: absolute; TOP: 88px"
					runat="server" Width="729px" TextMode="MultiLine" Height="76px"></asp:textbox>
				<asp:button id="btnUpdate" style="Z-INDEX: 103; LEFT: 20px; POSITION: absolute; TOP: 185px"
					runat="server" Text="Update Selected" Width="139px" Height="25" Enabled="False" CausesValidation="False"></asp:button>
				<asp:button id="btnSave" style="Z-INDEX: 104; LEFT: 641px; POSITION: absolute; TOP: 185px" runat="server"
					Text="Done" Width="97px" Height="25" CausesValidation="False" Enabled="False"></asp:button>
				<asp:button id="btnBack" style="Z-INDEX: 102; LEFT: 538px; POSITION: absolute; TOP: 185px" runat="server"
					Text="Cancel" Width="97" Height="25" CausesValidation="False"></asp:button><asp:label id="lblArchiveLocation" style="Z-INDEX: 105; LEFT: 17px; POSITION: absolute; TOP: 28px"
					runat="server">Archive Location</asp:label><asp:label id="lblComment" style="Z-INDEX: 108; LEFT: 17px; POSITION: absolute; TOP: 63px"
					runat="server">Comment:</asp:label>
				<HR style="Z-INDEX: 109; LEFT: 7px; POSITION: absolute; TOP: 175px; HEIGHT: 1px" width="98%"
					SIZE="1">
				<asp:requiredfieldvalidator id="rfvArchiveLocation" style="Z-INDEX: 112; LEFT: 365px; POSITION: absolute; TOP: 29px"
					runat="server" CssClass="ValidatorText" ToolTip="Required Field" ControlToValidate="ddlArchiveLocation">*</asp:requiredfieldvalidator>
				<asp:Label id="lblError" style="Z-INDEX: 113; LEFT: 726px; POSITION: absolute; TOP: 32px" runat="server"
					CssClass="ValidatorText" ToolTip="Required Field" Visible="False">*</asp:Label></DIV>
			<DIV id="ctlDIV" style="WIDTH: 742px; HEIGHT: 17px" runat="server"></DIV>
			<uc1:vlafooter id="VLAFooter1" runat="server"></uc1:vlafooter></form>
	</body>
</HTML>
