using System.Runtime.InteropServices;

namespace SolidWorksMcp.SolidWorksWorker.Infrastructure;

internal static class RunningObjectTable
{
    private const int MkEObjectUnavailable = unchecked((int)0x800401E3);

    public static bool TryGetActiveObject(string progId, out object? activeObject)
    {
        activeObject = null;

        var clsidResult = CLSIDFromProgID(progId, out var classId);
        if (clsidResult < 0)
        {
            return false;
        }

        var hresult = GetActiveObject(ref classId, IntPtr.Zero, out var instance);
        if (hresult == 0)
        {
            activeObject = instance;
            return true;
        }

        if (hresult == MkEObjectUnavailable)
        {
            return false;
        }

        Marshal.ThrowExceptionForHR(hresult);
        return false;
    }

    [DllImport("ole32.dll", CharSet = CharSet.Unicode)]
    private static extern int CLSIDFromProgID(string progId, out Guid classId);

    [DllImport("oleaut32.dll")]
    private static extern int GetActiveObject(
        ref Guid classId,
        IntPtr reserved,
        [MarshalAs(UnmanagedType.IUnknown)] out object activeObject);
}
