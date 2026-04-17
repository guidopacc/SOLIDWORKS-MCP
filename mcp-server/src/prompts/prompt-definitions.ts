export interface PromptArgumentDefinition {
  name: string;
  description: string;
  required?: boolean;
}

export interface PromptMessage {
  role: 'user' | 'assistant';
  content: {
    type: 'text';
    text: string;
  };
}

export interface PromptRenderResult {
  [key: string]: unknown;
  description?: string;
  messages: PromptMessage[];
}

export interface PromptDefinition {
  name: string;
  title: string;
  description: string;
  arguments?: PromptArgumentDefinition[];
  render: (args?: Record<string, string>) => PromptRenderResult;
}

const SAFE_MODELING_SESSION_PROMPT: PromptDefinition = {
  name: 'safe_modeling_session',
  title: 'Safe Modeling Session',
  description:
    'Guide a client through the current public alpha modeling flow without implying unsupported CAD capability.',
  arguments: [
    {
      name: 'user_request',
      description:
        'Optional short description of the part or modeling outcome the user wants within the current alpha boundary.',
    },
  ],
  render: (args): PromptRenderResult => {
    const userRequest = sanitizePromptArgument(args?.user_request);

    const requestSection = userRequest
      ? `Requested modeling goal: ${userRequest}\n\n`
      : '';

    return {
      description:
        'Use the current SOLIDWORKS-MCP public alpha safely and progressively, and stop clearly when the request exceeds the validated boundary.',
      messages: [
        {
          role: 'user',
          content: {
            type: 'text',
            text: `${requestSection}You are assisting with SOLIDWORKS-MCP's current public alpha. Stay inside the validated public alpha surfaces only.

Supported MCP tool surface:
- new_part
- select_plane
- start_sketch
- draw_line
- draw_circle
- draw_centered_rectangle
- close_sketch
- extrude_boss
- save_part
- export_step
- get_document_state
- list_available_tools
- get_project_status

Use the session in this safe order unless the user stops earlier:
1. new_part
2. select_plane on Front Plane, Top Plane, or Right Plane
3. start_sketch
4. draw supported primitive geometry
5. close_sketch
6. get_document_state if the next step depends on the current sketch or document facts
7. extrude_boss only after a closed sketch and only with a known sketchId
8. save_part when the user wants a file on disk
9. export_step only if the user asks for STEP output

Behavior rules:
- Keep the workflow progressive and explicit.
- If the host already provides the public_alpha_boundary resource, use it to stay aligned to the exposed public alpha surfaces before planning actions.
- If the host already provides the current_document_state resource, use it as initial read-only context.
- Use get_document_state instead of guessing sketchId, featureId, or document facts, especially after tool calls that may have changed state.
- Use list_available_tools or get_project_status when you need to reconfirm the exposed alpha boundary.
- Ask for missing save/export paths only when save_part or export_step is actually needed.
- If the request can be narrowed to the supported workflow, explain the supported subset and continue carefully.

Hard limits:
- Do not claim support for add_dimension, cut_extrude, add_fillet, assemblies, drawings, open/reopen-document editing, or SolidWorks versions beyond 2022.
- Do not promise broad natural-language CAD authoring.
- Do not improvise unsupported operations through unrelated tools.
- If the request requires unsupported behavior, say so plainly and stop or offer the nearest supported subset.`,
          },
        },
      ],
    };
  },
};

export const PROMPT_DEFINITIONS: PromptDefinition[] = [
  SAFE_MODELING_SESSION_PROMPT,
];

export const PUBLIC_ALPHA_PROMPT_DEFINITIONS: PromptDefinition[] = [
  SAFE_MODELING_SESSION_PROMPT,
];

export function getPromptDefinition(
  name: string,
  surface: 'all' | 'public-alpha' = 'all',
): PromptDefinition | undefined {
  const definitions =
    surface === 'public-alpha'
      ? PUBLIC_ALPHA_PROMPT_DEFINITIONS
      : PROMPT_DEFINITIONS;

  return definitions.find((definition) => definition.name === name);
}

function sanitizePromptArgument(value: string | undefined): string | undefined {
  if (!value) {
    return undefined;
  }

  const trimmed = value.trim();
  return trimmed.length > 0 ? trimmed : undefined;
}
