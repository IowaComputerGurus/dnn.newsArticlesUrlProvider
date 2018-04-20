using System;
using System.Collections;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Web;
using System.Text;
using DotNetNuke.Common.Utilities;
using DotNetNuke.Entities.Urls;
using System.Text.RegularExpressions;
using DNN.Modules.NewsArticlesFriendlyUrlProvider.Entities;

namespace DNN.Modules.NewsArticlesFriendlyUrlProvider
{
    internal static class UrlController
    {
        //keys used for cache entries for Urls and Querystrings
        private const string FriendlyUrlIndexKey = "iFinity_NewsArticles_Urls_Tab{0}";
        private const string QueryStringIndexKey = "iFinity_NewsArticles_QueryString_Tab{0}";

        /// <summary>
        /// Checks for, and adds to the indexes, a missing item.
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="tabId"></param>
        /// <param name="portalId"></param>
        /// <param name="provider"></param>
        /// <param name="options"></param>
        /// <param name="messages"></param>
        /// <returns>Valid path if found</returns>
        internal static string CheckForMissingNewsArticleItem(int itemId, string itemType, int tabId, int portalId, NewsArticlesFriendlyUrlProvider provider, FriendlyUrlOptions options, TabUrlOptions urlOptions, ref List<string> messages)
        {
            string path = null;
            FriendlyUrlInfo friendlyUrl = Data.DataProvider.Instance().GetNewsArticleItem(itemId, itemType, urlOptions, tabId);
            messages.Add("articleId not found : " + itemId.ToString() + " Checking Item directly");
            if (friendlyUrl != null)
            {
                messages.Add("articleId found : " + itemId.ToString() + " Rebuilding indexes");
                //call and get the path
                path = UrlController.MakeItemFriendlyUrl(friendlyUrl, provider, options, urlOptions);
                //so this entry did exist but wasn't in the index.  Rebuild the index
                UrlController.RebuildIndexes(tabId, portalId, provider, options, urlOptions);
            }
            return path;
        }

        /// <summary>
        /// Creates a Friendly Url For the Item
        /// </summary>
        /// <param name="friendlyUrl">Object containing the relevant properties to create a friendly url from</param>
        /// <param name="provider">The active module provider</param>
        /// <param name="options">THe current friendly Url Options</param>
        /// <returns></returns>
        private static string MakeItemFriendlyUrl(FriendlyUrlInfo friendlyUrl, NewsArticlesFriendlyUrlProvider provider, FriendlyUrlOptions options, TabUrlOptions urlOptions)
        {
            //calls back up the module provider to utilise the CleanNameForUrl method, which creates a safe Url using the current Url Master options.
            string friendlyUrlPath = provider.CleanNameForUrl(friendlyUrl.urlFragment, options);
            switch (friendlyUrl.itemType.ToLower())
            {
                case "article":
                case "page":
                    switch (urlOptions.ArticleStyle)
                    {
                        case ArticleUrlStyle.BlogStyle:
                            friendlyUrlPath = MakeBlogStyleUrl(friendlyUrl.urlDate, friendlyUrlPath, friendlyUrl.urlNum, DateUrlStyle.Day);
                            break;
                        case ArticleUrlStyle.TitleStyle:
                            if (friendlyUrl.urlNum > 1)//page number is whole index
                                friendlyUrlPath += "/p/" + friendlyUrl.urlNum.ToString();
                            break;
                    }
                    /*if (friendlyUrl.urlNum > 1)//page number is whole index, so add on page
                        friendlyUrlPath += "/p/" + friendlyUrl.urlNum.ToString();*/
                    break;
                case "author":
                    //no difference - authorStyle chosen at index build stage
                    break;

                case "category":
                    //no difference - category style chosen at index build stage
                    break;
                case "archive":
                    //holds dates in format of yyyy/mm, which is how it is returned from db
                    break;
            }
            return friendlyUrlPath;
        }

        private static string MakeBlogStyleUrl(DateTime blogDate, string friendlyUrlPath, int page, DateUrlStyle dateUrlStyle)
        {
            //blog style is /2011/01/11/article-name-here
            string year = blogDate.Year.ToString("0000");
            string month = blogDate.Month.ToString("00");
            string day = blogDate.Day.ToString("00");
            string path = "";
            switch (dateUrlStyle)
            {
                case DateUrlStyle.Year:
                    path = year;
                    break;
                case DateUrlStyle.Month:
                    path = year + "/" + month;
                    break;
                case DateUrlStyle.Day:
                    path = year + "/" + month + "/" + day;
                    break;
            }
            path += "/" + friendlyUrlPath;
            if (page > 1)
            {
                path += "/p" + page.ToString();
            }
            return path;
        }
        /// <summary>
        /// Returns a friendly url index from the cache or database.
        /// </summary>
        /// <param name="tabId"></param>
        /// <param name="portalId"></param>
        /// <param name="NewsArticlesFriendlyUrlProvider"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        internal static Hashtable GetFriendlyUrlIndex(int tabId, int portalId, NewsArticlesFriendlyUrlProvider provider, FriendlyUrlOptions options, TabUrlOptions urlOptions)
        {
            string furlCacheKey = GetFriendlyUrlIndexKeyForTab(tabId);
            Hashtable friendlyUrlIndex = DataCache.GetCache<Hashtable>(furlCacheKey);
            if (friendlyUrlIndex == null)
            {
                string qsCacheKey = GetQueryStringIndexCacheKeyForTab(tabId);
                Hashtable queryStringIndex = null;
                //build index for tab
                BuildUrlIndexes(tabId, portalId, provider, options, urlOptions, out friendlyUrlIndex, out queryStringIndex);
                StoreIndexes(friendlyUrlIndex, furlCacheKey, queryStringIndex, qsCacheKey);
            }
            return friendlyUrlIndex;

        }
        /// <summary>
        /// Return the index of all the querystrings that belong to friendly urls for the specific tab.
        /// </summary>
        /// <param name="tabId"></param>
        /// <param name="provider"></param>
        /// <returns></returns>
        internal static Hashtable GetQueryStringIndex(int tabId, int portalId, NewsArticlesFriendlyUrlProvider provider, FriendlyUrlOptions options, TabUrlOptions urlOptions, bool forceRebuild)
        {
            string qsCacheKey = GetQueryStringIndexCacheKeyForTab(tabId);
            Hashtable queryStringIndex = DataCache.GetCache<Hashtable>(qsCacheKey);
            if (queryStringIndex == null || forceRebuild)
            {
                string furlCacheKey = GetFriendlyUrlIndexKeyForTab(tabId);
                Hashtable friendlyUrlIndex = null;
                //build index for tab
                BuildUrlIndexes(tabId, portalId, provider, options, urlOptions, out friendlyUrlIndex, out queryStringIndex);
                StoreIndexes(friendlyUrlIndex, furlCacheKey, queryStringIndex, qsCacheKey);
            }
            return queryStringIndex;
        }

        private static void BuildUrlIndexes(int tabId, int portalId, NewsArticlesFriendlyUrlProvider provider, FriendlyUrlOptions options, TabUrlOptions urlOptions, out Hashtable friendlyUrlIndex, out Hashtable queryStringIndex)
        {
            friendlyUrlIndex = new Hashtable();
            queryStringIndex = new Hashtable();
            //call database procedure to get list of 
            FriendlyUrlInfoCol itemUrls = null;
            NewsArticleOptions naOptions = null; //list of module settings for the NA module
            if (tabId > 0 && portalId > -1) //927 : don't call db for tabid -1, it doesn't exist
            {
                Data.DataProvider.Instance().GetNewsArticlesItemsForTab(tabId, urlOptions, out itemUrls, out naOptions);
                options.PunctuationReplacement = naOptions.TitleReplacement;//override value with NA setting
                Dictionary<string, string> categoryParents = new Dictionary<string, string>();
                if (itemUrls != null)
                {
                    //build up the dictionary
                    foreach (FriendlyUrlInfo itemUrl in itemUrls)
                    {

                        string furlKey = itemUrl.FUrlKey;

                        //querystring index - look up by url, find querystring for the item
                        string furlValue = MakeItemFriendlyUrl(itemUrl, provider, options, urlOptions);
                        string qsKey = furlValue.ToLower();//the querystring lookup is the friendly Url value - but converted to lower case

                        string qsValue = null;
                        string itemId = itemUrl.itemId.ToString();
                        string parentId = itemUrl.parentId.ToString();
                        switch (itemUrl.itemType.ToLower())
                        {
                            case "article":
                                qsValue = "&articleType=ArticleView&articleId=" + itemId;//the querystring is just the entryId parameter
                                break;
                            case "page":
                                qsValue = "&articleType=ArticleView&pageId=" + itemId + "&articleId=" + parentId;
                                break;
                            case "author":
                                qsValue = "&articleType=AuthorView&authorId=" + itemId;
                                break;
                            case "category":
                                qsValue = "&articleType=CategoryView&categoryId=" + itemId;
                                if (parentId != "-1" && urlOptions.CategoryStyle == CategoryUrlStyle.CatHierarchy)
                                {
                                    //this category has a parent
                                    categoryParents.Add(furlKey, itemUrl.FUrlPrefix + parentId);
                                }
                                break;
                            case "archive":
                                if (parentId == "-1")
                                {
                                    //yearly
                                    qsValue = "&articleType=ArchiveView&year=" + itemId;
                                }
                                else
                                {
                                    //monthly
                                    qsValue = "&articleType=ArchiveView&year=" + parentId + "&month=" + itemUrl.urlNum.ToString();//url num holds the month
                                }
                                break;
                        }


                        //when not including the dnn page path into the friendly Url, then include the tabid in the querystring
                        if (provider.AlwaysUsesDnnPagePath(portalId) == false)
                            qsValue = "?TabId=" + tabId.ToString() + qsValue;

                        string suffix = "";
                        AddUniqueUrlToIndex(furlKey, ref qsKey, qsValue, portalId, queryStringIndex, options, true, out suffix);

                        //if the suffix for the qsKey was changed, we need to add it to the friendly url used for the friendly url index
                        furlValue += suffix;

                        //friendly url index - look up by entryid, find Url
                        //check to see if friendly url matches any page paths
                        if (friendlyUrlIndex.ContainsKey(furlKey) == false)//shouldn't return duplicate because content is controlled by module logic
                            friendlyUrlIndex.Add(furlKey, furlValue);

                        //if the options aren't standard, also add in some other versions that will identify the right entry but will get redirected
                        if (options.PunctuationReplacement != "")
                        {
                            FriendlyUrlOptions altOptions = options.Clone();
                            altOptions.PunctuationReplacement = "";//how the urls look with no replacement
                            string altQsKey = MakeItemFriendlyUrl(itemUrl, provider, altOptions, urlOptions).ToLower();//keys are always lowercase
                            string altQsValue = qsValue + "&do301=true&&rr=Title_Space_Replacement";
                            AddUniqueUrlToIndex(furlKey, ref altQsKey, altQsValue, portalId, queryStringIndex, options, false, out suffix);
                        }
                        //now build the alternative for the redirects
                        if (urlOptions.RedirectOtherStyles)
                        {
                            TabUrlOptions tempOptions = urlOptions.Clone();
                            if (urlOptions.ArticleStyle == ArticleUrlStyle.BlogStyle)
                                tempOptions.ArticleStyle = ArticleUrlStyle.TitleStyle;
                            else
                                tempOptions.ArticleStyle = ArticleUrlStyle.BlogStyle;
                            //get the Url for this alternate style
                            string altArtQsKey = MakeItemFriendlyUrl(itemUrl, provider, options, tempOptions).ToLower();
                            string altArtQsValue = qsValue += "&do301=true&rr=Wrong_Article_Style";
                            AddUniqueUrlToIndex(furlKey, ref altArtQsKey, altArtQsValue, portalId, queryStringIndex, options, false, out suffix);
                        }

                    }
                    //go back and recursively check for category parents to be updated
                    if (categoryParents != null && categoryParents.Count > 0)
                    {
                        Dictionary<string, string> updates = new Dictionary<string, string>();
                        //reallocate the friendly urls recursively so that categories include their parent path
                        foreach (string furlKey in categoryParents.Keys)
                        {
                            //got the key for the friendly url
                            //now find the parent
                            string parentKey = categoryParents[furlKey];
                            string childPath = (string)friendlyUrlIndex[furlKey];
                            string parentPath = GetParentPath(furlKey, parentKey, childPath, ref categoryParents, ref friendlyUrlIndex);
                            if (parentPath != null)
                                childPath = parentPath + "/" + childPath;
                            updates.Add(furlKey, childPath);//don't update until all done
                        }
                        //now process the update list and update any values that had hierarchial categories
                        foreach (string key in updates.Keys)
                        {
                            string oldVal = (string)friendlyUrlIndex[key];
                            string qsKey = oldVal.ToLower();
                            if (queryStringIndex.ContainsKey(qsKey))
                            {
                                //update the querystring index
                                string qsVal = (string)queryStringIndex[qsKey];
                                queryStringIndex.Remove(qsKey);
                                queryStringIndex.Add(updates[key].ToLower(), qsVal);
                            }
                            //update the new friendly url index
                            friendlyUrlIndex[key] = updates[key];
                        }
                    }
                }
            }
        }

        private static void AddUniqueUrlToIndex(string furlKey, ref string qsKey, string qsValue, int portalId, Hashtable queryStringIndex, FriendlyUrlOptions options, bool addSuffixIfDuplicateFound, out string suffix)
        {
            DotNetNuke.Entities.Tabs.TabController tc = new DotNetNuke.Entities.Tabs.TabController();
            bool duplicate = false;
            suffix = "";//can hold a de-duplicating suffix
            int suffixCounter = 1;
            bool furlKeyUsed = false;
            do
            {
                duplicate = false;//always start in the assumption that this is not a duplicate
                DotNetNuke.Entities.Tabs.TabInfo matchingTab = tc.GetTabByName(qsKey, portalId);
                if (matchingTab != null)
                    duplicate = true;
                else
                    if (portalId >= 0)
                {
                    matchingTab = tc.GetTabByName(qsKey, -1);//host tabs
                    if (matchingTab != null)
                        duplicate = true;
                }

                if (duplicate == false)
                {
                    //try and add to index
                    if (queryStringIndex.ContainsKey(qsKey) == false)
                        queryStringIndex.Add(qsKey, qsValue);
                    else
                        duplicate = true;
                }
                if (duplicate == true)
                {
                    if (furlKeyUsed == false)
                    {
                        furlKeyUsed = true;
                        suffix = options.PunctuationReplacement + furlKey;
                        qsKey += suffix;
                    }
                    else
                    {
                        suffix += suffixCounter.ToString();
                        qsKey += suffix;
                    }
                }
            }
            while (duplicate == true && addSuffixIfDuplicateFound == true);
        }
        private static string GetParentPath(string childKey, string parentKey, string childPath, ref Dictionary<string, string> categoryParents, ref Hashtable friendlyUrlIndex)
        {
            //now lookup on that parent
            string parentPath = null;
            if (friendlyUrlIndex.ContainsKey(parentKey) && friendlyUrlIndex.ContainsKey(childKey))
            {
                parentPath = (string)friendlyUrlIndex[parentKey];
                //update base path by appending values together
                if (categoryParents.ContainsKey(parentKey))
                {
                    string grandParentKey = categoryParents[parentKey];
                    string grandParentPath = GetParentPath(parentKey, grandParentKey, parentPath, ref categoryParents, ref friendlyUrlIndex);
                    if (grandParentPath != null)
                        parentPath = grandParentPath + "/" + parentPath;
                }
            }
            return parentPath;
        }
        /// <summary>
        /// REbuilds the two indexes and re-stores them into the cache
        /// </summary>
        /// <param name="tabId"></param>
        /// <param name="portalId"></param>
        /// <param name="provider"></param>
        /// <param name="options"></param>
        private static void RebuildIndexes(int tabId, int portalId, NewsArticlesFriendlyUrlProvider provider, FriendlyUrlOptions options, TabUrlOptions urlOptions)
        {
            Hashtable queryStringIndex = null;
            Hashtable friendlyUrlIndex = null;
            string qsCacheKey = GetQueryStringIndexCacheKeyForTab(tabId);
            string furlCacheKey = GetFriendlyUrlIndexKeyForTab(tabId);
            //build index for tab
            BuildUrlIndexes(tabId, portalId, provider, options, urlOptions, out friendlyUrlIndex, out queryStringIndex);
            StoreIndexes(friendlyUrlIndex, furlCacheKey, queryStringIndex, qsCacheKey);
        }
        /// <summary>
        /// Store the two indexes into the cache
        /// </summary>
        /// <param name="friendlyUrlIndex"></param>
        /// <param name="friendlyUrlCacheKey"></param>
        /// <param name="queryStringIndex"></param>
        /// <param name="queryStringCacheKey"></param>
        private static void StoreIndexes(Hashtable friendlyUrlIndex, string friendlyUrlCacheKey, Hashtable queryStringIndex, string queryStringCacheKey)
        {
            TimeSpan expire = new TimeSpan(24, 0, 0);
            DataCache.SetCache(friendlyUrlCacheKey, friendlyUrlIndex, expire);
            DataCache.SetCache(queryStringCacheKey, queryStringIndex, expire);
        }

        /// <summary>
        /// Return the caceh key for a tab index
        /// </summary>
        /// <param name="tabId"></param>
        /// <returns></returns>
        private static string GetFriendlyUrlIndexKeyForTab(int tabId)
        {
            return string.Format(FriendlyUrlIndexKey, tabId);
        }
        private static string GetQueryStringIndexCacheKeyForTab(int tabId)
        {
            return string.Format(QueryStringIndexKey, tabId);
        }
        /// <summary>
        /// Creates a friendly article url, depending on the options
        /// </summary>
        /// <param name="provider"></param>
        /// <param name="articleUrlMatch"></param>
        /// <param name="articleUrlRegex"></param>
        /// <param name="friendlyUrlPath"></param>
        /// <param name="tab"></param>
        /// <param name="options"></param>
        /// <param name="urlOptions"></param>
        /// <param name="cultureCode"></param>
        /// <param name="endingPageName"></param>
        /// <param name="useDnnPagePath"></param>
        /// <param name="messages"></param>
        /// <param name="articleUrl"></param>
        /// <returns></returns>
        internal static bool MakeArticleUrl(NewsArticlesFriendlyUrlProvider provider, Match articleUrlMatch, Regex articleUrlRegex, string friendlyUrlPath, DotNetNuke.Entities.Tabs.TabInfo tab, FriendlyUrlOptions options, TabUrlOptions urlOptions, string cultureCode, ref string endingPageName, ref bool useDnnPagePath, ref List<string> messages, out string articleUrl)
        {
            bool result = false;
            articleUrl = null;
            //this is a url that looks like an article url.  We want to modify it and create the new one.
            string rawId = articleUrlMatch.Groups["artid"].Value;
            int articleId = 0;
            if (int.TryParse(rawId, out articleId) && (provider.StartingArticleId <= articleId))
            {
                Hashtable friendlyUrlIndex = null; //the friendly url index is the lookup we use
                //we have obtained the item Id out of the Url
                //get the friendlyUrlIndex (it comes from the database via the cache)
                friendlyUrlIndex = UrlController.GetFriendlyUrlIndex(tab.TabID, tab.PortalID, provider, options, urlOptions);
                if (friendlyUrlIndex != null)
                {
                    string furlkey = null; int pageId = -1;
                    //first check if we are seeking page or article
                    if (articleUrlMatch.Groups["pageid"].Success)
                    {
                        //page item urls are index by p + page id.  But we only use this if it is present
                        //ie pages override articles when both are present
                        string rawPageId = articleUrlMatch.Groups["pageid"].Value;
                        if (int.TryParse(rawPageId, out pageId))
                            furlkey = "p" + rawPageId;
                    }
                    else
                        //item urls are indexed with a + articleId ("a5") - this is so we could mix and match entities if necessary
                        furlkey = "a" + articleId.ToString();  //create the lookup key for the friendly url index

                    string path = (string)friendlyUrlIndex[furlkey];//check if in the index
                    if (path == null)
                    {
                        //don't normally expect to have a no-match with a friendly url path when an articleId was in the Url.
                        //could be that the page id is bunk - in that case, just use the article Id
                        if (furlkey.Contains("p"))
                        {
                            furlkey = "a" + articleId.ToString();  //create the lookup key for the friendly url index
                            path = (string)friendlyUrlIndex[furlkey];//check if in the index
                        }
                        if (path == null)
                        {
                            //could be a new item that has been created and isn't in the index
                            //do a direct call and find out if it's there
                            path = UrlController.CheckForMissingNewsArticleItem(articleId, "article", tab.TabID, tab.PortalID, provider, options, urlOptions, ref messages);
                        }
                    }
                    if (path != null) //got a valid path
                    {
                        //url found in the index for this entry.  So replace the matched part of the path with the friendly url
                        if (articleUrlMatch.Groups["l"].Success) //if the path had a leading /, then make sure to add that onto the replacement
                            path = provider.EnsureLeadingChar("/", path);

                        /* finish it all off */
                        messages.Add("Item Friendly Url Replacing : " + friendlyUrlPath + " in Path : " + path);

                        //this is the point where the Url is modified!
                        //replace the path in the path - which leaves any other parts of a path intact.
                        articleUrl = articleUrlRegex.Replace(friendlyUrlPath, path);//replace the part in the friendly Url path with it's replacement.

                        //check if this tab is the one specified to not use a path
                        if (provider.NoDnnPagePathTabId == tab.TabID)
                            useDnnPagePath = false;//make this Url relative from the site root

                        //set back to default.aspx so that Url Master removes it - just in case it wasn't standard
                        endingPageName = DotNetNuke.Common.Globals.glbDefaultPage;

                        result = true;
                    }
                }
            }
            return result;
        }

        internal static bool MakeAuthorUrl(NewsArticlesFriendlyUrlProvider provider, Match authorUrlMatch, Regex authorUrlRegex, string friendlyUrlPath, DotNetNuke.Entities.Tabs.TabInfo tab, FriendlyUrlOptions options, TabUrlOptions urlOptions, string cultureCode, ref string endingPageName, ref bool useDnnPagePath, ref List<string> messages, out string authorUrl)
        {
            bool result = false;
            authorUrl = null;
            //this is a url that looks like an author url.  We want to modify it and create the new one.
            string rawId = authorUrlMatch.Groups["authid"].Value;
            int authorId = 0;
            if (int.TryParse(rawId, out authorId))
            {
                Hashtable friendlyUrlIndex = null; //the friendly url index is the lookup we use
                //we have obtained the item Id out of the Url
                //get the friendlyUrlIndex (it comes from the database via the cache)
                friendlyUrlIndex = UrlController.GetFriendlyUrlIndex(tab.TabID, tab.PortalID, provider, options, urlOptions);
                if (friendlyUrlIndex != null)
                {
                    //item urls are indexed with a + user id ("u5") - this is so authors/articles/categories can be mixed and matched
                    string furlkey = "u" + authorId.ToString();  //create the lookup key for the friendly url index
                    string path = (string)friendlyUrlIndex[furlkey];//check if in the index
                    if (path == null)
                    {
                        //don't normally expect to have a no-match with a friendly url path when an authorId was in the Url.
                        //could be a new item that has been created and isn't in the index
                        //do a direct call and find out if it's there
                        //path = UrlController.CheckForMissingNewsauthorItem(authorId, "author", tab.TabID, tab.PortalID, provider, options, urlOptions, ref messages);
                    }
                    if (path != null) //got a valid path
                    {
                        //url found in the index for this entry.  So replace the matched part of the path with the friendly url
                        if (authorUrlMatch.Groups["l"].Success) //if the path had a leading /, then make sure to add that onto the replacement
                            path = provider.EnsureLeadingChar("/", path);

                        /* finish it all off */
                        messages.Add("Item Friendly Url Replacing : " + friendlyUrlPath + " in Path : " + path);

                        //this is the point where the Url is modified!
                        //replace the path in the path - which leaves any other parts of a path intact.
                        authorUrl = authorUrlRegex.Replace(friendlyUrlPath, path);//replace the part in the friendly Url path with it's replacement.

                        //check if this tab is the one specified to not use a path
                        if (provider.NoDnnPagePathTabId == tab.TabID)
                            useDnnPagePath = false;//make this Url relative from the site root

                        //set back to default.aspx so that Url Master removes it - just in case it wasn't standard
                        endingPageName = DotNetNuke.Common.Globals.glbDefaultPage;

                        result = true;
                    }
                }
            }
            return result;
        }

        internal static bool MakeCategoryUrl(NewsArticlesFriendlyUrlProvider provider, Match categoryUrlMatch, Regex categoryUrlRegex, string friendlyUrlPath, DotNetNuke.Entities.Tabs.TabInfo tab, FriendlyUrlOptions options, TabUrlOptions urlOptions, string cultureCode, ref string endingPageName, ref bool useDnnPagePath, ref List<string> messages, out string categoryUrl)
        {
            bool result = false;
            categoryUrl = null;
            //this is a url that looks like an category url.  We want to modify it and create the new one.
            string rawId = categoryUrlMatch.Groups["catid"].Value;
            int categoryId = 0;
            if (int.TryParse(rawId, out categoryId))
            {
                Hashtable friendlyUrlIndex = null; //the friendly url index is the lookup we use
                //we have obtained the item Id out of the Url
                //get the friendlyUrlIndex (it comes from the database via the cache)
                friendlyUrlIndex = UrlController.GetFriendlyUrlIndex(tab.TabID, tab.PortalID, provider, options, urlOptions);
                if (friendlyUrlIndex != null)
                {
                    //item urls are indexed with a + category id ("c5") - this is so authors/articles/categories can be mixed and matched
                    string furlkey = "c" + categoryId.ToString();  //create the lookup key for the friendly url index
                    string path = (string)friendlyUrlIndex[furlkey];//check if in the index
                    if (path == null)
                    {
                        //don't normally expect to have a no-match with a friendly url path when an categoryId was in the Url.
                        //could be a new item that has been created and isn't in the index
                        //do a direct call and find out if it's there
                        //path = UrlController.CheckForMissingNewscategoryItem(categoryId, "category", tab.TabID, tab.PortalID, provider, options, urlOptions, ref messages);
                    }
                    if (path != null) //got a valid path
                    {
                        //url found in the index for this entry.  So replace the matched part of the path with the friendly url
                        if (categoryUrlMatch.Groups["l"].Success) //if the path had a leading /, then make sure to add that onto the replacement
                            path = provider.EnsureLeadingChar("/", path);

                        /* finish it all off */
                        messages.Add("Item Friendly Url Replacing : " + friendlyUrlPath + " in Path : " + path);

                        //this is the point where the Url is modified!
                        //replace the path in the path - which leaves any other parts of a path intact.
                        categoryUrl = categoryUrlRegex.Replace(friendlyUrlPath, path);//replace the part in the friendly Url path with it's replacement.

                        //check if this tab is the one specified to not use a path
                        if (provider.NoDnnPagePathTabId == tab.TabID)
                            useDnnPagePath = false;//make this Url relative from the site root

                        //set back to default.aspx so that Url Master removes it - just in case it wasn't standard
                        endingPageName = DotNetNuke.Common.Globals.glbDefaultPage;

                        result = true;
                    }
                }
            }
            return result;
        }

        internal static bool MakeArchiveUrl(NewsArticlesFriendlyUrlProvider provider, Match archiveUrlMatch, Regex archiveUrlRegex, string friendlyUrlPath, DotNetNuke.Entities.Tabs.TabInfo tab, FriendlyUrlOptions options, TabUrlOptions urlOptions, string cultureCode, ref string endingPageName, ref bool useDnnPagePath, ref List<string> messages, out string archiveUrl)
        {
            archiveUrl = friendlyUrlPath;
            bool result = false;
            Group mthGrp = archiveUrlMatch.Groups["mth"];
            Group yrGrp = archiveUrlMatch.Groups["yr"];
            bool month = false, year = false;
            string mm = null, yyyy = null;
            string path = null;
            if (mthGrp != null && mthGrp.Success)
            {
                //contains a month
                month = true;
                mm = archiveUrlMatch.Groups["mm"].Value;
            }
            if (yrGrp != null && yrGrp.Success)
            {
                year = true;
                yyyy = archiveUrlMatch.Groups["yyyy"].Value;
            }
            if (year)
            {
                path = "/" + yyyy;
            }
            if (month)
                path += "/" + mm;

            if (path != null) //got a valid path
            {
                //have a valid url replacement for this url.  So replace the matched part of the path with the friendly url
                if (archiveUrlMatch.Groups["l"].Success) //if the path had a leading /, then make sure to add that onto the replacement
                    path = provider.EnsureLeadingChar("/", path);

                /* finish it all off */
                messages.Add("Item Friendly Url Replacing Archive Url : " + friendlyUrlPath + " with Path : " + path);

                //this is the point where the Url is modified!
                //replace the path in the path - which leaves any other parts of a path intact.
                archiveUrl = archiveUrlRegex.Replace(friendlyUrlPath, path);//replace the part in the friendly Url path with it's replacement.

                //check if this tab is the one specified to not use a path
                if (provider.NoDnnPagePathTabId == tab.TabID)
                    useDnnPagePath = false;//make this Url relative from the site root

                //set back to default.aspx so that Url Master removes it - just in case it wasn't standard
                endingPageName = DotNetNuke.Common.Globals.glbDefaultPage;
                //return success
                result = true;
            }
            return result;

        }
        internal static bool CheckForDebug(HttpRequest request, NameValueCollection queryStringCol, bool checkRequestParams)
        {
            string debugValue = ""; bool retVal = false;
            string debugToken = "_nadebug";
            if (queryStringCol != null && queryStringCol[debugToken] != null)
            {
                debugValue = queryStringCol[debugToken];
            }
            else
                if (checkRequestParams)
            {
                //798 : change reference to debug parameters
                if (request != null && request.Params != null)
                    debugValue = (request.Params.Get("HTTP_" + debugToken.ToUpper()));
                if (debugValue == null) debugValue = "false";
            }
            switch (debugValue.ToLower())
            {
                case "true":
                    retVal = true;
                    break;
            }
            return retVal;
        }


    }


}
