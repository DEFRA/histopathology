<%@ Page Language="vb" AutoEventWireup="false" Codebehind="CopyBatch.aspx.vb" Inherits="HistopathologySystem.CopyBatch1" smartNavigation="True"%>
<%@ Register TagPrefix="uc1" TagName="VLAHeader" Src="VLAHeader.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAFooter" Src="VLAFooter.ascx" %>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<HTML>
	<HEAD>
		<title>CopyBatch</title>
		<meta content="Microsoft Visual Studio.NET 7.0" name="GENERATOR">
		<meta content="Visual Basic 7.0" name="CODE_LANGUAGE">
		<meta content="JavaScript" name="vs_defaultClientScript">
		<meta content="http://schemas.microsoft.com/intellisense/ie5" name="vs_targetSchema">
		<LINK href="Style/vla-ie.css" type="text/css" rel="stylesheet">
	</HEAD>
	<body>
		<form id="Form1" method="post" runat="server">
			<uc1:vlaheader id="VLAHeader1" runat="server"></uc1:vlaheader>
			<DIV style="WIDTH: 750px; POSITION: relative; HEIGHT: 135px" ms_positioning="GridLayout">
				<asp:label id="Label1" style="Z-INDEX: 101; LEFT: 10px; POSITION: absolute; TOP: 9px" runat="server" Height="23px">The following table displays the samples and tissues for the current submission. Replace the required sample reference with a new sample reference. To do this select the required sender ref and click on the Change button.</asp:label>
				<DIV id="ctlDivGrid" style="Z-INDEX: 102; LEFT: 10px; WIDTH: 682px; POSITION: absolute; TOP: 109px; HEIGHT: 7px" runat="server" ms_positioning="FlowLayout">
					<asp:LinkButton id="lbExpandAll" runat="server">Click here to expand all tissues, or</asp:LinkButton>&nbsp;
					<asp:LinkButton id="lbCollapseAll" runat="server">click here to collapse all tissues.</asp:LinkButton></DIV>
				<asp:Label id="Label2" style="Z-INDEX: 103; LEFT: 10px; POSITION: absolute; TOP: 68px" runat="server" Width="701px">Do not double click a row, after clicking a row wait until it becomes highlighted with blue before selecting another row.</asp:Label>
			</DIV>
			<DIV style="WIDTH: 750px">
				<asp:DataGrid id="grdBatchSummary" runat="server" AutoGenerateColumns="False">
					<SelectedItemStyle CssClass="GridSelectedItem"></SelectedItemStyle>
					<EditItemStyle CssClass="GridEditItem"></EditItemStyle>
					<ItemStyle CssClass="GridItem"></ItemStyle>
					<HeaderStyle CssClass="GridHeader"></HeaderStyle>
					<Columns>
						<asp:ButtonColumn Text="&lt;img src=&quot;Images/GridPager/sel.gif&quot;&gt;" CommandName="Select">
							<ItemStyle HorizontalAlign="Left" Width="20px"></ItemStyle>
						</asp:ButtonColumn>
						<asp:BoundColumn DataField="SenderRef" SortExpression="SenderRef" HeaderText="Sender Ref">
							<ItemStyle HorizontalAlign="Left" Width="120px"></ItemStyle>
						</asp:BoundColumn>
						<asp:ButtonColumn Text="&lt;img src=&quot;Images/plus.gif&quot;&gt;" CommandName="ExpandTissues">
							<ItemStyle HorizontalAlign="Left" Width="20px"></ItemStyle>
						</asp:ButtonColumn>
						<asp:BoundColumn DataField="TissueDetails" HeaderText="Tissue Details">
							<ItemStyle HorizontalAlign="Left" Width="220px"></ItemStyle>
						</asp:BoundColumn>
						<asp:TemplateColumn HeaderText="New Sender Ref">
							<ItemStyle HorizontalAlign="Left" Width="250px"></ItemStyle>
							<ItemTemplate>
								<asp:Label id="lblNewSenderRefDisplay" runat="server" Width="250px"></asp:Label>
							</ItemTemplate>
						</asp:TemplateColumn>
					</Columns>
				</asp:DataGrid></DIV>
			<DIV style="WIDTH: 751px; POSITION: relative; HEIGHT: 53px" ms_positioning="GridLayout">
				<HR style="Z-INDEX: 101; LEFT: 8px; WIDTH: 96.68%; POSITION: absolute; TOP: 10px; HEIGHT: 1px" width="96.68%" SIZE="1">
				<asp:Button id="btnCopySample" style="Z-INDEX: 106; LEFT: 11px; POSITION: absolute; TOP: 18px" runat="server" Text="Change" Width="102" Enabled="False" Height="25"></asp:Button>
				<asp:Button id="btnCopyBatch" style="Z-INDEX: 104; LEFT: 637px; POSITION: absolute; TOP: 18px" runat="server" Text=" Finish" Width="102px" Height="25px"></asp:Button>
				<asp:Button id="btnCancel" style="Z-INDEX: 103; LEFT: 524px; POSITION: absolute; TOP: 18px" runat="server" Text="Cancel" Width="102px" CausesValidation="False" Height="25px"></asp:Button>
			</DIV>
			<DIV id="ctlDIV" style="WIDTH: 749px; HEIGHT: 11px" runat="server" ms_positioning="FlowLayout"></DIV>
			<uc1:VLAFooter id="VLAFooter1" runat="server"></uc1:VLAFooter>
		</form>
	</body>
</HTML>
