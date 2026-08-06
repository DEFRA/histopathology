<%@ Register TagPrefix="uc1" TagName="VLAFooter" Src="VLAFooter.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAHeader" Src="VLAHeader.ascx" %>
<%@ Page Language="vb" AutoEventWireup="false" Codebehind="FinalPrintBatch.aspx.vb" Inherits="HistopathologySystem.FinalPrintBatch"%>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<HTML>
	<HEAD>
		<title>FinalPrintBatch</title>
		<meta name="GENERATOR" content="Microsoft Visual Studio.NET 7.0">
		<meta name="CODE_LANGUAGE" content="Visual Basic 7.0">
		<meta name="vs_defaultClientScript" content="JavaScript">
		<meta name="vs_targetSchema" content="http://schemas.microsoft.com/intellisense/ie5">
		<LINK href="Style/vla-ie.css" type="text/css" rel="stylesheet">
	</HEAD>
	<body>
		<form id="Form1" method="post" runat="server">
			<uc1:VLAHeader id="VLAHeader1" runat="server"></uc1:VLAHeader>
			<DIV style="WIDTH: 740px; POSITION: relative; HEIGHT: 104px" ms_positioning="GridLayout">
				<asp:Button id="btnPrintBatch" style="Z-INDEX: 101; LEFT: 19px; POSITION: absolute; TOP: 62px"
					runat="server" Text="Print Submission" Height="24" Width="143"></asp:Button>
				<asp:Button id="btnSubmissionNotes" style="Z-INDEX: 105; LEFT: 168px; POSITION: absolute; TOP: 62px"
					runat="server" Text="Print Submission Notes" Width="143px" Height="24px"></asp:Button>
				<asp:Button id="btnHome" style="Z-INDEX: 102; LEFT: 613px; POSITION: absolute; TOP: 62px" runat="server"
					Text="Done" Height="24" Width="78px"></asp:Button>
				<asp:Label id="lblExplain" style="Z-INDEX: 103; LEFT: 19px; POSITION: absolute; TOP: 9px" runat="server"> Submission saved. Click the ‘Print Submission’ button to print the submission form to send to Histopathology with your samples, or then click the Done button.</asp:Label>
				<HR style="Z-INDEX: 104; LEFT: 10px; WIDTH: 92.48%; POSITION: absolute; TOP: 56px; HEIGHT: 1px"
					width="92.48%" SIZE="1">
			</DIV>
			<uc1:VLAFooter id="VLAFooter1" runat="server"></uc1:VLAFooter>
		</form>
	</body>
</HTML>
