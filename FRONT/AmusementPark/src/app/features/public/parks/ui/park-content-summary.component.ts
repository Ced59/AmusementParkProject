import { Component, Input } from '@angular/core';
import { NgFor, NgIf } from '@angular/common';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { ButtonDirective } from 'primeng/button';

import { ParkContentSummaryViewModel } from '../models/park-content-summary.model';

@Component({
  selector: 'app-park-content-summary',
  templateUrl: './park-content-summary.component.html',
  styleUrls: ['./park-content-summary.component.scss'],
  imports: [NgFor, NgIf, RouterLink, TranslateModule, ButtonDirective]
})
export class ParkContentSummaryComponent {
  @Input() summary: ParkContentSummaryViewModel | null = null;
}
