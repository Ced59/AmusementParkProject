import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, Input } from '@angular/core';

import { CaptainCoasterSessionLogResponse } from '@app/models/admin/data/data-management.models';

@Component({
  selector: 'app-captain-coaster-session-logs',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './captain-coaster-session-logs.component.html',
  styleUrl: './captain-coaster-session-logs.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class CaptainCoasterSessionLogsComponent {
  @Input({ required: true })
  public logs: CaptainCoasterSessionLogResponse[] = [];
}
