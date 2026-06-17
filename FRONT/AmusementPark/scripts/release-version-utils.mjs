import { existsSync, readFileSync, writeFileSync } from 'node:fs';
import { resolve } from 'node:path';

const releaseVersionPattern = /^[0-9]+\.[0-9]+\.[0-9]+(?:-[0-9A-Za-z][0-9A-Za-z.-]*)?$/;

export function releaseVersionFilePath(projectRoot) {
  return resolve(projectRoot, 'release-version.json');
}

export function normalizeReleaseVersion(value) {
  const version = typeof value === 'string' ? value.trim() : '';

  if (!releaseVersionPattern.test(version)) {
    throw new Error('Release version must look like 1.2.0 or 1.2.0-rc.1.');
  }

  return version;
}

export function readConfiguredReleaseVersion(projectRoot) {
  const releaseVersion = readConfiguredReleaseDocument(projectRoot);

  if (!releaseVersion) {
    return null;
  }

  return normalizeReleaseVersion(releaseVersion.version);
}

export function readConfiguredReleaseDocument(projectRoot) {
  const filePath = releaseVersionFilePath(projectRoot);

  if (!existsSync(filePath)) {
    return null;
  }

  const content = readFileSync(filePath, 'utf8');
  return JSON.parse(content);
}

export function writeConfiguredReleaseVersion(projectRoot, version) {
  const normalizedVersion = normalizeReleaseVersion(version);
  const existingDocument = readConfiguredReleaseDocument(projectRoot) ?? {};
  const content = `${JSON.stringify({ ...existingDocument, version: normalizedVersion }, null, 2)}\n`;

  writeFileSync(releaseVersionFilePath(projectRoot), content, 'utf8');

  return normalizedVersion;
}
