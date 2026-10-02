import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import { once } from 'node:events';
import { resolve } from 'node:path';
import { test } from 'node:test';
import express from 'express';
import { registerPublicAgentDiscoveryRoutes } from '../../src/server/agent-discovery/public-agent-discovery-routes.ts';

const directory = resolve('src/agent-discovery');

test('agent documents are served as machine resources before the generic text proxy, including HEAD', async () => {
  const app = express();
  registerPublicAgentDiscoveryRoutes(app, directory);
  let genericProxyCalls = 0;
  app.get('*.txt', (_request, response) => { genericProxyCalls += 1; response.sendStatus(404); });
  const server = app.listen(0, '127.0.0.1');
  await once(server, 'listening');
  try {
    const origin = `http://127.0.0.1:${server.address().port}`;
    for (const [file, mime] of [['llms.txt', 'text/plain'], ['ai-catalog.json', 'application/ai-catalog+json'], ['agent-guide.md', 'text/markdown']]) {
      const response = await fetch(`${origin}/${file}`);
      assert.equal(response.status, 200);
      assert.ok(response.headers.get('content-type').startsWith(mime));
      assert.equal(response.headers.get('x-robots-tag'), 'noindex');
      assert.match(response.headers.get('cache-control'), /must-revalidate/);
      assert.equal(await response.text(), await readFile(resolve(directory, file), 'utf8'));
      const head = await fetch(`${origin}/${file}`, { method: 'HEAD' });
      assert.equal(head.status, 200);
      assert.equal(await head.text(), '');
    }
    assert.equal(genericProxyCalls, 0);
  } finally {
    await new Promise((resolveClose) => server.close(resolveClose));
  }
});

test('the catalog references a real public skill with bounded read-only capabilities', async () => {
  const catalog = JSON.parse(await readFile(resolve(directory, 'ai-catalog.json'), 'utf8'));
  assert.equal(catalog.specVersion, '1.0');
  assert.ok(catalog.entries.length > 0);
  for (const entry of catalog.entries) {
    assert.match(entry.identifier, /^urn:air:amusement-parks\.fun:public:[a-z-]+$/);
    assert.equal(entry.type, 'text/markdown; profile="urn:air:agent-skills"');
    assert.equal(typeof entry.displayName, 'string');
    assert.ok(entry.representativeQueries.length >= 2 && entry.representativeQueries.length <= 5);
    assert.equal(new URL(entry.url).origin, 'https://amusement-parks.fun');
    assert.equal(entry.data, undefined);
    const guide = await readFile(resolve(directory, new URL(entry.url).pathname.slice(1)), 'utf8');
    assert.match(guide, /^---\nname: public-park-discovery\ndescription:/);
    assert.match(guide, /search_public_parks/);
    assert.match(guide, /anonymous request/);
  }
  const summary = await readFile(resolve(directory, 'llms.txt'), 'utf8');
  assert.match(summary, /^# AMUSEMENT-PARKS\.fun\n\n> /);
  for (const language of ['en', 'fr', 'de', 'nl', 'it', 'es', 'pt', 'pl']) {
    assert.ok(summary.includes(`https://amusement-parks.fun/${language}/parks`));
  }
});
