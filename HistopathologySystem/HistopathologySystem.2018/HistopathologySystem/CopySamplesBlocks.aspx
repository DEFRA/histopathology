<%@ Register TagPrefix="uc1" TagName="VLAFooter" Src="VLAFooter.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAHeader" Src="VLAHeader.ascx" %>
<%@ Page Language="vb" AutoEventWireup="false" Codebehind="CopySamplesBlocks.aspx.vb" Inherits="HistopathologySystem.CopySamplesBlocks"%>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<HTML>
	<HEAD>
		<title>CopySamplesBlocks</title>
		<meta content="False" name="vs_snapToGrid">
		<meta content="Microsoft Visual Studio .NET 7.1" name="GENERATOR">
		<meta content="Visual Basic .NET 7.1" name="CODE_LANGUAGE">
		<meta content="JavaScript" name="vs_defaultClientScript">
		<meta content="http://schemas.microsoft.com/intellisense/ie5" name="vs_targetSchema">
		<LINK href="Style/vla-ie.css" type="text/css" rel="stylesheet">
	</HEAD>
	<body>
		<form id="Form1" method="post" runat="server">
			<uc1:vlaheader id="VLAHeader1" runat="server"></uc1:vlaheader>
			<DIV style="WIDTH: 764px; POSITION: relative; HEIGHT: 80px" ms_positioning="GridLayout"><asp:label id="Label1" style="Z-INDEX: 101; LEFT: 10px; POSITION: absolute; TOP: 9px" runat="server">The following table displays the blocks for the selected sample within the submission. Select the blocks that are required to be copied and click the Finish button to copy these blocks.</asp:label>
				<DIV id="ctlDivGrid" style="Z-INDEX: 102; LEFT: 10px; WIDTH: 408px; POSITION: absolute; TOP: 53px; HEIGHT: 16px"
					runat="server" ms_positioning="FlowLayout"><asp:linkbutton id="lbExpandAll" runat="server" CausesValidation="False">Click here to expand all blocks, or </asp:linkbutton>&nbsp;
					<asp:linkbutton id="lbCollapseAll" runat="server" CausesValidation="False">click here to collapse all blocks.</asp:linkbutton></DIV>
				<asp:checkbox id="cbSelectAll" style="Z-INDEX: 103; LEFT: 436px; POSITION: absolute; TOP: 50px"
					runat="server" Text="Select all blocks" AutoPostBack="True" TextAlign="Left"></asp:checkbox></DIV>
			<DIV style="WIDTH: 820px"><asp:datagrid id="grdBlockSummary" runat="server" AutoGenerateColumns="False">
					<SelectedItemStyle CssClass="GridSelectedItemSmall"></SelectedItemStyle>
					<EditItemStyle CssClass="GridEditItemSmall"></EditItemStyle>
					<ItemStyle CssClass="GridItemSmall"></ItemStyle>
					<HeaderStyle CssClass="GridHeader"></HeaderStyle>
					<Columns>
						<asp:BoundColumn DataField="BlockRef" SortExpression="BlockRef" HeaderText="Block Ref">
							<ItemStyle HorizontalAlign="Left" Width="65px"></ItemStyle>
						</asp:BoundColumn>
						<asp:ButtonColumn Text="&lt;img src=&quot;Images/plus.gif&quot;&gt;" CommandName="ExpandTissues">
							<ItemStyle HorizontalAlign="Left" Width="20px"></ItemStyle>
						</asp:ButtonColumn>
						<asp:BoundColumn DataField="TissueDetails" HeaderText="Tissue Details">
							<ItemStyle HorizontalAlign="Left" Width="220px"></ItemStyle>
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
						<asp:TemplateColumn HeaderText="IHC Prp">
							<ItemStyle HorizontalAlign="Left" Width="60px"></ItemStyle>
							<ItemTemplate>
								<asp:CheckBox ID="cbIHCPrpDisplay" Runat="server" Enabled="False"></asp:CheckBox>
							</ItemTemplate>
						</asp:TemplateColumn>
						<asp:TemplateColumn HeaderText="IHC Other">
							<ItemStyle HorizontalAlign="Left" Width="70px"></ItemStyle>
							<ItemTemplate>
								<asp:CheckBox ID="cbIHCOtherDisplay" Runat="server" Enabled="False"></asp:CheckBox>
							</ItemTemplate>
						</asp:TemplateColumn>
						<asp:TemplateColumn HeaderText="Special Stain">
							<ItemStyle HorizontalAlign="Left" Width="85px"></ItemStyle>
							<ItemTemplate>
								<asp:CheckBox ID="cbSpecialStainDisplay" Runat="server" Enabled="False"></asp:CheckBox>
							</ItemTemplate>
						</asp:TemplateColumn>
						<asp:TemplateColumn HeaderText="Select">
							<ItemStyle HorizontalAlign="Left" Width="50px"></ItemStyle>
							<ItemTemplate>
								<asp:CheckBox ID="cbSelected" Runat="server" Enabled="true" AutoPostBack="True"></asp:CheckBox>
							</ItemTemplate>
						</asp:TemplateColumn>
					</Columns>
					<PagerStyle Visible="False"></PagerStyle>
				</asp:datagrid></DIV>
			<DIV style="WIDTH: 785px; POSITION: relative; HEIGHT: 54px" ms_positioning="GridLayout">
				<HR style="Z-INDEX: 101; LEFT: 0px; POSITION: absolute; TOP: 8px; HEIGHT: 1px" width="97%"
					SIZE="1">
				<asp:button id="btnBack" style="Z-INDEX: 102; LEFT: 544px; POSITION: absolute; TOP: 16px" runat="server"
					CausesValidation="False" Width="97" Text="Back" Height="24"></asp:button><asp:button id="btnFinish" style="Z-INDEX: 103; LEFT: 648px; POSITION: absolute; TOP: 16px"
					runat="server" CausesValidation="False" Width="96px" Text="Finish" Height="24px"></asp:button><asp:button id="btnCancel" style="Z-INDEX: 104; LEFT: 440px; POSITION: absolute; TOP: 16px"
					runat="server" Width="97px" Text="Cancel" Height="24px"></asp:button></DIV>
			<uc1:vlafooter id="VLAFooter1" runat="server"></uc1:vlafooter></form>
	</body>
</HTML>
