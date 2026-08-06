<%@ Register TagPrefix="uc1" TagName="VLAFooter" Src="VLAFooter.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAHeader" Src="VLAHeader.ascx" %>
<%@ Page Language="vb" AutoEventWireup="false" Codebehind="SearchSender.aspx.vb" Inherits="HistopathologySystem.SearchSender"%>
<%@ Register TagPrefix="uc1" TagName="DataGridPager" Src="DataGridPager.ascx" %>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<HTML>
	<HEAD>
		<title>SearchSender</title>
		<meta content="Microsoft Visual Studio.NET 7.0" name="GENERATOR">
		<meta content="Visual Basic 7.0" name="CODE_LANGUAGE">
		<meta content="JavaScript" name="vs_defaultClientScript">
		<meta content="http://schemas.microsoft.com/intellisense/ie5" name="vs_targetSchema">
		<LINK href="Style/vla-ie.css" type="text/css" rel="stylesheet">
	</HEAD>
	<body>
		<form id="Form1" method="post" runat="server">
			<uc1:vlaheader id="VLAHeader1" runat="server"></uc1:vlaheader>
			<DIV style="WIDTH: 640px; POSITION: relative; HEIGHT: 95px" ms_positioning="GridLayout">
				<HR style="Z-INDEX: 101; LEFT: 8px; WIDTH: 98.4%; POSITION: absolute; TOP: 90px; HEIGHT: 1px"
					width="98.4%" SIZE="1">
				<asp:Label id="lblSenderRef" style="Z-INDEX: 102; LEFT: 16px; POSITION: absolute; TOP: 16px"
					runat="server">Sender Ref</asp:Label>
				<asp:TextBox id="txtSenderRef" style="Z-INDEX: 103; LEFT: 104px; POSITION: absolute; TOP: 16px"
					runat="server" Width="152px" Enabled="False"></asp:TextBox>
				<asp:Label id="lblExplain" style="Z-INDEX: 104; LEFT: 16px; POSITION: absolute; TOP: 47px"
					runat="server" Width="601px">The following matches for your sample reference were found. Click the required reference or click the Back button to return to the previous page.</asp:Label></DIV>
			<DIV style="WIDTH: 444px"><asp:datagrid id="grdSenders" runat="server" AutoGenerateColumns="False" PageSize="100" AllowPaging="True">
					<SelectedItemStyle CssClass="GridSelectedItemSmall"></SelectedItemStyle>
					<EditItemStyle CssClass="GridEditItemSmall"></EditItemStyle>
					<ItemStyle CssClass="GridItemSmall"></ItemStyle>
					<HeaderStyle CssClass="GridHeader"></HeaderStyle>
					<Columns>
						<asp:ButtonColumn Text="&lt;img src=&quot;Images/GridPager/sel.gif&quot;&gt;" CommandName="Select">
							<ItemStyle HorizontalAlign="Left" Width="20px"></ItemStyle>
						</asp:ButtonColumn>
						<asp:BoundColumn DataField="SenderRef" SortExpression="SenderRef" HeaderText="Sender Ref">
							<ItemStyle HorizontalAlign="Left" Width="200px"></ItemStyle>
						</asp:BoundColumn>
						<asp:BoundColumn DataField="HistologyRef" SortExpression="HistologyRef" HeaderText="Histology Ref">
							<ItemStyle HorizontalAlign="Left" Width="200px"></ItemStyle>
						</asp:BoundColumn>
					</Columns>
					<PagerStyle Visible="False"></PagerStyle>
				</asp:datagrid><uc1:datagridpager id="SenderPager" runat="server"></uc1:datagridpager></DIV>
			<DIV id="ctlMessageDiv" style="WIDTH: 640px; HEIGHT: 8px" runat="Server" ms_positioning="FlowLayout"></DIV>
			<DIV style="WIDTH: 640px; POSITION: relative; HEIGHT: 40px" ms_positioning="GridLayout"><asp:button id="btnCancel" style="Z-INDEX: 103; LEFT: 552px; POSITION: absolute; TOP: 16px"
					runat="server" Width="80" Height="22px" Text="Back" CausesValidation="False"></asp:button>
				<HR style="Z-INDEX: 104; LEFT: 8px; WIDTH: 98.4%; POSITION: absolute; TOP: 8px; HEIGHT: 1px"
					width="98.4%" SIZE="1">
			</DIV>
			<uc1:vlafooter id="VLAFooter1" runat="server"></uc1:vlafooter></form>
	</body>
</HTML>
