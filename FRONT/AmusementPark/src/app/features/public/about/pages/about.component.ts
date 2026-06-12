import { ChangeDetectionStrategy, Component, DestroyRef, OnInit } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Meta, Title } from '@angular/platform-browser';
import { RouterLink } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';

import { UiButtonDirective, UiChipComponent, UiKickerComponent, UiSectionHeaderComponent, UiSurfaceDirective } from '@ui/primitives';

@Component({
  selector: 'app-about',
  templateUrl: './about.component.html',
  styleUrl: './about.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    RouterLink,
    TranslateModule,
    UiButtonDirective,
    UiChipComponent,
    UiKickerComponent,
    UiSectionHeaderComponent,
    UiSurfaceDirective
  ]
})
export class AboutComponent implements OnInit {
  constructor(
    private readonly title: Title,
    private readonly meta: Meta,
    private readonly translateService: TranslateService,
    private readonly destroyRef: DestroyRef
  ) {
  }

  ngOnInit(): void {
    this.translateService.stream(['aboutPage.seo.title', 'aboutPage.seo.description'])
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((translations: Record<string, string>): void => {
        const seoTitle: string = translations['aboutPage.seo.title'];
        const seoDescription: string = translations['aboutPage.seo.description'];

        this.title.setTitle(seoTitle);
        this.meta.updateTag({ name: 'description', content: seoDescription });
        this.meta.updateTag({ property: 'og:title', content: seoTitle });
        this.meta.updateTag({ property: 'og:description', content: seoDescription });
        this.meta.updateTag({ name: 'twitter:title', content: seoTitle });
        this.meta.updateTag({ name: 'twitter:description', content: seoDescription });
      });
  }
}
