using System;

namespace rasterQ
{
    public class Projector
    {
        public static double[] Wgs84To3857(double x, double y)
        {
            x = x * 20037508.34 / 180;
            y = Math.Log(Math.Tan((90 + y) * Math.PI / 360)) / (Math.PI / 180);
            y = y * 20037508.34 / 180;
            return new[] {x, y};
        }

        public static double[] Wgs84ToUtm(double x, double y, int zoneNumber)
        {
            const double deg2Rad = Math.PI / 180;
            const int a = 6378137;
            const double eccSquared = 0.00669438;
            const double k0 = 0.9996;
            double longOrigin;
            double eccPrimeSquared;
            double n, T, c, aRenamed, m;
            var longTemp = x + 180 - (int) ((x + 180) / 360) * 360 - 180;
            var latRad = y * deg2Rad;
            var longRad = longTemp * deg2Rad;
            double longOriginRad;

            //// Handling of "wonky" norwegian zones. Not needed as we pass in zone. Might be nice to hold on to for future reference.
            //var zoneNumber = (int) (longTemp + 180) / 6 + 1;
            //if (y >= 56.0 && y < 64.0 && longTemp >= 3.0 && longTemp < 12.0) zoneNumber = 32;
            //if (y >= 72.0 && y < 84.0)
            //    if (longTemp >= 0.0 && longTemp < 9.0) zoneNumber = 31;
            //    else if (longTemp >= 9.0 && longTemp < 21.0) zoneNumber = 33;
            //    else if (longTemp >= 21.0 && longTemp < 33.0) zoneNumber = 35;
            //    else if (longTemp >= 33.0 && longTemp < 42.0) zoneNumber = 37;

            longOrigin = (zoneNumber - 1) * 6 - 180 + 3;
            longOriginRad = longOrigin * deg2Rad;
            eccPrimeSquared = (eccSquared) / (1 - eccSquared);
            n = a / Math.Sqrt(1 - eccSquared * Math.Sin(latRad) * Math.Sin(latRad));
            T = Math.Tan(latRad) * Math.Tan(latRad);
            c = eccPrimeSquared * Math.Cos(latRad) * Math.Cos(latRad);
            aRenamed = Math.Cos(latRad) * (longRad - longOriginRad);
            m = a * ((1 - eccSquared / 4 - 3 * eccSquared * eccSquared / 64 -
                      5 * eccSquared * eccSquared * eccSquared / 256) * latRad -
                     (3 * eccSquared / 8 + 3 * eccSquared * eccSquared / 32 +
                      45 * eccSquared * eccSquared * eccSquared / 1024) * Math.Sin(2 * latRad) +
                     (15 * eccSquared * eccSquared / 256 + 45 * eccSquared * eccSquared * eccSquared / 1024) *
                     Math.Sin(4 * latRad) - (35 * eccSquared * eccSquared * eccSquared / 3072) * Math.Sin(6 * latRad));
            var utmEasting = k0 * n * (aRenamed + (1 - T + c) * aRenamed * aRenamed * aRenamed / 6 +
                                       (5 - 18 * T + T * T + 72 * c - 58 * eccPrimeSquared) * aRenamed *
                                       aRenamed * aRenamed * aRenamed * aRenamed / 120) + 500000.0;
            var utmNorthing = k0 * (m + n * Math.Tan(latRad) * (aRenamed * aRenamed / 2 +
                                                                (5 - T + 9 * c + 4 * c * c) * aRenamed * aRenamed *
                                                                aRenamed * aRenamed / 24 +
                                                                (61 - 58 * T + T * T + 600 * c -
                                                                 330 * eccPrimeSquared) * aRenamed * aRenamed *
                                                                aRenamed * aRenamed * aRenamed * aRenamed / 720));
            if (y < 0) utmNorthing += 10000000.0;
            return new[] {utmEasting, utmNorthing};
        }

        public static double[] ProjectToLocal(double queryX, double queryY, int crs)
        {
            return crs == 0 ? new[] {queryX, queryY} : Wgs84ToUtm(queryX, queryY, crs);
        }
    }
}