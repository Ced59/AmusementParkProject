import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';

describe('passport historical statistics responsive contract', () => {
  it('keeps every content grid bounded down to a narrow viewport', () => {
    const styles: string = readFileSync(resolve(
      process.cwd(),
      'src/app/features/profile/passport/pages/passport-historical-statistics-page/passport-historical-statistics-page.component.scss'
    ), 'utf8');

    expect(styles).toContain('overflow-x: clip');
    expect(styles).toContain('min-width: 0');
    expect(styles).toContain('max-width: 100%');
    expect(styles).toContain('repeat(3, minmax(0, 1fr))');
    expect(styles).toContain('@media (max-width: 620px)');
    expect(styles).toContain('grid-template-columns: 1fr');
  });
});
