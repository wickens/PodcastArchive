using System;
using System.Globalization;
using System.Xml;
using uk.org.wickens.podarchive.Utility;

namespace uk.org.wickens.podarchive.Model
{
	public class Feed
	{
		public Uri Url { get; set; }
		public string Title { get; set; }
		
		public Episode[] Episodes { get; set; }


        /// <summary>
        /// Create a new feed by supplying the URL
        /// </summary>
        /// <param name="url"></param>
		public Feed(string url)
        {
            this.Url = new Uri(url);
            XmlDocument xmlDoc = DownloadAndParseFeed();


            // Popualate feed level properties

            var titleNode = xmlDoc.SelectSingleNode("//rss/channel/title");
            if (titleNode != null)
            {
                this.Title = titleNode.InnerText.Trim();
            }
            else
            {
                this.Title = String.Empty;
            }

            // New empty list to keep our episodes in 
            List<Episode> episodes = new List<Episode>();

            // List the episodes in the XML
            var episodeNodes = xmlDoc.SelectNodes("//rss/channel/item");
            if (episodeNodes != null)
            {
                foreach (XmlNode episodeNode in episodeNodes)
                {
                    // For each episode, parse the title, date, url
                    if (episodeNode != null)
                    {
                        try
                        {
                            string title = episodeNode.SelectSingleNode("title").InnerText.Trim();
                            string dateStr = episodeNode.SelectSingleNode("pubDate").InnerText.Trim();
                            string episodeUrl = episodeNode.SelectSingleNode("enclosure").Attributes["url"].Value;
                            Episode episode = new Episode(this) { Title = title, Date = DateTimeHelper.ParseDateTimeFromFeed(dateStr), MediaUrl = new Uri(episodeUrl) };

                            // Add
                            episodes.Add(episode);
                        }
                        catch { } // Ignore any items that cannot be parsed. 
                    }
                }
            }

            // Reverse sort of list, so older episodes are first
            episodes.Reverse();

            // Finalise the list 
            this.Episodes = episodes.ToArray();
        }

        /// <summary>
        /// Gets a .NET XML document from an URL containing an XML string string
        /// </summary>
        /// <returns></returns>
        private XmlDocument DownloadAndParseFeed()
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(this.Url.ToString());
            return xmlDoc;
        }
    }
}

