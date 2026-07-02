import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, effect } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { ButtonDirective } from '@shared/ui/primitives/button';
import { Card } from '@shared/ui/primitives/card';
import { Checkbox } from '@shared/ui/primitives/checkbox';
import { SelectModule } from '@shared/ui/primitives/select';
import { TableModule } from '@shared/ui/primitives/table';
import { Tag } from '@shared/ui/primitives/tag';

import { PaginationComponent } from '@shared/components/pagination/pagination.component';
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
  private lastAutoLoadedSessionId: string | null = null;
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
    effect(() => {
      const session = this.session();
      const canAutoLoad = session !== null
        && session.status === 'Completed'
        && session.comparisonResults > 0
        && !this.isLoaded()
        && !this.isLoadingPage();

      if (!canAutoLoad || session === null || this.lastAutoLoadedSessionId === session.id) {
        return;
      }

      this.lastAutoLoadedSessionId = session.id;
      queueMicrotask(() => {
        void this.loadComparisonResultsAsync();
      });
    });
  }

  protected async loadComparisonResultsAsync(): Promise<void> {
    await this.captainCoasterComparisonFacade.loadComparisonAsync();
  }

  protected getEmptyComparisonMessage(): string {
    const session = this.session();
    if (session === null) {
      return 'Aucune session Captain Coaster n’est sélectionnée.';
    }

    if (session.status !== 'Completed' && session.comparisonResults === 0) {
      return 'La comparaison n’est pas encore disponible : le pipeline doit terminer la construction des différences avant de pouvoir afficher des résultats.';
    }

    if (session.comparisonResults > 0) {
      return 'Aucun résultat ne correspond aux filtres actuels. Essaie de retirer les filtres ou d’actualiser.';
    }

    return 'Aucun résultat de comparaison n’est disponible pour cette session.';
  }
}
