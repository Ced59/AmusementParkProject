import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, signal } from '@angular/core';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { ButtonDirective } from 'primeng/button';
import { Card } from 'primeng/card';
import { Checkbox } from 'primeng/checkbox';
import { ProgressBar } from 'primeng/progressbar';
import { TableModule } from 'primeng/table';
import { Tag } from 'primeng/tag';
import { TabsModule } from 'primeng/tabs';

import {
  AdminDataSourceSummaryResponse,
  CaptainCoasterComparisonResultResponse,
  CaptainCoasterSettingsResponse,
  CaptainCoasterSyncSessionResponse
} from '../../../../models/admin/site/captain-coaster-admin.models';
import { CaptainCoasterAdminService } from '../../../../services/admin/site/captain-coaster-admin.service';

@Component({
  selector: 'app-admin-site',
  templateUrl: './admin-site.component.html',
  styleUrl: './admin-site.component.scss',
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    ButtonDirective,
    Card,
    Checkbox,
    ProgressBar,
    TableModule,
    Tag,
    TabsModule
  ]
})
export class AdminSiteComponent implements OnInit {
  protected readonly sources = signal<AdminDataSourceSummaryResponse[]>([]);
  protected readonly settings = signal<CaptainCoasterSettingsResponse | null>(null);
  protected readonly latestSession = signal<CaptainCoasterSyncSessionResponse | null>(null);
  protected readonly comparisonResults = signal<CaptainCoasterComparisonResultResponse[]>([]);
  protected readonly selectedComparisonIds = signal<string[]>([]);
  protected readonly selectedFiles = signal<File[]>([]);
  protected readonly isBusy = signal<boolean>(false);
  protected readonly errorMessage = signal<string>('');
  protected readonly successMessage = signal<string>('');
  protected readonly updatedCount = computed(() => this.comparisonResults().filter((item: CaptainCoasterComparisonResultResponse) => item.changeType === 'Updated').length);
  protected readonly missingCount = computed(() => this.comparisonResults().filter((item: CaptainCoasterComparisonResultResponse) => item.changeType === 'MissingLocal').length);
  protected readonly selectedFileNames = computed(() => this.selectedFiles().map((file: File) => file.name));

  protected readonly settingsForm = this.formBuilder.group({
    isEnabled: [true, [Validators.required]]
  });

  constructor(
    private readonly formBuilder: FormBuilder,
    private readonly captainCoasterAdminService: CaptainCoasterAdminService
  ) {
  }

  async ngOnInit(): Promise<void> {
    await this.loadInitialData();
  }

  protected async saveSettingsAsync(): Promise<void> {
    if (this.settingsForm.invalid) {
      this.errorMessage.set('La configuration est invalide.');
      return;
    }

    this.isBusy.set(true);
    this.clearMessages();

    try {
      const response: CaptainCoasterSettingsResponse = await firstValueFrom(this.captainCoasterAdminService.updateSettings({
        isEnabled: this.settingsForm.controls.isEnabled.value ?? true
      }));
      this.settings.set(response);
      this.successMessage.set('Configuration de la source enregistrée.');
      await this.refreshSourcesAsync();
    } catch (error: unknown) {
      this.errorMessage.set(this.extractErrorMessage(error));
    } finally {
      this.isBusy.set(false);
    }
  }

  protected onFilesSelected(event: Event): void {
    const input: HTMLInputElement = event.target as HTMLInputElement;
    const files: File[] = Array.from(input.files ?? []);
    const deduplicated: File[] = [];
    const seenNames: Set<string> = new Set<string>();

    for (const file of files) {
      const normalizedName: string = file.name.trim().toLowerCase();
      if (!seenNames.has(normalizedName)) {
        seenNames.add(normalizedName);
        deduplicated.push(file);
      }
    }

    this.selectedFiles.set(deduplicated);
    this.clearMessages();
    input.value = '';
  }

  protected removeSelectedFile(fileName: string): void {
    this.selectedFiles.set(this.selectedFiles().filter((file: File) => file.name !== fileName));
  }

  protected async importJsonAsync(): Promise<void> {
    if (this.selectedFiles().length === 0) {
      this.errorMessage.set('Ajoute au moins un fichier JSON avant de lancer l’import.');
      return;
    }

    this.isBusy.set(true);
    this.clearMessages();

    try {
      const response: CaptainCoasterSyncSessionResponse = await firstValueFrom(this.captainCoasterAdminService.importJson(this.selectedFiles()));
      this.latestSession.set(response);
      this.selectedFiles.set([]);
      await this.loadComparisonResults(response.id);
      await this.refreshSourcesAsync();
      this.successMessage.set('Import JSON Captain Coaster terminé.');
    } catch (error: unknown) {
      this.errorMessage.set(this.extractErrorMessage(error));
    } finally {
      this.isBusy.set(false);
    }
  }

  protected async refreshLatestSessionAsync(): Promise<void> {
    this.isBusy.set(true);
    this.clearMessages();

    try {
      await this.loadLatestSessionAndResultsAsync();
      await this.refreshSourcesAsync();
      this.successMessage.set('Vue des imports actualisée.');
    } catch (error: unknown) {
      this.errorMessage.set(this.extractErrorMessage(error));
    } finally {
      this.isBusy.set(false);
    }
  }

  protected async applySelectionAsync(): Promise<void> {
    const ids: string[] = this.selectedComparisonIds();
    if (ids.length === 0) {
      this.errorMessage.set('Sélectionne au moins une ligne à appliquer.');
      return;
    }

    this.isBusy.set(true);
    this.clearMessages();

    try {
      const response: { appliedCount: number } = await firstValueFrom(this.captainCoasterAdminService.applyComparisonResults(ids));
      this.successMessage.set(`${response.appliedCount} changement(s) appliqué(s).`);
      this.selectedComparisonIds.set([]);
      await this.loadLatestSessionAndResultsAsync();
      await this.refreshSourcesAsync();
    } catch (error: unknown) {
      this.errorMessage.set(this.extractErrorMessage(error));
    } finally {
      this.isBusy.set(false);
    }
  }

  protected toggleSelection(id: string, checked: boolean): void {
    const current: string[] = this.selectedComparisonIds();
    if (checked) {
      if (!current.includes(id)) {
        this.selectedComparisonIds.set([...current, id]);
      }
      return;
    }

    this.selectedComparisonIds.set(current.filter((item: string) => item !== id));
  }

  protected isSelected(id: string): boolean {
    return this.selectedComparisonIds().includes(id);
  }

  protected trackById(_index: number, item: CaptainCoasterComparisonResultResponse): string {
    return item.id;
  }

  private async loadInitialData(): Promise<void> {
    this.isBusy.set(true);
    this.clearMessages();

    try {
      await this.refreshSourcesAsync();

      const settings: CaptainCoasterSettingsResponse = await firstValueFrom(this.captainCoasterAdminService.getSettings());
      this.settings.set(settings);
      this.settingsForm.patchValue({ isEnabled: settings.isEnabled });

      await this.loadLatestSessionAndResultsAsync();
    } catch (error: unknown) {
      this.errorMessage.set(this.extractErrorMessage(error));
    } finally {
      this.isBusy.set(false);
    }
  }

  private async refreshSourcesAsync(): Promise<void> {
    const sources: AdminDataSourceSummaryResponse[] = await firstValueFrom(this.captainCoasterAdminService.getSources());
    this.sources.set(sources);
  }

  private async loadLatestSessionAndResultsAsync(): Promise<void> {
    try {
      const latestSession: CaptainCoasterSyncSessionResponse = await firstValueFrom(this.captainCoasterAdminService.getLatestSession());
      this.latestSession.set(latestSession);
      await this.loadComparisonResults(latestSession.id);
    } catch {
      this.latestSession.set(null);
      this.comparisonResults.set([]);
    }
  }

  private async loadComparisonResults(sessionId: string | null): Promise<void> {
    const results: CaptainCoasterComparisonResultResponse[] = await firstValueFrom(this.captainCoasterAdminService.getComparisonResults(sessionId));
    this.comparisonResults.set(results);
  }

  private clearMessages(): void {
    this.errorMessage.set('');
    this.successMessage.set('');
  }

  private extractErrorMessage(error: unknown): string {
    if (typeof error === 'object' && error !== null && 'error' in error) {
      const errorPayload: Record<string, unknown> = error.error as Record<string, unknown>;
      if (typeof errorPayload['message'] === 'string') {
        return errorPayload['message'];
      }
    }

    return 'Une erreur est survenue.';
  }
}
