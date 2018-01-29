using System.Collections.Generic;
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
            var taskList = new Dictionary<string, Task<string>>();

            foreach (var rasterFile in _rasterReader.Files) taskList[rasterFile.BlobName] = rasterFile.ReadValue(x, y, _rasterReader);

            await Task.WhenAll(taskList.Values);

            var values = new Dictionary<string, string>();

            foreach (var task in taskList.Where(t => t.Value.Result != string.Empty)) values[task.Key] = task.Value.Result;

            foreach (var task in taskList.Where( t => t.Key.EndsWith("_Global") && values.ContainsKey(t.Key.Split('_')[0]))) values.Remove(task.Key);

            var newValues = new Dictionary<string, string>();

            foreach (var value in values)
            {
                if (_rasterReader.NiNDictionary.ContainsKey(value.Key))
                {
                    var newKey = _rasterReader.NiNDictionary[value.Key][int.Parse(value.Value) -1];
                    newValues[newKey] = _rasterReader.CodeFetcher.NiNCodes.First(c => c.Kode.Id == newKey).Navn;
                }
                else newValues[value.Key] = value.Value;
            }

            return newValues;
        }

        [HttpGet("{x}/{y}/{dataset}")]
        public async Task<string> Get(double x, double y, string dataset)
        {
            return await _rasterReader.Files.First(d => d.BlobName == dataset).ReadValue(x, y, _rasterReader);
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

            foreach (var cloudPageBlob in _rasterReader.PageBlobs) metadata[cloudPageBlob.Key] = cloudPageBlob.Value.Metadata;

            return metadata;
        }
    }
}
