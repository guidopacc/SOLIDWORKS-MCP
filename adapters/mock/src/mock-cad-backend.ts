import type { CadBackend } from '../../../mcp-server/src/application/cad-backend.js';
import type { CadCommand } from '../../../mcp-server/src/domain/commands.js';
import {
  notFound,
  preconditionFailed,
  unsupportedOperation,
} from '../../../mcp-server/src/domain/errors.js';
import {
  cloneProjectState,
  getActiveSketch,
  type PartDocumentState,
  type ProjectRuntimeState,
  type SketchState,
} from '../../../mcp-server/src/domain/state.js';

export class MockCadBackend implements CadBackend {
  readonly backendName = 'mock-backend';
  readonly mode = 'mock' as const;

  async execute(
    command: CadCommand,
    state: ProjectRuntimeState,
  ): Promise<ProjectRuntimeState> {
    const nextState = cloneProjectState(state);

    switch (command.kind) {
      case 'new_part': {
        nextState.currentDocument = {
          documentType: 'part',
          name: command.name ?? 'Part1',
          units: 'mm',
          sketches: [],
          features: [],
          exports: [],
          modified: false,
          baselineVersion: '2022',
        };
        return nextState;
      }

      case 'select_plane': {
        const document = requireExistingDocument(nextState);
        document.selectedPlane = command.plane;
        document.modified = true;
        return nextState;
      }

      case 'start_sketch': {
        const document = requireExistingDocument(nextState);

        if (!document.selectedPlane) {
          throw preconditionFailed(
            'A plane must be selected before starting a sketch.',
            {
              command: command.kind,
            },
          );
        }

        if (document.activeSketchId) {
          throw preconditionFailed('An active sketch is already open.', {
            activeSketchId: document.activeSketchId,
          });
        }

        const sketch: SketchState = {
          id: createId('sketch', document.sketches.length + 1),
          plane: document.selectedPlane,
          isOpen: true,
          entities: [],
          dimensions: [],
        };

        document.sketches.push(sketch);
        document.activeSketchId = sketch.id;
        document.modified = true;
        return nextState;
      }

      case 'draw_line': {
        const sketch = requireActiveSketch(nextState);
        sketch.entities.push({
          id: createId('line', sketch.entities.length + 1),
          kind: 'line',
          start: command.start,
          end: command.end,
          construction: command.construction ?? false,
        });
        markModified(nextState);
        return nextState;
      }

      case 'draw_circle': {
        const sketch = requireActiveSketch(nextState);
        sketch.entities.push({
          id: createId('circle', sketch.entities.length + 1),
          kind: 'circle',
          center: command.center,
          radius: command.radius,
          construction: command.construction ?? false,
        });
        markModified(nextState);
        return nextState;
      }

      case 'draw_centered_rectangle': {
        const sketch = requireActiveSketch(nextState);
        sketch.entities.push({
          id: createId('rectangle', sketch.entities.length + 1),
          kind: 'centered_rectangle',
          center: command.center,
          corner: command.corner,
          construction: command.construction ?? false,
        });
        markModified(nextState);
        return nextState;
      }

      case 'add_dimension': {
        const sketch = requireActiveSketch(nextState);
        const entity = sketch.entities.find(
          (candidate) => candidate.id === command.entityId,
        );

        if (!entity) {
          throw notFound('Sketch entity not found for dimensioning.', {
            entityId: command.entityId,
          });
        }

        sketch.dimensions.push({
          id: createId('dimension', sketch.dimensions.length + 1),
          entityId: entity.id,
          value: command.value,
          orientation: command.orientation,
        });
        markModified(nextState);
        return nextState;
      }

      case 'close_sketch': {
        const document = requireExistingDocument(nextState);
        const sketch = requireActiveSketch(nextState);
        sketch.isOpen = false;
        document.activeSketchId = undefined;
        document.modified = true;
        return nextState;
      }

      case 'extrude_boss': {
        const document = requireExistingDocument(nextState);
        ensureNoOpenSketch(document, command.kind);
        const sketch = findClosedSketch(document, command.sketchId);

        document.features.push({
          id: createId('feature', document.features.length + 1),
          kind: 'extrude_boss',
          sketchId: sketch.id,
          depth: command.depth,
          mergeResult: command.mergeResult ?? true,
        });
        document.modified = true;
        return nextState;
      }

      case 'cut_extrude': {
        const document = requireExistingDocument(nextState);
        ensureNoOpenSketch(document, command.kind);
        const sketch = findClosedSketch(document, command.sketchId);

        document.features.push({
          id: createId('feature', document.features.length + 1),
          kind: 'cut_extrude',
          sketchId: sketch.id,
          depth: command.depth,
          mergeResult: command.mergeResult ?? true,
        });
        document.modified = true;
        return nextState;
      }

      case 'add_fillet': {
        const document = requireExistingDocument(nextState);
        const feature = document.features.find(
          (candidate) => candidate.id === command.featureId,
        );

        if (!feature) {
          throw notFound('Target feature not found for fillet.', {
            featureId: command.featureId,
          });
        }

        document.features.push({
          id: createId('feature', document.features.length + 1),
          kind: 'fillet',
          featureId: feature.id,
          radius: command.radius,
        });
        document.modified = true;
        return nextState;
      }

      case 'save_part': {
        const document = requireExistingDocument(nextState);
        document.savedPath = command.path;
        document.modified = false;
        return nextState;
      }

      case 'export_step': {
        const document = requireExistingDocument(nextState);
        document.exports.push({
          kind: 'step',
          path: command.path,
        });
        return nextState;
      }

      default:
        throw unsupportedOperation('Unsupported mock command.', {
          command: command satisfies never,
        });
    }
  }
}

function requireExistingDocument(
  state: ProjectRuntimeState,
): PartDocumentState {
  if (!state.currentDocument) {
    throw preconditionFailed('No active document exists.', {
      backendMode: state.backendMode,
    });
  }

  return state.currentDocument;
}

function requireActiveSketch(state: ProjectRuntimeState): SketchState {
  const document = requireExistingDocument(state);
  const sketch = getActiveSketch(document);

  if (!sketch) {
    throw preconditionFailed('No active sketch is open.', {
      documentName: document.name,
    });
  }

  return sketch;
}

function findClosedSketch(
  document: PartDocumentState,
  sketchId: string,
): SketchState {
  const sketch = document.sketches.find(
    (candidate) => candidate.id === sketchId,
  );

  if (!sketch) {
    throw notFound('Sketch not found.', { sketchId });
  }

  if (sketch.isOpen) {
    throw preconditionFailed(
      'The target sketch must be closed before feature creation.',
      {
        sketchId,
      },
    );
  }

  return sketch;
}

function ensureNoOpenSketch(
  document: PartDocumentState,
  command: string,
): void {
  if (document.activeSketchId) {
    throw preconditionFailed(
      'The active sketch must be closed before creating a solid feature.',
      {
        command,
        activeSketchId: document.activeSketchId,
      },
    );
  }
}

function markModified(state: ProjectRuntimeState): void {
  const document = requireExistingDocument(state);
  document.modified = true;
}

function createId(prefix: string, ordinal: number): string {
  return `${prefix}-${ordinal}`;
}
