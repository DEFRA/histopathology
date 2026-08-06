<%@ Page Language="vb" AutoEventWireup="false" Codebehind="PickListMaintenanceID.aspx.vb" Inherits="HistopathologySystem.PickListMaintenanceID"%>
<%@ Register TagPrefix="uc1" TagName="DataGridPager" Src="DataGridPager.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAHeader" Src="VLAHeader.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAFooter" Src="VLAFooter.ascx" %>
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
			<DIV style="WIDTH: 756px; POSITION: relative; HEIGHT: 61px" ms_positioning="GridLayout"><asp:checkbox id="cbActive" style="Z-INDEX: 100; LEFT: 366px; POSITION: absolute; TOP: 14px" runat="server" AutoPostBack="True" Checked="True" Text="Show deactivated items"></asp:checkbox>
				<HR style="Z-INDEX: 101; LEFT: 9px; WIDTH: 97.64%; POSITION: absolute; TOP: 52px; HEIGHT: 1px" width="97.64%" SIZE="1">
				<asp:dropdownlist id="ddlEditableLookups" style="Z-INDEX: 103; LEFT: 130px; POSITION: absolute; TOP: 14px" runat="server" AutoPostBack="True" Width="220px"></asp:dropdownlist>
				<asp:label id="lblSelectATable" style="Z-INDEX: 104; LEFT: 14px; POSITION: absolute; TOP: 14px" runat="server" Width="108px">Select a table</asp:label>
			</DIV>
			<DIV style="WIDTH: 461px"><asp:datagrid id="grdLookup" runat="server" AllowPaging="True" AllowSorting="True" AutoGenerateColumns="False">
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
											<asp:RequiredFieldValidator id="rfvDescription" runat="server" CssClass="ValidatorText" Height="8px" ToolTip="Required Field" ErrorMessage="*" ControlToValidate="txtDescriptionEdit" InitialValue=""></asp:RequiredFieldValidator>
										</td>
									</TR>
								</TABLE>
							</EditItemTemplate>
						</asp:TemplateColumn>
						<asp:TemplateColumn SortExpression="Area" HeaderText="Area">
							<ItemStyle HorizontalAlign="Left" Width="160px"></ItemStyle>
							<ItemTemplate>
								<asp:Label id="lblAreaDisplay" runat="server" Width="160px"></asp:Label>
							</ItemTemplate>
							<EditItemTemplate>
								<TABLE cellSpacing="0" cellPadding="0" border="0">
									<TR height="10">
										<td>
											<asp:DropDownList id="ddlAreaEdit" Width="150px" Runat="server"></asp:DropDownList>
										</td>
										<TD>
											<asp:RequiredFieldValidator id="rfvArea" runat="server" CssClass="ValidatorText" Height="8px" ToolTip="Required Field" ErrorMessage="*" ControlToValidate="ddlAreaEdit" InitialValue=""></asp:RequiredFieldValidator>
										</TD>
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
