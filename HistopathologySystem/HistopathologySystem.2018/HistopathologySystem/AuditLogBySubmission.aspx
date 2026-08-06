<%@ Page Language="vb" AutoEventWireup="false" Codebehind="AuditLogBySubmission.aspx.vb" Inherits="HistopathologySystem.AuditLogBySubmission" smartNavigation="True"%>
<%@ Register TagPrefix="uc1" TagName="CalendarDate" Src="CalendarDate.ascx" %>
<%@ Register TagPrefix="uc1" TagName="DataGridPager" Src="DataGridPager.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAHeader" Src="VLAHeader.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAFooter" Src="VLAFooter.ascx" %>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<HTML>
	<HEAD>
		<title>Submission Audit Log Report</title>
		<meta content="Microsoft Visual Studio.NET 7.0" name="GENERATOR">
		<meta content="Visual Basic 7.0" name="CODE_LANGUAGE">
		<meta content="JavaScript" name="vs_defaultClientScript">
		<meta content="http://schemas.microsoft.com/intellisense/ie5" name="vs_targetSchema">
		<LINK href="Style/vla-ie.css" type="text/css" rel="stylesheet">
	</HEAD>
	<body>
		<form id="Form1" method="post" runat="server">
			<uc1:vlaheader id="VLAHeader1" runat="server"></uc1:vlaheader>
			<DIV style="WIDTH: 744px; POSITION: relative; HEIGHT: 107px" ms_positioning="GridLayout">
				<asp:label id="lblSubmissionNumber" style="Z-INDEX: 102; LEFT: 16px; POSITION: absolute; TOP: 41px"
					runat="server" Width="152px">Submission Number</asp:label>
				<asp:label id="Label2" style="Z-INDEX: 103; LEFT: 371px; POSITION: absolute; TOP: 8px" runat="server"
					Width="24px">and</asp:label>
				<asp:label id="lblDateRange" style="Z-INDEX: 104; LEFT: 16px; POSITION: absolute; TOP: 8px"
					runat="server" Width="152px"> Log Entries Between</asp:label>
				<DIV style="Z-INDEX: 109; LEFT: 184px; WIDTH: 217px; POSITION: absolute; TOP: 8px; HEIGHT: 33px"
					ms_positioning="FlowLayout">
					<uc1:CalendarDate id="ctlStartDate" runat="server"></uc1:CalendarDate>
				</DIV>
				<DIV style="Z-INDEX: 120; LEFT: 403px; WIDTH: 161px; POSITION: absolute; TOP: 8px; HEIGHT: 48px"
					ms_positioning="FlowLayout">
					<uc1:CalendarDate id="ctlEndDate" runat="server"></uc1:CalendarDate>
				</DIV>
				<HR style="Z-INDEX: 105; LEFT: 8px; WIDTH: 96.14%; POSITION: absolute; TOP: 88px; HEIGHT: 1px"
					width="96.14%" SIZE="1">
				<asp:RegularExpressionValidator id="revSubmissionID" style="Z-INDEX: 106; LEFT: 254px; POSITION: absolute; TOP: 41px"
					runat="server" ToolTip="Must be numeric" CssClass="ValidatorText" ValidationExpression="^[1-9]+[0-9]*$" ControlToValidate="txtSubmissionID">*</asp:RegularExpressionValidator>
				<asp:RequiredFieldValidator id="rfvSubmissionID" style="Z-INDEX: 107; LEFT: 254px; POSITION: absolute; TOP: 41px"
					runat="server" ToolTip="Required Field" CssClass="ValidatorText" ControlToValidate="txtSubmissionID">*</asp:RequiredFieldValidator>
				<asp:TextBox id="txtSubmissionID" style="Z-INDEX: 108; LEFT: 184px; POSITION: absolute; TOP: 41px"
					runat="server" Width="68px"></asp:TextBox>
				<asp:button id="btnSearch" style="Z-INDEX: 101; LEFT: 602px; POSITION: absolute; TOP: 8px" runat="server"
					Width="116px" Text="Search" CausesValidation="False"></asp:button>
				<asp:Button id="btnAuditLogMenu" style="Z-INDEX: 111; LEFT: 602px; POSITION: absolute; TOP: 41px"
					runat="server" Text="Audit Log Menu" Width="116" CausesValidation="False"></asp:Button>
			</DIV>
			<asp:datagrid id="grdResults" runat="server" Width="568px" AllowPaging="True" AllowSorting="True"
				AutoGenerateColumns="False">
				<SelectedItemStyle CssClass="GridSelectedItem"></SelectedItemStyle>
				<EditItemStyle CssClass="GridEditItem"></EditItemStyle>
				<ItemStyle CssClass="GridItem"></ItemStyle>
				<HeaderStyle CssClass="GridHeader"></HeaderStyle>
				<Columns>
					<asp:BoundColumn DataField="TableName" SortExpression="TableName" ReadOnly="True" HeaderText="Table"></asp:BoundColumn>
					<asp:BoundColumn DataField="FieldName" SortExpression="FieldName" ReadOnly="True" HeaderText="Field"></asp:BoundColumn>
					<asp:BoundColumn DataField="DateTime" SortExpression="DateTime" ReadOnly="True" HeaderText="Date Time"
						DataFormatString="{0:G}"></asp:BoundColumn>
					<asp:BoundColumn DataField="UserName" SortExpression="UserName" ReadOnly="True" HeaderText="User"></asp:BoundColumn>
					<asp:BoundColumn DataField="BeforeValue" SortExpression="BeforeValue" ReadOnly="True" HeaderText="Before"></asp:BoundColumn>
					<asp:BoundColumn DataField="AfterValue" SortExpression="AfterValue" ReadOnly="True" HeaderText="After"></asp:BoundColumn>
					<asp:BoundColumn DataField="Reason" SortExpression="Reason" HeaderText="Reason"></asp:BoundColumn>
					<asp:BoundColumn DataField="KeyID" SortExpression="KeyID" HeaderText="Key"></asp:BoundColumn>
				</Columns>
				<PagerStyle Visible="False"></PagerStyle>
			</asp:datagrid>
			<DIV style="WIDTH: 751px; POSITION: relative; HEIGHT: 54px" ms_positioning="GridLayout">
				<uc1:datagridpager id="ResultsPager" runat="server"></uc1:datagridpager>
				<asp:hyperlink id="hlbExcel" style="Z-INDEX: 101; LEFT: 648px; POSITION: absolute; TOP: 32px" runat="server"
					Width="102px" NavigateUrl="ExcelExport.aspx" Target="_blank" Visible="False">Export to Excel</asp:hyperlink></DIV>
			<DIV id="ctlDiv" style="WIDTH: 748px; HEIGHT: 1px" runat="server"></DIV>
			<uc1:vlafooter id="VLAFooter1" runat="server"></uc1:vlafooter>
		</form>
	</body>
</HTML>
