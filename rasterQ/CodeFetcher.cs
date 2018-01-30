using System.Net.Http;
using Newtonsoft.Json;

namespace rasterQ
{
    public class CodeFetcher
    {
        public NiNCode[] NiNCodes;

        public CodeFetcher()
        {
            var client = new HttpClient();

            var response = client.GetAsync("http://webtjenester.artsdatabanken.no/NiN/v2b/variasjon/allekoder").Result;

            var jsonString = response.Content.ReadAsStringAsync().Result;

            NiNCodes = JsonConvert.DeserializeObject<NiNCode[]>(jsonString);
        }
    }
}