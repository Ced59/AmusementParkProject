import { normalizeDeploymentVersion, shouldReloadForDeploymentVersion } from './deployment-version.service';

describe('DeploymentVersionService helpers', () => {
  it('normalizes non-empty version strings', () => {
    expect(normalizeDeploymentVersion(' 1.6.0 ')).toBe('1.6.0');
  });

  it('rejects missing or blank versions', () => {
    expect(normalizeDeploymentVersion(null)).toBeNull();
    expect(normalizeDeploymentVersion('   ')).toBeNull();
  });

  it('requires a reload when the deployed version differs from the current bundle version', () => {
    expect(shouldReloadForDeploymentVersion('1.6.0', '1.6.1')).toBeTrue();
  });

  it('does not require a reload for the same version', () => {
    expect(shouldReloadForDeploymentVersion('1.6.0', ' 1.6.0 ')).toBeFalse();
  });
});
