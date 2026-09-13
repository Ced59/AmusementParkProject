import { signal } from '@angular/core';

export class FakeParkItemVideoStateFacade {
  readonly state = signal({ kind: 'loading' }).asReadonly();
  readonly item = signal(null).asReadonly();
  readonly park = signal(null).asReadonly();
  readonly itemImageId = signal(null).asReadonly();
  readonly parkImageId = signal(null).asReadonly();
  readonly video = signal(null).asReadonly();
  readonly rawVideo = signal(null).asReadonly();
  readonly previousVideo = signal(null).asReadonly();
  readonly nextVideo = signal(null).asReadonly();
  readonly languages: string[] = [];
  readonly loads: Array<{
    itemId: string;
    videoId: string;
  }> = [];

  setCurrentLanguage(language: string): void {
    this.languages.push(language);
  }

  loadItemVideo(itemId: string, videoId: string): void {
    this.loads.push({ itemId, videoId });
  }
}
