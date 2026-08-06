<%@ Page Language="vb" AutoEventWireup="false" Codebehind="SearchArchiveLocation.aspx.vb" Inherits="HistopathologySystem.SearchArchiveLocation" smartNavigation="True"%>
<%@ Register TagPrefix="uc1" TagName="DataGridPager" Src="DataGridPager.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAHeader" Src="VLAHeader.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAFooter" Src="VLAFooter.ascx" %>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<HTML>
	<HEAD>
		<title>Search Archive Location</title>
		<meta content="Microsoft Visual Studio.NET 7.0" name="GENERATOR">
		<meta content="Visual Basic 7.0" name="CODE_LANGUAGE">
		<meta content="JavaScript" name="vs_defaultClientScript">
		<meta content="http://schemas.microsoft.com/intellisense/ie5" name="vs_targetSchema">
		<LINK href="Style/vla-ie.css" type="text/css" rel="stylesheet">
	</HEAD>
	<body>
		<form id="Form1" method="post" runat="server">
			<uc1:vlaheader id="VLAHeader1" runat="server"></uc1:vlaheader>
			<DIV style="WIDTH: 735px; POSITION: relative; HEIGHT: 182px" ms_positioning="GridLayout">
				<HR style="Z-INDEX: 103; LEFT: 12px; WIDTH: 96.86%; POSITION: absolute; TOP: 176px; HEIGHT: 1px"
					width="96.86%" SIZE="1">
				<asp:label id="lblHistRef" style="Z-INDEX: 104; LEFT: 20px; POSITION: absolute; TOP: 14px"
					runat="server">Histology Ref</asp:label><asp:textbox id="txtHistRef" style="Z-INDEX: 105; LEFT: 173px; POSITION: absolute; TOP: 14px"
					runat="server" MaxLength="20" Height="25px" Width="153px"></asp:textbox><asp:textbox id="txtSenderRef" style="Z-INDEX: 108; LEFT: 173px; POSITION: absolute; TOP: 49px"
					runat="server" Height="25" Width="153"></asp:textbox><asp:dropdownlist id="ddlArchiveLocation" style="Z-INDEX: 110; LEFT: 173px; POSITION: absolute; TOP: 83px"
					runat="server" Height="25" Width="154px"></asp:dropdownlist><asp:radiobutton id="rbTissues" style="Z-INDEX: 112; LEFT: 367px; POSITION: absolute; TOP: 14px"
					runat="server" Text="Tissue Archive" AutoPostBack="True"></asp:radiobutton><asp:dropdownlist id="ddlTissue" style="Z-INDEX: 114; LEFT: 510px; POSITION: absolute; TOP: 12px"
					runat="server" Height="25px" Width="209px"></asp:dropdownlist><asp:radiobutton id="rbBlock" style="Z-INDEX: 111; LEFT: 367px; POSITION: absolute; TOP: 49px" runat="server"
					Text="Block Archive" AutoPostBack="True"></asp:radiobutton><asp:textbox id="txtBlockRef" style="Z-INDEX: 115; LEFT: 510px; POSITION: absolute; TOP: 49px"
					runat="server" Width="56px"></asp:textbox><asp:radiobutton id="rbSlide" style="Z-INDEX: 113; LEFT: 367px; POSITION: absolute; TOP: 83px" runat="server"
					Text="Slide Archive" AutoPostBack="True"></asp:radiobutton><asp:label id="lblError" style="Z-INDEX: 106; LEFT: 331px; POSITION: absolute; TOP: 14px" runat="server"
					CssClass="ValidatorText" ToolTip="Must enter either the Sender Ref or HistologyRef">*</asp:label><asp:button id="btnSearch" style="Z-INDEX: 101; LEFT: 507px; POSITION: absolute; TOP: 111px"
					runat="server" Width="108px" Text="Search" CausesValidation="False"></asp:button><asp:button id="btnDone" style="Z-INDEX: 102; LEFT: 628px; POSITION: absolute; TOP: 111px" runat="server"
					Width="88px" Text="Done"></asp:button><asp:label id="lblSenderRef" style="Z-INDEX: 107; LEFT: 20px; POSITION: absolute; TOP: 49px"
					runat="server">Sender Ref</asp:label><asp:label id="lblArchiveLocation" style="Z-INDEX: 109; LEFT: 20px; POSITION: absolute; TOP: 83px"
					runat="server">Archive Location</asp:label><asp:customvalidator id="revBlockArchive" style="Z-INDEX: 116; LEFT: 570px; POSITION: absolute; TOP: 49px"
					runat="server" CssClass="ValidatorText" ToolTip="Enter a value from 1 to 999. Numbers below 10 must be entered with the leading zero i.e. 01 to 09"
					ClientValidationFunction="ClientValidateBlockRef" OnServerValidate="ValidateBlockRefRef" ControlToValidate="txtBlockRef">*</asp:customvalidator>
				<DIV id="ctlDivGrid" style="Z-INDEX: 117; LEFT: 8px; WIDTH: 499px; POSITION: absolute; TOP: 144px; HEIGHT: 20px"
					runat="server" ms_positioning="FlowLayout"><asp:linkbutton id="lbExpandAll" runat="server">Click here to expand all tissues, or</asp:linkbutton>&nbsp;
					<asp:linkbutton id="lbCollapseAll" runat="server">click here to collapse all tissues.</asp:linkbutton></DIV>
			</DIV>
			<DIV id="ctlDiv" style="WIDTH: 733px; HEIGHT: 6px" runat="server"></DIV>
			<DIV id="ctlDivBlockArchive" style="WIDTH: 824px" runat="server"><asp:datagrid id="grdBlockArchive" runat="server" AutoGenerateColumns="False">
					<SelectedItemStyle CssClass="GridSelectedItemSmall"></SelectedItemStyle>
					<EditItemStyle CssClass="GridEditItemSmall"></EditItemStyle>
					<ItemStyle CssClass="GridItemSmall"></ItemStyle>
					<HeaderStyle CssClass="GridHeader"></HeaderStyle>
					<Columns>
						<asp:BoundColumn Visible="True" DataField="ID" HeaderText="Submission Number">
							<ItemStyle Width="100px"></ItemStyle>
						</asp:BoundColumn>
						<asp:BoundColumn Visible="False" DataField="NewID" HeaderText="NewID"></asp:BoundColumn>
						<asp:BoundColumn DataField="BlockRef" HeaderText="Block Ref">
							<ItemStyle Width="50px"></ItemStyle>
						</asp:BoundColumn>
						<asp:BoundColumn DataField="ArchiveLocation" HeaderText="Archive Location">
							<ItemStyle Width="150px"></ItemStyle>
						</asp:BoundColumn>
						<asp:BoundColumn DataField="ArchivedDate" HeaderText="Archived Date">
							<ItemStyle Width="150px"></ItemStyle>
						</asp:BoundColumn>
						<asp:ButtonColumn Text="&lt;img src=&quot;Images/plus.gif&quot;&gt;" CommandName="ExpandTissues">
							<ItemStyle Width="20px"></ItemStyle>
						</asp:ButtonColumn>
						<asp:BoundColumn DataField="TissueDescription" HeaderText="Tissue">
							<ItemStyle Width="250px"></ItemStyle>
						</asp:BoundColumn>
						<asp:BoundColumn DataField="NoPieces" HeaderText="No Pieces">
							<ItemStyle Width="50px"></ItemStyle>
						</asp:BoundColumn>
					</Columns>
					<PagerStyle Visible="False"></PagerStyle>
				</asp:datagrid></DIV>
			<DIV id="ctlDivTissueArchive" style="WIDTH: 680px" runat="server">
				<asp:datagrid id="grdTissueArchive" runat="server" Width="800px" AllowSorting="false" AutoGenerateColumns="False">
					<SelectedItemStyle CssClass="GridSelectedItemSmall"></SelectedItemStyle>
					<EditItemStyle CssClass="GridEditItemSmall"></EditItemStyle>
					<ItemStyle CssClass="GridItemSmall"></ItemStyle>
					<HeaderStyle CssClass="GridHeader"></HeaderStyle>
					<Columns>
						<asp:BoundColumn DataField="BatchID" HeaderText="Submission Number">
							<ItemStyle Width="200px"></ItemStyle>
						</asp:BoundColumn>
						<asp:ButtonColumn Text="&lt;img src=&quot;Images/plus.gif&quot;&gt;" CommandName="ExpandTissues">
							<ItemStyle Width="20px"></ItemStyle>
						</asp:ButtonColumn>
						<asp:BoundColumn DataField="TissueDescription" HeaderText="Tissue">
							<ItemStyle Width="350px"></ItemStyle>
						</asp:BoundColumn>
						<asp:BoundColumn DataField="ArchiveLocation" HeaderText="Archive Location">
							<ItemStyle Width="200px"></ItemStyle>
						</asp:BoundColumn>
						<asp:BoundColumn DataField="ArchivedDate" HeaderText="Archived Date">
							<ItemStyle Width="150px"></ItemStyle>
						</asp:BoundColumn>
						<asp:BoundColumn DataField="NoPieces" HeaderText="NoPieces">
							<ItemStyle Width="50px"></ItemStyle>
						</asp:BoundColumn>
						<asp:BoundColumn Visible="False" DataField="NewID"></asp:BoundColumn>
					</Columns>
					<PagerStyle Visible="False"></PagerStyle>
				</asp:datagrid>
			</DIV>
			<DIV id="ctlDivSlideArchive" style="WIDTH: 816px" runat="server"><asp:datagrid id="grdSlideArchive" runat="server" Width="800px" AutoGenerateColumns="False">
					<SelectedItemStyle CssClass="GridSelectedItemSmall"></SelectedItemStyle>
					<EditItemStyle CssClass="GridEditItemSmall"></EditItemStyle>
					<ItemStyle CssClass="GridItemSmall"></ItemStyle>
					<HeaderStyle CssClass="GridHeader"></HeaderStyle>
					<Columns>
						<asp:BoundColumn DataField="BatchID" HeaderText="Submission Number">
							<ItemStyle Width="150px"></ItemStyle>
						</asp:BoundColumn>
						<asp:BoundColumn DataField="BlockRef" HeaderText="Block Ref">
							<ItemStyle Width="50px"></ItemStyle>
						</asp:BoundColumn>
						<asp:BoundColumn DataField="ArchiveLocation" HeaderText="Archive Location">
							<ItemStyle Width="200px"></ItemStyle>
						</asp:BoundColumn>
						<asp:BoundColumn DataField="ArchivedDate" HeaderText="Archived Date">
							<ItemStyle Width="150px"></ItemStyle>
						</asp:BoundColumn>
						<asp:BoundColumn DataField="Description" HeaderText="Slide">
							<ItemStyle Width="150px"></ItemStyle>
						</asp:BoundColumn>
						<asp:ButtonColumn Text="&lt;img src=&quot;Images/plus.gif&quot;&gt;" CommandName="ExpandTissues"></asp:ButtonColumn>
						<asp:BoundColumn DataField="TissueDescription" HeaderText="Tissue">
							<ItemStyle Width="350px"></ItemStyle>
						</asp:BoundColumn>
						<asp:BoundColumn Visible="False" DataField="NewID"></asp:BoundColumn>
					</Columns>
					<PagerStyle Visible="False"></PagerStyle>
				</asp:datagrid></DIV>
			<p></p>
			<asp:hyperlink id="hlExcelExport" style="Z-INDEX: 101; LEFT: 640px; POSITION: relative" runat="server"
				Visible="False" Target="_blank" NavigateUrl="ExcelExport.aspx">Export to Excel</asp:hyperlink>
			<P></P>
			<uc1:vlafooter id="VLAFooter1" runat="server"></uc1:vlafooter>
		</form>
	</body>
</HTML>
