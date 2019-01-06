using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using TelegramBottleHub.KinoBot.Parsers.Core.Models;

namespace TelegramBottleHub.KinoBot.Parsers.PlanetaKino
{
    public static class PlanetaKinoParser
    {
        private const string BaseUrl = "https://planetakino.ua";
        private const string MoviesPart = "/movies";
        private const string ShowtimesPart = "/showtimes";

        private const int RequestsMinIntervalMilliseconds = 500;

        private static readonly string[] MovieTheatresUrls = new[]
        {
            BaseUrl + "/lvov",
            BaseUrl + "/lvov2"
        };

        private static readonly WebClient _webClient = new WebClient();

        public static List<Kino> ParseComingSoonKinos()
        {
            var result = new List<Kino>();
            var htmlWeb = new HtmlWeb
            {
                OverrideEncoding = Encoding.UTF8
            };

            foreach (var movieTheatresUrl in MovieTheatresUrls)
            {
                var moviesUrl = movieTheatresUrl + MoviesPart;
                var htmlDocument = htmlWeb.Load(moviesUrl);
                Thread.Sleep(RequestsMinIntervalMilliseconds);

                var movieNodes = htmlDocument.DocumentNode.SelectNodes(
                    "//div[@class=\"content__section\" and .//*[contains(text(), \"Скоро на екранах\")]]/div[@class=\"movies-list\"]/div[contains(@class, \"movie-block\")]");
                foreach (var movieNode in movieNodes)
                {
                    var movieExternalCode = movieNode.Attributes.FirstOrDefault(a => a.Name == "data-movieid")?.Value;
                    if (string.IsNullOrWhiteSpace(movieExternalCode))
                    {
                        continue;
                    }

                    var movieImageLinkNode = movieNode.Descendants().FirstOrDefault(c => c.Name == "a");
                    if (movieImageLinkNode == null)
                    {
                        continue;
                    }

                    var movieImageNode = movieImageLinkNode.Descendants().FirstOrDefault(c => c.Name == "img");
                    if (movieImageNode == null)
                    {
                        continue;
                    }

                    var movieName = movieImageNode.Attributes.FirstOrDefault(a => a.Name == "alt")?.Value;
                    if (string.IsNullOrWhiteSpace(movieName))
                    {
                        continue;
                    }

                    var kino = new Kino
                    {
                        ExternalCode = movieExternalCode,
                        Name = movieName,
                        State = Kino.KinoState.ComingSoon
                    };

                    result.Add(kino);

                    var movieLink = movieImageLinkNode.Attributes.FirstOrDefault(a => a.Name == "href")?.Value;
                    if (!string.IsNullOrWhiteSpace(movieLink))
                    {
                        kino.Url = BaseUrl + movieLink;
                    }

                    var movieImageUrl = movieImageNode.Attributes.FirstOrDefault(a => a.Name == "data-original")?.Value;
                    if (!string.IsNullOrWhiteSpace(movieImageUrl))
                    {
                        kino.ImageUrl = BaseUrl + movieImageUrl;
                    }

                    var startRunningDateText = movieNode.Descendants()
                        .FirstOrDefault(n => n.Attributes.Any(a => a.Name == "class" && a.Value == "movie-block__text-date"))?.InnerText?.Trim().Remove(0, 2);
                    if(!string.IsNullOrWhiteSpace(startRunningDateText) && 
                        DateTime.TryParseExact(startRunningDateText, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var startRunningDate))
                    {
                        kino.StartRunningDate = startRunningDate;
                    }
                }
            }

            var uniqueKinos = result.Distinct().ToList();
            ParseDetails(uniqueKinos);

            return uniqueKinos;
        }

        public static List<Kino> ParseTodayKinos()
        {
            var result = new List<Kino>();
            var htmlWeb = new HtmlWeb
            {
                OverrideEncoding = Encoding.UTF8
            };

            foreach (var movieTheatresUrl in MovieTheatresUrls)
            {
                var showtimesUrl = movieTheatresUrl + ShowtimesPart;
                var htmlDocument = htmlWeb.Load(showtimesUrl);
                Thread.Sleep(RequestsMinIntervalMilliseconds);

                var movieNodes = htmlDocument.DocumentNode.SelectNodes("//div[@class=\"showtime-movie-container\"]");
                foreach (var movieNode in movieNodes)
                {
                    var movieNodeChilds = movieNode.Descendants();
                    if (!movieNodeChilds.Any(n =>
                            n.Name == "span" && n.Attributes.Any(a => a.Name == "class" && a.Value == "date current")))
                    {
                        continue;
                    }

                    var movieCodeNode = movieNodeChilds.FirstOrDefault(n =>
                            n.Name == "div" && n.Attributes.Any(a => a.Name == "class" && a.Value.Contains("showtimes-row")));
                    var movieCodeRaw = movieCodeNode?.Attributes.FirstOrDefault(a => a.Name == "class")?
                            .Value.Split(' ').FirstOrDefault(v => v.Contains("movie-"))?.Replace("movie-", string.Empty);
                    if (string.IsNullOrWhiteSpace(movieCodeRaw))
                    {
                        continue;
                    }

                    var movieExternalCode = movieCodeRaw;
                    var kino = new Kino
                    {
                        ExternalCode = movieExternalCode,
                        State = Kino.KinoState.Running
                    };

                    result.Add(kino);

                    var movieTitleNode = movieNodeChilds.FirstOrDefault(n =>
                            n.Name == "p" && n.Attributes.Any(a => a.Name == "class" && a.Value.Contains("movie-title")));
                    var movieTitleRaw = movieTitleNode?.InnerText;
                    var movieTitle = movieTitleRaw?.Trim().Replace("\n", string.Empty);
                    kino.Name = movieTitle;

                    var movieUrl = movieTitleNode.Descendants().FirstOrDefault(n =>
                            n.Name == "a")?.Attributes.FirstOrDefault(a => a.Name == "href")?.Value;
                    if (!string.IsNullOrWhiteSpace(movieUrl))
                    {
                        kino.Url = BaseUrl + movieUrl;
                    }

                    var movieImageUrl = movieTitleNode.Descendants()
                        .FirstOrDefault(n => n.Name == "img")?.Attributes.FirstOrDefault(a => a.Name == "data-vend")?.Value;
                    if (!string.IsNullOrWhiteSpace(movieImageUrl))
                    {
                        kino.ImageUrl = movieImageUrl;
                    }
                }
            }

            var uniqueKinos = result.Distinct().ToList();
            ParseDetails(uniqueKinos);

            return uniqueKinos;
        }

        private static void ParseDetails(IList<Kino> kinos)
        {
            var htmlWeb = new HtmlWeb
            {
                OverrideEncoding = Encoding.UTF8
            };

            foreach (var kino in kinos)
            {
                var htmlDocument = htmlWeb.Load(kino.Url);

                Thread.Sleep(RequestsMinIntervalMilliseconds);

                var posterHeaderNode = htmlDocument.DocumentNode.Descendants()
                    .FirstOrDefault(c => c.Name == "header" && c.Attributes.Any(a => a.Name == "class" && a.Value == "movie-poster-block"));
                if(posterHeaderNode == null)
                {
                    continue;
                }

                var posterUrl = posterHeaderNode.Attributes.FirstOrDefault(a => a.Name == "data-mobile")?.Value;
                if(!string.IsNullOrWhiteSpace(posterUrl))
                {
                    kino.ImageUrl = BaseUrl + posterUrl;
                }

                var trailerFrame = posterHeaderNode.Descendants()
                    .FirstOrDefault(c => c.Name == "iframe" && c.Attributes.Any(a => a.Name == "id" && a.Value == "ytplayer"));
                var trailerUrlRaw = trailerFrame?.Attributes.FirstOrDefault(a => a.Name == "src")?.Value;
                if (!string.IsNullOrWhiteSpace(trailerUrlRaw))
                {
                    trailerUrlRaw = trailerUrlRaw.Trim().Replace("https://www.youtube.com/embed/", "https://www.youtube.com/watch?v=");
                    kino.TrailerUrl = trailerUrlRaw.Substring(0, trailerUrlRaw.LastIndexOf('?'));
                }
            }
        }
    }
}
