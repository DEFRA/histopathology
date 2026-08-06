<%@ Register TagPrefix="uc1" TagName="HistologyRef" Src="HistologyRef.ascx" %>
<%@ Page Language="vb" AutoEventWireup="false" Codebehind="SubmissionDetailsBlock.aspx.vb" Inherits="HistopathologySystem.SubmissionDetailsBlock" smartNavigation="True"%>
<%@ Register TagPrefix="uc1" TagName="CalendarDate" Src="CalendarDate.ascx" %>
<%@ Register TagPrefix="uc1" TagName="SenderRef" Src="SenderRef.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAHeader" Src="VLAHeader.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAFooter" Src="VLAFooter.ascx" %>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<HTML>
	<HEAD>
		<title>SubmissionDetailsBlock</title>
		<meta name="vs_snapToGrid" content="False">
		<META content="Microsoft Visual Studio.NET 7.0" name="GENERATOR">
		<META content="Visual Basic 7.0" name="CODE_LANGUAGE">
		<META content="JavaScript" name="vs_defaultClientScript">
		<META content="http://schemas.microsoft.com/intellisense/ie5" name="vs_targetSchema">
		<LINK href="Style/vla-ie.css" type="text/css" rel="stylesheet">
	</HEAD>
	<BODY>
		<script language="javascript">
		
		</script>
		<FORM id="Form1" method="post" runat="server">
			<uc1:vlaheader id="VLAHeader1" runat="server"></uc1:vlaheader>
			<DIV style="Z-INDEX: 120; WIDTH: 837px; POSITION: relative; HEIGHT: 107px" ms_positioning="GridLayout">
				<DIV style="Z-INDEX: 106; LEFT: 136px; WIDTH: 237px; POSITION: absolute; TOP: 8px; HEIGHT: 34px"><uc1:senderref id="SenderRef1" runat="server"></uc1:senderref></DIV>
				<DIV style="Z-INDEX: 109; LEFT: 397px; WIDTH: 269px; POSITION: absolute; TOP: 8px; HEIGHT: 41px"><uc1:calendardate id="ctlPMDate" runat="server"></uc1:calendardate></DIV>
				<DIV style="Z-INDEX: 108; LEFT: 136px; WIDTH: 235px; POSITION: absolute; TOP: 62px; HEIGHT: 32px"><uc1:histologyref id="HistologyRef1" runat="server"></uc1:histologyref></DIV>
				<asp:dropdownlist id="ddlHistologyType" style="Z-INDEX: 101; LEFT: 397px; POSITION: absolute; TOP: 62px"
					runat="server" Width="239px" AutoPostBack="True"></asp:dropdownlist><asp:label id="lblHistoRef" style="Z-INDEX: 100; LEFT: 14px; POSITION: absolute; TOP: 62px"
					runat="server">Histology Ref</asp:label><asp:label id="lblPick" style="Z-INDEX: 102; LEFT: 331px; POSITION: absolute; TOP: 62px" runat="server">Or Pick</asp:label>
				<HR style="Z-INDEX: 103; LEFT: 16px; POSITION: absolute; TOP: 90px; HEIGHT: 1px" width="97%"
					SIZE="1">
				<asp:label id="lblSenderRef" style="Z-INDEX: 104; LEFT: 14px; POSITION: absolute; TOP: 8px"
					runat="server">Sender Ref</asp:label><asp:label id="Label2" style="Z-INDEX: 107; LEFT: 14px; POSITION: absolute; TOP: 34px" runat="server">Enter the Histology ref if you know it.</asp:label><asp:label id="lblPMDate" style="Z-INDEX: 110; LEFT: 331px; POSITION: absolute; TOP: 8px" runat="server">PM Date</asp:label></DIV>
			<DIV style="WIDTH: 860px; POSITION: relative; HEIGHT: 116px" ms_positioning="GridLayout"><asp:label id="Label1" style="Z-INDEX: 101; LEFT: 10px; POSITION: absolute; TOP: 9px" runat="server">The following table displays the blocks for the selected sample within the submission. Do not double click a row, after clicking a row wait until it becomes highlighted with blue before selecting another row.</asp:label>
				<DIV id="ctlDivGrid" style="Z-INDEX: 102; LEFT: 10px; WIDTH: 682px; POSITION: absolute; TOP: 53px; HEIGHT: 7px"
					runat="server" ms_positioning="FlowLayout"><asp:linkbutton id="lbExpandAll" runat="server" CausesValidation="False">Click here to expand all blocks, or </asp:linkbutton>&nbsp;
					<asp:linkbutton id="lbCollapseAll" runat="server" CausesValidation="False">click here to collapse all blocks.</asp:linkbutton></DIV>
				<asp:label id="lblNumberBlocks" style="Z-INDEX: 103; LEFT: 10px; POSITION: absolute; TOP: 82px"
					runat="server" Width="320px"></asp:label>
				<asp:CheckBox id="cbSelectAll" style="Z-INDEX: 104; LEFT: 378px; POSITION: absolute; TOP: 82px"
					runat="server" AutoPostBack="True" Text="Select all blocks" TextAlign="Left"></asp:CheckBox></DIV>
			<DIV style="WIDTH: 820px"><asp:datagrid id="grdBlockSummary" runat="server" AutoGenerateColumns="False">
					<SelectedItemStyle CssClass="GridSelectedItemSmall"></SelectedItemStyle>
					<EditItemStyle CssClass="GridEditItemSmall"></EditItemStyle>
					<ItemStyle CssClass="GridItemSmall"></ItemStyle>
					<HeaderStyle CssClass="GridHeader"></HeaderStyle>
					<Columns>
						<asp:ButtonColumn Text="&lt;img src=&quot;Images/GridPager/sel.gif&quot;&gt;" CommandName="Select">
							<ItemStyle HorizontalAlign="Left" Width="20px"></ItemStyle>
						</asp:ButtonColumn>
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
								<asp:CheckBox ID="cbSelected" Runat="server" Enabled="true" OnCheckedChanged="Check_Clicked" AutoPostBack="True"></asp:CheckBox>
							</ItemTemplate>
						</asp:TemplateColumn>
					</Columns>
					<PagerStyle Visible="False"></PagerStyle>
				</asp:datagrid></DIV>
			<DIV style="WIDTH: 837px; POSITION: relative; HEIGHT: 57px" ms_positioning="GridLayout"><asp:button id="btnAddBlock" style="Z-INDEX: 101; LEFT: 5px; POSITION: absolute; TOP: 24px"
					runat="server" Width="96" CausesValidation="False" Text="Add Block" Height="24px"></asp:button><asp:button id="btnEditBlock" style="Z-INDEX: 102; LEFT: 106px; POSITION: absolute; TOP: 24px"
					runat="server" Width="96" CausesValidation="False" Text="Edit Block" Height="24px"></asp:button><asp:button id="btnDeleteBlock" style="Z-INDEX: 103; LEFT: 207px; POSITION: absolute; TOP: 24px"
					runat="server" Width="97" CausesValidation="False" Text="Delete Block" Height="24"></asp:button><asp:button id="btnCopyBlock" style="Z-INDEX: 107; LEFT: 309px; POSITION: absolute; TOP: 24px"
					runat="server" Width="129px" CausesValidation="False" Text="Copy To Samples" Height="24"></asp:button><asp:button id="btnBlockRefSearch" style="Z-INDEX: 108; LEFT: 446px; POSITION: absolute; TOP: 24px"
					runat="server" Width="124px" CausesValidation="False" Text="Block Ref Search" Height="24px"></asp:button><asp:button id="btSubmit" style="Z-INDEX: 104; LEFT: 690px; POSITION: absolute; TOP: 24px" runat="server"
					Width="96px" CausesValidation="False" Text="Done" Height="24px"></asp:button><asp:button id="btnCancel" style="Z-INDEX: 105; LEFT: 590px; POSITION: absolute; TOP: 24px"
					runat="server" Width="96px" CausesValidation="False" Text="Back" Height="24px"></asp:button>
				<HR style="Z-INDEX: 106; LEFT: 18px; POSITION: absolute; TOP: 8px; HEIGHT: 1px" width="97%"
					SIZE="1">
			</DIV>
			<DIV id="ctlDiv" style="WIDTH: 739px; HEIGHT: 17px" runat="server"></DIV>
			<uc1:vlafooter id="VLAFooter1" runat="server"></uc1:vlafooter></FORM>
	</BODY>
</HTML>
