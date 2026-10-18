import ts from 'typescript';

// Complete the existing CSS-mask system from the already locked Lucide package.
// Never imported by the application; no Angular/icon runtime is added.
export const completedIconAliases = {
  'pi-heart-fill': ['Heart', true], 'pi-bookmark-fill': ['Bookmark', true], 'pi-bookmark': ['Bookmark'],
  'pi-bell': ['Bell'], 'pi-bell-slash': ['BellOff'], 'pi-bell-fill': ['Bell', true],
  'pi-verified': ['BadgeCheck'], 'pi-wave-pulse': ['Activity'], 'pi-pause': ['Pause'],
  'pi-align-center': ['TextAlignCenter'], 'pi-align-right': ['TextAlignEnd'], 'pi-hourglass': ['Hourglass'],
  'pi-chart-pie': ['ChartPie'], 'pi-check-square': ['SquareCheck'], 'pi-pause-circle': ['CirclePause'],
  'pi-sort-amount-up': ['ArrowUpWideNarrow'], 'pi-question-circle': ['CircleQuestionMark'],
  'pi-comments': ['MessagesSquare'], 'pi-comment': ['MessageCircle'], 'pi-minus': ['Minus'],
  'pi-question': ['CircleQuestionMark'], 'pi-wifi': ['Wifi'], 'pi-wallet': ['Wallet'],
  'pi-lightbulb': ['Lightbulb'], 'pi-stop': ['Square', true], 'pi-equals': ['Equal'],
  'pi-crosshairs': ['Crosshair'], 'pi-file-pdf': ['FileText'], 'pi-passport': ['BookUser'],
  'pi-quote-left': ['Quote'], 'pi-trophy': ['Trophy'], 'pi-stop-circle': ['CircleStop'],
  'pi-arrow-right-arrow-left': ['ArrowRightLeft'], 'pi-table': ['Table'], 'pi-mobile': ['Smartphone'],
  'pi-wand': ['WandSparkles'], 'pi-folder': ['Folder'], 'pi-angle-double-up': ['ChevronsUp'],
  'pi-angle-up': ['ChevronUp'], 'pi-angle-double-down': ['ChevronsDown'], 'pi-print': ['Printer'],
  'pi-building-columns': ['Landmark'], 'pi-minus-circle': ['CircleMinus'],
};

function literal(node) {
  if (ts.isStringLiteral(node)) return node.text;
  if (ts.isNumericLiteral(node)) return Number(node.text);
  if (ts.isArrayLiteralExpression(node)) return node.elements.map(literal);
  if (ts.isObjectLiteralExpression(node)) return Object.fromEntries(node.properties.map(property => {
    if (!ts.isPropertyAssignment(property) || !(ts.isIdentifier(property.name) || ts.isStringLiteral(property.name))) throw new Error('Non-literal Lucide property.');
    return [property.name.text, literal(property.initializer)];
  }));
  throw new Error('Only literal installed icon data can be converted.');
}

function installedIcon(source, name) {
  if (!/^[A-Z][a-zA-Z]+$/.test(name)) throw new Error('Invalid Lucide icon name.');
  const match = new RegExp(`class Lucide${name} extends LucideIconBase \\{\\s*static icon = (\\{[\\s\\S]*?\\n    \\});`).exec(source);
  if (!match) throw new Error(`Missing installed Lucide icon ${name}.`);
  const file = ts.createSourceFile('installed-icon.ts', `const icon = ${match[1]};`, ts.ScriptTarget.Latest, true);
  if (file.parseDiagnostics.length) throw new Error('Installed icon data could not be parsed.');
  return literal(file.statements[0].declarationList.declarations[0].initializer);
}

const escapeXml = value => String(value).replace(/[&"<>]/g, character => ({ '&': '&amp;', '"': '&quot;', '<': '&lt;', '>': '&gt;' })[character]);

export function encodeIconSvg(svg) {
  // CSS url() stays double-quoted; SVG attribute quotes can be single quotes.
  // Keep <, >, #, %, controls and other unsafe bytes percent-encoded.
  return encodeURIComponent(svg).replaceAll('%20', ' ').replaceAll('%3D', '=')
    .replaceAll('%3A', ':').replaceAll('%2F', '/').replaceAll('%22', "'");
}

export function completedIconStyles(packageSource) {
  const svg = (name, filled = false) => {
    const icon = installedIcon(packageSource, name);
    if (icon.size !== 24 || !Array.isArray(icon.node) || !icon.node.length) throw new Error('Unexpected installed icon geometry.');
    const nodes = icon.node.map(([tag, attributes]) => {
      if (!['path', 'circle', 'rect', 'line', 'polyline', 'polygon', 'ellipse'].includes(tag)) throw new Error('Non-geometric SVG node.');
      return `<${tag} ${Object.entries(attributes).filter(([key]) => key !== 'key').map(([key, value]) => {
        if (!['d', 'cx', 'cy', 'r', 'rx', 'ry', 'x', 'y', 'x1', 'x2', 'y1', 'y2', 'width', 'height', 'points'].includes(key)) throw new Error('Unexpected SVG attribute.');
        return `${key}="${escapeXml(value)}"`;
      }).join(' ')} />`;
    }).join('');
    return `<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="${filled ? 'black' : 'none'}" stroke="black" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">${nodes}</svg>`;
  };
  return Object.entries(completedIconAliases).sort(([left], [right]) => left.localeCompare(right))
    .map(([alias, [name, filled]]) => `.pi.${alias} {\n  --app-icon-mask: url("data:image/svg+xml,${encodeIconSvg(svg(name, filled))}");\n}`).join('\n\n') + '\n';
}

export function missingIconAliases(documents, compatibilityStyles) {
  const defined = new Set([...compatibilityStyles.matchAll(/\.pi\.(pi-[a-z0-9-]+)\s*\{/g)].map(match => match[1]));
  const missing = new Map();
  for (const { path, source } of documents) {
    for (const [name] of source.matchAll(/\bpi-[a-z][a-z0-9-]*\b/g)) {
      if (name === 'pi-spin' || defined.has(name)) continue;
      if (!missing.has(name)) missing.set(name, []);
      if (!missing.get(name).includes(path)) missing.get(name).push(path);
    }
  }
  return [...missing].map(([name, paths]) => ({ name, paths }));
}
