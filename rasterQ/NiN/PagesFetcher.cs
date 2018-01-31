using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace rasterQ.NiN
{
    public class PagesFetcher
    {
        private const string PagePattern = "http://data.beta.artsdatabanken.no/api/Graph/Nodes/";
        private const string BaseUrl = "https://www.artsdatabanken.no/Pages/";

        public static string Get(string key)
        {
            var client = new HttpClient();
            var response = client.GetAsync("http://data.beta.artsdatabanken.no/api/Graph/NiN2.0/" + key).Result;
            var jsonString = response.Content.ReadAsStringAsync().Result;
            if (!jsonString.Contains(PagePattern)) return null;
            var split = jsonString.Split(PagePattern)[1];
            var page = split.Split('"')[0];
            return Uri.UnescapeDataString(BaseUrl + page);
        }
    }
}