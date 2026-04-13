import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component } from '@angular/core';

import { Card } from 'primeng/card';
import { ProgressBar } from 'primeng/progressbar';
import { Tag } from 'primeng/tag';

import { CaptainCoasterPipelineFacade } from '@features/admin/data/state/captain-coaster-pipeline.facade';
import { CaptainCoasterSessionLogsComponent } from './captain-coaster-session-logs.component';

@Component({
  selector: 'app-captain-coaster-progress-tab',
  standalone: true,
  imports: [CommonModule, Card, ProgressBar, Tag, CaptainCoasterSessionLogsComponent],
  templateUrl: './captain-coaster-progress-tab.component.html',
  styleUrl: './captain-coaster-progress-tab.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class CaptainCoasterProgressTabComponent {
  protected readonly session = this.captainCoasterPipelineFacade.session;
  protected readonly isSessionRunning = this.captainCoasterPipelineFacade.isSessionRunning;

  constructor(protected readonly captainCoasterPipelineFacade: CaptainCoasterPipelineFacade) {
  }
}
