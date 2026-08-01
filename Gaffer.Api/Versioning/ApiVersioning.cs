using Asp.Versioning;

namespace Gaffer.Api.Versioning {
    public static class ApiVersioning {
        public const int Min = 1;
        public const int Current = 1;
        public static List<ApiVersion> Versions(int min = Min, int max = Current) {
            return Enumerable.Range(min, max - min + 1).Select(x => new ApiVersion(x, 0)).ToList();
        }
    }
}