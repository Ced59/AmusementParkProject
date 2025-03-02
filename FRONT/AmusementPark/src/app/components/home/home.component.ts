import {Component, OnInit} from '@angular/core';
import {ApiService} from "../../services/api.service";
import {Park} from "../../models/parks/park";
import {Pagination} from "../../models/shared/pagination";
import {TranslationService} from "../../services/translation.service";
import {filter} from "rxjs/operators";
import {NavigationEnd, Router} from "@angular/router";
import {Subscription} from "rxjs";

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.scss']
})
export class HomeComponent implements OnInit{
  parks: Park[] = [];
  pagination: Pagination | null = null;
  currentLang: string = 'en';


  constructor(
    private apiService: ApiService,
    private translationService: TranslationService
  ) {}

  ngOnInit(): void {
    this.loadParks(1, 10);
    this.currentLang = this.translationService.getCurrentLang() || 'en';
  }

  loadParks(page: number, size: number): void {
    this.apiService.getParksPaginated(page, size).subscribe({
      next: (response) => {
        this.parks = response.data;
        this.pagination = response.pagination;
      },
      error: (error) => console.error('Error fetching parks:', error)
    });
  }

  onPageChange(event: any): void {
    this.loadParks(event.page + 1, event.rows);
  }

  slugify(text: string): string {
    return text
      .toLowerCase()
      .trim()
      .replace(/[^a-z0-9]+/g, '-')
      .replace(/(^-|-$)/g, '');
  }

}
