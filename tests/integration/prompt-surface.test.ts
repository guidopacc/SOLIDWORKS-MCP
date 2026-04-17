import { Client } from '@modelcontextprotocol/sdk/client/index.js';
import { InMemoryTransport } from '@modelcontextprotocol/sdk/inMemory.js';
import { describe, expect, it } from 'vitest';
import { createSolidWorksMcpServer } from '../../mcp-server/src/server.js';

describe('prompt surface', () => {
  it('exposes the first public prompt in MCP discovery', async () => {
    const { client, close, serverContext } = await connectPromptTestClient();

    try {
      const promptList = await client.listPrompts();
      const prompt = promptList.prompts.find(
        (entry) => entry.name === 'safe_modeling_session',
      );

      expect(prompt).toBeDefined();
      expect(prompt?.arguments).toEqual([
        {
          name: 'user_request',
          description:
            'Optional short description of the part or modeling outcome the user wants within the current alpha boundary.',
        },
      ]);
      expect(client.getServerCapabilities()?.prompts).toBeDefined();
      expect(client.getInstructions()).toContain('safe_modeling_session');
    } finally {
      await close();
      await serverContext.dispose();
    }
  });

  it('returns a boundary-safe prompt template for the supported modeling flow', async () => {
    const { client, close, serverContext } = await connectPromptTestClient();

    try {
      const prompt = await client.getPrompt({
        name: 'safe_modeling_session',
        arguments: {
          user_request: 'Create a simple extruded circle and export STEP.',
        },
      });
      const content = prompt.messages[0]?.content;

      expect(prompt.messages).toHaveLength(1);
      expect(prompt.description).toContain('public alpha safely');
      expect(content?.type).toBe('text');
      expect(content?.type === 'text' ? content.text : '').toContain(
        'Requested modeling goal: Create a simple extruded circle and export STEP.',
      );
      expect(content?.type === 'text' ? content.text : '').toContain(
        'new_part',
      );
      expect(content?.type === 'text' ? content.text : '').toContain(
        'get_document_state',
      );
      expect(content?.type === 'text' ? content.text : '').toContain(
        'extrude_boss',
      );
      expect(content?.type === 'text' ? content.text : '').toContain(
        'Do not claim support for add_dimension, cut_extrude, add_fillet, assemblies, drawings, open/reopen-document editing, or SolidWorks versions beyond 2022.',
      );
    } finally {
      await close();
      await serverContext.dispose();
    }
  });
});

async function connectPromptTestClient(): Promise<{
  client: Client;
  close: () => Promise<void>;
  serverContext: ReturnType<typeof createSolidWorksMcpServer>;
}> {
  const serverContext = createSolidWorksMcpServer();
  const client = new Client(
    {
      name: 'prompt-surface-test-client',
      version: '0.1.0',
    },
    {
      capabilities: {},
    },
  );
  const [clientTransport, serverTransport] =
    InMemoryTransport.createLinkedPair();

  await Promise.all([
    serverContext.server.connect(serverTransport),
    client.connect(clientTransport),
  ]);

  return {
    client,
    close: async (): Promise<void> => {
      await Promise.allSettled([client.close(), serverContext.server.close()]);
    },
    serverContext,
  };
}
