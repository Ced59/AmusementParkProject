import assert from 'node:assert/strict';
import { spawnSync } from 'node:child_process';
import { existsSync, readFileSync, statSync, unlinkSync } from 'node:fs';
import { webcrypto } from 'node:crypto';
import { test } from 'node:test';
import { fileURLToPath } from 'node:url';

const scriptPath = fileURLToPath(new URL('./park-data-editor-cloud-handoff.mjs', import.meta.url));

test('cloud handoff decrypts a browser-encrypted token into owner-only temporary memory', {
  skip: process.platform !== 'linux'
}, async () => {
  const generated = spawnSync(process.execPath, [scriptPath, 'generate'], { encoding: 'utf8' });
  assert.equal(generated.status, 0, generated.stderr);
  const [pathLine, ...pemLines] = generated.stdout.trim().split('\n');
  const privatePath = pathLine.slice('PRIVATE_KEY_PATH='.length);
  const publicPem = pemLines.join('\n');
  let tokenPath;

  try {
    assert.match(privatePath, /^\/dev\/shm\/apf-pde-key-[\w-]+\.pem$/);
    assert.equal(statSync(privatePath).mode & 0o077, 0);
    const keyBytes = Buffer.from(publicPem.replace(/-----BEGIN PUBLIC KEY-----|-----END PUBLIC KEY-----|\s/g, ''), 'base64');
    const publicKey = await webcrypto.subtle.importKey('spki', keyBytes, { name: 'RSA-OAEP', hash: 'SHA-256' }, false, ['encrypt']);
    const token = `apf_pde_0123456789abcdef0123456789abcdef.${'a'.repeat(43)}`;
    const ciphertext = await webcrypto.subtle.encrypt({ name: 'RSA-OAEP' }, publicKey, new TextEncoder().encode(token));
    const encrypted = `APF-PDE1.${Buffer.from(ciphertext).toString('base64url')}`;

    const invalid = spawnSync(process.execPath, [scriptPath, 'redeem', privatePath], {
      input: 'not an encrypted package', encoding: 'utf8'
    });
    assert.notEqual(invalid.status, 0);
    assert.equal(existsSync(privatePath), true);

    const redeemed = spawnSync(process.execPath, [scriptPath, 'redeem', privatePath], {
      input: encrypted, encoding: 'utf8'
    });
    assert.equal(redeemed.status, 0, redeemed.stderr);
    tokenPath = redeemed.stdout.trim();
    assert.match(tokenPath, /^\/dev\/shm\/apf-pde-session-[\w-]+\.token$/);
    assert.equal(statSync(tokenPath).mode & 0o077, 0);
    assert.equal(readFileSync(tokenPath, 'utf8'), token);
    assert.equal(existsSync(privatePath), false);
    assert.equal(redeemed.stdout.includes(token), false);

    const cleared = spawnSync(process.execPath, [scriptPath, 'clear', tokenPath], { encoding: 'utf8' });
    assert.equal(cleared.status, 0, cleared.stderr);
    assert.equal(existsSync(tokenPath), false);
    tokenPath = undefined;
  } finally {
    if (existsSync(privatePath)) unlinkSync(privatePath);
    if (tokenPath && existsSync(tokenPath)) unlinkSync(tokenPath);
  }
});
