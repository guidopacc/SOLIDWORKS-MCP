using System.Text.Json;
using SolidWorksMcp.SolidWorksWorker.Commands;
using SolidWorksMcp.SolidWorksWorker.Errors;
using SolidWorksMcp.SolidWorksWorker.Mapping;
using SolidWorksMcp.SolidWorksWorker.Protocol;
using SolidWorksMcp.SolidWorksWorker.Services;

namespace SolidWorksMcp.SolidWorksWorker;

internal static class Program
{
    [STAThread]
    private static int Main()
    {
        using var reader = new StreamReader(Console.OpenStandardInput());
        using var writer = new StreamWriter(Console.OpenStandardOutput())
        {
            AutoFlush = true
        };

        using var sessionService = new SolidWorksSessionService();
        var stateMapper = new WorkerStateMapper();
        var documentService = new SolidWorksDocumentService(sessionService);
        var dispatcher = new WorkerCommandDispatcher(
            sessionService,
            documentService,
            stateMapper);

        while (true)
        {
            var line = reader.ReadLine();
            if (line is null)
            {
                return 0;
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            WorkerResponse? response = null;

            try
            {
                var request = WorkerMessageReader.ReadRequest(line);
                response = dispatcher.Dispatch(request);
            }
            catch (WorkerCommandException error)
            {
                Console.Error.WriteLine(
                    $"Worker protocol error: {error.Code} {error.Message}");
            }
            catch (JsonException error)
            {
                Console.Error.WriteLine(
                    $"Worker received invalid JSON and ignored the line. {error.Message}");
            }
            catch (Exception error)
            {
                Console.Error.WriteLine(
                    $"Worker encountered an unexpected top-level exception: {error}");
            }

            if (response is null)
            {
                continue;
            }

            writer.WriteLine(WorkerJson.SerializeResponse(response));

            if (response is ShutdownResponse)
            {
                return 0;
            }
        }
    }
}
