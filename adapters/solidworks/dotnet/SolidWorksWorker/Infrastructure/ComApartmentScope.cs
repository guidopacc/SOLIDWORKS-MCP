using System.Runtime.InteropServices;
using SolidWorksMcp.SolidWorksWorker.Errors;

namespace SolidWorksMcp.SolidWorksWorker.Infrastructure;

internal sealed class ComApartmentScope : IDisposable
{
    private const uint CoinitApartmentThreaded = 0x2;

    private bool disposed;

    public ComApartmentScope()
    {
        var hresult = CoInitializeEx(IntPtr.Zero, CoinitApartmentThreaded);
        if (hresult < 0)
        {
            throw WorkerErrorFactory.ComInitializationFailed(hresult);
        }
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        CoUninitialize();
    }

    [DllImport("ole32.dll")]
    private static extern int CoInitializeEx(IntPtr reserved, uint coInit);

    [DllImport("ole32.dll")]
    private static extern void CoUninitialize();
}
