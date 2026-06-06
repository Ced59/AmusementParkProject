import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

import { readConfiguredReleaseVersion, releaseVersionFilePath } from './release-version-utils.mjs';

const scriptDirectory = dirname(fileURLToPath(import.meta.url));
const projectRoot = resolve(scriptDirectory, '..');
const version = readConfiguredReleaseVersion(projectRoot);

if (!version) {
  throw new Error(`Release version file not found: ${releaseVersionFilePath(projectRoot)}.`);
}

console.log(version);
