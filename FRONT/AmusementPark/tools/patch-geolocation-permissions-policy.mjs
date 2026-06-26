import { readFileSync, writeFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { dirname, join } from 'node:path';

const toolsDir = dirname(fileURLToPath(import.meta.url));
const projectRoot = join(toolsDir, '..');

const replacements = [
  {
    label: 'Angular SSR server',
    path: join(projectRoot, 'server.ts'),
    from: "geolocation=()",
    to: "geolocation=(self)"
  },
  {
    label: 'Front Nginx security headers',
    path: join(projectRoot, 'nginx/snippets/security-headers-base.conf'),
    from: "geolocation=()",
    to: "geolocation=(self)"
  }
];

let patchedCount = 0;

for (const replacement of replacements) {
  const content = readFileSync(replacement.path, 'utf8');
  const updated = content.replaceAll(replacement.from, replacement.to);

  if (updated === content) {
    console.log(`${replacement.label}: no geolocation policy patch needed.`);
    continue;
  }

  writeFileSync(replacement.path, updated, 'utf8');
  patchedCount += 1;
  console.log(`${replacement.label}: patched ${replacement.from} -> ${replacement.to}.`);
}

console.log(`Geolocation Permissions-Policy patch completed. Patched files: ${patchedCount}.`);
