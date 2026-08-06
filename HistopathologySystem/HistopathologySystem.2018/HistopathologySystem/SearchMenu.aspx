<%@ Register TagPrefix="uc1" TagName="VLAFooter" Src="VLAFooter.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAHeader" Src="VLAHeader.ascx" %>
<%@ Page Language="vb" AutoEventWireup="false" Codebehind="SearchMenu.aspx.vb" Inherits="HistopathologySystem.SearchMenu"%>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<HTML>
	<HEAD>
		<title>SearchMenu</title>
		<meta name="GENERATOR" content="Microsoft Visual Studio.NET 7.0">
		<meta name="CODE_LANGUAGE" content="Visual Basic 7.0">
		<meta name="vs_defaultClientScript" content="JavaScript">
		<meta name="vs_targetSchema" content="http://schemas.microsoft.com/intellisense/ie5">
		<LINK href="Style/vla-ie.css" type="text/css" rel="stylesheet">
	</HEAD>
	<body>
		<form id="Form1" method="post" runat="server">
			<p><uc1:VLAHeader id="VLAHeader1" runat="server"></uc1:VLAHeader></p>
			<p align="center"><asp:LinkButton ID="hlSearchTSETests" Runat="server">Search TSE Outputs</asp:LinkButton></p>
			<P></P>
			<p align="center"><asp:LinkButton ID="hlSearchNonTSETests" Runat="server">Search Non-TSE Outputs</asp:LinkButton></p>
			<P></P>
			<p><uc1:VLAFooter id="VLAFooter1" runat="server"></uc1:VLAFooter>
		</form>
		</P>
	</body>
</HTML>
