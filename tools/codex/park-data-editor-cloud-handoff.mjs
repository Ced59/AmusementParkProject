#!/usr/bin/env node
import {
  constants,
  lstatSync,
  readFileSync,
  statfsSync,
  unlinkSync,
  writeFileSync
} from 'node:fs';
import { basename, dirname, join, resolve } from 'node:path';
import { generateKeyPairSync, privateDecrypt, randomUUID } from 'node:crypto';

const temporaryDirectory = '/dev/shm';
const tmpfsMagic = 0x01021994;
const packagePrefix = 'APF-PDE1.';

function requireTemporaryStorage() {
  if (process.platform !== 'linux' || statfsSync(temporaryDirectory).type !== tmpfsMagic) {
    throw new Error('The cloud handoff requires Linux /dev/shm mounted as temporary memory.');
  }
}

function requirePrivateFile(filePath, expectedPrefix) {
  const absolute = resolve(filePath);
  if (dirname(absolute) !== temporaryDirectory || !basename(absolute).startsWith(expectedPrefix)) {
    throw new Error('The handoff file must be directly inside /dev/shm.');
  }

  const details = lstatSync(absolute);
  if (!details.isFile() || details.isSymbolicLink() || (details.mode & 0o077) !== 0 || details.uid !== process.getuid()) {
    throw new Error('The handoff file must be a regular owner-only file.');
  }
  return absolute;
}

async function readEncryptedPackage() {
  let input = '';
  for await (const chunk of process.stdin) {
    input += chunk;
    if (input.length > 4096) {
      throw new Error('The encrypted package is too large.');
    }
  }

  const value = input.trim();
  if (!value.startsWith(packagePrefix) || !/^APF-PDE1\.[A-Za-z0-9_-]+$/.test(value)) {
    throw new Error('Invalid encrypted package.');
  }
  return value.slice(packagePrefix.length);
}

function generate() {
  const { publicKey, privateKey } = generateKeyPairSync('rsa', {
    modulusLength: 2048,
    publicExponent: 0x10001,
    publicKeyEncoding: { type: 'spki', format: 'pem' },
    privateKeyEncoding: { type: 'pkcs8', format: 'pem' }
  });
  const privatePath = join(temporaryDirectory, `apf-pde-key-${randomUUID()}.pem`);
  writeFileSync(privatePath, privateKey, { mode: 0o600, flag: 'wx' });
  process.stdout.write(`PRIVATE_KEY_PATH=${privatePath}\n${publicKey}`);
}

async function redeem(privatePathArgument) {
  if (!privatePathArgument) {
    throw new Error('Usage: park-data-editor-cloud-handoff.mjs redeem PRIVATE_KEY_PATH < encrypted-package.txt');
  }

  const privatePath = requirePrivateFile(privatePathArgument, 'apf-pde-key-');
  const encoded = await readEncryptedPackage();
  const ciphertext = Buffer.from(encoded.replace(/-/g, '+').replace(/_/g, '/'), 'base64');
  const plaintext = privateDecrypt(
    { key: readFileSync(privatePath), padding: constants.RSA_PKCS1_OAEP_PADDING, oaepHash: 'sha256' },
    ciphertext
  );
  try {
    const token = plaintext.toString('utf8');
    if (!/^apf_pde_[0-9a-f]{32}\.[A-Za-z0-9_-]{43}$/.test(token)) {
      throw new Error('The decrypted payload is not a park data editor token.');
    }

    const tokenPath = join(temporaryDirectory, `apf-pde-session-${randomUUID()}.token`);
    writeFileSync(tokenPath, token, { mode: 0o600, flag: 'wx' });
    unlinkSync(privatePath);
    process.stdout.write(`${tokenPath}\n`);
  } finally {
    plaintext.fill(0);
  }
}

function clear(tokenPathArgument) {
  if (!tokenPathArgument) {
    throw new Error('Usage: park-data-editor-cloud-handoff.mjs clear TOKEN_PATH');
  }
  const tokenPath = requirePrivateFile(tokenPathArgument, 'apf-pde-session-');
  unlinkSync(tokenPath);
  process.stdout.write('Temporary token file removed.\n');
}

try {
  requireTemporaryStorage();
  switch (process.argv[2]) {
    case 'generate':
      generate();
      break;
    case 'redeem':
      await redeem(process.argv[3]);
      break;
    case 'clear':
      clear(process.argv[3]);
      break;
    default:
      throw new Error('Usage: park-data-editor-cloud-handoff.mjs generate|redeem|clear');
  }
} catch (error) {
  process.stderr.write(`${error instanceof Error ? error.message : 'Cloud handoff failed.'}\n`);
  process.exitCode = 1;
}
