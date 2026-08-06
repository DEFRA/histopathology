<%@ Page Language="vb" AutoEventWireup="false" Codebehind="BatchDetails.aspx.vb" Inherits="HistopathologySystem.BatchDetails" smartNavigation="True"%>
<%@ Register TagPrefix="uc1" TagName="DataGridPager" Src="DataGridPager.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAFooter" Src="VLAFooter.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAHeader" Src="VLAHeader.ascx" %>
<%@ Register TagPrefix="uc1" TagName="CalendarDate" Src="CalendarDate.ascx" %>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<HTML>
	<HEAD>
		<title>BatchDetails</title>
		<META content="Microsoft Visual Studio.NET 7.0" name="GENERATOR">
		<META content="Visual Basic 7.0" name="CODE_LANGUAGE">
		<META content="JavaScript" name="vs_defaultClientScript">
		<META content="http://schemas.microsoft.com/intellisense/ie5" name="vs_targetSchema">
		<LINK href="Style/vla-ie.css" type="text/css" rel="stylesheet">
	</HEAD>
	<BODY>
		<script language="javascript">
		function stopErrors(msg, url, line) {
			if(msg.indexOf("'contentWindow.document' is null or not an object") != -1)
			{ 
			   return true; 
			}
			return false;
		}
		window.onerror = stopErrors;
		</script>
		<FORM id="Form1" method="post" runat="server">
			<uc1:vlaheader id="VLAHeader1" runat="server"></uc1:vlaheader>
			<DIV style="WIDTH: 892px; POSITION: relative; HEIGHT: 303px" ms_positioning="GridLayout"><asp:dropdownlist id="ddlSubmittedBy" style="Z-INDEX: 125; LEFT: 203px; POSITION: absolute; TOP: 49px"
					runat="server" Width="163" Height="25"></asp:dropdownlist><asp:button id="btnNewSubmittedBy" style="Z-INDEX: 127; LEFT: 381px; POSITION: absolute; TOP: 49px"
					runat="server" Height="25" CausesValidation="False" Text="New"></asp:button><asp:requiredfieldvalidator id="rfvSubmittedBy" style="Z-INDEX: 113; LEFT: 365px; POSITION: absolute; TOP: 49px"
					runat="server" ToolTip="Required Field" ControlToValidate="ddlSubmittedBy" CssClass="ValidatorText" ErrorMessage="*"></asp:requiredfieldvalidator><asp:dropdownlist id="ddlUserArea" style="Z-INDEX: 117; LEFT: 603px; POSITION: absolute; TOP: 49px"
					runat="server" Width="162" Height="25" AutoPostBack="True"></asp:dropdownlist><asp:dropdownlist id="ddlProjectCode" style="Z-INDEX: 130; LEFT: 203px; POSITION: absolute; TOP: 85px"
					runat="server" Width="162px" Height="25" AutoPostBack="True"></asp:dropdownlist><asp:button id="btnNewProject" style="Z-INDEX: 128; LEFT: 381px; POSITION: absolute; TOP: 85px"
					runat="server" Height="25" CausesValidation="False" Text="New"></asp:button>
				<DIV style="Z-INDEX: 120; LEFT: 603px; WIDTH: 160px; POSITION: absolute; TOP: 85px; HEIGHT: 48px"><uc1:calendardate id="ctlBatchDate" runat="server"></uc1:calendardate></DIV>
				<asp:dropdownlist id="ddlContactName" style="Z-INDEX: 124; LEFT: 203px; POSITION: absolute; TOP: 121px"
					runat="server" Width="161" Height="25"></asp:dropdownlist><asp:button id="btnNewContact" style="Z-INDEX: 129; LEFT: 381px; POSITION: absolute; TOP: 121px"
					runat="server" Height="25" CausesValidation="False" Text="New"></asp:button><asp:dropdownlist id="ddlFixation" style="Z-INDEX: 110; LEFT: 603px; POSITION: absolute; TOP: 121px"
					runat="server" Width="160px"></asp:dropdownlist><asp:dropdownlist id="ddlSpecies" style="Z-INDEX: 131; LEFT: 203px; POSITION: absolute; TOP: 157px"
					runat="server" Width="162" Height="25" AutoPostBack="True"></asp:dropdownlist><asp:dropdownlist id="ddlSafeToHandle" style="Z-INDEX: 118; LEFT: 603px; POSITION: absolute; TOP: 157px"
					runat="server" Width="161px" Height="25px"></asp:dropdownlist>
				<DIV style="Z-INDEX: 122; LEFT: 203px; WIDTH: 174px; POSITION: absolute; TOP: 229px; HEIGHT: 59px"><uc1:calendardate id="ctlReceivedDate" runat="server"></uc1:calendardate></DIV>
				<asp:checkbox id="cbSampleOverride" style="Z-INDEX: 132; LEFT: 747px; POSITION: absolute; TOP: 193px"
					runat="server" AutoPostBack="True"></asp:checkbox><asp:label id="lblProjectCode" style="Z-INDEX: 101; LEFT: 17px; POSITION: absolute; TOP: 85px"
					runat="server" Height="18px">Project or Contract code</asp:label><asp:label id="lblContactName" style="Z-INDEX: 102; LEFT: 17px; POSITION: absolute; TOP: 121px"
					runat="server" Height="18px"> Pathologist</asp:label><asp:label id="lblSpecies" style="Z-INDEX: 103; LEFT: 17px; POSITION: absolute; TOP: 157px"
					runat="server" Height="18px">Species</asp:label><asp:label id="lblSubmissionDate" style="Z-INDEX: 105; LEFT: 438px; POSITION: absolute; TOP: 85px"
					runat="server" Height="18px">Submission Date</asp:label><asp:label id="lblSafeToHandle" style="Z-INDEX: 106; LEFT: 438px; POSITION: absolute; TOP: 157px"
					runat="server" Height="18px">Is it adequately fixed?</asp:label><asp:requiredfieldvalidator id="rvfProjectContract" style="Z-INDEX: 107; LEFT: 365px; POSITION: absolute; TOP: 85px"
					runat="server" ToolTip="Required Field" ControlToValidate="ddlProjectCode" CssClass="ValidatorText">*</asp:requiredfieldvalidator><asp:requiredfieldvalidator id="rvfContact" style="Z-INDEX: 108; LEFT: 365px; POSITION: absolute; TOP: 121px"
					runat="server" ToolTip="Required Field" ControlToValidate="ddlContactName" CssClass="ValidatorText">*</asp:requiredfieldvalidator><asp:requiredfieldvalidator id="rvfSpecies" style="Z-INDEX: 109; LEFT: 365px; POSITION: absolute; TOP: 157px"
					runat="server" ToolTip="Required Field" ControlToValidate="ddlSpecies" CssClass="ValidatorText">*</asp:requiredfieldvalidator><asp:label id="lblSubmittedBy" style="Z-INDEX: 111; LEFT: 16px; POSITION: absolute; TOP: 49px"
					runat="server">Submitted By</asp:label><asp:label id="lblSubmittedArea" style="Z-INDEX: 112; LEFT: 438px; POSITION: absolute; TOP: 49px"
					runat="server">Submitted Area</asp:label><asp:requiredfieldvalidator id="rfvSubmittedArea" style="Z-INDEX: 114; LEFT: 765px; POSITION: absolute; TOP: 49px"
					runat="server" ToolTip="Required Field" ControlToValidate="ddlUserArea" CssClass="ValidatorText" ErrorMessage="*"></asp:requiredfieldvalidator>
				<HR style="Z-INDEX: 115; LEFT: 8px; WIDTH: 88.95%; POSITION: absolute; TOP: 270px; HEIGHT: 1px"
					width="88.95%" SIZE="1">
				<asp:label id="lblOther" style="Z-INDEX: 116; LEFT: 17px; POSITION: absolute; TOP: 280px" runat="server">Select Histology and required tests:</asp:label><asp:label id="lblFixation" style="Z-INDEX: 119; LEFT: 438px; POSITION: absolute; TOP: 121px"
					runat="server">Fixation</asp:label><asp:requiredfieldvalidator id="rfvSafeToHandle" style="Z-INDEX: 121; LEFT: 765px; POSITION: absolute; TOP: 157px"
					runat="server" ToolTip="Required Field" ControlToValidate="ddlSafeToHandle" CssClass="ValidatorText" InitialValue="-1">*</asp:requiredfieldvalidator><asp:label id="lblCustomerReceivedDate" style="Z-INDEX: 123; LEFT: 17px; POSITION: absolute; TOP: 229px"
					runat="server">Customer Received Date</asp:label><asp:textbox id="txtTmpSelectedProject" style="Z-INDEX: 126; LEFT: 203px; POSITION: absolute; TOP: 87px"
					tabIndex="-1" runat="server" Width="129" Height="17px"></asp:textbox><asp:textbox id="txtTmpSelectedSpecies" style="Z-INDEX: 104; LEFT: 203px; POSITION: absolute; TOP: 157px"
					tabIndex="-1" runat="server" Width="129px" Height="20px"></asp:textbox><asp:label id="lblOverride" style="Z-INDEX: 133; LEFT: 438px; POSITION: absolute; TOP: 193px"
					runat="server">Same project code for all samples?</asp:label><asp:label id="lblSubmittedAs" style="Z-INDEX: 134; LEFT: 17px; POSITION: absolute; TOP: 193px"
					runat="server">Submitted As</asp:label><asp:textbox id="txtSubmittedAs" style="Z-INDEX: 135; LEFT: 203px; POSITION: absolute; TOP: 193px"
					runat="server" Width="162px" Height="22px" Enabled="False"></asp:textbox><asp:label id="lblEnteredBy" style="Z-INDEX: 136; LEFT: 16px; POSITION: absolute; TOP: 13px"
					runat="server">Entered By</asp:label><asp:dropdownlist id="ddlEnteredBy" style="Z-INDEX: 137; LEFT: 203px; POSITION: absolute; TOP: 13px"
					runat="server" Width="163px" Height="22" Enabled="False"></asp:dropdownlist><asp:label id="lblArea" style="Z-INDEX: 138; LEFT: 438px; POSITION: absolute; TOP: 13px" runat="server">Entered Area</asp:label><asp:dropdownlist id="ddlEnteredArea" style="Z-INDEX: 139; LEFT: 603px; POSITION: absolute; TOP: 13px"
					runat="server" Width="162px" Height="25px" Enabled="False"></asp:dropdownlist></DIV>
			<TABLE id="Table">
				<TR>
					<TD>Histology
					</TD>
					<TD>Antibodies</TD>
					<TD>Special Stain
					</TD>
					<td width="60"></td>
				</TR>
				<TR>
					<TD colSpan="100">
						<HR width="100%" SIZE="1">
					</TD>
				</TR>
				<TR>
					<TD vAlign="top"><asp:checkboxlist id="chkblHistology" runat="server" Width="170px" AutoPostBack="True"></asp:checkboxlist></TD>
					<TD vAlign="top"><asp:checkboxlist id="chkblAntibodies" runat="server" Width="170px" AutoPostBack="True"></asp:checkboxlist></TD>
					<TD vAlign="top"><asp:checkboxlist id="chkblSpecialStain" runat="server" Width="170px" AutoPostBack="True"></asp:checkboxlist></TD>
					<TD vAlign="top"><asp:label id="lblError" runat="server" ToolTip="Must add atleast one tissue and assign one test to the block"
							CssClass="ValidatorText" Visible="False">*</asp:label></TD>
				</TR>
			</TABLE>
			<DIV style="WIDTH: 768px; POSITION: relative; HEIGHT: 184px" ms_positioning="GridLayout"><asp:label id="lblComments" style="Z-INDEX: 101; LEFT: 18px; POSITION: absolute; TOP: 13px"
					runat="server">Submission Comments:</asp:label><asp:textbox id="txtComments" style="Z-INDEX: 102; LEFT: 18px; POSITION: absolute; TOP: 36px"
					runat="server" Width="726px" Height="56px" TextMode="MultiLine"></asp:textbox>
				<HR style="Z-INDEX: 103; LEFT: 18px; WIDTH: 95.42%; POSITION: absolute; TOP: 104px; HEIGHT: 1px"
					width="95.42%" SIZE="1">
				<asp:button id="btnBatchSummary" style="Z-INDEX: 106; LEFT: 19px; POSITION: absolute; TOP: 152px"
					runat="server" Width="112px" Height="24" CausesValidation="False" Text="Samples"></asp:button><asp:button id="btnSave" style="Z-INDEX: 105; LEFT: 670px; POSITION: absolute; TOP: 152px" runat="server"
					Width="79" Height="24" CausesValidation="False" Text="Finish"></asp:button><asp:button id="btnCancel" style="Z-INDEX: 104; LEFT: 581px; POSITION: absolute; TOP: 152px"
					runat="server" Width="79px" Height="24" CausesValidation="False" Text="Cancel"></asp:button><asp:label id="lblSamples" style="Z-INDEX: 107; LEFT: 19px; POSITION: absolute; TOP: 120px"
					runat="server">Click on Samples button to add or edit samples on the submission</asp:label></DIV>
			<DIV id="ctlDIV" style="WIDTH: 760px; HEIGHT: 20px" runat="server" ms_positioning="FlowLayout"></DIV>
			<uc1:vlafooter id="VLAFooter1" runat="server"></uc1:vlafooter></FORM>
	</BODY>
</HTML>
