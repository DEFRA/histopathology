<%@ Register TagPrefix="uc1" TagName="DataGridPager" Src="DataGridPager.ascx" %>
<%@ Page Language="vb" AutoEventWireup="false" Codebehind="BlockDetails.aspx.vb" Inherits="HistopathologySystem.BlockDetails" smartNavigation="True"%>
<%@ Register TagPrefix="uc1" TagName="VLAHeader" Src="VLAHeader.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAFooter" Src="VLAFooter.ascx" %>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<HTML>
	<HEAD>
		<title>BlockDetails</title>
		<meta content="Microsoft Visual Studio.NET 7.0" name="GENERATOR">
		<meta content="Visual Basic 7.0" name="CODE_LANGUAGE">
		<meta content="JavaScript" name="vs_defaultClientScript">
		<meta content="http://schemas.microsoft.com/intellisense/ie5" name="vs_targetSchema">
		<LINK href="Style/vla-ie.css" type="text/css" rel="stylesheet">
	</HEAD>
	<body>
		<form id="Form1" method="post" runat="server">
			<uc1:vlaheader id="VLAHeader1" runat="server"></uc1:vlaheader><asp:image id="imgDummy" runat="server" ImageUrl="images/spacer.gif"></asp:image>
			<DIV style="WIDTH: 745px; POSITION: relative; HEIGHT: 149px" ms_positioning="GridLayout"><asp:textbox id="txtSenderRef" style="Z-INDEX: 113; LEFT: 168px; POSITION: absolute; TOP: 16px"
					runat="server" Enabled="False" Width="153"></asp:textbox><asp:textbox id="txtBlockReference" style="Z-INDEX: 102; LEFT: 168px; POSITION: absolute; TOP: 48px"
					runat="server" Width="56px" MaxLength="3"></asp:textbox><asp:textbox id="txtNoBlocks" style="Z-INDEX: 106; LEFT: 168px; POSITION: absolute; TOP: 80px"
					runat="server" Width="56px" MaxLength="3" ToolTip="Number of blocks containing the same tissues and tests."></asp:textbox><asp:textbox id="txtHistologyRef" style="Z-INDEX: 115; LEFT: 512px; POSITION: absolute; TOP: 16px"
					runat="server" Enabled="False" Width="153px"></asp:textbox><asp:textbox id="txtCustomerRef" style="Z-INDEX: 117; LEFT: 512px; POSITION: absolute; TOP: 48px"
					runat="server" Width="153px" MaxLength="20"></asp:textbox><asp:checkbox id="chkRepeatBlock" style="Z-INDEX: 109; LEFT: 512px; POSITION: absolute; TOP: 80px"
					runat="server"></asp:checkbox><asp:label id="lblBlockReference" style="Z-INDEX: 101; LEFT: 16px; POSITION: absolute; TOP: 48px"
					runat="server"> Block Ref</asp:label><asp:requiredfieldvalidator id="rfvBlockRef" style="Z-INDEX: 104; LEFT: 224px; POSITION: absolute; TOP: 48px"
					runat="server" ToolTip="Required Field" CssClass="ValidatorText" ControlToValidate="txtBlockReference">*</asp:requiredfieldvalidator><asp:label id="lblNoBlocks" style="Z-INDEX: 105; LEFT: 16px; POSITION: absolute; TOP: 80px"
					runat="server">Number of blocks</asp:label><asp:regularexpressionvalidator id="revNoBlocks" style="Z-INDEX: 107; LEFT: 224px; POSITION: absolute; TOP: 80px"
					runat="server" ToolTip="Must be a numeric value" CssClass="ValidatorText" ControlToValidate="txtNoBlocks" ValidationExpression="^[1-9]+[0-9]*$">*</asp:regularexpressionvalidator><asp:requiredfieldvalidator id="rfvNoBlocks" style="Z-INDEX: 108; LEFT: 224px; POSITION: absolute; TOP: 80px"
					runat="server" ToolTip="Required field" CssClass="ValidatorText" ControlToValidate="txtNoBlocks">*</asp:requiredfieldvalidator><asp:label id="lblRepeatBlock" style="Z-INDEX: 110; LEFT: 344px; POSITION: absolute; TOP: 80px"
					runat="server">Additional Request</asp:label>
				<HR style="Z-INDEX: 111; LEFT: 16px; POSITION: absolute; TOP: 112px; HEIGHT: 1px" width="95%"
					SIZE="1">
				<asp:label id="lblSenderRef" style="Z-INDEX: 112; LEFT: 16px; POSITION: absolute; TOP: 16px"
					runat="server">Sender Ref</asp:label><asp:label id="lblHistologyRef" style="Z-INDEX: 114; LEFT: 344px; POSITION: absolute; TOP: 16px"
					runat="server">Histology Ref</asp:label><asp:label id="lblCustomerRef" style="Z-INDEX: 116; LEFT: 344px; POSITION: absolute; TOP: 48px"
					runat="server">Customer Reference</asp:label><asp:label id="lblStep1" style="Z-INDEX: 103; LEFT: 16px; POSITION: absolute; TOP: 123px" runat="server"
					Font-Bold="True">Step 1: Select tissues:</asp:label><asp:customvalidator id="revBlockRef" style="Z-INDEX: 118; LEFT: 224px; POSITION: absolute; TOP: 48px"
					runat="server" ToolTip="Enter a value between 1 and 999. The Block Ref must be at least two digits long. Valid ranges are therefore 01-99 and 100-999. Note, for > 01-99 no further leading zero should be entered. For example 001 is invalid, enter it as 01."
					CssClass="ValidatorText" ControlToValidate="txtBlockReference" ClientValidationFunction="ClientValidateBlockRef" OnServerValidate="ValidateBlockRefRef">*</asp:customvalidator></DIV>
			<DIV id="ctlDiv" style="WIDTH: 747px; POSITION: relative; HEIGHT: 36px" runat="server"
				ms_positioning="GridLayout"><asp:checkbox id="chkUseWholeTissueList" style="Z-INDEX: 101; LEFT: 200px; POSITION: absolute; TOP: 9px"
					runat="server"></asp:checkbox><asp:label id="lblTissueList" style="Z-INDEX: 102; LEFT: 16px; POSITION: absolute; TOP: 9px"
					runat="server">Use the entire tissue list?</asp:label></DIV>
			<DIV style="WIDTH: 576px"><asp:datagrid id="grdTissues" runat="server" AutoGenerateColumns="False" AllowPaging="True" PageSize="5">
					<SelectedItemStyle CssClass="GridSelectedItemSmall"></SelectedItemStyle>
					<EditItemStyle CssClass="GridEditItemSmall"></EditItemStyle>
					<ItemStyle CssClass="GridItemSmall"></ItemStyle>
					<HeaderStyle CssClass="GridHeader"></HeaderStyle>
					<Columns>
						<asp:ButtonColumn Text="&lt;img src=&quot;Images/GridPager/sel.gif&quot;&gt;" CommandName="Select">
							<ItemStyle HorizontalAlign="Left" Width="20px"></ItemStyle>
						</asp:ButtonColumn>
						<asp:TemplateColumn SortExpression="TissueCode" HeaderText="Tissue">
							<ItemStyle HorizontalAlign="Left" Width="400px"></ItemStyle>
							<ItemTemplate>
								<asp:Label id="lblTissueCodeDisplay" runat="server" Width="380px"></asp:Label>
							</ItemTemplate>
							<EditItemTemplate>
								<TABLE cellSpacing="0" cellPadding="0" border="0">
									<TR height="10">
										<td>
											<asp:DropDownList id="ddlTissueCodeEdit" Width="380px" Runat="server"></asp:DropDownList>
										</td>
										<td>
											<asp:Label ID="lblTissueError" Runat="server" CssClass="ValidatorText" ToolTip="Required Field">*</asp:Label>
										</td>
									</TR>
								</TABLE>
							</EditItemTemplate>
						</asp:TemplateColumn>
						<asp:TemplateColumn SortExpression="NoPieces" HeaderText="No Pieces">
							<ItemStyle HorizontalAlign="Left" Width="100px"></ItemStyle>
							<ItemTemplate>
								<asp:Label id="lblNoPiecesDisplay" runat="server" Width="100px"></asp:Label>
							</ItemTemplate>
							<EditItemTemplate>
								<TABLE cellSpacing="0" cellPadding="0" border="0">
									<TR height="10">
										<td>
											<asp:TextBox id="txtNoPiecesEdit" Width="80px" MaxLength="3" Runat="server"></asp:TextBox>
										</td>
										<td>
											<asp:RequiredFieldValidator id="rfvNoPieces" runat="server" CssClass="ValidatorText" Height="8px" ToolTip="Required Field"
												ErrorMessage="*" ControlToValidate="txtNoPiecesEdit" EnableClientScript="True" Enabled="True" EnableViewState="True"></asp:RequiredFieldValidator>
										</td>
										<TD>
											<asp:RegularExpressionValidator id="revNoPieces" runat="server" CssClass="ValidatorText" Height="8px" ToolTip="Numeric Value"
												ErrorMessage="*" ControlToValidate="txtNoPiecesEdit" ValidationExpression="^[1-9]+[0-9]*$"></asp:RegularExpressionValidator></TD>
									</TR>
								</TABLE>
							</EditItemTemplate>
						</asp:TemplateColumn>
					</Columns>
					<PagerStyle Visible="False"></PagerStyle>
				</asp:datagrid><uc1:datagridpager id="TissuesPager" runat="server"></uc1:datagridpager></DIV>
			<DIV style="WIDTH: 745px; POSITION: relative; HEIGHT: 50px" ms_positioning="GridLayout"><asp:label id="lblStep2" style="Z-INDEX: 101; LEFT: 16px; POSITION: absolute; TOP: 19px" runat="server"
					Font-Bold="True">Step 2: Select the required Histology, Antibodies and Special Stains:</asp:label>
				<HR style="Z-INDEX: 102; LEFT: 16px; POSITION: absolute; TOP: 8px; HEIGHT: 1px" width="95%"
					SIZE="1">
			</DIV>
			<TABLE id="Table1">
				<TR>
					<TD>Histology
					</TD>
					<TD>Antibodies
					</TD>
					<TD>Special Stain
					</TD>
				</TR>
				<TR>
					<TD colSpan="100">
						<HR width="100%" SIZE="1">
					</TD>
				</TR>
				<TR>
					<TD vAlign="top"><asp:checkboxlist id="chkblHistology" runat="server" Width="170px" AutoPostBack="True"></asp:checkboxlist></TD>
					<TD vAlign="top"><asp:checkboxlist id="chkblAntibodies" runat="server" Width="170px"></asp:checkboxlist></TD>
					<TD vAlign="top"><asp:checkboxlist id="chkblSpecialStain" runat="server" Width="170px"></asp:checkboxlist></TD>
					<TD vAlign="top"><asp:label id="lblError" runat="server" ToolTip="Must add atleast one tissue and assign one test to the block"
							CssClass="ValidatorText" Visible="False">*</asp:label></TD>
				</TR>
			</TABLE>
			<DIV style="WIDTH: 745px; POSITION: relative; HEIGHT: 176px" ms_positioning="GridLayout">
				<HR style="Z-INDEX: 101; LEFT: 16px; WIDTH: 95.42%; POSITION: absolute; TOP: 133px; HEIGHT: 1px"
					width="95.42%" SIZE="1">
				<asp:checkbox id="chkbCarryTests" style="Z-INDEX: 108; LEFT: 279px; POSITION: absolute; TOP: 7px"
					runat="server"></asp:checkbox><asp:textbox id="txtComments" style="Z-INDEX: 105; LEFT: 16px; POSITION: absolute; TOP: 69px"
					runat="server" Width="726px" TextMode="MultiLine" Height="56px"></asp:textbox><asp:button id="btnAddBlock" style="Z-INDEX: 103; LEFT: 390px; POSITION: absolute; TOP: 141px"
					runat="server" Width="104px" Height="24px" Text="Next Block" CausesValidation="False"></asp:button><asp:button id="btnDone" style="Z-INDEX: 102; LEFT: 616px; POSITION: absolute; TOP: 141px" runat="server"
					Width="104" Height="24px" Text="Done" CausesValidation="False"></asp:button><asp:button id="btnCancel" style="Z-INDEX: 104; LEFT: 503px; POSITION: absolute; TOP: 141px"
					runat="server" Width="104px" Height="24px" Text="Cancel" CausesValidation="False"></asp:button>
				<HR style="Z-INDEX: 106; LEFT: 16px; POSITION: absolute; TOP: 35px; HEIGHT: 1px" width="95%"
					SIZE="1">
				<asp:label id="lblComment" style="Z-INDEX: 107; LEFT: 16px; POSITION: absolute; TOP: 45px"
					runat="server">Comments and details of other tests:</asp:label><asp:label id="lblCarryTest" style="Z-INDEX: 109; LEFT: 16px; POSITION: absolute; TOP: 7px"
					runat="server">Use these tests for the next block?</asp:label></DIV>
			<DIV id="ctlErrorDiv" style="WIDTH: 750px; HEIGHT: 4px" runat="server"></DIV>
			<uc1:vlafooter id="VLAFooter1" runat="server"></uc1:vlafooter></form>
	</body>
</HTML>
