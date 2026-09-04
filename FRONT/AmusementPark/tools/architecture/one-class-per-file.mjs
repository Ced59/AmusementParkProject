import { readFileSync, readdirSync } from 'node:fs';
import { basename, extname, join, relative } from 'node:path';
import ts from 'typescript';

const SKIPPED_DIRECTORIES = new Set([
  '.git',
  'bin',
  'coverage',
  'dist',
  'node_modules',
  'obj',
  'third_party',
  'vendor',
]);

function walkSourceFiles(directory) {
  return readdirSync(directory, { withFileTypes: true })
    .flatMap((entry) => {
      if (entry.isDirectory() && SKIPPED_DIRECTORIES.has(entry.name.toLowerCase())) {
        return [];
      }

      const path = join(directory, entry.name);
      return entry.isDirectory() ? walkSourceFiles(path) : [path];
    });
}

function replaceRangeWithSpaces(characters, start, end) {
  for (let index = start; index < end; index += 1) {
    if (characters[index] !== '\n' && characters[index] !== '\r') {
      characters[index] = ' ';
    }
  }
}

export function stripCSharpTriviaAndLiterals(source) {
  const characters = [...source];
  const skipLineComment = (start) => {
    let cursor = start + 2;
    while (cursor < characters.length && characters[cursor] !== '\n') {
      cursor += 1;
    }
    return cursor;
  };
  const skipBlockComment = (start) => {
    let cursor = start + 2;
    while (
      cursor < characters.length
      && !(characters[cursor] === '*' && characters[cursor + 1] === '/')
    ) {
      cursor += 1;
    }
    return Math.min(cursor + 2, characters.length);
  };
  const skipCharacterLiteral = (start) => {
    let cursor = start + 1;
    while (cursor < characters.length) {
      if (characters[cursor] === '\\') {
        cursor += 2;
        continue;
      }
      cursor += 1;
      if (characters[cursor - 1] === '\'') {
        break;
      }
    }
    return cursor;
  };

  function skipStringLiteral(start) {
    let quoteCount = 1;
    while (characters[start + quoteCount] === '"') {
      quoteCount += 1;
    }
    if (quoteCount >= 3) {
      let cursor = start + quoteCount;
      while (cursor < characters.length) {
        let closingQuoteCount = 0;
        while (characters[cursor + closingQuoteCount] === '"') {
          closingQuoteCount += 1;
        }
        if (closingQuoteCount >= quoteCount) {
          return cursor + quoteCount;
        }
        cursor += 1;
      }
      return characters.length;
    }

    const isVerbatim = characters[start - 1] === '@' || characters[start - 2] === '@';
    const isInterpolated = characters[start - 1] === '$'
      || (characters[start - 1] === '@' && characters[start - 2] === '$')
      || (characters[start - 1] === '$' && characters[start - 2] === '@');
    let interpolationDepth = 0;
    let cursor = start + 1;

    while (cursor < characters.length) {
      if (interpolationDepth > 0) {
        if (characters[cursor] === '/' && characters[cursor + 1] === '/') {
          cursor = skipLineComment(cursor);
          continue;
        }
        if (characters[cursor] === '/' && characters[cursor + 1] === '*') {
          cursor = skipBlockComment(cursor);
          continue;
        }
        if (characters[cursor] === '\'') {
          cursor = skipCharacterLiteral(cursor);
          continue;
        }
        if (characters[cursor] === '"') {
          cursor = skipStringLiteral(cursor);
          continue;
        }
        if (characters[cursor] === '{') {
          interpolationDepth += 1;
        } else if (characters[cursor] === '}') {
          interpolationDepth -= 1;
        }
        cursor += 1;
        continue;
      }

      if (isVerbatim && characters[cursor] === '"' && characters[cursor + 1] === '"') {
        cursor += 2;
        continue;
      }
      if (!isVerbatim && characters[cursor] === '\\') {
        cursor += 2;
        continue;
      }
      if (isInterpolated && characters[cursor] === '{' && characters[cursor + 1] === '{') {
        cursor += 2;
        continue;
      }
      if (isInterpolated && characters[cursor] === '{') {
        interpolationDepth = 1;
        cursor += 1;
        continue;
      }
      cursor += 1;
      if (characters[cursor - 1] === '"') {
        break;
      }
    }
    return cursor;
  }

  let index = 0;
  while (index < characters.length) {
    const start = index;
    if (characters[index] === '/' && characters[index + 1] === '/') {
      index = skipLineComment(index);
    } else if (characters[index] === '/' && characters[index + 1] === '*') {
      index = skipBlockComment(index);
    } else if (characters[index] === '\'') {
      index = skipCharacterLiteral(index);
    } else if (characters[index] === '"') {
      index = skipStringLiteral(index);
    } else {
      index += 1;
      continue;
    }
    replaceRangeWithSpaces(characters, start, index);
  }

  return characters.join('');
}

function decodeCSharpUnicodeEscapes(source) {
  return source.replace(
    /\\(?:u([0-9A-Fa-f]{4})|U([0-9A-Fa-f]{8}))/g,
    (match, shortCodePoint, longCodePoint) => {
      const hexadecimal = shortCodePoint ?? longCodePoint;
      const codePoint = Number.parseInt(hexadecimal, 16);
      return codePoint <= 0x10FFFF ? String.fromCodePoint(codePoint) : match;
    },
  );
}

export function findCSharpDeclarations(source) {
  const code = decodeCSharpUnicodeEscapes(stripCSharpTriviaAndLiterals(source));
  const matches = code.matchAll(
    /\b((?:(?:public|internal|protected|private|abstract|sealed|static|partial|new|file|unsafe)\s+)*)(?:class|record(?!\s+struct\b)(?:\s+class)?)\s+(@?[\p{L}\p{Nl}_][\p{L}\p{Nl}\p{Nd}\p{Pc}\p{Mn}\p{Mc}\p{Cf}]*)/gu,
  );
  return [...matches].map((match) => ({
    name: match[2].replace(/^@/u, ''),
    isPartial: /(?:^|\s)partial(?:\s|$)/u.test(match[1]),
  }));
}

export function findCSharpClasses(source) {
  return findCSharpDeclarations(source).map((declaration) => declaration.name);
}

export function findTypeScriptClasses(source, fileName = 'source.ts') {
  const scriptKind = fileName.toLowerCase().endsWith('.tsx')
    ? ts.ScriptKind.TSX
    : ts.ScriptKind.TS;
  const sourceFile = ts.createSourceFile(
    fileName,
    source,
    ts.ScriptTarget.Latest,
    true,
    scriptKind,
  );
  const classes = [];

  function visit(node) {
    if (ts.isClassDeclaration(node) || ts.isClassExpression(node)) {
      if (node.name?.text) {
        classes.push(node.name.text);
      } else {
        const position = sourceFile.getLineAndCharacterOfPosition(node.getStart(sourceFile));
        classes.push(`<anonymous@${position.line + 1}:${position.character + 1}>`);
      }
    }
    ts.forEachChild(node, visit);
  }

  visit(sourceFile);
  return classes;
}

function normalizeTypeScriptFileIdentity(value) {
  return value
    .normalize('NFC')
    .replace(/[.-]/gu, '')
    .toLowerCase();
}

export function findFileNameMismatches(path, classes) {
  const extension = extname(path).toLowerCase();
  const fileName = basename(path);
  const declarationSuffix = ['.d.ts', '.d.mts', '.d.cts']
    .find((suffix) => fileName.toLowerCase().endsWith(suffix));
  const fileStem = declarationSuffix
    ? fileName.slice(0, -declarationSuffix.length)
    : basename(path, extension);

  if (extension === '.cs') {
    const normalizedFileStem = fileStem.normalize('NFC');
    return classes.filter((className) => className.normalize('NFC') !== normalizedFileStem);
  }

  const normalizedFileStem = normalizeTypeScriptFileIdentity(fileStem);
  return classes.filter(
    (className) => className.startsWith('<anonymous@')
      || normalizeTypeScriptFileIdentity(className) !== normalizedFileStem,
  );
}

function isGenerated(path, source) {
  const lowerName = basename(path).toLowerCase();
  return lowerName.endsWith('.g.cs')
    || lowerName.endsWith('.generated.cs')
    || lowerName.endsWith('.designer.cs')
    || source.slice(0, 512).toLowerCase().includes('<auto-generated');
}

function isTypeScriptSource(path) {
  const lowerName = basename(path).toLowerCase();
  return ['.ts', '.tsx', '.mts', '.cts'].some((extension) => lowerName.endsWith(extension));
}

export function collectClassFileViolations(repositoryRoot) {
  const roots = [
    join(repositoryRoot, 'API'),
    join(repositoryRoot, 'FRONT', 'AmusementPark'),
  ];
  const violations = [];

  for (const root of roots) {
    for (const path of walkSourceFiles(root)) {
      const extension = extname(path).toLowerCase();
      const isCSharpSource = extension === '.cs';
      const isTypeScriptProjectSource = isTypeScriptSource(path);
      if (!isCSharpSource && !isTypeScriptProjectSource) {
        continue;
      }

      const source = readFileSync(path, 'utf8');
      const canContainClass = isCSharpSource
        ? /\b(?:class|record)\b/u.test(source)
        : /\bclass\b/u.test(source);
      if (isGenerated(path, source) || !canContainClass) {
        continue;
      }

      const csharpDeclarations = isCSharpSource
        ? findCSharpDeclarations(source)
        : [];
      const classes = isCSharpSource
        ? csharpDeclarations.map((declaration) => declaration.name)
        : findTypeScriptClasses(source, path);
      const partialClasses = csharpDeclarations
        .filter((declaration) => declaration.isPartial)
        .map((declaration) => declaration.name);
      const filenameMismatches = findFileNameMismatches(path, classes);
      if (classes.length > 1 || filenameMismatches.length > 0 || partialClasses.length > 0) {
        violations.push({
          path: relative(repositoryRoot, path).replaceAll('\\', '/'),
          classes,
          filenameMismatches,
          partialClasses,
        });
      }
    }
  }

  return violations.sort((left, right) => left.path.localeCompare(right.path, 'en'));
}

function classCounts(classes) {
  const counts = new Map();
  for (const className of classes) {
    counts.set(className, (counts.get(className) ?? 0) + 1);
  }
  return counts;
}

export function findNewOrWorsenedViolations(currentViolations, baselineViolations) {
  const baselineByPath = new Map(baselineViolations.map((violation) => [violation.path, violation]));

  return currentViolations.filter((current) => {
    const baseline = baselineByPath.get(current.path);
    if (!baseline || current.classes.length > baseline.classes.length) {
      return true;
    }

    const allowedCounts = classCounts(baseline.classes);
    const currentCounts = classCounts(current.classes);
    const hasNewClass = [...currentCounts].some(
      ([className, count]) => count > (allowedCounts.get(className) ?? 0),
    );
    const allowedMismatches = classCounts(baseline.filenameMismatches ?? []);
    const currentMismatches = classCounts(current.filenameMismatches ?? []);
    const hasNewFilenameMismatch = [...currentMismatches].some(
      ([className, count]) => count > (allowedMismatches.get(className) ?? 0),
    );
    const allowedPartialClasses = classCounts(baseline.partialClasses ?? []);
    const currentPartialClasses = classCounts(current.partialClasses ?? []);
    const hasNewPartialClass = [...currentPartialClasses].some(
      ([className, count]) => count > (allowedPartialClasses.get(className) ?? 0),
    );
    return hasNewClass || hasNewFilenameMismatch || hasNewPartialClass;
  });
}

export function findStaleBaselineEntries(currentViolations, baselineViolations) {
  const currentByPath = new Map(currentViolations.map((violation) => [violation.path, violation]));

  return baselineViolations.filter((baseline) => {
    const current = currentByPath.get(baseline.path);
    if (!current || current.classes.length !== baseline.classes.length) {
      return true;
    }

    const baselineCounts = classCounts(baseline.classes);
    const currentCounts = classCounts(current.classes);
    const classesChanged = [...baselineCounts].some(
      ([className, count]) => count !== (currentCounts.get(className) ?? 0),
    );
    if (classesChanged) {
      return true;
    }

    const baselineMismatches = classCounts(baseline.filenameMismatches ?? []);
    const currentMismatches = classCounts(current.filenameMismatches ?? []);
    const filenameMismatchesChanged = baselineMismatches.size !== currentMismatches.size
      || [...baselineMismatches].some(
        ([className, count]) => count !== (currentMismatches.get(className) ?? 0),
      );
    if (filenameMismatchesChanged) {
      return true;
    }

    const baselinePartialClasses = classCounts(baseline.partialClasses ?? []);
    const currentPartialClasses = classCounts(current.partialClasses ?? []);
    return baselinePartialClasses.size !== currentPartialClasses.size
      || [...baselinePartialClasses].some(
        ([className, count]) => count !== (currentPartialClasses.get(className) ?? 0),
      );
  });
}

export function summarizeViolations(violations) {
  return violations.reduce(
    (summary, violation) => {
      if (violation.path.endsWith('.cs')) {
        summary.csharp += 1;
      } else {
        summary.typescript += 1;
      }
      if (violation.classes.length > 1) {
        summary.multiClassFiles += 1;
      }
      if ((violation.filenameMismatches ?? []).length > 0) {
        summary.filenameMismatchFiles += 1;
      }
      if ((violation.partialClasses ?? []).length > 0) {
        summary.partialClassFiles += 1;
      }
      return summary;
    },
    {
      csharp: 0,
      typescript: 0,
      multiClassFiles: 0,
      filenameMismatchFiles: 0,
      partialClassFiles: 0,
    },
  );
}
