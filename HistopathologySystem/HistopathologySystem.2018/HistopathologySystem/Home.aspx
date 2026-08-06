<%@ Register TagPrefix="uc1" TagName="VLAFooter" Src="VLAFooter.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAHeader" Src="VLAHeader.ascx" %>
<%@ Page Language="vb" AutoEventWireup="false" Codebehind="Home.aspx.vb" Inherits="HistopathologySystem.Home" %>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<HTML>
	<HEAD>
		<title>Home</title>
		<meta name="GENERATOR" content="Microsoft Visual Studio.NET 7.0">
		<meta name="CODE_LANGUAGE" content="Visual Basic 7.0">
		<meta name="vs_defaultClientScript" content="JavaScript">
		<meta name="vs_targetSchema" content="http://schemas.microsoft.com/intellisense/ie5">
		<LINK href="Style/vla-ie.css" type="text/css" rel="stylesheet">
	</HEAD>
	<body>
		<form id="Form1" method="post" runat="server">
			<uc1:VLAHeader id="VLAHeader1" runat="server"></uc1:VLAHeader>
			<DIV style="WIDTH: 767px; POSITION: relative; HEIGHT: 623px" ms_positioning="GridLayout">
				<h2 style="Z-INDEX: 100; LEFT: 77px; POSITION: absolute; TOP: 12px">Welcome to the 
					Histopathology Submissions System</h2>
				<h5 style="Z-INDEX: 101; LEFT: 23px; WIDTH: 629px; POSITION: absolute; TOP: 51px; HEIGHT: 38px">Please 
					select one of the options below to create a new submission or to view/search 
					previous submissions.</h5>
				<asp:LinkButton id="hlTSESubmission" style="Z-INDEX: 103; LEFT: 24px; POSITION: absolute; TOP: 111px"
					runat="server" Height="16" Width="214px">Create New TSE Submission</asp:LinkButton>
				<asp:LinkButton id="hlNonTSESubmission" style="Z-INDEX: 104; LEFT: 24px; POSITION: absolute; TOP: 143px"
					runat="server" Height="16px" Width="247px">Create New Non-TSE Submission</asp:LinkButton>
				<asp:LinkButton id="lbViewSubmissions" style="Z-INDEX: 125; LEFT: 454px; POSITION: absolute; TOP: 111px"
					runat="server">View Submissions</asp:LinkButton>
				<asp:HyperLink id="hlSearchPMDates" style="Z-INDEX: 114; LEFT: 454px; POSITION: absolute; TOP: 143px"
					runat="server" Width="131px" NavigateUrl="SearchPMDates.aspx">Search PM Dates</asp:HyperLink>
				<asp:HyperLink id="hlViewSamples" style="Z-INDEX: 116; LEFT: 454px; POSITION: absolute; TOP: 171px"
					runat="server" NavigateUrl="ViewSamples.aspx" Width="103px">View Samples</asp:HyperLink>
				<asp:HyperLink id="hlReceiveSubmissions" style="Z-INDEX: 105; LEFT: 24px; POSITION: absolute; TOP: 278px"
					runat="server" NavigateUrl="BatchesNotReceived.aspx" Width="160px" Height="16px">Receive Submissions</asp:HyperLink>
				<asp:LinkButton id="hlEnterBlocks" style="Z-INDEX: 110; LEFT: 24px; POSITION: absolute; TOP: 310px"
					runat="server" Width="191px">Assign Tissues to Blocks</asp:LinkButton>
				<asp:HyperLink id="hlQualityData" style="Z-INDEX: 106; LEFT: 24px; POSITION: absolute; TOP: 342px"
					runat="server" NavigateUrl="BatchesForDispatch.aspx" Width="133px" Height="19">Enter Quality Data</asp:HyperLink>
				<asp:HyperLink id="hlArchiveSubmission" style="Z-INDEX: 112; LEFT: 24px; POSITION: absolute; TOP: 374px"
					runat="server" NavigateUrl="BatchesForArchiving.aspx" Width="144px">Archive Submission</asp:HyperLink>
				<asp:LinkButton id="lbEditQcNotes" style="Z-INDEX: 123; LEFT: 24px; POSITION: absolute; TOP: 406px"
					runat="server" Width="104px">Edit QC Notes</asp:LinkButton>
				<asp:HyperLink id="hlViewHistoricdata" style="Z-INDEX: 127; LEFT: 24px; POSITION: absolute; TOP: 440px"
					runat="server" NavigateUrl="ViewImportedData.aspx">View Old ICC_Sub data</asp:HyperLink>
				<asp:LinkButton id="lbSearchSubmissions" style="Z-INDEX: 124; LEFT: 454px; POSITION: absolute; TOP: 278px"
					runat="server" CausesValidation="False">Search Submissions</asp:LinkButton>
				<asp:HyperLink id="hlSearchOutputs" style="Z-INDEX: 115; LEFT: 454px; POSITION: absolute; TOP: 310px"
					runat="server" NavigateUrl="SearchMenu.aspx" Width="114px">Search Outputs</asp:HyperLink>
				<asp:LinkButton id="lbBlockRefSearch" style="Z-INDEX: 120; LEFT: 454px; POSITION: absolute; TOP: 342px"
					runat="server" Width="138px" Height="19">Search Block Refs</asp:LinkButton>
				<asp:HyperLink id="hlSearchArciveLocation" style="Z-INDEX: 122; LEFT: 454px; POSITION: absolute; TOP: 374px"
					runat="server" Width="182px" Height="19px" NavigateUrl="SearchArchiveLocation.aspx">Search Archive Location</asp:HyperLink>
				<asp:HyperLink id="hlUnUsedHistologyRefs" style="Z-INDEX: 128; LEFT: 454px; POSITION: absolute; TOP: 406px"
					runat="server" Width="233px" Height="19px" NavigateUrl="SearchUnUsedHistologyRefs.aspx">Search Un-used Histology Refs</asp:HyperLink>
				<asp:HyperLink id="hlBlockBookHisto" style="Z-INDEX: 111; LEFT: 454px; POSITION: absolute; TOP: 440px"
					runat="server" NavigateUrl="BookingMenu.aspx" Width="48px"> Booking</asp:HyperLink>
				<asp:HyperLink id="hlEditSubmission" style="Z-INDEX: 113; LEFT: 454px; POSITION: absolute; TOP: 467px"
					runat="server" NavigateUrl="BatchesForEditing.aspx" Width="167px">Edit Submission Status</asp:HyperLink>
				<asp:panel id="Panel1" style="Z-INDEX: 108; LEFT: 10px; POSITION: absolute; TOP: 490px" runat="server"
					Width="621px" Height="14px">
					<HR id="hrMaintenanceLine" style="LEFT: 12px; WIDTH: 96.14%; TOP: 8px; HEIGHT: 1px"
						width="96.14%" SIZE="1">
				</asp:panel>
				<asp:panel id="Panel2" style="Z-INDEX: 109; LEFT: 10px; POSITION: absolute; TOP: 196px" runat="server"
					Width="621px" Height="14px">
					<HR id="Hr1" style="LEFT: 12px; WIDTH: 96.14%; TOP: 66px; HEIGHT: 1px" width="96.14%"
						SIZE="1">
				</asp:panel>
				<asp:LinkButton id="lbUserMaintenance" style="Z-INDEX: 117; LEFT: 24px; POSITION: absolute; TOP: 562px"
					runat="server" Width="134px">User Maintenance</asp:LinkButton>
				<asp:HyperLink id="hlPickListMaintenance" style="Z-INDEX: 102; LEFT: 24px; POSITION: absolute; TOP: 594px"
					runat="server" NavigateUrl="PickListMaintenance.aspx" Width="162px" Height="16px">Pick List Maintenance</asp:HyperLink>
				<asp:HyperLink id="hlAuditLogs" style="Z-INDEX: 107; LEFT: 454px; POSITION: absolute; TOP: 562px"
					runat="server" NavigateUrl="AuditLogMenu.aspx" Width="79px" Height="16px">Audit Logs</asp:HyperLink>
				<DIV id="ctlDivHistopath" style="Z-INDEX: 118; LEFT: 208px; WIDTH: 289px; POSITION: absolute; TOP: 229px; HEIGHT: 41px"
					runat="server">
					<H2>Histopathology Processing</H2>
				</DIV>
				<DIV id="ctlDivMaintenance" style="Z-INDEX: 119; LEFT: 188px; WIDTH: 339px; POSITION: absolute; TOP: 522px; HEIGHT: 42px"
					runat="server">
					<H2>Maintenance and Audit options</H2>
				</DIV>
				<asp:HyperLink id="hlEditHistologyRef" style="Z-INDEX: 126; LEFT: 454px; POSITION: absolute; TOP: 594px"
					runat="server" Width="167px" NavigateUrl="EditHistologyRef.aspx">Edit Sender/Histology Ref</asp:HyperLink>
				
			</DIV>
			<uc1:VLAFooter id="VLAFooter1" runat="server"></uc1:VLAFooter>
		</form>
	</body>
</HTML>
