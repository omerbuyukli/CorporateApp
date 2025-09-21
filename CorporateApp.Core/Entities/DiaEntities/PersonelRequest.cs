namespace CorporateApp.Core.Entities.DiaEntities
{
    public class PersonelRequest
    {
        public class Filter
        {
            public string Field { get; set; }
            public string Operator { get; set; }
            public string Value { get; set; }
        }

        public class Root
        {
            public PersonelListele PerPersonelListele { get; set; }
        }

        public class PersonelListele
        {
            public string SessionId { get; set; }
            public int FirmaKodu { get; set; }
            public int DonemKodu { get; set; }
            public List<Filter> Filters { get; set; }
            public List<Sort> Sorts { get; set; }
            public int Limit { get; set; }
            public int Offset { get; set; }
        }

        public class Sort
        {
            public string Field { get; set; }
            public string SortType { get; set; }
        }
    }
}