using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using rasterQ.Raster;

namespace rasterQ.Controllers
{
    [Route("v1/[controller]")]
    public class DatasetsController : Controller
    {
        private readonly Reader _rasterReader;

        public DatasetsController(Reader rasterReader)
        {
            _rasterReader = rasterReader;
        }

        [HttpGet("{dataset}")]
        public Dictionary<string, string> Get(string dataset)
        {
            return _rasterReader.Metadata[dataset];
        }

        [HttpGet("{dataset}/{key}")]
        public string Get(string dataset, string key)
        {
            return _rasterReader.Metadata[dataset][key];
        }

        [HttpGet]
        public Dictionary<string, Dictionary<string, string>> Get()
        {
            return _rasterReader.Metadata;
        }
    }
}