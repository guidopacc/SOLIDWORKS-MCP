import type { DimensionOrientation, PlaneName } from './commands.js';

export type BackendMode = 'mock' | 'solidworks';
export type UnitsSystem = 'mm';

export interface LineEntity {
  id: string;
  kind: 'line';
  start: { x: number; y: number };
  end: { x: number; y: number };
  construction: boolean;
}

export interface CircleEntity {
  id: string;
  kind: 'circle';
  center: { x: number; y: number };
  radius: number;
  construction: boolean;
}

export interface CenteredRectangleEntity {
  id: string;
  kind: 'centered_rectangle';
  center: { x: number; y: number };
  corner: { x: number; y: number };
  construction: boolean;
}

export type SketchEntity = LineEntity | CircleEntity | CenteredRectangleEntity;

export interface SketchDimension {
  id: string;
  entityId: string;
  value: number;
  orientation?: DimensionOrientation;
}

export interface SketchState {
  id: string;
  plane: PlaneName;
  isOpen: boolean;
  entities: SketchEntity[];
  dimensions: SketchDimension[];
}

export interface ExtrudeFeatureState {
  id: string;
  kind: 'extrude_boss';
  sketchId: string;
  depth: number;
  mergeResult: boolean;
}

export interface CutFeatureState {
  id: string;
  kind: 'cut_extrude';
  sketchId: string;
  depth: number;
  mergeResult: boolean;
}

export interface FilletFeatureState {
  id: string;
  kind: 'fillet';
  featureId: string;
  radius: number;
}

export type FeatureState =
  | ExtrudeFeatureState
  | CutFeatureState
  | FilletFeatureState;

export interface ExportArtifact {
  kind: 'step';
  path: string;
}

export interface PartDocumentState {
  documentType: 'part';
  name: string;
  units: UnitsSystem;
  selectedPlane?: PlaneName;
  activeSketchId?: string;
  sketches: SketchState[];
  features: FeatureState[];
  savedPath?: string;
  exports: ExportArtifact[];
  modified: boolean;
  baselineVersion: '2022';
}

export interface ProjectRuntimeState {
  backendMode: BackendMode;
  baselineVersion: 'SolidWorks 2022';
  availableTools: string[];
  currentDocument: PartDocumentState | null;
}

export function createInitialProjectState(
  availableTools: string[],
  backendMode: BackendMode,
): ProjectRuntimeState {
  return {
    backendMode,
    baselineVersion: 'SolidWorks 2022',
    availableTools: [...availableTools],
    currentDocument: null,
  };
}

export function cloneProjectState(
  state: ProjectRuntimeState,
): ProjectRuntimeState {
  return structuredClone(state);
}

export function getActiveSketch(
  document: PartDocumentState,
): SketchState | undefined {
  if (!document.activeSketchId) {
    return undefined;
  }

  return document.sketches.find(
    (sketch) => sketch.id === document.activeSketchId,
  );
}
