using System.Text.Json;
using SolidWorksMcp.SolidWorksWorker.Protocol;

namespace SolidWorksWorker.Tests;

public sealed class WorkerJsonTests
{
    [Fact]
    public void SerializeResponse_IncludesDerivedRuntimePayloadFields()
    {
        var response = new HandshakeResponse
        {
            MessageType = "handshake_response",
            RequestId = "check-1",
            ProtocolVersion = "0.1.0",
            Worker = new WorkerIdentity
            {
                Name = "solidworks-worker",
                Version = "0.1.0"
            },
            SupportedSolidWorksMajorVersions = [2022],
            Capabilities = new WorkerCapabilities
            {
                SupportsStateRoundTrip = true,
                SupportsObservedStateOnFailure = true
            },
            BackendMetadata = new WorkerBackendMetadata
            {
                BackendName = "solidworks-dotnet-worker",
                SliceName = "real-bootstrap-v1",
                Runtime = "net8.0-windows",
                UsesLateBoundCom = true,
                SupportsAttachFirst = true,
                SupportsFileExistenceVerification = true,
                SupportsSessionMetadataOnExecution = true,
                SolidWorksProgIdRegistered = true,
                SupportedCommands = ["new_part", "save_part"]
            }
        };

        var payload = WorkerJson.SerializeResponse(response);
        using var document = JsonDocument.Parse(payload);
        var root = document.RootElement;

        Assert.Equal("handshake_response", root.GetProperty("messageType").GetString());
        Assert.Equal(
            "solidworks-worker",
            root.GetProperty("worker").GetProperty("name").GetString());
        Assert.Equal(
            "real-bootstrap-v1",
            root.GetProperty("backendMetadata").GetProperty("sliceName").GetString());
        Assert.Equal(
            2022,
            root.GetProperty("supportedSolidWorksMajorVersions")[0].GetInt32());
    }
}
