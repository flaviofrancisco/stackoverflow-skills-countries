using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CountFromStackoverflow
{
    class Program
    {
        static readonly HttpClient client = new HttpClient();

        private const string rootUrl = @"https://stackoverflow.com/";
        private const string filter = @"jobs?sort=i";
        private const string fileName = @"G:\temp\stackoverflow-all.txt";

        private static Dictionary<string, Dictionary<string, int>> locationSkill = new Dictionary<string, Dictionary<string, int>>();

        static async Task Main(string[] args)
        {
            int pages = await GetPagesAsync();

            for (int i = 1; i < pages; i++)
            {
                IHtmlDocument document = await GetDocument($"{rootUrl}{filter}&pg={i}");

                var element = document.All
                    .Where(x => x?.ClassName != null 
                    && x.ClassName.Contains("js-search-results flush-left")).ToList();

                if (element.Any())
                {
                    var child = element[0].LastElementChild;

                    foreach (var e in child.Children)
                    {
                        var location = e.QuerySelector("span.fc-black-500");

                        if (location != null)
                        {
                            var locationKey = location.InnerHtml.Trim();
                            var cityCountry = locationKey.Split(',');     

                            if (cityCountry.Length > 1)
                            {
                                locationKey = Translator(cityCountry);
                            }

                            if (!locationSkill.ContainsKey(locationKey))
                            {
                                locationSkill.Add(locationKey, new Dictionary<string, int>());
                            }

                            var skillTagDict = locationSkill[locationKey];

                            var skillTags = e.QuerySelectorAll("a.post-tag.job-link.no-tag-menu");

                            foreach (var st in skillTags)
                            {
                                if (!skillTagDict.ContainsKey(st.InnerHtml))
                                {
                                    skillTagDict.Add(st.InnerHtml, 1);
                                }
                                else
                                {
                                    skillTagDict[st.InnerHtml]++;
                                }
                            }

                        }
                    }
                }
            }
                        
            using (StreamWriter sw = File.CreateText(fileName))
            {
                var locations = locationSkill.Keys.ToList();
                locations.Sort();

                foreach (var location in locations)
                {
                    sw.WriteLine($" {location} ");
                    sw.WriteLine();

                    var skills = locationSkill[location];

                    foreach (KeyValuePair<string, int> skill in skills.OrderByDescending(key => key.Value))
                    {
                        sw.WriteLine($"{skill.Key} - {skill.Value}");
                    }

                    sw.WriteLine();
                    sw.WriteLine();
                    sw.WriteLine();

                }
            } 
        }

        private static string Translator(string[] cityCountry)
        {
            string locationKey;
            switch (cityCountry[1].Trim())
            {
                case "Deutschland":
                    locationKey = "Germany";
                    break;
                case "Schweiz":
                    locationKey = "Switzerland";
                    break;
                default:
                    locationKey = cityCountry[1].Trim();
                    break;
            }

            var usa = new string[] { "AK", "AL", "AR", "AS", "AZ", "CA", "CO", "CT", "DC", "DE", "FL", "GA", "GU", "HI", "IA", "ID", "IL", "IN", "KS", "KY", "LA", "MA", "MD", "ME", "MI", "MN", "MO", "MP", "MS", "MT", "NC", "ND", "NE", "NH", "NJ", "NM", "NV", "NY", "OH", "OK", "OR", "PA", "PR", "RI", "SC", "SD", "TN", "TX", "UM", "UT", "VA", "VI", "VT", "WA", "WI", "WV", "WY" };

            if (usa.Any(x => x.Equals(locationKey, System.StringComparison.InvariantCultureIgnoreCase)))
            {
                locationKey = "USA";
            }

            var canada = new string[] { "AB", "BC", "MB", "NB", "NL", "NT", "NS", "NU", "ON", "PE", "QC", "SK", "YT" };

            if (canada.Any(x => x.Equals(locationKey, System.StringComparison.InvariantCultureIgnoreCase)))
            {
                locationKey = "CANADA";
            }

            return locationKey;
        }

        private static async Task<IHtmlDocument> GetDocument(string url)
        {
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
            client.DefaultRequestHeaders.Add("User-Agent", @"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/79.0.3945.130 Safari/537.36");
            client.DefaultRequestHeaders.Add("Referer", @"https://stackoverflow.com/jobs");
            HttpResponseMessage response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();
            var parser = new HtmlParser();
            IHtmlDocument document = parser.ParseDocument(responseBody);
            return document;
        }

        private static async Task<int> GetPagesAsync()
        {
            IHtmlDocument document = await GetDocument($"{rootUrl}{filter}");

            IEnumerable<IElement> anchors = document.QuerySelectorAll("a.s-pagination--item");
                        
            var pages = 0;

            foreach (IHtmlAnchorElement a in anchors)
            {
                if (a.Href.Contains($"{filter}"))
                {
                    var pgIndex = a.Href.IndexOf("pg=");

                    if (pgIndex < 0) 
                    {
                        continue;
                    }

                    var pageTerm = a.Href.Substring(pgIndex);

                    var digits = Regex.Match(pageTerm, @"\d{1,}");

                    if (digits.Success && int.TryParse(digits.Value, out int number))
                    {
                        if (number > pages)
                            pages = number;
                    }
                }
            }

            return pages;
        }
    }
}
