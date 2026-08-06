<%@ Control Language="vb" AutoEventWireup="false" Codebehind="VLAHeader.ascx.vb" Inherits="HistopathologySystem.VLAHeader" TargetSchema="http://schemas.microsoft.com/intellisense/ie5" %>
<meta content="True" name="vs_showGrid">
<a name="top"></a>
<DIV style="BORDER-RIGHT: #003399 2px solid; PADDING-RIGHT: 0px; BORDER-TOP: #003399 2px solid; PADDING-LEFT: 0px; PADDING-BOTTOM: 0px; MARGIN: 0px; BORDER-LEFT: #003399 2px solid; WIDTH: 780px; PADDING-TOP: 0px; BORDER-BOTTOM: #003399 2px solid; POSITION: relative; HEIGHT: 130px" ms_positioning="GridLayout"><asp:label id="lblAppTitle" style="Z-INDEX: 102; LEFT: 112px; POSITION: absolute; TOP: 4px" runat="server" Width="448px" CssClass="AppTitle">Histopathology Submissions</asp:label><asp:label id="lblPageTitle" style="Z-INDEX: 101; LEFT: 112px; POSITION: absolute; TOP: 68px" runat="server" CssClass="PageTitle"></asp:label>
	<DIV style="Z-INDEX: 103; LEFT: 0px; WIDTH: 100px; POSITION: absolute; TOP: 0px; HEIGHT: 100%">
		<P><asp:image id="Image1" runat="server" ImageUrl="Images/vlalogoExtended.gif"></asp:image></P>
	</DIV>
	<DIV style="BORDER-RIGHT: #003399 2px; BORDER-TOP: #003399 2px; Z-INDEX: 104; RIGHT: 0px; BORDER-LEFT: #003399 2px solid; WIDTH: 184px; BORDER-BOTTOM: #003399 2px; POSITION: absolute; TOP: 0px; HEIGHT: 100%; BACKGROUND-COLOR: white" ms_positioning="GridLayout">
		<asp:label id="lblUser" style="Z-INDEX: 102; LEFT: 4px; POSITION: absolute; TOP: 2px" runat="server" Width="161px" CssClass="topnavtext"></asp:label>
		<asp:label id="lblLab" style="Z-INDEX: 103; LEFT: 4px; POSITION: absolute; TOP: 26px" runat="server" Width="161px" CssClass="topnavtext"></asp:label>
		<asp:Label id="lblArea" style="Z-INDEX: 105; LEFT: 4px; POSITION: absolute; TOP: 50px" runat="server" CssClass="topnavtext" Width="161px" Height="19px">[lblArea]</asp:Label>
		<asp:Label ID="lblSubmission" style="Z-INDEX: 110; LEFT: 4px; POSITION: absolute; TOP: 74px" runat="server" Width="161px" CssClass="topnavtextGreen">[Submission ID]</asp:Label>
		<DIV style="PADDING-RIGHT: 0px; PADDING-LEFT: 0px; Z-INDEX: 104; RIGHT: 0px; LEFT: 0px; PADDING-BOTTOM: 0px; MARGIN: 0px; WIDTH: 184px; BORDER-TOP-STYLE: none; BOTTOM: 0px; PADDING-TOP: 0px; BORDER-BOTTOM: #003399 1px; BORDER-RIGHT-STYLE: none; BORDER-LEFT-STYLE: none; POSITION: absolute; HEIGHT: 27px; BACKGROUND-COLOR: #003399" ms_positioning="GridLayout">
			<asp:HyperLink id="lnkHelp" runat="server" style="Z-INDEX: 101; LEFT: 8px; POSITION: absolute; TOP: 8px" CssClass="topnavlinks" Target="_blank">Help</asp:HyperLink>
			<asp:LinkButton id="lnkHome" style="Z-INDEX: 103; LEFT: 136px; POSITION: absolute; TOP: 8px" runat="server" CssClass="topnavlinks" CausesValidation="False">Home</asp:LinkButton></DIV>
	</DIV>
	<asp:Label id="lblVersion" style="Z-INDEX: 105; LEFT: 112px; POSITION: absolute; TOP: 48px" Width="344px" runat="server" Font-Bold="True">Debug Version</asp:Label>
	<asp:Label id="lblBreadCrumb" style="Z-INDEX: 106; LEFT: 112px; POSITION: absolute; TOP: 98px" Width="467px" runat="server" CssClass="BreadCrumb"></asp:Label>
</DIV>
