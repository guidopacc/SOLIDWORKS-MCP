using System.Runtime.InteropServices;

namespace SolidWorksMcp.SolidWorksWorker.Errors;

internal static class WorkerErrorFactory
{
    public static WorkerCommandException SolidWorksNotInstalled()
    {
        return new WorkerCommandException(
            code: "unsupported_operation",
            message: "SolidWorks is not installed or the COM ProgID is not registered on this machine.",
            details: CreateDetails(
                "solidworks_not_installed",
                "session_bootstrap"));
    }

    public static WorkerCommandException SolidWorksUnavailable(Exception? innerException = null)
    {
        return new WorkerCommandException(
            code: "internal_error",
            message: "SolidWorks is installed but could not be attached to or launched through COM automation.",
            retryable: true,
            details: CreateDetails(
                "solidworks_unavailable",
                "session_bootstrap",
                innerException),
            innerException: innerException);
    }

    public static WorkerCommandException ComInitializationFailed(
        int hresult,
        Exception? innerException = null)
    {
        return new WorkerCommandException(
            code: "internal_error",
            message: "The worker could not initialize the COM apartment required for SolidWorks automation.",
            retryable: true,
            details: CreateDetails(
                "com_initialization_failed",
                "com_init",
                innerException,
                new Dictionary<string, object?>
                {
                    ["hresult"] = hresult
                }),
            innerException: innerException);
    }

    public static WorkerCommandException VersionNotDetectable(
        string? revisionNumber,
        Exception? innerException = null)
    {
        return new WorkerCommandException(
            code: "internal_error",
            message: "The worker connected to SolidWorks but could not determine the installed version.",
            retryable: false,
            details: CreateDetails(
                "version_not_detectable",
                "version_detection",
                innerException,
                new Dictionary<string, object?>
                {
                    ["revisionNumber"] = revisionNumber
                }),
            innerException: innerException);
    }

    public static WorkerCommandException BaselineVersionNotSatisfied(
        int detectedMajorVersion,
        int expectedMajorVersion)
    {
        return new WorkerCommandException(
            code: "unsupported_operation",
            message: $"Detected SolidWorks {detectedMajorVersion}, which is older than the required baseline {expectedMajorVersion}.",
            details: CreateDetails(
                "baseline_version_not_satisfied",
                "version_detection",
                extra: new Dictionary<string, object?>
                {
                    ["detectedMajorVersion"] = detectedMajorVersion,
                    ["expectedMajorVersion"] = expectedMajorVersion
                }));
    }

    public static WorkerCommandException NoActiveDocument(string? activeDocumentKind = null)
    {
        return new WorkerCommandException(
            code: "precondition_failed",
            message: "No active part document is available in SolidWorks.",
            details: CreateDetails(
                "no_active_document",
                "document_precondition",
                extra: new Dictionary<string, object?>
                {
                    ["activeDocumentKind"] = activeDocumentKind
                }));
    }

    public static WorkerCommandException UnableToCreateNewPart(
        string? templatePath,
        string reason,
        Exception? innerException = null,
        bool resyncRequired = true)
    {
        return new WorkerCommandException(
            code: "internal_error",
            message: "SolidWorks could not create a new part document.",
            retryable: false,
            resyncRequired: resyncRequired,
            captureObservedState: true,
            details: CreateDetails(
                "unable_to_create_new_part",
                "new_part",
                innerException,
                new Dictionary<string, object?>
                {
                    ["templatePath"] = templatePath,
                    ["reason"] = reason
                }),
            innerException: innerException);
    }

    public static WorkerCommandException DefaultPartTemplateNotConfigured(
        string? templatePath)
    {
        return new WorkerCommandException(
            code: "precondition_failed",
            message: "SolidWorks does not expose a usable default part template for automation.",
            details: CreateDetails(
                "default_part_template_not_configured",
                "new_part",
                extra: new Dictionary<string, object?>
                {
                    ["templatePath"] = templatePath
                }));
    }

    public static WorkerCommandException InvalidPlaneSelection(string plane)
    {
        return new WorkerCommandException(
            code: "invalid_input",
            message: $"Plane '{plane}' is not supported in the current worker slice.",
            details: CreateDetails(
                "invalid_plane_selection",
                "select_plane",
                extra: new Dictionary<string, object?>
                {
                    ["plane"] = plane,
                    ["supportedPlanes"] = new[]
                    {
                        Services.SolidWorksApiConstants.FrontPlaneName,
                        Services.SolidWorksApiConstants.TopPlaneName,
                        Services.SolidWorksApiConstants.RightPlaneName
                    }
                }));
    }

    public static WorkerCommandException NoPlaneSelected()
    {
        return new WorkerCommandException(
            code: "precondition_failed",
            message: "A standard plane must be selected before starting a sketch.",
            details: CreateDetails(
                "no_plane_selected",
                "sketch_precondition"));
    }

    public static WorkerCommandException ActiveSketchAlreadyOpen(string? activeSketchId)
    {
        return new WorkerCommandException(
            code: "precondition_failed",
            message: "An active sketch is already open.",
            details: CreateDetails(
                "active_sketch_already_open",
                "sketch_precondition",
                extra: new Dictionary<string, object?>
                {
                    ["activeSketchId"] = activeSketchId
                }));
    }

    public static WorkerCommandException NoActiveSketch(string stage)
    {
        return new WorkerCommandException(
            code: "precondition_failed",
            message: "No active sketch is open in SolidWorks.",
            details: CreateDetails(
                "no_active_sketch",
                stage));
    }

    public static WorkerCommandException SketchNotFound(string sketchId)
    {
        return new WorkerCommandException(
            code: "not_found",
            message: $"Sketch '{sketchId}' was not found in the current document state.",
            details: CreateDetails(
                "sketch_not_found",
                "extrude_boss",
                extra: new Dictionary<string, object?>
                {
                    ["sketchId"] = sketchId
                }));
    }

    public static WorkerCommandException SketchEntityNotFound(string entityId)
    {
        return new WorkerCommandException(
            code: "not_found",
            message: $"Sketch entity '{entityId}' was not found in the active sketch state.",
            details: CreateDetails(
                "sketch_entity_not_found",
                "add_dimension",
                extra: new Dictionary<string, object?>
                {
                    ["entityId"] = entityId
                }));
    }

    public static WorkerCommandException SketchMustBeClosed(
        string sketchId,
        string command)
    {
        return new WorkerCommandException(
            code: "precondition_failed",
            message: "The target sketch must be closed before creating a solid feature.",
            details: CreateDetails(
                "sketch_must_be_closed",
                command,
                extra: new Dictionary<string, object?>
                {
                    ["sketchId"] = sketchId
                }));
    }

    public static WorkerCommandException UnableToSelectPlane(
        string plane,
        string reason,
        Exception? innerException = null)
    {
        return new WorkerCommandException(
            code: "internal_error",
            message: "SolidWorks could not select the requested standard plane.",
            retryable: false,
            captureObservedState: true,
            details: CreateDetails(
                "unable_to_select_plane",
                "select_plane",
                innerException,
                new Dictionary<string, object?>
                {
                    ["plane"] = plane,
                    ["reason"] = reason
                }),
            innerException: innerException);
    }

    public static WorkerCommandException UnableToStartSketch(
        string plane,
        string reason,
        Exception? innerException = null)
    {
        return new WorkerCommandException(
            code: "internal_error",
            message: "SolidWorks could not start a new sketch on the requested plane.",
            retryable: false,
            captureObservedState: true,
            details: CreateDetails(
                "unable_to_start_sketch",
                "start_sketch",
                innerException,
                new Dictionary<string, object?>
                {
                    ["plane"] = plane,
                    ["reason"] = reason
                }),
            innerException: innerException);
    }

    public static WorkerCommandException UnableToDrawCircle(
        string sketchId,
        double radius,
        string reason,
        Exception? innerException = null)
    {
        return new WorkerCommandException(
            code: "internal_error",
            message: "SolidWorks could not create the requested circle in the active sketch.",
            retryable: false,
            captureObservedState: true,
            details: CreateDetails(
                "unable_to_draw_circle",
                "draw_circle",
                innerException,
                new Dictionary<string, object?>
                {
                    ["sketchId"] = sketchId,
                    ["radius"] = radius,
                    ["reason"] = reason
                }),
            innerException: innerException);
    }

    public static WorkerCommandException UnableToDrawLine(
        string sketchId,
        Dictionary<string, object?> geometry,
        string reason,
        Exception? innerException = null)
    {
        return new WorkerCommandException(
            code: "internal_error",
            message: "SolidWorks could not create the requested line in the active sketch.",
            retryable: false,
            captureObservedState: true,
            details: CreateDetails(
                "unable_to_draw_line",
                "draw_line",
                innerException,
                new Dictionary<string, object?>(geometry)
                {
                    ["sketchId"] = sketchId,
                    ["reason"] = reason
                }),
            innerException: innerException);
    }

    public static WorkerCommandException UnableToDrawCenteredRectangle(
        string sketchId,
        Dictionary<string, object?> geometry,
        string reason,
        Exception? innerException = null)
    {
        return new WorkerCommandException(
            code: "internal_error",
            message: "SolidWorks could not create the requested centered rectangle in the active sketch.",
            retryable: false,
            captureObservedState: true,
            details: CreateDetails(
                "unable_to_draw_centered_rectangle",
                "draw_centered_rectangle",
                innerException,
                new Dictionary<string, object?>(geometry)
                {
                    ["sketchId"] = sketchId,
                    ["reason"] = reason
                }),
            innerException: innerException);
    }

    public static WorkerCommandException DimensionEntityNotSupported(
        string entityId,
        string entityKind,
        string? requestedOrientation)
    {
        return new WorkerCommandException(
            code: "unsupported_operation",
            message: "The current worker slice only supports horizontal or vertical dimensions on line entities.",
            details: CreateDetails(
                "dimension_entity_not_supported",
                "add_dimension",
                extra: new Dictionary<string, object?>
                {
                    ["entityId"] = entityId,
                    ["entityKind"] = entityKind,
                    ["requestedOrientation"] = requestedOrientation
                }));
    }

    public static WorkerCommandException InvalidDimensionOrientation(
        string entityId,
        string? requestedOrientation,
        string reason)
    {
        return new WorkerCommandException(
            code: "invalid_input",
            message: "The requested dimension orientation is not valid for the target sketch entity.",
            details: CreateDetails(
                "invalid_dimension_orientation",
                "add_dimension",
                extra: new Dictionary<string, object?>
                {
                    ["entityId"] = entityId,
                    ["requestedOrientation"] = requestedOrientation,
                    ["reason"] = reason
                }));
    }

    public static WorkerCommandException UnableToAddDimension(
        string sketchId,
        string entityId,
        Dictionary<string, object?> geometry,
        string reason,
        Exception? innerException = null)
    {
        return new WorkerCommandException(
            code: "internal_error",
            message: "SolidWorks could not add the requested sketch dimension.",
            retryable: false,
            captureObservedState: true,
            details: CreateDetails(
                "unable_to_add_dimension",
                "add_dimension",
                innerException,
                new Dictionary<string, object?>(geometry)
                {
                    ["sketchId"] = sketchId,
                    ["entityId"] = entityId,
                    ["reason"] = reason
                }),
            innerException: innerException);
    }

    public static WorkerCommandException UnableToCloseSketch(
        string sketchId,
        string reason,
        Exception? innerException = null)
    {
        return new WorkerCommandException(
            code: "internal_error",
            message: "SolidWorks could not close the active sketch cleanly.",
            retryable: false,
            captureObservedState: true,
            details: CreateDetails(
                "unable_to_close_sketch",
                "close_sketch",
                innerException,
                new Dictionary<string, object?>
                {
                    ["sketchId"] = sketchId,
                    ["reason"] = reason
                }),
            innerException: innerException);
    }

    public static WorkerCommandException UnableToExtrudeBoss(
        string sketchId,
        double depth,
        string reason,
        Exception? innerException = null)
    {
        return new WorkerCommandException(
            code: "internal_error",
            message: "SolidWorks could not create the requested boss extrusion.",
            retryable: false,
            captureObservedState: true,
            details: CreateDetails(
                "unable_to_extrude_boss",
                "extrude_boss",
                innerException,
                new Dictionary<string, object?>
                {
                    ["sketchId"] = sketchId,
                    ["depth"] = depth,
                    ["reason"] = reason
                }),
            innerException: innerException);
    }

    public static WorkerCommandException InvalidExportPath(
        string? path,
        string reason,
        Exception? innerException = null)
    {
        return new WorkerCommandException(
            code: "invalid_input",
            message: "The STEP export path is invalid for the current worker slice.",
            details: CreateDetails(
                "invalid_export_path",
                "export_step",
                innerException,
                new Dictionary<string, object?>
                {
                    ["path"] = path,
                    ["reason"] = reason
                }),
            innerException: innerException);
    }

    public static WorkerCommandException ExportPathNotWritable(
        string path,
        Exception? innerException = null)
    {
        return new WorkerCommandException(
            code: "precondition_failed",
            message: "The STEP export target path is not writable.",
            details: CreateDetails(
                "export_path_not_writable",
                "export_step",
                innerException,
                new Dictionary<string, object?>
                {
                    ["path"] = path
                }),
            innerException: innerException);
    }

    public static WorkerCommandException DocumentNotExportableInCurrentSlice(
        string? activeDocumentKind,
        string command)
    {
        return new WorkerCommandException(
            code: "unsupported_operation",
            message: "Only part documents are exportable as STEP in the current worker slice.",
            details: CreateDetails(
                "document_not_exportable_in_current_slice",
                command,
                extra: new Dictionary<string, object?>
                {
                    ["activeDocumentKind"] = activeDocumentKind,
                    ["supportedDocumentKinds"] = new[] { "part" }
                }));
    }

    public static WorkerCommandException InvalidInput(
        string message,
        string stage,
        Dictionary<string, object?>? extra = null)
    {
        return new WorkerCommandException(
            code: "invalid_input",
            message: message,
            details: CreateDetails("invalid_input", stage, extra: extra));
    }

    public static WorkerCommandException UnableToSaveDocument(
        string path,
        string saveMode,
        int? errors,
        int? warnings,
        string reason,
        Exception? innerException = null,
        bool resyncRequired = true)
    {
        return new WorkerCommandException(
            code: "internal_error",
            message: "SolidWorks could not save the active part document.",
            retryable: false,
            resyncRequired: resyncRequired,
            captureObservedState: true,
            details: CreateDetails(
                "unable_to_save_document",
                "save_part",
                innerException,
                new Dictionary<string, object?>
                {
                    ["path"] = path,
                    ["saveMode"] = saveMode,
                    ["solidWorksErrors"] = errors,
                    ["solidWorksWarnings"] = warnings,
                    ["reason"] = reason
                }),
            innerException: innerException);
    }

    public static WorkerCommandException UnableToExportStep(
        string path,
        string exportMode,
        int? errors,
        int? warnings,
        string reason,
        Exception? innerException = null,
        bool resyncRequired = false)
    {
        return new WorkerCommandException(
            code: "internal_error",
            message: "SolidWorks could not export the active part document to STEP.",
            retryable: false,
            resyncRequired: resyncRequired,
            captureObservedState: true,
            details: CreateDetails(
                "unable_to_export_step",
                "export_step",
                innerException,
                new Dictionary<string, object?>
                {
                    ["path"] = path,
                    ["exportMode"] = exportMode,
                    ["solidWorksErrors"] = errors,
                    ["solidWorksWarnings"] = warnings,
                    ["reason"] = reason
                }),
            innerException: innerException);
    }

    public static WorkerCommandException CommandNotSupportedInCurrentSlice(
        string kind)
    {
        return new WorkerCommandException(
            code: "unsupported_operation",
            message: $"Command '{kind}' is not supported in the current real worker slice.",
            details: CreateDetails(
                "command_not_supported_in_current_slice",
                "command_dispatch",
                extra: new Dictionary<string, object?>
                {
                    ["command"] = kind,
                        ["supportedCommands"] = Protocol.WorkerProtocolConstants.SupportedCommandKinds
                }));
    }

    public static WorkerCommandException ProtocolVersionMismatch(
        string expectedProtocolVersion,
        string receivedProtocolVersion)
    {
        return new WorkerCommandException(
            code: "unsupported_operation",
            message: "The worker received an unsupported protocol version.",
            details: CreateDetails(
                "protocol_version_mismatch",
                "protocol_dispatch",
                extra: new Dictionary<string, object?>
                {
                    ["expectedProtocolVersion"] = expectedProtocolVersion,
                    ["receivedProtocolVersion"] = receivedProtocolVersion
                }));
    }

    public static WorkerCommandException Unexpected(
        string stage,
        Exception innerException,
        bool captureObservedState = true,
        bool resyncRequired = true)
    {
        return new WorkerCommandException(
            code: "internal_error",
            message: "The worker encountered an unexpected exception.",
            retryable: true,
            resyncRequired: resyncRequired,
            captureObservedState: captureObservedState,
            details: CreateDetails(
                "unexpected_exception",
                stage,
                innerException),
            innerException: innerException);
    }

    private static Dictionary<string, object?> CreateDetails(
        string classification,
        string stage,
        Exception? innerException = null,
        Dictionary<string, object?>? extra = null)
    {
        var details = new Dictionary<string, object?>
        {
            ["classification"] = classification,
            ["stage"] = stage
        };

        if (innerException is not null)
        {
            details["exceptionType"] = innerException.GetType().FullName;
            details["exceptionMessage"] = innerException.Message;
            if (innerException is COMException comException)
            {
                details["hresult"] = comException.HResult;
            }
        }

        if (extra is not null)
        {
            foreach (var pair in extra)
            {
                details[pair.Key] = pair.Value;
            }
        }

        return details;
    }
}
