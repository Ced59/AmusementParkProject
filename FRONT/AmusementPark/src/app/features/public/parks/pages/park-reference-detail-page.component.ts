import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, ParamMap, Router } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { TranslationService } from '@app/services/translation.service';
import { ParkReferenceKind } from '../models/park-reference-detail-view.model';
import { ParkReferenceDetailStateFacade } from '../state/park-reference-detail-state.facade';
import { ParkReferenceDetailViewComponent } from '../ui/park-reference-detail-view.component';

@Component({
  selector: 'app-park-reference-detail-page',
  templateUrl: './park-reference-detail-page.component.html',
  styleUrls: ['./park-reference-detail-page.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [ParkReferenceDetailStateFacade],
  imports: [ParkReferenceDetailViewComponent]
})
export class ParkReferenceDetailPageComponent implements OnInit {
  protected readonly state = this.stateFacade.state;
  protected readonly reference = this.stateFacade.reference;
  protected readonly currentLang = signal<string>('en');

  private readonly destroyRef: DestroyRef = inject(DestroyRef);

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly translationService: TranslationService,
    private readonly stateFacade: ParkReferenceDetailStateFacade
  ) {
  }

  ngOnInit(): void {
    const initialLanguage: string = this.route.parent?.snapshot.paramMap.get('lang')
      ?? this.translationService.getCurrentLang()
      ?? 'en';

    this.currentLang.set(initialLanguage);
    this.stateFacade.setCurrentLanguage(initialLanguage);

    this.route.paramMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((params: ParamMap) => {
      const id: string | null = params.get('id');
      const kind: ParkReferenceKind = this.resolveReferenceKind();

      if (!id) {
        return;
      }

      this.stateFacade.loadReference(kind, id);
    });

    if (this.route.parent) {
      this.route.parent.paramMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((params: ParamMap) => {
        const language: string = params.get('lang') ?? 'en';
        this.currentLang.set(language);
        this.stateFacade.setCurrentLanguage(language);
      });
    }

    this.translationService.languageChanged.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((language: string) => {
      this.currentLang.set(language);
      this.stateFacade.setCurrentLanguage(language);
    });
  }

  goBack(): void {
    this.router.navigate(['/', this.currentLang(), 'parks']);
  }

  private resolveReferenceKind(): ParkReferenceKind {
    const kind: unknown = this.route.snapshot.data['referenceKind'];

    if (kind === 'founder' || kind === 'manufacturer') {
      return kind;
    }

    return 'operator';
  }
}
