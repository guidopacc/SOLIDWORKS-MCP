import type { CadCommand } from '../domain/commands.js';
import { notFound } from '../domain/errors.js';
import {
  cloneProjectState,
  createInitialProjectState,
  type PartDocumentState,
  type ProjectRuntimeState,
} from '../domain/state.js';
import { PUBLIC_ALPHA_PROMPT_DEFINITIONS } from '../prompts/prompt-definitions.js';
import {
  CURRENT_DOCUMENT_STATE_RESOURCE_URI,
  PUBLIC_ALPHA_BOUNDARY_RESOURCE_URI,
  PUBLIC_ALPHA_RESOURCE_DEFINITIONS,
} from '../resources/resource-definitions.js';
import {
  listPlannedButNotPublicToolNames,
  PUBLIC_ALPHA_TOOL_DEFINITIONS,
} from '../tools/tool-definitions.js';
import type { CadBackend } from './cad-backend.js';

export interface CommandExecutionResult {
  command: CadCommand['kind'];
  summary: string;
  backendMode: ProjectRuntimeState['backendMode'];
  documentState: PartDocumentState | null;
}

export interface CurrentDocumentStateResourcePayload {
  resourceType: 'current_document_state';
  readOnly: true;
  queryEquivalent: 'get_document_state';
  promptCompanion: 'safe_modeling_session';
  documentPresent: boolean;
  documentState: PartDocumentState | null;
  documentSummary: {
    documentType: PartDocumentState['documentType'];
    name: string;
    savedPath: string | null;
    isSaved: boolean;
    modified: boolean;
    units: PartDocumentState['units'];
    selectedPlane: PartDocumentState['selectedPlane'] | null;
    activeSketchId: string | null;
    sketchCount: number;
    openSketchCount: number;
    featureCount: number;
    exportCount: number;
    lastExportPath: string | null;
  } | null;
}

export interface PublicAlphaBoundaryResourcePayload {
  resourceType: 'public_alpha_boundary';
  readOnly: true;
  scope: 'public_alpha';
  validatedOn: {
    operatingSystem: 'Windows';
    solidworksBaseline: '2022';
  };
  supportedSurfaces: {
    tools: string[];
    prompts: string[];
    resources: string[];
  };
  workflowShape: {
    coreSequence: string[];
    allowedSketchPrimitives: string[];
    supportedPlanes: string[];
    stateRefreshTools: string[];
  };
  companionReferences: {
    prompt: 'safe_modeling_session';
    dynamicResource: 'current_document_state';
    dynamicResourceUri: typeof CURRENT_DOCUMENT_STATE_RESOURCE_URI;
    queryTools: Array<
      'get_document_state' | 'list_available_tools' | 'get_project_status'
    >;
  };
  explicitOutOfScope: string[];
  knownLimits: string[];
}

export class ProjectService {
  private state: ProjectRuntimeState;

  constructor(
    private readonly backend: CadBackend,
    initialState?: ProjectRuntimeState,
  ) {
    this.state =
      initialState ?? createInitialProjectState([], this.backend.mode);
  }

  bootstrapAvailableTools(toolNames: string[]): void {
    this.state.availableTools = [...toolNames];
  }

  getState(): ProjectRuntimeState {
    return cloneProjectState(this.state);
  }

  getDocumentState(): PartDocumentState | null {
    return this.state.currentDocument
      ? structuredClone(this.state.currentDocument)
      : null;
  }

  listAvailableTools(): string[] {
    return [...this.state.availableTools];
  }

  async execute(command: CadCommand): Promise<CommandExecutionResult> {
    const candidateState = cloneProjectState(this.state);
    const nextState = await this.backend.execute(command, candidateState);

    nextState.availableTools = [...this.state.availableTools];
    nextState.backendMode = this.backend.mode;
    nextState.baselineVersion = this.state.baselineVersion;

    this.state = nextState;

    return {
      command: command.kind,
      summary: summarizeCommand(command),
      backendMode: this.state.backendMode,
      documentState: this.getDocumentState(),
    };
  }

  readResource(uri: string): string {
    switch (uri) {
      case 'solidworks://project/status':
        return JSON.stringify(this.buildProjectStatus(), null, 2);
      case CURRENT_DOCUMENT_STATE_RESOURCE_URI:
      case 'solidworks://document/current':
        return JSON.stringify(
          this.buildCurrentDocumentStateResource(),
          null,
          2,
        );
      case PUBLIC_ALPHA_BOUNDARY_RESOURCE_URI:
        return JSON.stringify(this.buildPublicAlphaBoundaryResource(), null, 2);
      case 'solidworks://tools/catalog':
        return JSON.stringify(
          {
            tools: this.listAvailableTools(),
          },
          null,
          2,
        );
      default:
        throw notFound(`Resource not found: ${uri}`, { uri });
    }
  }

  buildCurrentDocumentStateResource(): CurrentDocumentStateResourcePayload {
    const documentState = this.getDocumentState();

    return {
      resourceType: 'current_document_state',
      readOnly: true,
      queryEquivalent: 'get_document_state',
      promptCompanion: 'safe_modeling_session',
      documentPresent: documentState !== null,
      documentState,
      documentSummary: documentState
        ? {
            documentType: documentState.documentType,
            name: documentState.name,
            savedPath: documentState.savedPath ?? null,
            isSaved: documentState.savedPath !== undefined,
            modified: documentState.modified,
            units: documentState.units,
            selectedPlane: documentState.selectedPlane ?? null,
            activeSketchId: documentState.activeSketchId ?? null,
            sketchCount: documentState.sketches.length,
            openSketchCount: documentState.sketches.filter(
              (sketch) => sketch.isOpen,
            ).length,
            featureCount: documentState.features.length,
            exportCount: documentState.exports.length,
            lastExportPath:
              documentState.exports[documentState.exports.length - 1]?.path ??
              null,
          }
        : null,
    };
  }

  buildPublicAlphaBoundaryResource(): PublicAlphaBoundaryResourcePayload {
    return {
      resourceType: 'public_alpha_boundary',
      readOnly: true,
      scope: 'public_alpha',
      validatedOn: {
        operatingSystem: 'Windows',
        solidworksBaseline: '2022',
      },
      supportedSurfaces: {
        tools: PUBLIC_ALPHA_TOOL_DEFINITIONS.map(
          (definition) => definition.name,
        ),
        prompts: PUBLIC_ALPHA_PROMPT_DEFINITIONS.map(
          (definition) => definition.name,
        ),
        resources: PUBLIC_ALPHA_RESOURCE_DEFINITIONS.map(
          (definition) => definition.name,
        ),
      },
      workflowShape: {
        coreSequence: [
          'new_part',
          'select_plane',
          'start_sketch',
          'close_sketch',
          'extrude_boss',
          'save_part',
          'export_step',
        ],
        allowedSketchPrimitives: [
          'draw_line',
          'draw_circle',
          'draw_centered_rectangle',
        ],
        supportedPlanes: ['Front Plane', 'Top Plane', 'Right Plane'],
        stateRefreshTools: [
          'get_document_state',
          'list_available_tools',
          'get_project_status',
        ],
      },
      companionReferences: {
        prompt: 'safe_modeling_session',
        dynamicResource: 'current_document_state',
        dynamicResourceUri: CURRENT_DOCUMENT_STATE_RESOURCE_URI,
        queryTools: [
          'get_document_state',
          'list_available_tools',
          'get_project_status',
        ],
      },
      explicitOutOfScope: [
        'add_dimension',
        'cut_extrude',
        'add_fillet',
        'assemblies',
        'drawings',
        'open_or_reopen_documents',
        'solidworks_versions_beyond_2022',
      ],
      knownLimits: [
        'windows_only',
        'repo_first_alpha_not_installer_grade',
        'part_workflow_only',
        'current_document_state_is_session_only',
        'no_historical_document_browsing',
      ],
    };
  }

  buildProjectStatus(): Record<string, unknown> {
    const publicAlphaSurface = PUBLIC_ALPHA_TOOL_DEFINITIONS.map(
      (definition) => definition.name,
    );
    const publicPromptSurface = PUBLIC_ALPHA_PROMPT_DEFINITIONS.map(
      (definition) => definition.name,
    );
    const publicResourceSurface = PUBLIC_ALPHA_RESOURCE_DEFINITIONS.map(
      (definition) => definition.name,
    );
    const plannedButNotPublic = listPlannedButNotPublicToolNames();

    return {
      finalGoal:
        'A robust MCP server that can progressively drive SolidWorks from natural-language-guided tool use, starting from narrow, validation-backed native CAD primitives on Windows.',
      currentMilestone:
        'Developer/public alpha readiness now includes a first prompt-backed guidance layer plus a two-resource public alpha context layer on top of the current Level 1 real-modeling slice validated on SolidWorks 2022.',
      currentFocus:
        'Keep the exposed MCP tool, prompt, and resource surfaces aligned to the proven SolidWorks 2022 slice, use the current prompt-plus-resource layer to improve natural-language readiness honestly, and avoid widening the CAD scope prematurely.',
      nextAction:
        'Decide whether the current prompt-plus-two-resource public alpha layer is complete enough to shift back to a new bounded CAD slice, while keeping the current proven CAD baseline stable and honest.',
      backendMode: this.state.backendMode,
      baselineVersion: this.state.baselineVersion,
      alphaReadiness: {
        status: 'developer_alpha_with_first_prompt',
        publicAlphaSurface,
        notYetSupported: plannedButNotPublic,
        safeNaturalLanguageWorkflows: [
          'create a new part',
          'select Front/Top/Right Plane',
          'start and close a sketch',
          'draw a line, circle, or centered rectangle',
          'extrude a closed sketch with extrude_boss',
          'save the active part',
          'export STEP from the active part',
          'inspect document state or project status',
        ],
      },
      promptReadiness: {
        status: 'first_prompt_supported',
        publicPromptSurface,
        safePromptUseCases: [
          'guide a safe modeling session inside the current public alpha boundary',
          'keep prompt-assisted requests aligned to the proven progressive modeling flow',
          'stop clearly when a request exceeds the current public alpha scope',
        ],
        notYetSupported: [
          'multiple public prompts for advanced modeling intents',
          'resource-backed prompt bundles',
          'prompt guidance for unsupported CAD operations',
        ],
      },
      resourceReadiness: {
        status: 'second_resource_supported',
        publicResourceSurface,
        safeResourceUseCases: [
          'preload read-only context about the current modeling session before tool calls',
          'preload a static machine-friendly view of the current public alpha boundary before planning actions',
          'inspect the current normalized document state without implying new CAD capability',
          'pair host-side context loading with safe_modeling_session, current_document_state, and get_document_state',
        ],
        notYetSupported: [
          'historical document browsing',
          'open or reopen document resources',
          'large public resource packs beyond the current two-resource alpha layer',
        ],
      },
      existingComponents: [
        'TypeScript MCP server skeleton',
        'Typed CAD command contract',
        'Deterministic mock backend',
        'Versioned worker protocol for the future Windows adapter',
        'SolidWorks worker-backed backend wrapper',
        'Stdio worker launcher/transport',
        'Runtime backend resolver',
        'Validation-backed real Windows/.NET worker slice with structured errors',
        'Current public alpha MCP surface aligned to the proven real worker slice',
        'First public alpha MCP prompt for safe modeling sessions',
        'First public alpha MCP resource for current document state context',
        'Second public alpha MCP resource for static public boundary context',
        'Project state service',
        'Windows execution, evidence, retention, and handoff scripts',
        'Developer-alpha documentation and client configuration examples',
      ],
      missingComponents: [
        'Cross-machine external alpha validation beyond the company Windows baseline',
        'Installer- or desktop-extension-grade packaging',
        'A broader prompt/resource usability layer beyond the first guided modeling-session prompt and the current two-resource public alpha layer',
        'Assembly and drawing workflows',
        'Recovery and synchronization logic for COM session failures',
      ],
      testingStatus: {
        mockBackend: 'implemented',
        mockIntegration: 'implemented',
        realSolidWorks:
          'tested_on_real_solidworks_2022_for_current_public_alpha_surface',
        publicAlpha:
          'tool_surface_stable_with_first_prompt_and_two_resources_validated_locally',
      },
      documentationStatus: {
        planning: 'implemented',
        research: 'implemented',
        architecture: 'implemented',
        gettingStarted: 'implemented',
      },
      blockers: [
        'add_dimension remains unsafe on the baseline SolidWorks 2022 machine and is excluded from the public alpha surface.',
        'The public alpha is still validated on one company Windows/SolidWorks 2022 baseline, not yet across multiple external developer environments.',
        'The current alpha shape still depends on disciplined Windows setup rather than a packaged installer or desktop extension.',
      ],
      openDecisions: [
        'Decide whether the current prompt-plus-two-resource non-CAD layer is complete enough to justify returning to a new bounded CAD slice.',
        'Decide when the developer alpha should grow into a packaged desktop-extension or installer form.',
        'Decide whether worker health/version/session facts should stay in execution metadata or become explicit diagnostics tools.',
        'Decide whether the worker should move from late-bound COM to typed interop after broader Windows validation.',
      ],
    };
  }
}

function summarizeCommand(command: CadCommand): string {
  switch (command.kind) {
    case 'new_part':
      return `Created a new part document${command.name ? ` named ${command.name}` : ''}.`;
    case 'select_plane':
      return `Selected plane ${command.plane}.`;
    case 'start_sketch':
      return 'Started a new sketch on the selected plane.';
    case 'draw_line':
      return 'Added a line entity to the active sketch.';
    case 'draw_circle':
      return 'Added a circle entity to the active sketch.';
    case 'draw_centered_rectangle':
      return 'Added a centered rectangle to the active sketch.';
    case 'add_dimension':
      return `Added a dimension to entity ${command.entityId}.`;
    case 'close_sketch':
      return 'Closed the active sketch.';
    case 'extrude_boss':
      return `Created an extrude-boss feature from sketch ${command.sketchId}.`;
    case 'cut_extrude':
      return `Created a cut-extrude feature from sketch ${command.sketchId}.`;
    case 'add_fillet':
      return `Added a fillet to feature ${command.featureId}.`;
    case 'save_part':
      return `Saved the part to ${command.path}.`;
    case 'export_step':
      return `Exported the part to STEP at ${command.path}.`;
  }
}
