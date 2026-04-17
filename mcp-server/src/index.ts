import { StdioServerTransport } from '@modelcontextprotocol/sdk/server/stdio.js';
import { createSolidWorksMcpServer } from './server.js';

async function main(): Promise<void> {
  const { server, dispose } = createSolidWorksMcpServer();
  const transport = new StdioServerTransport();

  const shutdown = async (): Promise<void> => {
    await Promise.allSettled([server.close(), dispose()]);
  };

  process.once('SIGINT', () => {
    void shutdown().finally(() => process.exit(0));
  });

  process.once('SIGTERM', () => {
    void shutdown().finally(() => process.exit(0));
  });

  await server.connect(transport);
}

main().catch((error) => {
  const message =
    error instanceof Error ? (error.stack ?? error.message) : String(error);
  process.stderr.write(`${message}\n`);
  process.exit(1);
});
