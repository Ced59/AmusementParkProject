import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, EventEmitter, OnInit, Output } from '@angular/core';

import { ButtonDirective } from 'primeng/button';
import { TabsModule } from 'primeng/tabs';

import { CaptainCoasterComparisonFacade } from '@features/admin/data/state/captain-coaster-comparison.facade';
import { CaptainCoasterPipelineFacade } from '@features/admin/data/state/captain-coaster-pipeline.facade';
import { CaptainCoasterImportTabComponent } from './captain-coaster-import-tab.component';
import { CaptainCoasterProgressTabComponent } from './captain-coaster-progress-tab.component';
import { CaptainCoasterComparisonTabComponent } from './captain-coaster-comparison-tab.component';

@Component({
  selector: 'app-captain-coaster-admin-provider',
  standalone: true,
  imports: [
    CommonModule,
    ButtonDirective,
    TabsModule,
    CaptainCoasterImportTabComponent,
    CaptainCoasterProgressTabComponent,
    CaptainCoasterComparisonTabComponent
  ],
  templateUrl: './captain-coaster-admin-provider.component.html',
  styleUrl: './captain-coaster-admin-provider.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [CaptainCoasterPipelineFacade, CaptainCoasterComparisonFacade]
})
export class CaptainCoasterAdminProviderComponent implements OnInit {
  @Output()
  public readonly back = new EventEmitter<void>();

  protected readonly errorMessage = this.captainCoasterPipelineFacade.errorMessage;
  protected readonly successMessage = this.captainCoasterPipelineFacade.successMessage;
  protected readonly session = this.captainCoasterPipelineFacade.session;
  protected readonly isSessionRunning = this.captainCoasterPipelineFacade.isSessionRunning;
  protected readonly totalCount = this.captainCoasterComparisonFacade.totalCount;

  constructor(
    private readonly captainCoasterPipelineFacade: CaptainCoasterPipelineFacade,
    private readonly captainCoasterComparisonFacade: CaptainCoasterComparisonFacade
  ) {
  }

  ngOnInit(): void {
    void this.captainCoasterPipelineFacade.initializeAsync();
  }
}
