using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DNN.Modules.NewsArticlesFriendlyUrlProvider.Entities
{
    internal class NewsArticleOptions
    {
        //('TitleReplacementType','SEOShorternID','SEOUrlMode','AlwaysShowPageID')
        internal string TitleReplacement;
        internal string SeoShortenId;
        internal string SeoUrlMode;
        internal bool AlwaysShowPageId;
    }
}
