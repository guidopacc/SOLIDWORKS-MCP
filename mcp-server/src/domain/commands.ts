export const SUPPORTED_PLANES = [
  'Front Plane',
  'Top Plane',
  'Right Plane',
] as const;

export type PlaneName = (typeof SUPPORTED_PLANES)[number];

export type DimensionOrientation =
  | 'horizontal'
  | 'vertical'
  | 'radial'
  | 'diameter';

export interface Point2D {
  x: number;
  y: number;
}

export interface NewPartCommand {
  kind: 'new_part';
  name?: string;
}

export interface SelectPlaneCommand {
  kind: 'select_plane';
  plane: PlaneName;
}

export interface StartSketchCommand {
  kind: 'start_sketch';
}

export interface DrawLineCommand {
  kind: 'draw_line';
  start: Point2D;
  end: Point2D;
  construction?: boolean;
}

export interface DrawCircleCommand {
  kind: 'draw_circle';
  center: Point2D;
  radius: number;
  construction?: boolean;
}

export interface DrawCenteredRectangleCommand {
  kind: 'draw_centered_rectangle';
  center: Point2D;
  corner: Point2D;
  construction?: boolean;
}

export interface AddDimensionCommand {
  kind: 'add_dimension';
  entityId: string;
  value: number;
  orientation?: DimensionOrientation;
}

export interface CloseSketchCommand {
  kind: 'close_sketch';
}

export interface ExtrudeBossCommand {
  kind: 'extrude_boss';
  sketchId: string;
  depth: number;
  mergeResult?: boolean;
}

export interface CutExtrudeCommand {
  kind: 'cut_extrude';
  sketchId: string;
  depth: number;
  mergeResult?: boolean;
}

export interface AddFilletCommand {
  kind: 'add_fillet';
  featureId: string;
  radius: number;
}

export interface SavePartCommand {
  kind: 'save_part';
  path: string;
}

export interface ExportStepCommand {
  kind: 'export_step';
  path: string;
}

export type CadCommand =
  | NewPartCommand
  | SelectPlaneCommand
  | StartSketchCommand
  | DrawLineCommand
  | DrawCircleCommand
  | DrawCenteredRectangleCommand
  | AddDimensionCommand
  | CloseSketchCommand
  | ExtrudeBossCommand
  | CutExtrudeCommand
  | AddFilletCommand
  | SavePartCommand
  | ExportStepCommand;

export const COMMAND_NAMES = [
  'new_part',
  'select_plane',
  'start_sketch',
  'draw_line',
  'draw_circle',
  'draw_centered_rectangle',
  'add_dimension',
  'close_sketch',
  'extrude_boss',
  'cut_extrude',
  'add_fillet',
  'save_part',
  'export_step',
] as const;
