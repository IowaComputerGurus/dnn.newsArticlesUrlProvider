using System;
using System.Collections;

namespace DNN.Modules.NewsArticlesFriendlyUrlProvider.Entities
{
    internal enum ArticleUrlStyle
    {
        BlogStyle, TitleStyle
    }
    internal enum ArticleUrlSource
    {
        Title, MetaTitle, ShortUrl
    }
    internal enum PageUrlStyle
    {
        TitleAndNum, PageTitle
    }
    internal enum CategoryUrlStyle
    {
        CatName, CatHierarchy
    }
    internal enum AuthorUrlStyle
    {
        UserName, DisplayName
    }
    internal enum DateUrlStyle
    {
        Year, Month, Day
    }
    /// <summary>
    /// This class holds the options for each tab specified (or for all tabs)
    /// </summary>
    [Serializable()]
    internal class TabUrlOptions
    {
        int _tabId = -1;
        int _startingArticleId = -1;
        ArticleUrlStyle _articleStyle;
        ArticleUrlSource _articleSource;
        PageUrlStyle _pageStyle;
        AuthorUrlStyle _authorStyle;
        CategoryUrlStyle _categoryStyle;
        protected bool _redirectOtherStyles = false;
        #region public properties
        public int TabId
        {
            get { return _tabId; }
            set { _tabId = value; }
        }
        internal int StartingArticleId
        {
            get
            { return _startingArticleId; }
            set { _startingArticleId = value; }
        }
        internal bool RedirectOtherStyles
        {
            get { return _redirectOtherStyles; }
            set { _redirectOtherStyles = value; }
        }
        internal ArticleUrlStyle ArticleStyle
        {
            get { return _articleStyle; }
            set { _articleStyle = value; }
        }

        internal ArticleUrlSource ArticleSource
        {
            get { return _articleSource; }
            set { _articleSource = value; }
        }
        internal PageUrlStyle PageStyle
        {
            get { return _pageStyle; }
            set { _pageStyle = value; }
        }
        internal AuthorUrlStyle AuthorStyle
        {
            get { return _authorStyle; }
            set { _authorStyle = value; }
        }
        internal CategoryUrlStyle CategoryStyle
        {
            get { return _categoryStyle; }
            set { _categoryStyle = value; }
        }
        #endregion
        internal TabUrlOptions(int tabId, int startingArticleId, Hashtable articleStyle, Hashtable articleSource, Hashtable pageStyle, Hashtable authorStyle, Hashtable categoryStyle)
        {
            _tabId = tabId;
            _startingArticleId = startingArticleId;
            _articleStyle = (ArticleUrlStyle)EnumConversion.ConvertFromEnum(typeof(ArticleUrlStyle), GetTabSetting(articleStyle), ArticleUrlStyle.BlogStyle);
            _articleSource = (ArticleUrlSource)EnumConversion.ConvertFromEnum(typeof(ArticleUrlSource), GetTabSetting(articleSource), ArticleUrlSource.Title);
            _pageStyle = (PageUrlStyle)EnumConversion.ConvertFromEnum(typeof(PageUrlStyle), GetTabSetting(pageStyle), PageUrlStyle.PageTitle);
            _authorStyle = (AuthorUrlStyle)EnumConversion.ConvertFromEnum(typeof(AuthorUrlStyle), GetTabSetting(authorStyle), AuthorUrlStyle.DisplayName);
            _categoryStyle = (CategoryUrlStyle)EnumConversion.ConvertFromEnum(typeof(CategoryUrlStyle), GetTabSetting(categoryStyle), CategoryUrlStyle.CatName);
        }
        //private constructor for cloning
        internal TabUrlOptions()
        {
        }
        private string GetTabSetting(Hashtable settingList)
        {
            string result = null;
            string tabKey = _tabId.ToString();
            if (settingList != null && settingList.ContainsKey(tabKey))
            {
                result = (string)settingList[tabKey];
            }
            return result;
        }



        internal TabUrlOptions Clone()
        {
            TabUrlOptions clone = new TabUrlOptions();
            clone._articleSource = _articleSource;
            clone._articleStyle = _articleStyle;
            clone._authorStyle = _authorStyle;
            clone._categoryStyle = _categoryStyle;
            clone._pageStyle = _pageStyle;
            clone._tabId = _tabId;
            clone._redirectOtherStyles = _redirectOtherStyles;
            return clone;
        }
        internal static Hashtable GetHashTableFromSetting(string raw)
        {
            string rawResult;
            return GetHashTableFromSetting(raw, out rawResult);
        }
        internal static Hashtable GetHashTableFromSetting(string raw, out string rawResult)
        {
            Hashtable result = new Hashtable();
            rawResult = raw;
            if (raw != null && raw != "")
            {
                string[] pairs = raw.Split(';');
                foreach (string pair in pairs)
                {
                    string key = null, val = null;
                    string[] vals = pair.Split(',');
                    if (vals.GetUpperBound(0) >= 0)
                        key = vals[0];
                    if (vals.GetUpperBound(0) >= 1)
                        val = vals[1];
                    if (key != null && val != null)
                        result.Add(key, val);
                }
            }
            return result;
        }
    }
    internal static class EnumConversion
    {
        internal static Enum ConvertFromEnum(Type enumType, string rawValue, Enum defaultValue)
        {
            if (rawValue != null)
            {
                Enum result = defaultValue;
                try
                {
                    object enumVal = Enum.Parse(enumType, rawValue);
                    result = (Enum)Convert.ChangeType(enumVal, enumType);
                }
                catch (Exception ex)
                {
                    //ignore because we can't implement Enum.TryParse until .NET 4
                }
                return result;
            }
            else
                return defaultValue;

        }
    }
}