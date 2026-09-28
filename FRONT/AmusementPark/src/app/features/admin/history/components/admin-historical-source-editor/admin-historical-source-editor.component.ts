import { ChangeDetectionStrategy, Component, EventEmitter, Input, OnChanges, Output, SimpleChanges, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';

import { AdminHistoricalSource, SaveHistoricalSourceRequest } from '@app/models/history/admin-historical-workbench.models';
import { HISTORICAL_SOURCE_SCOPES, HISTORICAL_SOURCE_TYPES } from '../../models/admin-historical-workbench-options';

interface HistoricalSourceForm {
  type: FormControl<string>;
  title: FormControl<string>;
  publisherOrAuthor: FormControl<string>;
  url: FormControl<string>;
  bibliographicReference: FormControl<string>;
  publishedOn: FormControl<string>;
  accessedOn: FormControl<string>;
  languageCode: FormControl<string>;
  archiveUrl: FormControl<string>;
  adminNote: FormControl<string>;
  accessibility: FormControl<string>;
}

@Component({
  selector: 'app-admin-historical-source-editor',
  templateUrl: './admin-historical-source-editor.component.html',
  styleUrl: './admin-historical-source-editor.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, TranslateModule]
})
export class AdminHistoricalSourceEditorComponent implements OnChanges {
  @Input() public source: AdminHistoricalSource | null = null;
  @Input() public busy = false;
  @Output() public readonly submitted = new EventEmitter<{ sourceId: string | null; request: SaveHistoricalSourceRequest }>();
  @Output() public readonly cancelled = new EventEmitter<void>();

  protected readonly sourceTypes = HISTORICAL_SOURCE_TYPES;
  protected readonly sourceScopes = HISTORICAL_SOURCE_SCOPES;
  protected readonly selectedScopes = signal<ReadonlySet<string>>(new Set<string>());
  protected readonly scopeMissing = signal<boolean>(false);
  protected readonly form = new FormGroup<HistoricalSourceForm>({
    type: new FormControl<string>('OfficialWebsite', { nonNullable: true, validators: [Validators.required] }),
    title: new FormControl<string>('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(500)] }),
    publisherOrAuthor: new FormControl<string>('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(300)] }),
    url: new FormControl<string>('', { nonNullable: true }),
    bibliographicReference: new FormControl<string>('', { nonNullable: true }),
    publishedOn: new FormControl<string>('', { nonNullable: true }),
    accessedOn: new FormControl<string>(new Date().toISOString().slice(0, 10), { nonNullable: true, validators: [Validators.required] }),
    languageCode: new FormControl<string>('fr', { nonNullable: true }),
    archiveUrl: new FormControl<string>('', { nonNullable: true }),
    adminNote: new FormControl<string>('', { nonNullable: true }),
    accessibility: new FormControl<string>('Accessible', { nonNullable: true, validators: [Validators.required] })
  });

  public ngOnChanges(changes: SimpleChanges): void {
    if (changes['source']) {
      this.resetFromSource();
    }
  }

  protected submit(): void {
    this.scopeMissing.set(false);
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    if (this.selectedScopes().size === 0) {
      this.scopeMissing.set(true);
      return;
    }

    const value = this.form.getRawValue();
    this.submitted.emit({
      sourceId: this.source?.id ?? null,
      request: {
        expectedRevision: this.source?.revision ?? null,
        type: value.type,
        title: value.title.trim(),
        publisherOrAuthor: value.publisherOrAuthor.trim(),
        url: this.optional(value.url),
        bibliographicReference: this.optional(value.bibliographicReference),
        publishedOn: this.optional(value.publishedOn),
        accessedOn: value.accessedOn,
        languageCode: this.optional(value.languageCode),
        archiveUrl: this.optional(value.archiveUrl),
        scopes: [...this.selectedScopes()],
        adminNote: this.optional(value.adminNote),
        accessibility: value.accessibility,
        reviewNote: null
      }
    });
  }

  protected toggleScope(scope: string): void {
    const selectedScopes: Set<string> = new Set(this.selectedScopes());
    if (selectedScopes.has(scope)) {
      selectedScopes.delete(scope);
    } else {
      selectedScopes.add(scope);
    }

    this.selectedScopes.set(selectedScopes);
    this.scopeMissing.set(false);
  }

  private resetFromSource(): void {
    const source: AdminHistoricalSource | null = this.source;
    this.form.reset({
      type: source?.type ?? 'OfficialWebsite',
      title: source?.title ?? '',
      publisherOrAuthor: source?.publisherOrAuthor ?? '',
      url: source?.url ?? '',
      bibliographicReference: source?.bibliographicReference ?? '',
      publishedOn: source?.publishedOn ?? '',
      accessedOn: source?.accessedOn ?? new Date().toISOString().slice(0, 10),
      languageCode: source?.languageCode ?? 'fr',
      archiveUrl: source?.archiveUrl ?? '',
      adminNote: source?.adminNote ?? '',
      accessibility: source?.accessibility ?? 'Accessible'
    });
    this.selectedScopes.set(new Set(source?.scopes ?? []));
    this.scopeMissing.set(false);
  }

  private optional(value: string): string | null {
    const normalizedValue: string = value.trim();
    return normalizedValue || null;
  }
}
