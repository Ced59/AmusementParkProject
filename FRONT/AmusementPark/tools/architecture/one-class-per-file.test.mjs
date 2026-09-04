import assert from 'node:assert/strict';
import { mkdirSync, mkdtempSync, rmSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import test from 'node:test';
import {
  collectClassFileViolations,
  findCSharpClasses,
  findCSharpDeclarations,
  findFileNameMismatches,
  findNewOrWorsenedViolations,
  findStaleBaselineEntries,
  findTypeScriptClasses,
} from './one-class-per-file.mjs';

test('collectClassFileViolations scans reference records and hand-written declaration files', () => {
  const repositoryRoot = mkdtempSync(join(tmpdir(), 'one-class-per-file-'));
  try {
    const apiDirectory = join(repositoryRoot, 'API', 'Example');
    const frontendDirectory = join(repositoryRoot, 'FRONT', 'AmusementPark');
    mkdirSync(apiDirectory, { recursive: true });
    mkdirSync(frontendDirectory, { recursive: true });
    writeFileSync(
      join(apiDirectory, 'GroupedRecords.cs'),
      'public sealed record FirstRecord;\npublic sealed record SecondRecord;\n',
      'utf8',
    );
    const declarationDirectory = join(frontendDirectory, 'types');
    mkdirSync(declarationDirectory, { recursive: true });
    writeFileSync(
      join(declarationDirectory, 'hand-written.d.ts'),
      'declare class FirstDeclaration {}\ndeclare class SecondDeclaration {}\n',
      'utf8',
    );

    assert.deepEqual(collectClassFileViolations(repositoryRoot), [
      {
        path: 'API/Example/GroupedRecords.cs',
        classes: ['FirstRecord', 'SecondRecord'],
        filenameMismatches: ['FirstRecord', 'SecondRecord'],
        partialClasses: [],
      },
      {
        path: 'FRONT/AmusementPark/types/hand-written.d.ts',
        classes: ['FirstDeclaration', 'SecondDeclaration'],
        filenameMismatches: ['FirstDeclaration', 'SecondDeclaration'],
        partialClasses: [],
      },
    ]);
  } finally {
    rmSync(repositoryRoot, { recursive: true, force: true });
  }
});

test('collectClassFileViolations scans every supported TypeScript source extension', () => {
  const repositoryRoot = mkdtempSync(join(tmpdir(), 'one-class-per-file-typescript-'));
  try {
    mkdirSync(join(repositoryRoot, 'API'), { recursive: true });
    const frontendDirectory = join(repositoryRoot, 'FRONT', 'AmusementPark');
    mkdirSync(frontendDirectory, { recursive: true });
    const extensions = ['tsx', 'mts', 'cts', 'd.mts', 'd.cts'];
    for (const extension of extensions) {
      writeFileSync(
        join(frontendDirectory, `grouped.${extension}`),
        'export class First {}\nexport class Second {}\n',
        'utf8',
      );
    }

    assert.deepEqual(
      collectClassFileViolations(repositoryRoot).map((violation) => violation.path),
      extensions
        .map((extension) => `FRONT/AmusementPark/grouped.${extension}`)
        .sort((left, right) => left.localeCompare(right, 'en')),
    );
  } finally {
    rmSync(repositoryRoot, { recursive: true, force: true });
  }
});

test('findCSharpDeclarations normalizes verbatim identifiers and detects partial classes', () => {
  const source = `
    public sealed class Normal {}
    internal partial class @event {}
    public sealed record @class;
    public sealed class \\u0046oo {}
    public sealed record \\U00000042ar;
    public sealed class Ⅳ {}
    public sealed class Café {}
    public sealed class Aा {}
    public sealed class A‌ {}
    public sealed class A‿1 {}
  `;

  assert.deepEqual(findCSharpDeclarations(source), [
    { name: 'Normal', isPartial: false },
    { name: 'event', isPartial: true },
    { name: 'class', isPartial: false },
    { name: 'Foo', isPartial: false },
    { name: 'Bar', isPartial: false },
    { name: 'Ⅳ', isPartial: false },
    { name: 'Café', isPartial: false },
    { name: 'Aा', isPartial: false },
    { name: 'A‌', isPartial: false },
    { name: 'A‿1', isPartial: false },
  ]);
});

test('findCSharpClasses ignores comments and every common string literal', () => {
  const source = `
    // class LineComment {}
    /* class BlockComment {} */
    const string Regular = "class RegularString {}";
    const string Verbatim = @"class VerbatimString {}";
    const string Interpolated = $"class InterpolatedString {value}";
    const string NestedInterpolation = $"{Format("class NestedString {}")}";
    const string Raw = """class RawString {}""";
    public sealed class RealClass {}
  `;

  assert.deepEqual(findCSharpClasses(source), ['RealClass']);
});

test('findCSharpClasses includes nested and implicit reference record declarations', () => {
  const source = `
    public class Outer {
      private class Nested {}
    }
    public sealed record class Recorded;
    public sealed record ImplicitRecord;
    public readonly record struct ValueRecord;
  `;

  assert.deepEqual(findCSharpClasses(source), ['Outer', 'Nested', 'Recorded', 'ImplicitRecord']);
});

test('findFileNameMismatches respects C# and Angular filename conventions', () => {
  assert.deepEqual(findFileNameMismatches('RankingSnapshotHeader.cs', ['RankingSnapshotHeader']), []);
  assert.deepEqual(findFileNameMismatches('Café.cs', ['Café']), []);
  assert.deepEqual(
    findFileNameMismatches('public-rating-state.facade.ts', ['PublicRatingStateFacade']),
    [],
  );
  assert.deepEqual(findFileNameMismatches('Declaration.d.ts', ['Declaration']), []);
  assert.deepEqual(findFileNameMismatches('Declaration.d.mts', ['Declaration']), []);
  assert.deepEqual(findFileNameMismatches('Declaration.d.cts', ['Declaration']), []);
  assert.deepEqual(findFileNameMismatches('$foo.ts', ['$Foo']), []);
  assert.deepEqual(findFileNameMismatches('foo_bar.ts', ['Foo_Bar']), []);
  assert.deepEqual(findFileNameMismatches('foo.ts', ['$Foo']), ['$Foo']);
  assert.deepEqual(findFileNameMismatches('foo-bar.ts', ['Foo_Bar']), ['Foo_Bar']);
  assert.deepEqual(
    findFileNameMismatches('grouped-models.cs', ['FirstModel', 'SecondModel']),
    ['FirstModel', 'SecondModel'],
  );
  assert.deepEqual(
    findFileNameMismatches('anonymous.ts', ['<anonymous@1:1>']),
    ['<anonymous@1:1>'],
  );
});

test('findTypeScriptClasses uses the TypeScript syntax tree', () => {
  const source = `
    const text = 'class NotAClass {}';
    export class First {}
    const anonymous = class {};
    class Container { static Nested = class NamedNested {}; }
  `;

  assert.deepEqual(findTypeScriptClasses(source), [
    'First',
    '<anonymous@4:23>',
    'Container',
    'NamedNested',
  ]);
});

test('findTypeScriptClasses parses TSX with the matching script kind', () => {
  const source = 'export class Card { render() { return <section>class Hidden {}</section>; } }';

  assert.deepEqual(findTypeScriptClasses(source, 'card.tsx'), ['Card']);
});

test('findNewOrWorsenedViolations does not classify removed debt as a regression', () => {
  const baseline = [{ path: 'Example.cs', classes: ['First', 'Second', 'Third'] }];
  const current = [{ path: 'Example.cs', classes: ['First', 'Second'] }];

  assert.deepEqual(findNewOrWorsenedViolations(current, baseline), []);
});

test('findStaleBaselineEntries requires the inventory to shrink with the code', () => {
  const baseline = [{ path: 'Example.cs', classes: ['First', 'Second', 'Third'] }];
  const current = [{ path: 'Example.cs', classes: ['First', 'Second'] }];

  assert.deepEqual(findStaleBaselineEntries(current, baseline), baseline);
});

test('findNewOrWorsenedViolations rejects new files and new colocated classes', () => {
  const baseline = [{ path: 'Example.cs', classes: ['First', 'Second'] }];
  const current = [
    { path: 'Example.cs', classes: ['First', 'Replacement'] },
    { path: 'New.ts', classes: ['NewFirst', 'NewSecond'] },
  ];

  assert.deepEqual(findNewOrWorsenedViolations(current, baseline), current);
});
