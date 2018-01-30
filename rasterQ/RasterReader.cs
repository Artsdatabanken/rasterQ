﻿using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace rasterQ
{
    public class RasterReader
    {
        public CodeFetcher CodeFetcher;
        public CloudBlobContainer Container;
        public List<RasterFile> Files = new List<RasterFile>();
        public Dictionary<string, List<string>> NiNDictionary = new Dictionary<string, List<string>>();
        public Dictionary<string, CloudPageBlob> PageBlobs = new Dictionary<string, CloudPageBlob>();

        public RasterReader(string key, string containerReference)
        {
            var cloudStorageAccount = CloudStorageAccount.Parse("DefaultEndpointsProtocol=https;AccountName=artsdbstorage;AccountKey=erWuCQB0eYcWmOrARt3yR4n3WqRp++u/2ziW7chGMSJUCJSZQg4hB1pozJa4D7BvPN5fkKKIWbzAsR1EKJUK1g==;EndpointSuffix=core.windows.net");

            var client = cloudStorageAccount.CreateCloudBlobClient();

            Container = client.GetContainerReference("rasterdata");

            BlobContinuationToken token = null;
            do
            {
                var resultSegment = Container.ListBlobsSegmentedAsync(token).Result;
                token = resultSegment.ContinuationToken;

                foreach (var item in resultSegment.Results) ReadBlobMetadata((CloudPageBlob) item).Wait();
            } while (token != null);

            CodeFetcher = new CodeFetcher();

            foreach (var rasterFile in Files)
            {
                if (CodeFetcher.NiNCodes.All(c => c.Kode.Id != rasterFile.BlobName)) continue;
                var codeList = CodeFetcher.NiNCodes.First(c => c.Kode.Id == rasterFile.BlobName).UnderordnetKoder
                    .Select(underordnetKode => underordnetKode.Id).ToList();
                codeList.Sort();
                NiNDictionary[rasterFile.BlobName] = codeList;
            }
        }

        private async Task ReadBlobMetadata(CloudPageBlob pageBlob)
        {
            await pageBlob.FetchAttributesAsync();
            var metadata = pageBlob.Metadata;

            var nullValue = float.NaN;
            var nullValueParsed = metadata.ContainsKey("nullvalue") && float.TryParse(metadata["nullvalue"], NumberStyles.Any, CultureInfo.InvariantCulture, out nullValue);

            var dataset = new RasterFile
            {
                BlobName = pageBlob.Name,
                RowLength = int.Parse(metadata["rowlength"]),
                ColumnLength = int.Parse(metadata["columnlength"]),
                HeaderOffset = int.Parse(metadata["headeroffset"]),
                MinX = ParseHeaderDouble(metadata["minx"]),
                MaxX = ParseHeaderDouble(metadata["maxx"]),
                MinY = ParseHeaderDouble(metadata["miny"]),
                MaxY = ParseHeaderDouble(metadata["maxy"]),
                ValueLength = int.Parse(metadata["valuelength"]),
                Resolution = ParseHeaderDouble(metadata["resolution"]),
                Crs = metadata.ContainsKey("crs") && metadata["crs"] != "WGS-84" ? int.Parse(metadata["crs"]) : 0,
                NullValue = nullValueParsed ? nullValue : float.NaN,
                DataOrigin = metadata["dataorigin"]
            };

            Files.Add(dataset);
            PageBlobs[dataset.BlobName] = pageBlob;
        }

        private static double ParseHeaderDouble(string value)
        {
            double.TryParse(value, out var parseValue);

            if (parseValue.ToString(CultureInfo.InvariantCulture) == "0")
                double.TryParse(value.Replace('.', ','), out parseValue);

            return parseValue;
        }
    }
}