using Asp.Versioning;

namespace Gaffer.Api.Versioning;

public class GafferApiVersioningDefinition(int min = ApiVersioning.Min, int max = ApiVersioning.Current) {
    public List<ApiVersion> Versions { get; } = ApiVersioning.Versions(min, max);
}
