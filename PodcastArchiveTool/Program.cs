using System;
using System.Globalization;
using System.Net;
using uk.org.wickens.podarchive.Model;
using uk.org.wickens.podarchive.Utility;
using CommandLine;

namespace uk.org.wickens.podarchive // Note: actual namespace depends on the project name.
{
    internal class Program
    {
        /// <summary>
        /// Commandline Options
        /// </summary>
        public class Options
        {
            [Option('u', "url", Required = true, HelpText = "The URL of the podcast RSS feed.")]
            public string? Url { get; set; }

            [Option('s', "start", Required = false, HelpText = "Start date. yyyy-MM-dd Only download episodes on or after this date.")]
            public string? StartDate { get; set; }

            [Option('e', "end", Required = false, HelpText = "End date. yyyy-MM-dd Only download episodes on or before this date.")]
            public string? EndDate { get; set; }

        }



        static async Task Main(string[] args)
        {

            // Get input parameters
            DateTime startDate = DateTime.MinValue;
            DateTime endDate = DateTime.MaxValue;
            Uri? feedUrl = null;

            // Get our commandline options
            Parser.Default.ParseArguments<Options>(args)
                         .WithParsed<Options>(o =>
                         {
                             // Get the URL
                             Uri.TryCreate(o.Url, UriKind.Absolute, out feedUrl);

                             if (!DateTime.TryParseExact(o.StartDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out startDate))
                             {
                                 if (o.StartDate is not null)
                                 {
                                     Console.WriteLine("Could not parse start date. Make sure you're using yyyy-mm-dd (e.g. 2001-01-01).");
                                 }
                             }


                             if (!DateTime.TryParseExact(o.EndDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out endDate))
                             {
                                 if (o.EndDate is not null)
                                 {
                                     Console.WriteLine("Could not parse end date. Make sure you're using yyyy-mm-dd (e.g. 2001-01-01).");
                                 }
                                 endDate = DateTime.MaxValue;
                             }

                         });

            if (feedUrl is null)
            {
                Console.WriteLine("Invalid URL.");
                return;
            }

            // Load the feed
            Feed f = new Feed(feedUrl.ToString());
            Console.WriteLine("Successfully loaded \"{0}\", {1} episodes.", f.Title, f.Episodes.Length);

            // Start downloading
            DownloadManager downloadManager = new DownloadManager(f.Episodes);

            await downloadManager.StartDownload(startDate, endDate);

            Console.WriteLine("Finished.");
        }
    }
}