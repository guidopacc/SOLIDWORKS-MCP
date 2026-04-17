using SolidWorksMcp.SolidWorksWorker.Infrastructure;

namespace SolidWorksWorker.Tests;

public sealed class ComInteropTests
{
    [Fact]
    public void InvokeWithByRefArgs_UpdatesByRefOutputs()
    {
        var target = new ByRefProbe();
        var args = new object?[] { 5, 0, 0 };

        var result = ComInterop.InvokeWithByRefArgs(
            target,
            nameof(ByRefProbe.Execute),
            args,
            1,
            2);

        Assert.True(Assert.IsType<bool>(result));
        Assert.Equal(15, Assert.IsType<int>(args[1]));
        Assert.Equal(25, Assert.IsType<int>(args[2]));
    }

    private sealed class ByRefProbe
    {
        public bool Execute(int input, ref int errors, ref int warnings)
        {
            errors = input + 10;
            warnings = input + 20;
            return true;
        }
    }
}
