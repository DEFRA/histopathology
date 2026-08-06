<%@ Register TagPrefix="uc1" TagName="VLAFooter" Src="VLAFooter.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAHeader" Src="VLAHeader.ascx" %>
<%@ Register TagPrefix="uc1" TagName="DataGridPager" Src="DataGridPager.ascx" %>
<%@ Page Language="vb" AutoEventWireup="false" Codebehind="PickListUserArea.aspx.vb" Inherits="HistopathologySystem.PickListUserArea"%>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<HTML>
	<HEAD>
		<title>PickListUserArea</title>
		<meta content="Microsoft Visual Studio.NET 7.0" name="GENERATOR">
		<meta content="Visual Basic 7.0" name="CODE_LANGUAGE">
		<meta content="JavaScript" name="vs_defaultClientScript">
		<meta content="http://schemas.microsoft.com/intellisense/ie5" name="vs_targetSchema">
		<LINK href="Style/vla-ie.css" type="text/css" rel="stylesheet">
	</HEAD>
	<body>
		<form id="Form1" method="post" runat="server">
			<uc1:vlaheader id="VLAHeader1" runat="server"></uc1:vlaheader>
			<DIV style="WIDTH: 756px; POSITION: relative; HEIGHT: 87px" ms_positioning="GridLayout"><asp:checkbox id="cbActive" style="Z-INDEX: 101; LEFT: 9px; POSITION: absolute; TOP: 49px" runat="server"
					AutoPostBack="True" Checked="True" Text="Show deactivated items"></asp:checkbox><asp:label id="lblDescription" style="Z-INDEX: 102; LEFT: 9px; POSITION: absolute; TOP: 7px"
					runat="server" Height="30px" Width="727px">Use the controls under the table to add, edit or delete entries in the table. The code and description fields must be completed for the entry to be successfully saved.</asp:label>
				<HR style="Z-INDEX: 103; LEFT: 9px; WIDTH: 97.64%; POSITION: absolute; TOP: 72px; HEIGHT: 1px"
					width="97.64%" SIZE="1">
			</DIV>
			<DIV style="WIDTH: 392px"><asp:datagrid id="grdLookup" runat="server" AllowPaging="True" AllowSorting="True" AutoGenerateColumns="False">
					<SelectedItemStyle CssClass="GridSelectedItem"></SelectedItemStyle>
					<EditItemStyle CssClass="GridEditItem"></EditItemStyle>
					<ItemStyle CssClass="GridItem"></ItemStyle>
					<HeaderStyle CssClass="GridHeader"></HeaderStyle>
					<Columns>
						<asp:ButtonColumn Text="&lt;img src=&quot;Images/GridPager/sel.gif&quot;&gt;" CommandName="Select">
							<HeaderStyle Width="25px"></HeaderStyle>
						</asp:ButtonColumn>
						<asp:TemplateColumn SortExpression="Description" HeaderText="Description">
							<ItemStyle HorizontalAlign="Left" Width="300px"></ItemStyle>
							<ItemTemplate>
								<asp:Label id="lblDescriptionDisplay" runat="server" Width="280px"></asp:Label>
							</ItemTemplate>
							<EditItemTemplate>
								<TABLE cellSpacing="0" cellPadding="0" border="0">
									<TR height="10">
										<td>
											<asp:TextBox id="txtDescriptionEdit" Width="280px" Runat="server" MaxLength="50"></asp:TextBox>
										</td>
										<td>
											<asp:RequiredFieldValidator id="rfvDescription" runat="server" CssClass="ValidatorText" Height="8px" ToolTip="Required Field"
												ErrorMessage="*" ControlToValidate="txtDescriptionEdit" InitialValue=""></asp:RequiredFieldValidator>
										</td>
									</TR>
								</TABLE>
							</EditItemTemplate>
						</asp:TemplateColumn>
						<asp:TemplateColumn SortExpression="IsActive" HeaderText="Active">
							<ItemTemplate>
								<asp:CheckBox ID="cbActiveDisplay" Runat="server" Enabled="False"></asp:CheckBox>
							</ItemTemplate>
							<EditItemTemplate>
								<asp:CheckBox ID="cbActiveEdit" Runat="server" Enabled="true"></asp:CheckBox>
							</EditItemTemplate>
						</asp:TemplateColumn>
						<asp:BoundColumn Visible="False" DataField="Area" HeaderText="Area"></asp:BoundColumn>
					</Columns>
					<PagerStyle Visible="False"></PagerStyle>
				</asp:datagrid><uc1:datagridpager id="Pager" runat="server"></uc1:datagridpager></DIV>
			<DIV style="WIDTH: 592px; POSITION: relative; HEIGHT: 48px" ms_positioning="GridLayout"><asp:button id="btnDone" style="Z-INDEX: 101; LEFT: 490px; POSITION: absolute; TOP: 16px" runat="server"
					Text="Done" Width="70px"></asp:button>
				<HR style="Z-INDEX: 102; LEFT: 7px; WIDTH: 96.41%; POSITION: absolute; TOP: 5px; HEIGHT: 1px"
					width="96.41%" SIZE="1">
			</DIV>
			<uc1:vlafooter id="VLAFooter1" runat="server"></uc1:vlafooter></form>
	</body>
</HTML>
