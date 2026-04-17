using SolidWorksMcp.SolidWorksWorker.Protocol;

namespace SolidWorksWorker.Tests;

public sealed class WorkerProtocolConstantsTests
{
    [Fact]
    public void SupportedCommandKinds_IncludeDrawLineButNotAddDimension()
    {
        Assert.Contains("draw_line", WorkerProtocolConstants.SupportedCommandKinds);
        Assert.DoesNotContain("add_dimension", WorkerProtocolConstants.SupportedCommandKinds);
    }

    [Fact]
    public void SliceName_ReflectsCurrentLevel1RealModelingMilestone()
    {
        Assert.Equal("level1-real-modeling-v1", WorkerProtocolConstants.SliceName);
    }
}
