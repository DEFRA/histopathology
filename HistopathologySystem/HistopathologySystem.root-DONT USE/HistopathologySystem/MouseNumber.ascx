<%@ Control Language="vb" AutoEventWireup="false" Codebehind="MouseNumber.ascx.vb" Inherits="HistopathologySystem.MouseNumber" TargetSchema="http://schemas.microsoft.com/intellisense/ie5" %>
<div style="LEFT: 0px; POSITION: absolute; TOP: 0px"><asp:textbox id="txtMouseNumber" runat="server" Width="160px" MaxLength="8"></asp:textbox></div>
<div style="LEFT: 163px; POSITION: absolute; TOP: 2px"><asp:requiredfieldvalidator id="rfvMouseNumber" runat="server" CssClass="ValidatorText" ControlToValidate="txtMouseNumber" ToolTip="Required Field">*</asp:requiredfieldvalidator></div>
<div style="LEFT: 163px; POSITION: absolute; TOP: 2px"><asp:customvalidator id="valMouseNumber" runat="server" CssClass="ValidatorText" ControlToValidate="txtMouseNumber" ToolTip="Format: MCNNNNNN" OnServerValidate="ValidateMouseNumber" ClientValidationFunction="ClientValidateMouseNumber">*</asp:customvalidator></div>
<div style="LEFT: 163px; POSITION: absolute; TOP: 2px"><asp:Label id="lblError" runat="server" CssClass="ValidatorText" Visible="False">*</asp:Label></div>
<div style="LEFT: 163px; POSITION: absolute; TOP: 2px"></div>
