import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { ButtonDirective } from '@shared/primeless/button';
import { Card } from '@shared/primeless/card';
import { Checkbox } from '@shared/primeless/checkbox';

import { CaptainCoasterPipelineFacade } from '@features/admin/data/state/captain-coaster-pipeline.facade';

@Component({
  selector: 'app-captain-coaster-import-tab',
  standalone: true,
  imports: [CommonModule, FormsModule, ButtonDirective, Card, Checkbox],
  templateUrl: './captain-coaster-import-tab.component.html',
  styleUrl: './captain-coaster-import-tab.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class CaptainCoasterImportTabComponent {
  protected readonly status = this.captainCoasterPipelineFacade.status;
  protected readonly settings = this.captainCoasterPipelineFacade.settings;
  protected readonly isBusy = this.captainCoasterPipelineFacade.isBusy;
  protected readonly canImport = this.captainCoasterPipelineFacade.canImport;
  protected readonly importKind = this.captainCoasterPipelineFacade.importKind;
  protected readonly manualUrlsText = this.captainCoasterPipelineFacade.manualUrlsText;
  protected readonly manualUrlCount = this.captainCoasterPipelineFacade.manualUrlCount;
  protected readonly startAtStep = this.captainCoasterPipelineFacade.startAtStep;
  protected readonly resumeLatestSession = this.captainCoasterPipelineFacade.resumeLatestSession;
  protected readonly scrapingStepOptions = this.captainCoasterPipelineFacade.scrapingStepOptions;
  protected readonly scrapingThrottleFields = this.captainCoasterPipelineFacade.scrapingThrottleFields;
  protected readonly scrapingSelectorFields = this.captainCoasterPipelineFacade.scrapingSelectorFields;

  constructor(protected readonly captainCoasterPipelineFacade: CaptainCoasterPipelineFacade) {
  }
}
