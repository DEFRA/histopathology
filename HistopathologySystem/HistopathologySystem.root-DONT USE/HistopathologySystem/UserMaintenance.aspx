<%@ Page Language="vb" AutoEventWireup="false" Codebehind="UserMaintenance.aspx.vb" Inherits="HistopathologySystem.UserMaintenance"%>
<%@ Register TagPrefix="uc1" TagName="DataGridPager" Src="DataGridPager.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAHeader" Src="VLAHeader.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAFooter" Src="VLAFooter.ascx" %>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<HTML>
	<HEAD>
		<title>UserMaintenance</title>
		<meta content="Microsoft Visual Studio.NET 7.0" name="GENERATOR">
		<meta content="Visual Basic 7.0" name="CODE_LANGUAGE">
		<meta content="JavaScript" name="vs_defaultClientScript">
		<meta content="http://schemas.microsoft.com/intellisense/ie5" name="vs_targetSchema">
		<LINK href="Style/vla-ie.css" type="text/css" rel="stylesheet">
	</HEAD>
	<body>
		<form id="Form1" method="post" runat="server">
			<uc1:vlaheader id="VLAHeader1" runat="server"></uc1:vlaheader>
			<DIV style="WIDTH: 867px; POSITION: relative; HEIGHT: 65px" ms_positioning="GridLayout"><asp:label id="lblDescription" style="Z-INDEX: 101; LEFT: 14px; POSITION: absolute; TOP: 10px"
					runat="server">Use the controls under the table to add or edit users. </asp:label><asp:label id="lblDescription2" style="Z-INDEX: 102; LEFT: 401px; POSITION: absolute; TOP: 10px"
					runat="server">If you wish the user to access the system contact Histopath</asp:label>
				<HR style="Z-INDEX: 103; LEFT: 8px; POSITION: absolute; TOP: 58px" width="100%" SIZE="1">
				<asp:checkbox id="cbActive" style="Z-INDEX: 104; LEFT: 9px; POSITION: absolute; TOP: 36px" runat="server"
					Checked="True" Text="Show deactivated items" AutoPostBack="True"></asp:checkbox></DIV>
			<DIV style="WIDTH: 808px"><asp:datagrid id="grdUsers" runat="server" AutoGenerateColumns="False" AllowSorting="True" AllowPaging="True">
					<SelectedItemStyle CssClass="GridSelectedItemSmall"></SelectedItemStyle>
					<EditItemStyle CssClass="GridEditItemSmall"></EditItemStyle>
					<ItemStyle CssClass="GridItemSmall"></ItemStyle>
					<HeaderStyle CssClass="GridHeader"></HeaderStyle>
					<Columns>
						<asp:ButtonColumn Text="&lt;img src=&quot;Images/GridPager/sel.gif&quot;&gt;" CommandName="Select">
							<ItemStyle HorizontalAlign="Left" Width="20px"></ItemStyle>
						</asp:ButtonColumn>
						<asp:TemplateColumn SortExpression="NTLogin" HeaderText="NTLogin">
							<ItemStyle HorizontalAlign="Left" Width="120px"></ItemStyle>
							<ItemTemplate>
								<asp:Label id="lblNTLoginDisplay" runat="server" Width="120px"></asp:Label>
							</ItemTemplate>
							<EditItemTemplate>
								<TABLE cellSpacing="0" cellPadding="0" border="0">
									<TR height="10">
										<td>
											<asp:textbox id="txtNTLoginEdit" Width="100px" Runat="server" MaxLength="25"></asp:textbox>
										</td>
										<td>
											<asp:RequiredFieldValidator id="rfvNTLogin" runat="server" CssClass="ValidatorText" Height="8px" ToolTip="Required Field"
												ErrorMessage="*" ControlToValidate="txtNTLoginEdit" InitialValue=""></asp:RequiredFieldValidator>
										</td>
									</TR>
								</TABLE>
							</EditItemTemplate>
						</asp:TemplateColumn>
						<asp:TemplateColumn SortExpression="Name" HeaderText="Name">
							<ItemStyle HorizontalAlign="Left" Width="150px"></ItemStyle>
							<ItemTemplate>
								<asp:Label id="lblNameDisplay" runat="server" Width="150px"></asp:Label>
							</ItemTemplate>
							<EditItemTemplate>
								<TABLE cellSpacing="0" cellPadding="0" border="0">
									<TR height="10">
										<td>
											<asp:textbox id="txtNameEdit" Width="130px" Runat="server" MaxLength="35"></asp:textbox>
										</td>
										<td>
											<asp:RequiredFieldValidator id="rfvName" runat="server" CssClass="ValidatorText" Height="8px" ToolTip="Required Field"
												ErrorMessage="*" ControlToValidate="txtNameEdit" InitialValue=""></asp:RequiredFieldValidator>
										</td>
									</TR>
								</TABLE>
							</EditItemTemplate>
						</asp:TemplateColumn>
						<asp:BoundColumn DataField="Email" SortExpression="Email" HeaderText="Email">
							<ItemStyle HorizontalAlign="Left" Width="150px"></ItemStyle>
						</asp:BoundColumn>
						<asp:TemplateColumn SortExpression="UserGroup" HeaderText="Group">
							<ItemStyle HorizontalAlign="Left" Width="150px"></ItemStyle>
							<ItemTemplate>
								<asp:Label id="lblGroupDisplay" runat="server" Width="150px"></asp:Label>
							</ItemTemplate>
							<EditItemTemplate>
								<TABLE cellSpacing="0" cellPadding="0" border="0">
									<TR height="10">
										<td>
											<asp:DropDownList id="ddlGroupEdit" Width="130px" Runat="server"></asp:DropDownList>
										</td>
										<td>
											<asp:RequiredFieldValidator id="rfvGroup" runat="server" CssClass="ValidatorText" Height="8px" ToolTip="Required Field"
												ErrorMessage="*" ControlToValidate="ddlGroupEdit" InitialValue=""></asp:RequiredFieldValidator>
										</td>
									</TR>
								</TABLE>
							</EditItemTemplate>
						</asp:TemplateColumn>
						<asp:TemplateColumn SortExpression="UserArea" HeaderText="Area">
							<ItemStyle HorizontalAlign="Left" Width="140px"></ItemStyle>
							<ItemTemplate>
								<asp:Label id="lblAreaDisplay" runat="server" Width="120px"></asp:Label>
							</ItemTemplate>
							<EditItemTemplate>
								<TABLE cellSpacing="0" cellPadding="0" border="0">
									<TR height="10">
										<td>
											<asp:DropDownList id="ddlAreaEdit" Width="120px" Runat="server"></asp:DropDownList>
										</td>
										<TD>
											<asp:RequiredFieldValidator id="rfvArea" runat="server" CssClass="ValidatorText" Height="8px" ToolTip="Required Field"
												ErrorMessage="*" ControlToValidate="ddlAreaEdit" InitialValue=""></asp:RequiredFieldValidator>
										</TD>
									</TR>
								</TABLE>
							</EditItemTemplate>
						</asp:TemplateColumn>
						<asp:TemplateColumn SortExpression="Active" HeaderText="Active">
							<ItemStyle HorizontalAlign="Center" Width="50px"></ItemStyle>
							<ItemTemplate>
								<asp:CheckBox id="cbActiveDisplay" runat="server" Enabled="False"></asp:CheckBox>
							</ItemTemplate>
							<EditItemTemplate>
								<asp:CheckBox id="cbActiveEdit" runat="server"></asp:CheckBox>
							</EditItemTemplate>
						</asp:TemplateColumn>
					</Columns>
					<PagerStyle Visible="False"></PagerStyle>
				</asp:datagrid></DIV>
			<DIV style="WIDTH: 736px; POSITION: relative; HEIGHT: 32px" ms_positioning="GridLayout"><uc1:datagridpager id="Pager" runat="server"></uc1:datagridpager></DIV>
			<DIV style="WIDTH: 800px; POSITION: relative; HEIGHT: 72px" ms_positioning="GridLayout"><asp:button id="btnDone" style="Z-INDEX: 101; LEFT: 720px; POSITION: absolute; TOP: 8px" runat="server"
					Text="Done" Width="63px"></asp:button></DIV>
			<uc1:vlafooter id="VLAFooter1" runat="server"></uc1:vlafooter></form>
	</body>
</HTML>
