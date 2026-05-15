import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { Tag } from 'primeng/tag';

import { CaptainCoasterComparisonResultResponse } from '@app/models/admin/data/data-management.models';
import { CaptainCoasterComparisonFacade } from '@features/admin/data/state/captain-coaster-comparison.facade';

@Component({
  selector: 'app-captain-coaster-comparison-analysis',
  standalone: true,
  imports: [CommonModule, FormsModule, Tag],
  templateUrl: './captain-coaster-comparison-analysis.component.html',
  styleUrl: './captain-coaster-comparison-analysis.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class CaptainCoasterComparisonAnalysisComponent {
  @Input({ required: true })
  public row!: CaptainCoasterComparisonResultResponse;

  constructor(protected readonly captainCoasterComparisonFacade: CaptainCoasterComparisonFacade) {
  }
}
