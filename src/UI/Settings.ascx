<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="Settings.ascx.cs" Inherits="DNN.Modules.NewsArticlesFriendlyUrlProvider.UI.Settings" %>
<%@ Register TagPrefix="dnn" TagName="Label" Src="~/controls/LabelControl.ascx" %>

<h2>News Articles URL Provider Settings</h2>
<div class="dnnForm"> 
    <div id="settings">
        <asp:Label ID="lblHeader" runat="server" CssClass="NormalBold" />
        <div id="tabsettingscont">
        <table class="Normal" id="tabsettings" cellpadding="0" cellspacing="0">
            <tr>
                <td><asp:Label ID="lblStartingArticleId" runat="server" ResourceKey="StartingArticleId" /></td>
                <td><asp:TextBox ID="txtStartingArticleId" runat="server" /></td>
            </tr>
            <tr>
                <td><asp:Label ID="lblRedirectOldUrls" runat="server" ResourceKey="RedirectOldUrls" /></td>
                <td><asp:CheckBox ID="chkRedirectOldUrls" runat="server" /></td>
            </tr>

            <tr>
                <td colspan="2"><asp:Label id="lblTabSettings" runat="server" ResourceKey="TabSettings"/></td>
            </tr>
            <asp:Repeater ID="rptTabs" runat="server">
                <ItemTemplate>
                    <tr>
                        <td class="nap_tl nap_tr" colspan="2">
                            <asp:Label ID="lblPageName" CssClass="Normal nap_pagename" runat="server" />
                        </td>
                    </tr>
                    <tr>
                        <asp:HiddenField ID="hdnTabId" runat="server" />
                        <td class="nap_l"><asp:Label ID="lblArticleUrlStyle" runat="server" CssClass="Normal"/></td>
                        <td class="nap_r"><asp:Label ID="lblArticleUrlSource" runat="server" CssClass="Normal" /></td>
                    </tr>
                    <tr>
                        <td class="nap_l"><asp:DropDownList ID="ddlArticleUrlStyle" CssClass="Normal" runat="server" /></td>
                        <td class="nap_r"><asp:DropDownList ID="ddlArticleUrlSource" CssClass="Normal" runat="server" /></td>
                    </tr>
                    <tr>
                        <td class="nap_l"><asp:Label ID="lblCategoryUrlStyle" runat="server" CssClass="Normal" /></td>
                        <td class="nap_r"><asp:Label ID="lblAuthorUrlStyle" runat="server" CssClass="Normal" /></td>
                    </tr>
                    <tr>
                        <td class="nap_l"><asp:DropDownList ID="ddlCategoryUrlStyle" runat="server" CssClass="Normal" /></td>
                        <td class="nap_r"><asp:DropDownList ID="ddlAuthorUrlStyle" runat="server" CssClass="Normal" /></td>
                    </tr>
                    <tr>
                        <td class="nap_l"><asp:Label ID="lblPageUrlStyle" runat="server" CssClass="Normal" /></td>
                        <td class="nap_r"><asp:Label ID="lblNoDnnPagePath" runat="server" CssClass="Normal" /></td>
                    </tr>
                    <tr>
                        <td class="nap_bl"><asp:DropDownList ID="ddlPageUrlStyle" runat="server"></asp:DropDownList></td>
                        <td class="nap_br"><asp:CheckBox ID="chkNoDnnPagePath" runat="server" CssClass="nap_noDnnPagePath" /></td>
                    </tr>
                </ItemTemplate>
            </asp:Repeater>
        </table>
        </div>
    </div>
</div>