using SolidWorksMcp.SolidWorksWorker.Protocol;

namespace SolidWorksWorker.Tests;

public sealed class WorkerMessageReaderTests
{
    [Fact]
    public void ReadRequest_ParsesExportStepCommand()
    {
        const string Payload = """
            {"messageType":"execute_command_request","requestId":"export-1","protocolVersion":"0.1.0","command":{"kind":"export_step","path":"C:\\SolidWorksMcpValidation\\slice4\\part.step"},"stateBefore":{"backendMode":"solidworks","baselineVersion":"SolidWorks 2022","availableTools":["export_step"],"currentDocument":{"documentType":"part","name":"Part1","units":"mm","sketches":[],"features":[],"exports":[],"modified":false,"baselineVersion":"2022"}},"desiredSolidWorksMajorVersion":2022}
            """;

        var request = Assert.IsType<ExecuteCommandRequest>(
            WorkerMessageReader.ReadRequest(Payload));

        var command = Assert.IsType<ExportStepCommand>(request.Command);
        Assert.Equal("export_step", command.Kind);
        Assert.Equal(
            @"C:\SolidWorksMcpValidation\slice4\part.step",
            command.Path);
    }

    [Fact]
    public void ReadRequest_ParsesDrawLineCommand()
    {
        const string Payload = """
            {"messageType":"execute_command_request","requestId":"line-1","protocolVersion":"0.1.0","command":{"kind":"draw_line","start":{"x":0,"y":0},"end":{"x":30,"y":5},"construction":true},"stateBefore":{"backendMode":"solidworks","baselineVersion":"SolidWorks 2022","availableTools":["draw_line"],"currentDocument":{"documentType":"part","name":"Part1","units":"mm","selectedPlane":"Front Plane","activeSketchId":"sketch-1","sketches":[{"id":"sketch-1","plane":"Front Plane","isOpen":true,"entities":[],"dimensions":[]}],"features":[],"exports":[],"modified":true,"baselineVersion":"2022"}},"desiredSolidWorksMajorVersion":2022}
            """;

        var request = Assert.IsType<ExecuteCommandRequest>(
            WorkerMessageReader.ReadRequest(Payload));

        var command = Assert.IsType<DrawLineCommand>(request.Command);
        Assert.Equal("draw_line", command.Kind);
        Assert.Equal(0, command.Start.X);
        Assert.Equal(0, command.Start.Y);
        Assert.Equal(30, command.End.X);
        Assert.Equal(5, command.End.Y);
        Assert.True(command.Construction);
    }

    [Fact]
    public void ReadRequest_ParsesDrawCenteredRectangleCommand()
    {
        const string Payload = """
            {"messageType":"execute_command_request","requestId":"rectangle-1","protocolVersion":"0.1.0","command":{"kind":"draw_centered_rectangle","center":{"x":0,"y":0},"corner":{"x":25,"y":15},"construction":false},"stateBefore":{"backendMode":"solidworks","baselineVersion":"SolidWorks 2022","availableTools":["draw_centered_rectangle"],"currentDocument":{"documentType":"part","name":"Part1","units":"mm","selectedPlane":"Front Plane","activeSketchId":"sketch-1","sketches":[{"id":"sketch-1","plane":"Front Plane","isOpen":true,"entities":[],"dimensions":[]}],"features":[],"exports":[],"modified":true,"baselineVersion":"2022"}},"desiredSolidWorksMajorVersion":2022}
            """;

        var request = Assert.IsType<ExecuteCommandRequest>(
            WorkerMessageReader.ReadRequest(Payload));

        var command = Assert.IsType<DrawCenteredRectangleCommand>(request.Command);
        Assert.Equal("draw_centered_rectangle", command.Kind);
        Assert.Equal(0, command.Center.X);
        Assert.Equal(0, command.Center.Y);
        Assert.Equal(25, command.Corner.X);
        Assert.Equal(15, command.Corner.Y);
        Assert.False(command.Construction);
    }

    [Fact]
    public void ReadRequest_ParsesAddDimensionCommand()
    {
        const string Payload = """
            {"messageType":"execute_command_request","requestId":"dimension-1","protocolVersion":"0.1.0","command":{"kind":"add_dimension","entityId":"line-1","value":30,"orientation":"horizontal"},"stateBefore":{"backendMode":"solidworks","baselineVersion":"SolidWorks 2022","availableTools":["add_dimension"],"currentDocument":{"documentType":"part","name":"Part1","units":"mm","selectedPlane":"Front Plane","activeSketchId":"sketch-1","sketches":[{"id":"sketch-1","plane":"Front Plane","isOpen":true,"entities":[{"id":"line-1","kind":"line","start":{"x":0,"y":0},"end":{"x":30,"y":0},"construction":false}],"dimensions":[]}],"features":[],"exports":[],"modified":true,"baselineVersion":"2022"}},"desiredSolidWorksMajorVersion":2022}
            """;

        var request = Assert.IsType<ExecuteCommandRequest>(
            WorkerMessageReader.ReadRequest(Payload));

        var command = Assert.IsType<AddDimensionCommand>(request.Command);
        Assert.Equal("add_dimension", command.Kind);
        Assert.Equal("line-1", command.EntityId);
        Assert.Equal(30, command.Value);
        Assert.Equal("horizontal", command.Orientation);
    }
}
