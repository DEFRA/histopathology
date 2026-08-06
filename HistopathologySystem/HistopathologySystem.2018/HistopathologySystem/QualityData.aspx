<%@ Page Language="vb" AutoEventWireup="false" Codebehind="QualityData.aspx.vb" Inherits="HistopathologySystem.QualityData" smartNavigation="True"%>
<%@ Register TagPrefix="uc1" TagName="CalendarDate" Src="CalendarDate.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAHeader" Src="VLAHeader.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAFooter" Src="VLAFooter.ascx" %>
<%@ Register TagPrefix="uc1" TagName="DataGridPager" Src="DataGridPager.ascx" %>
<%@ Register TagPrefix="uc1" TagName="Batch" Src="Batch.ascx" %>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<HTML>
	<HEAD>
		<title>QualityData</title>
		<meta content="Microsoft Visual Studio.NET 7.0" name="GENERATOR">
		<meta content="Visual Basic 7.0" name="CODE_LANGUAGE">
		<meta content="JavaScript" name="vs_defaultClientScript">
		<meta content="http://schemas.microsoft.com/intellisense/ie5" name="vs_targetSchema">
		<LINK href="Style/vla-ie.css" type="text/css" rel="stylesheet">
	</HEAD>
	<body>
		<form id="Form1" method="post" runat="server">
			<uc1:vlaheader id="VLAHeader1" runat="server"></uc1:vlaheader>
			<DIV style="WIDTH: 152px"><uc1:batch id="Batch1" runat="server"></uc1:batch></DIV>
			<DIV style="WIDTH: 768px; POSITION: relative; HEIGHT: 78px" ms_positioning="GridLayout"><asp:label id="lblHistoFilter" style="Z-INDEX: 101; LEFT: 119px; POSITION: absolute; TOP: 8px"
					runat="server">Histology Ref</asp:label><asp:label id="lblTestFilter" style="Z-INDEX: 102; LEFT: 398px; POSITION: absolute; TOP: 8px"
					runat="server">Test</asp:label><asp:radiobuttonlist id="rblFilter" style="Z-INDEX: 103; LEFT: 8px; POSITION: absolute; TOP: 1px" runat="server"
					AutoPostBack="True" RepeatDirection="Horizontal" CssClass="Body"></asp:radiobuttonlist><asp:dropdownlist id="ddlHistologyRefList" style="Z-INDEX: 105; LEFT: 227px; POSITION: absolute; TOP: 8px"
					runat="server" Width="156px"></asp:dropdownlist><asp:dropdownlist id="ddlTestList" style="Z-INDEX: 107; LEFT: 445px; POSITION: absolute; TOP: 8px"
					runat="server" Width="156"></asp:dropdownlist><asp:button id="btnFilter" style="Z-INDEX: 100; LEFT: 621px; POSITION: absolute; TOP: 8px" runat="server"
					Width="56px" Text="Go" CausesValidation="False"></asp:button><asp:textbox id="txtPage" style="Z-INDEX: 108; LEFT: 120px; POSITION: absolute; TOP: 36px" runat="server"
					Width="75px" MaxLength="5"></asp:textbox><asp:label id="lblPage" style="Z-INDEX: 109; LEFT: 18px; POSITION: absolute; TOP: 36px" runat="server">Go To Page</asp:label><asp:button id="btnGoToPage" style="Z-INDEX: 110; LEFT: 227px; POSITION: absolute; TOP: 36px"
					runat="server" Text="Go to Page" CausesValidation="False"></asp:button><asp:checkbox id="chkSelectAll" style="Z-INDEX: 104; LEFT: 715px; POSITION: absolute; TOP: 40px"
					runat="server" AutoPostBack="True" Text="All"></asp:checkbox><asp:requiredfieldvalidator id="rfvPageNumber" style="Z-INDEX: 112; LEFT: 198px; POSITION: absolute; TOP: 36px"
					runat="server" CssClass="ValidatorText" ControlToValidate="txtPage" ToolTip="Required Field">*</asp:requiredfieldvalidator>
				<asp:customvalidator id="revPageNumber" style="Z-INDEX: 118; LEFT: 198px; POSITION: absolute; TOP: 36px"
					runat="server" CssClass="ValidatorText" ToolTip="Must be numeric" ControlToValidate="txtPage" ClientValidationFunction="ClientValidatePageNumber"
					OnServerValidate="ValidatePageNumber">*</asp:customvalidator></DIV>
			<DIV style="WIDTH: 787px"><asp:datagrid id="grdQuality" runat="server" AllowPaging="True" AutoGenerateColumns="False" PageSize="12"
					AllowSorting="True">
					<SelectedItemStyle CssClass="GridSelectedItem"></SelectedItemStyle>
					<EditItemStyle CssClass="GridEditItem"></EditItemStyle>
					<ItemStyle CssClass="GridItem"></ItemStyle>
					<HeaderStyle CssClass="GridHeader"></HeaderStyle>
					<Columns>
						<asp:ButtonColumn Text="&lt;img src=&quot;Images/GridPager/sel.gif&quot;&gt;" CommandName="Select">
							<ItemStyle HorizontalAlign="Left" Width="20px"></ItemStyle>
						</asp:ButtonColumn>
						<asp:BoundColumn DataField="HistologyRef" SortExpression="HistologyRef" HeaderText="Histology Ref">
							<ItemStyle HorizontalAlign="Left" Width="100px"></ItemStyle>
						</asp:BoundColumn>
						<asp:BoundColumn DataField="BlockRef" SortExpression="BlockRef" HeaderText="Block Ref">
							<ItemStyle HorizontalAlign="Left" Width="100px"></ItemStyle>
						</asp:BoundColumn>
						<asp:BoundColumn DataField="TestDetails" SortExpression="TestDetails" HeaderText="Test">
							<ItemStyle HorizontalAlign="Left" Width="150px"></ItemStyle>
						</asp:BoundColumn>
						<asp:TemplateColumn SortExpression="Failed" HeaderText="Failed">
							<ItemStyle HorizontalAlign="Left" Width="55px"></ItemStyle>
							<ItemTemplate>
								<asp:CheckBox id="cbFailed" Enabled="false" Runat="server"></asp:CheckBox>
							</ItemTemplate>
						</asp:TemplateColumn>
						<asp:TemplateColumn SortExpression="Passed" HeaderText="Passed">
							<ItemStyle HorizontalAlign="Left" Width="65px"></ItemStyle>
							<ItemTemplate>
								<asp:CheckBox ID="cbPassed" Runat="server" Enabled="false"></asp:CheckBox>
							</ItemTemplate>
						</asp:TemplateColumn>
						<asp:TemplateColumn SortExpression="Dispatched" HeaderText="Dispatched">
							<ItemStyle HorizontalAlign="Left" Width="90px"></ItemStyle>
							<ItemTemplate>
								<asp:CheckBox ID="cbDispatchedDisplay" Runat="server" Enabled="false"></asp:CheckBox>
							</ItemTemplate>
						</asp:TemplateColumn>
						<asp:TemplateColumn SortExpression="Archived" HeaderText="Archived">
							<ItemStyle HorizontalAlign="Left" Width="50px"></ItemStyle>
							<ItemTemplate>
								<asp:CheckBox ID="cbArchived" Runat="server" Enabled="false"></asp:CheckBox>
							</ItemTemplate>
						</asp:TemplateColumn>
						<asp:TemplateColumn SortExpression="OnHold" HeaderText="On Hold">
							<ItemStyle HorizontalAlign="Left" Width="50px"></ItemStyle>
							<ItemTemplate>
								<asp:CheckBox ID="cbOnHold" Runat="server" Enabled="false"></asp:CheckBox>
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
				</asp:datagrid><uc1:datagridpager id="BlockPager" runat="server"></uc1:datagridpager></DIV>
			<DIV style="WIDTH: 904px; POSITION: relative; HEIGHT: 571px" ms_positioning="GridLayout"><asp:dropdownlist id="ddlTestResult" style="Z-INDEX: 128; LEFT: 184px; POSITION: absolute; TOP: 19px"
					runat="server" AutoPostBack="True" Width="165" Height="25"></asp:dropdownlist><asp:textbox id="txtStainRef" style="Z-INDEX: 116; LEFT: 184px; POSITION: absolute; TOP: 51px"
					runat="server" Width="166px" Height="27px" MaxLength="20"></asp:textbox><asp:checkbox id="chkDispatched" style="Z-INDEX: 117; LEFT: 184px; POSITION: absolute; TOP: 83px"
					runat="server" AutoPostBack="True" Width="165px"></asp:checkbox><asp:textbox id="txtDispatchedTo" style="Z-INDEX: 144; LEFT: 184px; POSITION: absolute; TOP: 115px"
					runat="server" Width="166" Height="27" MaxLength="30"></asp:textbox>
				<DIV style="Z-INDEX: 145; LEFT: 184px; WIDTH: 160px; POSITION: absolute; TOP: 147px; HEIGHT: 48px"><uc1:calendardate id="ctlDispatchDate" runat="server"></uc1:calendardate></DIV>
				<asp:dropdownlist id="ddlDispatchedBy" style="Z-INDEX: 120; LEFT: 184px; POSITION: absolute; TOP: 179px"
					runat="server" Width="166"></asp:dropdownlist><asp:textbox id="txtNumberOfSlides" style="Z-INDEX: 143; LEFT: 184px; POSITION: absolute; TOP: 211px"
					runat="server" Width="166" Height="27px" MaxLength="4"></asp:textbox><asp:dropdownlist id="ddlQCCode" style="Z-INDEX: 111; LEFT: 184px; POSITION: absolute; TOP: 243px"
					runat="server" Width="166"></asp:dropdownlist><asp:checkbox id="chkQCNote" style="Z-INDEX: 112; LEFT: 184px; POSITION: absolute; TOP: 275px"
					runat="server" AutoPostBack="True" Width="165px" Enabled="False"></asp:checkbox><asp:textbox id="txtQCNoteRef" style="Z-INDEX: 115; LEFT: 184px; POSITION: absolute; TOP: 307px"
					runat="server" Width="166" MaxLength="10"></asp:textbox><asp:dropdownlist id="ddlRemedialAction" style="Z-INDEX: 130; LEFT: 558px; POSITION: absolute; TOP: 19px"
					runat="server" Width="167px" Height="25"></asp:dropdownlist><asp:dropdownlist id="ddlArchiveLocation" style="Z-INDEX: 133; LEFT: 558px; POSITION: absolute; TOP: 51px"
					runat="server" AutoPostBack="True" Width="167" Height="25px"></asp:dropdownlist>
				<DIV style="Z-INDEX: 141; LEFT: 558px; WIDTH: 282px; POSITION: absolute; TOP: 83px; HEIGHT: 31px"><uc1:calendardate id="ctlArchiveDate" runat="server"></uc1:calendardate></DIV>
				<asp:textbox id="txtArchiveComment" style="Z-INDEX: 135; LEFT: 558px; POSITION: absolute; TOP: 115px"
					runat="server" Width="166px" Height="54px" TextMode="MultiLine"></asp:textbox>
				<DIV style="Z-INDEX: 140; LEFT: 558px; WIDTH: 207px; POSITION: absolute; TOP: 179px; HEIGHT: 227px"><asp:checkboxlist id="chkblTCCodes" runat="server"></asp:checkboxlist></DIV>
				<asp:textbox id="txtComment" style="Z-INDEX: 105; LEFT: 10px; POSITION: absolute; TOP: 409px"
					runat="server" Width="729px" Height="71px" MaxLength="255" TextMode="MultiLine"></asp:textbox><asp:button id="btnEdit" style="Z-INDEX: 108; LEFT: 16px; POSITION: absolute; TOP: 513px" runat="server"
					Width="123px" Text="Update Selected" CausesValidation="False" Height="24px" Enabled="False"></asp:button><asp:button id="btnSave" style="Z-INDEX: 103; LEFT: 625px; POSITION: absolute; TOP: 513px" runat="server"
					Width="114" Text="Done" CausesValidation="False" Height="24" Enabled="False"></asp:button><asp:label id="lblComments" style="Z-INDEX: 106; LEFT: 17px; POSITION: absolute; TOP: 387px"
					runat="server"> Comments:</asp:label><asp:button id="btnCancel" style="Z-INDEX: 104; LEFT: 506px; POSITION: absolute; TOP: 513px"
					runat="server" Width="114px" Text="Cancel" CausesValidation="False" Height="24px"></asp:button>
				<HR style="Z-INDEX: 107; LEFT: 10px; POSITION: absolute; TOP: 8px; HEIGHT: 1px" width="85%"
					SIZE="1">
				<asp:label id="lblStatus" style="Z-INDEX: 109; LEFT: 17px; POSITION: absolute; TOP: 19px" runat="server"
					Width="120px">Test Result</asp:label><asp:label id="lblQCCode" style="Z-INDEX: 110; LEFT: 17px; POSITION: absolute; TOP: 243px"
					runat="server">QC Code</asp:label><asp:label id="lblQCNote" style="Z-INDEX: 113; LEFT: 17px; POSITION: absolute; TOP: 275px"
					runat="server">QC Note</asp:label><asp:label id="lblQCNoteRef" style="Z-INDEX: 114; LEFT: 17px; POSITION: absolute; TOP: 307px"
					runat="server">QC Note Ref</asp:label><asp:label id="lblStainRef" style="Z-INDEX: 101; LEFT: 17px; POSITION: absolute; TOP: 51px"
					runat="server">Stain Ref</asp:label><asp:label id="lblDispatched" style="Z-INDEX: 102; LEFT: 17px; POSITION: absolute; TOP: 83px"
					runat="server">Dispatched?</asp:label>
				<HR style="Z-INDEX: 118; LEFT: 10px; POSITION: absolute; TOP: 497px; HEIGHT: 1px" width="85%"
					SIZE="1">
				<asp:label id="lblPremium" style="Z-INDEX: 119; LEFT: 411px; POSITION: absolute; TOP: 179px"
					runat="server"> Charges</asp:label><asp:label id="lblDispatchDate" style="Z-INDEX: 121; LEFT: 17px; POSITION: absolute; TOP: 147px"
					runat="server">Dispatched Date</asp:label><asp:label id="lblDispatchedBy" style="Z-INDEX: 122; LEFT: 17px; POSITION: absolute; TOP: 179px"
					runat="server">Dispatched By</asp:label><asp:requiredfieldvalidator id="rfvDispatchedBy" style="Z-INDEX: 124; LEFT: 349px; POSITION: absolute; TOP: 179px"
					runat="server" CssClass="ValidatorText" ControlToValidate="ddlDispatchedBy" ToolTip="Required Field">*</asp:requiredfieldvalidator><asp:label id="lblDispatchedTo" style="Z-INDEX: 125; LEFT: 17px; POSITION: absolute; TOP: 115px"
					runat="server">Dispatched To</asp:label><asp:requiredfieldvalidator id="rfvDispatchedTo" style="Z-INDEX: 126; LEFT: 349px; POSITION: absolute; TOP: 115px"
					runat="server" CssClass="validatorText" ControlToValidate="txtDispatchedTo" ToolTip="Required Field">*</asp:requiredfieldvalidator><asp:requiredfieldvalidator id="rfvQCCode" style="Z-INDEX: 127; LEFT: 349px; POSITION: absolute; TOP: 243px"
					runat="server" CssClass="ValidatorText" ControlToValidate="ddlQCCode" ToolTip="Required Field">*</asp:requiredfieldvalidator><asp:label id="lblRemedialAction" style="Z-INDEX: 129; LEFT: 411px; POSITION: absolute; TOP: 19px"
					runat="server">Remedial Action</asp:label><asp:requiredfieldvalidator id="rfvRemedialAction" style="Z-INDEX: 131; LEFT: 724px; POSITION: absolute; TOP: 19px"
					runat="server" CssClass="ValidatorText" ControlToValidate="ddlRemedialAction" ToolTip="Required Field">*</asp:requiredfieldvalidator><asp:label id="lblArchiveLocation" style="Z-INDEX: 132; LEFT: 411px; POSITION: absolute; TOP: 51px"
					runat="server">Archive Location</asp:label><asp:label id="lblArchiveDate" style="Z-INDEX: 134; LEFT: 411px; POSITION: absolute; TOP: 83px"
					runat="server">Archive Date</asp:label><asp:label id="lblArchiveComment" style="Z-INDEX: 136; LEFT: 411px; POSITION: absolute; TOP: 115px"
					runat="server">Archive Comment</asp:label><asp:label id="lblNoSlides" style="Z-INDEX: 137; LEFT: 17px; POSITION: absolute; TOP: 211px"
					runat="server">Number of Blocks\Slides</asp:label><asp:requiredfieldvalidator id="rfvNumberOfSlides" style="Z-INDEX: 138; LEFT: 349px; POSITION: absolute; TOP: 211px"
					runat="server" CssClass="ValidatorText" ControlToValidate="txtNumberOfSlides" ToolTip="Required Field">*</asp:requiredfieldvalidator><asp:regularexpressionvalidator id="revNumberOfSlides" style="Z-INDEX: 139; LEFT: 349px; POSITION: absolute; TOP: 210px"
					runat="server" CssClass="ValidatorText" ValidationExpression="^[1-9]+[0-9]*$" ControlToValidate="txtNumberOfSlides" ToolTip="Must be a numeric value">*</asp:regularexpressionvalidator><asp:label id="lblError" style="Z-INDEX: 142; LEFT: 349px; POSITION: absolute; TOP: 147px"
					runat="server" CssClass="ValidatorText" ToolTip="Required Field" Visible="False">*</asp:label><asp:textbox id="txtTmpNoSlides" style="Z-INDEX: 123; LEFT: 184px; POSITION: absolute; TOP: 211px"
					tabIndex="-1" runat="server" Width="32px" Height="20px"></asp:textbox><asp:label id="lblArchiveLocationError" style="Z-INDEX: 146; LEFT: 724px; POSITION: absolute; TOP: 51px"
					runat="server" CssClass="ValidatorText" ToolTip="Required Field">*</asp:label>
				<asp:regularexpressionvalidator id="revArchiveNotes" style="Z-INDEX: 231; LEFT: 724px; POSITION: absolute; TOP: 115px"
					runat="server" ControlToValidate="txtArchiveComment" ErrorMessage="*" ValidationExpression=".{0,200}" CssClass="ValidatorText"
					ToolTip="Archive comments must be less than or equal to 500 characters"></asp:regularexpressionvalidator>
				<asp:regularexpressionvalidator id="revComments" style="Z-INDEX: 231; LEFT: 744px; POSITION: absolute; TOP: 408px"
					runat="server" ControlToValidate="txtComment" ErrorMessage="*" ValidationExpression=".{0,2000}" CssClass="ValidatorText"
					ToolTip="Comments must be less than or equal to 2000 characters"></asp:regularexpressionvalidator>
			</DIV>
			<DIV id="ctlDIV" style="WIDTH: 751px; HEIGHT: 20px" runat="Server"></DIV>
			<uc1:vlafooter id="VLAFooter1" runat="server"></uc1:vlafooter></form>
	</body>
</HTML>
