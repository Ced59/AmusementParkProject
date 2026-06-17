import { TestBed } from '@angular/core/testing';

import { provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { SafeVideoEmbedUrlService } from './safe-video-embed-url.service';

describe('SafeVideoEmbedUrlService', () => {
  let service: SafeVideoEmbedUrlService;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: provideCommonTestDependencies() });
    service = TestBed.inject(SafeVideoEmbedUrlService);
  });

  it('trusts known provider embed urls only', () => {
    expect(service.resolve('https://www.youtube.com/embed/abc')).not.toBeNull();
    expect(service.resolve('https://www.dailymotion.com/embed/video/abc')).not.toBeNull();
    expect(service.resolve('https://player.vimeo.com/video/123')).not.toBeNull();
    expect(service.resolve('https://www.youtube.com/watch?v=abc')).toBeNull();
    expect(service.resolve('http://www.youtube.com/embed/abc')).toBeNull();
    expect(service.resolve('javascript:alert(1)')).toBeNull();
  });
});
