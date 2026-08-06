<%@ Page Language="vb" AutoEventWireup="false" Codebehind="CopyBatchBlocks.aspx.vb" Inherits="HistopathologySystem.CopyBatchBlocks"%>
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
			<DIV style="WIDTH: 760px; POSITION: relative; HEIGHT: 122px" ms_positioning="GridLayout"><asp:label id="Label1" style="Z-INDEX: 101; LEFT: 10px; POSITION: absolute; TOP: 9px" runat="server"
					Height="61px">The following table displays the samples for the current submission. Replace the required sender reference with a new sender reference. To do this select the required sender reference and click on the Change button. Any sender references that dont have a new sender reference will not be copied.</asp:label>
				<HR style="Z-INDEX: 102; LEFT: 8px; POSITION: absolute; TOP: 115px; HEIGHT: 1px" width="96.68%"
					SIZE="1">
				<asp:Label id="Label2" style="Z-INDEX: 103; LEFT: 10px; POSITION: absolute; TOP: 72px" runat="server"
					Width="701px">Do not double click a row, after clicking a row wait until it becomes highlighted with blue before selecting another row.</asp:Label>
			</DIV>
			<DIV style="WIDTH: 750px"><asp:datagrid id="grdBatchSummary" runat="server" AutoGenerateColumns="False">
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
						<asp:TemplateColumn HeaderText="New Sender Ref">
							<ItemStyle HorizontalAlign="Left" Width="250px"></ItemStyle>
							<ItemTemplate>
								<asp:Label id="lblNewSenderRefDisplay" runat="server" Width="250px"></asp:Label>
							</ItemTemplate>
						</asp:TemplateColumn>
					</Columns>
					<PagerStyle Visible="False"></PagerStyle>
				</asp:datagrid></DIV>
			<DIV style="WIDTH: 760px; POSITION: relative; HEIGHT: 57px" ms_positioning="GridLayout">
				<HR style="Z-INDEX: 100; LEFT: 8px; WIDTH: 96.68%; POSITION: absolute; TOP: 10px; HEIGHT: 1px"
					width="96.68%" SIZE="1">
				<asp:button id="btnSummary" style="Z-INDEX: 105; LEFT: 121px; POSITION: absolute; TOP: 17px"
					runat="server" Height="25px" Width="103px" Text="Summary"></asp:button>
				<asp:button id="btnCopySample" style="Z-INDEX: 104; LEFT: 7px; POSITION: absolute; TOP: 17px"
					runat="server" Height="25" Enabled="False" Width="103" Text="Change"></asp:button>
				<asp:button id="btnCopyBatch" style="Z-INDEX: 103; LEFT: 638px; POSITION: absolute; TOP: 17px"
					runat="server" Height="25px" Width="102px" Text="Finish "></asp:button><asp:button id="btnCancel" style="Z-INDEX: 102; LEFT: 526px; POSITION: absolute; TOP: 17px"
					runat="server" Height="25px" Width="102px" Text="Cancel" CausesValidation="False"></asp:button>
			</DIV>
			<DIV id="ctlDIV" style="WIDTH: 749px; HEIGHT: 11px" runat="server" ms_positioning="FlowLayout"></DIV>
			<uc1:vlafooter id="VLAFooter1" runat="server"></uc1:vlafooter></form>
	</body>
</HTML>
