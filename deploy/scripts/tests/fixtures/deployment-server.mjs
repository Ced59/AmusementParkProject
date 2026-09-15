import { createServer, request as httpRequest } from 'node:http';
import { appendFileSync, existsSync, writeFileSync } from 'node:fs';
import { hostname } from 'node:os';
import { installGracefulShutdown } from './server-graceful-shutdown.ts';

const kind = process.env.KIND;
const name = hostname();
const token = 'ci-fixture-only-token';
const identity = { kind, name, version: process.env.FIXTURE_VERSION };
const wait = (milliseconds) => new Promise((resolve) => setTimeout(resolve, milliseconds));
const send = (response, value, status = 200) => {
  response.writeHead(status, { 'Content-Type': 'application/json' });
  response.end(JSON.stringify(value));
};

const server = createServer(async (request, response) => {
  const url = new URL(request.url, 'http://fixture.test');
  if (kind === 'api') {
    const host = (request.headers.host ?? '').split(':')[0];
    if (!(process.env.AllowedHosts ?? '').split(';').includes(host)) {
      send(response, { error: 'Invalid Hostname' }, 400);
      return;
    }
    if (url.pathname === '/health' && process.env.DEPLOYMENT_GENERATION !== 'unmanaged'
        && process.env.DurableBackgroundJobs__Worker__Enabled !== 'false'
        && !existsSync('/control/canonical-api-ready')) {
      send(response, identity, 503);
    } else if (url.pathname === '/callback') {
      const result = await fetch(`${process.env.Ssr__InternalBaseUrl}/internal/cache/invalidate`, {
        method: 'POST', headers: { 'X-AmusementPark-Cache-Token': token }, body: '{}',
      });
      send(response, { api: identity, callback: await result.json(), status: result.status });
    } else if (url.pathname === '/write') {
      for await (const _chunk of request) { /* Consume the entire request once. */ }
      appendFileSync('/control/writes', `${name}\n`);
      writeFileSync('/control/started-write', name);
      while (!existsSync('/control/release-write')) {
        await wait(30);
      }
      send(response, identity);
    } else {
      send(response, identity);
    }
    return;
  }

  if (url.pathname.startsWith('/api/')) {
    const target = new URL(request.url.slice(4), process.env.SSR_API_INTERNAL_URL);
    const upstream = httpRequest(target, { method: request.method, headers: request.headers }, (result) => {
      response.writeHead(result.statusCode, result.headers);
      result.pipe(response);
    });
    upstream.on('error', () => send(response, { error: 'upstream' }, 502));
    request.pipe(upstream);
  } else if (url.pathname === '/pair') {
    const result = await fetch(`${process.env.SSR_API_INTERNAL_URL}/identity`);
    send(response, { front: identity, api: await result.json() });
  } else if (url.pathname === '/slow') {
    const key = url.searchParams.get('key');
    if (!/^[a-z]+$/.test(key)) {
      send(response, {}, 400);
      return;
    }
    writeFileSync(`/control/started-${key}`, name);
    if (key !== 'headers') {
      response.writeHead(200, { 'Content-Type': 'text/plain', 'X-Accel-Buffering': 'no' });
      response.write(`start:${name}\n`);
    }
    while (!existsSync(`/control/release-${key}`)) {
      await wait(30);
    }
    response.end(`end:${name}\n`);
  } else if (url.pathname.startsWith('/internal/')) {
    if (request.headers['x-amusementpark-cache-token'] !== token) {
      send(response, {}, 403);
      return;
    }
    appendFileSync('/control/callbacks', `${name}\n`);
    send(response, identity);
  } else {
    send(response, identity);
  }
});
server.listen(kind === 'api' ? 8080 : 4000, '0.0.0.0');
installGracefulShutdown(server, () => appendFileSync('/control/stopped', `${name}\n`), {
  on(event, handler) {
    process.on(event, () => {
      writeFileSync(`/control/signal-received-${name}`, event);
      handler();
    });
  },
  exit(code) { process.exit(code); },
});
