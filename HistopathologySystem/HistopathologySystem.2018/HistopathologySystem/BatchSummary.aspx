<%@ Register TagPrefix="uc1" TagName="VLAFooter" Src="VLAFooter.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAHeader" Src="VLAHeader.ascx" %>
<%@ Page Language="vb" AutoEventWireup="false" Codebehind="BatchSummary.aspx.vb" Inherits="HistopathologySystem.BatchSummary" smartNavigation="True"%>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<HTML>
	<HEAD>
		<title>BatchSummary</title>
		<meta content="Microsoft Visual Studio.NET 7.0" name="GENERATOR">
		<meta content="Visual Basic 7.0" name="CODE_LANGUAGE">
		<meta content="JavaScript" name="vs_defaultClientScript">
		<meta content="http://schemas.microsoft.com/intellisense/ie5" name="vs_targetSchema">
		<LINK href="Style/vla-ie.css" type="text/css" rel="stylesheet">
	</HEAD>
	<body>
		<form id="Form1" method="post" runat="server">
			<uc1:vlaheader id="VLAHeader1" runat="server"></uc1:vlaheader>
			<DIV style="WIDTH: 750px; POSITION: relative; HEIGHT: 130px" ms_positioning="GridLayout"><asp:label id="Label1" runat="server" style="Z-INDEX: 101; LEFT: 10px; POSITION: absolute; TOP: 9px">The following table displays all samples and tissues for the current submission. Do not double click a row, after clicking a row wait until it becomes highlighted with blue before selecting another row.</asp:label>
				<DIV style="Z-INDEX: 102; LEFT: 10px; WIDTH: 682px; POSITION: absolute; TOP: 51px; HEIGHT: 7px"
					ms_positioning="FlowLayout" runat="server" id="ctlDivGrid">
					<asp:LinkButton id="lbExpandAll" runat="server" style="LEFT: 0px; TOP: 1px">Click here to expand all tissues, or</asp:LinkButton>&nbsp;
					<asp:LinkButton id="lbCollapseAll" runat="server">click here to collapse all tissues.</asp:LinkButton></DIV>
				<asp:Label id="lblNumberSamples" style="Z-INDEX: 103; LEFT: 10px; POSITION: absolute; TOP: 76px"
					runat="server" Width="692px"></asp:Label>
                <span style="Z-INDEX: 103; LEFT: 10px; POSITION: absolute; TOP: 100px">Bypass Sort</span><asp:CheckBox ID="chkByPassSort" style="Z-INDEX: 103; LEFT: 80px; POSITION: absolute; TOP: 100px"
					runat="server" Width="692px" AutoPostBack="true" OnCheckedChanged="chkByPassSort_CheckedChanged" />
			</DIV>
			<DIV style="WIDTH: 479px"><asp:datagrid id="grdBatchSummary" runat="server" AutoGenerateColumns="False">
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
						<asp:BoundColumn DataField="HistologyRef" SortExpression="HistologyRef" HeaderText="Histology Ref">
							<ItemStyle HorizontalAlign="Left" Width="120px"></ItemStyle>
						</asp:BoundColumn>
						<asp:ButtonColumn Text="&lt;img src=&quot;Images/plus.gif&quot;&gt;" CommandName="ExpandTissues">
							<ItemStyle HorizontalAlign="Left" Width="20px"></ItemStyle>
						</asp:ButtonColumn>
						<asp:BoundColumn DataField="TissueDetails" HeaderText="Tissue Details">
							<ItemStyle HorizontalAlign="Left" Width="220px"></ItemStyle>
						</asp:BoundColumn>
					</Columns>
					<PagerStyle Visible="False"></PagerStyle>
				</asp:datagrid></DIV>
			<DIV style="WIDTH: 752px; POSITION: relative; HEIGHT: 56px" ms_positioning="GridLayout">
				<HR style="Z-INDEX: 103; LEFT: 5px; POSITION: absolute; TOP: 8px; HEIGHT: 1px" width="98%"
					SIZE="1">
				<asp:Button id="btnAddSubmission" style="Z-INDEX: 106; LEFT: 6px; POSITION: absolute; TOP: 15px"
					runat="server" Text="Add Sample" Width="122px" Height="24px"></asp:Button>
				<asp:button id="btnEditSubmission" style="Z-INDEX: 101; LEFT: 135px; POSITION: absolute; TOP: 15px"
					runat="server" Height="24" CausesValidation="False" Width="114" Text="Edit Sample" Enabled="False"></asp:button>
				<asp:button id="btnDeleteSubmission" style="Z-INDEX: 102; LEFT: 256px; POSITION: absolute; TOP: 15px"
					runat="server" Height="24" CausesValidation="False" Width="122" Text="Delete Sample" Enabled="False"></asp:button>
				<asp:Button id="btnCopySubmission" style="Z-INDEX: 105; LEFT: 387px; POSITION: absolute; TOP: 15px"
					runat="server" Text="Copy Sample" Width="114px" Height="24px" Enabled="False"></asp:Button>
				<asp:Button id="btnCancel" style="Z-INDEX: 104; LEFT: 616px; POSITION: absolute; TOP: 15px"
					runat="server" Height="24px" Width="113" Text="Done"></asp:Button>
			</DIV>
			<uc1:vlafooter id="VLAFooter1" runat="server"></uc1:vlafooter>
		</form>
	</body>
</HTML>
