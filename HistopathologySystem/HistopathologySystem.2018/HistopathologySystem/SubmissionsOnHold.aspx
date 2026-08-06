<%@ Register TagPrefix="uc1" TagName="VLAFooter" Src="VLAFooter.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAHeader" Src="VLAHeader.ascx" %>
<%@ Page Language="vb" AutoEventWireup="false" Codebehind="SubmissionsOnHold.aspx.vb" Inherits="HistopathologySystem.SubmissionsOnHold"%>
<%@ Register TagPrefix="uc1" TagName="DataGridPager" Src="DataGridPager.ascx" %>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<HTML>
	<HEAD>
		<title>SubmissionsOnHold</title>
		<meta content="Microsoft Visual Studio.NET 7.0" name="GENERATOR">
		<meta content="Visual Basic 7.0" name="CODE_LANGUAGE">
		<meta content="JavaScript" name="vs_defaultClientScript">
		<meta content="http://schemas.microsoft.com/intellisense/ie5" name="vs_targetSchema">
		<LINK href="Style/vla-ie.css" type="text/css" rel="stylesheet">
	</HEAD>
	<body>
		<form id="Form1" method="post" runat="server">
			<uc1:vlaheader id="VLAHeader1" runat="server"></uc1:vlaheader>
			<DIV style="WIDTH: 718px; POSITION: relative; HEIGHT: 46px" ms_positioning="GridLayout">
				<HR style="Z-INDEX: 103; LEFT: 8px; POSITION: absolute; TOP: 40px; HEIGHT: 1px" width="98%" SIZE="1">
			</DIV>
			<DIV style="WIDTH: 405px"><asp:datagrid id="grdSubmissions" runat="server" PageSize="15" AutoGenerateColumns="False" AllowSorting="True">
					<SelectedItemStyle CssClass="GridSelectedItem"></SelectedItemStyle>
					<EditItemStyle CssClass="GridEditItem"></EditItemStyle>
					<ItemStyle CssClass="GridItem"></ItemStyle>
					<HeaderStyle CssClass="GridHeader"></HeaderStyle>
					<Columns>
						<asp:ButtonColumn Text="&lt;img src=&quot;Images/GridPager/sel.gif&quot;&gt;" CommandName="Select">
							<ItemStyle HorizontalAlign="Left" Width="20px"></ItemStyle>
						</asp:ButtonColumn>
						<asp:BoundColumn DataField="SenderRef" SortExpression="SenderRef" ReadOnly="True" HeaderText="Sender Ref">
							<ItemStyle HorizontalAlign="Left" Width="150px"></ItemStyle>
						</asp:BoundColumn>
						<asp:BoundColumn DataField="HistologyRef" SortExpression="HistologyRef" ReadOnly="True" HeaderText="HistologyRef">
							<ItemStyle HorizontalAlign="Left" Width="150px"></ItemStyle>
						</asp:BoundColumn>
						<asp:TemplateColumn SortExpression="OnHold" HeaderText="On Hold">
							<ItemStyle HorizontalAlign="Center" Width="100px"></ItemStyle>
							<ItemTemplate>
								<asp:CheckBox ID="cbOnHoldDisplay" runat="server" Enabled="False"></asp:CheckBox>
							</ItemTemplate>
							<EditItemTemplate>
								<asp:CheckBox ID="cbOnHoldEdit" runat="server" Enabled="True"></asp:CheckBox>
							</EditItemTemplate>
						</asp:TemplateColumn>
					</Columns>
				</asp:datagrid><uc1:datagridpager id="PagerSubmissions" runat="server"></uc1:datagridpager></DIV>
			<DIV style="WIDTH: 718px; POSITION: relative; HEIGHT: 52px" ms_positioning="GridLayout"><asp:button id="btnDone" style="Z-INDEX: 102; LEFT: 617px; POSITION: absolute; TOP: 14px" runat="server" Text="Done" Width="89" Height="25"></asp:button>
				<HR style="Z-INDEX: 103; LEFT: 8px; POSITION: absolute; TOP: 5px; HEIGHT: 1px" width="98%" SIZE="1">
			</DIV>
			<uc1:vlafooter id="VLAFooter1" runat="server"></uc1:vlafooter></form>
	</body>
</HTML>
