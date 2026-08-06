<%@ Register TagPrefix="uc1" TagName="VLAHeader" Src="VLAHeader.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAFooter" Src="VLAFooter.ascx" %>
<%@ Register TagPrefix="uc1" TagName="DataGridPager" Src="DataGridPager.ascx" %>
<%@ Page Language="vb" AutoEventWireup="false" Codebehind="BatchBlockSummary.aspx.vb" Inherits="HistopathologySystem.BatchBlockSummary" smartNavigation="False"%>
<%@ Register TagPrefix="uc1" TagName="HistologyRef" Src="HistologyRef.ascx" %>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<HTML>
	<HEAD>
		<title>BatchBlockSummary</title>
		<meta content="Microsoft Visual Studio.NET 7.0" name="GENERATOR">
		<meta content="Visual Basic 7.0" name="CODE_LANGUAGE">
		<meta content="JavaScript" name="vs_defaultClientScript">
		<meta content="http://schemas.microsoft.com/intellisense/ie5" name="vs_targetSchema">
		<LINK href="Style/vla-ie.css" type="text/css" rel="stylesheet">
	</HEAD>
	<body>
		<form id="Form1" method="post" runat="server">
			<uc1:vlaheader id="VLAHeader1" runat="server"></uc1:vlaheader>
			<DIV style="WIDTH: 734px; POSITION: relative; HEIGHT: 100px" ms_positioning="GridLayout"><asp:label id="Label1" style="Z-INDEX: 100; LEFT: 10px; POSITION: absolute; TOP: 9px" runat="server"> The following table displays all sampless for the current submission. Do not double click a row, after clicking a row wait until it becomes highlighted with blue before selecting another row.</asp:label><asp:label id="lblNumberSamples" style="Z-INDEX: 102; LEFT: 10px; POSITION: absolute; TOP: 50px"
					runat="server" Width="692px"></asp:label>
    		<span style="Z-INDEX: 103; LEFT: 10px; POSITION: absolute; TOP: 80px">Bypass Sort</span><asp:CheckBox ID="chkByPassSort" style="Z-INDEX: 103; LEFT: 80px; POSITION: absolute; TOP: 80px"
				runat="server" Width="692px" AutoPostBack="true"  OnCheckedChanged="chkByPassSort_CheckedChanged"/>
					</DIV>
             
			<DIV style="WIDTH: 322px"><asp:datagrid id="grdBatchSummary" runat="server" AutoGenerateColumns="False" AllowPaging="True"
					AllowSorting="True">
					<SelectedItemStyle CssClass="GridSelectedItemSmall"></SelectedItemStyle>
					<EditItemStyle CssClass="GridEditItemSmall"></EditItemStyle>
					<ItemStyle CssClass="GridItemSmall"></ItemStyle>
					<HeaderStyle CssClass="GridHeader"></HeaderStyle>
					<Columns>
						<asp:ButtonColumn Text="&lt;img src=&quot;Images/GridPager/sel.gif&quot;&gt;" CommandName="Select">
							<ItemStyle HorizontalAlign="Left" Width="20px"></ItemStyle>
						</asp:ButtonColumn>
						<asp:BoundColumn Visible="False" DataField="SenderRef" SortExpression="SenderRef" ReadOnly="True"
							HeaderText="Sender Ref"></asp:BoundColumn>
						<asp:TemplateColumn SortExpression="SenderRef" HeaderText="Sender Ref">
							<ItemStyle HorizontalAlign="Left" Width="120px"></ItemStyle>
							<ItemTemplate>
								<asp:Label id="lblSenderRefDisplay" runat="server" Width="120px"></asp:Label>
							</ItemTemplate>
							<EditItemTemplate>
								<asp:Label id="lblSenderRefEdit" Runat="server"></asp:Label>
							</EditItemTemplate>
						</asp:TemplateColumn>
						<asp:TemplateColumn SortExpression="HistologyRef" HeaderText="Histology Ref">
							<ItemStyle HorizontalAlign="Left" Width="160px"></ItemStyle>
							<ItemTemplate>
								<asp:Label id="lblHistologyRefDisplay" runat="server" Width="120px"></asp:Label>
							</ItemTemplate>
							<EditItemTemplate>
								<TABLE cellSpacing="0" cellPadding="0" border="0">
									<TR height="10">
										<td>
											<asp:TextBox id="txtHistologyRefEdit" MaxLength="20" Runat="server"></asp:TextBox>
										</td>
										<td>
											<asp:customvalidator id="valHistologyRef" runat="server" ToolTip="Format: NN/NNNNN. Year part must not be greater than current year."
												ControlToValidate="txtHistologyRefEdit" CssClass="ValidatorText" ClientValidationFunction="ClientValidateHistologyRef"
												OnServerValidate="ValidateHistologyRef">*</asp:customvalidator>
										</td>
										<td>
											<asp:RequiredFieldValidator ID="valrequiredHistologyRef" Runat="server" Tooltip="Required Field" CssClass="ValidatorText"
												ControlToValidate="txtHistologyRefEdit">*</asp:RequiredFieldValidator>
										</td>
									</TR>
								</TABLE>
							</EditItemTemplate>
						</asp:TemplateColumn>
					</Columns>
					<PagerStyle Visible="False"></PagerStyle>
				</asp:datagrid><uc1:datagridpager id="SummaryGridPager" runat="server"></uc1:datagridpager></DIV>
			<DIV style="WIDTH: 743px; POSITION: relative; HEIGHT: 54px" ms_positioning="GridLayout"><asp:button id="btnAddSubmission" style="Z-INDEX: 106; LEFT: 8px; POSITION: absolute; TOP: 17px"
					runat="server" Width="122px" Height="24px" Text="Add Sample"></asp:button><asp:button id="btnEditSubmission" style="Z-INDEX: 101; LEFT: 137px; POSITION: absolute; TOP: 17px"
					runat="server" Width="114" Height="24" Text="Edit Sample" CausesValidation="False" Enabled="False"></asp:button><asp:button id="btnDeleteSubmission" style="Z-INDEX: 102; LEFT: 258px; POSITION: absolute; TOP: 17px"
					runat="server" Width="122" Height="24" Text="Delete Sample" CausesValidation="False" Enabled="False"></asp:button><asp:button id="btnCopySubmission" style="Z-INDEX: 105; LEFT: 387px; POSITION: absolute; TOP: 17px"
					runat="server" Width="114px" Height="24px" Text="Copy Sample" Enabled="False"></asp:button><asp:button id="btnCancel" style="Z-INDEX: 104; LEFT: 616px; POSITION: absolute; TOP: 17px"
					runat="server" Width="113" Height="24px" Text="Done"></asp:button>
				<HR style="Z-INDEX: 103; LEFT: 11px; WIDTH: 97%; POSITION: absolute; TOP: 9px; HEIGHT: 1px"
					width="97%" SIZE="1">
			</DIV>
			<DIV id="ctlDiv" style="WIDTH: 802px; HEIGHT: 1px" runat="server"></DIV>
			<uc1:vlafooter id="VLAFooter1" runat="server"></uc1:vlafooter></form>
	</body>
</HTML>
