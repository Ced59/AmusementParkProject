import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-public-footer',
  templateUrl: './public-footer.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class PublicFooterComponent {
}
