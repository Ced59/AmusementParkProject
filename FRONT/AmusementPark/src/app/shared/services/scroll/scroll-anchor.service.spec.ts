import { TestBed } from '@angular/core/testing';

import { ScrollAnchorService } from './scroll-anchor.service';

describe('ScrollAnchorService', () => {
  let service: ScrollAnchorService;
  let root: HTMLElement;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(ScrollAnchorService);
    root = document.createElement('div');
    document.body.appendChild(root);
  });

  afterEach(() => {
    root.remove();
  });

  it('scrolls the closest pagination target from the pagination host', () => {
    const target: HTMLElement = document.createElement('section');
    const host: HTMLElement = document.createElement('app-pagination');
    target.setAttribute('data-pagination-scroll-target', '');
    target.appendChild(host);
    root.appendChild(target);
    const scrollSpy: jasmine.Spy = spyOn(target, 'scrollIntoView');

    service.scrollToPaginationTarget(host, { behavior: 'auto' });

    expect(scrollSpy).toHaveBeenCalledOnceWith({
      behavior: 'auto',
      block: 'start',
      inline: 'nearest'
    });
  });

  it('prefers an explicit target selector over the closest pagination target', () => {
    const closestTarget: HTMLElement = document.createElement('section');
    const explicitTarget: HTMLElement = document.createElement('section');
    const host: HTMLElement = document.createElement('app-pagination');
    closestTarget.setAttribute('data-pagination-scroll-target', '');
    explicitTarget.id = 'explicit-scroll-target';
    closestTarget.appendChild(host);
    root.appendChild(closestTarget);
    root.appendChild(explicitTarget);
    const closestScrollSpy: jasmine.Spy = spyOn(closestTarget, 'scrollIntoView');
    const explicitScrollSpy: jasmine.Spy = spyOn(explicitTarget, 'scrollIntoView');

    service.scrollToPaginationTarget(host, {
      targetSelector: '#explicit-scroll-target',
      block: 'nearest'
    });

    expect(closestScrollSpy).not.toHaveBeenCalled();
    expect(explicitScrollSpy).toHaveBeenCalledOnceWith({
      behavior: 'smooth',
      block: 'nearest',
      inline: 'nearest'
    });
  });

  it('scrolls a selector target directly', () => {
    const target: HTMLElement = document.createElement('section');
    target.id = 'selector-scroll-target';
    root.appendChild(target);
    const scrollSpy: jasmine.Spy = spyOn(target, 'scrollIntoView');

    service.scrollToSelector('#selector-scroll-target', { behavior: 'auto' });

    expect(scrollSpy).toHaveBeenCalledOnceWith({
      behavior: 'auto',
      block: 'start',
      inline: 'nearest'
    });
  });
});
