<%@ Register TagPrefix="uc1" TagName="VLAFooter" Src="VLAFooter.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAHeader" Src="VLAHeader.ascx" %>
<%@ Page Language="vb" AutoEventWireup="false" Codebehind="AuditLogMenu.aspx.vb" Inherits="HistopathologySystem.AuditLogMenu" %>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<HTML>
	<HEAD>
		<title>AuditLogMenu</title>
		<meta name="GENERATOR" content="Microsoft Visual Studio.NET 7.0">
		<meta name="CODE_LANGUAGE" content="Visual Basic 7.0">
		<meta name="vs_defaultClientScript" content="JavaScript">
		<meta name="vs_targetSchema" content="http://schemas.microsoft.com/intellisense/ie5">
		<link href="Style/vla-ie.css" type="text/css" rel="stylesheet">
	</HEAD>
	<body>
		<form id="Form1" method="post" runat="server">
			<P>
				<uc1:VLAHeader id="VLAHeader1" runat="server"></uc1:VLAHeader></P>
			<P align="center">
				<asp:HyperLink id="hlDailyAuditLog" runat="server" NavigateUrl="AuditLogByDate.aspx">Daily Audit Log</asp:HyperLink></P>
			<P align="center">
				<asp:HyperLink id="hlAuditLogByUser" runat="server" NavigateUrl="AuditLogByUser.aspx">Audit Log By User</asp:HyperLink></P>
			<P align="center">
				<asp:HyperLink id="hlAuditLogSubmission" runat="server" NavigateUrl="AuditLogBySubmission.aspx">Audit Log By Submission</asp:HyperLink></p>
				<uc1:VLAFooter id="VLAFooter1" runat="server"></uc1:VLAFooter>
		</form>
	</body>
</HTML>
