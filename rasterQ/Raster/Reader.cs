using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using rasterQ.NiN;

namespace rasterQ.Raster
{
    public class Reader
    {
        public CloudBlobContainer Container;
        public List<Dataset> Files = new List<Dataset>();
        public Code[] NiNCodes;
        public Dictionary<string, List<string>> NiNDictionary = new Dictionary<string, List<string>>();
        public Dictionary<string, CloudPageBlob> PageBlobs = new Dictionary<string, CloudPageBlob>();
        public Dictionary<string, Dictionary<string, string>> Metadata = new Dictionary<string, Dictionary<string, string>>();

        public Reader(string key, string containerReference)
        {
            var cloudStorageAccount = CloudStorageAccount.Parse(key);

            var client = cloudStorageAccount.CreateCloudBlobClient();

            Container = client.GetContainerReference(containerReference);

            NiNCodes = CodeFetcher.Get();

            BlobContinuationToken token = null;
            do
            {
                var resultSegment = Container.ListBlobsSegmentedAsync(token).Result;
                token = resultSegment.ContinuationToken;

                foreach (var item in resultSegment.Results) ReadBlobMetadata((CloudPageBlob) item).Wait();
            } while (token != null);

            foreach (var rasterFile in Files)
            {
                if (NiNCodes.All(c => c.Kode.Id != rasterFile.BlobName)) continue;
                var codeList = NiNCodes.First(c => c.Kode.Id == rasterFile.BlobName).UnderordnetKoder
                    .Select(underordnetKode => underordnetKode.Id).ToList();
                codeList.Sort();
                NiNDictionary[rasterFile.BlobName] = codeList;
            }
        }

        private async Task ReadBlobMetadata(CloudPageBlob pageBlob)
        {
            await pageBlob.FetchAttributesAsync();
            
            var metadata = AddMetadata(pageBlob);

            var nullValue = float.NaN;
            var nullValueParsed = metadata.ContainsKey("nullvalue") && float.TryParse(metadata["nullvalue"],
                                      NumberStyles.Any, CultureInfo.InvariantCulture, out nullValue);

            var dataset = new Dataset
            {
                BlobName = pageBlob.Name,
                MinX = ParseHeaderDouble(metadata["minx"]),
                MaxX = ParseHeaderDouble(metadata["maxx"]),
                MinY = ParseHeaderDouble(metadata["miny"]),
                MaxY = ParseHeaderDouble(metadata["maxy"]),
                Resolution = ParseHeaderDouble(metadata["resolution"]),
                RowLength = int.Parse(metadata["rowlength"]),
                ColumnLength = int.Parse(metadata["columnlength"]),
                HeaderOffset = int.Parse(metadata["headeroffset"]),
                ValueLength = int.Parse(metadata["valuelength"]),
                Crs = metadata.ContainsKey("crs") && metadata["crs"] != "WGS-84" ? int.Parse(metadata["crs"]) : 0,
                NullValue = nullValueParsed ? nullValue : float.NaN
            };

            Files.Add(dataset);
            PageBlobs[dataset.BlobName] = pageBlob;
        }

        private Dictionary<string, string> AddMetadata(CloudBlob pageBlob)
        {
            Metadata[pageBlob.Name] = new Dictionary<string, string>();

            foreach (var record in pageBlob.Metadata) Metadata[pageBlob.Name][record.Key] = Uri.UnescapeDataString(record.Value);

            if (NiNCodes.All(c => c.Kode.Id != pageBlob.Name)) return Metadata[pageBlob.Name];

            var ninCode = NiNCodes.First(c => c.Kode.Id == pageBlob.Name);
            Metadata[pageBlob.Name]["name"] = ninCode.Navn;
            Metadata[pageBlob.Name]["definition"] = ninCode.Kode.Definisjon;

            return Metadata[pageBlob.Name];
        }

        private static double ParseHeaderDouble(string value)
        {
            double.TryParse(value, out var parseValue);

            if (parseValue.ToString(CultureInfo.InvariantCulture) == "0")
                double.TryParse(value.Replace('.', ','), out parseValue);

            return parseValue;
        }

        public Dictionary<string, Dictionary<string, string>> PopulateValues(Dictionary<string, Task<Result>> taskList, HttpRequest request)
        {
            var values = new Dictionary<string, Dictionary<string, string>>();

            foreach (var task in taskList.Where(t => t.Value.Result != null && t.Value.Result.Value != string.Empty))
            {
                values[task.Value.Result.Key] = new Dictionary<string, string>
                {
                    {"value", task.Value.Result.Value},
                    {"dataset", $"{request.Scheme}://{request.Host}{request.PathBase}/v1/datasets/" + task.Key}
                };
                if (!NiNDictionary.ContainsKey(task.Key)) continue;

                values[task.Value.Result.Key]["definition"] =
                    NiNCodes.First(c => c.Kode.Id == task.Key).Kode.Definisjon;
                values[task.Value.Result.Key]["name"] =
                    NiNCodes.First(c => c.Kode.Id == task.Key).Navn;

                values[task.Value.Result.Key]["article"] = Metadata[task.Key]["article"];

                values[task.Value.Result.Key]["logo"] = Metadata[task.Key]["logo"];
            }

            return values;
        }
    }
}