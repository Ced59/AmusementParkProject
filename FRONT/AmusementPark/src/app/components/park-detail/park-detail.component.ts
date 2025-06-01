import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { ApiService } from '../../services/api.service';
import { Park } from '../../models/parks/park';

@Component({
  selector: 'app-park-detail',
  templateUrl: './park-detail.component.html',
  styleUrls: ['./park-detail.component.scss']
})
export class ParkDetailComponent implements OnInit {
  park: Park | undefined;
  currentLang: string = 'en';

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private apiService: ApiService
  ) {}

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    const lang = this.route.snapshot.paramMap.get('lang') || this.route.parent?.snapshot.paramMap.get('lang');
    if (lang) {
      this.currentLang = lang;
    }
    if (id) {
      this.apiService.getParkById(id).subscribe((park: Park) => {
        this.park = park;
      });
    }
  }

  goBack(): void {
    this.router.navigate([`/${this.currentLang}/parks`]);
  }
}
