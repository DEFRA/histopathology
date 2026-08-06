<%@ Control Language="vb" AutoEventWireup="false" Codebehind="SenderRef.ascx.vb" Inherits="HistopathologySystem.SenderRef" TargetSchema="http://schemas.microsoft.com/intellisense/ie5" %>
<div style="LEFT: 0px; POSITION: absolute; TOP: 0px"><asp:textbox id="txtSenderRef" MaxLength="20" Width="160px" runat="server"></asp:textbox></div>
<div style="LEFT: 163px; POSITION: absolute; TOP: 2px"><asp:requiredfieldvalidator id="rfvSenderRef" runat="server" ToolTip="Required Field" ControlToValidate="txtSenderRef" CssClass="ValidatorText">*</asp:requiredfieldvalidator></div>
<div style="LEFT: 163px; POSITION: absolute; TOP: 2px"><asp:customvalidator id="valSenderRef" runat="server" ToolTip="PG Number format PGNNNN/NN, Mouse number format MCNNNNNN" ControlToValidate="txtSenderRef" CssClass="ValidatorText" OnServerValidate="ValidateSenderRef">*</asp:customvalidator></div>
<div style="LEFT: 163px; POSITION: absolute; TOP: 2px"><asp:Label id="lblError" runat="server" CssClass="ValidatorText" Visible="False">*</asp:Label></div>
<div style="LEFT: 163px; POSITION: absolute; TOP: 2px"></div>
