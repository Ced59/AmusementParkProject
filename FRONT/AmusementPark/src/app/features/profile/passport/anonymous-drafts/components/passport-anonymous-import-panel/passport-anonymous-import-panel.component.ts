import { ChangeDetectionStrategy, Component, EventEmitter, OnInit, Output, inject, signal } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';

import { UiButtonDirective, UiKickerComponent, UiSurfaceDirective } from '@ui/primitives';
import {
  PassportAnonymousDraftPreview,
  PassportAnonymousImportChoice,
  PassportAnonymousImportReportItem,
  PassportAnonymousMetadataChoice,
  PassportAnonymousRideDraft,
  PassportAnonymousServerRidePreview
} from '../../models/passport-anonymous-draft.models';
import { PassportAnonymousImportStateFacade } from '../../state/passport-anonymous-import-state.facade';

@Component({
  selector: 'app-passport-anonymous-import-panel',
  templateUrl: './passport-anonymous-import-panel.component.html',
  styleUrl: './passport-anonymous-import-panel.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [PassportAnonymousImportStateFacade],
  imports: [TranslateModule, UiButtonDirective, UiKickerComponent, UiSurfaceDirective]
})
export class PassportAnonymousImportPanelComponent implements OnInit {
  @Output() passportChanged = new EventEmitter<void>();

  protected readonly facade = inject(PassportAnonymousImportStateFacade);
  protected readonly consent = signal<boolean>(false);

  ngOnInit(): void {
    void this.facade.load();
  }

  protected setChoice(draftId: string, value: string): void {
    if (value === 'Separate' || value === 'Merge' || value === 'Ignore') {
      this.facade.setChoice(draftId, value as PassportAnonymousImportChoice);
    }
  }

  protected setMetadataChoice(draftId: string, value: string): void {
    if (value === 'KeepServer' || value === 'UseLocal') {
      this.facade.setMetadataChoice(draftId, value as PassportAnonymousMetadataChoice);
    }
  }

  protected setTarget(draftId: string, visitId: string): void {
    void this.facade.setTargetVisit(draftId, visitId);
  }

  protected prepareComparison(): void {
    void this.facade.prepareComparison(this.consent());
  }

  protected hasMergeCandidate(preview: PassportAnonymousDraftPreview): boolean {
    return preview.similarVisits.some((visit): boolean => visit.status === 'Draft');
  }

  protected async importDrafts(): Promise<void> {
    await this.facade.importAll(this.consent());
    const report = this.facade.report();
    if (report && report.importedVisitCount + report.mergedVisitCount > 0) {
      this.passportChanged.emit();
    }
  }

  protected trackPreview(_index: number, preview: PassportAnonymousDraftPreview): string {
    return preview.draft.id;
  }

  protected trackLocalRide(_index: number, ride: PassportAnonymousRideDraft): string {
    return ride.id;
  }

  protected trackServerRide(_index: number, ride: PassportAnonymousServerRidePreview): string {
    return ride.id;
  }

  protected trackReport(_index: number, item: PassportAnonymousImportReportItem): string {
    return item.draftId;
  }
}
