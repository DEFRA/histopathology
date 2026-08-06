<%@ Register TagPrefix="uc1" TagName="DataGridPager" Src="DataGridPager.ascx" %>
<%@ Register TagPrefix="uc1" TagName="CalendarDate" Src="CalendarDate.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAFooter" Src="VLAFooter.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAHeader" Src="VLAHeader.ascx" %>
<%@ Page Language="vb" AutoEventWireup="false" Codebehind="ViewSubmissions.aspx.vb" Inherits="HistopathologySystem.ViewSubmissions" smartNavigation="True"%>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<HTML>
	<HEAD>
		<title>ViewSubmissions</title>
		<META content="Microsoft Visual Studio.NET 7.0" name="GENERATOR">
		<META content="Visual Basic 7.0" name="CODE_LANGUAGE">
		<META content="JavaScript" name="vs_defaultClientScript">
		<META content="http://schemas.microsoft.com/intellisense/ie5" name="vs_targetSchema">
		<LINK href="Style/vla-ie.css" type="text/css" rel="stylesheet">
	</HEAD>
	<BODY>
		<FORM id="Form1" method="post" runat="server">
			<uc1:vlaheader id="VLAHeader1" runat="server"></uc1:vlaheader>
			<DIV style="WIDTH: 814px; POSITION: relative; HEIGHT: 285px" ms_positioning="GridLayout"><asp:textbox id="txtSubmissionID" style="Z-INDEX: 114; LEFT: 216px; POSITION: absolute; TOP: 19px"
					runat="server" Width="160" Height="27px"></asp:textbox><asp:dropdownlist id="ddlEnteredBy" style="Z-INDEX: 130; LEFT: 609px; POSITION: absolute; TOP: 19px"
					runat="server" Width="161" Height="25"></asp:dropdownlist><asp:dropdownlist id="ddlStatus" style="Z-INDEX: 117; LEFT: 216px; POSITION: absolute; TOP: 50px"
					runat="server" Width="161" Height="25"></asp:dropdownlist><asp:dropdownlist id="ddlSubmittedBy" style="Z-INDEX: 131; LEFT: 609px; POSITION: absolute; TOP: 50px"
					runat="server" Width="161px" Height="25px"></asp:dropdownlist><asp:dropdownlist id="ddlProject" style="Z-INDEX: 132; LEFT: 216px; POSITION: absolute; TOP: 79px"
					runat="server" Width="161px" Height="25px"></asp:dropdownlist><asp:dropdownlist id="ddlFixation" style="Z-INDEX: 105; LEFT: 609px; POSITION: absolute; TOP: 79px"
					runat="server" Width="160px" Height="29px"></asp:dropdownlist><asp:dropdownlist id="ddlContact" style="Z-INDEX: 133; LEFT: 216px; POSITION: absolute; TOP: 108px"
					runat="server" Width="161px" Height="25px"></asp:dropdownlist><asp:textbox id="txtHistRef" style="Z-INDEX: 109; LEFT: 609px; POSITION: absolute; TOP: 108px"
					runat="server" Width="160" Height="27" MaxLength="20"></asp:textbox><asp:dropdownlist id="ddlSpecies" style="Z-INDEX: 104; LEFT: 216px; POSITION: absolute; TOP: 139px"
					runat="server" Width="161" Height="25"></asp:dropdownlist><asp:textbox id="txtSenderRef" style="Z-INDEX: 112; LEFT: 609px; POSITION: absolute; TOP: 139px"
					runat="server" Width="159px" Height="27" MaxLength="20"></asp:textbox><asp:label id="lblProjectCode" style="Z-INDEX: 101; LEFT: 21px; POSITION: absolute; TOP: 79px"
					runat="server" Height="18px">Project or Contract code</asp:label><asp:label id="lblContactName" style="Z-INDEX: 102; LEFT: 21px; POSITION: absolute; TOP: 108px"
					runat="server" Height="18px"> Pathologist</asp:label><asp:label id="lblSpecies" style="Z-INDEX: 103; LEFT: 21px; POSITION: absolute; TOP: 139px"
					runat="server" Height="18px">Species</asp:label>
				<DIV style="Z-INDEX: 108; LEFT: 609px; POSITION: absolute; TOP: 51px"></DIV>
				<asp:label id="lblSubmittedBy" style="Z-INDEX: 106; LEFT: 445px; POSITION: absolute; TOP: 50px"
					runat="server">Submitted By</asp:label><asp:label id="lblFixation" style="Z-INDEX: 107; LEFT: 445px; POSITION: absolute; TOP: 79px"
					runat="server">Fixation</asp:label><asp:label id="lblHistRef" style="Z-INDEX: 110; LEFT: 445px; POSITION: absolute; TOP: 108px"
					runat="server">Histology Ref</asp:label>
				<HR style="Z-INDEX: 111; LEFT: 13px; WIDTH: 97.54%; POSITION: absolute; TOP: 276px; HEIGHT: 1px"
					width="97.54%" SIZE="1">
				<asp:label id="lblSenderRef" style="Z-INDEX: 113; LEFT: 445px; POSITION: absolute; TOP: 139px"
					runat="server">Sender Ref</asp:label><asp:label id="lblSubmissionNumber" style="Z-INDEX: 115; LEFT: 21px; POSITION: absolute; TOP: 19px"
					runat="server">Submission Number</asp:label><asp:regularexpressionvalidator id="revSubmissionNumber" style="Z-INDEX: 116; LEFT: 376px; POSITION: absolute; TOP: 19px"
					runat="server" ValidationExpression="^[1-9]+[0-9]*$" ToolTip="Must be numeric" ControlToValidate="txtSubmissionID" CssClass="ValidatorText">*</asp:regularexpressionvalidator><asp:label id="lblStatus" style="Z-INDEX: 118; LEFT: 21px; POSITION: absolute; TOP: 50px" runat="server"
					Width="58px">Status</asp:label>
				<DIV style="Z-INDEX: 127; LEFT: 216px; POSITION: absolute; TOP: 168px" ms_positioning="FlowLayout"><uc1:calendardate id="ctlSubmittedDateFrom" runat="server"></uc1:calendardate></DIV>
				<DIV style="Z-INDEX: 128; LEFT: 445px; POSITION: absolute; TOP: 168px" ms_positioning="FlowLayout"><uc1:calendardate id="ctlSubmittedDateTo" runat="server"></uc1:calendardate></DIV>
				<DIV style="Z-INDEX: 124; LEFT: 216px; POSITION: absolute; TOP: 199px" ms_positioning="FlowLayout"><uc1:calendardate id="ctlReceivedDateFrom" runat="server"></uc1:calendardate></DIV>
				<DIV style="Z-INDEX: 126; LEFT: 445px; POSITION: absolute; TOP: 199px" ms_positioning="FlowLayout"><uc1:calendardate id="ctlReceivedDateTo" runat="server"></uc1:calendardate></DIV>
				<asp:button id="btnview" style="Z-INDEX: 121; LEFT: 447px; POSITION: absolute; TOP: 242px" runat="server"
					Width="158px" Height="25px" Text="Search"></asp:button><asp:button id="btnClearFilter" style="Z-INDEX: 122; LEFT: 609px; POSITION: absolute; TOP: 242px"
					runat="server" Width="158" Height="25" Text="Clear Search" CausesValidation="False"></asp:button><asp:label id="lblSubmittedDateFrom" style="Z-INDEX: 123; LEFT: 21px; POSITION: absolute; TOP: 168px"
					runat="server">Submitted Date Between</asp:label><asp:label id="lblAnd1" style="Z-INDEX: 119; LEFT: 397px; POSITION: absolute; TOP: 168px" runat="server">and</asp:label><asp:label id="lblReceivedFrom" style="Z-INDEX: 125; LEFT: 21px; POSITION: absolute; TOP: 199px"
					runat="server">Received Date Between</asp:label><asp:label id="lblAnd2" style="Z-INDEX: 120; LEFT: 397px; POSITION: absolute; TOP: 199px" runat="server">and</asp:label><asp:label id="lblEnteredBy" style="Z-INDEX: 129; LEFT: 445px; POSITION: absolute; TOP: 19px"
					runat="server">Entered By</asp:label></DIV>
			<DIV id="ctlDiv" style="WIDTH: 811px; HEIGHT: 9px" runat="server"></DIV>
			<DIV style="WIDTH: 888px">
				<P><asp:datagrid id="grdviewResults" runat="server" AllowPaging="True" AllowSorting="True" AutoGenerateColumns="False">
						<SelectedItemStyle CssClass="GridSelectedItemSmall"></SelectedItemStyle>
						<EditItemStyle CssClass="GridEditItemSmall"></EditItemStyle>
						<ItemStyle CssClass="GridItemSmall"></ItemStyle>
						<HeaderStyle CssClass="GridHeaderSmall"></HeaderStyle>
						<Columns>
							<asp:ButtonColumn Text="&lt;img src=&quot;Images/GridPager/sel.gif&quot;&gt;" CommandName="Select">
								<ItemStyle HorizontalAlign="Left" Width="20px"></ItemStyle>
							</asp:ButtonColumn>
							<asp:BoundColumn DataField="ID" SortExpression="ID" HeaderText="Sub. Number">
								<ItemStyle HorizontalAlign="Left" Width="65px"></ItemStyle>
							</asp:BoundColumn>
							<asp:BoundColumn DataField="ProjectDescription" SortExpression="ProjectDescription" HeaderText="Project Code">
								<ItemStyle HorizontalAlign="Left" Width="120px"></ItemStyle>
							</asp:BoundColumn>
							<asp:BoundColumn DataField="ContactDescription" SortExpression="ContactDescription" HeaderText="Pathologist">
								<ItemStyle HorizontalAlign="Left" Width="120px"></ItemStyle>
							</asp:BoundColumn>
							<asp:BoundColumn DataField="Species" SortExpression="Species" HeaderText="Species">
								<ItemStyle HorizontalAlign="Left" Width="100px"></ItemStyle>
							</asp:BoundColumn>
							<asp:BoundColumn DataField="BatchDate" SortExpression="BatchDate" HeaderText="Date Submitted" DataFormatString="{0:d}">
								<ItemStyle HorizontalAlign="Left" Width="85px"></ItemStyle>
							</asp:BoundColumn>
							<asp:BoundColumn DataField="DateReceived" SortExpression="DateReceived" HeaderText="Date Received / Rejected"
								DataFormatString="{0:d}">
								<ItemStyle HorizontalAlign="Left" Width="85px"></ItemStyle>
							</asp:BoundColumn>
							<asp:BoundColumn DataField="DateCompleted" SortExpression="DateCompleted" HeaderText="Date Completed"
								DataFormatString="{0:d}">
								<ItemStyle HorizontalAlign="Left" Width="85px"></ItemStyle>
							</asp:BoundColumn>
							<asp:BoundColumn DataField="CustomerReceivedDate" SortExpression="CustomerReceivedDate" HeaderText="Customer Received Date"
								DataFormatString="{0:d}">
								<ItemStyle HorizontalAlign="Left" Width="85px"></ItemStyle>
							</asp:BoundColumn>
							<asp:BoundColumn DataField="Status" SortExpression="Status" HeaderText="Status">
								<ItemStyle HorizontalAlign="Left" Width="80px"></ItemStyle>
							</asp:BoundColumn>
							<asp:BoundColumn Visible="False" DataField="SubmittedBy"></asp:BoundColumn>
						</Columns>
						<PagerStyle Visible="False"></PagerStyle>
					</asp:datagrid></P>
			</DIV>
			<DIV style="WIDTH: 682px; POSITION: relative; HEIGHT: 44px" ms_positioning="GridLayout"><uc1:datagridpager id="viewResultsPager" runat="server"></uc1:datagridpager></DIV>
			<asp:linkbutton id="lbExportExcel" style="Z-INDEX: 101; LEFT: 720px; POSITION: relative" runat="server"
				Width="110px" Visible="False">Export to Excel</asp:linkbutton>
			<DIV style="WIDTH: 880px; POSITION: relative; HEIGHT: 65px" ms_positioning="GridLayout">
				<HR style="Z-INDEX: 101; LEFT: 10px; POSITION: absolute; TOP: 7px" width="98%" SIZE="1">
				<asp:button id="btnPrintSubmission" style="Z-INDEX: 102; LEFT: 8px; POSITION: absolute; TOP: 16px"
					runat="server" Width="105" Height="25" Text="Print Submission" Enabled="False"></asp:button><asp:button id="btnCopySubmission" style="Z-INDEX: 103; LEFT: 273px; POSITION: absolute; TOP: 16px"
					runat="server" Width="105px" Height="25" Text="Copy Submission" Enabled="False"></asp:button><asp:button id="btnEditSubmission" style="Z-INDEX: 104; LEFT: 386px; POSITION: absolute; TOP: 16px"
					runat="server" Width="105" Height="25" Text="Edit Submission" Enabled="False"></asp:button><asp:button id="btnViewSubmission" style="Z-INDEX: 105; LEFT: 499px; POSITION: absolute; TOP: 16px"
					runat="server" Width="105" Height="25" Text="View Submission" Enabled="False"></asp:button><asp:button id="btnReceiveSubmission" style="Z-INDEX: 106; LEFT: 616px; POSITION: absolute; TOP: 16px"
					runat="server" Width="105" Height="25px" Text="Date Returned" Enabled="False"></asp:button>
				<asp:Button id="btnSubmissionNotes" style="Z-INDEX: 107; LEFT: 121px; POSITION: absolute; TOP: 16px"
					runat="server" Height="25px" Width="144px" Text="Print Submission Notes" Enabled="False"></asp:Button></DIV>
			<uc1:vlafooter id="VLAFooter1" runat="server"></uc1:vlafooter></FORM>
	</BODY>
</HTML>
