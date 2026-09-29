import { readdir, readFile, stat } from 'node:fs/promises';
import { dirname, resolve } from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';

const toolDirectory = dirname(fileURLToPath(import.meta.url));
const projectDirectory = resolve(toolDirectory, '../..');
const defaultBrowserDirectory = resolve(projectDirectory, 'dist/amusement-park/browser');
const defaultConfigPath = resolve(toolDirectory, 'bundle-budget.config.json');

export function extractInitialAssetNames(html) {
  const assets = [];
  const expression = /(?:href|src)="([^"]+\.(?:js|css))"/gu;
  for (const match of html.matchAll(expression)) {
    if (!/^https?:\/\//u.test(match[1]) && !assets.includes(match[1])) {
      assets.push(match[1]);
    }
  }

  return assets;
}

export function extractStaticImportAssetNames(javascript) {
  const assets = [];
  const expression = /(?:\bfrom\s*|\bimport\s*)["']\.\/([^"']+\.js)["']/gu;
  for (const match of javascript.matchAll(expression)) {
    if (!assets.includes(match[1])) {
      assets.push(match[1]);
    }
  }

  return assets;
}

export function evaluateBundleBudget(metrics, budget) {
  const failures = [];
  const checks = [
    ['initialBytes', 'maximumInitialBytes', 'chargement initial'],
    ['largestLazyChunkBytes', 'maximumLargestLazyChunkBytes', 'plus gros chunk différé'],
    ['totalJavaScriptBytes', 'maximumTotalJavaScriptBytes', 'JavaScript total'],
    ['totalStylesheetBytes', 'maximumTotalStylesheetBytes', 'CSS total'],
  ];

  for (const [metricKey, budgetKey, label] of checks) {
    if (metrics[metricKey] > budget[budgetKey]) {
      failures.push(`${label}: ${metrics[metricKey]} octets > ${budget[budgetKey]} octets`);
    }
  }

  return failures;
}

export async function inspectBundle(browserDirectory) {
  const indexPath = resolve(browserDirectory, 'index.csr.html');
  const html = await readFile(indexPath, 'utf8');
  const initialAssetNames = extractInitialAssetNames(html);
  const initialAssetNameSet = new Set(initialAssetNames);
  const pendingJavaScriptNames = initialAssetNames.filter((name) => name.endsWith('.js'));

  while (pendingJavaScriptNames.length > 0) {
    const name = pendingJavaScriptNames.shift();
    const javascript = await readFile(resolve(browserDirectory, name), 'utf8');
    for (const dependencyName of extractStaticImportAssetNames(javascript)) {
      if (!initialAssetNameSet.has(dependencyName)) {
        initialAssetNameSet.add(dependencyName);
        initialAssetNames.push(dependencyName);
        pendingJavaScriptNames.push(dependencyName);
      }
    }
  }

  const fileNames = await readdir(browserDirectory);
  const javascriptNames = fileNames.filter((name) => name.endsWith('.js'));
  const stylesheetNames = fileNames.filter((name) => name.endsWith('.css'));
  const initialJavaScriptNames = new Set(initialAssetNames.filter((name) => name.endsWith('.js')));
  const lazyJavaScriptNames = javascriptNames.filter((name) => !initialJavaScriptNames.has(name));

  const sizes = new Map();
  for (const name of [...new Set([...initialAssetNames, ...javascriptNames, ...stylesheetNames])]) {
    sizes.set(name, (await stat(resolve(browserDirectory, name))).size);
  }

  return {
    initialBytes: initialAssetNames.reduce((total, name) => total + sizes.get(name), 0),
    largestLazyChunkBytes: lazyJavaScriptNames.reduce((maximum, name) => Math.max(maximum, sizes.get(name)), 0),
    totalJavaScriptBytes: javascriptNames.reduce((total, name) => total + sizes.get(name), 0),
    totalStylesheetBytes: stylesheetNames.reduce((total, name) => total + sizes.get(name), 0),
    initialAssets: initialAssetNames.map((name) => ({ name, bytes: sizes.get(name) })),
    largestLazyChunks: lazyJavaScriptNames
      .map((name) => ({ name, bytes: sizes.get(name) }))
      .sort((left, right) => right.bytes - left.bytes)
      .slice(0, 5),
  };
}

async function runCli() {
  const browserDirectory = resolve(process.env.BROWSER_DIST_DIR ?? defaultBrowserDirectory);
  const configPath = resolve(process.env.BUNDLE_BUDGET_CONFIG ?? defaultConfigPath);
  const budget = JSON.parse(await readFile(configPath, 'utf8'));
  const metrics = await inspectBundle(browserDirectory);
  const failures = evaluateBundleBudget(metrics, budget);
  const report = {
    schemaVersion: budget.schemaVersion,
    browserDirectory,
    metrics,
    budget,
    failures,
  };

  process.stdout.write(`${JSON.stringify(report, null, 2)}\n`);
  if (failures.length > 0) {
    process.exitCode = 1;
  }
}

const invokedPath = process.argv[1] ? pathToFileURL(resolve(process.argv[1])).href : null;
if (invokedPath === import.meta.url) {
  await runCli();
}
