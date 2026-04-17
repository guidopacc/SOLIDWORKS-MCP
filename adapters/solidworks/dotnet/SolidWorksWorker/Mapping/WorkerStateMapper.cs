using System.IO;
using SolidWorksMcp.SolidWorksWorker.Protocol;
using SolidWorksMcp.SolidWorksWorker.Services;

namespace SolidWorksMcp.SolidWorksWorker.Mapping;

internal sealed class WorkerStateMapper
{
    public ProjectRuntimeStateModel Map(
        ProjectRuntimeStateModel stateBefore,
        SolidWorksSessionSnapshot sessionSnapshot,
        WorkerCadCommand? executedCommand = null,
        IReadOnlyDictionary<string, object?>? operationDetails = null)
    {
        var nextState = CloneState(stateBefore);
        nextState = nextState with
        {
            BackendMode = "solidworks",
            BaselineVersion = string.IsNullOrWhiteSpace(stateBefore.BaselineVersion)
                ? "SolidWorks 2022"
                : stateBefore.BaselineVersion,
            AvailableTools = [.. stateBefore.AvailableTools]
        };

        if (!sessionSnapshot.HasActiveDocument ||
            !string.Equals(
                sessionSnapshot.ActiveDocumentKind,
                "part",
                StringComparison.OrdinalIgnoreCase))
        {
            return nextState with
            {
                CurrentDocument = null
            };
        }

        var currentDocument = CreateDocumentBase(stateBefore.CurrentDocument, sessionSnapshot);
        currentDocument = ApplyCommandMutation(currentDocument, executedCommand, operationDetails);

        return nextState with
        {
            CurrentDocument = currentDocument
        };
    }

    private static PartDocumentStateModel CreateDocumentBase(
        PartDocumentStateModel? stateBeforeDocument,
        SolidWorksSessionSnapshot sessionSnapshot)
    {
        if (stateBeforeDocument is null)
        {
            return new PartDocumentStateModel
            {
                Name = sessionSnapshot.ActiveDocumentName ?? "Untitled",
                SavedPath = sessionSnapshot.ActiveDocumentPath,
                Modified = sessionSnapshot.ActiveDocumentModified ?? false
            };
        }

        return stateBeforeDocument with
        {
            Name = sessionSnapshot.ActiveDocumentName ?? stateBeforeDocument.Name,
            SavedPath = sessionSnapshot.ActiveDocumentPath ?? stateBeforeDocument.SavedPath,
            Modified = sessionSnapshot.ActiveDocumentModified ?? stateBeforeDocument.Modified,
            Sketches =
            [
                .. stateBeforeDocument.Sketches.Select(CloneSketch)
            ],
            Features =
            [
                .. stateBeforeDocument.Features.Select(CloneFeature)
            ],
            Exports =
            [
                .. stateBeforeDocument.Exports.Select(CloneExport)
            ]
        };
    }

    private static PartDocumentStateModel ApplyCommandMutation(
        PartDocumentStateModel document,
        WorkerCadCommand? executedCommand,
        IReadOnlyDictionary<string, object?>? operationDetails)
    {
        if (executedCommand is null)
        {
            return document;
        }

        return executedCommand switch
        {
            NewPartCommand => document with
            {
                SelectedPlane = null,
                ActiveSketchId = null,
                Sketches = [],
                Features = [],
                Exports = []
            },
            SelectPlaneCommand selectPlaneCommand => document with
            {
                SelectedPlane = selectPlaneCommand.Plane,
                Modified = true
            },
            StartSketchCommand => StartSketch(document),
            DrawLineCommand drawLineCommand => DrawLine(document, drawLineCommand),
            DrawCircleCommand drawCircleCommand => DrawCircle(document, drawCircleCommand),
            DrawCenteredRectangleCommand drawCenteredRectangleCommand => DrawCenteredRectangle(
                document,
                drawCenteredRectangleCommand),
            AddDimensionCommand addDimensionCommand => AddDimension(
                document,
                addDimensionCommand,
                operationDetails),
            CloseSketchCommand => CloseSketch(document),
            ExtrudeBossCommand extrudeBossCommand => ExtrudeBoss(document, extrudeBossCommand),
            ExportStepCommand exportStepCommand => ExportStep(
                document,
                exportStepCommand,
                operationDetails),
            SavePartCommand => document,
            _ => document
        };
    }

    private static PartDocumentStateModel StartSketch(PartDocumentStateModel document)
    {
        var nextSketchId = $"sketch-{document.Sketches.Count + 1}";
        var plane = document.SelectedPlane ?? SolidWorksApiConstants.FrontPlaneName;

        var sketches = new List<SketchStateModel>(document.Sketches.Select(CloneSketch))
        {
            new()
            {
                Id = nextSketchId,
                Plane = plane,
                IsOpen = true,
                Entities = [],
                Dimensions = []
            }
        };

        return document with
        {
            Sketches = sketches,
            ActiveSketchId = nextSketchId,
            Modified = true
        };
    }

    private static PartDocumentStateModel DrawCircle(
        PartDocumentStateModel document,
        DrawCircleCommand command)
    {
        if (string.IsNullOrWhiteSpace(document.ActiveSketchId))
        {
            return document;
        }

        var sketches = new List<SketchStateModel>();
        foreach (var sketch in document.Sketches)
        {
            if (!string.Equals(sketch.Id, document.ActiveSketchId, StringComparison.Ordinal))
            {
                sketches.Add(CloneSketch(sketch));
                continue;
            }

            var updatedEntities = new List<SketchEntityModel>(sketch.Entities.Select(CloneSketchEntity))
            {
                new()
                {
                    Id = $"circle-{sketch.Entities.Count + 1}",
                    Kind = "circle",
                    Center = new Point2DModel
                    {
                        X = command.Center.X,
                        Y = command.Center.Y
                    },
                    Radius = command.Radius,
                    Construction = command.Construction
                }
            };

            sketches.Add(sketch with
            {
                Entities = updatedEntities
            });
        }

        return document with
        {
            Sketches = sketches,
            Modified = true
        };
    }

    private static PartDocumentStateModel DrawLine(
        PartDocumentStateModel document,
        DrawLineCommand command)
    {
        if (string.IsNullOrWhiteSpace(document.ActiveSketchId))
        {
            return document;
        }

        var sketches = new List<SketchStateModel>();
        foreach (var sketch in document.Sketches)
        {
            if (!string.Equals(sketch.Id, document.ActiveSketchId, StringComparison.Ordinal))
            {
                sketches.Add(CloneSketch(sketch));
                continue;
            }

            var updatedEntities = new List<SketchEntityModel>(sketch.Entities.Select(CloneSketchEntity))
            {
                new()
                {
                    Id = $"line-{sketch.Entities.Count + 1}",
                    Kind = "line",
                    Start = new Point2DModel
                    {
                        X = command.Start.X,
                        Y = command.Start.Y
                    },
                    End = new Point2DModel
                    {
                        X = command.End.X,
                        Y = command.End.Y
                    },
                    Construction = command.Construction
                }
            };

            sketches.Add(sketch with
            {
                Entities = updatedEntities
            });
        }

        return document with
        {
            Sketches = sketches,
            Modified = true
        };
    }

    private static PartDocumentStateModel DrawCenteredRectangle(
        PartDocumentStateModel document,
        DrawCenteredRectangleCommand command)
    {
        if (string.IsNullOrWhiteSpace(document.ActiveSketchId))
        {
            return document;
        }

        var sketches = new List<SketchStateModel>();
        foreach (var sketch in document.Sketches)
        {
            if (!string.Equals(sketch.Id, document.ActiveSketchId, StringComparison.Ordinal))
            {
                sketches.Add(CloneSketch(sketch));
                continue;
            }

            var updatedEntities = new List<SketchEntityModel>(sketch.Entities.Select(CloneSketchEntity))
            {
                new()
                {
                    Id = $"rectangle-{sketch.Entities.Count + 1}",
                    Kind = "centered_rectangle",
                    Center = new Point2DModel
                    {
                        X = command.Center.X,
                        Y = command.Center.Y
                    },
                    Corner = new Point2DModel
                    {
                        X = command.Corner.X,
                        Y = command.Corner.Y
                    },
                    Construction = command.Construction
                }
            };

            sketches.Add(sketch with
            {
                Entities = updatedEntities
            });
        }

        return document with
        {
            Sketches = sketches,
            Modified = true
        };
    }

    private static PartDocumentStateModel AddDimension(
        PartDocumentStateModel document,
        AddDimensionCommand command,
        IReadOnlyDictionary<string, object?>? operationDetails)
    {
        if (string.IsNullOrWhiteSpace(document.ActiveSketchId))
        {
            return document;
        }

        var appliedOrientation = TryReadString(operationDetails, "orientationApplied") ?? command.Orientation;
        var sketches = new List<SketchStateModel>();
        foreach (var sketch in document.Sketches)
        {
            if (!string.Equals(sketch.Id, document.ActiveSketchId, StringComparison.Ordinal))
            {
                sketches.Add(CloneSketch(sketch));
                continue;
            }

            if (!sketch.Entities.Any(entity => string.Equals(entity.Id, command.EntityId, StringComparison.Ordinal)))
            {
                sketches.Add(CloneSketch(sketch));
                continue;
            }

            var updatedDimensions = new List<SketchDimensionModel>(sketch.Dimensions.Select(dimension => dimension with { }))
            {
                new()
                {
                    Id = $"dimension-{sketch.Dimensions.Count + 1}",
                    EntityId = command.EntityId,
                    Value = command.Value,
                    Orientation = appliedOrientation
                }
            };

            sketches.Add(sketch with
            {
                Dimensions = updatedDimensions
            });
        }

        return document with
        {
            Sketches = sketches,
            Modified = true
        };
    }

    private static PartDocumentStateModel CloseSketch(PartDocumentStateModel document)
    {
        var sketches = new List<SketchStateModel>();
        foreach (var sketch in document.Sketches)
        {
            if (!string.Equals(sketch.Id, document.ActiveSketchId, StringComparison.Ordinal))
            {
                sketches.Add(CloneSketch(sketch));
                continue;
            }

            sketches.Add(sketch with
            {
                IsOpen = false
            });
        }

        return document with
        {
            Sketches = sketches,
            ActiveSketchId = null,
            Modified = true
        };
    }

    private static PartDocumentStateModel ExtrudeBoss(
        PartDocumentStateModel document,
        ExtrudeBossCommand command)
    {
        var features = new List<FeatureStateModel>(document.Features.Select(CloneFeature))
        {
            new()
            {
                Id = $"feature-{document.Features.Count + 1}",
                Kind = "extrude_boss",
                SketchId = command.SketchId,
                Depth = command.Depth,
                MergeResult = command.MergeResult
            }
        };

        return document with
        {
            Features = features,
            Modified = true
        };
    }

    private static PartDocumentStateModel ExportStep(
        PartDocumentStateModel document,
        ExportStepCommand command,
        IReadOnlyDictionary<string, object?>? operationDetails)
    {
        var resolvedPath = TryReadResolvedPath(operationDetails) ?? command.Path;
        var exports = new List<ExportArtifactModel>(
            document.Exports
                .Select(CloneExport)
                .Where(existingExport => !PathsMatch(existingExport.Path, resolvedPath)))
        {
            new()
            {
                Kind = "step",
                Path = resolvedPath
            }
        };

        return document with
        {
            Exports = exports
        };
    }

    private static ProjectRuntimeStateModel CloneState(ProjectRuntimeStateModel state)
    {
        return state with
        {
            AvailableTools = [.. state.AvailableTools],
            CurrentDocument = state.CurrentDocument is null
                ? null
                : CloneDocument(state.CurrentDocument)
        };
    }

    private static PartDocumentStateModel CloneDocument(PartDocumentStateModel document)
    {
        return document with
        {
            Sketches =
            [
                .. document.Sketches.Select(CloneSketch)
            ],
            Features =
            [
                .. document.Features.Select(CloneFeature)
            ],
            Exports =
            [
                .. document.Exports.Select(CloneExport)
            ]
        };
    }

    private static SketchStateModel CloneSketch(SketchStateModel sketch)
    {
        return sketch with
        {
            Entities =
            [
                .. sketch.Entities.Select(CloneSketchEntity)
            ],
            Dimensions =
            [
                .. sketch.Dimensions.Select(dimension => dimension with { })
            ]
        };
    }

    private static SketchEntityModel CloneSketchEntity(SketchEntityModel entity)
    {
        return entity with
        {
            Start = entity.Start is null ? null : entity.Start with { },
            End = entity.End is null ? null : entity.End with { },
            Center = entity.Center is null ? null : entity.Center with { },
            Corner = entity.Corner is null ? null : entity.Corner with { }
        };
    }

    private static FeatureStateModel CloneFeature(FeatureStateModel feature)
    {
        return feature with { };
    }

    private static ExportArtifactModel CloneExport(ExportArtifactModel exportArtifact)
    {
        return exportArtifact with { };
    }

    private static string? TryReadResolvedPath(
        IReadOnlyDictionary<string, object?>? operationDetails)
    {
        if (operationDetails is null ||
            !operationDetails.TryGetValue("resolvedPath", out var value) ||
            value is not string resolvedPath ||
            string.IsNullOrWhiteSpace(resolvedPath))
        {
            return null;
        }

        return resolvedPath;
    }

    private static string? TryReadString(
        IReadOnlyDictionary<string, object?>? operationDetails,
        string key)
    {
        if (operationDetails is null ||
            !operationDetails.TryGetValue(key, out var value) ||
            value is not string stringValue ||
            string.IsNullOrWhiteSpace(stringValue))
        {
            return null;
        }

        return stringValue;
    }

    private static bool PathsMatch(string? left, string? right)
    {
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
        {
            return false;
        }

        try
        {
            return string.Equals(
                Path.GetFullPath(left),
                Path.GetFullPath(right),
                StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
        }
    }
}
