import { spawnSync } from 'node:child_process';

const npmCommand = process.platform === 'win32' ? 'npm.cmd' : 'npm';

const steps = [
  ['run', 'build'],
  ['run', 'typecheck'],
  ['run', 'lint'],
  ['test'],
  ['run', 'audit'],
];

for (const args of steps) {
  const label = `${npmCommand} ${args.join(' ')}`;
  process.stdout.write(`\n> ${label}\n`);

  const result = spawnSync(npmCommand, args, {
    cwd: process.cwd(),
    stdio: 'inherit',
    shell: process.platform === 'win32',
  });

  if (result.status !== 0) {
    process.exit(result.status ?? 1);
  }
}

process.stdout.write('\nVerification completed successfully.\n');
