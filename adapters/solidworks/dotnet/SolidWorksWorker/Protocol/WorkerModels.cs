namespace SolidWorksMcp.SolidWorksWorker.Protocol;

public abstract record WorkerRequest
{
    public required string MessageType { get; init; }
    public required string RequestId { get; init; }
    public required string ProtocolVersion { get; init; }
}

public sealed record HandshakeRequest : WorkerRequest
{
    public WorkerIdentity? Client { get; init; }
    public int DesiredSolidWorksMajorVersion { get; init; }
}

public sealed record ExecuteCommandRequest : WorkerRequest
{
    public required WorkerCadCommand Command { get; init; }
    public required ProjectRuntimeStateModel StateBefore { get; init; }
    public int DesiredSolidWorksMajorVersion { get; init; }
    public int? TimeoutMs { get; init; }
}

public sealed record ShutdownRequest : WorkerRequest;

public sealed record UnsupportedWorkerRequest : WorkerRequest
{
    public required string UnsupportedMessageType { get; init; }
}

public abstract record WorkerCadCommand
{
    public required string Kind { get; init; }
}

public sealed record NewPartCommand : WorkerCadCommand
{
    public string? Name { get; init; }
}

public sealed record SelectPlaneCommand : WorkerCadCommand
{
    public required string Plane { get; init; }
}

public sealed record StartSketchCommand : WorkerCadCommand;

public sealed record Point2DModel
{
    public required double X { get; init; }
    public required double Y { get; init; }
}

public sealed record DrawCircleCommand : WorkerCadCommand
{
    public required Point2DModel Center { get; init; }
    public required double Radius { get; init; }
    public bool Construction { get; init; }
}

public sealed record DrawLineCommand : WorkerCadCommand
{
    public required Point2DModel Start { get; init; }
    public required Point2DModel End { get; init; }
    public bool Construction { get; init; }
}

public sealed record DrawCenteredRectangleCommand : WorkerCadCommand
{
    public required Point2DModel Center { get; init; }
    public required Point2DModel Corner { get; init; }
    public bool Construction { get; init; }
}

public sealed record AddDimensionCommand : WorkerCadCommand
{
    public required string EntityId { get; init; }
    public required double Value { get; init; }
    public string? Orientation { get; init; }
}

public sealed record CloseSketchCommand : WorkerCadCommand;

public sealed record ExtrudeBossCommand : WorkerCadCommand
{
    public required string SketchId { get; init; }
    public required double Depth { get; init; }
    public bool MergeResult { get; init; } = true;
}

public sealed record SavePartCommand : WorkerCadCommand
{
    public required string Path { get; init; }
}

public sealed record ExportStepCommand : WorkerCadCommand
{
    public required string Path { get; init; }
}

public sealed record UnsupportedCadCommand : WorkerCadCommand;

public abstract record WorkerResponse
{
    public required string MessageType { get; init; }
    public required string RequestId { get; init; }
    public required string ProtocolVersion { get; init; }
}

public sealed record HandshakeResponse : WorkerResponse
{
    public required WorkerIdentity Worker { get; init; }
    public required IReadOnlyList<int> SupportedSolidWorksMajorVersions { get; init; }
    public required WorkerCapabilities Capabilities { get; init; }
    public required WorkerBackendMetadata BackendMetadata { get; init; }
}

public sealed record ExecuteCommandSuccessResponse : WorkerResponse
{
    public bool Ok { get; init; } = true;
    public required ProjectRuntimeStateModel NextState { get; init; }
    public required WorkerExecutionMetadata Execution { get; init; }
}

public sealed record ExecuteCommandFailureResponse : WorkerResponse
{
    public bool Ok { get; init; } = false;
    public required WorkerErrorPayload Error { get; init; }
    public ProjectRuntimeStateModel? ObservedState { get; init; }
    public bool ResyncRequired { get; init; }
}

public sealed record ShutdownResponse : WorkerResponse;

public sealed record WorkerIdentity
{
    public required string Name { get; init; }
    public required string Version { get; init; }
}

public sealed record WorkerCapabilities
{
    public required bool SupportsStateRoundTrip { get; init; }
    public required bool SupportsObservedStateOnFailure { get; init; }
}

public sealed record WorkerBackendMetadata
{
    public required string BackendName { get; init; }
    public required string SliceName { get; init; }
    public required string Runtime { get; init; }
    public required bool UsesLateBoundCom { get; init; }
    public required bool SupportsAttachFirst { get; init; }
    public required bool SupportsFileExistenceVerification { get; init; }
    public required bool SupportsSessionMetadataOnExecution { get; init; }
    public required bool SolidWorksProgIdRegistered { get; init; }
    public required IReadOnlyList<string> SupportedCommands { get; init; }
}

public sealed record WorkerExecutionMetadata
{
    public required string BackendName { get; init; }
    public required long DurationMs { get; init; }
    public int? SolidWorksMajorVersion { get; init; }
    public required WorkerBackendMetadata BackendMetadata { get; init; }
    public required SolidWorksSessionMetadata Session { get; init; }
    public required Dictionary<string, object?> OperationDetails { get; init; }
}

public sealed record WorkerErrorPayload
{
    public required string Code { get; init; }
    public required string Message { get; init; }
    public required bool Retryable { get; init; }
    public required Dictionary<string, object?> Details { get; init; }
}

public sealed record SolidWorksSessionMetadata
{
    public required bool Connected { get; init; }
    public required string ConnectionMode { get; init; }
    public required bool SolidWorksProgIdRegistered { get; init; }
    public bool HasActiveDocument { get; init; }
    public string? ActiveDocumentKind { get; init; }
    public string? ActiveDocumentName { get; init; }
    public string? ActiveDocumentPath { get; init; }
    public bool? ActiveDocumentModified { get; init; }
    public int? SolidWorksMajorVersion { get; init; }
    public string? RevisionNumber { get; init; }
    public bool BaselineSatisfied { get; init; }
}

public sealed record SketchEntityModel
{
    public required string Id { get; init; }
    public required string Kind { get; init; }
    public Point2DModel? Start { get; init; }
    public Point2DModel? End { get; init; }
    public Point2DModel? Center { get; init; }
    public Point2DModel? Corner { get; init; }
    public double? Radius { get; init; }
    public bool Construction { get; init; }
}

public sealed record SketchDimensionModel
{
    public required string Id { get; init; }
    public required string EntityId { get; init; }
    public required double Value { get; init; }
    public string? Orientation { get; init; }
}

public sealed record SketchStateModel
{
    public required string Id { get; init; }
    public required string Plane { get; init; }
    public bool IsOpen { get; init; }
    public List<SketchEntityModel> Entities { get; init; } = [];
    public List<SketchDimensionModel> Dimensions { get; init; } = [];
}

public sealed record FeatureStateModel
{
    public required string Id { get; init; }
    public required string Kind { get; init; }
    public string? SketchId { get; init; }
    public double? Depth { get; init; }
    public bool? MergeResult { get; init; }
    public string? FeatureId { get; init; }
    public double? Radius { get; init; }
}

public sealed record ExportArtifactModel
{
    public required string Kind { get; init; }
    public required string Path { get; init; }
}

public sealed record ProjectRuntimeStateModel
{
    public string BackendMode { get; init; } = "solidworks";
    public string BaselineVersion { get; init; } = "SolidWorks 2022";
    public List<string> AvailableTools { get; init; } = [];
    public PartDocumentStateModel? CurrentDocument { get; init; }
}

public sealed record PartDocumentStateModel
{
    public string DocumentType { get; init; } = "part";
    public string Name { get; init; } = "Untitled";
    public string Units { get; init; } = "mm";
    public string? SelectedPlane { get; init; }
    public string? ActiveSketchId { get; init; }
    public List<SketchStateModel> Sketches { get; init; } = [];
    public List<FeatureStateModel> Features { get; init; } = [];
    public string? SavedPath { get; init; }
    public List<ExportArtifactModel> Exports { get; init; } = [];
    public bool Modified { get; init; }
    public string BaselineVersion { get; init; } = "2022";
}
