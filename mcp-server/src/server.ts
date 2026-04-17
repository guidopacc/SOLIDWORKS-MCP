import { Server } from '@modelcontextprotocol/sdk/server/index.js';
import {
  CallToolRequestSchema,
  type CallToolResult,
  ErrorCode,
  GetPromptRequestSchema,
  type GetPromptResult,
  ListPromptsRequestSchema,
  type ListPromptsResult,
  ListResourcesRequestSchema,
  type ListResourcesResult,
  ListToolsRequestSchema,
  type ListToolsResult,
  McpError,
  ReadResourceRequestSchema,
  type ReadResourceResult,
} from '@modelcontextprotocol/sdk/types.js';
import { resolveBackendRuntime } from './application/backend-resolver.js';
import { ProjectService } from './application/project-service.js';
import {
  formatToolErrorEnvelope,
  internalError,
  isSolidWorksMcpError,
} from './domain/errors.js';
import {
  getPromptDefinition,
  PUBLIC_ALPHA_PROMPT_DEFINITIONS,
} from './prompts/prompt-definitions.js';
import {
  getResourceDefinition,
  PUBLIC_ALPHA_RESOURCE_DEFINITIONS,
} from './resources/resource-definitions.js';
import {
  getToolDefinition,
  PUBLIC_ALPHA_TOOL_DEFINITIONS,
} from './tools/tool-definitions.js';

export interface SolidWorksMcpServerContext {
  server: Server;
  projectService: ProjectService;
  dispose: () => Promise<void>;
}

export function createSolidWorksMcpServer(): SolidWorksMcpServerContext {
  const runtime = resolveBackendRuntime();
  const projectService = new ProjectService(runtime.backend);
  projectService.bootstrapAvailableTools(
    PUBLIC_ALPHA_TOOL_DEFINITIONS.map((tool) => tool.name),
  );

  const server = new Server(
    {
      name: 'solidworks-mcp',
      version: '0.1.0',
    },
    {
      capabilities: {
        tools: {},
        prompts: {},
        resources: {},
      },
      instructions:
        'SOLIDWORKS-MCP public alpha for a narrow, validation-backed SolidWorks 2022 workflow on Windows. Supported tools in this alpha are new_part, select_plane, start_sketch, draw_line, draw_circle, draw_centered_rectangle, close_sketch, extrude_boss, save_part, export_step, get_document_state, list_available_tools, and get_project_status. The first public prompt is safe_modeling_session. Public resources are current_document_state at solidworks://document/current-state and public_alpha_boundary at solidworks://alpha/public-boundary. Do not assume dimensions, cut extrude, fillets, assemblies, drawings, or open-document workflows are available.',
    },
  );

  server.setRequestHandler(
    ListToolsRequestSchema,
    async (): Promise<ListToolsResult> => {
      return {
        tools: PUBLIC_ALPHA_TOOL_DEFINITIONS.map((tool) => ({
          name: tool.name,
          description: tool.description,
          inputSchema: tool.inputSchema,
        })),
      };
    },
  );

  server.setRequestHandler(
    ListPromptsRequestSchema,
    async (): Promise<ListPromptsResult> => ({
      prompts: PUBLIC_ALPHA_PROMPT_DEFINITIONS.map((prompt) => ({
        name: prompt.name,
        title: prompt.title,
        description: prompt.description,
        arguments: prompt.arguments,
      })),
    }),
  );

  server.setRequestHandler(
    GetPromptRequestSchema,
    async (request): Promise<GetPromptResult> => {
      const definition = getPromptDefinition(
        request.params.name,
        'public-alpha',
      );

      if (!definition) {
        throw new McpError(
          ErrorCode.InvalidRequest,
          `Unknown prompt: ${request.params.name}`,
        );
      }

      return definition.render(request.params.arguments);
    },
  );

  server.setRequestHandler(
    CallToolRequestSchema,
    async (request): Promise<CallToolResult> => {
      const definition = getToolDefinition(request.params.name, 'public-alpha');

      if (!definition) {
        throw new McpError(
          ErrorCode.MethodNotFound,
          `Unknown tool: ${request.params.name}`,
        );
      }

      try {
        const payload =
          definition.kind === 'command'
            ? await projectService.execute(
                definition.parse(request.params.arguments),
              )
            : resolveQueryPayload(definition.name, projectService);

        return {
          content: [
            {
              type: 'text',
              text: JSON.stringify(payload, null, 2),
            },
          ],
          structuredContent: payload as Record<string, unknown>,
        };
      } catch (error) {
        const envelope = formatToolErrorEnvelope(
          error instanceof Error ? error : internalError(String(error)),
        );

        return {
          content: [
            {
              type: 'text',
              text: JSON.stringify(envelope, null, 2),
            },
          ],
          structuredContent: envelope as Record<string, unknown>,
          isError: true,
        };
      }
    },
  );

  server.setRequestHandler(
    ListResourcesRequestSchema,
    async (): Promise<ListResourcesResult> => ({
      resources: PUBLIC_ALPHA_RESOURCE_DEFINITIONS.map((resource) => ({
        uri: resource.uri,
        name: resource.name,
        title: resource.title,
        description: resource.description,
        mimeType: resource.mimeType,
      })),
    }),
  );

  server.setRequestHandler(
    ReadResourceRequestSchema,
    async (request): Promise<ReadResourceResult> => {
      try {
        const resource = getResourceDefinition(
          request.params.uri,
          'public-alpha',
        );

        if (!resource) {
          throw new McpError(
            ErrorCode.InvalidRequest,
            `Unknown resource: ${request.params.uri}`,
          );
        }

        const text = projectService.readResource(request.params.uri);

        return {
          contents: [
            {
              uri: resource.uri,
              mimeType: resource.mimeType,
              text,
            },
          ],
        };
      } catch (error) {
        if (isSolidWorksMcpError(error) && error.code === 'not_found') {
          throw new McpError(
            ErrorCode.InvalidRequest,
            error.message,
            error.details,
          );
        }

        throw new McpError(
          ErrorCode.InternalError,
          error instanceof Error ? error.message : 'Unexpected internal error',
        );
      }
    },
  );

  return {
    server,
    projectService,
    dispose: runtime.dispose,
  };
}

function resolveQueryPayload(
  toolName:
    | 'get_document_state'
    | 'list_available_tools'
    | 'get_project_status',
  projectService: ProjectService,
): Record<string, unknown> {
  switch (toolName) {
    case 'get_document_state':
      return {
        documentState: projectService.getDocumentState(),
      };
    case 'list_available_tools':
      return {
        tools: projectService.listAvailableTools(),
      };
    case 'get_project_status':
      return projectService.buildProjectStatus();
  }
}
