using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using rasterQ.Raster;
using rasterQ.Tools;

namespace rasterQ.Controllers
{
    [Route("v1/[controller]")]
    public class PointController : Controller
    {
        private readonly Reader _rasterReader;

        public PointController(Reader rasterReader)
        {
            _rasterReader = rasterReader;
        }

        [HttpGet]
        public ActionResult Get()
        {
            return RedirectToAction("Get", "Capabilities");
        }

        [HttpGet("{x}/{y}")]
        public async Task<Dictionary<string, Dictionary<string, string>>> Get(double x, double y)
        {
            var taskList = new Dictionary<string, Task<Result>>();

            foreach (var rasterFile in _rasterReader.Files)
                taskList[rasterFile.BlobName] = rasterFile.ReadValue(x, y, _rasterReader);

            await Task.WhenAll(taskList.Values);

            var values = _rasterReader.PopulateValues(taskList, Request);

            HeightHandler.SelectBestHeight(taskList, values);

            return values;
        }

        [HttpGet("{x}/{y}/{dataset}")]
        public async Task<Dictionary<string, string>> Get(double x, double y, string dataset)
        {
            var result = await _rasterReader.Files.First(d => d.BlobName == dataset).ReadValue(x, y, _rasterReader);
            return new Dictionary<string, string> { { result.Key, result.Value } };
        }
    }
}