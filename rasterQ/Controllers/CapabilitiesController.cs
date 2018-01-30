using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;

namespace rasterQ.Controllers
{

    [Route("v1/[controller]")]
    public class CapabilitiesController : Controller
    {

        [HttpGet]
        public Dictionary<string, Dictionary<string, string>> Get()
        {
            var baseUrl = GetBaseUrl();
            return new Dictionary<string, Dictionary<string, string>>
            {
                {
                    "point", new Dictionary<string, string>
                    {
                        {"PointQuery", baseUrl + "point/{lon}/{lat}"},
                        {"DatasetPointQuery", baseUrl + "point/{lon}/{lat}/{dataset}"}
                    }
                },
                {
                    "datasets", new Dictionary<string, string>
                    {
                        {"DatasetsQuery", baseUrl + "datasets"},
                        {"DatasetQuery", baseUrl + "datasets/{dataset}"},
                        {"DatasetValueQuery", baseUrl + "datasets/{dataset}/{key}"}
                    }
                }
            };
        }

        private string GetBaseUrl()
        {
            return $"{Request.Scheme}://{Request.Host}{Request.PathBase}/v1/";
        }
    }
}
