using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using rasterQ.Raster;

namespace rasterQ.Tools
{
    public class HeightHandler
    {
        private const string Height = "Høyde";
        private const string Global = "_Global";
        private const string GlobalHeight = Height + Global;
        private const string Depth = "Dybde";

        public static void SelectBestHeight(Dictionary<string, Task<Result>> taskList,
            Dictionary<string, Dictionary<string, string>> values)
        {
            foreach (var task in taskList.Where(t =>
                t.Key.EndsWith(Global) && values.ContainsKey(t.Value.Result.Key.Split('_')[0])))
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