import { Component, Input } from '@angular/core';
import { Park } from '@app/models/parks/park';
import { stripHtml, resolveLocalizedValue } from '@app/commons/localized-item.utils';
import { buildParkAddressLine, buildParkLocationLine, buildParkSlug } from '@app/commons/park-presentation.utils';
import { NgIf } from '@angular/common';
import { ImageDisplayComponent } from '../../shared/image-display/image-display.component';
import { Bind } from 'primeng/bind';
import { ButtonDirective } from 'primeng/button';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

@Component({
    selector: 'app-park-card',
    templateUrl: './park-card.component.html',
    styleUrls: ['./park-card.component.scss'],
    imports: [NgIf, ImageDisplayComponent, Bind, ButtonDirective, RouterLink, TranslateModule]
})
export class ParkCardComponent {
  @Input() park: Park | null = null;
  @Input() currentLang = 'en';
  @Input() compact = false;

  get parkLink(): string[] | null {
    if (!this.park?.id || !this.park?.name) {
      return null;
    }

    return ['/', this.currentLang, 'park', this.park.id, buildParkSlug(this.park.name)];
  }

  get hasLogoImageId(): boolean {
    return !!this.park?.currentLogoImageId?.trim();
  }

  get locationLine(): string | null {
    return buildParkLocationLine(this.park);
  }

  get addressLine(): string | null {
    return buildParkAddressLine(this.park);
  }

  get coordinatesLine(): string | null {
    if (!this.park) {
      return null;
    }

    return `${this.park.latitude.toFixed(3)}, ${this.park.longitude.toFixed(3)}`;
  }

  get shortDescription(): string | null {
    const localizedDescription: string | undefined = resolveLocalizedValue(this.park?.descriptions, this.currentLang);
    const plainText: string = stripHtml(localizedDescription);

    if (!plainText) {
      return null;
    }

    if (plainText.length <= 140) {
      return plainText;
    }

    return `${plainText.slice(0, 137).trimEnd()}...`;
  }
}
