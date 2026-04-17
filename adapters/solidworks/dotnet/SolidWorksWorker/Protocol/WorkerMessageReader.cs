using System.Text.Json;
using SolidWorksMcp.SolidWorksWorker.Errors;

namespace SolidWorksMcp.SolidWorksWorker.Protocol;

internal static class WorkerMessageReader
{
    public static WorkerRequest ReadRequest(string line)
    {
        using var document = JsonDocument.Parse(line);
        var root = document.RootElement;

        var messageType = ReadRequiredString(root, "messageType");
        var requestId = ReadRequiredString(root, "requestId");
        var protocolVersion = ReadRequiredString(root, "protocolVersion");

        return messageType switch
        {
            "handshake_request" => new HandshakeRequest
            {
                MessageType = messageType,
                RequestId = requestId,
                ProtocolVersion = protocolVersion,
                Client = ReadIdentity(root),
                DesiredSolidWorksMajorVersion = ReadOptionalInt(
                    root,
                    "desiredSolidWorksMajorVersion",
                    WorkerProtocolConstants.BaselineSolidWorksMajorVersion)
            },
            "execute_command_request" => new ExecuteCommandRequest
            {
                MessageType = messageType,
                RequestId = requestId,
                ProtocolVersion = protocolVersion,
                Command = ReadCommand(root),
                StateBefore = ReadState(root),
                DesiredSolidWorksMajorVersion = ReadOptionalInt(
                    root,
                    "desiredSolidWorksMajorVersion",
                    WorkerProtocolConstants.BaselineSolidWorksMajorVersion),
                TimeoutMs = ReadNullableInt(root, "timeoutMs")
            },
            "shutdown_request" => new ShutdownRequest
            {
                MessageType = messageType,
                RequestId = requestId,
                ProtocolVersion = protocolVersion
            },
            _ => new UnsupportedWorkerRequest
            {
                MessageType = messageType,
                UnsupportedMessageType = messageType,
                RequestId = requestId,
                ProtocolVersion = protocolVersion
            }
        };
    }

    private static WorkerIdentity? ReadIdentity(JsonElement root)
    {
        if (!root.TryGetProperty("client", out var client) ||
            client.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        return new WorkerIdentity
        {
            Name = ReadRequiredString(client, "name"),
            Version = ReadRequiredString(client, "version")
        };
    }

    private static WorkerCadCommand ReadCommand(JsonElement root)
    {
        if (!root.TryGetProperty("command", out var command))
        {
            throw WorkerErrorFactory.InvalidInput(
                "Worker request is missing the command payload.",
                "command_parse",
                new Dictionary<string, object?>
                {
                    ["missingProperty"] = "command"
                });
        }

        var kind = ReadRequiredString(command, "kind");

        return kind switch
        {
            "new_part" => new NewPartCommand
            {
                Kind = kind,
                Name = ReadOptionalString(command, "name")
            },
            "select_plane" => new SelectPlaneCommand
            {
                Kind = kind,
                Plane = ReadRequiredString(command, "plane")
            },
            "start_sketch" => new StartSketchCommand
            {
                Kind = kind
            },
            "draw_circle" => new DrawCircleCommand
            {
                Kind = kind,
                Center = ReadPoint(command, "center"),
                Radius = ReadRequiredDouble(command, "radius"),
                Construction = ReadOptionalBool(command, "construction")
            },
            "draw_line" => new DrawLineCommand
            {
                Kind = kind,
                Start = ReadPoint(command, "start"),
                End = ReadPoint(command, "end"),
                Construction = ReadOptionalBool(command, "construction")
            },
            "draw_centered_rectangle" => new DrawCenteredRectangleCommand
            {
                Kind = kind,
                Center = ReadPoint(command, "center"),
                Corner = ReadPoint(command, "corner"),
                Construction = ReadOptionalBool(command, "construction")
            },
            "add_dimension" => new AddDimensionCommand
            {
                Kind = kind,
                EntityId = ReadRequiredString(command, "entityId"),
                Value = ReadRequiredDouble(command, "value"),
                Orientation = ReadOptionalString(command, "orientation")
            },
            "close_sketch" => new CloseSketchCommand
            {
                Kind = kind
            },
            "extrude_boss" => new ExtrudeBossCommand
            {
                Kind = kind,
                SketchId = ReadRequiredString(command, "sketchId"),
                Depth = ReadRequiredDouble(command, "depth"),
                MergeResult = ReadOptionalBool(command, "mergeResult", defaultValue: true)
            },
            "save_part" => new SavePartCommand
            {
                Kind = kind,
                Path = ReadRequiredString(command, "path")
            },
            "export_step" => new ExportStepCommand
            {
                Kind = kind,
                Path = ReadRequiredString(command, "path")
            },
            _ => new UnsupportedCadCommand
            {
                Kind = kind
            }
        };
    }

    private static ProjectRuntimeStateModel ReadState(JsonElement root)
    {
        if (!root.TryGetProperty("stateBefore", out var state))
        {
            throw WorkerErrorFactory.InvalidInput(
                "Worker request is missing stateBefore.",
                "state_parse",
                new Dictionary<string, object?>
                {
                    ["missingProperty"] = "stateBefore"
                });
        }

        PartDocumentStateModel? currentDocument = null;
        if (state.TryGetProperty("currentDocument", out var currentDocumentElement) &&
            currentDocumentElement.ValueKind != JsonValueKind.Null)
        {
            currentDocument = new PartDocumentStateModel
            {
                DocumentType = ReadOptionalString(currentDocumentElement, "documentType") ?? "part",
                Name = ReadOptionalString(currentDocumentElement, "name") ?? "Untitled",
                Units = ReadOptionalString(currentDocumentElement, "units") ?? "mm",
                SavedPath = ReadOptionalString(currentDocumentElement, "savedPath"),
                Modified = ReadOptionalBool(currentDocumentElement, "modified"),
                SelectedPlane = ReadOptionalString(currentDocumentElement, "selectedPlane"),
                ActiveSketchId = ReadOptionalString(currentDocumentElement, "activeSketchId"),
                Sketches = ReadSketches(currentDocumentElement, "sketches"),
                Features = ReadFeatures(currentDocumentElement, "features"),
                Exports = ReadExports(currentDocumentElement, "exports"),
                BaselineVersion = ReadOptionalString(currentDocumentElement, "baselineVersion") ?? "2022"
            };
        }

        return new ProjectRuntimeStateModel
        {
            BackendMode = ReadOptionalString(state, "backendMode") ?? "solidworks",
            BaselineVersion = ReadOptionalString(state, "baselineVersion") ?? "SolidWorks 2022",
            AvailableTools = ReadStringArray(state, "availableTools"),
            CurrentDocument = currentDocument
        };
    }

    private static string ReadRequiredString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) ||
            property.ValueKind != JsonValueKind.String)
        {
            throw WorkerErrorFactory.InvalidInput(
                $"Worker request property '{propertyName}' must be a string.",
                "protocol_parse",
                new Dictionary<string, object?>
                {
                    ["property"] = propertyName
                });
        }

        return property.GetString() ?? string.Empty;
    }

    private static string? ReadOptionalString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) ||
            property.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        if (property.ValueKind != JsonValueKind.String)
        {
            throw WorkerErrorFactory.InvalidInput(
                $"Worker request property '{propertyName}' must be a string when provided.",
                "protocol_parse",
                new Dictionary<string, object?>
                {
                    ["property"] = propertyName
                });
        }

        return property.GetString();
    }

    private static int ReadOptionalInt(
        JsonElement element,
        string propertyName,
        int defaultValue)
    {
        if (!element.TryGetProperty(propertyName, out var property) ||
            property.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return defaultValue;
        }

        if (!property.TryGetInt32(out var value))
        {
            throw WorkerErrorFactory.InvalidInput(
                $"Worker request property '{propertyName}' must be an integer.",
                "protocol_parse",
                new Dictionary<string, object?>
                {
                    ["property"] = propertyName
                });
        }

        return value;
    }

    private static int? ReadNullableInt(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) ||
            property.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        if (!property.TryGetInt32(out var value))
        {
            throw WorkerErrorFactory.InvalidInput(
                $"Worker request property '{propertyName}' must be an integer when provided.",
                "protocol_parse",
                new Dictionary<string, object?>
                {
                    ["property"] = propertyName
                });
        }

        return value;
    }

    private static double ReadRequiredDouble(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) ||
            property.ValueKind != JsonValueKind.Number ||
            !property.TryGetDouble(out var value))
        {
            throw WorkerErrorFactory.InvalidInput(
                $"Worker request property '{propertyName}' must be a number.",
                "protocol_parse",
                new Dictionary<string, object?>
                {
                    ["property"] = propertyName
                });
        }

        return value;
    }

    private static bool ReadOptionalBool(
        JsonElement element,
        string propertyName,
        bool defaultValue = false)
    {
        if (!element.TryGetProperty(propertyName, out var property) ||
            property.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return defaultValue;
        }

        if (property.ValueKind is not JsonValueKind.True and not JsonValueKind.False)
        {
            throw WorkerErrorFactory.InvalidInput(
                $"Worker request property '{propertyName}' must be a boolean when provided.",
                "protocol_parse",
                new Dictionary<string, object?>
                {
                    ["property"] = propertyName
                });
        }

        return property.GetBoolean();
    }

    private static Point2DModel ReadPoint(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var point) ||
            point.ValueKind != JsonValueKind.Object)
        {
            throw WorkerErrorFactory.InvalidInput(
                $"Worker request property '{propertyName}' must be an object with x and y.",
                "protocol_parse",
                new Dictionary<string, object?>
                {
                    ["property"] = propertyName
                });
        }

        return new Point2DModel
        {
            X = ReadRequiredDouble(point, "x"),
            Y = ReadRequiredDouble(point, "y")
        };
    }

    private static List<string> ReadStringArray(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) ||
            property.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return [];
        }

        if (property.ValueKind != JsonValueKind.Array)
        {
            throw WorkerErrorFactory.InvalidInput(
                $"Worker request property '{propertyName}' must be an array.",
                "protocol_parse",
                new Dictionary<string, object?>
                {
                    ["property"] = propertyName
                });
        }

        var values = new List<string>();
        foreach (var entry in property.EnumerateArray())
        {
            if (entry.ValueKind != JsonValueKind.String)
            {
                throw WorkerErrorFactory.InvalidInput(
                    $"Worker request property '{propertyName}' must contain only strings.",
                    "protocol_parse",
                    new Dictionary<string, object?>
                    {
                        ["property"] = propertyName
                    });
            }

            values.Add(entry.GetString() ?? string.Empty);
        }

        return values;
    }

    private static List<SketchStateModel> ReadSketches(
        JsonElement element,
        string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) ||
            property.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return [];
        }

        if (property.ValueKind != JsonValueKind.Array)
        {
            throw WorkerErrorFactory.InvalidInput(
                $"Worker request property '{propertyName}' must be an array.",
                "protocol_parse",
                new Dictionary<string, object?>
                {
                    ["property"] = propertyName
                });
        }

        var sketches = new List<SketchStateModel>();
        foreach (var sketch in property.EnumerateArray())
        {
            sketches.Add(new SketchStateModel
            {
                Id = ReadRequiredString(sketch, "id"),
                Plane = ReadRequiredString(sketch, "plane"),
                IsOpen = ReadOptionalBool(sketch, "isOpen"),
                Entities = ReadSketchEntities(sketch, "entities"),
                Dimensions = ReadSketchDimensions(sketch, "dimensions")
            });
        }

        return sketches;
    }

    private static List<SketchEntityModel> ReadSketchEntities(
        JsonElement element,
        string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) ||
            property.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return [];
        }

        if (property.ValueKind != JsonValueKind.Array)
        {
            throw WorkerErrorFactory.InvalidInput(
                $"Worker request property '{propertyName}' must be an array.",
                "protocol_parse",
                new Dictionary<string, object?>
                {
                    ["property"] = propertyName
                });
        }

        var entities = new List<SketchEntityModel>();
        foreach (var entity in property.EnumerateArray())
        {
            entities.Add(new SketchEntityModel
            {
                Id = ReadRequiredString(entity, "id"),
                Kind = ReadRequiredString(entity, "kind"),
                Start = ReadOptionalPoint(entity, "start"),
                End = ReadOptionalPoint(entity, "end"),
                Center = ReadOptionalPoint(entity, "center"),
                Corner = ReadOptionalPoint(entity, "corner"),
                Radius = ReadOptionalDouble(entity, "radius"),
                Construction = ReadOptionalBool(entity, "construction")
            });
        }

        return entities;
    }

    private static List<SketchDimensionModel> ReadSketchDimensions(
        JsonElement element,
        string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) ||
            property.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return [];
        }

        if (property.ValueKind != JsonValueKind.Array)
        {
            throw WorkerErrorFactory.InvalidInput(
                $"Worker request property '{propertyName}' must be an array.",
                "protocol_parse",
                new Dictionary<string, object?>
                {
                    ["property"] = propertyName
                });
        }

        var dimensions = new List<SketchDimensionModel>();
        foreach (var dimension in property.EnumerateArray())
        {
            dimensions.Add(new SketchDimensionModel
            {
                Id = ReadRequiredString(dimension, "id"),
                EntityId = ReadRequiredString(dimension, "entityId"),
                Value = ReadRequiredDouble(dimension, "value"),
                Orientation = ReadOptionalString(dimension, "orientation")
            });
        }

        return dimensions;
    }

    private static List<FeatureStateModel> ReadFeatures(
        JsonElement element,
        string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) ||
            property.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return [];
        }

        if (property.ValueKind != JsonValueKind.Array)
        {
            throw WorkerErrorFactory.InvalidInput(
                $"Worker request property '{propertyName}' must be an array.",
                "protocol_parse",
                new Dictionary<string, object?>
                {
                    ["property"] = propertyName
                });
        }

        var features = new List<FeatureStateModel>();
        foreach (var feature in property.EnumerateArray())
        {
            features.Add(new FeatureStateModel
            {
                Id = ReadRequiredString(feature, "id"),
                Kind = ReadRequiredString(feature, "kind"),
                SketchId = ReadOptionalString(feature, "sketchId"),
                Depth = ReadOptionalDouble(feature, "depth"),
                MergeResult = ReadOptionalNullableBool(feature, "mergeResult"),
                FeatureId = ReadOptionalString(feature, "featureId"),
                Radius = ReadOptionalDouble(feature, "radius")
            });
        }

        return features;
    }

    private static List<ExportArtifactModel> ReadExports(
        JsonElement element,
        string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) ||
            property.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return [];
        }

        if (property.ValueKind != JsonValueKind.Array)
        {
            throw WorkerErrorFactory.InvalidInput(
                $"Worker request property '{propertyName}' must be an array.",
                "protocol_parse",
                new Dictionary<string, object?>
                {
                    ["property"] = propertyName
                });
        }

        var exports = new List<ExportArtifactModel>();
        foreach (var exportArtifact in property.EnumerateArray())
        {
            exports.Add(new ExportArtifactModel
            {
                Kind = ReadRequiredString(exportArtifact, "kind"),
                Path = ReadRequiredString(exportArtifact, "path")
            });
        }

        return exports;
    }

    private static double? ReadOptionalDouble(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) ||
            property.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        if (!property.TryGetDouble(out var value))
        {
            throw WorkerErrorFactory.InvalidInput(
                $"Worker request property '{propertyName}' must be a number when provided.",
                "protocol_parse",
                new Dictionary<string, object?>
                {
                    ["property"] = propertyName
                });
        }

        return value;
    }

    private static bool? ReadOptionalNullableBool(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) ||
            property.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        if (property.ValueKind is not JsonValueKind.True and not JsonValueKind.False)
        {
            throw WorkerErrorFactory.InvalidInput(
                $"Worker request property '{propertyName}' must be a boolean when provided.",
                "protocol_parse",
                new Dictionary<string, object?>
                {
                    ["property"] = propertyName
                });
        }

        return property.GetBoolean();
    }

    private static Point2DModel? ReadOptionalPoint(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) ||
            property.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        if (property.ValueKind != JsonValueKind.Object)
        {
            throw WorkerErrorFactory.InvalidInput(
                $"Worker request property '{propertyName}' must be an object when provided.",
                "protocol_parse",
                new Dictionary<string, object?>
                {
                    ["property"] = propertyName
                });
        }

        return new Point2DModel
        {
            X = ReadRequiredDouble(property, "x"),
            Y = ReadRequiredDouble(property, "y")
        };
    }
}
