import { readdirSync, readFileSync, statSync } from 'node:fs';
import { join } from 'node:path';

const ROOT = join(process.cwd(), 'src', 'app', 'features');
const FACADE_SUFFIX = 'facade.ts';
const DATA_ACCESS_IMPORT = /from ['"](?:@app\/data-access|@data-access)\//;
const CONCRETE_API_CONSTRUCTOR = /private readonly \w+:\s*\w+ApiService(?!Port)\b/;
const PORT_TOKEN_INJECTION = /@Inject\([A-Z0-9_]+_PORT\)\s*private readonly \w+:\s*\w+Port\b/;

function walk(directory) {
  return readdirSync(directory)
    .flatMap((entry) => {
      const path = join(directory, entry);
      return statSync(path).isDirectory() ? walk(path) : [path];
    });
}

const facadeFiles = walk(ROOT).filter((path) => path.endsWith(FACADE_SUFFIX));
const violations = [];

for (const path of facadeFiles) {
  const content = readFileSync(path, 'utf8');
  const hasDataAccessImport = content
    .split('\n')
    .some((line) => line.includes('ApiService') && DATA_ACCESS_IMPORT.test(line));
  const hasConcreteConstructor = CONCRETE_API_CONSTRUCTOR.test(content);
  const referencesApiService = /this\.\w+ApiService\s*\./.test(content);
  const needsDataPort = /this\.\w+ApiService\s*\./.test(content) || /ApiServicePort/.test(content);
  const hasPortInjection = PORT_TOKEN_INJECTION.test(content);

  if (hasDataAccessImport) {
    violations.push(`${path}: imports a concrete data-access ApiService`);
  }

  if (hasConcreteConstructor) {
    violations.push(`${path}: injects a concrete ApiService in the constructor`);
  }

  if (referencesApiService && needsDataPort && !hasPortInjection) {
    violations.push(`${path}: references an API dependency but does not inject a facade data port`);
  }
}

if (violations.length > 0) {
  console.error('Facade data port architecture check failed:');
  for (const violation of violations) {
    console.error(`- ${violation}`);
  }

  process.exit(1);
}

console.log(`Facade data port architecture check passed for ${facadeFiles.length} facades.`);
