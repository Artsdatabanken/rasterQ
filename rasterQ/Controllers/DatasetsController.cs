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
            return _rasterReader.PageBlobs[dataset].Metadata as Dictionary<string, string>;
        }

        [HttpGet("{dataset}/{key}")]
        public string Get(string dataset, string key)
        {
            return _rasterReader.PageBlobs[dataset].Metadata[key];
        }

        [HttpGet]
        public Dictionary<string, IDictionary<string, string>> Get()
        {
            var metadata = new Dictionary<string, IDictionary<string, string>>();

            foreach (var cloudPageBlob in _rasterReader.PageBlobs)
                metadata[cloudPageBlob.Key] = cloudPageBlob.Value.Metadata;

            return metadata;
        }
    }
}