import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import test from 'node:test';
import { captureHomeImageFixtures, homeImageFixturePlan, homeImageFixtureResponse } from '../design-v6/home-image-fixtures.mjs';

const id = '487f2b94d0ea48e1af71fd89bb915792';
const home = () => new Map([['/api/parks/home-featured?limit=3', { status: 200, bytes: Buffer.from(JSON.stringify([{ currentLogoImageId: id }])) }]]);
const local = key => ({ method: 'GET', url: 'https://apf-account-layout.invalid' + key });

test('expired home images are derived only from the exact bounded public JSON population', () => {
  assert.deepEqual(homeImageFixturePlan(home()), [64, 320, 480, 640].map(width => `/api/images/binary/${id}?width=${width}&v=${width <= 192 ? 3 : 2}`));
  const invalid = [
    new Map([['/api/me/images', [...home().values()][0]]]),
    new Map([['https://other.invalid/api/parks/home-featured', [...home().values()][0]]]),
    new Map([['/api/parks/home-featured', { status: 200, bytes: Buffer.from('[{"currentLogoImageId":"../private"}]') }]]),
    new Map([['/api/parks/home-featured', { status: 503, bytes: Buffer.from('[]') }]]),
  ];
  for (const value of invalid) assert.throws(() => homeImageFixturePlan(value));
  const many = Array.from({ length: 17 }, (_, n) => ({ currentLogoImageId: n.toString(16).padStart(32, '0') }));
  assert.throws(() => homeImageFixturePlan(new Map([['/api/parks/home-featured', { status: 200, bytes: Buffer.from(JSON.stringify(many)) }]])), /population/);
});

test('image collection uses sequential anonymous GETs before account isolation, never placeholders or mutations', async () => {
  const requests = [];
  let active = 0;
  const bytes = Buffer.from('RIFF\x04\x00\x00\x00WEBP');
  const fakeFetch = async (url, options) => {
    assert.equal(++active, 1);
    requests.push({ url: url.href, options });
    await Promise.resolve();
    active--;
    return new Response(bytes, { status: 200, headers: { 'content-type': 'image/webp' } });
  };
  const fixtures = await captureHomeImageFixtures(home(), fakeFetch, { ci: 'true', platform: 'linux' });
  assert.equal(fixtures.size, 4);
  for (const request of requests) {
    assert.equal(new URL(request.url).origin, 'https://amusement-parks.fun');
    assert.equal(request.options.method, 'GET');
    assert.equal(request.options.redirect, 'error');
    assert.deepEqual(Object.keys(request.options.headers), ['accept']);
  }
  for (const [key, record] of fixtures) {
    assert.deepEqual(homeImageFixtureResponse(local(key), fixtures).bytes, bytes);
    assert.match(record.sha256, /^[0-9a-f]{64}$/);
    for (const bad of [{ ...local(key), method: 'POST' }, { ...local(key), url: 'https://amusement-parks.fun' + key },
      local(key + '&retry=1'), local(key.replace(/width=\d+/, 'width=128')), local(key + '#fragment')]) {
      assert.equal(homeImageFixtureResponse(bad, fixtures), null);
    }
  }
  assert.equal(homeImageFixtureResponse(local(`/api/images/binary/${'f'.repeat(32)}?width=64&v=3`), fixtures), null);
});

test('unavailable, invalid or oversized image data and local execution cannot pass', async () => {
  let calls = 0;
  const never = async () => { calls++; throw new Error('must not run'); };
  await assert.rejects(captureHomeImageFixtures(home(), never, { ci: 'false', platform: 'linux' }), /isolated Linux CI/);
  await assert.rejects(captureHomeImageFixtures(home(), never, { ci: 'true', platform: 'win32' }), /isolated Linux CI/);
  assert.equal(calls, 0);
  for (const response of [new Response('{}', { headers: { 'content-type': 'application/json' } }),
    new Response('missing', { status: 404, headers: { 'content-type': 'image/webp' } }),
    new Response(Buffer.alloc(1048577), { headers: { 'content-type': 'image/webp' } })]) {
    await assert.rejects(captureHomeImageFixtures(home(), async () => response, { ci: 'true', platform: 'linux' }), /unavailable|bound/);
  }
  const root = new URL('../design-v6/capture-account-layout.mjs', import.meta.url);
  const source = readFileSync(root, 'utf8');
  assert.doesNotMatch(source, /fetch\(|nativeFetch|request\.headers|request\.postData/);
  assert.ok(source.includes("fixture.kind === 'expired' ? homeImageFixtureResponse"));
  assert.ok(source.includes('no exact local fixture; never forwarded'));
  assert.ok(source.includes('result.fixtureFailures.length || result.exceptions.length'));
  const campaign = readFileSync(new URL('../design-v6/capture-public-ssr.mjs', import.meta.url), 'utf8');
  assert.ok(campaign.includes("...accountCases.filter(value => value.kind === 'expired'), ...accountCases.filter(value => value.kind !== 'expired')"));
  assert.ok(campaign.includes('captureHomeImageFixtures(homeSsrFixtures, nativeFetch)'));
  assert.ok(campaign.includes('homeFixtures: homeSsrFixtures, homeImageFixtures'));
});
