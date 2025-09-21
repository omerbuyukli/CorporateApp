namespace CorporateApp.Infrastructure.Constants
{
    public static class DiaApiConstants
    {
        // Modül endpoint'leri
        public static class Modules
        {
            public const string SIS = "SIS";
            public const string SCF = "SCF";
            public const string GTS = "GTS";
            public const string PER = "PER";
            public const string FIN = "FIN";
            public const string STK = "STK";
        }

        // Response kodları
        public static class ResponseCodes
        {
            public const string Success = "200";
            public const string Unauthorized = "401";
            public const string NotFound = "404";
            public const string Error = "500";
        }

        // Default değerler
        public const int DEFAULT_PAGE_SIZE = 100;
        public const int DEFAULT_TIMEOUT = 30;
    }
}