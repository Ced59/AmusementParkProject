import assert from 'node:assert/strict';
import test from 'node:test';

import {
  evaluateBundleBudget,
  extractInitialAssetNames,
  extractStaticImportAssetNames,
} from './bundle-budget.mjs';

test('extracts unique local initial scripts and styles only', () => {
  const html = '<link rel="stylesheet" href="styles.css"><link rel="modulepreload" href="chunk-a.js">'
    + '<script src="main.js"></script><script src="https://example.com/external.js"></script>'
    + '<script src="main.js"></script>';

  assert.deepEqual(extractInitialAssetNames(html), ['styles.css', 'chunk-a.js', 'main.js']);
});

test('follows static chunk imports without treating dynamic imports as initial', () => {
  const javascript = 'import{a}from"./chunk-a.js";import"./chunk-b.js";'
    + 'const lazy=()=>import("./lazy-page.js");export{b}from"./chunk-c.js";';

  assert.deepEqual(extractStaticImportAssetNames(javascript), [
    'chunk-a.js',
    'chunk-b.js',
    'chunk-c.js',
  ]);
});

test('reports every exceeded bundle budget', () => {
  const metrics = {
    initialBytes: 101,
    largestLazyChunkBytes: 50,
    totalJavaScriptBytes: 301,
    totalStylesheetBytes: 20,
  };
  const budget = {
    maximumInitialBytes: 100,
    maximumLargestLazyChunkBytes: 60,
    maximumTotalJavaScriptBytes: 300,
    maximumTotalStylesheetBytes: 30,
  };

  assert.deepEqual(evaluateBundleBudget(metrics, budget), [
    'chargement initial: 101 octets > 100 octets',
    'JavaScript total: 301 octets > 300 octets',
  ]);
});
