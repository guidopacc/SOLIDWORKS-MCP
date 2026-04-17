using SolidWorksMcp.SolidWorksWorker.Protocol;

namespace SolidWorksMcp.SolidWorksWorker.Services;

internal sealed record SolidWorksSessionSnapshot
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

    public SolidWorksSessionMetadata ToMetadata()
    {
        return new SolidWorksSessionMetadata
        {
            Connected = Connected,
            ConnectionMode = ConnectionMode,
            SolidWorksProgIdRegistered = SolidWorksProgIdRegistered,
            HasActiveDocument = HasActiveDocument,
            ActiveDocumentKind = ActiveDocumentKind,
            ActiveDocumentName = ActiveDocumentName,
            ActiveDocumentPath = ActiveDocumentPath,
            ActiveDocumentModified = ActiveDocumentModified,
            SolidWorksMajorVersion = SolidWorksMajorVersion,
            RevisionNumber = RevisionNumber,
            BaselineSatisfied = BaselineSatisfied
        };
    }
}
