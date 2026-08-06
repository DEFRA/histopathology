<%@ Register TagPrefix="uc1" TagName="VLAFooter" Src="VLAFooter.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAHeader" Src="VLAHeader.ascx" %>
<%@ Page Language="vb" AutoEventWireup="false" Codebehind="BatchBlocks.aspx.vb" Inherits="HistopathologySystem.BatchBlocks" smartNavigation="True"%>
<%@ Register TagPrefix="uc1" TagName="Batch" Src="Batch.ascx" %>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<HTML>
	<HEAD>
		<title>BatchBlocks</title>
		<meta content="Microsoft Visual Studio.NET 7.0" name="GENERATOR">
		<meta content="Visual Basic 7.0" name="CODE_LANGUAGE">
		<meta content="JavaScript" name="vs_defaultClientScript">
		<meta content="http://schemas.microsoft.com/intellisense/ie5" name="vs_targetSchema">
		<LINK href="Style/vla-ie.css" type="text/css" rel="stylesheet">
	</HEAD>
	<body>
		<form id="Form1" method="post" runat="server">
			<uc1:vlaheader id="VLAHeader1" runat="server"></uc1:vlaheader>
			<DIV style="WIDTH: 144px"><uc1:batch id="Batch1" runat="server"></uc1:batch></DIV>
			<DIV style="WIDTH: 806px; POSITION: relative; HEIGHT: 124px" ms_positioning="GridLayout"><asp:label id="Label1" style="Z-INDEX: 104; LEFT: 10px; POSITION: absolute; TOP: 9px" runat="server"
					Height="46px" Width="751px">The following table displays all samples, blocks, tissues and tests for the current submission. A green star next to the Sender Ref indicates that not all tissues for that sample have been assigned to blocks.</asp:label>
				<DIV id="ctlDivGrid" style="Z-INDEX: 102; LEFT: 10px; WIDTH: 597px; POSITION: absolute; TOP: 94px; HEIGHT: 23px"
					runat="server" ms_positioning="FlowLayout"><asp:linkbutton id="lbExpandAll" runat="server" style="LEFT: 0px; TOP: 1px">Click here to expand all tissues, or </asp:linkbutton>&nbsp;
					<asp:linkbutton id="lbCollapseAll" runat="server" style="LEFT: 258px; TOP: 13px">click here to collapse all tissues.</asp:linkbutton></DIV>
				<asp:Label id="Label2" style="Z-INDEX: 103; LEFT: 10px; POSITION: absolute; TOP: 52px" runat="server">Do not double click a row, after clicking a row wait until it becomes highlighted with blue before selecting another row.</asp:Label>
				<asp:checkbox id="chkAllTissuesAssigned" style="Z-INDEX: 100; LEFT: 12px; POSITION: absolute; TOP: 10px"
					runat="server" Width="203px" Visible="False" TabIndex="-1"></asp:checkbox>
			</DIV>
			<DIV style="WIDTH: 1044px"><asp:datagrid id="grdBlockSummary" runat="server" AutoGenerateColumns="False">
					<SelectedItemStyle CssClass="GridSelectedItemSmall"></SelectedItemStyle>
					<EditItemStyle CssClass="GridEditItemSmall"></EditItemStyle>
					<ItemStyle CssClass="GridItemSmall"></ItemStyle>
					<HeaderStyle CssClass="GridHeader"></HeaderStyle>
					<Columns>
						<asp:ButtonColumn Text="&lt;img src=&quot;Images/GridPager/sel.gif&quot;&gt;" CommandName="Select">
							<ItemStyle HorizontalAlign="Left" Width="20px"></ItemStyle>
						</asp:ButtonColumn>
						<asp:BoundColumn Visible="False" DataField="SenderRef"></asp:BoundColumn>
						<asp:TemplateColumn HeaderText="Sender Ref">
							<ItemStyle HorizontalAlign="Left" Width="115px"></ItemStyle>
							<ItemTemplate>
								<asp:Literal ID="litText" Runat="server"></asp:Literal>
							</ItemTemplate>
						</asp:TemplateColumn>
						<asp:BoundColumn DataField="HistologyRef" SortExpression="HistologyRef" HeaderText="Histology Ref">
							<ItemStyle HorizontalAlign="Left" Width="115px"></ItemStyle>
						</asp:BoundColumn>
						<asp:BoundColumn DataField="BlockRef" SortExpression="BlockRef" HeaderText="Block Ref">
							<ItemStyle HorizontalAlign="Left" Width="30px"></ItemStyle>
						</asp:BoundColumn>
						<asp:ButtonColumn Text="&lt;img src=&quot;Images/plus.gif&quot;&gt;" CommandName="ExpandTissues">
							<ItemStyle HorizontalAlign="Left" Width="20px"></ItemStyle>
						</asp:ButtonColumn>
						<asp:BoundColumn DataField="TissueDetails" HeaderText="Tissue Details">
							<ItemStyle HorizontalAlign="Left" Width="155px"></ItemStyle>
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
						<asp:TemplateColumn HeaderText="Special Stain">
							<ItemStyle HorizontalAlign="Left" Width="75px"></ItemStyle>
							<ItemTemplate>
								<asp:CheckBox ID="cbSpecialStainDisplay" Runat="server" Enabled="False"></asp:CheckBox>
							</ItemTemplate>
						</asp:TemplateColumn>
						<asp:BoundColumn Visible="False" HeaderText="SenderRef"></asp:BoundColumn>
					</Columns>
					<PagerStyle Visible="False"></PagerStyle>
				</asp:datagrid></DIV>
			<DIV style="WIDTH: 845px; POSITION: relative; HEIGHT: 57px" ms_positioning="GridLayout"><asp:button id="btnAddSample" style="Z-INDEX: 101; LEFT: 0px; POSITION: absolute; TOP: 16px"
					runat="server" Height="24px" Width="105px" CausesValidation="False" Text="Add Sample" ToolTip="Use this button to add a new sample to the current submission"></asp:button><asp:button id="btnEditSample" style="Z-INDEX: 102; LEFT: 112px; POSITION: absolute; TOP: 16px"
					runat="server" Height="24px" Width="105px" CausesValidation="False" Text="Edit Sample"></asp:button>
				<asp:button id="btnDeleteSample" style="Z-INDEX: 103; LEFT: 224px; POSITION: absolute; TOP: 16px"
					runat="server" Height="24" Width="106" CausesValidation="False" Text="Delete Sample"></asp:button>
				<asp:Button id="btnCopySamples" style="Z-INDEX: 108; LEFT: 336px; POSITION: absolute; TOP: 16px"
					runat="server" Width="176px" Text="Copy From Prev. Submission" Height="24px"></asp:Button>
				<asp:button id="btSubmit" style="Z-INDEX: 104; LEFT: 704px; POSITION: absolute; TOP: 16px" runat="server"
					Height="24px" Width="65px" Text="Done"></asp:button>
				<asp:button id="btnCancel" style="Z-INDEX: 105; LEFT: 624px; POSITION: absolute; TOP: 16px"
					runat="server" Height="24px" Width="73px" CausesValidation="False" Text="Cancel"></asp:button>
				<HR style="Z-INDEX: 106; LEFT: 18px; WIDTH: 90.16%; POSITION: absolute; TOP: 8px; HEIGHT: 1px"
					width="90.16%" SIZE="1">
			</DIV>
			<DIV id="ctlDIV" style="WIDTH: 776px; HEIGHT: 15px" runat="server" ms_positioning="FlowLayout"></DIV>
			<uc1:vlafooter id="VLAFooter1" runat="server"></uc1:vlafooter>
			<P></P>
		</form>
	</body>
</HTML>
