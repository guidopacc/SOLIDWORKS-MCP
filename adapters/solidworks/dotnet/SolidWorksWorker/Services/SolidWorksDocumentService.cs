using SolidWorksMcp.SolidWorksWorker.Errors;
using SolidWorksMcp.SolidWorksWorker.Infrastructure;
using SolidWorksMcp.SolidWorksWorker.Protocol;

namespace SolidWorksMcp.SolidWorksWorker.Services;

internal sealed class SolidWorksDocumentService
{
    private readonly SolidWorksSessionService sessionService;

    public SolidWorksDocumentService(SolidWorksSessionService sessionService)
    {
        this.sessionService = sessionService;
    }

    public DocumentOperationResult CreateNewPart(
        string? requestedName,
        int desiredMajorVersion)
    {
        var solidWorksApplication = sessionService.EnsureApplication(desiredMajorVersion);
        var templatePath = ResolveDefaultPartTemplate(solidWorksApplication);
        object? createdDocument = null;

        try
        {
            createdDocument = ComInterop.Invoke(
                solidWorksApplication,
                "NewDocument",
                templatePath,
                0,
                0d,
                0d);

            if (createdDocument is null)
            {
                throw WorkerErrorFactory.UnableToCreateNewPart(
                    templatePath,
                    "NewDocument returned null.");
            }

            var sessionSnapshot = sessionService.TryCaptureSnapshot(
                connectIfNeeded: false,
                desiredMajorVersion);

            sessionSnapshot = CoerceSnapshotDocumentKind(
                sessionSnapshot,
                "part");

            if (!sessionSnapshot.HasActiveDocument ||
                !string.Equals(
                    sessionSnapshot.ActiveDocumentKind,
                    "part",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw WorkerErrorFactory.UnableToCreateNewPart(
                    templatePath,
                    "SolidWorks did not expose an active part document after NewDocument.");
            }

            return new DocumentOperationResult
            {
                Session = sessionSnapshot,
                OperationDetails = new Dictionary<string, object?>
                {
                    ["templatePath"] = templatePath,
                    ["requestedName"] = requestedName,
                    ["requestedNameApplied"] = false
                }
            };
        }
        catch (WorkerCommandException)
        {
            throw;
        }
        catch (Exception error)
        {
            throw WorkerErrorFactory.UnableToCreateNewPart(
                templatePath,
                "SolidWorks raised an exception during NewDocument.",
                error);
        }
        finally
        {
            ComInterop.ReleaseIfComObject(createdDocument);
        }
    }

    public DocumentOperationResult SelectPlane(
        ProjectRuntimeStateModel stateBefore,
        string requestedPlane,
        int desiredMajorVersion)
    {
        EnsureSupportedPlane(requestedPlane);
        object? activeDocument = null;
        object? planeFeature = null;

        try
        {
            var context = EnsureActivePartDocument(stateBefore, desiredMajorVersion);
            activeDocument = context.ActiveDocument;
            var planeSelection = FindStandardPlaneFeature(activeDocument, requestedPlane);
            planeFeature = planeSelection.Feature;

            ClearSelection(activeDocument);
            var selected = ComInterop.ToBoolean(
                ComInterop.Invoke(planeFeature, "Select2", false, 0));

            if (!selected)
            {
                throw WorkerErrorFactory.UnableToSelectPlane(
                    requestedPlane,
                    "IFeature.Select2 returned false.");
            }

            return new DocumentOperationResult
            {
                Session = CapturePartSnapshot(desiredMajorVersion),
                OperationDetails = new Dictionary<string, object?>
                {
                    ["plane"] = requestedPlane,
                    ["planeFeatureName"] = planeSelection.FeatureName,
                    ["selectionResolution"] = planeSelection.Resolution
                }
            };
        }
        catch (WorkerCommandException)
        {
            throw;
        }
        catch (Exception error)
        {
            throw WorkerErrorFactory.UnableToSelectPlane(
                requestedPlane,
                "SolidWorks raised an exception during plane selection.",
                error);
        }
        finally
        {
            ComInterop.ReleaseIfComObject(planeFeature);
            ComInterop.ReleaseIfComObject(activeDocument);
        }
    }

    public DocumentOperationResult StartSketch(
        ProjectRuntimeStateModel stateBefore,
        int desiredMajorVersion)
    {
        var selectedPlane = stateBefore.CurrentDocument?.SelectedPlane;
        if (string.IsNullOrWhiteSpace(selectedPlane))
        {
            throw WorkerErrorFactory.NoPlaneSelected();
        }

        if (!string.IsNullOrWhiteSpace(stateBefore.CurrentDocument?.ActiveSketchId))
        {
            throw WorkerErrorFactory.ActiveSketchAlreadyOpen(
                stateBefore.CurrentDocument.ActiveSketchId);
        }

        object? activeDocument = null;
        object? planeFeature = null;
        object? sketchManager = null;
        object? activeSketch = null;
        var sketchActivationMethod = "sketch_manager_insert_sketch";

        try
        {
            var context = EnsureActivePartDocument(stateBefore, desiredMajorVersion);
            activeDocument = context.ActiveDocument;
            var planeSelection = FindStandardPlaneFeature(activeDocument, selectedPlane);
            planeFeature = planeSelection.Feature;

            ClearSelection(activeDocument);
            var planeSelected = ComInterop.ToBoolean(
                ComInterop.Invoke(planeFeature, "Select2", false, 0));
            if (!planeSelected)
            {
                throw WorkerErrorFactory.UnableToStartSketch(
                    selectedPlane,
                    "The target plane could not be reselected.");
            }

            sketchManager = ComInterop.GetProperty<object?>(activeDocument, "SketchManager");
            if (sketchManager is null)
            {
                throw WorkerErrorFactory.UnableToStartSketch(
                    selectedPlane,
                    "Active document did not expose SketchManager.");
            }

            ComInterop.Invoke(sketchManager, "InsertSketch", true);
            activeSketch = ComInterop.GetProperty<object?>(sketchManager, "ActiveSketch");
            if (activeSketch is null)
            {
                try
                {
                    ComInterop.Invoke(activeDocument, "EditSketch");
                    activeSketch = ComInterop.GetProperty<object?>(sketchManager, "ActiveSketch");
                    if (activeSketch is not null)
                    {
                        sketchActivationMethod = "modeldoc_editsketch_fallback";
                    }
                }
                catch
                {
                    // Best-effort fallback only.
                }
            }

            if (activeSketch is null)
            {
                throw WorkerErrorFactory.UnableToStartSketch(
                    selectedPlane,
                    "SketchManager.ActiveSketch remained null after InsertSketch.");
            }

            return new DocumentOperationResult
            {
                Session = CapturePartSnapshot(desiredMajorVersion),
                OperationDetails = new Dictionary<string, object?>
                {
                    ["plane"] = selectedPlane,
                    ["selectionResolution"] = planeSelection.Resolution,
                    ["planeFeatureName"] = planeSelection.FeatureName,
                    ["sketchActivationMethod"] = sketchActivationMethod
                }
            };
        }
        catch (WorkerCommandException)
        {
            throw;
        }
        catch (Exception error)
        {
            throw WorkerErrorFactory.UnableToStartSketch(
                selectedPlane,
                "SolidWorks raised an exception while opening the sketch.",
                error);
        }
        finally
        {
            ComInterop.ReleaseIfComObject(activeSketch);
            ComInterop.ReleaseIfComObject(sketchManager);
            ComInterop.ReleaseIfComObject(planeFeature);
            ComInterop.ReleaseIfComObject(activeDocument);
        }
    }

    public DocumentOperationResult DrawCircle(
        ProjectRuntimeStateModel stateBefore,
        DrawCircleCommand command,
        int desiredMajorVersion)
    {
        if (command.Radius <= 0)
        {
            throw WorkerErrorFactory.InvalidInput(
                "draw_circle requires a positive radius.",
                "draw_circle",
                new Dictionary<string, object?>
                {
                    ["radius"] = command.Radius
                });
        }

        var activeSketchId = stateBefore.CurrentDocument?.ActiveSketchId;
        if (string.IsNullOrWhiteSpace(activeSketchId))
        {
            throw WorkerErrorFactory.NoActiveSketch("draw_circle");
        }

        object? activeDocument = null;
        object? sketchManager = null;
        object? activeSketch = null;
        object? createdSegment = null;

        try
        {
            var context = EnsureActivePartDocument(stateBefore, desiredMajorVersion);
            activeDocument = context.ActiveDocument;
            sketchManager = ComInterop.GetProperty<object?>(activeDocument, "SketchManager");
            if (sketchManager is null)
            {
                throw WorkerErrorFactory.UnableToDrawCircle(
                    activeSketchId,
                    command.Radius,
                    "Active document did not expose SketchManager.");
            }

            activeSketch = ComInterop.GetProperty<object?>(sketchManager, "ActiveSketch");
            if (activeSketch is null)
            {
                throw WorkerErrorFactory.NoActiveSketch("draw_circle");
            }

            createdSegment = ComInterop.Invoke(
                sketchManager,
                "CreateCircleByRadius",
                MillimetersToMeters(command.Center.X),
                MillimetersToMeters(command.Center.Y),
                0d,
                MillimetersToMeters(command.Radius));

            if (createdSegment is null)
            {
                throw WorkerErrorFactory.UnableToDrawCircle(
                    activeSketchId,
                    command.Radius,
                    "CreateCircleByRadius returned null.");
            }

            var constructionApplied = false;
            if (command.Construction)
            {
                constructionApplied = ComInterop.TrySetProperty(
                    createdSegment,
                    "ConstructionGeometry",
                    true);

                if (!constructionApplied)
                {
                    throw WorkerErrorFactory.UnableToDrawCircle(
                        activeSketchId,
                        command.Radius,
                        "ConstructionGeometry could not be applied to the created circle.");
                }
            }

            return new DocumentOperationResult
            {
                Session = CapturePartSnapshot(desiredMajorVersion),
                OperationDetails = new Dictionary<string, object?>
                {
                    ["sketchId"] = activeSketchId,
                    ["centerMm"] = new Dictionary<string, object?>
                    {
                        ["x"] = command.Center.X,
                        ["y"] = command.Center.Y
                    },
                    ["radiusMm"] = command.Radius,
                    ["constructionApplied"] = constructionApplied
                }
            };
        }
        catch (WorkerCommandException)
        {
            throw;
        }
        catch (Exception error)
        {
            throw WorkerErrorFactory.UnableToDrawCircle(
                activeSketchId,
                command.Radius,
                "SolidWorks raised an exception while creating the circle.",
                error);
        }
        finally
        {
            ComInterop.ReleaseIfComObject(createdSegment);
            ComInterop.ReleaseIfComObject(activeSketch);
            ComInterop.ReleaseIfComObject(sketchManager);
            ComInterop.ReleaseIfComObject(activeDocument);
        }
    }

    public DocumentOperationResult DrawLine(
        ProjectRuntimeStateModel stateBefore,
        DrawLineCommand command,
        int desiredMajorVersion)
    {
        var geometry = new Dictionary<string, object?>
        {
            ["startMm"] = new Dictionary<string, object?>
            {
                ["x"] = command.Start.X,
                ["y"] = command.Start.Y
            },
            ["endMm"] = new Dictionary<string, object?>
            {
                ["x"] = command.End.X,
                ["y"] = command.End.Y
            },
            ["constructionRequested"] = command.Construction
        };

        var deltaX = command.End.X - command.Start.X;
        var deltaY = command.End.Y - command.Start.Y;
        var lengthMm = Math.Sqrt((deltaX * deltaX) + (deltaY * deltaY));
        geometry["lengthMm"] = lengthMm;

        if (lengthMm <= double.Epsilon)
        {
            throw WorkerErrorFactory.InvalidInput(
                "draw_line requires distinct start and end points.",
                "draw_line",
                geometry);
        }

        var activeSketchId = stateBefore.CurrentDocument?.ActiveSketchId;
        if (string.IsNullOrWhiteSpace(activeSketchId))
        {
            throw WorkerErrorFactory.NoActiveSketch("draw_line");
        }

        object? activeDocument = null;
        object? sketchManager = null;
        object? activeSketch = null;
        object? createdSegment = null;

        try
        {
            var context = EnsureActivePartDocument(stateBefore, desiredMajorVersion);
            activeDocument = context.ActiveDocument;
            sketchManager = ComInterop.GetProperty<object?>(activeDocument, "SketchManager");
            if (sketchManager is null)
            {
                throw WorkerErrorFactory.UnableToDrawLine(
                    activeSketchId,
                    geometry,
                    "Active document did not expose SketchManager.");
            }

            activeSketch = ComInterop.GetProperty<object?>(sketchManager, "ActiveSketch");
            if (activeSketch is null)
            {
                throw WorkerErrorFactory.NoActiveSketch("draw_line");
            }

            createdSegment = ComInterop.Invoke(
                sketchManager,
                "CreateLine",
                MillimetersToMeters(command.Start.X),
                MillimetersToMeters(command.Start.Y),
                0d,
                MillimetersToMeters(command.End.X),
                MillimetersToMeters(command.End.Y),
                0d);

            if (createdSegment is null)
            {
                throw WorkerErrorFactory.UnableToDrawLine(
                    activeSketchId,
                    geometry,
                    "CreateLine returned null.");
            }

            var constructionApplied = false;
            if (command.Construction)
            {
                constructionApplied = ComInterop.TrySetProperty(
                    createdSegment,
                    "ConstructionGeometry",
                    true);

                if (!constructionApplied)
                {
                    throw WorkerErrorFactory.UnableToDrawLine(
                        activeSketchId,
                        geometry,
                        "ConstructionGeometry could not be applied to the created line.");
                }
            }

            return new DocumentOperationResult
            {
                Session = CapturePartSnapshot(desiredMajorVersion),
                OperationDetails = new Dictionary<string, object?>(geometry)
                {
                    ["sketchId"] = activeSketchId,
                    ["constructionApplied"] = constructionApplied
                }
            };
        }
        catch (WorkerCommandException)
        {
            throw;
        }
        catch (Exception error)
        {
            throw WorkerErrorFactory.UnableToDrawLine(
                activeSketchId,
                geometry,
                "SolidWorks raised an exception while creating the line.",
                error);
        }
        finally
        {
            ComInterop.ReleaseIfComObject(createdSegment);
            ComInterop.ReleaseIfComObject(activeSketch);
            ComInterop.ReleaseIfComObject(sketchManager);
            ComInterop.ReleaseIfComObject(activeDocument);
        }
    }

    public DocumentOperationResult DrawCenteredRectangle(
        ProjectRuntimeStateModel stateBefore,
        DrawCenteredRectangleCommand command,
        int desiredMajorVersion)
    {
        var geometry = new Dictionary<string, object?>
        {
            ["centerMm"] = new Dictionary<string, object?>
            {
                ["x"] = command.Center.X,
                ["y"] = command.Center.Y
            },
            ["cornerMm"] = new Dictionary<string, object?>
            {
                ["x"] = command.Corner.X,
                ["y"] = command.Corner.Y
            },
            ["constructionRequested"] = command.Construction
        };

        if (Math.Abs(command.Corner.X - command.Center.X) <= double.Epsilon ||
            Math.Abs(command.Corner.Y - command.Center.Y) <= double.Epsilon)
        {
            throw WorkerErrorFactory.InvalidInput(
                "draw_centered_rectangle requires a non-degenerate center/corner pair.",
                "draw_centered_rectangle",
                geometry);
        }

        var activeSketchId = stateBefore.CurrentDocument?.ActiveSketchId;
        if (string.IsNullOrWhiteSpace(activeSketchId))
        {
            throw WorkerErrorFactory.NoActiveSketch("draw_centered_rectangle");
        }

        object? activeDocument = null;
        object? sketchManager = null;
        object? activeSketch = null;
        var createdSegments = new List<object>();

        try
        {
            var context = EnsureActivePartDocument(stateBefore, desiredMajorVersion);
            activeDocument = context.ActiveDocument;
            sketchManager = ComInterop.GetProperty<object?>(activeDocument, "SketchManager");
            if (sketchManager is null)
            {
                throw WorkerErrorFactory.UnableToDrawCenteredRectangle(
                    activeSketchId,
                    geometry,
                    "Active document did not expose SketchManager.");
            }

            activeSketch = ComInterop.GetProperty<object?>(sketchManager, "ActiveSketch");
            if (activeSketch is null)
            {
                throw WorkerErrorFactory.NoActiveSketch("draw_centered_rectangle");
            }

            var createdGeometry = ComInterop.Invoke(
                sketchManager,
                "CreateCenterRectangle",
                MillimetersToMeters(command.Center.X),
                MillimetersToMeters(command.Center.Y),
                0d,
                MillimetersToMeters(command.Corner.X),
                MillimetersToMeters(command.Corner.Y),
                0d);

            createdSegments.AddRange(ExtractCreatedSketchEntities(createdGeometry));
            if (createdSegments.Count == 0)
            {
                throw WorkerErrorFactory.UnableToDrawCenteredRectangle(
                    activeSketchId,
                    geometry,
                    "CreateCenterRectangle returned no sketch segments.");
            }

            var constructionApplied = false;
            if (command.Construction)
            {
                constructionApplied = createdSegments.All(segment =>
                    ComInterop.TrySetProperty(segment, "ConstructionGeometry", true));

                if (!constructionApplied)
                {
                    throw WorkerErrorFactory.UnableToDrawCenteredRectangle(
                        activeSketchId,
                        geometry,
                        "ConstructionGeometry could not be applied to every rectangle segment.");
                }
            }

            return new DocumentOperationResult
            {
                Session = CapturePartSnapshot(desiredMajorVersion),
                OperationDetails = new Dictionary<string, object?>(geometry)
                {
                    ["sketchId"] = activeSketchId,
                    ["segmentCount"] = createdSegments.Count,
                    ["constructionApplied"] = constructionApplied
                }
            };
        }
        catch (WorkerCommandException)
        {
            throw;
        }
        catch (Exception error)
        {
            throw WorkerErrorFactory.UnableToDrawCenteredRectangle(
                activeSketchId,
                geometry,
                "SolidWorks raised an exception while creating the centered rectangle.",
                error);
        }
        finally
        {
            ReleaseComObjects(createdSegments);
            ComInterop.ReleaseIfComObject(activeSketch);
            ComInterop.ReleaseIfComObject(sketchManager);
            ComInterop.ReleaseIfComObject(activeDocument);
        }
    }

    public DocumentOperationResult AddDimension(
        ProjectRuntimeStateModel stateBefore,
        AddDimensionCommand command,
        int desiredMajorVersion)
    {
        var activeSketchId = stateBefore.CurrentDocument?.ActiveSketchId;
        if (string.IsNullOrWhiteSpace(activeSketchId))
        {
            throw WorkerErrorFactory.NoActiveSketch("add_dimension");
        }

        var activeSketch = stateBefore.CurrentDocument?.Sketches.FirstOrDefault(
            sketch => string.Equals(sketch.Id, activeSketchId, StringComparison.Ordinal));
        if (activeSketch is null)
        {
            throw WorkerErrorFactory.SketchNotFound(activeSketchId);
        }

        var targetEntity = activeSketch.Entities.FirstOrDefault(
            entity => string.Equals(entity.Id, command.EntityId, StringComparison.Ordinal));
        if (targetEntity is null)
        {
            throw WorkerErrorFactory.SketchEntityNotFound(command.EntityId);
        }

        var geometry = CreateDimensionGeometry(activeSketchId, command, targetEntity);
        geometry["sketchId"] = activeSketchId;
        geometry["entityKind"] = targetEntity.Kind;
        var appliedOrientation = ResolveDimensionOrientation(command, targetEntity);
        geometry["orientationApplied"] = appliedOrientation;

        object? activeDocument = null;
        object? modelExtension = null;
        object? sketchManager = null;
        object? liveActiveSketch = null;
        object? createdDisplayDimension = null;
        object? dimensionValueTarget = null;
        object? selectedSegment = null;

        try
        {
            var context = EnsureActivePartDocument(stateBefore, desiredMajorVersion);
            activeDocument = context.ActiveDocument;
            modelExtension = ComInterop.GetProperty<object?>(activeDocument, "Extension");
            if (modelExtension is null)
            {
                throw WorkerErrorFactory.UnableToAddDimension(
                    activeSketchId,
                    command.EntityId,
                    geometry,
                    "Active document did not expose ModelDocExtension.");
            }

            sketchManager = ComInterop.GetProperty<object?>(activeDocument, "SketchManager");
            liveActiveSketch = sketchManager is null
                ? null
                : ComInterop.GetProperty<object?>(sketchManager, "ActiveSketch");
            if (liveActiveSketch is null)
            {
                throw WorkerErrorFactory.NoActiveSketch("add_dimension");
            }

            ClearSelection(activeDocument);
            var selection = SelectLineEntityForDimension(
                modelExtension,
                activeDocument,
                liveActiveSketch,
                targetEntity,
                geometry);
            if (!selection.Selected)
            {
                throw WorkerErrorFactory.UnableToAddDimension(
                    activeSketchId,
                    command.EntityId,
                    geometry,
                    "SelectByID2 could not resolve the target sketch segment.");
            }

            geometry["selectionType"] = selection.SelectionType;
            selectedSegment = selection.SelectedSegment;

            var textPosition = GetDimensionTextPositionMm(geometry, appliedOrientation);
            geometry["textPositionMm"] = new Dictionary<string, object?>
            {
                ["x"] = textPosition.X,
                ["y"] = textPosition.Y
            };

            createdDisplayDimension = ComInterop.Invoke(
                activeDocument,
                appliedOrientation == "horizontal"
                    ? "AddHorizontalDimension2"
                    : "AddVerticalDimension2",
                MillimetersToMeters(textPosition.X),
                MillimetersToMeters(textPosition.Y),
                0d);

            if (createdDisplayDimension is null)
            {
                throw WorkerErrorFactory.UnableToAddDimension(
                    activeSketchId,
                    command.EntityId,
                    geometry,
                    "SolidWorks returned null while creating the display dimension.");
            }

            dimensionValueTarget = ResolveDimensionValueTarget(createdDisplayDimension);
            if (dimensionValueTarget is null)
            {
                throw WorkerErrorFactory.UnableToAddDimension(
                    activeSketchId,
                    command.EntityId,
                    geometry,
                    "The created display dimension did not expose a writable dimension object.");
            }

            var systemValueMeters = MillimetersToMeters(command.Value);
            if (!TryApplyDimensionValue(dimensionValueTarget, systemValueMeters))
            {
                throw WorkerErrorFactory.UnableToAddDimension(
                    activeSketchId,
                    command.EntityId,
                    geometry,
                    "The created dimension could not be assigned the requested system value.");
            }

            geometry["valueMm"] = command.Value;
            geometry["valueAppliedMeters"] = systemValueMeters;

            try
            {
                ComInterop.Invoke(activeDocument, "EditRebuild3");
            }
            catch
            {
                // Best-effort only.
            }

            return new DocumentOperationResult
            {
                Session = CapturePartSnapshot(desiredMajorVersion),
                OperationDetails = geometry
            };
        }
        catch (WorkerCommandException)
        {
            throw;
        }
        catch (Exception error)
        {
            throw WorkerErrorFactory.UnableToAddDimension(
                activeSketchId,
                command.EntityId,
                geometry,
                "SolidWorks raised an exception while adding the dimension.",
                error);
        }
        finally
        {
            if (activeDocument is not null)
            {
                ClearSelection(activeDocument);
            }

            if (dimensionValueTarget is not null &&
                !ReferenceEquals(dimensionValueTarget, createdDisplayDimension))
            {
                ComInterop.ReleaseIfComObject(dimensionValueTarget);
            }

            ComInterop.ReleaseIfComObject(createdDisplayDimension);
            if (selectedSegment is not null)
            {
                ComInterop.ReleaseIfComObject(selectedSegment);
            }

            ComInterop.ReleaseIfComObject(liveActiveSketch);
            ComInterop.ReleaseIfComObject(sketchManager);
            ComInterop.ReleaseIfComObject(modelExtension);
            ComInterop.ReleaseIfComObject(activeDocument);
        }
    }

    public DocumentOperationResult CloseSketch(
        ProjectRuntimeStateModel stateBefore,
        int desiredMajorVersion)
    {
        var activeSketchId = stateBefore.CurrentDocument?.ActiveSketchId;
        if (string.IsNullOrWhiteSpace(activeSketchId))
        {
            throw WorkerErrorFactory.NoActiveSketch("close_sketch");
        }

        object? activeDocument = null;
        object? sketchManager = null;
        object? activeSketch = null;

        try
        {
            var context = EnsureActivePartDocument(stateBefore, desiredMajorVersion);
            activeDocument = context.ActiveDocument;
            sketchManager = ComInterop.GetProperty<object?>(activeDocument, "SketchManager");
            if (sketchManager is null)
            {
                throw WorkerErrorFactory.UnableToCloseSketch(
                    activeSketchId,
                    "Active document did not expose SketchManager.");
            }

            activeSketch = ComInterop.GetProperty<object?>(sketchManager, "ActiveSketch");
            if (activeSketch is null)
            {
                throw WorkerErrorFactory.NoActiveSketch("close_sketch");
            }

            ComInterop.Invoke(sketchManager, "InsertSketch", true);
            activeSketch = ComInterop.GetProperty<object?>(sketchManager, "ActiveSketch");
            if (activeSketch is not null)
            {
                throw WorkerErrorFactory.UnableToCloseSketch(
                    activeSketchId,
                    "SketchManager.ActiveSketch remained non-null after closing.");
            }

            return new DocumentOperationResult
            {
                Session = CapturePartSnapshot(desiredMajorVersion),
                OperationDetails = new Dictionary<string, object?>
                {
                    ["sketchId"] = activeSketchId
                }
            };
        }
        catch (WorkerCommandException)
        {
            throw;
        }
        catch (Exception error)
        {
            throw WorkerErrorFactory.UnableToCloseSketch(
                activeSketchId,
                "SolidWorks raised an exception while closing the active sketch.",
                error);
        }
        finally
        {
            ComInterop.ReleaseIfComObject(activeSketch);
            ComInterop.ReleaseIfComObject(sketchManager);
            ComInterop.ReleaseIfComObject(activeDocument);
        }
    }

    public DocumentOperationResult ExtrudeBoss(
        ProjectRuntimeStateModel stateBefore,
        ExtrudeBossCommand command,
        int desiredMajorVersion)
    {
        if (command.Depth <= 0)
        {
            throw WorkerErrorFactory.InvalidInput(
                "extrude_boss requires a positive depth.",
                "extrude_boss",
                new Dictionary<string, object?>
                {
                    ["depth"] = command.Depth
                });
        }

        var currentDocument = stateBefore.CurrentDocument
            ?? throw WorkerErrorFactory.NoActiveDocument();

        if (!string.IsNullOrWhiteSpace(currentDocument.ActiveSketchId))
        {
            throw WorkerErrorFactory.SketchMustBeClosed(
                command.SketchId,
                "extrude_boss");
        }

        var sketchState = currentDocument.Sketches.FirstOrDefault(
            sketch => string.Equals(sketch.Id, command.SketchId, StringComparison.Ordinal));
        if (sketchState is null)
        {
            throw WorkerErrorFactory.SketchNotFound(command.SketchId);
        }

        if (sketchState.IsOpen)
        {
            throw WorkerErrorFactory.SketchMustBeClosed(
                command.SketchId,
                "extrude_boss");
        }

        object? activeDocument = null;
        object? sketchFeature = null;
        object? featureManager = null;
        object? createdFeature = null;

        try
        {
            var context = EnsureActivePartDocument(stateBefore, desiredMajorVersion);
            activeDocument = context.ActiveDocument;

            sketchFeature = FindSketchFeatureById(activeDocument, command.SketchId);
            if (sketchFeature is null)
            {
                throw WorkerErrorFactory.UnableToExtrudeBoss(
                    command.SketchId,
                    command.Depth,
                    "Could not resolve the requested sketch feature in the SolidWorks feature tree.");
            }

            ClearSelection(activeDocument);
            var selected = ComInterop.ToBoolean(
                ComInterop.Invoke(sketchFeature, "Select2", false, 0));
            if (!selected)
            {
                throw WorkerErrorFactory.UnableToExtrudeBoss(
                    command.SketchId,
                    command.Depth,
                    "Sketch feature selection returned false.");
            }

            featureManager = ComInterop.GetProperty<object?>(activeDocument, "FeatureManager");
            if (featureManager is null)
            {
                throw WorkerErrorFactory.UnableToExtrudeBoss(
                    command.SketchId,
                    command.Depth,
                    "Active document did not expose FeatureManager.");
            }

            createdFeature = ComInterop.Invoke(
                featureManager,
                "FeatureExtrusion3",
                true,
                false,
                false,
                SolidWorksApiConstants.SwEndConditionBlind,
                SolidWorksApiConstants.SwEndConditionBlind,
                MillimetersToMeters(command.Depth),
                0d,
                false,
                false,
                false,
                false,
                0d,
                0d,
                false,
                false,
                false,
                false,
                command.MergeResult,
                true,
                true,
                0,
                0d,
                false);

            if (createdFeature is null)
            {
                throw WorkerErrorFactory.UnableToExtrudeBoss(
                    command.SketchId,
                    command.Depth,
                    "FeatureExtrusion3 returned null.");
            }

            var featureName = ComInterop.InvokeString(createdFeature, "Name");
            var featureType = ComInterop.InvokeString(createdFeature, "GetTypeName2");

            return new DocumentOperationResult
            {
                Session = CapturePartSnapshot(desiredMajorVersion),
                OperationDetails = new Dictionary<string, object?>
                {
                    ["sketchId"] = command.SketchId,
                    ["depthMm"] = command.Depth,
                    ["mergeResult"] = command.MergeResult,
                    ["featureName"] = featureName,
                    ["featureType"] = featureType
                }
            };
        }
        catch (WorkerCommandException)
        {
            throw;
        }
        catch (Exception error)
        {
            throw WorkerErrorFactory.UnableToExtrudeBoss(
                command.SketchId,
                command.Depth,
                "SolidWorks raised an exception while creating the boss extrusion.",
                error);
        }
        finally
        {
            ComInterop.ReleaseIfComObject(createdFeature);
            ComInterop.ReleaseIfComObject(featureManager);
            ComInterop.ReleaseIfComObject(sketchFeature);
            ComInterop.ReleaseIfComObject(activeDocument);
        }
    }

    public DocumentOperationResult SavePart(
        ProjectRuntimeStateModel stateBefore,
        string requestedPath,
        int desiredMajorVersion)
    {
        var normalizedPath = NormalizeSavePath(requestedPath);
        object? activeDocument = null;

        try
        {
            var context = EnsureActivePartDocument(stateBefore, desiredMajorVersion);
            activeDocument = context.ActiveDocument;

            var currentPath = ComInterop.InvokeString(activeDocument, "GetPathName");
            var saveResult = PathsMatch(currentPath, normalizedPath)
                ? SaveInPlace(activeDocument, normalizedPath)
                : SaveAs(activeDocument, normalizedPath);

            if (!saveResult.Success || !File.Exists(normalizedPath))
            {
                throw WorkerErrorFactory.UnableToSaveDocument(
                    normalizedPath,
                    saveResult.SaveMode,
                    saveResult.Errors,
                    saveResult.Warnings,
                    saveResult.FailureReason ?? "SolidWorks reported save failure.");
            }

            var sessionSnapshot = CapturePartSnapshot(desiredMajorVersion);

            if (!PathsMatch(sessionSnapshot.ActiveDocumentPath, normalizedPath))
            {
                throw WorkerErrorFactory.UnableToSaveDocument(
                    normalizedPath,
                    saveResult.SaveMode,
                    saveResult.Errors,
                    saveResult.Warnings,
                    "The active document path after save did not match the requested path.");
            }

            return new DocumentOperationResult
            {
                Session = sessionSnapshot,
                OperationDetails = new Dictionary<string, object?>
                {
                    ["resolvedPath"] = normalizedPath,
                    ["saveMode"] = saveResult.SaveMode,
                    ["solidWorksErrors"] = saveResult.Errors,
                    ["solidWorksWarnings"] = saveResult.Warnings
                }
            };
        }
        catch (WorkerCommandException)
        {
            throw;
        }
        catch (Exception error)
        {
            throw WorkerErrorFactory.UnableToSaveDocument(
                normalizedPath,
                "unknown",
                null,
                null,
                "SolidWorks raised an exception during save.",
                error);
        }
        finally
        {
            ComInterop.ReleaseIfComObject(activeDocument);
        }
    }

    public DocumentOperationResult ExportStep(
        ProjectRuntimeStateModel stateBefore,
        string requestedPath,
        int desiredMajorVersion)
    {
        var normalizedPath = NormalizeStepExportPath(requestedPath);
        object? activeDocument = null;

        try
        {
            var solidWorksApplication = sessionService.EnsureApplication(desiredMajorVersion);
            activeDocument = ComInterop.GetProperty<object?>(
                solidWorksApplication,
                "ActiveDoc");

            if (activeDocument is null)
            {
                throw WorkerErrorFactory.NoActiveDocument();
            }

            var currentPath = ComInterop.InvokeString(activeDocument, "GetPathName");
            var documentKind = InferDocumentKind(currentPath, stateBefore);
            if (!string.Equals(documentKind, "part", StringComparison.OrdinalIgnoreCase))
            {
                throw WorkerErrorFactory.DocumentNotExportableInCurrentSlice(
                    documentKind,
                    "export_step");
            }

            var exportResult = ExportStepFile(activeDocument, normalizedPath);
            if (!exportResult.Success || !File.Exists(normalizedPath))
            {
                throw WorkerErrorFactory.UnableToExportStep(
                    normalizedPath,
                    exportResult.ExportMode,
                    exportResult.Errors,
                    exportResult.Warnings,
                    exportResult.FailureReason ?? "SolidWorks reported STEP export failure.");
            }

            var sessionSnapshot = CapturePartSnapshot(desiredMajorVersion);
            if (!string.IsNullOrWhiteSpace(currentPath) &&
                !PathsMatch(sessionSnapshot.ActiveDocumentPath, currentPath))
            {
                throw WorkerErrorFactory.UnableToExportStep(
                    normalizedPath,
                    exportResult.ExportMode,
                    exportResult.Errors,
                    exportResult.Warnings,
                    "The active SolidWorks document path changed unexpectedly after STEP export.");
            }

            return new DocumentOperationResult
            {
                Session = sessionSnapshot,
                OperationDetails = new Dictionary<string, object?>
                {
                    ["resolvedPath"] = normalizedPath,
                    ["exportKind"] = "step",
                    ["exportMode"] = exportResult.ExportMode,
                    ["sourceDocumentPath"] = currentPath,
                    ["solidWorksErrors"] = exportResult.Errors,
                    ["solidWorksWarnings"] = exportResult.Warnings
                }
            };
        }
        catch (WorkerCommandException)
        {
            throw;
        }
        catch (Exception error)
        {
            throw WorkerErrorFactory.UnableToExportStep(
                normalizedPath,
                "unknown",
                null,
                null,
                "SolidWorks raised an exception during STEP export.",
                error);
        }
        finally
        {
            ComInterop.ReleaseIfComObject(activeDocument);
        }
    }

    private (object Application, object ActiveDocument) EnsureActivePartDocument(
        ProjectRuntimeStateModel stateBefore,
        int desiredMajorVersion)
    {
        var solidWorksApplication = sessionService.EnsureApplication(desiredMajorVersion);
        var activeDocument = ComInterop.GetProperty<object?>(
            solidWorksApplication,
            "ActiveDoc");

        if (activeDocument is null)
        {
            throw WorkerErrorFactory.NoActiveDocument();
        }

        var currentPath = ComInterop.InvokeString(activeDocument, "GetPathName");
        var documentKind = InferDocumentKind(currentPath, stateBefore);
        if (documentKind is not null &&
            !string.Equals(documentKind, "part", StringComparison.OrdinalIgnoreCase))
        {
            ComInterop.ReleaseIfComObject(activeDocument);
            throw WorkerErrorFactory.NoActiveDocument(documentKind);
        }

        return (solidWorksApplication, activeDocument);
    }

    private SolidWorksSessionSnapshot CapturePartSnapshot(int desiredMajorVersion)
    {
        return CoerceSnapshotDocumentKind(
            sessionService.TryCaptureSnapshot(
                connectIfNeeded: false,
                desiredMajorVersion),
            "part");
    }

    private static void EnsureSupportedPlane(string requestedPlane)
    {
        if (requestedPlane is SolidWorksApiConstants.FrontPlaneName or
            SolidWorksApiConstants.TopPlaneName or
            SolidWorksApiConstants.RightPlaneName)
        {
            return;
        }

        throw WorkerErrorFactory.InvalidPlaneSelection(requestedPlane);
    }

    private static PlaneSelectionResult FindStandardPlaneFeature(
        object activeDocument,
        string requestedPlane)
    {
        object? feature = null;
        var referencePlanes = new List<object>();

        try
        {
            feature = ComInterop.Invoke(activeDocument, "FirstFeature");
            while (feature is not null)
            {
                var featureType = ComInterop.InvokeString(feature, "GetTypeName2");
                var featureName = ComInterop.InvokeString(feature, "Name");

                if (string.Equals(
                        featureType,
                        SolidWorksApiConstants.RefPlaneFeatureType,
                        StringComparison.OrdinalIgnoreCase))
                {
                    referencePlanes.Add(feature);

                    if (string.Equals(featureName, requestedPlane, StringComparison.OrdinalIgnoreCase))
                    {
                        return new PlaneSelectionResult
                        {
                            Feature = feature,
                            FeatureName = featureName ?? requestedPlane,
                            Resolution = "exact_name"
                        };
                    }
                }

                feature = ComInterop.Invoke(feature, "GetNextFeature");
            }

            var fallbackIndex = requestedPlane switch
            {
                SolidWorksApiConstants.FrontPlaneName => 0,
                SolidWorksApiConstants.TopPlaneName => 1,
                SolidWorksApiConstants.RightPlaneName => 2,
                _ => -1
            };

            if (fallbackIndex >= 0 && referencePlanes.Count > fallbackIndex)
            {
                var fallbackFeature = referencePlanes[fallbackIndex];
                return new PlaneSelectionResult
                {
                    Feature = fallbackFeature,
                    FeatureName = ComInterop.InvokeString(fallbackFeature, "Name") ?? requestedPlane,
                    Resolution = "refplane_ordinal_fallback"
                };
            }

            throw WorkerErrorFactory.UnableToSelectPlane(
                requestedPlane,
                "The requested standard plane could not be resolved in the feature tree.");
        }
        finally
        {
            foreach (var referencePlane in referencePlanes)
            {
                if (feature is not null && ReferenceEquals(referencePlane, feature))
                {
                    continue;
                }

                ComInterop.ReleaseIfComObject(referencePlane);
            }
        }
    }

    private static object? FindSketchFeatureById(object activeDocument, string sketchId)
    {
        var ordinal = ParseSketchOrdinal(sketchId);
        if (ordinal is null)
        {
            return null;
        }

        object? feature = null;
        var currentSketchOrdinal = 0;

        try
        {
            feature = ComInterop.Invoke(activeDocument, "FirstFeature");
            while (feature is not null)
            {
                var featureType = ComInterop.InvokeString(feature, "GetTypeName2");
                if (string.Equals(
                        featureType,
                        SolidWorksApiConstants.SketchProfileFeatureType,
                        StringComparison.OrdinalIgnoreCase))
                {
                    currentSketchOrdinal += 1;
                    if (currentSketchOrdinal == ordinal)
                    {
                        return feature;
                    }
                }

                feature = ComInterop.Invoke(feature, "GetNextFeature");
            }

            return null;
        }
        catch
        {
            ComInterop.ReleaseIfComObject(feature);
            throw;
        }
    }

    private static int? ParseSketchOrdinal(string sketchId)
    {
        const string Prefix = "sketch-";
        if (!sketchId.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return int.TryParse(sketchId[Prefix.Length..], out var ordinal) && ordinal > 0
            ? ordinal
            : null;
    }

    private static IReadOnlyList<object> ExtractCreatedSketchEntities(object? createdGeometry)
    {
        if (createdGeometry is null or bool)
        {
            return [];
        }

        if (createdGeometry is Array array)
        {
            return array
                .Cast<object?>()
                .Where(entry => entry is not null)
                .Cast<object>()
                .ToArray();
        }

        return [createdGeometry];
    }

    private static Dictionary<string, object?> CreateDimensionGeometry(
        string activeSketchId,
        AddDimensionCommand command,
        SketchEntityModel targetEntity)
    {
        if (!string.Equals(targetEntity.Kind, "line", StringComparison.Ordinal))
        {
            throw WorkerErrorFactory.DimensionEntityNotSupported(
                command.EntityId,
                targetEntity.Kind,
                command.Orientation);
        }

        if (targetEntity.Start is null || targetEntity.End is null)
        {
            throw WorkerErrorFactory.UnableToAddDimension(
                activeSketchId,
                command.EntityId,
                new Dictionary<string, object?>
                {
                    ["entityKind"] = targetEntity.Kind
                },
                "The target line entity was missing start/end coordinates in the normalized state.");
        }

        var midpointX = (targetEntity.Start.X + targetEntity.End.X) / 2d;
        var midpointY = (targetEntity.Start.Y + targetEntity.End.Y) / 2d;

        return new Dictionary<string, object?>
        {
            ["entityId"] = command.EntityId,
            ["requestedOrientation"] = command.Orientation,
            ["startMm"] = new Dictionary<string, object?>
            {
                ["x"] = targetEntity.Start.X,
                ["y"] = targetEntity.Start.Y
            },
            ["endMm"] = new Dictionary<string, object?>
            {
                ["x"] = targetEntity.End.X,
                ["y"] = targetEntity.End.Y
            },
            ["selectionPointMm"] = new Dictionary<string, object?>
            {
                ["x"] = midpointX,
                ["y"] = midpointY
            },
            ["lineLengthMm"] = Math.Sqrt(
                Math.Pow(targetEntity.End.X - targetEntity.Start.X, 2) +
                Math.Pow(targetEntity.End.Y - targetEntity.Start.Y, 2))
        };
    }

    private static string ResolveDimensionOrientation(
        AddDimensionCommand command,
        SketchEntityModel targetEntity)
    {
        if (!string.Equals(targetEntity.Kind, "line", StringComparison.Ordinal) ||
            targetEntity.Start is null ||
            targetEntity.End is null)
        {
            throw WorkerErrorFactory.DimensionEntityNotSupported(
                command.EntityId,
                targetEntity.Kind,
                command.Orientation);
        }

        var isHorizontal = Math.Abs(targetEntity.End.Y - targetEntity.Start.Y) <= double.Epsilon;
        var isVertical = Math.Abs(targetEntity.End.X - targetEntity.Start.X) <= double.Epsilon;

        if (string.IsNullOrWhiteSpace(command.Orientation))
        {
            if (isHorizontal)
            {
                return "horizontal";
            }

            if (isVertical)
            {
                return "vertical";
            }

            throw WorkerErrorFactory.DimensionEntityNotSupported(
                command.EntityId,
                targetEntity.Kind,
                command.Orientation);
        }

        return command.Orientation switch
        {
            "horizontal" when isHorizontal => "horizontal",
            "vertical" when isVertical => "vertical",
            "horizontal" or "vertical" => throw WorkerErrorFactory.InvalidDimensionOrientation(
                command.EntityId,
                command.Orientation,
                "The requested orientation does not match the target line geometry."),
            _ => throw WorkerErrorFactory.DimensionEntityNotSupported(
                command.EntityId,
                targetEntity.Kind,
                command.Orientation)
        };
    }

    private static (bool Selected, string? SelectionType, object? SelectedSegment) SelectLineEntityForDimension(
        object modelExtension,
        object activeDocument,
        object activeSketch,
        SketchEntityModel targetEntity,
        IReadOnlyDictionary<string, object?> geometry)
    {
        var selectionPoint = ReadPoint(geometry, "selectionPointMm");
        if (selectionPoint is null)
        {
            return (false, null, null);
        }

        foreach (var selectionType in new[] { "SKETCHSEGMENT", "EXTSKETCHSEGMENT" })
        {
            ClearSelection(activeDocument);
            try
            {
                var result = ComInterop.Invoke(
                    modelExtension,
                    "SelectByID2",
                    string.Empty,
                    selectionType,
                    MillimetersToMeters(selectionPoint.X),
                    MillimetersToMeters(selectionPoint.Y),
                    0d,
                    false,
                    0,
                    null,
                    0);

                if (TryConvertToBoolean(result) == true)
                {
                    return (true, selectionType, null);
                }
            }
            catch
            {
            }
        }

        var matchedSegment = TryFindMatchingLineSegment(activeSketch, targetEntity);
        if (matchedSegment is not null)
        {
            ClearSelection(activeDocument);
            if (TrySelectSketchSegmentObject(matchedSegment))
            {
                return (true, "active_sketch_segment", matchedSegment);
            }

            ComInterop.ReleaseIfComObject(matchedSegment);
        }

        return (false, null, null);
    }

    private static Point2DModel GetDimensionTextPositionMm(
        IReadOnlyDictionary<string, object?> geometry,
        string orientation)
    {
        var selectionPoint = ReadPoint(geometry, "selectionPointMm") ?? new Point2DModel
        {
            X = 0,
            Y = 0
        };

        return orientation == "horizontal"
            ? new Point2DModel
            {
                X = selectionPoint.X,
                Y = selectionPoint.Y + 10d
            }
            : new Point2DModel
            {
                X = selectionPoint.X + 10d,
                Y = selectionPoint.Y
            };
    }

    private static object? ResolveDimensionValueTarget(object createdDisplayDimension)
    {
        object? fallback = createdDisplayDimension;

        foreach (var candidate in EnumerateDimensionValueTargets(createdDisplayDimension))
        {
            if (candidate is null || ReferenceEquals(candidate, createdDisplayDimension))
            {
                continue;
            }

            return candidate;
        }

        return fallback;
    }

    private static IEnumerable<object?> EnumerateDimensionValueTargets(object createdDisplayDimension)
    {
        yield return createdDisplayDimension;

        object? candidate = null;
        try
        {
            candidate = ComInterop.Invoke(createdDisplayDimension, "GetDimension2", 0);
        }
        catch
        {
        }

        if (candidate is not null)
        {
            yield return candidate;
        }

        candidate = null;
        try
        {
            candidate = ComInterop.Invoke(createdDisplayDimension, "GetDimension");
        }
        catch
        {
        }

        if (candidate is not null)
        {
            yield return candidate;
        }
    }

    private static bool TryApplyDimensionValue(object dimensionValueTarget, double systemValueMeters)
    {
        return ComInterop.TrySetProperty(dimensionValueTarget, "SystemValue", systemValueMeters) ||
               ComInterop.TrySetProperty(dimensionValueTarget, "Value", systemValueMeters);
    }

    private static Point2DModel? ReadPoint(
        IReadOnlyDictionary<string, object?> geometry,
        string key)
    {
        if (!geometry.TryGetValue(key, out var value) ||
            value is not IReadOnlyDictionary<string, object?> pointDictionary)
        {
            return null;
        }

        if (!pointDictionary.TryGetValue("x", out var x) ||
            !pointDictionary.TryGetValue("y", out var y))
        {
            return null;
        }

        try
        {
            return new Point2DModel
            {
                X = Convert.ToDouble(x),
                Y = Convert.ToDouble(y)
            };
        }
        catch
        {
            return null;
        }
    }

    private static object? TryFindMatchingLineSegment(
        object activeSketch,
        SketchEntityModel targetEntity)
    {
        var segments = ExtractCreatedSketchEntities(ComInterop.Invoke(activeSketch, "GetSketchSegments"));
        object? matchedSegment = null;

        foreach (var segment in segments)
        {
            if (matchedSegment is null && DoesSketchSegmentMatchLine(segment, targetEntity))
            {
                matchedSegment = segment;
                continue;
            }

            ComInterop.ReleaseIfComObject(segment);
        }

        return matchedSegment;
    }

    private static bool DoesSketchSegmentMatchLine(
        object segment,
        SketchEntityModel targetEntity)
    {
        if (targetEntity.Start is null || targetEntity.End is null)
        {
            return false;
        }

        var segmentEndpoints = TryReadSketchSegmentEndpointsMm(segment);
        if (segmentEndpoints is null)
        {
            return false;
        }

        var (segmentStart, segmentEnd) = segmentEndpoints.Value;
        return (PointsMatch(segmentStart, targetEntity.Start) && PointsMatch(segmentEnd, targetEntity.End)) ||
               (PointsMatch(segmentStart, targetEntity.End) && PointsMatch(segmentEnd, targetEntity.Start));
    }

    private static (Point2DModel Start, Point2DModel End)? TryReadSketchSegmentEndpointsMm(object segment)
    {
        object? startPoint = null;
        object? endPoint = null;

        try
        {
            startPoint = TryInvokePoint(segment, "GetStartPoint2") ?? TryInvokePoint(segment, "GetStartPoint");
            endPoint = TryInvokePoint(segment, "GetEndPoint2") ?? TryInvokePoint(segment, "GetEndPoint");
            if (startPoint is null || endPoint is null)
            {
                return null;
            }

            var start = TryReadSketchPointMm(startPoint);
            var end = TryReadSketchPointMm(endPoint);
            return start is null || end is null ? null : (start, end);
        }
        finally
        {
            ComInterop.ReleaseIfComObject(startPoint);
            ComInterop.ReleaseIfComObject(endPoint);
        }
    }

    private static object? TryInvokePoint(object segment, string methodName)
    {
        try
        {
            return ComInterop.Invoke(segment, methodName);
        }
        catch
        {
            return null;
        }
    }

    private static Point2DModel? TryReadSketchPointMm(object sketchPoint)
    {
        try
        {
            var x = ComInterop.GetProperty<object?>(sketchPoint, "X");
            var y = ComInterop.GetProperty<object?>(sketchPoint, "Y");
            if (x is null || y is null)
            {
                return null;
            }

            return new Point2DModel
            {
                X = MetersToMillimeters(Convert.ToDouble(x)),
                Y = MetersToMillimeters(Convert.ToDouble(y))
            };
        }
        catch
        {
            return null;
        }
    }

    private static bool TrySelectSketchSegmentObject(object segment)
    {
        foreach (var candidate in new (string Method, object?[] Args)[]
                 {
                     ("Select4", [false, null]),
                     ("Select2", [false, 0]),
                     ("Select", [false])
                 })
        {
            try
            {
                if (TryConvertToBoolean(ComInterop.Invoke(segment, candidate.Method, candidate.Args)) == true)
                {
                    return true;
                }
            }
            catch
            {
            }
        }

        return false;
    }

    private static bool PointsMatch(Point2DModel left, Point2DModel right)
    {
        const double ToleranceMm = 0.001d;
        return Math.Abs(left.X - right.X) <= ToleranceMm &&
               Math.Abs(left.Y - right.Y) <= ToleranceMm;
    }

    private static void ReleaseComObjects(IEnumerable<object> objects)
    {
        foreach (var value in objects)
        {
            ComInterop.ReleaseIfComObject(value);
        }
    }

    private static void ClearSelection(object activeDocument)
    {
        try
        {
            ComInterop.Invoke(activeDocument, "ClearSelection2", true);
        }
        catch
        {
            // Best-effort only.
        }
    }

    private static string ResolveDefaultPartTemplate(object solidWorksApplication)
    {
        string? templatePath = null;

        try
        {
            templatePath = ComInterop.InvokeString(
                solidWorksApplication,
                "GetUserPreferenceStringValue",
                SolidWorksApiConstants.SwDefaultTemplatePart);
        }
        catch (Exception error)
        {
            throw WorkerErrorFactory.UnableToCreateNewPart(
                templatePath,
                "SolidWorks could not read the default part template preference.",
                error,
                resyncRequired: false);
        }

        if (string.IsNullOrWhiteSpace(templatePath) || !File.Exists(templatePath))
        {
            throw WorkerErrorFactory.DefaultPartTemplateNotConfigured(templatePath);
        }

        return templatePath;
    }

    private static string NormalizeSavePath(string requestedPath)
    {
        if (string.IsNullOrWhiteSpace(requestedPath))
        {
            throw WorkerErrorFactory.InvalidInput(
                "save_part requires a non-empty file path.",
                "save_part",
                new Dictionary<string, object?>
                {
                    ["path"] = requestedPath
                });
        }

        var normalizedPath = Path.GetFullPath(requestedPath);
        if (!string.Equals(
                Path.GetExtension(normalizedPath),
                ".sldprt",
                StringComparison.OrdinalIgnoreCase))
        {
            throw WorkerErrorFactory.InvalidInput(
                "save_part currently requires a .sldprt target path.",
                "save_part",
                new Dictionary<string, object?>
                {
                    ["path"] = normalizedPath
                });
        }

        var directory = Path.GetDirectoryName(normalizedPath);
        if (string.IsNullOrWhiteSpace(directory))
        {
            throw WorkerErrorFactory.InvalidInput(
                "save_part requires a path with a writable parent directory.",
                "save_part",
                new Dictionary<string, object?>
                {
                    ["path"] = normalizedPath
                });
        }

        Directory.CreateDirectory(directory);
        return normalizedPath;
    }

    private static string NormalizeStepExportPath(string requestedPath)
    {
        if (string.IsNullOrWhiteSpace(requestedPath))
        {
            throw WorkerErrorFactory.InvalidExportPath(
                requestedPath,
                "export_step requires a non-empty file path.");
        }

        string normalizedPath;
        try
        {
            normalizedPath = Path.GetFullPath(requestedPath);
        }
        catch (Exception error)
        {
            throw WorkerErrorFactory.InvalidExportPath(
                requestedPath,
                "The STEP export path could not be normalized.",
                error);
        }

        var extension = Path.GetExtension(normalizedPath);
        if (!string.Equals(extension, ".step", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(extension, ".stp", StringComparison.OrdinalIgnoreCase))
        {
            throw WorkerErrorFactory.InvalidExportPath(
                normalizedPath,
                "export_step currently requires a .step or .stp target path.");
        }

        var directory = Path.GetDirectoryName(normalizedPath);
        if (string.IsNullOrWhiteSpace(directory))
        {
            throw WorkerErrorFactory.InvalidExportPath(
                normalizedPath,
                "export_step requires a path with a writable parent directory.");
        }

        try
        {
            Directory.CreateDirectory(directory);
        }
        catch (Exception error)
        {
            throw WorkerErrorFactory.ExportPathNotWritable(normalizedPath, error);
        }

        return normalizedPath;
    }

    private static SaveOperationResult SaveInPlace(object activeDocument, string normalizedPath)
    {
        var args = new object?[]
        {
            SolidWorksApiConstants.SwSaveAsOptionsSilent,
            0,
            0
        };

        try
        {
            var success = ComInterop.ToBoolean(
                ComInterop.InvokeWithByRefArgs(
                    activeDocument,
                    "Save3",
                    args,
                    1,
                    2));

            return new SaveOperationResult
            {
                Success = success,
                SaveMode = "save3_in_place",
                Errors = ComInterop.ToInt32(args[1]),
                Warnings = ComInterop.ToInt32(args[2])
            };
        }
        catch (Exception error)
        {
            return new SaveOperationResult
            {
                Success = false,
                SaveMode = "save3_in_place",
                Errors = TryReadInt(args[1]),
                Warnings = TryReadInt(args[2]),
                FailureReason = error.Message
            };
        }
    }

    private static SaveOperationResult SaveAs(object activeDocument, string normalizedPath)
    {
        var args = new object?[]
        {
            normalizedPath,
            SolidWorksApiConstants.SwSaveAsCurrentVersion,
            SolidWorksApiConstants.SwSaveAsOptionsSilent,
            0,
            0
        };

        try
        {
            var success = ComInterop.ToBoolean(
                ComInterop.InvokeWithByRefArgs(
                    activeDocument,
                    "SaveAs4",
                    args,
                    3,
                    4));

            return new SaveOperationResult
            {
                Success = success,
                SaveMode = "saveas4_explicit_path",
                Errors = ComInterop.ToInt32(args[3]),
                Warnings = ComInterop.ToInt32(args[4])
            };
        }
        catch (Exception error)
        {
            return new SaveOperationResult
            {
                Success = false,
                SaveMode = "saveas4_explicit_path",
                Errors = TryReadInt(args[3]),
                Warnings = TryReadInt(args[4]),
                FailureReason = error.Message
            };
        }
    }

    private static ExportOperationResult ExportStepFile(
        object activeDocument,
        string normalizedPath)
    {
        var args = new object?[]
        {
            normalizedPath,
            SolidWorksApiConstants.SwSaveAsCurrentVersion,
            SolidWorksApiConstants.SwSaveAsOptionsSilent,
            0,
            0
        };

        try
        {
            var success = ComInterop.ToBoolean(
                ComInterop.InvokeWithByRefArgs(
                    activeDocument,
                    "SaveAs4",
                    args,
                    3,
                    4));

            return new ExportOperationResult
            {
                Success = success,
                ExportMode = "modeldoc2_saveas4_step",
                Errors = ComInterop.ToInt32(args[3]),
                Warnings = ComInterop.ToInt32(args[4])
            };
        }
        catch (Exception error)
        {
            return new ExportOperationResult
            {
                Success = false,
                ExportMode = "modeldoc2_saveas4_step",
                Errors = TryReadInt(args[3]),
                Warnings = TryReadInt(args[4]),
                FailureReason = error.Message
            };
        }
    }

    private static bool PathsMatch(string? left, string? right)
    {
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
        {
            return false;
        }

        return string.Equals(
            Path.GetFullPath(left),
            Path.GetFullPath(right),
            StringComparison.OrdinalIgnoreCase);
    }

    private static string? InferDocumentKind(
        string? currentPath,
        ProjectRuntimeStateModel stateBefore)
    {
        var pathBasedKind = InferDocumentKindFromPath(currentPath);
        if (!string.IsNullOrWhiteSpace(pathBasedKind))
        {
            return pathBasedKind;
        }

        return stateBefore.CurrentDocument is null ? null : "part";
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

    private static SolidWorksSessionSnapshot CoerceSnapshotDocumentKind(
        SolidWorksSessionSnapshot sessionSnapshot,
        string documentKind)
    {
        if (!sessionSnapshot.HasActiveDocument)
        {
            return sessionSnapshot;
        }

        return sessionSnapshot with
        {
            ActiveDocumentKind = documentKind
        };
    }

    private static int? TryReadInt(object? value)
    {
        try
        {
            return ComInterop.ToInt32(value);
        }
        catch
        {
            return null;
        }
    }

    private static bool? TryConvertToBoolean(object? value)
    {
        try
        {
            return ComInterop.ToBoolean(value);
        }
        catch
        {
            return null;
        }
    }

    private static double MillimetersToMeters(double millimeters)
    {
        return millimeters / 1000d;
    }

    private static double MetersToMillimeters(double meters)
    {
        return meters * 1000d;
    }
}

internal sealed record DocumentOperationResult
{
    public required SolidWorksSessionSnapshot Session { get; init; }
    public required Dictionary<string, object?> OperationDetails { get; init; }
}

internal sealed record SaveOperationResult
{
    public required bool Success { get; init; }
    public required string SaveMode { get; init; }
    public int? Errors { get; init; }
    public int? Warnings { get; init; }
    public string? FailureReason { get; init; }
}

internal sealed record ExportOperationResult
{
    public required bool Success { get; init; }
    public required string ExportMode { get; init; }
    public int? Errors { get; init; }
    public int? Warnings { get; init; }
    public string? FailureReason { get; init; }
}

internal sealed record PlaneSelectionResult
{
    public required object Feature { get; init; }
    public required string FeatureName { get; init; }
    public required string Resolution { get; init; }
}
