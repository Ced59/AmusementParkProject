import { existsSync, mkdirSync, readFileSync, readdirSync, writeFileSync } from 'node:fs';
import { dirname, join, relative, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const scriptDirectory = dirname(fileURLToPath(import.meta.url));
const frontendRoot = resolve(scriptDirectory, '..', '..');
const appRoot = join(frontendRoot, 'src', 'app');
const repositoryRoot = resolve(frontendRoot, '..', '..');
const outputDirectory = process.env.V6_INVENTORY_DIR
  ? resolve(process.env.V6_INVENTORY_DIR)
  : join(repositoryRoot, 'docs', 'design-v6', 'generated');

const aliasRoots = new Map([
  ['@app/', appRoot],
  ['@core/', join(appRoot, 'core')],
  ['@shared/', join(appRoot, 'shared')],
  ['@features/', join(appRoot, 'features')],
  ['@data-access/', join(appRoot, 'data-access')],
  ['@ui/', join(appRoot, 'ui')]
]);

function normalizePath(path) {
  return path.replaceAll('\\', '/');
}

function repositoryPath(path) {
  return normalizePath(relative(repositoryRoot, path));
}

function scanBalanced(source, startIndex, open, close) {
  let depth = 0;
  let quote = '';
  let lineComment = false;
  let blockComment = false;
  let escaped = false;

  for (let index = startIndex; index < source.length; index += 1) {
    const char = source[index];
    const next = source[index + 1] ?? '';

    if (lineComment) {
      if (char === '\n') {
        lineComment = false;
      }
      continue;
    }

    if (blockComment) {
      if (char === '*' && next === '/') {
        blockComment = false;
        index += 1;
      }
      continue;
    }

    if (quote) {
      if (escaped) {
        escaped = false;
      } else if (char === '\\') {
        escaped = true;
      } else if (char === quote) {
        quote = '';
      }
      continue;
    }

    if (char === '/' && next === '/') {
      lineComment = true;
      index += 1;
      continue;
    }

    if (char === '/' && next === '*') {
      blockComment = true;
      index += 1;
      continue;
    }

    if (char === '\'' || char === '"' || char === '`') {
      quote = char;
      continue;
    }

    if (char === open) {
      depth += 1;
    } else if (char === close) {
      depth -= 1;
      if (depth === 0) {
        return index;
      }
    }
  }

  throw new Error(`Unbalanced ${open}${close} block at ${startIndex}.`);
}

function findRoutesArray(source) {
  const markers = [
    /export\s+const\s+routes\s*:\s*Routes\s*=\s*\[/m,
    /export\s+const\s+PROFILE_ROUTES\s*:\s*Routes\s*=\s*\[/m,
    /const\s+routes\s*:\s*Routes\s*=\s*\[/m
  ];

  for (const marker of markers) {
    const match = marker.exec(source);
    if (match) {
      const start = source.indexOf('[', match.index);
      const end = scanBalanced(source, start, '[', ']');
      return source.slice(start, end + 1);
    }
  }

  throw new Error('No Angular Routes array found.');
}

function splitRouteObjects(arraySource) {
  const objects = [];
  let index = 1;

  while (index < arraySource.length - 1) {
    if (arraySource[index] === '{') {
      const end = scanBalanced(arraySource, index, '{', '}');
      objects.push(arraySource.slice(index, end + 1));
      index = end + 1;
    } else {
      index += 1;
    }
  }

  return objects;
}

function directProperties(objectSource) {
  const body = objectSource.slice(1, -1);
  const properties = new Map();
  let index = 0;

  while (index < body.length) {
    while (index < body.length && /[\s,]/.test(body[index])) {
      index += 1;
    }
    const keyMatch = /^[A-Za-z_$][\w$]*/.exec(body.slice(index));
    if (!keyMatch) {
      index += 1;
      continue;
    }

    const key = keyMatch[0];
    let cursor = index + key.length;
    while (/\s/.test(body[cursor] ?? '')) {
      cursor += 1;
    }
    if (body[cursor] !== ':') {
      index += key.length;
      continue;
    }

    cursor += 1;
    const valueStart = cursor;
    let curly = 0;
    let square = 0;
    let round = 0;
    let quote = '';
    let escaped = false;
    let lineComment = false;
    let blockComment = false;

    for (; cursor < body.length; cursor += 1) {
      const char = body[cursor];
      const next = body[cursor + 1] ?? '';

      if (lineComment) {
        if (char === '\n') {
          lineComment = false;
        }
        continue;
      }
      if (blockComment) {
        if (char === '*' && next === '/') {
          blockComment = false;
          cursor += 1;
        }
        continue;
      }
      if (quote) {
        if (escaped) {
          escaped = false;
        } else if (char === '\\') {
          escaped = true;
        } else if (char === quote) {
          quote = '';
        }
        continue;
      }
      if (char === '/' && next === '/') {
        lineComment = true;
        cursor += 1;
        continue;
      }
      if (char === '/' && next === '*') {
        blockComment = true;
        cursor += 1;
        continue;
      }
      if (char === '\'' || char === '"' || char === '`') {
        quote = char;
        continue;
      }
      if (char === '{') {
        curly += 1;
      } else if (char === '}') {
        curly -= 1;
      } else if (char === '[') {
        square += 1;
      } else if (char === ']') {
        square -= 1;
      } else if (char === '(') {
        round += 1;
      } else if (char === ')') {
        round -= 1;
      } else if (char === ',' && curly === 0 && square === 0 && round === 0) {
        break;
      }
    }

    properties.set(key, body.slice(valueStart, cursor).trim());
    index = cursor + 1;
  }

  return properties;
}

function unquote(value) {
  const match = /^['"]([\s\S]*?)['"]$/.exec(value?.trim() ?? '');
  return match ? match[1] : '';
}

function resolveSourceImport(routeFile, importPath) {
  let basePath = '';
  for (const [alias, aliasRoot] of aliasRoots) {
    if (importPath.startsWith(alias)) {
      basePath = join(aliasRoot, importPath.slice(alias.length));
      break;
    }
  }
  if (!basePath) {
    basePath = resolve(dirname(routeFile), importPath);
  }

  const candidates = basePath.endsWith('.ts')
    ? [basePath]
    : [`${basePath}.ts`, join(basePath, 'index.ts')];
  return candidates.find((candidate) => existsSync(candidate)) ?? candidates[0];
}

function joinRoute(prefix, path) {
  const normalizedPrefix = prefix === '/' ? '' : prefix.replace(/\/$/, '');
  const normalizedPath = path.replace(/^\//, '').replace(/\/$/, '');
  const joined = [normalizedPrefix, normalizedPath].filter(Boolean).join('/');
  return joined ? `/${joined.replace(/^\//, '')}` : '/';
}

function parseImport(value) {
  const match = /import\(['"]([^'"]+)['"]\)\s*\.then\(\s*\(?\s*\w+\s*\)?\s*=>\s*\w+\.([A-Za-z_$][\w$]*)\s*\)/m.exec(value ?? '');
  return match ? { importPath: match[1], symbol: match[2] } : null;
}

function findRoutingModule(moduleFile) {
  const directory = dirname(moduleFile);
  const candidates = readdirSync(directory)
    .filter((name) => name.endsWith('-routing.module.ts'))
    .sort();
  if (candidates.length !== 1) {
    throw new Error(`Expected one routing module beside ${repositoryPath(moduleFile)}, found ${candidates.length}.`);
  }
  return join(directory, candidates[0]);
}

function routeFamily(route) {
  if (route === '/') {
    return 'root';
  }
  if (route === '/:lang') {
    return 'layout';
  }
  if (route.startsWith('/:lang/admin')) {
    return 'admin';
  }
  if (route.startsWith('/:lang/profile') || route.startsWith('/:lang/passport/local') || /confirm-account|forgot-password|reset-password/.test(route)) {
    return 'account';
  }
  if (route.includes('/item/')) {
    return 'parkitem';
  }
  if (route.includes('/attraction/')) {
    return 'standalone';
  }
  if (route.includes('/park/')) {
    return 'park';
  }
  return 'public';
}

function findComponentMetadata(componentFile) {
  if (!componentFile || !existsSync(componentFile)) {
    return { template: '', styles: [], metrics: emptyMetrics() };
  }
  const source = readFileSync(componentFile, 'utf8');
  const templateMatch = /templateUrl\s*:\s*['"]([^'"]+)['"]/.exec(source);
  const styleMatches = [];
  const styleUrlMatch = /styleUrl\s*:\s*['"]([^'"]+)['"]/.exec(source);
  if (styleUrlMatch) {
    styleMatches.push(styleUrlMatch[1]);
  }
  const styleUrlsMatch = /styleUrls\s*:\s*\[([\s\S]*?)\]/.exec(source);
  if (styleUrlsMatch) {
    for (const match of styleUrlsMatch[1].matchAll(/['"]([^'"]+)['"]/g)) {
      styleMatches.push(match[1]);
    }
  }

  const templateFile = templateMatch ? resolve(dirname(componentFile), templateMatch[1]) : '';
  const templateSource = templateFile && existsSync(templateFile) ? readFileSync(templateFile, 'utf8') : '';
  return {
    template: templateFile && existsSync(templateFile) ? repositoryPath(templateFile) : templateMatch ? `MISSING:${templateMatch[1]}` : 'inline-or-none',
    styles: styleMatches.map((style) => {
      const styleFile = resolve(dirname(componentFile), style);
      return existsSync(styleFile) ? repositoryPath(styleFile) : `MISSING:${style}`;
    }),
    metrics: inspectPresentation(source, templateSource)
  };
}

function countMatches(source, expression) {
  return [...source.matchAll(expression)].length;
}

function emptyMetrics() {
  return {
    buttons: 0,
    links: 0,
    forms: 0,
    inputs: 0,
    clicks: 0,
    conditions: 0,
    loops: 0,
    deferred: 0,
    loading: false,
    empty: false,
    error: false,
    dialogs: 0,
    tables: 0
  };
}

function inspectPresentation(componentSource, templateSource) {
  const combined = `${componentSource}\n${templateSource}`;
  return {
    buttons: countMatches(templateSource, /<button\b/gi) + countMatches(templateSource, /appUiButton\b/g),
    links: countMatches(templateSource, /<a\b/gi),
    forms: countMatches(templateSource, /<form\b/gi),
    inputs: countMatches(templateSource, /<(?:input|select|textarea)\b/gi),
    clicks: countMatches(templateSource, /\(click\)\s*=/g),
    conditions: countMatches(templateSource, /@if\s*\(/g) + countMatches(templateSource, /\*ngIf\s*=/g),
    loops: countMatches(templateSource, /@for\s*\(/g) + countMatches(templateSource, /\*ngFor\s*=/g),
    deferred: countMatches(templateSource, /@defer\b/g),
    loading: /loading|skeleton|isLoading|chargement/i.test(combined),
    empty: /empty|noData|isEmpty|aucun|vide/i.test(combined),
    error: /error|erreur|failed|failure/i.test(combined),
    dialogs: countMatches(templateSource, /<(?:app-ui-dialog|p-dialog)\b/gi),
    tables: countMatches(templateSource, /<(?:table|p-table)\b/gi)
  };
}

function compactText(value) {
  return (value ?? '')
    .replace(/<!--[\s\S]*?-->/g, ' ')
    .replace(/<[^>]+>/g, ' ')
    .replace(/\{\{[\s\S]*?\}\}/g, '{{…}}')
    .replace(/\s+/g, ' ')
    .trim()
    .slice(0, 180);
}

function attributeValue(source, name) {
  const escapedName = name.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
  const match = new RegExp(`(?:^|\\s)${escapedName}\\s*=\\s*(["'])([\\s\\S]*?)\\1`, 'i').exec(source);
  return match?.[2]?.replace(/\s+/g, ' ').trim() ?? '';
}

function inspectInteractions(component) {
  if (!component.template || component.template === 'inline-or-none' || component.template.startsWith('MISSING:')) {
    return [];
  }

  const templateFile = resolve(repositoryRoot, component.template);
  if (!existsSync(templateFile)) {
    return [];
  }

  const templateSource = readFileSync(templateFile, 'utf8');
  const interactions = [];
  const pushInteraction = (type, attributes, content = '') => {
    interactions.push({
      scope: component.scope,
      componentFile: component.componentFile,
      template: component.template,
      type,
      label: attributeValue(attributes, 'aria-label')
        || attributeValue(attributes, '[attr.aria-label]')
        || attributeValue(attributes, '[label]')
        || attributeValue(attributes, 'label')
        || attributeValue(attributes, '[placeholder]')
        || attributeValue(attributes, 'placeholder')
        || attributeValue(attributes, 'title')
        || attributeValue(attributes, 'name')
        || compactText(content),
      click: attributeValue(attributes, '(click)'),
      submit: attributeValue(attributes, '(ngSubmit)'),
      routerLink: attributeValue(attributes, '[routerLink]') || attributeValue(attributes, 'routerLink'),
      href: attributeValue(attributes, 'href') || attributeValue(attributes, '[href]'),
      disabled: attributeValue(attributes, '[disabled]') || attributeValue(attributes, 'disabled'),
      visibility: attributeValue(attributes, '*ngIf') || attributeValue(attributes, '[hidden]')
    });
  };

  for (const match of templateSource.matchAll(/<button\b([^>]*)>([\s\S]*?)<\/button>/gi)) {
    pushInteraction('button', match[1], match[2]);
  }
  for (const match of templateSource.matchAll(/<a\b([^>]*)>([\s\S]*?)<\/a>/gi)) {
    pushInteraction('link', match[1], match[2]);
  }
  for (const match of templateSource.matchAll(/<form\b([^>]*)>/gi)) {
    pushInteraction('form', match[1]);
  }
  for (const match of templateSource.matchAll(/<(input|select|textarea)\b([^>]*)>/gi)) {
    pushInteraction(match[1].toLowerCase(), match[2]);
  }
  for (const match of templateSource.matchAll(/<app-ui-button\b([^>]*)>([\s\S]*?)<\/app-ui-button>/gi)) {
    pushInteraction('app-ui-button', match[1], match[2]);
  }

  return interactions;
}

function parseRoutesFile(routeFile, prefix = '', ancestryGuards = []) {
  const source = readFileSync(routeFile, 'utf8');
  const routeArray = findRoutesArray(source);
  return parseRouteArray(routeArray, routeFile, prefix, ancestryGuards);
}

function parseRouteArray(routeArray, routeFile, prefix, ancestryGuards) {
  const routes = [];
  for (const objectSource of splitRouteObjects(routeArray)) {
    const properties = directProperties(objectSource);
    const path = unquote(properties.get('path'));
    const fullRoute = joinRoute(prefix, path);
    const ownGuards = [properties.get('canActivate'), properties.get('canActivateChild')]
      .filter(Boolean)
      .flatMap((value) => [...value.matchAll(/([A-Za-z_$][\w$]*Guard)\b/g)].map((match) => match[1]));
    const guards = [...new Set([...ancestryGuards, ...ownGuards])];
    const children = properties.get('children');
    const lazy = parseImport(properties.get('loadChildren'));
    const component = parseImport(properties.get('loadComponent'));
    const redirect = unquote(properties.get('redirectTo'));
    const resolvers = [...(properties.get('resolve') ?? '').matchAll(/:\s*([A-Za-z_$][\w$]*Resolver)\b/g)].map((match) => match[1]);

    if (children) {
      routes.push(...parseRouteArray(children, routeFile, fullRoute, guards));
      continue;
    }

    if (lazy) {
      const moduleFile = resolveSourceImport(routeFile, lazy.importPath);
      const routingModule = findRoutingModule(moduleFile);
      routes.push(...parseRoutesFile(routingModule, fullRoute, guards));
      continue;
    }

    let componentFile = '';
    let componentSymbol = '';
    if (component) {
      componentFile = resolveSourceImport(routeFile, component.importPath);
      componentSymbol = component.symbol;
    }
    const metadata = findComponentMetadata(componentFile);
    routes.push({
      family: routeFamily(fullRoute),
      route: fullRoute,
      kind: redirect ? 'redirect' : component ? 'component' : 'guard-or-layout',
      component: componentSymbol || (redirect ? `redirect:${redirect}` : ''),
      componentFile: componentFile ? repositoryPath(componentFile) : '',
      template: metadata.template,
      styles: metadata.styles.join(' | '),
      routeFile: repositoryPath(routeFile),
      guards: guards.join(' | '),
      resolvers: resolvers.join(' | '),
      ...metadata.metrics
    });
  }
  return routes;
}

function csvEscape(value) {
  const text = String(value ?? '');
  return `"${text.replaceAll('"', '""')}"`;
}

function writeCsv(file, rows, columns) {
  const lines = [columns.map(csvEscape).join(';')];
  for (const row of rows) {
    lines.push(columns.map((column) => csvEscape(row[column])).join(';'));
  }
  writeFileSync(file, `${lines.join('\n')}\n`, 'utf8');
}

function walk(directory) {
  const files = [];
  for (const entry of readdirSync(directory, { withFileTypes: true })) {
    const path = join(directory, entry.name);
    if (entry.isDirectory()) {
      files.push(...walk(path));
    } else {
      files.push(path);
    }
  }
  return files;
}

function buildComponentInventory() {
  return walk(appRoot)
    .filter((path) => path.endsWith('.component.ts') && !path.endsWith('.spec.ts'))
    .sort()
    .map((componentFile) => {
      const metadata = findComponentMetadata(componentFile);
      const scopeMatch = /src\/app\/(ui|shared|features\/[^/]+)/.exec(normalizePath(componentFile));
      return {
        scope: scopeMatch?.[1] ?? 'app',
        componentFile: repositoryPath(componentFile),
        template: metadata.template,
        styles: metadata.styles.join(' | '),
        ...metadata.metrics
      };
    });
}

function buildStyleInventory() {
  const styleRoots = [join(frontendRoot, 'src', 'styles'), join(frontendRoot, 'src', 'app')];
  return styleRoots
    .flatMap((directory) => walk(directory))
    .filter((path) => path.endsWith('.scss'))
    .sort()
    .map((styleFile) => {
      // Repository-normalized bytes make this inventory reproducible on Windows and Linux.
      const source = readFileSync(styleFile, 'utf8').replaceAll('\r\n', '\n');
      return {
        styleFile: repositoryPath(styleFile),
        bytes: Buffer.byteLength(source, 'utf8'),
        lines: source.split(/\r?\n/).length,
        customProperties: countMatches(source, /--[a-z0-9-]+\s*:/gi),
        variableReferences: countMatches(source, /var\(\s*--[a-z0-9-]+/gi),
        mediaQueries: countMatches(source, /@media\b/gi),
        containerQueries: countMatches(source, /@container\b/gi),
        important: countMatches(source, /!important\b/gi),
        gradients: countMatches(source, /(?:linear|radial|conic)-gradient\(/gi),
        shadows: countMatches(source, /(?:box|text)-shadow\s*:/gi),
        radiusDeclarations: countMatches(source, /border-radius\s*:/gi)
      };
    });
}

mkdirSync(outputDirectory, { recursive: true });

const routes = parseRoutesFile(join(appRoot, 'app.routes.ts'));
const routeColumns = [
  'family', 'route', 'kind', 'component', 'componentFile', 'template', 'styles', 'routeFile',
  'guards', 'resolvers', 'buttons', 'links', 'forms', 'inputs', 'clicks', 'conditions', 'loops',
  'deferred', 'loading', 'empty', 'error', 'dialogs', 'tables'
];
writeCsv(join(outputDirectory, 'V6-ROUTE-INVENTORY.csv'), routes, routeColumns);

const components = buildComponentInventory();
const componentColumns = [
  'scope', 'componentFile', 'template', 'styles', 'buttons', 'links', 'forms', 'inputs', 'clicks',
  'conditions', 'loops', 'deferred', 'loading', 'empty', 'error', 'dialogs', 'tables'
];
writeCsv(join(outputDirectory, 'V6-COMPONENT-INVENTORY.csv'), components, componentColumns);

const interactions = components.flatMap(inspectInteractions);
const interactionColumns = [
  'scope', 'componentFile', 'template', 'type', 'label', 'click', 'submit', 'routerLink', 'href',
  'disabled', 'visibility'
];
writeCsv(join(outputDirectory, 'V6-INTERACTION-INVENTORY.csv'), interactions, interactionColumns);

const styles = buildStyleInventory();
const styleColumns = [
  'styleFile', 'bytes', 'lines', 'customProperties', 'variableReferences', 'mediaQueries',
  'containerQueries', 'important', 'gradients', 'shadows', 'radiusDeclarations'
];
writeCsv(join(outputDirectory, 'V6-STYLE-INVENTORY.csv'), styles, styleColumns);

const familyCounts = [...new Set(routes.map((route) => route.family))]
  .sort()
  .map((family) => ({ family, count: routes.filter((route) => route.family === family).length }));
const scopeCounts = [...new Set(components.map((component) => component.scope))]
  .sort()
  .map((scope) => ({ scope, count: components.filter((component) => component.scope === scope).length }));
const missingTemplates = routes.filter((route) => route.kind === 'component' && route.template.startsWith('MISSING:'));

const summary = [
  '# Inventaire P00 généré',
  '',
  `- Routes terminales, redirections et écrans développés : **${routes.length}**`,
  `- Composants Angular de présentation recensés : **${components.length}**`,
  `- Interactions et champs explicitement recensés : **${interactions.length}**`,
  `- Feuilles SCSS globales et locales recensées : **${styles.length}**`,
  `- Templates de routes manquants : **${missingTemplates.length}**`,
  '',
  '## Routes par famille',
  '',
  '| Famille | Nombre |',
  '|---|---:|',
  ...familyCounts.map(({ family, count }) => `| ${family} | ${count} |`),
  '',
  '## Composants par périmètre',
  '',
  '| Périmètre | Nombre |',
  '|---|---:|',
  ...scopeCounts.map(({ scope, count }) => `| ${scope} | ${count} |`),
  '',
  'Les CSV voisins sont la source exhaustive. Ils doivent être régénérés après chaque intégration de `origin/master` et avant P22.',
  ''
].join('\n');
writeFileSync(join(outputDirectory, 'README.md'), summary, 'utf8');

console.log(`Generated ${routes.length} routes and ${components.length} components in ${repositoryPath(outputDirectory)}.`);
