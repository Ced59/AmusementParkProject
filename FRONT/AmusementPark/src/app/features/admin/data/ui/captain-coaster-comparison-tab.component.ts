import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { ButtonDirective } from 'primeng/button';
import { Card } from 'primeng/card';
import { Checkbox } from 'primeng/checkbox';
import { SelectModule } from 'primeng/select';
import { TableModule } from 'primeng/table';
import { Tag } from 'primeng/tag';

import { PaginationComponent } from '@app/components/shared/pagination/pagination.component';
import { CaptainCoasterComparisonFacade } from '@features/admin/data/state/captain-coaster-comparison.facade';
import { CaptainCoasterPipelineFacade } from '@features/admin/data/state/captain-coaster-pipeline.facade';
import { CaptainCoasterComparisonAnalysisComponent } from './captain-coaster-comparison-analysis.component';

@Component({
  selector: 'app-captain-coaster-comparison-tab',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ButtonDirective,
    Card,
    Checkbox,
    SelectModule,
    TableModule,
    Tag,
    PaginationComponent,
    CaptainCoasterComparisonAnalysisComponent
  ],
  templateUrl: './captain-coaster-comparison-tab.component.html',
  styleUrl: './captain-coaster-comparison-tab.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class CaptainCoasterComparisonTabComponent {
  protected readonly isBusy = this.captainCoasterPipelineFacade.isBusy;
  protected readonly session = this.captainCoasterPipelineFacade.session;
  protected readonly isLoaded = this.captainCoasterComparisonFacade.isLoaded;
  protected readonly isLoadingPage = this.captainCoasterComparisonFacade.isLoadingPage;
  protected readonly filters = this.captainCoasterComparisonFacade.filters;
  protected readonly currentItems = this.captainCoasterComparisonFacade.currentItems;
  protected readonly totalCount = this.captainCoasterComparisonFacade.totalCount;
  protected readonly selectedCount = this.captainCoasterComparisonFacade.selectedCount;
  protected readonly allPageSelected = this.captainCoasterComparisonFacade.allPageSelected;
  protected readonly currentPage = this.captainCoasterComparisonFacade.currentPage;
  protected readonly pageSize = this.captainCoasterComparisonFacade.pageSize;
  protected readonly sessionUpdated = this.captainCoasterComparisonFacade.sessionUpdated;
  protected readonly sessionMissing = this.captainCoasterComparisonFacade.sessionMissing;
  protected readonly sessionDuplicate = this.captainCoasterComparisonFacade.sessionDuplicate;
  protected readonly sessionApplied = this.captainCoasterComparisonFacade.sessionApplied;
  protected readonly entityTypeOptions = this.captainCoasterComparisonFacade.entityTypeOptions;
  protected readonly changeTypeOptions = this.captainCoasterComparisonFacade.changeTypeOptions;
  protected readonly appliedOptions = this.captainCoasterComparisonFacade.appliedOptions;

  constructor(
    protected readonly captainCoasterComparisonFacade: CaptainCoasterComparisonFacade,
    protected readonly captainCoasterPipelineFacade: CaptainCoasterPipelineFacade
  ) {
  }
}
