/*
' Copyright (c) 2017 patapscoresearch.com
'  All rights reserved.
' 
' THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED
' TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL
' THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF
' CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
' DEALINGS IN THE SOFTWARE.
' 
*/

using System.Data;
using System;
using DotNetNuke.Common.Utilities;
using DotNetNuke.Framework.Providers;
using DNN.Modules.NewsArticlesFriendlyUrlProvider.Entities;
using System.Data.SqlClient;
using Microsoft.ApplicationBlocks.Data;

namespace DNN.Modules.NewsArticlesFriendlyUrlProvider.Data
{
    internal class SqlDataProvider : DataProvider
    {
        private const string ModuleQualifier = "nap_";
        private const string OwnerQualifier = "ifty_";

        #region Private Members
        private const string ProviderType = "data";
        private ProviderConfiguration _providerConfiguration = ProviderConfiguration.GetProviderConfiguration(ProviderType);
        private string _connectionString;
        private string _providerPath;
        private string _objectQualifier;
        private string _databaseOwner;
        #endregion
        #region Constructor
        /// <summary>
        /// Constructs new SqlDataProvider instance
        /// </summary>
        internal SqlDataProvider()
        {
            //Read the configuration specific information for this provider
            Provider objProvider = (Provider)_providerConfiguration.Providers[_providerConfiguration.DefaultProvider];

            //Read the attributes for this provider
            //Get Connection string from web.config
            _connectionString = Config.GetConnectionString();

            if (_connectionString.Length == 0)
            {
                // Use connection string specified in provider
                _connectionString = objProvider.Attributes["connectionString"];
            }

            _providerPath = objProvider.Attributes["providerPath"];

            //override the standard dotNetNuke qualifier with a iFinity one if it exists
            _objectQualifier = objProvider.Attributes["objectQualifier"];
            if ((_objectQualifier != "") && (_objectQualifier.EndsWith("_") == false))
            {
                _objectQualifier += "_";
            }
            else
                if (_objectQualifier == null) _objectQualifier = "";

            _objectQualifier += OwnerQualifier;

            _databaseOwner = objProvider.Attributes["databaseOwner"];
            if ((_databaseOwner != "") && (_databaseOwner.EndsWith(".") == false))
            {
                _databaseOwner += ".";
            }
        }
        #endregion
        #region Properties

        /// <summary>
        /// Gets and sets the connection string
        /// </summary>
        public string ConnectionString
        {
            get { return _connectionString; }
        }
        /// <summary>
        /// Gets and sets the Provider path
        /// </summary>
        public string ProviderPath
        {
            get { return _providerPath; }
        }
        /// <summary>
        /// Gets and sets the Object qualifier
        /// </summary>
        public string ObjectQualifier
        {
            get { return _objectQualifier; }
        }
        /// <summary>
        /// Gets and sets the database ownere
        /// </summary>
        public string DatabaseOwner
        {
            get { return _databaseOwner; }
        }

        #endregion
        #region private members
        /// -----------------------------------------------------------------------------
        /// <summary>
        /// Gets the fully qualified name of the stored procedure
        /// </summary>
        /// <param name="name">The name of the stored procedure</param>
        /// <returns>The fully qualified name</returns>
        /// -----------------------------------------------------------------------------
        private string GetFullyQualifiedName(string name)
        {
            return DatabaseOwner + ObjectQualifier + ModuleQualifier + name;
        }
        #endregion

        #region abstract overridden properties
        internal override void GetNewsArticlesItemsForTab(int tabId, TabUrlOptions urlOptions, out FriendlyUrlInfoCol urls, out NewsArticleOptions naOptions)
        {
            urls = new FriendlyUrlInfoCol();

            string sp = GetFullyQualifiedName("GetNewsArticlesEntriesForTab");
            SqlParameter[] parms = new SqlParameter[2];
            parms[0] = new SqlParameter("@TabId", tabId);
            if (urlOptions != null)
                parms[1] = new SqlParameter("@startingArticleId", urlOptions.StartingArticleId);
            else
                parms[1] = new SqlParameter("@startingArticleId", 0);
            //call the db
            SqlDataReader rdr = SqlHelper.ExecuteReader(_connectionString, CommandType.StoredProcedure, sp, parms);

            //work out which url fragment to use
            string articleFragmentName = "";
            string pageFragmentName = "";
            string authorFragmentName = "";
            GetFragmentNames(urlOptions, out articleFragmentName, out pageFragmentName, out authorFragmentName);

            while (rdr.Read())
            {
                FriendlyUrlInfo fuf = new FriendlyUrlInfo();
                BindReaderToFriendlyUrl(ref fuf, rdr, pageFragmentName, articleFragmentName, authorFragmentName);
                urls.Add(fuf);
            }
            //get any options in the mix
            naOptions = new NewsArticleOptions();
            if (rdr.NextResult())
            {
                /*AlwaysShowPageID	False
                 SEOShorternID	ID
                 SEOUrlMode	Shorterned
                 TitleReplacementType	Dash*/
                //set defaults - module settings may not be there
                naOptions.TitleReplacement = "-";
                while (rdr.Read())
                {
                    string settingName = (string)rdr["SettingName"];
                    string settingValue = (string)rdr["SettingValue"];
                    if (settingName != null)
                    {
                        switch (settingName.ToLower())
                        {
                            case "seoshorternid":
                                naOptions.SeoShortenId = settingValue;
                                break;
                            case "seourlmode":
                                naOptions.SeoUrlMode = settingValue;
                                break;
                            case "titlereplacementtype":
                                if (settingValue.ToLower() == "dash")
                                    naOptions.TitleReplacement = "-";
                                else
                                    naOptions.TitleReplacement = "_";
                                break;
                            case "alwaysshowpageid":
                                bool result = false;
                                bool.TryParse(settingValue, out result);
                                naOptions.AlwaysShowPageId = result;
                                break;
                        }
                    }
                    else
                    {
                        //no value, use defaults
                        naOptions.TitleReplacement = "-";
                        naOptions.AlwaysShowPageId = false;

                    }
                }
            }
            rdr.Close();
            rdr.Dispose();
        }

        private void GetFragmentNames(TabUrlOptions urlOptions, out string articleFragmentName, out string pageFragmentName, out string authorFragmentName)
        {
            articleFragmentName = "UrlFragment1";
            pageFragmentName = "UrlFragment1";
            authorFragmentName = "UrlFragment1";
            if (urlOptions != null)
            {
                switch (urlOptions.ArticleSource)
                {
                    case ArticleUrlSource.ShortUrl:
                        articleFragmentName = "UrlFragment3";
                        break;
                    case ArticleUrlSource.MetaTitle:
                        articleFragmentName = "UrlFragment2";
                        break;
                    default:
                        articleFragmentName = "UrlFragment1";
                        break;
                }
                switch (urlOptions.PageStyle)
                {
                    case PageUrlStyle.TitleAndNum:
                        pageFragmentName = "UrlFragment2";
                        break;
                    default:
                        pageFragmentName = "UrlFragment1";
                        break;
                }

                switch (urlOptions.AuthorStyle)
                {
                    case AuthorUrlStyle.UserName:
                        authorFragmentName = "UrlFragment2";
                        break;
                    default:
                        authorFragmentName = "UrlFragment1";
                        break;
                }
            }

        }
        internal override FriendlyUrlInfo GetNewsArticleItem(int itemId, string itemType, TabUrlOptions urlOptions, int tabId)
        {
            FriendlyUrlInfo fuf = null;
            //[dnn_ifty_nap_GetNewsArticlesEntry]
            string sp = GetFullyQualifiedName("GetNewsArticlesEntry");
            SqlParameter[] parms = new SqlParameter[3];
            parms[0] = new SqlParameter("@TabId", tabId);
            parms[1] = new SqlParameter("@startingArticleId", urlOptions.StartingArticleId);
            parms[2] = new SqlParameter("@itemId", itemId);
            SqlDataReader rdr = SqlHelper.ExecuteReader(_connectionString, CommandType.StoredProcedure, sp, parms);

            //work out which url fragment to use
            string articleFragmentName = "";
            string pageFragmentName = "";
            string authorFragmentName = "";
            GetFragmentNames(urlOptions, out articleFragmentName, out pageFragmentName, out authorFragmentName);

            if (rdr.Read())
            {
                BindReaderToFriendlyUrl(ref fuf, rdr, articleFragmentName, pageFragmentName, authorFragmentName);
            }
            rdr.Close();
            rdr.Dispose();
            return fuf;
        }
        internal string GetSafeString(SqlDataReader rdr, string colName)
        {
            string result = "";
            if (colName != null && colName != "" && !Convert.IsDBNull(rdr[colName]))
                result = (string)rdr[colName];
            return result;
        }
        private void BindReaderToFriendlyUrl(ref FriendlyUrlInfo url, SqlDataReader rdr, string articleFragmentName, string pageFragmentName, string authorFragmentName)
        {
            if (url == null) url = new FriendlyUrlInfo();
            if (!Convert.IsDBNull(rdr["ItemId"]))
                url.itemId = (int)rdr["ItemId"];

            if (!Convert.IsDBNull(rdr["ParentId"]))
                url.parentId = (int)rdr["ParentId"];
            else
                url.parentId = -1;

            string itemType = "";
            if (!Convert.IsDBNull(rdr["ItemType"]))
                itemType = (string)rdr["ItemType"];

            //the date is a general purpose field that is context-dependent on the item type
            if (!Convert.IsDBNull(rdr["urlDate"]))
                url.urlDate = (DateTime)rdr["urlDate"];
            else
                url.urlDate = DateTime.MinValue;

            //the num is a general purpose field that is context-dependent on the item Type
            url.urlNum = (int)rdr["UrlNum"];

            url.itemType = itemType;
            //get the 3 different types of url fragment
            switch (itemType.ToLower())
            {
                case "article":
                    string fragment = GetSafeString(rdr, articleFragmentName);
                    if (fragment == "")//fallback if empty
                        fragment = GetSafeString(rdr, "UrlFragment1");
                    url.urlFragment = fragment;
                    break;
                case "author":
                    url.urlFragment = GetSafeString(rdr, authorFragmentName);
                    break;
                case "category":
                    url.urlFragment = GetSafeString(rdr, "UrlFragment1");
                    break;
                case "page":
                    string pageFragment = GetSafeString(rdr, pageFragmentName);
                    if (pageFragment == "")//fallback if empty
                        pageFragment = GetSafeString(rdr, "UrlFragment1");
                    url.urlFragment = pageFragment;
                    break;
            }
        }
        #endregion
    }
}
