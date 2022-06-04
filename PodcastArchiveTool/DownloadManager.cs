using System;
using System.Net;
using uk.org.wickens.podarchive.Model;
using uk.org.wickens.podarchive.Utility;

namespace uk.org.wickens.podarchive
{


    /// <summary>
    /// Manage the downloading of files.
    /// </summary>
    public class DownloadManager
    {
        public Episode[] Episodes { get; }

        public DownloadManager(Episode[] episodes)
        {
            this.Episodes = episodes;
        }



        /// <summary>
        /// Start downloading all of the episodes in the feed, between these two dates. 
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        public async Task StartDownload(DateTime? start = null, DateTime? end = null)
        {
            start = start ?? DateTime.MinValue;
            end = end ?? DateTime.MaxValue;

            List<Episode> filtered = new List<Episode>(GetEpisodeDateRange(this.Episodes, start.Value, end.Value));

            if (this.Episodes.Length == 0)
            {
                Console.WriteLine("The feed was empty.");
            }
            else if (filtered.Count == 0 && this.Episodes.Length > 0)
            {
                // Inform the user they filtered everything out
                Console.WriteLine("Date filter resulted in 0 matches.");
            }
            else if (filtered.Count == this.Episodes.Length)
            {
                Console.WriteLine("Downloading all episides.");
            }
            else
            {
                Console.WriteLine("Found {0} episodes matching your date fiter.", filtered.Count);
            }


            // Do the actual downloading

            int counter = 0;
            foreach (Episode episode in filtered)
            {
                // Print progress (I should really make this an event that's fired, so this class doesn't talk to the presentation layer, but 🤷‍♂️, maybe one day)
                counter++;
                Console.WriteLine("Downloading episode {0}/{1} - \"{2}\"", counter, filtered.Count, episode.Title);


                // Start the download (TODO: support multiple downloads at once)
                await this.DownloadEpisodeAsync(episode);


            }
        }



        /// <summary>
        /// Downloads an epsisode if it doesn't already exist on disk.
        /// </summary>
        /// <param name="episode"></param>
        /// <returns></returns>
        private async Task DownloadEpisodeAsync(Episode episode)
        {
            HttpClient client = new HttpClient();
            // Set the timeout to 1 hour
            client.Timeout = new TimeSpan(1, 0, 0);
            // Set the user agent to look like a web browser
            client.DefaultRequestHeaders.Add("User-Agent", Constants.USER_AGENT);

            try
            {
                // Create the directory structure based on the show name, date etc.
                string directory = this.GenerateDirectoryForEpisode(episode);
                // Generate a filename based on the episode title & dir path above
                string filename = GeneratePathForEpisode(episode, directory);

                // Find out if we've already downloaded this one...
                bool shouldDownload = await IsDownloadNeeded(episode, filename);

                if (shouldDownload)
                {
                    // No, we have no
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    // Start the HTTP request
                    HttpResponseMessage response = await client.GetAsync(episode.MediaUrl);
                    response.EnsureSuccessStatusCode();

                    // Create a file stream
                    using (var fs = new FileStream(filename, FileMode.Create))
                    {
                        // Copy the response from the HTTP request to the filestream
                        await response.Content.CopyToAsync(fs);
                    }

                    // Set the date stamp on the downloaded file to be the date/time of the podcast episode itself
                    File.SetCreationTime(filename, episode.Date);
                }
                else
                {
                    // yes we have, so inform the user we will be skipping
                    Console.WriteLine("\tDownload already exists, skipping.");
                }

            }
            catch (HttpRequestException e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error :{0} ", e.Message);
                Console.ResetColor();

            }
        }

        /// <summary>
        /// Returns a string for the full file path for an episode, given the directory and episode supplied. 
        /// </summary>
        /// <param name="episode"></param>
        /// <param name="directory"></param>
        /// <returns></returns>
        private string GeneratePathForEpisode(Episode episode, string directory)
        {
            string filename = this.CleanFileName(episode.Title) + this.GetExtenstionFromUrl(episode.MediaUrl.ToString());
            string result = Path.Combine(directory, filename);
            return result;
        }


        /// <summary>
        /// Returns a string for the directory structure for a given podcast episode (no filename)
        /// </summary>
        /// <param name="episode"></param>
        /// <returns></returns>
        private string GenerateDirectoryForEpisode(Episode episode)
        {
            string rootDir = this.CleanFileName(episode.ShowTitle);
            string year = this.CleanFileName(episode.Date.ToString("yyyy"));
            string month = this.CleanFileName(episode.Date.ToString("MMM"));
            string result = Path.Combine(rootDir, year, month);
            return result;
        }

        /// <summary>
        /// Removes unsupported filesystem characters from filenames.
        /// </summary>
        /// <param name="pathToClean"></param>
        /// <returns></returns>
        private string CleanFileName(string pathToClean)
        {

            string invalid = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());

            foreach (char c in invalid)
            {
                pathToClean = pathToClean.Replace(c.ToString(), "_");
            }

            return pathToClean;
        }


        /// <summary>
        /// Get the file extension from a file in a URL, ignoring any query string parameters. 
        /// </summary>
        /// <param name="url"></param>
        /// <param name="defaultExtensionIfNone"></param>
        /// <returns></returns>
        private string GetExtenstionFromUrl(string url, string defaultExtensionIfNone = Constants.DEFAULT_FILE_EXT)
        {
            string result = String.Empty;

            string extension = Path.GetExtension(url);
            if (extension.Contains('?'))
            {

                result = extension.Split('?', StringSplitOptions.RemoveEmptyEntries)[0];
            }
            else
            {
                result = extension;
            }

            if (String.IsNullOrEmpty(result))
            {
                result = defaultExtensionIfNone;
            }

            return result;

        }


        /// <summary>
        /// Returns true if the downloaded file does not exist, or does, but has a different length to the one on the server.
        /// </summary>
        /// <param name="episode"></param>
        /// <param name="downloadedFile"></param>
        /// <returns></returns>
        private static async Task<bool> IsDownloadNeeded(Episode episode, string downloadedFile)
        {
            bool result = true;

            HttpClient client = new HttpClient();
            // Set the timeout to 30 seconds
            client.Timeout = new TimeSpan(0, 0, 30);

            long contentLength = 0;

            // Get just the headers of the download, to find out how big the file is
            using (HttpResponseMessage response = await client.GetAsync(episode.MediaUrl, HttpCompletionOption.ResponseHeadersRead))
            {
                response.EnsureSuccessStatusCode();
                // This is the file size
                contentLength = response.Content.Headers.ContentLength.GetValueOrDefault(0);

            }


            // Now get the file size of a file that we may or may not have downloaded

            if (File.Exists(downloadedFile))
            {

                FileInfo file = new FileInfo(downloadedFile);
                long fileLength = file.Length;

                // Our return value should be false if the files are not the same size. If they are, then it means the download already exists.
                result = fileLength != contentLength;
            }

            return result;
        }


        /// <summary>
        /// Filters a list of episodes by date
        /// </summary>
        /// <param name="episodes"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public static IEnumerable<Episode> GetEpisodeDateRange(IEnumerable<Episode> episodes, DateTime start, DateTime end)
        {
            var result = from e in episodes
                         where (e.Date >= start && e.Date <= end)
                         select e;

            return result;
        }




    }
}

