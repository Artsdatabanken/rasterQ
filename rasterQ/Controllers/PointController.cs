using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using rasterQ.Raster;

namespace rasterQ.Controllers
{
    [Route("v1/[controller]")]
    public class PointController : Controller
    {
        private readonly Reader _rasterReader;
        private const string Height = "Høyde";
        private const string GlobalHeight = Height + "_Global";
        private const string Depth = "Dybde";

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

            var values = PopulateValues(taskList);

            SelectBestHeight(taskList, values);

            return values;
        }

        [HttpGet("{x}/{y}/{dataset}")]
        public async Task<Dictionary<string, string>> Get(double x, double y, string dataset)
        {
            var result = await _rasterReader.Files.First(d => d.BlobName == dataset).ReadValue(x, y, _rasterReader);
            return new Dictionary<string, string> { { result.Key, result.Value } };
        }

        private Dictionary<string, Dictionary<string, string>> PopulateValues(Dictionary<string, Task<Result>> taskList)
        {
            var values = new Dictionary<string, Dictionary<string, string>>();

            foreach (var task in taskList.Where(t => t.Value.Result != null && t.Value.Result.Value != string.Empty))
            {
                values[task.Value.Result.Key] = new Dictionary<string, string>
                {
                    {"value", task.Value.Result.Value},
                    {"dataset", $"{Request.Scheme}://{Request.Host}{Request.PathBase}/v1/datasets/" + task.Key}
                };
                if (!_rasterReader.NiNDictionary.ContainsKey(task.Key)) continue;

                values[task.Value.Result.Key]["definition"] =
                    _rasterReader.NiNCodes.First(c => c.Kode.Id == task.Key).Kode.Definisjon;
                values[task.Value.Result.Key]["name"] =
                    _rasterReader.NiNCodes.First(c => c.Kode.Id == task.Key).Navn;
            }

            return values;
        }

        private static void SelectBestHeight(Dictionary<string, Task<Result>> taskList,
            Dictionary<string, Dictionary<string, string>> values)
        {
            foreach (var task in taskList.Where(t =>
                t.Key.EndsWith("_Global") && values.ContainsKey(t.Value.Result.Key.Split('_')[0])))
                values.Remove(task.Value.Result.Key);

            if (values.ContainsKey(GlobalHeight)) OverwriteHeight(values, GlobalHeight);

            var previousKeys = new string[values.Keys.Count];
            values.Keys.CopyTo(previousKeys, 0);

            foreach (var valuesKey in previousKeys.Where(k => k.StartsWith(Height + "_")))
                OverwriteHeight(values, valuesKey);

            if (!values.ContainsKey(Depth) || !values.ContainsKey(Height)) return;

            OverwriteHeight(values, Depth);
        }

        private static void OverwriteHeight(IDictionary<string, Dictionary<string, string>> values, string newValueKey)
        {
            values[Height] = values[newValueKey];
            values.Remove(newValueKey);
        }
    }
}