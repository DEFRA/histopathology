<%@ Page Language="vb" AutoEventWireup="false" Codebehind="PickListMaintenance.aspx.vb" Inherits="HistopathologySystem.PickListMaintenance" %>
<%@ Register TagPrefix="uc1" TagName="DataGridPager" Src="DataGridPager.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAHeader" Src="VLAHeader.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAFooter" Src="VLAFooter.ascx" %>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<HTML>
	<HEAD>
		<title>PickListMaintenance</title>
		<meta content="Microsoft Visual Studio.NET 7.0" name="GENERATOR">
		<meta content="Visual Basic 7.0" name="CODE_LANGUAGE">
		<meta content="JavaScript" name="vs_defaultClientScript">
		<meta content="http://schemas.microsoft.com/intellisense/ie5" name="vs_targetSchema">
		<LINK href="Style/vla-ie.css" type="text/css" rel="stylesheet">
	</HEAD>
	<body>
		<form id="Form1" method="post" runat="server">
			<uc1:vlaheader id="VLAHeader1" runat="server"></uc1:vlaheader>
			<DIV style="WIDTH: 750px; POSITION: relative; HEIGHT: 54px" ms_positioning="GridLayout"><asp:dropdownlist id="ddlEditableLookups" style="Z-INDEX: 101; LEFT: 139px; POSITION: absolute; TOP: 16px"
					runat="server" Width="212px" AutoPostBack="True"></asp:dropdownlist><asp:label id="lblSelectATable" style="Z-INDEX: 102; LEFT: 16px; POSITION: absolute; TOP: 16px"
					runat="server" Width="108px">Select a table</asp:label><asp:checkbox id="cbActive" style="Z-INDEX: 108; LEFT: 373px; POSITION: absolute; TOP: 18px" runat="server"
					AutoPostBack="True" Text="Show deactivated items" Checked="True"></asp:checkbox></DIV>
			<DIV style="WIDTH: 616px"><asp:datagrid id="grdLookup" runat="server" AllowPaging="True" AllowSorting="True" AutoGenerateColumns="False">
					<SelectedItemStyle CssClass="GridSelectedItem"></SelectedItemStyle>
					<EditItemStyle CssClass="GridEditItem"></EditItemStyle>
					<ItemStyle CssClass="GridItem"></ItemStyle>
					<HeaderStyle CssClass="GridHeader"></HeaderStyle>
					<Columns>
						<asp:ButtonColumn Text="&lt;img src=&quot;Images/GridPager/sel.gif&quot;&gt;" CommandName="Select">
							<HeaderStyle Width="25px"></HeaderStyle>
						</asp:ButtonColumn>
						<asp:TemplateColumn SortExpression="Code" HeaderText="Code">
							<ItemTemplate>
								<asp:Label id="lblCodeDisplay" runat="server" Width="220"></asp:Label>
							</ItemTemplate>
							<EditItemTemplate>
								<TABLE cellSpacing="0" cellPadding="0" border="0">
									<TR height="10">
										<td>
											<asp:TextBox id="txtCodeEdit" Width="200px" Runat="server" MaxLength="15"></asp:TextBox>
										</td>
										<td>
											<asp:RequiredFieldValidator id="rvfCode" runat="server" CssClass="ValidatorText" Height="8px" ToolTip="Required Field"
												ErrorMessage="*" ControlToValidate="txtCodeEdit" InitialValue=""></asp:RequiredFieldValidator>
										</td>
									</TR>
								</TABLE>
							</EditItemTemplate>
						</asp:TemplateColumn>
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
					</Columns>
					<PagerStyle Visible="False"></PagerStyle>
				</asp:datagrid><uc1:datagridpager id="Pager" runat="server"></uc1:datagridpager></DIV>
			<uc1:vlafooter id="VLAFooter1" runat="server"></uc1:vlafooter></form>
	</body>
</HTML>
