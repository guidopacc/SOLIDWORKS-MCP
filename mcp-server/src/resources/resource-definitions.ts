export interface ResourceDefinition {
  uri: string;
  aliases?: string[];
  name: string;
  title: string;
  description: string;
  mimeType: 'application/json';
}

export const CURRENT_DOCUMENT_STATE_RESOURCE_URI =
  'solidworks://document/current-state';
export const PUBLIC_ALPHA_BOUNDARY_RESOURCE_URI =
  'solidworks://alpha/public-boundary';

const CURRENT_DOCUMENT_STATE_RESOURCE: ResourceDefinition = {
  uri: CURRENT_DOCUMENT_STATE_RESOURCE_URI,
  aliases: ['solidworks://document/current'],
  name: 'current_document_state',
  title: 'Current Document State',
  description:
    'Read-only snapshot of the current normalized document state for the active modeling session.',
  mimeType: 'application/json',
};

const PUBLIC_ALPHA_BOUNDARY_RESOURCE: ResourceDefinition = {
  uri: PUBLIC_ALPHA_BOUNDARY_RESOURCE_URI,
  name: 'public_alpha_boundary',
  title: 'Public Alpha Boundary',
  description:
    'Static read-only boundary summary for the current public alpha tool, prompt, and resource surfaces.',
  mimeType: 'application/json',
};

export const RESOURCE_DEFINITIONS: ResourceDefinition[] = [
  CURRENT_DOCUMENT_STATE_RESOURCE,
  PUBLIC_ALPHA_BOUNDARY_RESOURCE,
];

export const PUBLIC_ALPHA_RESOURCE_DEFINITIONS: ResourceDefinition[] = [
  CURRENT_DOCUMENT_STATE_RESOURCE,
  PUBLIC_ALPHA_BOUNDARY_RESOURCE,
];

export function getResourceDefinition(
  uri: string,
  surface: 'all' | 'public-alpha' = 'all',
): ResourceDefinition | undefined {
  const definitions =
    surface === 'public-alpha'
      ? PUBLIC_ALPHA_RESOURCE_DEFINITIONS
      : RESOURCE_DEFINITIONS;

  return definitions.find(
    (definition) =>
      definition.uri === uri || definition.aliases?.includes(uri) === true,
  );
}
