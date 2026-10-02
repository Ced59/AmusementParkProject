import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import { once } from 'node:events';
import { resolve } from 'node:path';
import { test } from 'node:test';
import express from 'express';
import { registerPublicTextSitemapRoutes } from '../../src/server/seo/public-text-sitemap-routes.ts';

const fileName = 'sitemap-static-fr.txt';
const directory = resolve('src/seo-discovery');

test('the text sitemap stays aligned with the canonical static-page provider and build assets', async () => {
  const content = await readFile(resolve(directory, fileName), 'utf8');
  const urls = content.trimEnd().split('\n');
  assert.ok(!content.startsWith('\uFEFF'));
  assert.equal(urls.length, 9);
  assert.equal(new Set(urls).size, urls.length);
  const provider = await readFile(resolve('../../API/AmusementPark.Application/Features/Seo/Services/StaticPagesSitemapSectionProvider.cs'), 'utf8');
  const segments = [...provider.matchAll(/new StaticSitemapPage\("([^"]+)"/g)].map((match) => match[1]);
  assert.deepEqual([...urls].sort(), segments.map((segment) => `https://amusement-parks.fun/fr/${segment}`).sort());
  for (const value of urls) {
    const url = new URL(value);
    assert.equal(url.origin, 'https://amusement-parks.fun');
    assert.equal(url.search + url.hash, '');
    assert.ok(url.pathname.startsWith('/fr/'));
  }
  const build = JSON.parse(await readFile('angular.json', 'utf8'));
  assert.ok(build.projects.AmusementPark.architect.build.options.assets.some((asset) =>
    asset.input === 'src/seo-discovery' && asset.glob === fileName && asset.output === '/'));
});

test('GET and HEAD bypass the generic key-file proxy without changing other text routes', async () => {
  const app = express();
  registerPublicTextSitemapRoutes(app, directory);
  let genericProxyCalls = 0;
  app.get('*.txt', (_request, response) => { genericProxyCalls += 1; response.sendStatus(404); });
  const server = app.listen(0, '127.0.0.1');
  await once(server, 'listening');
  try {
    const origin = `http://127.0.0.1:${server.address().port}`;
    const response = await fetch(`${origin}/${fileName}`);
    assert.equal(response.status, 200);
    assert.equal(response.headers.get('content-type'), 'text/plain; charset=utf-8');
    assert.equal(response.headers.get('x-robots-tag'), null);
    assert.equal(response.headers.get('x-accel-buffering'), 'no');
    assert.match(response.headers.get('cache-control'), /must-revalidate/);
    assert.equal(await response.text(), await readFile(resolve(directory, fileName), 'utf8'));
    const head = await fetch(`${origin}/${fileName}`, { method: 'HEAD' });
    assert.equal(head.status, 200);
    assert.equal(head.headers.get('content-type'), response.headers.get('content-type'));
    assert.equal(await head.text(), '');
    assert.equal(genericProxyCalls, 0);
    assert.equal((await fetch(`${origin}/unrelated-key.txt`)).status, 404);
    assert.equal(genericProxyCalls, 1);
  } finally {
    await new Promise((resolveClose) => server.close(resolveClose));
  }
});

test('a missing sitemap asset returns 404 instead of HTML or a successful empty response', async () => {
  const app = express();
  registerPublicTextSitemapRoutes(app, resolve('src/seo-discovery/missing'));
  app.get('*', (_request, response) => response.send('SSR fallback'));
  const server = app.listen(0, '127.0.0.1');
  await once(server, 'listening');
  try {
    const response = await fetch(`http://127.0.0.1:${server.address().port}/${fileName}`);
    assert.equal(response.status, 404);
    assert.ok(!(await response.text()).includes('SSR fallback'));
  } finally {
    await new Promise((resolveClose) => server.close(resolveClose));
  }
});
