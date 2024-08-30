import {Component, OnInit} from '@angular/core';
import {ApiService} from "../../services/api.service";

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrl: './home.component.scss'
})
export class HomeComponent {
  constructor(private apiService: ApiService) {
  }



}
