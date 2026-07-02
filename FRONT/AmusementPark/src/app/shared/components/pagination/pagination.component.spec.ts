import { ElementRef } from '@angular/core';
import { PaginatorState } from '@shared/primeless/paginator';

import { ScrollAnchorService } from '@shared/services/scroll/scroll-anchor.service';
import { PaginationComponent } from './pagination.component';

interface ExposedPaginationComponent {
  onPageChange(event: PaginatorState): void;
}

describe('PaginationComponent', () => {
  it('ignores current page events emitted by the paginator during initialization', () => {
    const context = createComponent();
    context.component.pagination = {
      currentPage: 1,
      totalPages: 2,
      totalItems: 18,
      itemsPerPage: 9
    };
    const pageChangedSpy = spyOn(context.component.pageChanged, 'emit');

    context.exposed.onPageChange({ first: 0, rows: 9, page: 0, pageCount: 2 });

    expect(pageChangedSpy).not.toHaveBeenCalled();
    expect(context.scrollService.scrollToPaginationTarget).not.toHaveBeenCalled();
  });

  it('ignores incomplete current page events emitted by the paginator during initialization', () => {
    const context = createComponent();
    context.component.pagination = {
      currentPage: 1,
      totalPages: 2,
      totalItems: 18,
      itemsPerPage: 9
    };
    const pageChangedSpy = spyOn(context.component.pageChanged, 'emit');

    context.exposed.onPageChange({ page: 0, pageCount: 2 });

    expect(pageChangedSpy).not.toHaveBeenCalled();
    expect(context.scrollService.scrollToPaginationTarget).not.toHaveBeenCalled();
  });

  it('emits and scrolls when the page really changes', () => {
    const context = createComponent();
    context.component.pagination = {
      currentPage: 1,
      totalPages: 2,
      totalItems: 18,
      itemsPerPage: 9
    };
    const pageChangedSpy = spyOn(context.component.pageChanged, 'emit');
    const event: PaginatorState = { first: 9, rows: 9, page: 1, pageCount: 2 };

    context.exposed.onPageChange(event);

    expect(pageChangedSpy).toHaveBeenCalledOnceWith(event);
    expect(context.scrollService.scrollToPaginationTarget).toHaveBeenCalledOnceWith(context.host, {
      targetSelector: null
    });
  });
});

function createComponent(): {
  component: PaginationComponent;
  exposed: ExposedPaginationComponent;
  host: HTMLElement;
  scrollService: jasmine.SpyObj<ScrollAnchorService>;
} {
  const host: HTMLElement = document.createElement('app-pagination');
  const scrollService: jasmine.SpyObj<ScrollAnchorService> = jasmine.createSpyObj<ScrollAnchorService>('ScrollAnchorService', ['scrollToPaginationTarget']);
  const component: PaginationComponent = new PaginationComponent(
    { nativeElement: host } as ElementRef<HTMLElement>,
    scrollService
  );

  return {
    component,
    exposed: component as unknown as ExposedPaginationComponent,
    host,
    scrollService
  };
}
