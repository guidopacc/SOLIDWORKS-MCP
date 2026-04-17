using System.Diagnostics;
using SolidWorksMcp.SolidWorksWorker.Errors;
using SolidWorksMcp.SolidWorksWorker.Mapping;
using SolidWorksMcp.SolidWorksWorker.Protocol;
using SolidWorksMcp.SolidWorksWorker.Services;

namespace SolidWorksMcp.SolidWorksWorker.Commands;

internal sealed class WorkerCommandDispatcher
{
    private readonly SolidWorksSessionService sessionService;
    private readonly SolidWorksDocumentService documentService;
    private readonly WorkerStateMapper stateMapper;

    public WorkerCommandDispatcher(
        SolidWorksSessionService sessionService,
        SolidWorksDocumentService documentService,
        WorkerStateMapper stateMapper)
    {
        this.sessionService = sessionService;
        this.documentService = documentService;
        this.stateMapper = stateMapper;
    }

    public WorkerResponse Dispatch(WorkerRequest request)
    {
        return request switch
        {
            HandshakeRequest handshakeRequest => HandleHandshake(handshakeRequest),
            ExecuteCommandRequest executeCommandRequest => HandleExecuteCommand(executeCommandRequest),
            ShutdownRequest shutdownRequest => HandleShutdown(shutdownRequest),
            UnsupportedWorkerRequest unsupportedRequest => HandleUnsupportedRequest(unsupportedRequest),
            _ => throw WorkerErrorFactory.Unexpected(
                "command_dispatch",
                new InvalidOperationException(
                    $"Unhandled worker request type {request.GetType().Name}."),
                captureObservedState: false,
                resyncRequired: false)
        };
    }

    private HandshakeResponse HandleHandshake(HandshakeRequest request)
    {
        return new HandshakeResponse
        {
            MessageType = "handshake_response",
            RequestId = request.RequestId,
            ProtocolVersion = WorkerProtocolConstants.ProtocolVersion,
            Worker = new WorkerIdentity
            {
                Name = WorkerProtocolConstants.WorkerName,
                Version = WorkerProtocolConstants.WorkerVersion
            },
            SupportedSolidWorksMajorVersions =
                WorkerProtocolConstants.SupportedSolidWorksMajorVersions,
            Capabilities = new WorkerCapabilities
            {
                SupportsStateRoundTrip = true,
                SupportsObservedStateOnFailure = true
            },
            BackendMetadata = CreateBackendMetadata()
        };
    }

    private WorkerResponse HandleExecuteCommand(ExecuteCommandRequest request)
    {
        if (!string.Equals(
                request.ProtocolVersion,
                WorkerProtocolConstants.ProtocolVersion,
                StringComparison.Ordinal))
        {
            var error = WorkerErrorFactory.ProtocolVersionMismatch(
                WorkerProtocolConstants.ProtocolVersion,
                request.ProtocolVersion);
            return CreateFailureResponse(request, error);
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            DocumentOperationResult operationResult = request.Command switch
            {
                NewPartCommand newPartCommand => documentService.CreateNewPart(
                    newPartCommand.Name,
                    request.DesiredSolidWorksMajorVersion),
                SelectPlaneCommand selectPlaneCommand => documentService.SelectPlane(
                    request.StateBefore,
                    selectPlaneCommand.Plane,
                    request.DesiredSolidWorksMajorVersion),
                StartSketchCommand => documentService.StartSketch(
                    request.StateBefore,
                    request.DesiredSolidWorksMajorVersion),
                DrawLineCommand drawLineCommand => documentService.DrawLine(
                    request.StateBefore,
                    drawLineCommand,
                    request.DesiredSolidWorksMajorVersion),
                DrawCircleCommand drawCircleCommand => documentService.DrawCircle(
                    request.StateBefore,
                    drawCircleCommand,
                    request.DesiredSolidWorksMajorVersion),
                DrawCenteredRectangleCommand drawCenteredRectangleCommand => documentService.DrawCenteredRectangle(
                    request.StateBefore,
                    drawCenteredRectangleCommand,
                    request.DesiredSolidWorksMajorVersion),
                CloseSketchCommand => documentService.CloseSketch(
                    request.StateBefore,
                    request.DesiredSolidWorksMajorVersion),
                ExtrudeBossCommand extrudeBossCommand => documentService.ExtrudeBoss(
                    request.StateBefore,
                    extrudeBossCommand,
                    request.DesiredSolidWorksMajorVersion),
                SavePartCommand savePartCommand => documentService.SavePart(
                    request.StateBefore,
                    savePartCommand.Path,
                    request.DesiredSolidWorksMajorVersion),
                ExportStepCommand exportStepCommand => documentService.ExportStep(
                    request.StateBefore,
                    exportStepCommand.Path,
                    request.DesiredSolidWorksMajorVersion),
                UnsupportedCadCommand unsupportedCadCommand =>
                    throw WorkerErrorFactory.CommandNotSupportedInCurrentSlice(
                        unsupportedCadCommand.Kind),
                _ => throw WorkerErrorFactory.CommandNotSupportedInCurrentSlice(
                    request.Command.Kind)
            };

            stopwatch.Stop();

            return new ExecuteCommandSuccessResponse
            {
                MessageType = "execute_command_response",
                RequestId = request.RequestId,
                ProtocolVersion = WorkerProtocolConstants.ProtocolVersion,
                NextState = stateMapper.Map(
                    request.StateBefore,
                    operationResult.Session,
                    request.Command,
                    operationResult.OperationDetails),
                Execution = new WorkerExecutionMetadata
                {
                    BackendName = WorkerProtocolConstants.BackendName,
                    DurationMs = stopwatch.ElapsedMilliseconds,
                    SolidWorksMajorVersion = operationResult.Session.SolidWorksMajorVersion,
                    BackendMetadata = CreateBackendMetadata(),
                    Session = operationResult.Session.ToMetadata(),
                    OperationDetails = operationResult.OperationDetails
                }
            };
        }
        catch (WorkerCommandException error)
        {
            stopwatch.Stop();
            return CreateFailureResponse(request, error);
        }
        catch (Exception error)
        {
            stopwatch.Stop();
            return CreateFailureResponse(
                request,
                WorkerErrorFactory.Unexpected("execute_command", error));
        }
    }

    private ShutdownResponse HandleShutdown(ShutdownRequest request)
    {
        return new ShutdownResponse
        {
            MessageType = "shutdown_response",
            RequestId = request.RequestId,
            ProtocolVersion = WorkerProtocolConstants.ProtocolVersion
        };
    }

    private ExecuteCommandFailureResponse HandleUnsupportedRequest(
        UnsupportedWorkerRequest request)
    {
        var error = WorkerErrorFactory.InvalidInput(
            $"Unsupported worker message type '{request.UnsupportedMessageType}'.",
            "protocol_dispatch",
            new Dictionary<string, object?>
            {
                ["messageType"] = request.UnsupportedMessageType
            });

        return new ExecuteCommandFailureResponse
        {
            MessageType = "execute_command_response",
            RequestId = request.RequestId,
            ProtocolVersion = WorkerProtocolConstants.ProtocolVersion,
            Error = CreateErrorPayload(error, request.RequestId, null),
            ResyncRequired = false
        };
    }

    private ExecuteCommandFailureResponse CreateFailureResponse(
        ExecuteCommandRequest request,
        WorkerCommandException error)
    {
        var observedSession = sessionService.TryCaptureSnapshot(
            connectIfNeeded: false,
            request.DesiredSolidWorksMajorVersion);

        error.Details["backendMetadata"] = CreateBackendMetadata();
        error.Details["session"] = observedSession.ToMetadata();
        error.Details["requestId"] = request.RequestId;

        ProjectRuntimeStateModel? observedState = null;
        if (error.CaptureObservedState || error.ResyncRequired)
        {
            observedState = stateMapper.Map(request.StateBefore, observedSession);
        }

        return new ExecuteCommandFailureResponse
        {
            MessageType = "execute_command_response",
            RequestId = request.RequestId,
            ProtocolVersion = WorkerProtocolConstants.ProtocolVersion,
            Error = CreateErrorPayload(error, request.RequestId, observedSession),
            ObservedState = observedState,
            ResyncRequired = error.ResyncRequired
        };
    }

    private static WorkerErrorPayload CreateErrorPayload(
        WorkerCommandException error,
        string requestId,
        SolidWorksSessionSnapshot? observedSession)
    {
        var details = new Dictionary<string, object?>(error.Details)
        {
            ["requestId"] = requestId
        };

        if (observedSession is not null)
        {
            details["session"] = observedSession.ToMetadata();
        }

        return new WorkerErrorPayload
        {
            Code = error.Code,
            Message = error.Message,
            Retryable = error.Retryable,
            Details = details
        };
    }

    private WorkerBackendMetadata CreateBackendMetadata()
    {
        return new WorkerBackendMetadata
        {
            BackendName = WorkerProtocolConstants.BackendName,
            SliceName = WorkerProtocolConstants.SliceName,
            Runtime = "net8.0-windows",
            UsesLateBoundCom = true,
            SupportsAttachFirst = true,
            SupportsFileExistenceVerification = true,
            SupportsSessionMetadataOnExecution = true,
            SolidWorksProgIdRegistered = sessionService.IsSolidWorksRegistered(),
            SupportedCommands = WorkerProtocolConstants.SupportedCommandKinds
        };
    }
}
