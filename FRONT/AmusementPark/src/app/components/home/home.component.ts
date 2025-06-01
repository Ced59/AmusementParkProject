import { Component, OnInit, OnDestroy } from '@angular/core';
import { Subject, Subscription, of } from 'rxjs';
import { debounceTime, distinctUntilChanged, switchMap, map } from 'rxjs/operators';

interface Item {
  id: number;
  title: string;
  description: string;
  type: 'parc' | 'coaster' | 'autre'; // vous pourrez ajouter d’autres valeurs plus tard
}

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.scss']
})
export class HomeComponent implements OnInit, OnDestroy {
  // Texte saisi dans la barre de recherche
  searchTerm: string = '';

  // Tableau des catégories sélectionnées (values)
  selectedCategories: string[] = [];

  // Options à afficher dans le MultiSelect
  categoryOptions: { label: string; value: string }[] = [
    { label: 'Parcs',    value: 'parc' },
    { label: 'Coasters', value: 'coaster' },
    // Par la suite, si vous ajoutez “Restaurants”, “Hôtels”, etc. :
    // { label: 'Restaurants', value: 'restaurant' }, etc.
  ];

  private searchSubject = new Subject<string>();
  private subscription$!: Subscription;

  results: Item[] = [];

  // Exemple de “base de données” en dur
  private allItems: Item[] = [
    { id: 1, title: 'Parc Asterix',     description: 'Parc à thème français.',           type: 'parc' },
    { id: 2, title: 'Disneyland Paris', description: 'Le parc magique.',                    type: 'parc' },
    { id: 3, title: 'Magic Coaster',    description: 'Montagnes russes vertigineuses.',     type: 'coaster' },
    { id: 4, title: 'Roller Fun',       description: 'Une expérience à sensations.',         type: 'coaster' },
    { id: 5, title: 'Parc Astérix Pro', description: 'Zone repas & attractions.',          type: 'parc' },
    // … vous pouvez ajouter d’autres “type: 'restaurant'” ou “type: 'hotel'” plus tard
  ];

  ngOnInit(): void {
    this.subscription$ = this.searchSubject
      .pipe(
        debounceTime(300),
        distinctUntilChanged(),
        switchMap((term: string) => of(term).pipe(map((t) => this.filterItems(t))))
      )
      .subscribe((filtered: Item[]) => {
        this.results = filtered;
      });
  }

  ngOnDestroy(): void {
    if (this.subscription$) {
      this.subscription$.unsubscribe();
    }
  }

  /**
   * Appelé à chaque frappe sur l’input
   */
  onSearchInput(value: string) {
    this.searchTerm = value.trim();
    this.searchSubject.next(this.searchTerm);
  }

  /**
   * Appelé à chaque changement dans le MultiSelect
   */
  onFilterChange() {
    // On repart du même terme de recherche actuel
    this.searchSubject.next(this.searchTerm);
  }

  /**
   * Filtrage combiné : texte + catégories
   */
  private filterItems(term: string): Item[] {
    // Si aucun terme et aucune catégorie sélectionnée, on n'affiche rien
    if (!term && this.selectedCategories.length === 0) {
      return [];
    }

    const lower = term.toLowerCase();

    return this.allItems.filter((item) => {
      // 1) Filtrer par texte (si term non vide)
      const matchesText =
        !term ||
        item.title.toLowerCase().includes(lower) ||
        item.description.toLowerCase().includes(lower);

      // 2) Filtrer par catégories (si en a sélectionné au moins une)
      let matchesCategory = true;
      if (this.selectedCategories.length > 0) {
        // Si l’item.type (ex. 'parc') figure dans selectedCategories (['parc','coaster']), alors true
        matchesCategory = this.selectedCategories.includes(item.type);
      }

      return matchesText && matchesCategory;
    });
  }
}
