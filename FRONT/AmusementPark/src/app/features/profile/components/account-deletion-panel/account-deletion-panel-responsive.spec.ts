import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';

describe('Account deletion panel responsive layout', () => {
  it('keeps long confirmations inside narrow mobile viewports', () => {
    const styles: string = readFileSync(
      resolve(
        process.cwd(),
        'src/app/features/profile/components/account-deletion-panel/account-deletion-panel.component.scss'
      ),
      'utf8'
    );

    expect(styles).toContain('min-width: 0');
    expect(styles).toContain('overflow-wrap: anywhere');
    expect(styles).toContain('@media (max-width: 640px)');
    expect(styles).toContain('grid-template-columns: minmax(0, 1fr)');
    expect(styles).toContain('width: 100%');
  });
});
