import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

import { UnlocatedItemsPanelComponent, UnlocatedItemsPanelItem } from './unlocated-items-panel.component';

describe('UnlocatedItemsPanelComponent', () => {
  let fixture: ComponentFixture<UnlocatedItemsPanelComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        UnlocatedItemsPanelComponent,
        TranslateModule.forRoot()
      ],
      providers: [
        provideRouter([])
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(UnlocatedItemsPanelComponent);
  });

  it('paginates unlocated items', () => {
    const items: UnlocatedItemsPanelItem[] = Array.from({ length: 6 }, (_value: unknown, index: number) => ({
      id: `item-${index + 1}`,
      name: `Item ${index + 1}`,
      categoryLabelKey: 'parkExplorer.categories.attraction',
      typeLabelKey: 'parkExplorer.types.familyRide',
      detailLink: ['/items', `${index + 1}`]
    }));

    fixture.componentRef.setInput('items', items);
    fixture.componentRef.setInput('pageSize', 5);
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Item 1');
    expect(fixture.nativeElement.textContent).not.toContain('Item 6');

    const nextButton: HTMLButtonElement = fixture.nativeElement.querySelectorAll('.unlocated-items-panel__pagination button')[1];
    nextButton.click();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).not.toContain('Item 1');
    expect(fixture.nativeElement.textContent).toContain('Item 6');
  });

  it('keeps items without detail links visible', () => {
    const items: UnlocatedItemsPanelItem[] = [
      {
        id: 'item-without-link',
        name: 'Item without link',
        categoryLabelKey: 'parkExplorer.categories.service',
        typeLabelKey: 'parkExplorer.types.other',
        detailLink: null
      }
    ];

    fixture.componentRef.setInput('items', items);
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Item without link');
  });
});
