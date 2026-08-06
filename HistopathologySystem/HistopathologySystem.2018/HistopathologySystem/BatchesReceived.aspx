<%@ Register TagPrefix="uc1" TagName="DataGridPager" Src="DataGridPager.ascx" %>
<%@ Page Language="vb" AutoEventWireup="false" Codebehind="BatchesReceived.aspx.vb" Inherits="HistopathologySystem.ReceivedBatches" smartNavigation="True"%>
<%@ Register TagPrefix="uc1" TagName="VLAHeader" Src="VLAHeader.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAFooter" Src="VLAFooter.ascx" %>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<HTML>
	<HEAD>
		<title>ReceivedBatches</title>
		<meta content="Microsoft Visual Studio.NET 7.0" name="GENERATOR">
		<meta content="Visual Basic 7.0" name="CODE_LANGUAGE">
		<meta content="JavaScript" name="vs_defaultClientScript">
		<meta content="http://schemas.microsoft.com/intellisense/ie5" name="vs_targetSchema">
		<LINK href="Style/vla-ie.css" type="text/css" rel="stylesheet">
	</HEAD>
	<body>
		<form id="Form1" method="post" runat="server">
			<uc1:vlaheader id="VLAHeader1" runat="server"></uc1:vlaheader>
			<DIV style="WIDTH: 738px; POSITION: relative; HEIGHT: 105px" ms_positioning="GridLayout">
				<asp:TextBox id="txtSubmissionID" style="Z-INDEX: 101; LEFT: 215px; POSITION: absolute; TOP: 61px"
					runat="server" Width="86px"></asp:TextBox>
				<asp:Label id="lblEnter" style="Z-INDEX: 102; LEFT: 10px; POSITION: absolute; TOP: 62px" runat="server">Enter Submission Number:</asp:Label>
				<asp:RegularExpressionValidator id="revSubmissionID" style="Z-INDEX: 103; LEFT: 304px; POSITION: absolute; TOP: 61px"
					runat="server" ValidationExpression="^[1-9]+[0-9]*$" CssClass="ValidatorText" ControlToValidate="txtSubmissionID"
					ToolTip="Must be numeric">*</asp:RegularExpressionValidator>
				<asp:Button id="btnGo" style="Z-INDEX: 104; LEFT: 333px; POSITION: absolute; TOP: 61px" runat="server"
					Width="46px" Text="Go" CausesValidation="False"></asp:Button>
				<asp:Label id="lblExplain" style="Z-INDEX: 105; LEFT: 10px; POSITION: absolute; TOP: 13px"
					runat="server">If the submission number is known enter the number into the 'Enter Submission Number' textbox and click on the 'Go' button. If not, select the submission from the grid.</asp:Label>
				<HR style="Z-INDEX: 106; LEFT: 13px; WIDTH: 95.79%; POSITION: absolute; TOP: 98px; HEIGHT: 1px"
					width="95.79%" SIZE="1">
				<asp:RequiredFieldValidator id="rfvSubmissionID" style="Z-INDEX: 107; LEFT: 304px; POSITION: absolute; TOP: 61px"
					runat="server" ToolTip="Required Field" ControlToValidate="txtSubmissionID" CssClass="ValidatorText">*</asp:RequiredFieldValidator>
			</DIV>
			<DIV id="ctlDiv" style="WIDTH: 735px" runat="server" ms_positioning="FlowLayout"></DIV>
			<DIV style="WIDTH: 741px; POSITION: relative; HEIGHT: 32px" ms_positioning="GridLayout">
				<asp:label id="lblBatches" style="Z-INDEX: 101; LEFT: 10px; POSITION: absolute; TOP: 8px" runat="server"
					Font-Bold="True"> Submissions Received</asp:label></DIV>
			<DIV style="WIDTH: 715px">
				<asp:datagrid id="grdBatches" runat="server" PageSize="20" AllowPaging="True" AllowSorting="True"
					AutoGenerateColumns="False">
					<SelectedItemStyle CssClass="GridSelectedItemSmall"></SelectedItemStyle>
					<EditItemStyle CssClass="GridEditItemSmall"></EditItemStyle>
					<ItemStyle CssClass="GridItemSmall"></ItemStyle>
					<HeaderStyle CssClass="GridHeader"></HeaderStyle>
					<Columns>
						<asp:ButtonColumn Text="&lt;img src=&quot;Images/GridPager/sel.gif&quot;&gt;" CommandName="Select">
							<ItemStyle HorizontalAlign="Left" Width="20px"></ItemStyle>
						</asp:ButtonColumn>
						<asp:BoundColumn DataField="ID" SortExpression="ID" HeaderText="Submission Number">
							<ItemStyle HorizontalAlign="Left" Width="150px"></ItemStyle>
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
						<asp:BoundColumn Visible="False" DataField="SubmittedBy"></asp:BoundColumn>
						<asp:TemplateColumn SortExpression="AllTissuesAssigned" HeaderText="All Tissues Assigned">
							<ItemStyle HorizontalAlign="Center" Width="100px"></ItemStyle>
							<ItemTemplate>
								<asp:CheckBox ID="cbAllTissuesAssigned" Enabled="False" Runat="server"></asp:CheckBox>
							</ItemTemplate>
						</asp:TemplateColumn>
					</Columns>
					<PagerStyle Visible="False"></PagerStyle>
				</asp:datagrid>
				<uc1:datagridpager id="BatchesPager" runat="server"></uc1:datagridpager></DIV>
			<DIV style="WIDTH: 738px; POSITION: relative; HEIGHT: 37px" ms_positioning="GridLayout">
				<asp:Button id="btnHome" style="Z-INDEX: 101; LEFT: 654px; POSITION: absolute; TOP: 5px" runat="server"
					Width="74px" Text="Done" CausesValidation="False"></asp:Button></DIV>
			<uc1:vlafooter id="VLAFooter1" runat="server"></uc1:vlafooter>
		</form>
	</body>
</HTML>
