import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit } from '@angular/core';

import { AdminDataSourcesFacade } from '@features/admin/data/state/admin-data-sources.facade';
import { AdminDataSourcesListComponent } from '@features/admin/data/ui/admin-data-sources-list.component';
import { CaptainCoasterAdminProviderComponent } from '@features/admin/data/ui/captain-coaster-admin-provider.component';

@Component({
  selector: 'app-admin-data',
  standalone: true,
  imports: [
    CommonModule,
    AdminDataSourcesListComponent,
    CaptainCoasterAdminProviderComponent
  ],
  templateUrl: './admin-data.component.html',
  styleUrl: './admin-data.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [AdminDataSourcesFacade]
})
export class AdminDataComponent implements OnInit {
  protected readonly dataSources = this.adminDataSourcesFacade.dataSources;
  protected readonly selectedSourceKey = this.adminDataSourcesFacade.selectedSourceKey;

  constructor(private readonly adminDataSourcesFacade: AdminDataSourcesFacade) {
  }

  ngOnInit(): void {
    void this.adminDataSourcesFacade.loadSourcesAsync();
  }

  protected selectSource(sourceKey: string): void {
    this.adminDataSourcesFacade.selectSource(sourceKey);
  }

  protected backToSources(): void {
    this.adminDataSourcesFacade.clearSelection();
  }
}
