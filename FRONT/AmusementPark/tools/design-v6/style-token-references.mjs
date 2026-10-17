// Lexical coverage only: a declaration elsewhere does not prove CSS scope or contrast.
// Browser captures remain required for computed style, both themes and each layout.
export const optionalExternalStyleTokens = ['--app-public-header-height', '--p-primary-color'];

export function auditStyleTokenReferences(documents, optionalNames = optionalExternalStyleTokens) {
  const definitions = new Set();
  const references = new Map();
  for (const { path, source } of documents) {
    const text = source.replace(/\/\*[\s\S]*?\*\//g, match => match.replace(/[^\n]/g, ' '))
      .replace(/^\s*\/\/[^\n]*/gm, match => match.replace(/[^\n]/g, ' '));
    for (const pattern of [/(--[\w-]+)\s*:/g, /['"](--[\w-]+)['"]\s*:/g,
      /style\.(--[\w-]+)/g, /setProperty\(\s*['"](--[\w-]+)['"]/g]) {
      for (const match of text.matchAll(pattern)) definitions.add(match[1]);
    }
    for (const match of text.matchAll(/var\(\s*(--[\w-]+)/g)) {
      const entries = references.get(match[1]) ?? [];
      entries.push({ path, line: text.slice(0, match.index).split('\n').length,
        hasFallback: text.slice(match.index + match[0].length).trimStart().startsWith(',') });
      references.set(match[1], entries);
    }
  }
  const optional = new Set(optionalNames);
  const unresolved = [...references].filter(([name]) => !definitions.has(name))
    .map(([name, uses]) => ({ name, uses }));
  return {
    scope: 'lexical; computed CSS scope, theme values and contrast require browser verification',
    files: documents.length,
    definedNames: definitions.size,
    referencedNames: references.size,
    external: unresolved.filter(({ name, uses }) => optional.has(name) && uses.every(use => use.hasFallback)),
    issues: unresolved.filter(({ name, uses }) => !optional.has(name) || uses.some(use => !use.hasFallback)),
  };
}
