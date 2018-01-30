using System.Net.Http;
using Newtonsoft.Json;

namespace rasterQ.NiN
{
    public class CodeFetcher
    {
        public static Code[] Get()
        {
            var client = new HttpClient();

            var response = client.GetAsync("http://webtjenester.artsdatabanken.no/NiN/v2b/variasjon/allekoder").Result;

            var jsonString = response.Content.ReadAsStringAsync().Result;

            return JsonConvert.DeserializeObject<Code[]>(jsonString);
        }
    }
}