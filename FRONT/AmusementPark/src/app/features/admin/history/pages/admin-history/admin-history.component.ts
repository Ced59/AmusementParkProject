import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { InputText } from '@shared/ui/primitives/inputtext';
import { Tag } from '@shared/ui/primitives/tag';

import { HistoryArticleContent, HistoryDatePrecision, HistoryEntityType, HistoryEvent, HistoryEventWriteModel, HistorySourceReference } from '@app/models/history/history.models';
import { LocalizedItem } from '@app/models/shared/localized-item';
import { resolveLocalizedText } from '@shared/utils/localization/localized-text.helpers';
import { resolveHistoryEventTypeLabel } from '@features/public/history/utils/history-event-labels';
import { PARK_HISTORY_EVENT_TYPES, PARK_ITEM_HISTORY_EVENT_TYPES } from '../../models/admin-history-event-types';
import { AdminHistoryStateFacade } from '../../state/admin-history-state.facade';

const ADMIN_HISTORY_LANGUAGES: readonly string[] = ['fr', 'en', 'de', 'nl', 'it', 'es', 'pl', 'pt'];

interface AdminHistoryFiltersForm {
  entityType: FormControl<HistoryEntityType | ''>;
  ownerId: FormControl<string>;
  search: FormControl<string>;
  includeHidden: FormControl<boolean>;
}

interface AdminHistoryEventForm {
  key: FormControl<string>;
  entityType: FormControl<HistoryEntityType>;
  ownerId: FormControl<string>;
  parkId: FormControl<string>;
  parkItemId: FormControl<string>;
  contextParkId: FormControl<string>;
  year: FormControl<number>;
  month: FormControl<number | null>;
  day: FormControl<number | null>;
  datePrecision: FormControl<HistoryDatePrecision>;
  eventType: FormControl<string>;
  isMajor: FormControl<boolean>;
  isVisible: FormControl<boolean>;
  slug: FormControl<string>;
  mainImageId: FormControl<string>;
  previousName: FormControl<string>;
  newName: FormControl<string>;
  previousLogoImageId: FormControl<string>;
  newLogoImageId: FormControl<string>;
  previousOperatorId: FormControl<string>;
  newOperatorId: FormControl<string>;
  locationLabel: FormControl<string>;
  relatedParkIdsJson: FormControl<string>;
  relatedParkItemIdsJson: FormControl<string>;
  titlesJson: FormControl<string>;
  summariesJson: FormControl<string>;
  sourcesJson: FormControl<string>;
  articleJson: FormControl<string>;
}

function createLocalizedItems(preferredLanguage: string, value: string): LocalizedItem<string>[] {
  const orderedLanguages: string[] = ADMIN_HISTORY_LANGUAGES.includes(preferredLanguage)
    ? [preferredLanguage, ...ADMIN_HISTORY_LANGUAGES.filter((languageCode: string): boolean => languageCode !== preferredLanguage)]
    : [...ADMIN_HISTORY_LANGUAGES];

  return orderedLanguages.map((languageCode: string): LocalizedItem<string> => ({
    languageCode,
    value: languageCode === preferredLanguage ? value : ''
  }));
}

@Component({
  selector: 'app-admin-history',
  templateUrl: './admin-history.component.html',
  styleUrls: ['./admin-history.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [AdminHistoryStateFacade],
  imports: [
    CommonModule,
    ReactiveFormsModule,
    TranslateModule,
    InputText,
    Tag
  ]
})
export class AdminHistoryComponent implements OnInit {
  protected readonly events = this.stateFacade.events;
  protected readonly loading = this.stateFacade.loading;
  protected readonly totalRecords = this.stateFacade.totalRecords;
  protected readonly errorKey = this.stateFacade.errorKey;
  protected readonly saving = this.stateFacade.saving;
  protected readonly deleting = this.stateFacade.deleting;
  protected readonly editingEvent = signal<HistoryEvent | null>(null);
  protected readonly messageKey = signal<string | null>(null);
  protected readonly formErrorKey = signal<string | null>(null);
  protected readonly currentLang = signal<string>('en');
  protected readonly eventTypeOptions = computed(() => this.eventForm.controls.entityType.value === 'ParkItem' ? PARK_ITEM_HISTORY_EVENT_TYPES : PARK_HISTORY_EVENT_TYPES);

  protected readonly filterForm = new FormGroup<AdminHistoryFiltersForm>({
    entityType: new FormControl<HistoryEntityType | ''>('', { nonNullable: true }),
    ownerId: new FormControl<string>('', { nonNullable: true }),
    search: new FormControl<string>('', { nonNullable: true }),
    includeHidden: new FormControl<boolean>(true, { nonNullable: true })
  });

  protected readonly eventForm = new FormGroup<AdminHistoryEventForm>({
    key: new FormControl<string>('', { nonNullable: true }),
    entityType: new FormControl<HistoryEntityType>('Park', { nonNullable: true, validators: [Validators.required] }),
    ownerId: new FormControl<string>('', { nonNullable: true, validators: [Validators.required] }),
    parkId: new FormControl<string>('', { nonNullable: true }),
    parkItemId: new FormControl<string>('', { nonNullable: true }),
    contextParkId: new FormControl<string>('', { nonNullable: true }),
    year: new FormControl<number>(new Date().getFullYear(), { nonNullable: true, validators: [Validators.required, Validators.min(1)] }),
    month: new FormControl<number | null>(null),
    day: new FormControl<number | null>(null),
    datePrecision: new FormControl<HistoryDatePrecision>('Year', { nonNullable: true }),
    eventType: new FormControl<string>('Opening', { nonNullable: true, validators: [Validators.required] }),
    isMajor: new FormControl<boolean>(false, { nonNullable: true }),
    isVisible: new FormControl<boolean>(true, { nonNullable: true }),
    slug: new FormControl<string>('', { nonNullable: true }),
    mainImageId: new FormControl<string>('', { nonNullable: true }),
    previousName: new FormControl<string>('', { nonNullable: true }),
    newName: new FormControl<string>('', { nonNullable: true }),
    previousLogoImageId: new FormControl<string>('', { nonNullable: true }),
    newLogoImageId: new FormControl<string>('', { nonNullable: true }),
    previousOperatorId: new FormControl<string>('', { nonNullable: true }),
    newOperatorId: new FormControl<string>('', { nonNullable: true }),
    locationLabel: new FormControl<string>('', { nonNullable: true }),
    relatedParkIdsJson: new FormControl<string>('[]', { nonNullable: true }),
    relatedParkItemIdsJson: new FormControl<string>('[]', { nonNullable: true }),
    titlesJson: new FormControl<string>(JSON.stringify(createLocalizedItems('en', ''), null, 2), { nonNullable: true }),
    summariesJson: new FormControl<string>(JSON.stringify(createLocalizedItems('en', ''), null, 2), { nonNullable: true }),
    sourcesJson: new FormControl<string>('[]', { nonNullable: true }),
    articleJson: new FormControl<string>('null', { nonNullable: true })
  });

  private readonly destroyRef: DestroyRef = inject(DestroyRef);

  constructor(
    private readonly stateFacade: AdminHistoryStateFacade,
    private readonly route: ActivatedRoute
  ) {
  }

  ngOnInit(): void {
    this.currentLang.set(this.route.root.firstChild?.snapshot.params['lang'] ?? this.route.snapshot.params['lang'] ?? 'en');
    this.load();
  }

  protected load(): void {
    const filters = this.filterForm.getRawValue();
    this.stateFacade.load({
      page: 1,
      size: 50,
      entityType: filters.entityType || null,
      ownerId: this.normalizeOptionalText(filters.ownerId),
      search: this.normalizeOptionalText(filters.search),
      includeHidden: filters.includeHidden
    });
  }

  protected resetFilters(): void {
    this.filterForm.setValue({
      entityType: '',
      ownerId: '',
      search: '',
      includeHidden: true
    });
    this.load();
  }

  protected startCreate(entityType: HistoryEntityType = 'Park'): void {
    this.editingEvent.set(null);
    this.messageKey.set(null);
    this.formErrorKey.set(null);
    this.eventForm.reset({
      key: '',
      entityType,
      ownerId: '',
      parkId: '',
      parkItemId: '',
      contextParkId: '',
      year: new Date().getFullYear(),
      month: null,
      day: null,
      datePrecision: 'Year',
      eventType: entityType === 'ParkItem' ? 'Opening' : 'Opening',
      isMajor: false,
      isVisible: true,
      slug: '',
      mainImageId: '',
      previousName: '',
      newName: '',
      previousLogoImageId: '',
      newLogoImageId: '',
      previousOperatorId: '',
      newOperatorId: '',
      locationLabel: '',
      relatedParkIdsJson: '[]',
      relatedParkItemIdsJson: '[]',
      titlesJson: JSON.stringify(createLocalizedItems(this.currentLang(), ''), null, 2),
      summariesJson: JSON.stringify(createLocalizedItems(this.currentLang(), ''), null, 2),
      sourcesJson: '[]',
      articleJson: 'null'
    });
  }

  protected edit(event: HistoryEvent): void {
    this.editingEvent.set(event);
    this.messageKey.set(null);
    this.formErrorKey.set(null);
    this.eventForm.reset({
      key: event.key ?? '',
      entityType: event.entityType === 'ParkItem' ? 'ParkItem' : 'Park',
      ownerId: event.ownerId ?? '',
      parkId: event.parkId ?? '',
      parkItemId: event.parkItemId ?? '',
      contextParkId: event.contextParkId ?? '',
      year: event.year,
      month: event.month ?? null,
      day: event.day ?? null,
      datePrecision: this.normalizePrecision(event.datePrecision),
      eventType: event.eventType ?? 'Opening',
      isMajor: !!event.isMajor,
      isVisible: !!event.isVisible,
      slug: event.slug ?? '',
      mainImageId: event.mainImageId ?? '',
      previousName: event.previousName ?? '',
      newName: event.newName ?? '',
      previousLogoImageId: event.previousLogoImageId ?? '',
      newLogoImageId: event.newLogoImageId ?? '',
      previousOperatorId: event.previousOperatorId ?? '',
      newOperatorId: event.newOperatorId ?? '',
      locationLabel: event.locationLabel ?? '',
      relatedParkIdsJson: JSON.stringify(event.relatedParkIds ?? [], null, 2),
      relatedParkItemIdsJson: JSON.stringify(event.relatedParkItemIds ?? [], null, 2),
      titlesJson: JSON.stringify(event.titles ?? [], null, 2),
      summariesJson: JSON.stringify(event.summaries ?? [], null, 2),
      sourcesJson: JSON.stringify(event.sources ?? [], null, 2),
      articleJson: JSON.stringify(event.article ?? null, null, 2)
    });
  }

  protected onEntityTypeChanged(): void {
    const entityType: HistoryEntityType = this.eventForm.controls.entityType.value;
    const eventTypes: readonly string[] = entityType === 'ParkItem' ? PARK_ITEM_HISTORY_EVENT_TYPES : PARK_HISTORY_EVENT_TYPES;
    if (!eventTypes.includes(this.eventForm.controls.eventType.value)) {
      this.eventForm.controls.eventType.setValue(eventTypes[0] ?? 'Other');
    }
  }

  protected insertArticleTemplate(): void {
    const title: string = this.resolveCurrentTitle();
    const article: HistoryArticleContent = {
      slug: this.eventForm.controls.slug.value || null,
      titles: createLocalizedItems(this.currentLang(), title || 'Article historique'),
      subtitles: createLocalizedItems(this.currentLang(), ''),
      summaries: createLocalizedItems(this.currentLang(), ''),
      mainImageId: this.normalizeOptionalText(this.eventForm.controls.mainImageId.value),
      blocks: [
        {
          id: 'intro',
          type: 'Paragraph',
          sortOrder: 0,
          headingLevel: null,
          texts: createLocalizedItems(this.currentLang(), ''),
          imageId: null,
          imageIds: [],
          captions: []
        }
      ],
      sources: [],
      isPublished: true
    };
    this.eventForm.controls.isMajor.setValue(true);
    this.eventForm.controls.articleJson.setValue(JSON.stringify(article, null, 2));
  }

  protected submit(): void {
    this.messageKey.set(null);
    this.formErrorKey.set(null);

    if (this.eventForm.invalid) {
      this.eventForm.markAllAsTouched();
      this.formErrorKey.set('admin.history.errors.required');
      return;
    }

    const request: HistoryEventWriteModel | null = this.buildRequest();
    if (request === null) {
      this.formErrorKey.set('admin.history.errors.invalidJson');
      return;
    }

    this.stateFacade.save(this.editingEvent()?.id ?? null, request)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (): void => {
          this.messageKey.set('admin.history.messages.saved');
          this.startCreate(request.entityType);
        },
        error: (): void => {
          this.formErrorKey.set('admin.history.errors.saveFailed');
        }
      });
  }

  protected delete(event: HistoryEvent): void {
    if (!event.id) {
      return;
    }

    this.stateFacade.delete(event.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (): void => {
          this.messageKey.set('admin.history.messages.deleted');
          if (this.editingEvent()?.id === event.id) {
            this.startCreate(event.entityType === 'ParkItem' ? 'ParkItem' : 'Park');
          }
        },
        error: (): void => {
          this.formErrorKey.set('admin.history.errors.deleteFailed');
        }
      });
  }

  protected title(event: HistoryEvent): string {
    return resolveLocalizedText(event.titles, this.currentLang(), event.key || event.eventType);
  }

  protected summary(event: HistoryEvent): string {
    return resolveLocalizedText(event.summaries, this.currentLang(), '');
  }

  protected eventTypeLabel(eventType: string): string {
    return resolveHistoryEventTypeLabel(eventType, this.currentLang());
  }

  protected eventTrackBy(_: number, event: HistoryEvent): string {
    return event.id ?? `${event.ownerId}-${event.key}`;
  }

  protected editingTitleKey(): string {
    return this.editingEvent() ? 'admin.history.editor.updateTitle' : 'admin.history.editor.createTitle';
  }

  private buildRequest(): HistoryEventWriteModel | null {
    const formValue = this.eventForm.getRawValue();
    const titles: LocalizedItem<string>[] | null = this.parseJsonArray<LocalizedItem<string>>(formValue.titlesJson);
    const summaries: LocalizedItem<string>[] | null = this.parseJsonArray<LocalizedItem<string>>(formValue.summariesJson);
    const sources: HistorySourceReference[] | null = this.parseJsonArray<HistorySourceReference>(formValue.sourcesJson);
    const relatedParkIds: string[] | null = this.parseJsonArray<string>(formValue.relatedParkIdsJson);
    const relatedParkItemIds: string[] | null = this.parseJsonArray<string>(formValue.relatedParkItemIdsJson);
    const article: HistoryArticleContent | null | undefined = this.parseJsonNullable<HistoryArticleContent>(formValue.articleJson);

    if (titles === null || summaries === null || sources === null || relatedParkIds === null || relatedParkItemIds === null || article === undefined) {
      return null;
    }

    return {
      id: this.editingEvent()?.id ?? null,
      key: this.normalizeOptionalText(formValue.key),
      entityType: formValue.entityType,
      ownerId: formValue.ownerId.trim(),
      parkId: this.normalizeOptionalText(formValue.parkId),
      parkItemId: this.normalizeOptionalText(formValue.parkItemId),
      contextParkId: this.normalizeOptionalText(formValue.contextParkId),
      year: Number(formValue.year),
      month: formValue.month === null ? null : Number(formValue.month),
      day: formValue.day === null ? null : Number(formValue.day),
      datePrecision: formValue.datePrecision,
      eventType: formValue.eventType,
      isMajor: formValue.isMajor,
      isVisible: formValue.isVisible,
      slug: this.normalizeOptionalText(formValue.slug),
      titles,
      summaries,
      mainImageId: this.normalizeOptionalText(formValue.mainImageId),
      previousName: this.normalizeOptionalText(formValue.previousName),
      newName: this.normalizeOptionalText(formValue.newName),
      previousLogoImageId: this.normalizeOptionalText(formValue.previousLogoImageId),
      newLogoImageId: this.normalizeOptionalText(formValue.newLogoImageId),
      previousOperatorId: this.normalizeOptionalText(formValue.previousOperatorId),
      newOperatorId: this.normalizeOptionalText(formValue.newOperatorId),
      locationLabel: this.normalizeOptionalText(formValue.locationLabel),
      relatedParkIds,
      relatedParkItemIds,
      sources,
      article: formValue.isMajor ? article ?? null : null
    };
  }

  private parseJsonArray<T>(value: string): T[] | null {
    try {
      const parsed: unknown = JSON.parse(value || '[]');
      return Array.isArray(parsed) ? parsed as T[] : null;
    } catch {
      return null;
    }
  }

  private parseJsonNullable<T>(value: string): T | null | undefined {
    try {
      const parsed: unknown = JSON.parse(value || 'null');
      if (parsed === null) {
        return null;
      }

      return typeof parsed === 'object' ? parsed as T : undefined;
    } catch {
      return undefined;
    }
  }

  private resolveCurrentTitle(): string {
    const titles: LocalizedItem<string>[] | null = this.parseJsonArray<LocalizedItem<string>>(this.eventForm.controls.titlesJson.value);
    return titles ? resolveLocalizedText(titles, this.currentLang(), '') : '';
  }

  private normalizeOptionalText(value: string | null | undefined): string | null {
    const normalizedValue: string = value?.trim() ?? '';
    return normalizedValue.length > 0 ? normalizedValue : null;
  }

  private normalizePrecision(value: string | null | undefined): HistoryDatePrecision {
    return value === 'Day' || value === 'Month' ? value : 'Year';
  }
}
