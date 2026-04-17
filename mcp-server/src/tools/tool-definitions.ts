import type {
  AddDimensionCommand,
  AddFilletCommand,
  CadCommand,
  CutExtrudeCommand,
  DrawCenteredRectangleCommand,
  DrawCircleCommand,
  DrawLineCommand,
  ExportStepCommand,
  ExtrudeBossCommand,
  NewPartCommand,
  SavePartCommand,
  SelectPlaneCommand,
} from '../domain/commands.js';
import { SUPPORTED_PLANES } from '../domain/commands.js';
import { invalidInput } from '../domain/errors.js';

export type QueryToolKind =
  | 'get_document_state'
  | 'list_available_tools'
  | 'get_project_status';

type JsonSchema = {
  type: 'object';
  properties?: Record<string, object>;
  required?: string[];
  additionalProperties?: boolean;
} & Record<string, unknown>;

export interface CommandToolDefinition {
  name: string;
  description: string;
  inputSchema: JsonSchema;
  kind: 'command';
  parse: (args: Record<string, unknown> | undefined) => CadCommand;
}

export interface QueryToolDefinition {
  name: QueryToolKind;
  description: string;
  inputSchema: JsonSchema;
  kind: 'query';
}

export type ToolDefinition = CommandToolDefinition | QueryToolDefinition;

export const TOOL_DEFINITIONS: ToolDefinition[] = [
  {
    name: 'new_part',
    description: 'Create a new part document in the active CAD backend.',
    kind: 'command',
    inputSchema: {
      type: 'object',
      properties: {
        name: { type: 'string' },
      },
      additionalProperties: false,
    },
    parse: (args): NewPartCommand => ({
      kind: 'new_part',
      name: optionalString(args, 'name'),
    }),
  },
  {
    name: 'select_plane',
    description: 'Select one of the supported default reference planes.',
    kind: 'command',
    inputSchema: {
      type: 'object',
      required: ['plane'],
      properties: {
        plane: { type: 'string', enum: [...SUPPORTED_PLANES] },
      },
      additionalProperties: false,
    },
    parse: (args): SelectPlaneCommand => {
      const plane = requiredString(args, 'plane');
      if (!SUPPORTED_PLANES.includes(plane as SelectPlaneCommand['plane'])) {
        throw invalidInput('Unsupported plane name.', {
          plane,
          supportedPlanes: SUPPORTED_PLANES,
        });
      }
      return {
        kind: 'select_plane',
        plane: plane as SelectPlaneCommand['plane'],
      };
    },
  },
  {
    name: 'start_sketch',
    description: 'Start a sketch on the selected plane.',
    kind: 'command',
    inputSchema: {
      type: 'object',
      additionalProperties: false,
    },
    parse: (): CadCommand => ({ kind: 'start_sketch' }),
  },
  {
    name: 'draw_line',
    description: 'Create a line segment inside the active sketch.',
    kind: 'command',
    inputSchema: pointPairSchema('start', 'end'),
    parse: (args): DrawLineCommand => ({
      kind: 'draw_line',
      start: requiredPoint(args, 'start'),
      end: requiredPoint(args, 'end'),
      construction: optionalBoolean(args, 'construction') ?? false,
    }),
  },
  {
    name: 'draw_circle',
    description: 'Create a circle inside the active sketch.',
    kind: 'command',
    inputSchema: {
      type: 'object',
      required: ['center', 'radius'],
      properties: {
        center: pointSchema(),
        radius: { type: 'number', exclusiveMinimum: 0 },
        construction: { type: 'boolean' },
      },
      additionalProperties: false,
    },
    parse: (args): DrawCircleCommand => ({
      kind: 'draw_circle',
      center: requiredPoint(args, 'center'),
      radius: positiveNumber(args, 'radius'),
      construction: optionalBoolean(args, 'construction') ?? false,
    }),
  },
  {
    name: 'draw_centered_rectangle',
    description: 'Create a centered rectangle inside the active sketch.',
    kind: 'command',
    inputSchema: pointPairSchema('center', 'corner'),
    parse: (args): DrawCenteredRectangleCommand => ({
      kind: 'draw_centered_rectangle',
      center: requiredPoint(args, 'center'),
      corner: requiredPoint(args, 'corner'),
      construction: optionalBoolean(args, 'construction') ?? false,
    }),
  },
  {
    name: 'add_dimension',
    description: 'Add a dimension to an existing sketch entity.',
    kind: 'command',
    inputSchema: {
      type: 'object',
      required: ['entityId', 'value'],
      properties: {
        entityId: { type: 'string' },
        value: { type: 'number', exclusiveMinimum: 0 },
        orientation: {
          type: 'string',
          enum: ['horizontal', 'vertical', 'radial', 'diameter'],
        },
      },
      additionalProperties: false,
    },
    parse: (args): AddDimensionCommand => ({
      kind: 'add_dimension',
      entityId: requiredString(args, 'entityId'),
      value: positiveNumber(args, 'value'),
      orientation: optionalString(args, 'orientation') as
        | AddDimensionCommand['orientation']
        | undefined,
    }),
  },
  {
    name: 'close_sketch',
    description: 'Close the active sketch.',
    kind: 'command',
    inputSchema: {
      type: 'object',
      additionalProperties: false,
    },
    parse: (): CadCommand => ({ kind: 'close_sketch' }),
  },
  {
    name: 'extrude_boss',
    description: 'Create a boss extrusion from a closed sketch.',
    kind: 'command',
    inputSchema: featureFromSketchSchema(),
    parse: (args): ExtrudeBossCommand => ({
      kind: 'extrude_boss',
      sketchId: requiredString(args, 'sketchId'),
      depth: positiveNumber(args, 'depth'),
      mergeResult: optionalBoolean(args, 'mergeResult') ?? true,
    }),
  },
  {
    name: 'cut_extrude',
    description: 'Create a cut extrusion from a closed sketch.',
    kind: 'command',
    inputSchema: featureFromSketchSchema(),
    parse: (args): CutExtrudeCommand => ({
      kind: 'cut_extrude',
      sketchId: requiredString(args, 'sketchId'),
      depth: positiveNumber(args, 'depth'),
      mergeResult: optionalBoolean(args, 'mergeResult') ?? true,
    }),
  },
  {
    name: 'add_fillet',
    description: 'Add a fillet to an existing solid feature.',
    kind: 'command',
    inputSchema: {
      type: 'object',
      required: ['featureId', 'radius'],
      properties: {
        featureId: { type: 'string' },
        radius: { type: 'number', exclusiveMinimum: 0 },
      },
      additionalProperties: false,
    },
    parse: (args): AddFilletCommand => ({
      kind: 'add_fillet',
      featureId: requiredString(args, 'featureId'),
      radius: positiveNumber(args, 'radius'),
    }),
  },
  {
    name: 'save_part',
    description: 'Save the active part document to a target path.',
    kind: 'command',
    inputSchema: pathOnlySchema(),
    parse: (args): SavePartCommand => ({
      kind: 'save_part',
      path: requiredString(args, 'path'),
    }),
  },
  {
    name: 'export_step',
    description: 'Export the active part document to STEP.',
    kind: 'command',
    inputSchema: pathOnlySchema(),
    parse: (args): ExportStepCommand => ({
      kind: 'export_step',
      path: requiredString(args, 'path'),
    }),
  },
  {
    name: 'get_document_state',
    description: 'Return the normalized current document state.',
    kind: 'query',
    inputSchema: {
      type: 'object',
      additionalProperties: false,
    },
  },
  {
    name: 'list_available_tools',
    description: 'Return the currently registered MCP tools.',
    kind: 'query',
    inputSchema: {
      type: 'object',
      additionalProperties: false,
    },
  },
  {
    name: 'get_project_status',
    description: 'Return the project planning and implementation status.',
    kind: 'query',
    inputSchema: {
      type: 'object',
      additionalProperties: false,
    },
  },
];

const PUBLIC_ALPHA_TOOL_NAME_SET = new Set([
  'new_part',
  'select_plane',
  'start_sketch',
  'draw_line',
  'draw_circle',
  'draw_centered_rectangle',
  'close_sketch',
  'extrude_boss',
  'save_part',
  'export_step',
  'get_document_state',
  'list_available_tools',
  'get_project_status',
]);

export const PUBLIC_ALPHA_TOOL_DEFINITIONS: ToolDefinition[] =
  TOOL_DEFINITIONS.filter((definition) =>
    PUBLIC_ALPHA_TOOL_NAME_SET.has(definition.name),
  );

export function listPlannedButNotPublicToolNames(): string[] {
  return TOOL_DEFINITIONS.filter(
    (definition) => !PUBLIC_ALPHA_TOOL_NAME_SET.has(definition.name),
  ).map((definition) => definition.name);
}

export function getToolDefinition(
  name: string,
  surface: 'all' | 'public-alpha' = 'all',
): ToolDefinition | undefined {
  const definitions =
    surface === 'public-alpha'
      ? PUBLIC_ALPHA_TOOL_DEFINITIONS
      : TOOL_DEFINITIONS;

  return definitions.find((definition) => definition.name === name);
}

function pointSchema(): JsonSchema {
  return {
    type: 'object',
    required: ['x', 'y'],
    properties: {
      x: { type: 'number' },
      y: { type: 'number' },
    },
    additionalProperties: false,
  };
}

function pointPairSchema(first: string, second: string): JsonSchema {
  return {
    type: 'object',
    required: [first, second],
    properties: {
      [first]: pointSchema(),
      [second]: pointSchema(),
      construction: { type: 'boolean' },
    },
    additionalProperties: false,
  };
}

function featureFromSketchSchema(): JsonSchema {
  return {
    type: 'object',
    required: ['sketchId', 'depth'],
    properties: {
      sketchId: { type: 'string' },
      depth: { type: 'number', exclusiveMinimum: 0 },
      mergeResult: { type: 'boolean' },
    },
    additionalProperties: false,
  };
}

function pathOnlySchema(): JsonSchema {
  return {
    type: 'object',
    required: ['path'],
    properties: {
      path: { type: 'string' },
    },
    additionalProperties: false,
  };
}

function requiredObject(
  args: Record<string, unknown> | undefined,
  key: string,
): Record<string, unknown> {
  const value = args?.[key];
  if (!value || typeof value !== 'object' || Array.isArray(value)) {
    throw invalidInput(`Field "${key}" must be an object.`, { key });
  }
  return value as Record<string, unknown>;
}

function requiredPoint(
  args: Record<string, unknown> | undefined,
  key: string,
): { x: number; y: number } {
  const value = requiredObject(args, key);
  const x = value.x;
  const y = value.y;

  if (typeof x !== 'number' || typeof y !== 'number') {
    throw invalidInput(`Field "${key}" must contain numeric x and y values.`, {
      key,
    });
  }

  return { x, y };
}

function requiredString(
  args: Record<string, unknown> | undefined,
  key: string,
): string {
  const value = args?.[key];
  if (typeof value !== 'string' || value.trim().length === 0) {
    throw invalidInput(`Field "${key}" must be a non-empty string.`, { key });
  }
  return value;
}

function optionalString(
  args: Record<string, unknown> | undefined,
  key: string,
): string | undefined {
  const value = args?.[key];
  if (value === undefined) {
    return undefined;
  }
  if (typeof value !== 'string' || value.trim().length === 0) {
    throw invalidInput(
      `Field "${key}" must be a non-empty string when provided.`,
      {
        key,
      },
    );
  }
  return value;
}

function positiveNumber(
  args: Record<string, unknown> | undefined,
  key: string,
): number {
  const value = args?.[key];
  if (typeof value !== 'number' || Number.isNaN(value) || value <= 0) {
    throw invalidInput(`Field "${key}" must be a positive number.`, { key });
  }
  return value;
}

function optionalBoolean(
  args: Record<string, unknown> | undefined,
  key: string,
): boolean | undefined {
  const value = args?.[key];
  if (value === undefined) {
    return undefined;
  }
  if (typeof value !== 'boolean') {
    throw invalidInput(`Field "${key}" must be a boolean when provided.`, {
      key,
    });
  }
  return value;
}
