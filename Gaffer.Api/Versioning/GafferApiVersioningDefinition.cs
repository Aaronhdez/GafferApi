using Asp.Versioning;

namespace Gaffer.Api.Versioning;

public class GafferApiVersioningDefinition {
    public List<ApiVersion> Versions { get; }

    public GafferApiVersioningDefinition(int min = ApiVersioning.Min, int max = ApiVersioning.Current)
    {
        Versions = ApiVersioning.Versions(min, max);
    }
}