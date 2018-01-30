namespace rasterQ
{
    public class NiNCode
    {
        public string Navn { get; set; }
        public Kode Kode { get; set; }
        public Kode OverordnetKode { get; set; }
        public Kode[] UnderordnetKoder { get; set; }
    }
}