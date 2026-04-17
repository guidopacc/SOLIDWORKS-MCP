using SolidWorksMcp.SolidWorksWorker.Mapping;
using SolidWorksMcp.SolidWorksWorker.Protocol;
using SolidWorksMcp.SolidWorksWorker.Services;

namespace SolidWorksWorker.Tests;

public sealed class WorkerStateMapperTests
{
    [Fact]
    public void Map_ExportStep_AppendsResolvedExportArtifact()
    {
        var stateBefore = new ProjectRuntimeStateModel
        {
            BackendMode = "solidworks",
            BaselineVersion = "SolidWorks 2022",
            AvailableTools = ["export_step"],
            CurrentDocument = new PartDocumentStateModel
            {
                Name = "slice4-part.SLDPRT",
                SavedPath = @"C:\SolidWorksMcpValidation\slice4\slice4-part.SLDPRT",
                Modified = false,
                Sketches = [],
                Features = [],
                Exports = []
            }
        };

        var sessionSnapshot = new SolidWorksSessionSnapshot
        {
            Connected = true,
            ConnectionMode = "launched_new",
            SolidWorksProgIdRegistered = true,
            HasActiveDocument = true,
            ActiveDocumentKind = "part",
            ActiveDocumentName = "slice4-part.SLDPRT",
            ActiveDocumentPath = @"C:\SolidWorksMcpValidation\slice4\slice4-part.SLDPRT",
            ActiveDocumentModified = false,
            SolidWorksMajorVersion = 2022,
            RevisionNumber = "30.4.0",
            BaselineSatisfied = true
        };

        var mapper = new WorkerStateMapper();
        var nextState = mapper.Map(
            stateBefore,
            sessionSnapshot,
            new ExportStepCommand
            {
                Kind = "export_step",
                Path = @"slice4\relative.step"
            },
            new Dictionary<string, object?>
            {
                ["resolvedPath"] = @"C:\SolidWorksMcpValidation\slice4\slice4-export.step"
            });

        Assert.NotNull(nextState.CurrentDocument);
        var currentDocument = nextState.CurrentDocument!;
        var exportArtifact = Assert.Single(currentDocument.Exports);
        Assert.Equal("step", exportArtifact.Kind);
        Assert.Equal(
            @"C:\SolidWorksMcpValidation\slice4\slice4-export.step",
            exportArtifact.Path);
        Assert.Equal(
            @"C:\SolidWorksMcpValidation\slice4\slice4-part.SLDPRT",
            currentDocument.SavedPath);
        Assert.False(currentDocument.Modified);
    }

    [Fact]
    public void Map_DrawLine_AppendsLineEntityToActiveSketch()
    {
        var stateBefore = new ProjectRuntimeStateModel
        {
            BackendMode = "solidworks",
            BaselineVersion = "SolidWorks 2022",
            AvailableTools = ["draw_line"],
            CurrentDocument = new PartDocumentStateModel
            {
                Name = "Part1",
                SelectedPlane = "Front Plane",
                ActiveSketchId = "sketch-1",
                Modified = false,
                Sketches =
                [
                    new SketchStateModel
                    {
                        Id = "sketch-1",
                        Plane = "Front Plane",
                        IsOpen = true,
                        Entities = [],
                        Dimensions = []
                    }
                ],
                Features = [],
                Exports = []
            }
        };

        var sessionSnapshot = new SolidWorksSessionSnapshot
        {
            Connected = true,
            ConnectionMode = "attached_existing",
            SolidWorksProgIdRegistered = true,
            HasActiveDocument = true,
            ActiveDocumentKind = "part",
            ActiveDocumentName = "Part1",
            ActiveDocumentModified = true,
            SolidWorksMajorVersion = 2022,
            RevisionNumber = "30.4.0",
            BaselineSatisfied = true
        };

        var mapper = new WorkerStateMapper();
        var nextState = mapper.Map(
            stateBefore,
            sessionSnapshot,
            new DrawLineCommand
            {
                Kind = "draw_line",
                Start = new Point2DModel
                {
                    X = 0,
                    Y = 0
                },
                End = new Point2DModel
                {
                    X = 30,
                    Y = 5
                },
                Construction = true
            });

        Assert.NotNull(nextState.CurrentDocument);
        var currentDocument = nextState.CurrentDocument!;
        var sketch = Assert.Single(currentDocument.Sketches);
        var line = Assert.Single(sketch.Entities);
        Assert.Equal("line", line.Kind);
        Assert.Equal(0, line.Start?.X);
        Assert.Equal(0, line.Start?.Y);
        Assert.Equal(30, line.End?.X);
        Assert.Equal(5, line.End?.Y);
        Assert.True(line.Construction);
        Assert.True(currentDocument.Modified);
    }

    [Fact]
    public void Map_DrawCenteredRectangle_AppendsRectangleEntityToActiveSketch()
    {
        var stateBefore = new ProjectRuntimeStateModel
        {
            BackendMode = "solidworks",
            BaselineVersion = "SolidWorks 2022",
            AvailableTools = ["draw_centered_rectangle"],
            CurrentDocument = new PartDocumentStateModel
            {
                Name = "Part1",
                SelectedPlane = "Front Plane",
                ActiveSketchId = "sketch-1",
                Modified = false,
                Sketches =
                [
                    new SketchStateModel
                    {
                        Id = "sketch-1",
                        Plane = "Front Plane",
                        IsOpen = true,
                        Entities = [],
                        Dimensions = []
                    }
                ],
                Features = [],
                Exports = []
            }
        };

        var sessionSnapshot = new SolidWorksSessionSnapshot
        {
            Connected = true,
            ConnectionMode = "attached_existing",
            SolidWorksProgIdRegistered = true,
            HasActiveDocument = true,
            ActiveDocumentKind = "part",
            ActiveDocumentName = "Part1",
            ActiveDocumentModified = true,
            SolidWorksMajorVersion = 2022,
            RevisionNumber = "30.4.0",
            BaselineSatisfied = true
        };

        var mapper = new WorkerStateMapper();
        var nextState = mapper.Map(
            stateBefore,
            sessionSnapshot,
            new DrawCenteredRectangleCommand
            {
                Kind = "draw_centered_rectangle",
                Center = new Point2DModel
                {
                    X = 0,
                    Y = 0
                },
                Corner = new Point2DModel
                {
                    X = 25,
                    Y = 15
                },
                Construction = false
            });

        Assert.NotNull(nextState.CurrentDocument);
        var currentDocument = nextState.CurrentDocument!;
        var sketch = Assert.Single(currentDocument.Sketches);
        var rectangle = Assert.Single(sketch.Entities);
        Assert.Equal("centered_rectangle", rectangle.Kind);
        Assert.Equal(0, rectangle.Center?.X);
        Assert.Equal(0, rectangle.Center?.Y);
        Assert.Equal(25, rectangle.Corner?.X);
        Assert.Equal(15, rectangle.Corner?.Y);
        Assert.True(currentDocument.Modified);
    }

    [Fact]
    public void Map_AddDimension_AppendsDimensionToActiveSketch()
    {
        var stateBefore = new ProjectRuntimeStateModel
        {
            BackendMode = "solidworks",
            BaselineVersion = "SolidWorks 2022",
            AvailableTools = ["add_dimension"],
            CurrentDocument = new PartDocumentStateModel
            {
                Name = "Part1",
                SelectedPlane = "Front Plane",
                ActiveSketchId = "sketch-1",
                Modified = false,
                Sketches =
                [
                    new SketchStateModel
                    {
                        Id = "sketch-1",
                        Plane = "Front Plane",
                        IsOpen = true,
                        Entities =
                        [
                            new SketchEntityModel
                            {
                                Id = "line-1",
                                Kind = "line",
                                Start = new Point2DModel
                                {
                                    X = 0,
                                    Y = 0
                                },
                                End = new Point2DModel
                                {
                                    X = 30,
                                    Y = 0
                                },
                                Construction = false
                            }
                        ],
                        Dimensions = []
                    }
                ],
                Features = [],
                Exports = []
            }
        };

        var sessionSnapshot = new SolidWorksSessionSnapshot
        {
            Connected = true,
            ConnectionMode = "attached_existing",
            SolidWorksProgIdRegistered = true,
            HasActiveDocument = true,
            ActiveDocumentKind = "part",
            ActiveDocumentName = "Part1",
            ActiveDocumentModified = true,
            SolidWorksMajorVersion = 2022,
            RevisionNumber = "30.4.0",
            BaselineSatisfied = true
        };

        var mapper = new WorkerStateMapper();
        var nextState = mapper.Map(
            stateBefore,
            sessionSnapshot,
            new AddDimensionCommand
            {
                Kind = "add_dimension",
                EntityId = "line-1",
                Value = 30,
                Orientation = null
            },
            new Dictionary<string, object?>
            {
                ["orientationApplied"] = "horizontal"
            });

        Assert.NotNull(nextState.CurrentDocument);
        var currentDocument = nextState.CurrentDocument!;
        var sketch = Assert.Single(currentDocument.Sketches);
        var dimension = Assert.Single(sketch.Dimensions);
        Assert.Equal("line-1", dimension.EntityId);
        Assert.Equal(30, dimension.Value);
        Assert.Equal("horizontal", dimension.Orientation);
        Assert.True(currentDocument.Modified);
    }
}
