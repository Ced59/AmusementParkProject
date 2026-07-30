import { ChangeDetectionStrategy, Component, Input, OnChanges, Signal, SimpleChanges, computed } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

import { CommentSummary, CommentTargetType } from '@app/models/comments/comment.models';
import { LANGUAGES, LanguageOption } from '@shared/models/localization';
import { LocalizedPluralPipe } from '@shared/pipes';
import { NaturalTextTruncatorService } from '@shared/services/text/natural-text-truncator.service';
import { findExactLocalizedText, stripHtml } from '@shared/utils/localization';
import { CommentSummaryStateFacade } from '../state/comment-summary-state.facade';

@Component({
  selector: 'app-comment-summary-link',
  templateUrl: './comment-summary-link.component.html',
  styleUrls: ['./comment-summary-link.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [CommentSummaryStateFacade],
  imports: [RouterLink, TranslateModule, LocalizedPluralPipe]
})
export class CommentSummaryLinkComponent implements OnChanges {
  @Input({ required: true }) targetType!: CommentTargetType;
  @Input({ required: true }) targetId!: string;
  @Input({ required: true }) commentsLink!: string[];
  @Input() currentLanguage: string = 'en';

  protected readonly summary: Signal<CommentSummary | null> = this.stateFacade.summary;
  protected readonly canWrite: Signal<boolean> = this.stateFacade.canWrite;
  protected readonly officialPreview: Signal<string | null> = computed(() => {
    const currentSummary: CommentSummary | null = this.summary();
    const officialBody: string | undefined = findExactLocalizedText(
      currentSummary?.officialComment?.bodies,
      this.currentLanguage
    )?.value;
    return this.textTruncator.truncate(stripHtml(officialBody), {
      maxLength: 220,
      ellipsis: '…'
    });
  });
  protected readonly currentLanguageLabel: Signal<string> = computed(() => {
    const languageCode: string = this.summary()?.languageCode ?? this.currentLanguage;
    return LANGUAGES.find(
      (language: LanguageOption): boolean => language.value === languageCode
    )?.label ?? languageCode.toUpperCase();
  });

  constructor(
    private readonly stateFacade: CommentSummaryStateFacade,
    private readonly textTruncator: NaturalTextTruncatorService
  ) {
  }

  ngOnChanges(_changes: SimpleChanges): void {
    if (this.targetType && this.targetId) {
      this.stateFacade.initializeAuthorAccess();
      this.stateFacade.load(this.targetType, this.targetId, this.currentLanguage);
    }
  }
}
