﻿using System.Collections.Generic;
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

        public PointController(Reader rasterReader)
        {
            _rasterReader = rasterReader;
        }

        [HttpGet("{x}/{y}")]
        public async Task<Dictionary<string, Dictionary<string, string>>> Get(double x, double y)
        {
            var taskList = new Dictionary<string, Task<Result>>();

            foreach (var rasterFile in _rasterReader.Files)
                taskList[rasterFile.BlobName] = rasterFile.ReadValue(x, y, _rasterReader);

            await Task.WhenAll(taskList.Values);

            var values = new Dictionary<string, Dictionary<string, string>>();

            foreach (var task in taskList.Where(t => t.Value.Result != null && t.Value.Result.Value != string.Empty))
                values[task.Value.Result.Key] = new Dictionary<string, string>
                {
                    {"value", task.Value.Result.Value},
                    {"dataset", $"{Request.Scheme}://{Request.Host}{Request.PathBase}/v1/datasets/" + task.Key}
                };

            NormalizeHeights(taskList, values);

            return values;
        }

        private static void NormalizeHeights(Dictionary<string, Task<Result>> taskList,
            Dictionary<string, Dictionary<string, string>> values)
        {
            foreach (var task in taskList.Where(t =>
                t.Key.EndsWith("_Global") && values.ContainsKey(t.Value.Result.Key.Split('_')[0])))
                values.Remove(task.Value.Result.Key);


            if (values.ContainsKey("Høyde_Global"))
            {
                values["Høyde"] = values["Høyde_Global"];
                values.Remove("Høyde_Global");
            }

            var previousKeys = new string[values.Keys.Count];
            values.Keys.CopyTo(previousKeys, 0);

            foreach (var valuesKey in previousKeys.Where(k => k.StartsWith("Høyde_")))
            {
                values["Høyde"] = values[valuesKey];
                values.Remove(valuesKey);
            }

            if (!values.ContainsKey("Dybde") || !values.ContainsKey("Høyde")) return;

            values["Høyde"] = values["Dybde"];
            values.Remove("Dybde");
        }

        [HttpGet("{x}/{y}/{dataset}")]
        public async Task<Dictionary<string, string>> Get(double x, double y, string dataset)
        {
            var result = await _rasterReader.Files.First(d => d.BlobName == dataset).ReadValue(x, y, _rasterReader);
            return new Dictionary<string, string> {{result.Key, result.Value}};
        }

        [HttpGet]
        public ActionResult Get()
        {
            return RedirectToAction("Get", "Capabilities");
        }
    }
}