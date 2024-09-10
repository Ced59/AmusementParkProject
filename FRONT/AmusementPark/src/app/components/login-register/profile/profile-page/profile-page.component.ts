import {Component, OnInit} from '@angular/core';
import {UserDto} from "../../../../models/users/user_dto";
import {ApiService} from "../../../../services/api.service";
import {AuthService} from "../../../../services/auth/auth.service";
import {ActivatedRoute} from "@angular/router";
import {environment} from "../../../../../environments/environment";

@Component({
  selector: 'app-profile-page',
  templateUrl: './profile-page.component.html',
  styleUrl: './profile-page.component.scss'
})
export class ProfilePageComponent implements OnInit{
  user: UserDto | null = null;

  constructor(
    private apiService: ApiService,
    private authService: AuthService,
    private route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    const userId = this.authService.getUserIdFromToken();
    if (userId) {
      this.apiService.getUserById(userId).subscribe(user => {
        this.user = user;
      });
    }
  }

  editField(field: string): void {
    // Ajoutez ici la logique pour modifier le champ spécifié
    console.log(`Modifier le champ: ${field}`);
  }

  protected readonly environment = environment;
}
