<%@ Register TagPrefix="uc1" TagName="DataGridPager" Src="DataGridPager.ascx" %>
<%@ Page Language="vb" AutoEventWireup="false" Codebehind="SearchSubmissions.aspx.vb" Inherits="HistopathologySystem.SearchSubmissions" smartNavigation="True"%>
<%@ Register TagPrefix="uc1" TagName="CalendarDate" Src="CalendarDate.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAHeader" Src="VLAHeader.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAFooter" Src="VLAFooter.ascx" %>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<HTML>
	<HEAD>
		<title>SearchSubmissions</title>
		<meta name="vs_snapToGrid" content="False">
		<META content="Microsoft Visual Studio.NET 7.0" name="GENERATOR">
		<META content="Visual Basic 7.0" name="CODE_LANGUAGE">
		<META content="JavaScript" name="vs_defaultClientScript">
		<META content="http://schemas.microsoft.com/intellisense/ie5" name="vs_targetSchema">
		<LINK href="Style/vla-ie.css" type="text/css" rel="stylesheet">
	</HEAD>
	<BODY>
		<FORM id="Form1" method="post" runat="server">
			<uc1:vlaheader id="VLAHeader1" runat="server"></uc1:vlaheader>
			<DIV style="Z-INDEX: 160; WIDTH: 790px; POSITION: relative; HEIGHT: 314px" ms_positioning="GridLayout"><asp:textbox id="txtSubmissionID" style="Z-INDEX: 116; LEFT: 216px; POSITION: absolute; TOP: 22px"
					runat="server" Width="160" Height="25px"></asp:textbox><asp:dropdownlist id="ddlUserArea" style="Z-INDEX: 107; LEFT: 609px; POSITION: absolute; TOP: 22px"
					runat="server" Width="161" Height="25"></asp:dropdownlist><asp:dropdownlist id="ddlStatus" style="Z-INDEX: 119; LEFT: 216px; POSITION: absolute; TOP: 54px"
					runat="server" Width="161" Height="25"></asp:dropdownlist><asp:dropdownlist id="ddlSubmittedBy" style="Z-INDEX: 132; LEFT: 609px; POSITION: absolute; TOP: 52px"
					runat="server" Width="161px" Height="25px"></asp:dropdownlist><asp:dropdownlist id="ddlProject" style="Z-INDEX: 133; LEFT: 216px; POSITION: absolute; TOP: 83px"
					runat="server" Width="161px" Height="25px"></asp:dropdownlist><asp:dropdownlist id="ddlEnteredBy" style="Z-INDEX: 127; LEFT: 609px; POSITION: absolute; TOP: 83px"
					runat="server" Width="160px"></asp:dropdownlist><asp:dropdownlist id="ddlContact" style="Z-INDEX: 134; LEFT: 216px; POSITION: absolute; TOP: 112px"
					runat="server" Width="161px" Height="25px"></asp:dropdownlist><asp:textbox id="txtHistRef" style="Z-INDEX: 110; LEFT: 609px; POSITION: absolute; TOP: 112px"
					runat="server" Width="160" Height="25px" MaxLength="20"></asp:textbox><asp:dropdownlist id="ddlSpecies" style="Z-INDEX: 103; LEFT: 216px; POSITION: absolute; TOP: 141px"
					runat="server" Width="161" Height="25"></asp:dropdownlist><asp:textbox id="txtSenderRef" style="Z-INDEX: 114; LEFT: 609px; POSITION: absolute; TOP: 141px"
					runat="server" Width="159px" Height="25px" MaxLength="20"></asp:textbox><asp:dropdownlist id="ddlFixation" style="Z-INDEX: 104; LEFT: 216px; POSITION: absolute; TOP: 170px"
					runat="server" Width="161" Height="25"></asp:dropdownlist><asp:label id="lblProjectCode" style="Z-INDEX: 100; LEFT: 21px; POSITION: absolute; TOP: 83px"
					runat="server" Height="18px">Project or Contract code</asp:label><asp:label id="lblContactName" style="Z-INDEX: 101; LEFT: 21px; POSITION: absolute; TOP: 112px"
					runat="server" Height="18px"> Pathologist</asp:label><asp:label id="lblSpecies" style="Z-INDEX: 102; LEFT: 21px; POSITION: absolute; TOP: 141px"
					runat="server" Height="18px">Species</asp:label><asp:label id="lblSubmittedBy" style="Z-INDEX: 105; LEFT: 445px; POSITION: absolute; TOP: 54px"
					runat="server">Submitted By</asp:label><asp:label id="lblSubmittedArea" style="Z-INDEX: 106; LEFT: 445px; POSITION: absolute; TOP: 22px"
					runat="server">Submitted Area</asp:label><asp:label id="lblFixation" style="Z-INDEX: 108; LEFT: 21px; POSITION: absolute; TOP: 170px"
					runat="server">Fixation</asp:label><asp:label id="lblHistRef" style="Z-INDEX: 111; LEFT: 445px; POSITION: absolute; TOP: 112px"
					runat="server">Histology Ref</asp:label>
				<HR style="Z-INDEX: 113; LEFT: 13px; WIDTH: 97.54%; POSITION: absolute; TOP: 307px; HEIGHT: 1px"
					width="97.54%" SIZE="1">
				<asp:label id="lblSenderRef" style="Z-INDEX: 115; LEFT: 445px; POSITION: absolute; TOP: 141px"
					runat="server">Sender Ref</asp:label><asp:label id="lblSubmissionNumber" style="Z-INDEX: 117; LEFT: 21px; POSITION: absolute; TOP: 22px"
					runat="server">Submission Number</asp:label><asp:regularexpressionvalidator id="revSubmissionNumber" style="Z-INDEX: 118; LEFT: 376px; POSITION: absolute; TOP: 22px"
					runat="server" CssClass="ValidatorText" ValidationExpression="^[1-9]+[0-9]*$" ToolTip="Must be numeric" ControlToValidate="txtSubmissionID">*</asp:regularexpressionvalidator><asp:label id="lblStatus" style="Z-INDEX: 120; LEFT: 21px; POSITION: absolute; TOP: 54px" runat="server"
					Width="58px">Status</asp:label>
				<DIV style="Z-INDEX: 130; LEFT: 216px; POSITION: absolute; TOP: 199px" ms_positioning="FlowLayout"><uc1:calendardate id="ctlSubmittedDateFrom" runat="server"></uc1:calendardate></DIV>
				<DIV style="Z-INDEX: 131; LEFT: 445px; POSITION: absolute; TOP: 199px" ms_positioning="FlowLayout"><uc1:calendardate id="ctlSubmittedDateTo" runat="server"></uc1:calendardate></DIV>
				<DIV style="Z-INDEX: 128; LEFT: 216px; POSITION: absolute; TOP: 228px" ms_positioning="FlowLayout"><uc1:calendardate id="ctlReceivedDateFrom" runat="server"></uc1:calendardate></DIV>
				<DIV style="Z-INDEX: 129; LEFT: 445px; POSITION: absolute; TOP: 228px" ms_positioning="FlowLayout"><uc1:calendardate id="ctlReceivedDateTo" runat="server"></uc1:calendardate></DIV>
				<asp:button id="btnSearch" style="Z-INDEX: 109; LEFT: 445px; POSITION: absolute; TOP: 276px"
					runat="server" Width="102px" Height="25" CausesValidation="False" Text="Search"></asp:button>
				<asp:button id="btnClearFilter" style="Z-INDEX: 109; LEFT: 553px; POSITION: absolute; TOP: 276px"
					runat="server" Width="102" Height="25" CausesValidation="False" Text="Clear Search"></asp:button>
				<asp:button id="btnSearchMenu" style="Z-INDEX: 112; LEFT: 663px; POSITION: absolute; TOP: 276px"
					runat="server" Width="102px" Height="25" Text="Done"></asp:button><asp:label id="lblSubmittedDateFrom" style="Z-INDEX: 124; LEFT: 21px; POSITION: absolute; TOP: 199px"
					runat="server">Submitted Date Between</asp:label><asp:label id="lblAnd1" style="Z-INDEX: 122; LEFT: 397px; POSITION: absolute; TOP: 207px" runat="server">and</asp:label><asp:label id="lblReceivedFrom" style="Z-INDEX: 125; LEFT: 21px; POSITION: absolute; TOP: 228px"
					runat="server">Received Date Between</asp:label><asp:label id="lblAnd2" style="Z-INDEX: 123; LEFT: 397px; POSITION: absolute; TOP: 238px" runat="server">and</asp:label><asp:label id="lblEnteredBy" style="Z-INDEX: 126; LEFT: 445px; POSITION: absolute; TOP: 83px"
					runat="server">Entered By</asp:label></DIV>
			<DIV id="ctlDiv" style="WIDTH: 789px; HEIGHT: 5px" runat="server"></DIV>
			<DIV style="WIDTH: 754px">
				<P><asp:datagrid id="grdSearchResults" runat="server" AllowPaging="True" AllowSorting="True" AutoGenerateColumns="False">
						<SelectedItemStyle CssClass="GridSelectedItemSmall"></SelectedItemStyle>
						<EditItemStyle CssClass="GridEditItemSmall"></EditItemStyle>
						<ItemStyle CssClass="GridItemSmall"></ItemStyle>
						<HeaderStyle CssClass="GridHeader"></HeaderStyle>
						<Columns>
							<asp:ButtonColumn Text="&lt;img src=&quot;Images/GridPager/sel.gif&quot;&gt;" CommandName="Select">
								<ItemStyle HorizontalAlign="Left" Width="20px"></ItemStyle>
							</asp:ButtonColumn>
							<asp:BoundColumn DataField="ID" SortExpression="ID" HeaderText="Submission Number">
								<ItemStyle HorizontalAlign="Left" Width="120px"></ItemStyle>
							</asp:BoundColumn>
							<asp:BoundColumn DataField="ProjectDescription" SortExpression="ProjectDescription" HeaderText="Project Code">
								<ItemStyle HorizontalAlign="Left" Width="145px"></ItemStyle>
							</asp:BoundColumn>
							<asp:BoundColumn DataField="ContactDescription" SortExpression="ContactDescription" HeaderText="Pathologist">
								<ItemStyle HorizontalAlign="Left" Width="145px"></ItemStyle>
							</asp:BoundColumn>
							<asp:BoundColumn DataField="Species" SortExpression="Species" HeaderText="Species">
								<ItemStyle HorizontalAlign="Left" Width="100px"></ItemStyle>
							</asp:BoundColumn>
							<asp:BoundColumn DataField="BatchDate" SortExpression="BatchDate" HeaderText="Date" DataFormatString="{0:d}">
								<ItemStyle HorizontalAlign="Left" Width="80px"></ItemStyle>
							</asp:BoundColumn>
							<asp:BoundColumn DataField="Status" SortExpression="Status" HeaderText="Status">
								<ItemStyle HorizontalAlign="Left" Width="100px"></ItemStyle>
							</asp:BoundColumn>
							<asp:BoundColumn Visible="False" DataField="SubmittedBy"></asp:BoundColumn>
						</Columns>
						<PagerStyle Visible="False"></PagerStyle>
					</asp:datagrid></P>
			</DIV>
			<DIV style="WIDTH: 592px; POSITION: relative; HEIGHT: 38px" ms_positioning="GridLayout"><uc1:datagridpager id="SearchResultsPager" runat="server"></uc1:datagridpager></DIV>
			<asp:linkbutton id="lbExportExcel" style="Z-INDEX: 101; LEFT: 640px; POSITION: relative" runat="server"
				Width="128px" Visible="False">Export to Excel</asp:linkbutton>
			<DIV style="Z-INDEX: 150; WIDTH: 812px; POSITION: relative; HEIGHT: 54px" ms_positioning="GridLayout">
				<HR style="Z-INDEX: 101; LEFT: 10px; POSITION: absolute; TOP: 7px" width="98%" SIZE="1">
				<asp:button id="btnPrintSubmission" style="Z-INDEX: 102; LEFT: 4px; POSITION: absolute; TOP: 18px"
					runat="server" Width="122" Height="25" Text="Print Options" Enabled="False"></asp:button><asp:button id="btnEditSubmission" style="Z-INDEX: 103; LEFT: 130px; POSITION: absolute; TOP: 18px"
					runat="server" Width="123" Height="25px" Text="Edit Submission" Enabled="False"></asp:button><asp:button id="btnViewSubmission" style="Z-INDEX: 104; LEFT: 257px; POSITION: absolute; TOP: 18px"
					runat="server" Width="124" Height="25" Text="View Submission" Enabled="False"></asp:button><asp:button id="btnViewQualityData" style="Z-INDEX: 105; LEFT: 385px; POSITION: absolute; TOP: 18px"
					runat="server" Width="124px" Height="25" Text="View Quality Data" Enabled="False"></asp:button><asp:button id="btnViewArchiveData" style="Z-INDEX: 106; LEFT: 515px; POSITION: absolute; TOP: 18px"
					runat="server" Width="132" Height="25" Text="View Archive" Enabled="False"></asp:button><asp:button id="btnViewReceipt" style="Z-INDEX: 107; LEFT: 652px; POSITION: absolute; TOP: 18px"
					runat="server" Width="132px" Height="25px" Text="View Receipt" Enabled="False"></asp:button></DIV>
			<uc1:vlafooter id="VLAFooter1" runat="server"></uc1:vlafooter></FORM>
	</BODY>
</HTML>
