import { Component } from '@angular/core';
import { Bind } from 'primeng/bind';
import { ButtonDirective } from 'primeng/button';
import { RouterLinkActive, RouterLink, RouterOutlet } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

@Component({
    selector: 'app-admin-dashboard',
    templateUrl: './admin-dashboard.component.html',
    styleUrl: './admin-dashboard.component.scss',
    imports: [Bind, ButtonDirective, RouterLinkActive, RouterLink, RouterOutlet, TranslateModule]
})
export class AdminDashboardComponent {

}
