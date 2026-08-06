<%@ Page Language="vb" AutoEventWireup="false" Codebehind="QCNotes.aspx.vb" Inherits="HistopathologySystem.QCNotes" smartNavigation="True"%>
<%@ Register TagPrefix="uc1" TagName="DataGridPager" Src="DataGridPager.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAHeader" Src="VLAHeader.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAFooter" Src="VLAFooter.ascx" %>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<HTML>
	<HEAD>
		<title>QCNotes</title>
		<meta name="GENERATOR" content="Microsoft Visual Studio.NET 7.0">
		<meta name="CODE_LANGUAGE" content="Visual Basic 7.0">
		<meta name="vs_defaultClientScript" content="JavaScript">
		<meta name="vs_targetSchema" content="http://schemas.microsoft.com/intellisense/ie5">
		<LINK href="Style/vla-ie.css" type="text/css" rel="stylesheet">
	</HEAD>
	<body>
		<form id="Form1" method="post" runat="server">
			<uc1:VLAHeader id="VLAHeader1" runat="server"></uc1:VLAHeader>
			<DIV style="WIDTH: 736px; POSITION: relative; HEIGHT: 105px" ms_positioning="GridLayout">
				<asp:TextBox id="txtQCNote" style="Z-INDEX: 101; LEFT: 151px; POSITION: absolute; TOP: 61px"
					runat="server" Width="86px"></asp:TextBox>
				<asp:Label id="lblEnter" style="Z-INDEX: 102; LEFT: 10px; POSITION: absolute; TOP: 62px" runat="server">Enter QC Note ref:</asp:Label>
				<asp:RegularExpressionValidator id="revQCNoteRef" style="Z-INDEX: 103; LEFT: 239px; POSITION: absolute; TOP: 61px"
					runat="server" CssClass="ValidatorText" ToolTip="Must be numeric" ControlToValidate="txtQCNote" ValidationExpression="^[1-9]+[0-9]*$">*</asp:RegularExpressionValidator>
				<asp:Button id="btnGo" style="Z-INDEX: 104; LEFT: 271px; POSITION: absolute; TOP: 61px" runat="server"
					Text="Go" Width="46px" CausesValidation="False"></asp:Button>
				<asp:Label id="lblExplain" style="Z-INDEX: 105; LEFT: 10px; POSITION: absolute; TOP: 13px"
					runat="server">If the QC Note ref is known enter the number into the 'Enter  QC Note ref' textbox and click on the 'Go' button. If not, select the QC Note from the grid.</asp:Label>
				<HR style="Z-INDEX: 106; LEFT: 13px; WIDTH: 95.79%; POSITION: absolute; TOP: 98px; HEIGHT: 1px"
					width="95.79%" SIZE="1">
				<asp:RequiredFieldValidator id="rfvQcNote" style="Z-INDEX: 107; LEFT: 239px; POSITION: absolute; TOP: 61px"
					runat="server" CssClass="ValidatorText" ToolTip="Required Field" ControlToValidate="txtQCNote">*</asp:RequiredFieldValidator></DIV>
			<DIV id="ctlDiv" style="WIDTH: 735px; HEIGHT: 24px" runat="server" ms_positioning="FlowLayout"></DIV>
			<DIV style="WIDTH: 682px">
				<asp:datagrid id="grdQCNotes" runat="server" PageSize="20" AutoGenerateColumns="False" AllowSorting="True"
					AllowPaging="True">
					<SelectedItemStyle CssClass="GridSelectedItem"></SelectedItemStyle>
					<EditItemStyle CssClass="GridEditItem"></EditItemStyle>
					<ItemStyle CssClass="GridItem"></ItemStyle>
					<HeaderStyle CssClass="GridHeader"></HeaderStyle>
					<Columns>
						<asp:ButtonColumn Text="&lt;img src=&quot;Images/GridPager/sel.gif&quot;&gt;" CommandName="Select">
							<ItemStyle HorizontalAlign="Left" Width="20px"></ItemStyle>
						</asp:ButtonColumn>
						<asp:BoundColumn DataField="QCNoteRef" SortExpression="QCNoteRef" HeaderText="QC Note Ref">
							<ItemStyle HorizontalAlign="Left" Width="120px"></ItemStyle>
						</asp:BoundColumn>
						<asp:BoundColumn DataField="ID" SortExpression="ID" HeaderText="Submission Number">
							<ItemStyle HorizontalAlign="Left" Width="120px"></ItemStyle>
						</asp:BoundColumn>
						<asp:BoundColumn DataField="StainRef" SortExpression="StainRef" HeaderText="Stain Ref">
							<ItemStyle HorizontalAlign="Left" Width="120px"></ItemStyle>
						</asp:BoundColumn>
						<asp:BoundColumn DataField="ProjectDescription" SortExpression="ProjectDescription" HeaderText="Project Code">
							<ItemStyle HorizontalAlign="Left" Width="150px"></ItemStyle>
						</asp:BoundColumn>
						<asp:BoundColumn DataField="Species" SortExpression="Species" HeaderText="Species">
							<ItemStyle HorizontalAlign="Left" Width="120px"></ItemStyle>
						</asp:BoundColumn>
					</Columns>
					<PagerStyle Visible="False"></PagerStyle>
				</asp:datagrid>
				<uc1:DataGridPager id="QCNotePager" runat="server"></uc1:DataGridPager></DIV>
			<DIV style="WIDTH: 744px; POSITION: relative; HEIGHT: 56px" ms_positioning="GridLayout">
				<HR style="Z-INDEX: 101; LEFT: 8px; WIDTH: 95.7%; POSITION: absolute; TOP: 8px; HEIGHT: 1px"
					width="95.7%" SIZE="1">
				<asp:Button id="btnPrint" style="Z-INDEX: 102; LEFT: 16px; POSITION: absolute; TOP: 16px" runat="server"
					Width="105" Text="Print" Height="24" CausesValidation="False"></asp:Button>
				<asp:Button id="btnEdit" style="Z-INDEX: 103; LEFT: 128px; POSITION: absolute; TOP: 16px" runat="server"
					Width="105px" Text="Edit" Height="24px" CausesValidation="False"></asp:Button>
				<asp:Button id="btnDone" style="Z-INDEX: 104; LEFT: 616px; POSITION: absolute; TOP: 16px" runat="server"
					Width="105px" Text="Done" Height="24px" CausesValidation="False"></asp:Button></DIV>
			<uc1:VLAFooter id="VLAFooter1" runat="server"></uc1:VLAFooter>
		</form>
	</body>
</HTML>
