using System.Text.Json;
using System.Text.Json.Serialization;

namespace Gaffer.Api.Tests.TestShared;

/// <summary>
/// Options for reading API responses with System.Text.Json on the test side:
/// the API writes enums as their member names (Newtonsoft StringEnumConverter).
/// </summary>
public static class TestJson {
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web) {
        Converters = { new JsonStringEnumConverter() }
    };
}
