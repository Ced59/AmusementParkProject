import { Component, DestroyRef, Inject, OnInit, PLATFORM_ID, inject } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { PageStateComponent } from '../../shared/page-state/page-state.component';
import { SigninGoogleStateFacade } from '@features/auth/state/signin-google-state.facade';

@Component({
  selector: 'app-signin-google',
  templateUrl: './signin-google.component.html',
  styleUrls: ['./signin-google.component.scss'],
  providers: [SigninGoogleStateFacade],
  imports: [PageStateComponent]
})
export class SigninGoogleComponent implements OnInit {
  protected readonly state = this.stateFacade.state;

  private lastVisitedUrl: string | null = null;
  private readonly destroyRef: DestroyRef = inject(DestroyRef);

  constructor(
    private readonly route: ActivatedRoute,
    private readonly stateFacade: SigninGoogleStateFacade,
    @Inject(PLATFORM_ID) private readonly platformId: object
  ) {
  }

  ngOnInit(): void {
    if (isPlatformBrowser(this.platformId)) {
      this.lastVisitedUrl = localStorage.getItem('lastVisitedUrl');
    }

    this.route.queryParams.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((params: { [key: string]: unknown }) => {
      const code: string | null = typeof params['code'] === 'string' ? params['code'] : null;
      this.stateFacade.handleCallback(code, this.lastVisitedUrl);
    });
  }
}
