<%@ Register TagPrefix="uc1" TagName="VLAFooter" Src="VLAFooter.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAHeader" Src="VLAHeader.ascx" %>
<%@ Page Language="vb" AutoEventWireup="false" Codebehind="BookingMenu.aspx.vb" Inherits="HistopathologySystem.BookingMenu"%>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<HTML>
	<HEAD>
		<title>BookingMenu</title>
		<meta name="GENERATOR" content="Microsoft Visual Studio .NET 7.1">
		<meta name="CODE_LANGUAGE" content="Visual Basic .NET 7.1">
		<meta name="vs_defaultClientScript" content="JavaScript">
		<meta name="vs_targetSchema" content="http://schemas.microsoft.com/intellisense/ie5">
		<LINK href="Style/vla-ie.css" type="text/css" rel="stylesheet">
	</HEAD>
	<body>
		<form id="Form1" method="post" runat="server">
			<P>
				<uc1:VLAHeader id="VLAHeader1" runat="server"></uc1:VLAHeader></P>
			<DIV style="WIDTH: 744px; POSITION: relative; HEIGHT: 100px" ms_positioning="GridLayout">
				<asp:HyperLink id="hlBookHistologyRefs" style="Z-INDEX: 101; LEFT: 248px; POSITION: absolute; TOP: 24px"
					runat="server" NavigateUrl="BookHistologyRef.aspx">Book Non-PG Histology Ref</asp:HyperLink>
				<asp:HyperLink id="hlBookBlocks" style="Z-INDEX: 102; LEFT: 288px; POSITION: absolute; TOP: 48px"
					runat="server" NavigateUrl="BookBlockRef.aspx">Book Blocks</asp:HyperLink></DIV>
			<P>
				<uc1:VLAFooter id="VLAFooter1" runat="server"></uc1:VLAFooter></P>
			<P>&nbsp;</P>
		</form>
	</body>
</HTML>
