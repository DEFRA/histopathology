<%@ Register TagPrefix="uc1" TagName="DataGridPager" Src="DataGridPager.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAFooter" Src="VLAFooter.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAHeader" Src="VLAHeader.ascx" %>
<%@ Page Language="vb" AutoEventWireup="false" Codebehind="BatchesForEditing.aspx.vb" Inherits="HistopathologySystem.BatchesForEditing" smartNavigation="True"%>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<HTML>
	<HEAD>
		<title>BatchesForEditing</title>
		<meta name="GENERATOR" content="Microsoft Visual Studio.NET 7.0">
		<meta name="CODE_LANGUAGE" content="Visual Basic 7.0">
		<meta name="vs_defaultClientScript" content="JavaScript">
		<meta name="vs_targetSchema" content="http://schemas.microsoft.com/intellisense/ie5">
		<LINK href="Style/vla-ie.css" type="text/css" rel="stylesheet">
	</HEAD>
	<body>
		<form id="Form1" method="post" runat="server">
			<uc1:VLAHeader id="VLAHeader1" runat="server"></uc1:VLAHeader>
			<DIV style="WIDTH: 738px; POSITION: relative; HEIGHT: 105px" ms_positioning="GridLayout">
				<asp:TextBox id="txtSubmissionID" style="Z-INDEX: 101; LEFT: 215px; POSITION: absolute; TOP: 61px" runat="server" Width="86px"></asp:TextBox>
				<asp:Label id="lblEnter" style="Z-INDEX: 102; LEFT: 10px; POSITION: absolute; TOP: 62px" runat="server">Enter Submission Number:</asp:Label>
				<asp:RegularExpressionValidator id="revSubmissionID" style="Z-INDEX: 103; LEFT: 304px; POSITION: absolute; TOP: 61px" runat="server" CssClass="ValidatorText" ToolTip="Must be numeric" ControlToValidate="txtSubmissionID" ValidationExpression="^[1-9]+[0-9]*$">*</asp:RegularExpressionValidator>
				<asp:Button id="btnGo" style="Z-INDEX: 104; LEFT: 333px; POSITION: absolute; TOP: 61px" runat="server" Width="46px" Text="Go" CausesValidation="False"></asp:Button>
				<asp:Label id="lblExplain" style="Z-INDEX: 105; LEFT: 10px; POSITION: absolute; TOP: 13px" runat="server">If the submission number is known enter the number into the 'Enter Submission number' textbox and click on the 'Go' button. If not, select the submission from the grid.</asp:Label>
				<HR style="Z-INDEX: 106; LEFT: 13px; WIDTH: 95.79%; POSITION: absolute; TOP: 98px; HEIGHT: 1px" width="95.79%" SIZE="1">
				<asp:RequiredFieldValidator id="rfvSubmissionID" style="Z-INDEX: 107; LEFT: 304px; POSITION: absolute; TOP: 61px" runat="server" CssClass="ValidatorText" ToolTip="Required Field" ControlToValidate="txtSubmissionID">*</asp:RequiredFieldValidator></DIV>
			<DIV id="ctlDiv" style="WIDTH: 735px" runat="server" ms_positioning="FlowLayout"></DIV>
			<DIV style="WIDTH: 741px; POSITION: relative; HEIGHT: 32px" ms_positioning="GridLayout">
				<asp:label id="lblBatches" style="Z-INDEX: 101; LEFT: 10px; POSITION: absolute; TOP: 8px" runat="server" Font-Bold="True"></asp:label></DIV>
			<DIV style="WIDTH: 748px">
				<asp:datagrid id="grdBatchesForEditing" runat="server" AllowPaging="True" AllowSorting="True" AutoGenerateColumns="False" PageSize="20">
					<SelectedItemStyle CssClass="GridSelectedItemSmall"></SelectedItemStyle>
					<EditItemStyle CssClass="GridEditItemSmall"></EditItemStyle>
					<ItemStyle CssClass="GridItemSmall"></ItemStyle>
					<HeaderStyle CssClass="GridHeader"></HeaderStyle>
					<Columns>
						<asp:ButtonColumn Text="&lt;img src=&quot;Images/GridPager/sel.gif&quot;&gt;" CommandName="Select">
							<ItemStyle HorizontalAlign="Left" Width="20px"></ItemStyle>
						</asp:ButtonColumn>
						<asp:BoundColumn DataField="ID" SortExpression="ID" HeaderText="Submission Number">
							<ItemStyle HorizontalAlign="Left" Width="120px"></ItemStyle>
						</asp:BoundColumn>
						<asp:BoundColumn DataField="ProjectDescription" SortExpression="ProjectDescription" HeaderText="Project Code">
							<ItemStyle HorizontalAlign="Left" Width="150px"></ItemStyle>
						</asp:BoundColumn>
						<asp:BoundColumn DataField="ContactDescription" SortExpression="ContactDescription" HeaderText="Pathologist">
							<ItemStyle HorizontalAlign="Left" Width="150px"></ItemStyle>
						</asp:BoundColumn>
						<asp:BoundColumn DataField="Species" SortExpression="Species" HeaderText="Species">
							<ItemStyle HorizontalAlign="Left" Width="80px"></ItemStyle>
						</asp:BoundColumn>
						<asp:BoundColumn DataField="BatchDate" SortExpression="BatchDate" HeaderText="Submitted Date" DataFormatString="{0:d}">
							<ItemStyle HorizontalAlign="Left" Width="100px"></ItemStyle>
						</asp:BoundColumn>
						<asp:BoundColumn DataField="Status" SortExpression="Status" HeaderText="Status">
							<ItemStyle HorizontalAlign="Left" Width="100px"></ItemStyle>
						</asp:BoundColumn>
						<asp:BoundColumn Visible="False" DataField="SubmittedBy"></asp:BoundColumn>
					</Columns>
					<PagerStyle Visible="False"></PagerStyle>
				</asp:datagrid>
				<uc1:DataGridPager id="BatchesPager" runat="server"></uc1:DataGridPager></DIV>
			<DIV style="WIDTH: 747px; POSITION: relative; HEIGHT: 40px" ms_positioning="GridLayout">
				<asp:Button id="btnHome" style="Z-INDEX: 101; LEFT: 660px; POSITION: absolute; TOP: 7px" runat="server" Width="75px" Text="Done" CausesValidation="False"></asp:Button></DIV>
			<uc1:VLAFooter id="VLAFooter1" runat="server"></uc1:VLAFooter>
		</form>
	</body>
</HTML>
