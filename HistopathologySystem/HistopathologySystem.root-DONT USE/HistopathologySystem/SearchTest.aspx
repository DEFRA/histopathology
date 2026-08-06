<%@ Register TagPrefix="uc1" TagName="VLAFooter" Src="VLAFooter.ascx" %>
<%@ Page Language="vb" AutoEventWireup="false" Codebehind="SearchTest.aspx.vb" Inherits="HistopathologySystem.SearchTest" smartNavigation="True"%>
<%@ Register TagPrefix="uc1" TagName="CalendarDate" Src="CalendarDate.ascx" %>
<%@ Register TagPrefix="uc1" TagName="VLAHeader" Src="VLAHeader.ascx" %>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<HTML>
	<HEAD>
		<title>SearchTest</title>
		<meta content="Microsoft Visual Studio.NET 7.0" name="GENERATOR">
		<meta content="Visual Basic 7.0" name="CODE_LANGUAGE">
		<meta content="JavaScript" name="vs_defaultClientScript">
		<meta content="http://schemas.microsoft.com/intellisense/ie5" name="vs_targetSchema">
		<LINK href="Style/vla-ie.css" type="text/css" rel="stylesheet">
	</HEAD>
	<body>
	    
		<form id="Form1" method="post" runat="server">
			<uc1:vlaheader id="VLAHeader1" runat="server"></uc1:vlaheader>
			<DIV style="WIDTH: 784px; POSITION: relative; HEIGHT: 124px" ms_positioning="GridLayout">
				<asp:dropdownlist id="ddlProjectList" style="Z-INDEX: 103; LEFT: 191px; POSITION: absolute; TOP: 42px"
					runat="server" Width="165px" tabIndex="3"></asp:dropdownlist>
				<asp:label id="lblProjectCode" style="Z-INDEX: 101; LEFT: 6px; POSITION: absolute; TOP: 40px"
					runat="server" Height="18px">Project or Contract code</asp:label><asp:label id="lblStartDate" style="Z-INDEX: 102; LEFT: 6px; POSITION: absolute; TOP: 12px"
					runat="server" Height="18px">Start Date</asp:label>
				<DIV style="Z-INDEX: 105; LEFT: 191px; WIDTH: 184px; POSITION: absolute; TOP: 12px; HEIGHT: 40px"
					tabIndex="1"><uc1:calendardate id="StartDate" runat="server"></uc1:calendardate></DIV>
				<asp:label id="lblEndDate" style="Z-INDEX: 104; LEFT: 396px; POSITION: absolute; TOP: 12px"
					runat="server" Height="18px">End Date</asp:label>
				<DIV style="Z-INDEX: 106; LEFT: 464px; WIDTH: 160px; POSITION: absolute; TOP: 12px; HEIGHT: 40px"
					ms_positioning="FlowLayout">
					<uc1:CalendarDate id="EndDate" runat="server"></uc1:CalendarDate></DIV>
			</DIV>
			<TABLE id="Table">
				<TR>
					<TD style="WIDTH: 142px">Test:</TD>
					<TD>Histology
					</TD>
					<TD>Antibodies</TD>
					<TD>Special Stain
					</TD>
					<td width="60"></td>
				</TR>
				<TR>
					<TD colSpan="100">
						<HR width="100%" SIZE="1">
					</TD>
				</TR>
				<TR>
					<TD style="WIDTH: 142px" vAlign="top"></TD>
					<TD vAlign="top"><asp:checkboxlist id="chkblHistology" runat="server" Width="170px" AutoPostBack="True" tabIndex="4"></asp:checkboxlist></TD>
					<TD vAlign="top"><asp:checkboxlist id="chkblAntibodies" runat="server" Width="170px" tabIndex="5"></asp:checkboxlist></TD>
					<TD vAlign="top"><asp:checkboxlist id="chkblSpecialStain" runat="server" Width="170px" tabIndex="6"></asp:checkboxlist></TD>
					<TD vAlign="top"></TD>
				</TR>
			</TABLE>
			<DIV style="WIDTH: 740px; POSITION: relative; HEIGHT: 110px" ms_positioning="GridLayout">
			    <asp:button id="btnCount" style="Z-INDEX: 100; LEFT: 4px; POSITION: absolute; TOP: 11px" runat="server" Width="125" Text="Analyse Results" Height="25" tabIndex="7"></asp:button>
			    <asp:button id="bntBatch" style="Z-INDEX: 100; LEFT: 4px; POSITION: absolute; TOP: 40px" runat="server" Width="125" Text="Analyse Submissions" Height="25" tabIndex="8"></asp:button>
			    <asp:label id="Label1" style="Z-INDEX: 103; LEFT: 148px; POSITION: absolute; TOP: 11px" runat="server" Width="445px">Please wait for the table of results to appear after pressing 'Analyse Results'.</asp:label>
				<asp:hyperlink id="hlbExcel" style="Z-INDEX: 104; LEFT: 624px; POSITION: absolute; TOP: 17px" runat="server" Width="95px" Visible="False" Target="_blank" NavigateUrl="ExcelExport.aspx" tabIndex="10">Export Outputs to Excel</asp:hyperlink>
				<asp:hyperlink id="hlbBatchExcel" style="Z-INDEX: 104; LEFT: 624px; POSITION: absolute; TOP: 50px" runat="server" Width="95px" Visible="False" Target="_blank" NavigateUrl="ExcelExport.aspx" tabIndex="11">Export Submissions to Excel</asp:hyperlink>
				<asp:Button id="btnOutputsMenu" style="Z-INDEX: 107; LEFT: 4px; POSITION: absolute; TOP: 70px" runat="server" Width="125px" Text="Outputs Menu" Height="25px" tabIndex="9"></asp:Button></DIV>
			<DIV id="ctlDiv" style="WIDTH: 741px; HEIGHT: 10px" runat="server"></DIV>
			<DIV style="WIDTH: 914px">
			<asp:datagrid id="grdResults" runat="server" Height="6px" Width="700px" AllowSorting="True">
					<HeaderStyle CssClass="GridHeader"></HeaderStyle>
			</asp:datagrid></DIV>
			<br />
			<div style="WIDTH: 700px">
			        <asp:DataGrid ID="grdBatchResult" runat="server" AutoGenerateColumns="false" Width="700px">
			            <HeaderStyle CssClass="GridHeader"></HeaderStyle>
			            <Columns>
			                <asp:BoundColumn HeaderText="Premium" DataField="Description" ItemStyle-Width="80px"></asp:BoundColumn>
			                <asp:TemplateColumn HeaderText="Submissions" ItemStyle-Width="500px" ItemStyle-Wrap="true">
			                    <ItemTemplate>
			                        <asp:Panel ID="pnlSubmissions" runat="server" Width="500px"></asp:Panel>
			                    </ItemTemplate>
			                </asp:TemplateColumn>
			            </Columns>    
			        </asp:DataGrid>
			</div>
			<uc1:vlafooter id="VLAFooter1" runat="server"></uc1:vlafooter>
		</form>
	</body>
</HTML>
