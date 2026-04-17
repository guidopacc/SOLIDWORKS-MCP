using System.Diagnostics;
using System.Runtime.InteropServices;
using SolidWorksMcp.SolidWorksWorker.Errors;
using SolidWorksMcp.SolidWorksWorker.Infrastructure;
using SolidWorksMcp.SolidWorksWorker.Protocol;

namespace SolidWorksMcp.SolidWorksWorker.Services;

internal sealed class SolidWorksSessionService : IDisposable
{
    private ComApartmentScope? comApartmentScope;
    private object? application;
    private string connectionMode = "not_connected";
    private bool disposed;

    public bool IsSolidWorksRegistered()
    {
        return Type.GetTypeFromProgID(SolidWorksApiConstants.ProgId, throwOnError: false) is not null;
    }

    public object EnsureApplication(int desiredMajorVersion)
    {
        ThrowIfDisposed();

        if (application is not null)
        {
            EnsureBaselineSatisfied(application, desiredMajorVersion);
            return application;
        }

        EnsureComApartment();

        var applicationType = Type.GetTypeFromProgID(
            SolidWorksApiConstants.ProgId,
            throwOnError: false);

        if (applicationType is null)
        {
            throw WorkerErrorFactory.SolidWorksNotInstalled();
        }

        try
        {
            var solidWorksProcessIdsBeforeActivation = GetSolidWorksProcessIds();

            if (RunningObjectTable.TryGetActiveObject(
                SolidWorksApiConstants.ProgId,
                out var activeObject) &&
                activeObject is not null)
            {
                application = activeObject;
                connectionMode = "attached_existing";
            }
            else
            {
                application = Activator.CreateInstance(applicationType);
                if (application is null)
                {
                    throw WorkerErrorFactory.SolidWorksUnavailable();
                }

                connectionMode = DetermineConnectionModeAfterActivation(
                    solidWorksProcessIdsBeforeActivation,
                    GetSolidWorksProcessIds());
            }
        }
        catch (WorkerCommandException)
        {
            throw;
        }
        catch (COMException error)
        {
            throw WorkerErrorFactory.SolidWorksUnavailable(error);
        }
        catch (Exception error)
        {
            throw WorkerErrorFactory.SolidWorksUnavailable(error);
        }

        try
        {
            ComInterop.TrySetProperty(application, "Visible", true);
            ComInterop.TrySetProperty(application, "UserControl", true);
            EnsureBaselineSatisfied(application, desiredMajorVersion);
            return application;
        }
        catch
        {
            ComInterop.ReleaseIfComObject(application);
            application = null;
            connectionMode = "not_connected";
            throw;
        }
    }

    public SolidWorksSessionSnapshot TryCaptureSnapshot(
        bool connectIfNeeded,
        int desiredMajorVersion)
    {
        ThrowIfDisposed();

        try
        {
            if (connectIfNeeded)
            {
                EnsureApplication(desiredMajorVersion);
            }
            else if (application is null)
            {
                return BuildDisconnectedSnapshot();
            }

            if (application is null)
            {
                return BuildDisconnectedSnapshot();
            }

            return CaptureConnectedSnapshot(application, desiredMajorVersion);
        }
        catch (WorkerCommandException)
        {
            return BuildDisconnectedSnapshot();
        }
        catch
        {
            return BuildDisconnectedSnapshot();
        }
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        ComInterop.ReleaseIfComObject(application);
        application = null;

        comApartmentScope?.Dispose();
        comApartmentScope = null;
    }

    private void EnsureComApartment()
    {
        comApartmentScope ??= new ComApartmentScope();
    }

    private void EnsureBaselineSatisfied(object solidWorksApplication, int desiredMajorVersion)
    {
        var snapshot = CaptureConnectedSnapshot(solidWorksApplication, desiredMajorVersion);
        if (snapshot.SolidWorksMajorVersion is null)
        {
            throw WorkerErrorFactory.VersionNotDetectable(snapshot.RevisionNumber);
        }

        if (snapshot.SolidWorksMajorVersion < desiredMajorVersion)
        {
            throw WorkerErrorFactory.BaselineVersionNotSatisfied(
                snapshot.SolidWorksMajorVersion.Value,
                desiredMajorVersion);
        }
    }

    private SolidWorksSessionSnapshot CaptureConnectedSnapshot(
        object solidWorksApplication,
        int desiredMajorVersion)
    {
        string? revisionNumber = null;
        int? solidWorksMajorVersion = null;
        object? activeDocument = null;

        try
        {
            revisionNumber = ComInterop.InvokeString(
                solidWorksApplication,
                "RevisionNumber");
            solidWorksMajorVersion = ParseMajorVersion(revisionNumber);

            activeDocument = ComInterop.GetProperty<object?>(
                solidWorksApplication,
                "ActiveDoc");

            if (activeDocument is null)
            {
                return new SolidWorksSessionSnapshot
                {
                    Connected = true,
                    ConnectionMode = connectionMode,
                    SolidWorksProgIdRegistered = IsSolidWorksRegistered(),
                    HasActiveDocument = false,
                    SolidWorksMajorVersion = solidWorksMajorVersion,
                    RevisionNumber = revisionNumber,
                    BaselineSatisfied = solidWorksMajorVersion is null
                        ? false
                        : solidWorksMajorVersion >= desiredMajorVersion
                };
            }

            var activeDocumentName = ComInterop.InvokeString(activeDocument, "GetTitle");
            var activeDocumentPath = ComInterop.InvokeString(activeDocument, "GetPathName");
            var activeDocumentModified = TryReadModifiedFlag(activeDocument);
            var activeDocumentKind = InferDocumentKindFromPath(activeDocumentPath);

            return new SolidWorksSessionSnapshot
            {
                Connected = true,
                ConnectionMode = connectionMode,
                SolidWorksProgIdRegistered = IsSolidWorksRegistered(),
                HasActiveDocument = true,
                ActiveDocumentKind = activeDocumentKind,
                ActiveDocumentName = activeDocumentName,
                ActiveDocumentPath = string.IsNullOrWhiteSpace(activeDocumentPath)
                    ? null
                    : activeDocumentPath,
                ActiveDocumentModified = activeDocumentModified,
                SolidWorksMajorVersion = solidWorksMajorVersion,
                RevisionNumber = revisionNumber,
                BaselineSatisfied = solidWorksMajorVersion is null
                    ? false
                    : solidWorksMajorVersion >= desiredMajorVersion
            };
        }
        catch (WorkerCommandException)
        {
            throw;
        }
        catch (Exception error)
        {
            if (revisionNumber is not null)
            {
                throw WorkerErrorFactory.VersionNotDetectable(revisionNumber, error);
            }

            throw WorkerErrorFactory.Unexpected("session_snapshot", error);
        }
        finally
        {
            ComInterop.ReleaseIfComObject(activeDocument);
        }
    }

    private SolidWorksSessionSnapshot BuildDisconnectedSnapshot()
    {
        return new SolidWorksSessionSnapshot
        {
            Connected = false,
            ConnectionMode = "not_connected",
            SolidWorksProgIdRegistered = IsSolidWorksRegistered(),
            HasActiveDocument = false,
            BaselineSatisfied = false
        };
    }

    private static int? ParseMajorVersion(string? revisionNumber)
    {
        if (string.IsNullOrWhiteSpace(revisionNumber))
        {
            return null;
        }

        var segments = revisionNumber.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0 || !int.TryParse(segments[0], out var revisionMajor))
        {
            return null;
        }

        if (revisionMajor < 8)
        {
            return null;
        }

        return revisionMajor + 1992;
    }

    private static bool? TryReadModifiedFlag(object activeDocument)
    {
        try
        {
            var result = ComInterop.Invoke(activeDocument, "GetSaveFlag");
            return ComInterop.ToBoolean(result);
        }
        catch
        {
            return null;
        }
    }

    private void ThrowIfDisposed()
    {
        if (disposed)
        {
            throw new ObjectDisposedException(nameof(SolidWorksSessionService));
        }
    }

    internal static string DetermineConnectionModeAfterActivation(
        IReadOnlyCollection<int> solidWorksProcessIdsBeforeActivation,
        IReadOnlyCollection<int> solidWorksProcessIdsAfterActivation)
    {
        if (solidWorksProcessIdsBeforeActivation.Count == 0)
        {
            return "launched_new";
        }

        return solidWorksProcessIdsAfterActivation
            .Except(solidWorksProcessIdsBeforeActivation)
            .Any()
            ? "launched_new"
            : "attached_existing";
    }

    private static HashSet<int> GetSolidWorksProcessIds()
    {
        return Process.GetProcessesByName("SLDWORKS")
            .Select(process => process.Id)
            .ToHashSet();
    }

    private static string? InferDocumentKindFromPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        return Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".sldprt" => "part",
            ".sldasm" => "assembly",
            ".slddrw" => "drawing",
            _ => "unknown"
        };
    }
}
