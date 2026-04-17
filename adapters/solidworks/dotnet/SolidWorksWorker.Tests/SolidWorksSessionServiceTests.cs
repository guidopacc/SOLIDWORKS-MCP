using SolidWorksMcp.SolidWorksWorker.Services;

namespace SolidWorksWorker.Tests;

public sealed class SolidWorksSessionServiceTests
{
    [Fact]
    public void DetermineConnectionModeAfterActivation_ReturnsLaunchedNew_WhenNoProcessExistedBefore()
    {
        var connectionMode = SolidWorksSessionService.DetermineConnectionModeAfterActivation(
            [],
            []);

        Assert.Equal("launched_new", connectionMode);
    }

    [Fact]
    public void DetermineConnectionModeAfterActivation_ReturnsAttachedExisting_WhenProcessAlreadyExistedAndNoNewPidAppeared()
    {
        var connectionMode = SolidWorksSessionService.DetermineConnectionModeAfterActivation(
            [17496],
            [17496]);

        Assert.Equal("attached_existing", connectionMode);
    }

    [Fact]
    public void DetermineConnectionModeAfterActivation_ReturnsLaunchedNew_WhenNewPidAppearsAfterActivation()
    {
        var connectionMode = SolidWorksSessionService.DetermineConnectionModeAfterActivation(
            [17496],
            [17496, 20312]);

        Assert.Equal("launched_new", connectionMode);
    }
}
