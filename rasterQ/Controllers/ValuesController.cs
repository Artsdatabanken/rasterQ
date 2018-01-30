﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace rasterQ.Controllers
{
    [Route("v1/[controller]")]
    public class ValuesController : Controller
    {
        private readonly RasterReader _rasterReader;

        public ValuesController(RasterReader rasterReader)
        {
            _rasterReader = rasterReader;
        }

        [HttpGet("{x}/{y}")]
        public async Task<Dictionary<string, string>> Get(double x, double y)
        {
            var taskList = new Dictionary<string, Task<RasterResult>>();

            foreach (var rasterFile in _rasterReader.Files)
                taskList[rasterFile.BlobName] = rasterFile.ReadValue(x, y, _rasterReader);

            await Task.WhenAll(taskList.Values);

            var values = new Dictionary<string, string>();

            foreach (var task in taskList.Where(t => t.Value.Result != null && t.Value.Result.Value != string.Empty))
                values[task.Value.Result.Key] = task.Value.Result.Value;

            foreach (var task in taskList.Where(t =>
                t.Key.EndsWith("_Global") && values.ContainsKey(t.Value.Result.Key.Split('_')[0])))
                values.Remove(task.Value.Result.Key);

            return values;
        }

        [HttpGet("{x}/{y}/{dataset}")]
        public async Task<Dictionary<string, string>> Get(double x, double y, string dataset)
        {
            var result = await _rasterReader.Files.First(d => d.BlobName == dataset).ReadValue(x, y, _rasterReader);
            return new Dictionary<string, string> {{result.Key, result.Value}};
        }

        [HttpGet("{dataset}")]
        public Dictionary<string, string> Get(string dataset)
        {
            return _rasterReader.PageBlobs[dataset].Metadata as Dictionary<string, string>;
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