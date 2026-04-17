using System.Text.Json;
using System.Text.Json.Serialization;

namespace SolidWorksMcp.SolidWorksWorker.Protocol;

internal static class WorkerJson
{
    public static JsonSerializerOptions CreateSerializerOptions()
    {
        return new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    public static string SerializeResponse(WorkerResponse response)
    {
        return JsonSerializer.Serialize(
            response,
            response.GetType(),
            CreateSerializerOptions());
    }
}
