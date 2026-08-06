<%@ Register TagPrefix="uc1" TagName="VLAHeader" Src="VLAHeader.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAFooter" Src="VLAFooter.ascx" %>
<%@ Page Language="vb" AutoEventWireup="false" Codebehind="CopyBatchBlocksSummary.aspx.vb" Inherits="HistopathologySystem.CopyBatchBlocksSummary"%>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<HTML>
	<HEAD>
		<title>BatchBlockSummary</title>
		<meta content="Microsoft Visual Studio.NET 7.0" name="GENERATOR">
		<meta content="Visual Basic 7.0" name="CODE_LANGUAGE">
		<meta content="JavaScript" name="vs_defaultClientScript">
		<meta content="http://schemas.microsoft.com/intellisense/ie5" name="vs_targetSchema">
		<LINK href="Style/vla-ie.css" type="text/css" rel="stylesheet">
	</HEAD>
	<body>
		<form id="Form1" method="post" runat="server">
			<uc1:vlaheader id="VLAHeader1" runat="server"></uc1:vlaheader>
			<DIV style="WIDTH: 734px; POSITION: relative; HEIGHT: 102px" ms_positioning="GridLayout">
				<asp:label id="Label1" style="Z-INDEX: 100; LEFT: 10px; POSITION: absolute; TOP: 9px" runat="server">The following table displays all samples, blocks, tissues and tests for the current submission. Do not double click a row, after clicking a row wait until it becomes highlighted with blue before selecting another row.</asp:label>
				<DIV id="ctlDivGrid" style="Z-INDEX: 102; LEFT: 10px; WIDTH: 682px; POSITION: absolute; TOP: 71px; HEIGHT: 7px" runat="server" ms_positioning="FlowLayout">
					<asp:LinkButton id="lbExpandAll" runat="server">Click here to expand all tissues, or </asp:LinkButton>&nbsp;
					<asp:LinkButton id="lbCollapseAll" runat="server">click here to collapse all tissues.</asp:LinkButton></DIV>
				<HR style="Z-INDEX: 104; LEFT: 8px; POSITION: absolute; TOP: 96px; HEIGHT: 1px" width="96.68%" SIZE="1">
			</DIV>
			<DIV style="WIDTH: 905px"><asp:datagrid id="grdBatchSummary" runat="server" AutoGenerateColumns="False">
					<SelectedItemStyle CssClass="GridSelectedItemSmall"></SelectedItemStyle>
					<EditItemStyle CssClass="GridEditItemSmall"></EditItemStyle>
					<ItemStyle CssClass="GridItemSmall"></ItemStyle>
					<HeaderStyle CssClass="GridHeader"></HeaderStyle>
					<Columns>
						<asp:ButtonColumn Text="&lt;img src=&quot;Images/GridPager/sel.gif&quot;&gt;" CommandName="Select">
							<ItemStyle HorizontalAlign="Left" Width="20px"></ItemStyle>
						</asp:ButtonColumn>
						<asp:BoundColumn DataField="SenderRef" SortExpression="SenderRef" HeaderText="Sender Ref">
							<ItemStyle HorizontalAlign="Left" Width="120px"></ItemStyle>
						</asp:BoundColumn>
						<asp:BoundColumn DataField="BlockRef" SortExpression="BlockRef" HeaderText="Block Ref">
							<ItemStyle HorizontalAlign="Left" Width="65px"></ItemStyle>
						</asp:BoundColumn>
						<asp:ButtonColumn Text="&lt;img src=&quot;Images/plus.gif&quot;&gt;" CommandName="ExpandTissues">
							<ItemStyle HorizontalAlign="Left" Width="20px"></ItemStyle>
						</asp:ButtonColumn>
						<asp:BoundColumn DataField="TissueDetails" HeaderText="Tissue Details">
							<ItemStyle HorizontalAlign="Left" Width="140px"></ItemStyle>
						</asp:BoundColumn>
						<asp:TemplateColumn HeaderText="Archive">
							<ItemStyle HorizontalAlign="Left" Width="65px"></ItemStyle>
							<ItemTemplate>
								<asp:CheckBox ID="cbArchiveDisplay" Runat="server" Enabled="False"></asp:CheckBox>
							</ItemTemplate>
						</asp:TemplateColumn>
						<asp:TemplateColumn HeaderText="EO">
							<ItemStyle HorizontalAlign="Left" Width="30px"></ItemStyle>
							<ItemTemplate>
								<asp:CheckBox ID="cbEODisplay" Runat="server" Enabled="False"></asp:CheckBox>
							</ItemTemplate>
						</asp:TemplateColumn>
						<asp:TemplateColumn HeaderText="H&amp;E">
							<ItemStyle HorizontalAlign="Left" Width="30px"></ItemStyle>
							<ItemTemplate>
								<asp:CheckBox ID="cbHAndEDisplay" Runat="server" Enabled="False"></asp:CheckBox>
							</ItemTemplate>
						</asp:TemplateColumn>
						<asp:TemplateColumn HeaderText="H&amp;E (BSE)">
							<ItemStyle HorizontalAlign="Left" Width="65px"></ItemStyle>
							<ItemTemplate>
								<asp:CheckBox ID="cbHAndEBseDisplay" Runat="server" Enabled="False"></asp:CheckBox>
							</ItemTemplate>
						</asp:TemplateColumn>
						<asp:TemplateColumn HeaderText="Special Stain">
							<ItemStyle HorizontalAlign="Left" Width="75px"></ItemStyle>
							<ItemTemplate>
								<asp:CheckBox ID="cbSpecialStainDisplay" Runat="server" Enabled="False"></asp:CheckBox>
							</ItemTemplate>
						</asp:TemplateColumn>
						<asp:TemplateColumn HeaderText="IHC Prp">
							<ItemStyle HorizontalAlign="Left" Width="45px"></ItemStyle>
							<ItemTemplate>
								<asp:CheckBox ID="cbIHCPrpDisplay" Runat="server" Enabled="False"></asp:CheckBox>
							</ItemTemplate>
						</asp:TemplateColumn>
						<asp:TemplateColumn HeaderText="IHC Other">
							<ItemStyle HorizontalAlign="Left" Width="50px"></ItemStyle>
							<ItemTemplate>
								<asp:CheckBox ID="cbIHCOtherDisplay" Runat="server" Enabled="False"></asp:CheckBox>
							</ItemTemplate>
						</asp:TemplateColumn>
						<asp:TemplateColumn SortExpression="RepeatBlock" HeaderText="Additional Request">
							<ItemStyle HorizontalAlign="Left" Width="70px"></ItemStyle>
							<ItemTemplate>
								<asp:CheckBox ID="cbRepeatBlockDisplay" Runat="server" Enabled="False"></asp:CheckBox>
							</ItemTemplate>
						</asp:TemplateColumn>
					</Columns>
					<PagerStyle Visible="False"></PagerStyle>
				</asp:datagrid></DIV>
			<DIV style="WIDTH: 743px; POSITION: relative; HEIGHT: 54px" ms_positioning="GridLayout">
				<asp:button id="btnCancel" style="Z-INDEX: 104; LEFT: 616px; POSITION: absolute; TOP: 17px" runat="server" Text="Done" Width="113" Height="24px"></asp:button>
				<HR style="Z-INDEX: 103; LEFT: 11px; WIDTH: 97%; POSITION: absolute; TOP: 9px; HEIGHT: 1px" width="97%" SIZE="1">
			</DIV>
			<uc1:vlafooter id="VLAFooter1" runat="server"></uc1:vlafooter></form>
	</body>
</HTML>
