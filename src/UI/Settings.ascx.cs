/*
' Copyright (c) 2017  patapscoresearch.com
'  All rights reserved.
' 
' THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED
' TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL
' THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF
' CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
' DEALINGS IN THE SOFTWARE.
' 
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Web.UI.WebControls;

using DotNetNuke.Entities.Tabs;
using DotNetNuke.Entities.Modules;
using DotNetNuke.Entities.Urls;

using DNN.Modules.NewsArticlesFriendlyUrlProvider.Entities;
using DotNetNuke.Services.Localization;

namespace DNN.Modules.NewsArticlesFriendlyUrlProvider.UI

{
    /// <summary>
    /// This is the code-behind for the Settings.ascx control.  This inherits from the standard .NET UserControl, but also implements the ModuleProvider specific IProviderSettings.
    /// This control will be loaded by the Portal Urls page.  It is optional for module providers, but allows users to control module settings via the interface, rather than 
    /// having to set options via web.config settings.  The writing / reading of the items from the configuration file is handled by the Url Master module, and doesn't need to 
    /// be implemented.
    /// </summary>
    public partial class Settings : PortalModuleBase, IExtensionUrlProviderSettingsControl
    {
        //private int _portalId;
        private NewsArticlesFriendlyUrlProviderInfo _provider;
        #region controls
        //protected Label lblHeader;
        protected Label lblSettings;
        //protected TextBox txtStartingArticleId;
        //protected CheckBox chkRedirectOldUrls;
        //protected Repeater rptTabs;

        protected Dictionary<string, string> _providerSettings;

        protected string _articleUrlStyleText;
        protected string _articleUrlSourceText;
        protected string _categoryUrlStyleText;
        protected string _pageUrlStyleText;
        protected string _authorUrlStyleText;
        protected string _noDnnPagePathText;
        protected int _noDnnPagePathTabId;
        protected string _needTabSpecifiedText;

        #endregion
        #region Web Form Designer Generated Code
        //[System.Diagnostics.DebuggerStepThrough]
        override protected void OnInit(EventArgs e)
        {
            InitializeComponent();
            base.OnInit(e);
        }

        /// <summary>
        ///		Required method for Designer support - do not modify
        ///		the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.Load += new System.EventHandler(this.Page_Load);
            rptTabs.ItemDataBound += new RepeaterItemEventHandler(rptTabs_ItemDataBound);
        }

        private void rptTabs_ItemDataBound(object sender, RepeaterItemEventArgs e)
        {
            TabToScreen(e.Item);
        }

        #endregion
        #region events code
        protected void Page_Load(object sender, EventArgs e)
        {
            try
            {
                //note page load runs after LoadSettings(); because of dynamic control loading

                //now get the javascript in
                ScriptController.InjectjQueryLibary(this.Page, true, true);
                //module-specific jquery code
                ScriptController.InjectJsLibrary(this.Page, "nap", this.ControlPath + "/js/nap.js", true, ScriptController.ScriptInjectOrder.e_Default);
                //and get the css file in
                ScriptController.InjectCssReference(this.Page, "nap_css", this.ControlPath + "/newsarticlesprovider.css", true, ScriptController.CssInjectOrder.f_Last);
                //and put in a pre-dnn 6 ui script plus the jquery tabs library
                string preDnn6CssFile = this.ControlPath + "/ui-tabs.css";
                ScriptController.InjectjQueryTabsCss(this.Page, preDnn6CssFile, null);
            }
            catch (Exception ex)
            {
                DotNetNuke.Services.Exceptions.Exceptions.ProcessModuleLoadException(this, ex);
            }
        }

        protected void ddlSelTab_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
        #endregion
        #region content methods



        private static string GetResourceString(string resourceFile, string resourceName, string resourceDefaultValue)
        {
            var theText = Localization.GetString(resourceName, resourceFile);
            if (string.IsNullOrEmpty(theText)) theText = resourceDefaultValue;
            return theText;
        }

        private void LocalizeControls()
        {
            try
            {
                lblHeader.Text = GetResourceString(LocalResourceFile, "Header.Text", "News Article Friendly Url Provider Settings");
                _articleUrlSourceText = GetResourceString(LocalResourceFile, "ArticleUrlSource.Text", "Source of Article Url");
                _articleUrlStyleText = GetResourceString(LocalResourceFile, "ArticleUrlStyle.Text", "Style of Article Url");
                _categoryUrlStyleText = GetResourceString(LocalResourceFile, "CategoryUrlStyle.Text", "Style of Category Url");
                _authorUrlStyleText = GetResourceString(LocalResourceFile, "AuthorUrlStyle.Text", "Style of Author Url");
                _pageUrlStyleText = GetResourceString(LocalResourceFile, "PageUrlStyle.Text", "Style of Page Url");
                _noDnnPagePathText = GetResourceString(LocalResourceFile, "NoDnnPagePath.Text", "Don't show Page name for Urls on this page");
                _needTabSpecifiedText = GetResourceString(LocalResourceFile, "NeedTabSpecified.Text", "Cannot set 'No Path' value for All Tabs");
            }
            catch (Exception)
            {

                //suppress exception to let page load
            }

        }
        #endregion
        #region IProviderSettings Members
        /// <summary>
        /// LoadSettings is called when the module control is first loaded onto the page
        /// </summary>
        /// <remarks>
        /// This method shoudl read all the custom properties of the provider and set the controls
        /// of the page to reflect the current settings of the provider.
        /// </remarks>
        /// <param name="provider"></param>
        /// 

        public void LoadSettings()
        {
            if (_provider != null && !IsPostBack)
            {
                //build list of controls
                if (!IsPostBack)
                    LocalizeControls();

                _articleUrlStyleText = "";
                _articleUrlSourceText = "";
                _categoryUrlStyleText = "";
                _pageUrlStyleText = "";
                _authorUrlStyleText = "";
                _noDnnPagePathText = "";
                _noDnnPagePathTabId = int.Parse(_provider.Settings["NoDnnPagePathTabId"]);
                _needTabSpecifiedText = "";

                List<int> tabIds = new List<int>();
                if (_provider.TabIds.Count > 0)
                {
                    tabIds.AddRange(_provider.TabIds);
                }
                else
                    if (_provider.AllTabs)
                    tabIds.Add(-1);
                //bind to tab repeater
                rptTabs.DataSource = tabIds;
                rptTabs.DataBind();
                //Starting article id
                txtStartingArticleId.Text = _provider.Settings["StartingArticleId"];
                chkRedirectOldUrls.Checked = Convert.ToBoolean(_provider.Settings["RedirectUrls"]);
            }
        }


        private void TabToScreen(RepeaterItem item)
        {
            TabController tc = new TabController();
            int tabId = (int)item.DataItem;
            string tabName = "";
            if (tabId > -1)
            {
                //TabInfo tab = tc.GetTab(tabId);
                TabInfo tab = tc.GetTab(tabId, PortalId);
                tabName = tab.TabName;
            }
            else
            {
                tabName = GetResourceString(this.LocalResourceFile, "AllTabsName.Text", "All Tabs");
            }
            Label lblPageName = (Label)item.FindControl("lblPageName");
            lblPageName.Text = tabName;

            //now get the individual settings
            Hashtable articleUrlStyles = TabUrlOptions.GetHashTableFromSetting(_providerSettings["articleUrlStyle"]);
            Hashtable articleUrlSource = TabUrlOptions.GetHashTableFromSetting(_providerSettings["articleUrlSource"]);
            Hashtable pageUrlStyles = TabUrlOptions.GetHashTableFromSetting(_providerSettings["pageUrlStyle"]);
            Hashtable authorUrlStyles = TabUrlOptions.GetHashTableFromSetting(_providerSettings["authorUrlStyle"]);
            Hashtable categoryUrlStyles = TabUrlOptions.GetHashTableFromSetting(_providerSettings["categoryUrlStyle"]);
            //build options from lists
            TabUrlOptions tabOptions = new TabUrlOptions(tabId, -1, articleUrlStyles, articleUrlSource, pageUrlStyles, authorUrlStyles, categoryUrlStyles);
            //now get controls
            HiddenField hdnTabId = (HiddenField)item.FindControl("hdnTabId");
            hdnTabId.Value = tabId.ToString();
            DropDownList ddlArticleUrlStyle = (DropDownList)item.FindControl("ddlArticleUrlStyle");
            DropDownList ddlArticleUrlSource = (DropDownList)item.FindControl("ddlArticleUrlSource");
            DropDownList ddlCategoryUrlStyle = (DropDownList)item.FindControl("ddlCategoryUrlStyle");
            DropDownList ddlAuthorUrlStyle = (DropDownList)item.FindControl("ddlAuthorUrlStyle");
            DropDownList ddlPageUrlStyle = (DropDownList)item.FindControl("ddlPageUrlStyle");
            CheckBox chkNoDnnPagePath = (CheckBox)item.FindControl("chkNoDnnPagePath");
            BuildDropDownList(ddlArticleUrlStyle, tabOptions.ArticleStyle, typeof(ArticleUrlStyle));
            BuildDropDownList(ddlArticleUrlSource, tabOptions.ArticleSource, typeof(ArticleUrlSource));
            BuildDropDownList(ddlCategoryUrlStyle, tabOptions.CategoryStyle, typeof(CategoryUrlStyle));
            BuildDropDownList(ddlAuthorUrlStyle, tabOptions.AuthorStyle, typeof(AuthorUrlStyle));
            BuildDropDownList(ddlPageUrlStyle, tabOptions.PageStyle, typeof(PageUrlStyle));
            Label lblArticleUrlStyle = (Label)item.FindControl("lblArticleUrlStyle");
            Label lblArticleUrlSource = (Label)item.FindControl("lblArticleUrlSource");
            Label lblCategoryUrlStyle = (Label)item.FindControl("lblCategoryUrlStyle");
            Label lblAuthorUrlStyle = (Label)item.FindControl("lblAuthorUrlStyle");
            Label lblPageUrlStyle = (Label)item.FindControl("lblPageUrlStyle");
            Label lblNoDnnPagePath = (Label)item.FindControl("lblNoDnnPagePath");
            if (tabId > -1)
            {
                chkNoDnnPagePath.Checked = (_noDnnPagePathTabId == tabId);
                lblNoDnnPagePath.Text = _noDnnPagePathText;
            }
            else
            {
                chkNoDnnPagePath.Visible = false;
                lblNoDnnPagePath.Text = _needTabSpecifiedText;
            }
            lblArticleUrlStyle.Text = _articleUrlStyleText;
            lblArticleUrlSource.Text = _articleUrlSourceText;
            lblCategoryUrlStyle.Text = _categoryUrlStyleText;
            lblAuthorUrlStyle.Text = _authorUrlStyleText;
            lblPageUrlStyle.Text = _pageUrlStyleText;
        }

        private void BuildDropDownList(DropDownList ddlList, Enum setValue, Type enumType)
        {
            //loop iteration and build drop down list
            foreach (string name in Enum.GetNames(enumType))
            {
                string fullName = GetResourceString(LocalResourceFile, name + ".Text", name);
                ListItem item = new ListItem(fullName, name);
                if (name == setValue.ToString())
                    item.Selected = true;
                ddlList.Items.Add(item);
            }
        }

        /// <summary>
        /// UpdateSettings is called when the 'update' button is clicked on the interface.
        /// This should take any values from the page, and set the individual properties on the 
        /// instance of the module provider.
        /// </summary>
        /// <param name="provider"></param>
        public Dictionary<string, string> UpdateSettings()
        {
            //check type safety before cast
            if (_provider.ProviderType == Convert.ToString(typeof(NewsArticlesFriendlyUrlProvider)))
            {
                //take values from the page and set values on provider    
                _providerSettings = _provider.Settings;

                //starting articleId
                int startingArticleId = -1;
                if (int.TryParse(txtStartingArticleId.Text, out startingArticleId))
                {
                    if (startingArticleId > -1)
                        _provider.Settings["StartingArticleId"] = startingArticleId.ToString();
                }
                _provider.Settings["RedirectUrls"] = chkRedirectOldUrls.Checked.ToString();

                //now get the individual settings from the provider, as they are now
                Hashtable articleUrlStyles = TabUrlOptions.GetHashTableFromSetting(_providerSettings["articleUrlStyle"]);
                Hashtable articleUrlSource = TabUrlOptions.GetHashTableFromSetting(_providerSettings["articleUrlSource"]);
                Hashtable pageUrlStyles = TabUrlOptions.GetHashTableFromSetting(_providerSettings["pageUrlStyle"]);
                Hashtable authorUrlStyles = TabUrlOptions.GetHashTableFromSetting(_providerSettings["authorUrlStyle"]);
                Hashtable categoryUrlStyles = TabUrlOptions.GetHashTableFromSetting(_providerSettings["categoryUrlStyle"]);

                //build a super-list of hashtables, where there is an entry for each setting
                Hashtable allHashtables = new Hashtable();
                allHashtables.Add("articleUrlStyle", articleUrlStyles);
                allHashtables.Add("articleUrlSource", articleUrlSource);
                allHashtables.Add("pageUrlStyle", pageUrlStyles);
                allHashtables.Add("authorUrlStyle", authorUrlStyles);
                allHashtables.Add("categoryUrlStyle", categoryUrlStyles);
                //in case there is any leftover settings from tabs that have been removed.
                RemoveInactiveTabIds(_provider.TabIds, _provider.AllTabs, allHashtables);
                //934 : reset the noDnnPagePath 
                _provider.Settings["NoDnnPagePathTabId"] = Convert.ToString(-1); //will be set in 'TabFromScreen' if still set
                //get the valeus for each tab
                foreach (RepeaterItem item in rptTabs.Items)
                {
                    TabFromScreen(_provider, item, allHashtables);
                }

                //now feed back the hashtables back into the actual attributes for the provider
                SetProviderProperty(_provider, allHashtables, "ArticleUrlStyle", "articleUrlStyle");
                SetProviderProperty(_provider, allHashtables, "ArticleUrlSource", "articleUrlSource");
                SetProviderProperty(_provider, allHashtables, "PageUrlStyle", "pageUrlStyle");
                SetProviderProperty(_provider, allHashtables, "CategoryUrlStyle", "categoryUrlStyle");
                SetProviderProperty(_provider, allHashtables, "AuthorUrlStyle", "authorUrlStyle");
           
                
            }
            return _providerSettings;
        }


        private void RemoveInactiveTabIds(List<int> activeTabIds, bool allTabs, Hashtable allHashtables)
        {
            foreach (string settingName in allHashtables.Keys)
            {
                //get the setting hashtable
                Hashtable settingTable = (Hashtable)allHashtables[settingName];
                List<string> inactiveTabs = new List<string>();
                //walk through the list of tabs in the settings, and see if they are in the active list
                foreach (string tabIdRaw in settingTable.Keys)
                {
                    int tabId = 0;
                    if (int.TryParse(tabIdRaw, out tabId))
                    {
                        if (activeTabIds.Contains(tabId) == false)//this tab isn't in the list of active tabs
                            inactiveTabs.Add(tabIdRaw);
                    }
                }
                //now remove the entries
                foreach (string inactiveTabId in inactiveTabs)
                {
                    settingTable.Remove(inactiveTabId);
                }
            }

        }

        private void SetProviderProperty(NewsArticlesFriendlyUrlProviderInfo provider, Hashtable allHashtables, string providerPropertyName, string attributeName)
        {
            //use reflection to get the property
            System.Reflection.PropertyInfo settingProperty = provider.GetType().GetProperty(providerPropertyName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (settingProperty != null && settingProperty.CanWrite)
            {
                //get hash table from list of all hashtables for all settings
                Hashtable settingHashTable = (Hashtable)allHashtables[attributeName];
                //get delimited string of all contents of setting hash table
                string settingValue = GetDelimitedString(settingHashTable, ",", ";");
                //set provider property with hashtable values
                settingProperty.SetValue(provider, settingValue, null);
            }
        }

        private string GetDelimitedString(Hashtable settingHashTable, string colDelim, string rowDelim)
        {
            string result = "";
            if (settingHashTable != null)
            {
                foreach (object key in settingHashTable.Keys)
                {
                    object value = settingHashTable[key];
                    result += key.ToString() + colDelim + value.ToString() + rowDelim;
                }
            }
            return result;
        }

        private void TabFromScreen(NewsArticlesFriendlyUrlProviderInfo provider, RepeaterItem item, Hashtable allHashTables)
        {
            if (item != null)
            {
                HiddenField hdnTabid = (HiddenField)item.FindControl("hdnTabId");
                if (hdnTabid != null)
                {
                    string tabIdStr = hdnTabid.Value;
                    //get the values from the drop downs
                    SetHashTableFromDropDownValue(tabIdStr, item, allHashTables, "articleUrlStyle", "ddlArticleUrlStyle");
                    SetHashTableFromDropDownValue(tabIdStr, item, allHashTables, "articleUrlSource", "ddlArticleUrlSource");
                    SetHashTableFromDropDownValue(tabIdStr, item, allHashTables, "categoryUrlStyle", "ddlCategoryUrlStyle");
                    SetHashTableFromDropDownValue(tabIdStr, item, allHashTables, "authorUrlStyle", "ddlAuthorUrlStyle");
                    SetHashTableFromDropDownValue(tabIdStr, item, allHashTables, "pageUrlStyle", "ddlPageUrlStyle");
                    int tabIdInt = -1;
                    if (int.TryParse(tabIdStr, out tabIdInt))
                    {
                        if (tabIdInt > -1)
                        {
                            //check if tabis marked as the noDnnPagePath
                            CheckBox chkNoDnnPagePath = (CheckBox)item.FindControl("chkNoDnnPagePath");
                            if (chkNoDnnPagePath != null && chkNoDnnPagePath.Checked)
                                provider.Settings["NoDnnPagePathTabId"] = tabIdInt.ToString();
                        }
                    }
                }
            }
        }

        private void SetHashTableFromDropDownValue(string tabId, RepeaterItem item, Hashtable allHashTables, string attributeName, string ddlId)
        {
            Hashtable tabsSetting = (Hashtable)allHashTables[attributeName];
            DropDownList dropDownList = (DropDownList)item.FindControl(ddlId);
            if (dropDownList.SelectedIndex > -1 && tabsSetting != null)
            {
                if (tabsSetting.ContainsKey(tabId))
                    tabsSetting[tabId] = dropDownList.SelectedValue;
                else
                    tabsSetting.Add(tabId, dropDownList.SelectedValue);
            }
        }

        #endregion

        void IExtensionUrlProviderSettingsControl.LoadSettings()
        {
            LoadSettings();
        }

        ExtensionUrlProviderInfo IExtensionUrlProviderSettingsControl.Provider
        {
            get
            {
                return (ExtensionUrlProviderInfo)_provider;
            }
            set
            {
                if (value.GetType() == typeof(NewsArticlesFriendlyUrlProviderInfo))
                {
                    _provider = (NewsArticlesFriendlyUrlProviderInfo)value;
                }
            }
        }

        Dictionary<string, string> IExtensionUrlProviderSettingsControl.SaveSettings()
        {
            return UpdateSettings();
        }
    }

}

