<%@ Page Language="vb" AutoEventWireup="false" Codebehind="ArchiveTissues.aspx.vb" Inherits="HistopathologySystem.ArchiveTissues" smartNavigation="True"%>
<%@ Register TagPrefix="uc1" TagName="DataGridPager" Src="DataGridPager.ascx" %>
<%@ Register TagPrefix="uc1" TagName="CalendarDate" Src="CalendarDate.ascx" %>
<%@ Register TagPrefix="uc1" TagName="Batch" Src="Batch.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAHeader" Src="VLAHeader.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAFooter" Src="VLAFooter.ascx" %>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<HTML>
	<HEAD>
		<title>ArchiveTissues</title>
		<meta content="Microsoft Visual Studio.NET 7.0" name="GENERATOR">
		<meta content="Visual Basic 7.0" name="CODE_LANGUAGE">
		<meta content="JavaScript" name="vs_defaultClientScript">
		<meta content="http://schemas.microsoft.com/intellisense/ie5" name="vs_targetSchema">
		<LINK href="Style/vla-ie.css" type="text/css" rel="stylesheet">
	</HEAD>
	<body>
		<form id="Form1" method="post" runat="server">
			<uc1:vlaheader id="VLAHeader1" runat="server"></uc1:vlaheader>
			<DIV style="WIDTH: 108px; HEIGHT: 58px"><uc1:batch id="Batch1" runat="server"></uc1:batch></DIV>
			<DIV style="WIDTH: 768px; POSITION: relative; HEIGHT: 73px" ms_positioning="GridLayout">
				<asp:radiobuttonlist id="rblFilter" style="Z-INDEX: 103; LEFT: 8px; POSITION: absolute; TOP: 1px" runat="server"
					AutoPostBack="True" CssClass="Body" RepeatDirection="Horizontal"></asp:radiobuttonlist>
				<asp:dropdownlist id="ddlHistologyRefList" style="Z-INDEX: 105; LEFT: 227px; POSITION: absolute; TOP: 8px"
					runat="server" Width="156px"></asp:dropdownlist>
				<asp:dropdownlist id="ddlTissueList" style="Z-INDEX: 107; LEFT: 454px; POSITION: absolute; TOP: 8px"
					runat="server" Width="156"></asp:dropdownlist>
				<asp:button id="btnFilter" style="Z-INDEX: 100; LEFT: 621px; POSITION: absolute; TOP: 8px" runat="server"
					Text="Go" Width="56px" CausesValidation="False"></asp:button>
				<asp:TextBox id="txtPage" style="Z-INDEX: 108; LEFT: 119px; POSITION: absolute; TOP: 35px" runat="server"
					Width="75px" MaxLength="5"></asp:TextBox>
				<asp:Label id="lblPage" style="Z-INDEX: 109; LEFT: 17px; POSITION: absolute; TOP: 37px" runat="server">Go To Page</asp:Label>
				<asp:RequiredFieldValidator id="rfvPageNumber" style="Z-INDEX: 112; LEFT: 197px; POSITION: absolute; TOP: 35px"
					runat="server" CssClass="ValidatorText" ToolTip="Required Field" ControlToValidate="txtPage">*</asp:RequiredFieldValidator>
				<asp:Button id="btnGoToPage" style="Z-INDEX: 114; LEFT: 227px; POSITION: absolute; TOP: 35px"
					runat="server" CausesValidation="False" Text="Go to Page"></asp:Button>
				<asp:checkbox id="cbSelectAll" style="Z-INDEX: 104; LEFT: 680px; POSITION: absolute; TOP: 40px"
					runat="server" Text="All" AutoPostBack="True"></asp:checkbox>
				<asp:label id="lblHistoFilter" style="Z-INDEX: 101; LEFT: 119px; POSITION: absolute; TOP: 8px"
					runat="server">Histology Ref</asp:label><asp:label id="lblTissueFilter" style="Z-INDEX: 102; LEFT: 398px; POSITION: absolute; TOP: 8px"
					runat="server">Tissue</asp:label>
				<asp:customvalidator id="revPageNumber" style="Z-INDEX: 118; LEFT: 197px; POSITION: absolute; TOP: 35px"
					runat="server" CssClass="ValidatorText" ControlToValidate="txtPage" ToolTip="Must be numeric" OnServerValidate="ValidatePageNumber"
					ClientValidationFunction="ClientValidatePageNumber">*</asp:customvalidator>
			</DIV>
			<DIV style="WIDTH: 736px"><asp:datagrid id="grdTissues" runat="server" AutoGenerateColumns="False" PageSize="12" AllowPaging="True"
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
						<asp:BoundColumn Visible="False" DataField="AnimalID" SortExpression="AnimalID" HeaderText="AnimalID">
							<ItemStyle HorizontalAlign="Left" Width="100px"></ItemStyle>
						</asp:BoundColumn>
						<asp:BoundColumn DataField="SenderRef" SortExpression="SenderRef" HeaderText="Sender Ref">
							<ItemStyle HorizontalAlign="Left" Width="120px"></ItemStyle>
						</asp:BoundColumn>
						<asp:BoundColumn DataField="HistologyRef" SortExpression="HistologyRef" HeaderText="HistologyRef">
							<ItemStyle HorizontalAlign="Left" Width="120px"></ItemStyle>
						</asp:BoundColumn>
						<asp:BoundColumn DataField="TissueCode" SortExpression="Tissue" HeaderText="Tissue">
							<ItemStyle HorizontalAlign="Left" Width="200px"></ItemStyle>
						</asp:BoundColumn>
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
							<ItemStyle HorizontalAlign="Left" Width="50px"></ItemStyle>
							<ItemTemplate>
								<asp:CheckBox ID="cbSelected" Runat="server" Enabled="true" OnCheckedChanged="Check_Clicked" AutoPostBack="True"></asp:CheckBox>
							</ItemTemplate>
						</asp:TemplateColumn>
					</Columns>
					<PagerStyle Visible="False"></PagerStyle>
				</asp:datagrid><uc1:datagridpager id="TissuesPager" runat="server"></uc1:datagridpager></DIV>
			<DIV style="WIDTH: 755px; POSITION: relative; HEIGHT: 224px" ms_positioning="GridLayout">
				<HR style="Z-INDEX: 103; LEFT: 7px; POSITION: absolute; TOP: 7px" width="98%" SIZE="1">
				<asp:dropdownlist id="ddlArchiveLocation" style="Z-INDEX: 107; LEFT: 184px; POSITION: absolute; TOP: 19px"
					runat="server" Width="172px"></asp:dropdownlist>
				<DIV style="Z-INDEX: 111; LEFT: 558px; WIDTH: 148px; POSITION: absolute; TOP: 19px; HEIGHT: 48px"><uc1:calendardate id="ctlArchiveDate" runat="server"></uc1:calendardate></DIV>
				<asp:textbox id="txtComment" style="Z-INDEX: 104; LEFT: 17px; POSITION: absolute; TOP: 88px"
					runat="server" Width="729px" Height="76px" TextMode="MultiLine"></asp:textbox><asp:label id="lblArchiveLocation" style="Z-INDEX: 105; LEFT: 17px; POSITION: absolute; TOP: 19px"
					runat="server">Archive Location</asp:label><asp:label id="lblComment" style="Z-INDEX: 106; LEFT: 17px; POSITION: absolute; TOP: 52px"
					runat="server">Comment:</asp:label><asp:requiredfieldvalidator id="rfvArchiveLocation" style="Z-INDEX: 108; LEFT: 357px; POSITION: absolute; TOP: 19px"
					runat="server" ControlToValidate="ddlArchiveLocation" ToolTip="Required Field" CssClass="ValidatorText">*</asp:requiredfieldvalidator>
				<asp:button id="btnUpdateSelected" style="Z-INDEX: 112; LEFT: 17px; POSITION: absolute; TOP: 190px"
					runat="server" Text="Update Selected" Enabled="False" CausesValidation="False"></asp:button>
				<asp:button id="btnSave" style="Z-INDEX: 102; LEFT: 637px; POSITION: absolute; TOP: 190px" runat="server"
					Text="Done" Width="98px" Height="25px" CausesValidation="False" Enabled="False"></asp:button>
				<asp:button id="btnCancel" style="Z-INDEX: 101; LEFT: 532px; POSITION: absolute; TOP: 190px"
					runat="server" Text="Cancel" Width="98" Height="25" CausesValidation="False"></asp:button>
				<asp:label id="lblArchivedDate" style="Z-INDEX: 110; LEFT: 411px; POSITION: absolute; TOP: 19px"
					runat="server">Archived Date</asp:label>
				<HR style="Z-INDEX: 109; LEFT: 7px; POSITION: absolute; TOP: 176px" width="98%" SIZE="1">
				<asp:Label id="lblError" style="Z-INDEX: 113; LEFT: 726px; POSITION: absolute; TOP: 17px" runat="server"
					CssClass="ValidatorText" Visible="False" ToolTip="Required Field">*</asp:Label>
			</DIV>
			<DIV id="ctlDIV" style="WIDTH: 746px; HEIGHT: 2px" runat="server"></DIV>
			<uc1:vlafooter id="VLAFooter1" runat="server"></uc1:vlafooter></form>
	</body>
</HTML>
