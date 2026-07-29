import type { Mock } from 'vitest';
import { NgZone } from '@angular/core';
import {
  normalizeDeploymentVersion,
  shouldReloadForDeploymentVersion,
} from './deployment-version.service';
import { DeploymentVersionService } from './deployment-version.service';

describe('DeploymentVersionService helpers', () => {
  it('normalizes non-empty version strings', () => {
    expect(normalizeDeploymentVersion(' 1.6.0 ')).toBe('1.6.0');
  });

  it('rejects missing or blank versions', () => {
    expect(normalizeDeploymentVersion(null)).toBeNull();
    expect(normalizeDeploymentVersion('   ')).toBeNull();
  });

  it('requires a reload when the deployed version differs from the current bundle version', () => {
    expect(shouldReloadForDeploymentVersion('1.6.0', '1.6.1')).toBe(true);
  });

  it('does not require a reload for the same version', () => {
    expect(shouldReloadForDeploymentVersion('1.6.0', ' 1.6.0 ')).toBe(false);
  });

  it('delays the first version probe outside the initial critical path', () => {
    vi.useFakeTimers();

    try {
      const testDocument: Document =
        document.implementation.createHTMLDocument('test');
      const ngZone: NgZone = {
        runOutsideAngular: (callback: () => void): void => callback(),
      } as unknown as NgZone;
      const service: DeploymentVersionService = new DeploymentVersionService(
        testDocument,
        'browser' as unknown as object,
        ngZone,
      );
      const internals = service as unknown as {
        checkForDeploymentVersionChange: Mock;
      };
      vi.spyOn(
        internals,
        'checkForDeploymentVersionChange',
      ).mockResolvedValue(undefined);

      service.initialize();

      expect(internals.checkForDeploymentVersionChange).not.toHaveBeenCalled();

      vi.advanceTimersByTime(14999);
      expect(internals.checkForDeploymentVersionChange).not.toHaveBeenCalled();

      vi.advanceTimersByTime(1);
      expect(internals.checkForDeploymentVersionChange).toHaveBeenCalledTimes(
        1,
      );
    } finally {
      vi.useRealTimers();
    }
  });
});
