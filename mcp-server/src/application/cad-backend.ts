import type { CadCommand } from '../domain/commands.js';
import type { ProjectRuntimeState } from '../domain/state.js';

export interface CadBackend {
  readonly backendName: string;
  readonly mode: ProjectRuntimeState['backendMode'];
  execute(
    command: CadCommand,
    state: ProjectRuntimeState,
  ): Promise<ProjectRuntimeState>;
}
