using System.Net.Http;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Newtonsoft.Json;

namespace rasterQ
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .Build();
    }

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

    public class NiNCode
    {
        public string Navn { get; set; }
        public Kode Kode { get; set; }
        public Kode OverordnetKode { get; set; }
        public Kode[] UnderordnetKoder { get; set; }

    }

    public class Kode
    {
        public string Id { get; set; }
        public string Definisjon { get; set; }
    }
}