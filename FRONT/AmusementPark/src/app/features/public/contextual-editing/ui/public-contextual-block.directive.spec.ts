import { Component } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';

import { PublicContextualBlockMarker } from '../models/public-contextual-block-marker.model';
import { PublicContextualBlockMarkerRegistry } from '../state/public-contextual-block-marker.registry';
import { PublicContextualBlockDirective } from './public-contextual-block.directive';

@Component({
  template: '<section [appPublicContextualBlock]="marker">Public block</section>',
  imports: [PublicContextualBlockDirective]
})
class HostComponent {
  marker: PublicContextualBlockMarker | null = {
    type: 'park.description',
    parkId: 'park-1',
    contextLabel: 'Phantasialand',
    languageCode: 'fr'
  };
}

describe('PublicContextualBlockDirective', () => {
  let fixture: ComponentFixture<HostComponent>;
  let markerRegistry: PublicContextualBlockMarkerRegistry;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HostComponent]
    }).compileComponents();

    markerRegistry = TestBed.inject(PublicContextualBlockMarkerRegistry);
    fixture = TestBed.createComponent(HostComponent);
    fixture.detectChanges();
  });

  it('stores marker data outside of public DOM attributes', () => {
    const host: HTMLElement = getHostElement(fixture);
    const markerId: string | null = host.getAttribute('data-admin-contextual-block-marker-id');

    expect(markerId).toBeTruthy();
    expect(host.getAttribute('data-admin-contextual-block-type')).toBe('park.description');
    expect(markerRegistry.getMarker(markerId)?.parkId).toBe('park-1');
  });

  it('clears registry data when the marker is destroyed', () => {
    const host: HTMLElement = getHostElement(fixture);
    const markerId: string = host.getAttribute('data-admin-contextual-block-marker-id') as string;

    fixture.destroy();

    expect(markerRegistry.getMarker(markerId)).toBeNull();
  });
});

function getHostElement(fixture: ComponentFixture<HostComponent>): HTMLElement {
  return (fixture.nativeElement as HTMLElement).querySelector('section') as HTMLElement;
}
