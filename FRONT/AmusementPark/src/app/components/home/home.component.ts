import {Component, OnInit} from '@angular/core';
import {ApiService} from "../../services/api.service";
import {Park} from "../../models/parks/park";
import {Pagination} from "../../models/shared/pagination";

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrl: './home.component.scss'
})
export class HomeComponent implements OnInit{
  parks: Park[] = [];
  pagination: Pagination | null = null;


  constructor(private apiService: ApiService) {
  }

  ngOnInit(): void {
    this.loadParks(1, 10);
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

}
