import { RobotHtmlPreparationResult } from '../../app/core/ssr/robot-html-optimizer';
import { prepareFacebookHtmlResponse } from './facebook-html-response';

describe('Facebook final HTML response', () => {
  const requestUrl: string = '/fr/park/park-1/test?facebook-image=image-1';
  const publicUrl: string = `https://amusement-parks.fun${requestUrl}`;

  it.each([
    ['a cold SSR render', '<p>Rendered park content '.repeat(40)],
    ['an SSR cache hit', '<p>Cached park content '.repeat(40)],
    ['a stale SSR cache hit', '<p>Stale park content '.repeat(40)],
  ])('applies the deterministic override to %s after robot optimization', (
    _responseMode: string,
    bodyContent: string,
  ) => {
    const html: string = createSeoReadyHtml(bodyContent);

    const result: RobotHtmlPreparationResult = prepareFacebookHtmlResponse(
      html,
      requestUrl,
      publicUrl,
      '123456789012345',
      {
        allowRobotNoJsOptimization: true,
        robotNoJsHtmlEnabled: true,
        isRobotRequest: true,
      },
    );

    expect(result.robotHtmlStatus).toBe('no-js');
    expect(result.html).not.toContain('<script');
    expect(result.html.match(/property="fb:app_id"/g)).toHaveLength(1);
    expect(result.html.match(/property="og:image"/g)).toHaveLength(1);
    expect(result.html.match(/property="og:image:secure_url"/g)).toHaveLength(1);
    expect(result.html.match(/name="twitter:image"/g)).toHaveLength(1);
    expect(result.html).toContain('/social-preview-v2?expectedOwnerType=PARK&amp;');
  });

  it('also applies the override to the CSR fallback without changing canonical metadata', () => {
    const html: string = createSeoReadyHtml('<p>Fallback park content '.repeat(40));

    const result: RobotHtmlPreparationResult = prepareFacebookHtmlResponse(
      html,
      requestUrl,
      publicUrl,
      '123456789012345',
      {
        allowRobotNoJsOptimization: false,
        robotNoJsHtmlEnabled: true,
        isRobotRequest: true,
      },
    );

    expect(result.robotHtmlStatus).toBe('not-allowed');
    expect(result.html).toContain(
      '<link rel="canonical" href="https://amusement-parks.fun/fr/park/park-1/test">',
    );
    expect(result.html).not.toContain(
      'rel="canonical" href="https://amusement-parks.fun/fr/park/park-1/test?',
    );
    expect(result.html).toContain('/social-preview-v2?expectedOwnerType=PARK&amp;');
  });
});

function createSeoReadyHtml(bodyContent: string): string {
  return '<!doctype html><html><head>'
    + '<title>Park title</title>'
    + '<meta name="description" content="Park description">'
    + '<link rel="canonical" href="https://amusement-parks.fun/fr/park/park-1/test">'
    + '<meta property="og:image" content="https://example.test/default-1.jpg">'
    + '<meta property="og:image" content="https://example.test/default-2.jpg">'
    + '<meta property="fb:app_id" content="old">'
    + '<meta property="fb:app_id" content="duplicate">'
    + '<script src="main.js"></script>'
    + '</head><body>'
    + bodyContent
    + '</body></html>';
}
