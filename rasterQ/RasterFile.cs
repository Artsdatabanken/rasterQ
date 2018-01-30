using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

// ReSharper disable CompareOfFloatsByEqualityOperator

namespace rasterQ
{
    public class RasterFile
    {
        public string BlobName { get; set; }
        public double Resolution { get; set; }
        public double MinX { get; set; }
        public double MaxX { get; set; }
        public double MinY { get; set; }
        public double MaxY { get; set; }
        public int HeaderOffset { get; set; }
        public int ValueLength { get; set; }
        public int RowLength { get; set; }
        public int ColumnLength { get; set; }
        public long Column { get; set; }
        public long Row { get; set; }
        public int Crs { get; set; }
        public double NullValue { get; set; }

        public async Task<RasterResult> ReadValue(double queryX, double queryY, RasterReader rasterReader)
        {
            var localCoordinates = Crs == 0 ? new[] {queryX, queryY} : Projector.Wgs84ToUtm(queryX, queryY, Crs);

            if (OutsideBbox(localCoordinates)) return null;

            CalculateImageCooordinates(localCoordinates);

            var offset = CalculateOffset();

            var valueBytes = new byte[ValueLength];

            await rasterReader.PageBlobs[BlobName].DownloadRangeToByteArrayAsync(valueBytes, 0, offset, ValueLength);

            var value = GetValueFromBytes(valueBytes);

            if (value == string.Empty) return null;

            var result = new RasterResult
            {
                Key = rasterReader.NiNDictionary.ContainsKey(BlobName)
                    ? rasterReader.NiNDictionary[BlobName][int.Parse(value) - 1]
                    : BlobName
            };

            result.Value = rasterReader.NiNDictionary.ContainsKey(BlobName)
                ? rasterReader.CodeFetcher.NiNCodes.First(c => c.Kode.Id == result.Key).Navn
                : value;

            return result;
        }

        private string GetValueFromBytes(byte[] valueBytes)
        {
            switch (ValueLength)
            {
                case 1:
                    if (valueBytes[0] == NullValue || valueBytes[0] == 0) return string.Empty;
                    return valueBytes[0].ToString();
                case 2:
                    var int16Value = BitConverter.ToInt16(valueBytes, 0);
                    if (int16Value == NullValue || int16Value == 0) return string.Empty;
                    return int16Value.ToString(CultureInfo.InvariantCulture);
                case 4:
                    var singelValue = BitConverter.ToSingle(valueBytes, 0);
                    if (singelValue == NullValue || singelValue == 0) return string.Empty;
                    return singelValue.ToString(CultureInfo.InvariantCulture);
                case 8:
                    var doubleValue = BitConverter.ToDouble(valueBytes, 0);
                    if (doubleValue == NullValue || doubleValue == 0) return string.Empty;
                    return doubleValue.ToString(CultureInfo.InvariantCulture);
                default:
                    throw new NotImplementedException("Datatype not implemented: " + ValueLength + " bytes");
            }
        }

        private bool OutsideBbox(IReadOnlyList<double> localCoordinates)
        {
            return MaxY < localCoordinates[1] || MinX > localCoordinates[0] || MinY > localCoordinates[1] ||
                   MaxX < localCoordinates[0];
        }

        private long CalculateOffset()
        {
            var rowBytes = RowLength * ValueLength;
            var rowOffset = Row * rowBytes;
            var columnOffset = Column * ValueLength;

            return rowOffset + columnOffset + HeaderOffset;
        }

        private void CalculateImageCooordinates(IReadOnlyList<double> coordinates)
        {
            Column = (long) ((coordinates[0] - MinX) / Resolution);
            Row = (long) ((MaxY - coordinates[1]) / Resolution);
        }
    }
}