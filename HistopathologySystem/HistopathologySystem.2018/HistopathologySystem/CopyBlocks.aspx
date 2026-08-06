<%@ Register TagPrefix="uc1" TagName="VLAFooter" Src="VLAFooter.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAHeader" Src="VLAHeader.ascx" %>
<%@ Page Language="vb" AutoEventWireup="false" Codebehind="CopyBlocks.aspx.vb" Inherits="HistopathologySystem.CopyBlocks" smartNavigation="True"%>
<%@ Register TagPrefix="uc1" TagName="SenderRef" Src="SenderRef.ascx" %>
<%@ Register TagPrefix="uc1" TagName="HistologyRef" Src="HistologyRef.ascx" %>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<HTML>
	<HEAD>
		<title>CopyBlocks</title>
		<meta content="Microsoft Visual Studio.NET 7.0" name="GENERATOR">
		<meta content="Visual Basic 7.0" name="CODE_LANGUAGE">
		<meta content="JavaScript" name="vs_defaultClientScript">
		<meta content="http://schemas.microsoft.com/intellisense/ie5" name="vs_targetSchema">
		<LINK href="Style/vla-ie.css" type="text/css" rel="stylesheet">
	</HEAD>
	<body>
		<form id="Form1" method="post" runat="server">
			<uc1:vlaheader id="VLAHeader1" runat="server"></uc1:vlaheader>
			<DIV style="WIDTH: 760px; POSITION: relative; HEIGHT: 134px" ms_positioning="GridLayout">
				<HR style="Z-INDEX: 101; LEFT: 10px; WIDTH: 97.23%; POSITION: absolute; TOP: 126px; HEIGHT: 1px"
					width="97.23%" SIZE="1">
				<asp:label id="lblDescription" style="Z-INDEX: 102; LEFT: 13px; POSITION: absolute; TOP: 11px"
					runat="server" Width="664px">Select the samples, which the block is to be copied to and then click the Finish button. Note the tissue list must be the same for each sample. </asp:label><asp:checkbox id="cbSelectAll" style="Z-INDEX: 103; LEFT: 13px; POSITION: absolute; TOP: 98px"
					runat="server" AutoPostBack="True" Text="Select All"></asp:checkbox><asp:checkbox id="cbAutoGenerateHisto" style="Z-INDEX: 104; LEFT: 136px; POSITION: absolute; TOP: 98px"
					runat="server" AutoPostBack="True" Text="Auto Generate Histology Refs"></asp:checkbox><asp:label id="lblSenderRef" style="Z-INDEX: 105; LEFT: 13px; POSITION: absolute; TOP: 61px"
					runat="server">Copy block from Sender Ref</asp:label><asp:label id="lblHistologyRef" style="Z-INDEX: 106; LEFT: 417px; POSITION: absolute; TOP: 61px"
					runat="server">Histology Ref</asp:label>
				<DIV style="Z-INDEX: 107; LEFT: 229px; WIDTH: 254px; POSITION: absolute; TOP: 61px; HEIGHT: 36px"><uc1:senderref id="SenderRef1" runat="server"></uc1:senderref></DIV>
				<DIV style="Z-INDEX: 108; LEFT: 520px; WIDTH: 238px; POSITION: absolute; TOP: 61px; HEIGHT: 33px"><uc1:histologyref id="HistologyRef1" runat="server"></uc1:histologyref></DIV>
			</DIV>
			<DIV style="WIDTH: 752px"><asp:datagrid id="grdAnimal" runat="server" AutoGenerateColumns="False">
					<SelectedItemStyle CssClass="GridSelectedItem"></SelectedItemStyle>
					<EditItemStyle CssClass="GridEditItem"></EditItemStyle>
					<ItemStyle CssClass="GridItem"></ItemStyle>
					<HeaderStyle CssClass="GridHeader"></HeaderStyle>
					<Columns>
						<asp:BoundColumn DataField="SenderRef" SortExpression="SenderRef" HeaderText="Sender Ref">
							<ItemStyle Width="150px" HorizontalAlign="Left"></ItemStyle>
						</asp:BoundColumn>
						<asp:TemplateColumn HeaderText="Copy?">
							<ItemStyle Width="30px" HorizontalAlign="Center"></ItemStyle>
							<ItemTemplate>
								<asp:CheckBox ID="cbCopy" Runat="server" Enabled="True"></asp:CheckBox>
							</ItemTemplate>
						</asp:TemplateColumn>
					</Columns>
				</asp:datagrid></DIV>
			<DIV style="WIDTH: 746px; HEIGHT: 3px" runat="server" id="ctlDiv"></DIV>
			<DIV style="WIDTH: 750px; POSITION: relative; HEIGHT: 49px" ms_positioning="GridLayout"><asp:button id="btnFinish" style="Z-INDEX: 101; LEFT: 640px; POSITION: absolute; TOP: 16px"
					runat="server" Text="Finish" Height="24px" Width="97px" CausesValidation="False"></asp:button><asp:button id="btnCancel" style="Z-INDEX: 102; LEFT: 536px; POSITION: absolute; TOP: 16px"
					runat="server" Text="Cancel" Height="24" Width="97" CausesValidation="False"></asp:button>
				<HR style="Z-INDEX: 103; LEFT: 8px; WIDTH: 97.71%; POSITION: absolute; TOP: 8px; HEIGHT: 1px"
					width="97.71%" SIZE="1">
			</DIV>
			<P><uc1:vlafooter id="VLAFooter1" runat="server"></uc1:vlafooter></P>
		</form>
	</body>
</HTML>
