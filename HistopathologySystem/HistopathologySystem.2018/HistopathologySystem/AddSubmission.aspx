<%@ Register TagPrefix="uc1" TagName="SenderRef" Src="SenderRef.ascx" %>
<%@ Page Language="vb" AutoEventWireup="false" Codebehind="AddSubmission.aspx.vb" Inherits="HistopathologySystem.AddSubmission"%>
<%@ Register TagPrefix="uc1" TagName="MouseNumber" Src="MouseNumber.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAHeader" Src="VLAHeader.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAFooter" Src="VLAFooter.ascx" %>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<HTML>
	<HEAD>
		<title>AddSubmission</title>
		<meta content="Microsoft Visual Studio.NET 7.0" name="GENERATOR">
		<meta content="Visual Basic 7.0" name="CODE_LANGUAGE">
		<meta content="JavaScript" name="vs_defaultClientScript">
		<meta content="http://schemas.microsoft.com/intellisense/ie5" name="vs_targetSchema">
		<LINK href="Style/vla-ie.css" type="text/css" rel="stylesheet">
	</HEAD>
	<body>
		<form id="Form1" method="post" runat="server">
			<uc1:vlaheader id="VLAHeader1" runat="server"></uc1:vlaheader>
			<DIV style="WIDTH: 801px; POSITION: relative; HEIGHT: 52px" ms_positioning="GridLayout"><asp:label id="lblDescription" style="Z-INDEX: 111; LEFT: 25px; POSITION: absolute; TOP: 9px"
					runat="server" Width="749px" Height="17px">Enter your reference for the sample which, you are submitting tissue for, then click the 'Next' button. It is recommended that the Sender Ref is no longer than 10 characters.</asp:label></DIV>
			<DIV id="ctlDivSampleOverride" style="WIDTH: 798px; POSITION: relative; HEIGHT: 31px"
				runat="server" ms_positioning="GridLayout"><asp:checkbox id="cbProjectOverride" style="Z-INDEX: 102; LEFT: 25px; POSITION: absolute; TOP: 5px"
					runat="server" TextAlign="Left" Text="Same project code for all samples?"></asp:checkbox></DIV>
			<DIV id="ctlDivValidationOverride" style="WIDTH: 799px; POSITION: relative; HEIGHT: 31px"
				runat="server" ms_positioning="GridLayout"><asp:checkbox id="cbUseValidation" style="Z-INDEX: 104; LEFT: 25px; POSITION: absolute; TOP: 7px"
					runat="server" TextAlign="Left" Text="Validate Sender Ref?" AutoPostBack="True"></asp:checkbox></DIV>
			<DIV style="WIDTH: 802px; POSITION: relative; HEIGHT: 78px" ms_positioning="GridLayout"><asp:dropdownlist id="ddlSenderRef" style="Z-INDEX: 101; LEFT: 24px; POSITION: absolute; TOP: 9px"
					tabIndex="2" runat="server" Width="210px" Height="22" AutoPostBack="True"></asp:dropdownlist>
				<DIV style="Z-INDEX: 103; LEFT: 24px; WIDTH: 160px; POSITION: absolute; TOP: 6px; HEIGHT: 60px"
					tabIndex="-1"><uc1:senderref id="SenderRef1" runat="server"></uc1:senderref></DIV>
				<asp:dropdownlist id="ddlProjectsList" style="Z-INDEX: 106; LEFT: 281px; POSITION: absolute; TOP: 9px"
					runat="server" Width="161px" Visible="False"></asp:dropdownlist>
				<DIV id="ctlDivLookup" style="Z-INDEX: 105; LEFT: 24px; WIDTH: 677px; POSITION: absolute; TOP: 38px; HEIGHT: 17px"
					runat="server" ms_positioning="FlowLayout">If you wish to search for a previous 
					reference,
					<asp:linkbutton id="lbLookup" runat="server" CausesValidation="False">Click here.</asp:linkbutton></DIV>
				<asp:requiredfieldvalidator id="rfvSenderDropDown" style="Z-INDEX: 102; LEFT: 187px; POSITION: absolute; TOP: 9px"
					runat="server" ControlToValidate="ddlSenderRef" ToolTip="Required Field" CssClass="ValidatorText">*</asp:requiredfieldvalidator><asp:label id="lblProject" style="Z-INDEX: 107; LEFT: 219px; POSITION: absolute; TOP: 9px"
					runat="server" Visible="False">Project</asp:label><asp:label id="lblError" style="Z-INDEX: 108; LEFT: 444px; POSITION: absolute; TOP: 9px" runat="server"
					Visible="False" ToolTip="Required Field" CssClass="ValidatorText">*</asp:label><asp:textbox id="txtSpecies" style="Z-INDEX: 109; LEFT: 554px; POSITION: absolute; TOP: 9px"
					runat="server" Width="161px" Visible="False" Enabled="False"></asp:textbox><asp:label id="lblSpecies" style="Z-INDEX: 110; LEFT: 472px; POSITION: absolute; TOP: 9px"
					runat="server" Visible="False">Species</asp:label></DIV>
			<DIV id="ctlMouseDiv" style="WIDTH: 730px; POSITION: relative; HEIGHT: 143px" runat="server"
				ms_positioning="GridLayout">
				<DIV style="Z-INDEX: 101; LEFT: 187px; WIDTH: 160px; POSITION: absolute; TOP: 84px; HEIGHT: 52px"><uc1:mousenumber id="MouseNumber1" runat="server"></uc1:mousenumber></DIV>
				<DIV style="Z-INDEX: 102; LEFT: 401px; WIDTH: 160px; POSITION: absolute; TOP: 84px; HEIGHT: 52px"><uc1:mousenumber id="MouseNumber2" runat="server"></uc1:mousenumber></DIV>
				<asp:label id="lblMouseRangeFrom" style="Z-INDEX: 103; LEFT: 24px; POSITION: absolute; TOP: 84px"
					runat="server">Mouse number from</asp:label><asp:label id="lblMouseRangeTo" style="Z-INDEX: 104; LEFT: 367px; POSITION: absolute; TOP: 84px"
					runat="server">to</asp:label><asp:label id="lblMouseNumbers" style="Z-INDEX: 105; LEFT: 25px; POSITION: absolute; TOP: 17px"
					runat="server"> Alternatively you can assign mouse ranges by entering the mouse numbers in the following text boxes and clicking on the 'Next' button. The mouse number format is MC followed by 6 digits, i.e. MC000105.</asp:label>
				<HR style="Z-INDEX: 109; LEFT: 14px; POSITION: absolute; TOP: 10px; HEIGHT: 1px" width="98%"
					SIZE="1">
			</DIV>
			<DIV style="WIDTH: 730px; POSITION: relative; HEIGHT: 139px" ms_positioning="GridLayout"><asp:button id="btnNext" style="Z-INDEX: 102; LEFT: 608px; POSITION: absolute; TOP: 16px" runat="server"
					Width="106px" Height="25px" Text="Next" CausesValidation="False"></asp:button><asp:button id="btnCancel" style="Z-INDEX: 101; LEFT: 498px; POSITION: absolute; TOP: 16px"
					runat="server" Width="106px" Height="25px" Text="Back" CausesValidation="False"></asp:button>
				<HR style="Z-INDEX: 103; LEFT: 14px; POSITION: absolute; TOP: 8px; HEIGHT: 1px" width="98%"
					SIZE="1">
				<div id="ctlUploadDiv" runat="server">
					<INPUT id="corrFile" style="Z-INDEX: 104; LEFT: 29px; WIDTH: 376px; POSITION: absolute; TOP: 58px; HEIGHT: 20px"
						type="file" size="43" name="corrFile" runat="server">
					<asp:label id="uploadMsg" style="Z-INDEX: 105; LEFT: 29px; POSITION: absolute; TOP: 109px"
						Visible="False" Runat="server" ForeColor="#ff0000"></asp:label><asp:button id="btnUpload" style="Z-INDEX: 106; LEFT: 28px; POSITION: absolute; TOP: 82px" runat="server"
						Width="103px" Text="Upload" CausesValidation="False"></asp:button>
					<asp:Label id="lblUpload" runat="server" style="Z-INDEX: 107; LEFT: 29px; POSITION: absolute; TOP: 20px"
						Width="434px">You can use the following upload button to upload mouse numbers from the mouse number excel template.</asp:Label></div>
			</DIV>
			<DIV id="ctlDiv" style="WIDTH: 741px; HEIGHT: 21px" runat="server"></DIV>
		</form>
		<uc1:vlafooter id="VLAFooter1" runat="server"></uc1:vlafooter>
	</body>
</HTML>
