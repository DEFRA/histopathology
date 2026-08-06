<%@ Page Language="vb" AutoEventWireup="false" Codebehind="ArchiveMenu.aspx.vb" Inherits="HistopathologySystem.ArchiveMenu"%>
<%@ Register TagPrefix="uc1" TagName="VLAHeader" Src="VLAHeader.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAFooter" Src="VLAFooter.ascx" %>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<HTML>
	<HEAD>
		<title>ArchiveMenu</title>
		<meta content="Microsoft Visual Studio.NET 7.0" name="GENERATOR">
		<meta content="Visual Basic 7.0" name="CODE_LANGUAGE">
		<meta content="JavaScript" name="vs_defaultClientScript">
		<meta content="http://schemas.microsoft.com/intellisense/ie5" name="vs_targetSchema">
		<LINK href="Style/vla-ie.css" type="text/css" rel="stylesheet">
	</HEAD>
	<body>
		<form id="Form1" method="post" runat="server">
			<P>
				<uc1:VLAHeader id="VLAHeader1" runat="server"></uc1:VLAHeader></P>
			<p align="center">
				<asp:LinkButton ID="hlArchiveBlocks" Runat="server">Archive Blocks</asp:LinkButton></p>
			<p align="center">
				<asp:LinkButton ID="hlArchiveTissues" Runat="server">Archive Tissues</asp:LinkButton></p>
			<P align="center">
			</P>
			<DIV style="WIDTH: 834px; POSITION: relative; HEIGHT: 44px" ms_positioning="GridLayout">
				<asp:Button id="btnCancel" style="Z-INDEX: 101; LEFT: 633px; POSITION: absolute; TOP: 9px" runat="server" Text="Cancel" Width="74px"></asp:Button></DIV>
			<P></P>
			<uc1:VLAFooter id="VLAFooter1" runat="server"></uc1:VLAFooter>
			<P></P>
		</form>
	</body>
</HTML>
