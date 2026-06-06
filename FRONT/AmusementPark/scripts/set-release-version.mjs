import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

import { writeConfiguredReleaseVersion } from './release-version-utils.mjs';

const scriptDirectory = dirname(fileURLToPath(import.meta.url));
const projectRoot = resolve(scriptDirectory, '..');
const requestedVersion = process.argv[2];

if (!requestedVersion) {
  throw new Error('Usage: npm run release:version -- 1.2.0');
}

const version = writeConfiguredReleaseVersion(projectRoot, requestedVersion);

console.log(`Configured frontend release version ${version}.`);
