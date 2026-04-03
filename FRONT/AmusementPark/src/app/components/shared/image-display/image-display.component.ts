import { Component, Input, OnChanges, SimpleChanges } from '@angular/core';
import { ApiService } from '../../../services/api.service';

/**
 * Composant partagé d'affichage d'image.
 *
 * Centralise :
 *  - la résolution de l'URL réelle à partir d'un imageId (ID d'entité Image),
 *    d'un chemin relatif (/images/{id}) ou d'une URL absolue ;
 *  - le fallback automatique vers l'image par défaut en cas d'erreur de chargement ;
 *  - l'affichage d'une image par défaut (placeholder) quand aucun imageId n'est fourni.
 *
 * Le serveur backend gère nativement la négociation de format (WebP si le navigateur
 * le supporte, sinon JPG) via l'en-tête Accept, de façon transparente pour ce composant.
 */
@Component({
  selector: 'app-image-display',
  templateUrl: './image-display.component.html',
  styleUrls: ['./image-display.component.scss'],
  standalone: false
})
export class ImageDisplayComponent implements OnChanges {

  /** ID de l'entité Image, chemin relatif (/images/…) ou URL absolue. */
  @Input() imageId: string | null = null;

  /** Texte alternatif pour l'attribut alt de l'image. */
  @Input() alt: string = '';

  /**
   * Classe(s) CSS à appliquer à la balise <img>.
   * Utiliser :host ::ng-deep .<classe> dans le SCSS du composant hôte.
   */
  @Input() imgClass: string = '';

  imageLoadFailed: boolean = false;

  constructor(private readonly apiService: ApiService) {}

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['imageId']) {
      this.imageLoadFailed = false;
    }
  }

  get resolvedImageUrl(): string | null {
    const rawValue: string | undefined = this.imageId?.trim();

    if (!rawValue) {
      return null;
    }

    if (/^https?:\/\//i.test(rawValue)) {
      return rawValue;
    }

    if (rawValue.startsWith('/images/')) {
      const entityId: string = rawValue.replace(/^\/images\//, '');
      return this.apiService.buildImageUrl(entityId);
    }

    return this.apiService.buildImageUrl(rawValue);
  }

  get showImage(): boolean {
    return !!this.resolvedImageUrl && !this.imageLoadFailed;
  }

  onImageError(): void {
    this.imageLoadFailed = true;
  }
}
