<%@ Register TagPrefix="uc1" TagName="VLAFooter" Src="VLAFooter.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAHeader" Src="VLAHeader.ascx" %>
<%@ Register TagPrefix="uc1" TagName="SenderRef" Src="SenderRef.ascx" %>
<%@ Register TagPrefix="uc1" TagName="MouseNumber" Src="MouseNumber.ascx" %>
<%@ Page Language="vb" AutoEventWireup="false" Codebehind="AddSample.aspx.vb" Inherits="HistopathologySystem.AddSample" %>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<HTML>
	<HEAD>
		<title>AddSample</title>
		<meta content="Microsoft Visual Studio.NET 7.0" name="GENERATOR">
		<meta content="Visual Basic 7.0" name="CODE_LANGUAGE">
		<meta content="JavaScript" name="vs_defaultClientScript">
		<meta content="http://schemas.microsoft.com/intellisense/ie5" name="vs_targetSchema">
		<LINK href="Style/vla-ie.css" type="text/css" rel="stylesheet">
	</HEAD>
	<body>
		<form id="Form1" method="post" runat="server">
			<uc1:vlaheader id="VLAHeader1" runat="server"></uc1:vlaheader>
			<DIV style="WIDTH: 801px; POSITION: relative; HEIGHT: 51px" ms_positioning="GridLayout">
				<asp:label id="Label1" style="Z-INDEX: 111; LEFT: 25px; POSITION: absolute; TOP: 9px" runat="server"
					Width="749px" Height="17px">Enter your reference for the sample which, you are submitting tissue for, then click the 'Next' button. It is recommended that the Sender Ref is no longer than 10 characters.</asp:label></DIV>
			<DIV style="WIDTH: 798px; POSITION: relative; HEIGHT: 31px" ms_positioning="GridLayout"
				id="ctlDivSampleOverride" runat="server">
				<asp:CheckBox id="cbProjectOverride" style="Z-INDEX: 102; LEFT: 25px; POSITION: absolute; TOP: 5px"
					runat="server" Text="Same project code for all samples?" TextAlign="Left"></asp:CheckBox></DIV>
			<DIV id="ctlDivValidationOverride" style="WIDTH: 799px; POSITION: relative; HEIGHT: 31px"
				runat="server" ms_positioning="GridLayout">
				<asp:CheckBox id="cbUseValidation" style="Z-INDEX: 104; LEFT: 25px; POSITION: absolute; TOP: 7px"
					runat="server" AutoPostBack="True" Text="Validate Sender Ref?" TextAlign="Left"></asp:CheckBox></DIV>
			<DIV style="WIDTH: 803px; POSITION: relative; HEIGHT: 71px" ms_positioning="GridLayout"><asp:label id="lblSender" style="Z-INDEX: 101; LEFT: 24px; POSITION: absolute; TOP: 11px" runat="server"> Sender Ref:</asp:label>
				<asp:label id="lblProject" style="Z-INDEX: 107; LEFT: 309px; POSITION: absolute; TOP: 11px"
					runat="server" Visible="False">Project</asp:label>
				<asp:dropdownlist id="ddlProjectsList" style="Z-INDEX: 106; LEFT: 373px; POSITION: absolute; TOP: 11px"
					runat="server" Width="161" Visible="False"></asp:dropdownlist>
				<asp:label id="lblError" style="Z-INDEX: 108; LEFT: 536px; POSITION: absolute; TOP: 11px" runat="server"
					Visible="False" CssClass="ValidatorText" ToolTip="Required Field">*</asp:label>
				<asp:label id="lblSpecies" style="Z-INDEX: 110; LEFT: 565px; POSITION: absolute; TOP: 11px"
					runat="server" Visible="False">Species</asp:label>
				<asp:textbox id="txtSpecies" style="Z-INDEX: 109; LEFT: 634px; POSITION: absolute; TOP: 11px"
					runat="server" Width="161px" Visible="False" Enabled="False"></asp:textbox>
				<DIV style="Z-INDEX: 105; LEFT: 130px; WIDTH: 160px; POSITION: absolute; TOP: 11px; HEIGHT: 54px"
					tabIndex="-1">
					<uc1:SenderRef id="SenderRef1" runat="server"></uc1:SenderRef></DIV>
				<DIV id="ctlDivLookup" style="Z-INDEX: 107; LEFT: 24px; WIDTH: 628px; POSITION: absolute; TOP: 40px; HEIGHT: 19px"
					runat="server" ms_positioning="FlowLayout">If you wish to search for a previous 
					reference,
					<asp:LinkButton id="lbLookup" runat="server" CausesValidation="False">Click here.</asp:LinkButton></DIV>
			</DIV>
			<DIV id="ctlMouseDiv" style="WIDTH: 740px; POSITION: relative; HEIGHT: 139px" runat="server"
				ms_positioning="GridLayout">
				<DIV style="Z-INDEX: 101; LEFT: 180px; WIDTH: 160px; POSITION: absolute; TOP: 81px; HEIGHT: 58px">
					<uc1:MouseNumber id="MouseNumber1" runat="server"></uc1:MouseNumber></DIV>
				<DIV style="Z-INDEX: 102; LEFT: 390px; WIDTH: 160px; POSITION: absolute; TOP: 81px; HEIGHT: 56px">
					<uc1:MouseNumber id="MouseNumber2" runat="server"></uc1:MouseNumber></DIV>
				<asp:label id="lblMouseRangeFrom" style="Z-INDEX: 103; LEFT: 24px; POSITION: absolute; TOP: 81px"
					runat="server">Mouse number from</asp:label>
				<asp:label id="lblMouseRangeTo" style="Z-INDEX: 104; LEFT: 360px; POSITION: absolute; TOP: 81px"
					runat="server">to</asp:label>
				<asp:label id="lblMouseNumbers" style="Z-INDEX: 105; LEFT: 25px; POSITION: absolute; TOP: 16px"
					runat="server"> Alternatively you can assign mouse ranges by entering the mouse numbers in the following text boxes and clicking on the 'Next' button. The mouse number format is MC followed by 6 digits, i.e. MC000105.</asp:label>
				<HR style="Z-INDEX: 108; LEFT: 14px; POSITION: absolute; TOP: 9px; HEIGHT: 1px" width="98%"
					SIZE="1">
			</DIV>
			<DIV style="WIDTH: 740px; POSITION: relative; HEIGHT: 136px" ms_positioning="GridLayout">
				<asp:button id="btnNext" style="Z-INDEX: 102; LEFT: 610px; POSITION: absolute; TOP: 18px" runat="server"
					Width="101px" Text="Next" Height="25px" CausesValidation="False"></asp:button>
				<asp:button id="btnCancel" style="Z-INDEX: 101; LEFT: 493px; POSITION: absolute; TOP: 18px"
					runat="server" Width="101px" Text="Back" CausesValidation="False" Height="25px"></asp:button>
				<HR style="Z-INDEX: 103; LEFT: 14px; WIDTH: 725px; POSITION: absolute; TOP: 10px; HEIGHT: 1px"
					width="98%" SIZE="1">
				<div id="ctlUploadDiv" runat="server">
					<INPUT id="corrFile" style="Z-INDEX: 104; LEFT: 29px; WIDTH: 376px; POSITION: absolute; TOP: 58px; HEIGHT: 20px"
						type="file" size="43" name="corrFile" runat="server">
					<asp:label id="uploadMsg" style="Z-INDEX: 105; LEFT: 29px; POSITION: absolute; TOP: 109px"
						Visible="False" Runat="server" ForeColor="#ff0000"></asp:label><asp:button id="btnUpload" style="Z-INDEX: 106; LEFT: 28px; POSITION: absolute; TOP: 82px" runat="server"
						Width="103px" Text="Upload" CausesValidation="False"></asp:button>
					<asp:Label id="lblUpload" runat="server" style="Z-INDEX: 107; LEFT: 29px; POSITION: absolute; TOP: 20px"
						Width="434px">You can use the following upload button to upload mouse numbers from the mouse number excel template.</asp:Label></div>
			</DIV>
			<DIV id="ctlDiv" style="WIDTH: 735px; HEIGHT: 17px" runat="server" ms_positioning="flowlayout"></DIV>
		</form>
		<uc1:vlafooter id="VLAFooter1" runat="server"></uc1:vlafooter>
	</body>
</HTML>
