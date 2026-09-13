import { signal } from '@angular/core';

export class FakeParkVideoStateFacade {
  readonly state = signal({ kind: 'loading' }).asReadonly();
  readonly park = signal(null).asReadonly();
  readonly parkImageId = signal(null).asReadonly();
  readonly video = signal(null).asReadonly();
  readonly rawVideo = signal(null).asReadonly();
  readonly previousVideo = signal(null).asReadonly();
  readonly nextVideo = signal(null).asReadonly();
  readonly languages: string[] = [];
  readonly loads: Array<{
    parkId: string;
    videoId: string;
  }> = [];

  setCurrentLanguage(language: string): void {
    this.languages.push(language);
  }

  loadParkVideo(parkId: string, videoId: string): void {
    this.loads.push({ parkId, videoId });
  }
}
