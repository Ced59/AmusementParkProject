import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import {
  collectClassFileViolations,
  summarizeViolations,
} from './one-class-per-file.mjs';

const architectureDirectory = dirname(fileURLToPath(import.meta.url));
const repositoryRoot = resolve(architectureDirectory, '../../../..');
const currentViolations = collectClassFileViolations(repositoryRoot);

if (currentViolations.length > 0) {
  console.error('One-class-per-file architecture check failed:');
  for (const violation of currentViolations) {
    console.error(`- ${violation.path}: ${violation.classes.join(', ')}`);
  }
  console.error('Move every class to its own dedicated file.');
  process.exit(1);
}

const summary = summarizeViolations(currentViolations);
console.log(
  `One-class-per-file architecture check passed: ${summary.csharp} C# and ${summary.typescript} TypeScript violations.`,
);
