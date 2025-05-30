import {Component, OnInit} from '@angular/core';
import {ApiService} from "../../services/api.service";
import {Park} from "../../models/parks/park";
import {Pagination} from "../../models/shared/pagination";
import {TranslationService} from "../../services/translation.service";


@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.scss'],
  host: { 'class': 'home-component' }
})
export class HomeComponent {

}
