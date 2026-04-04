import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';

/**
 * Ce composant est conservé pour la rétrocompatibilité des routes.
 * La gestion des données externes a été déplacée dans l'onglet "Données".
 * Toute navigation vers /admin/site est automatiquement redirigée.
 */
@Component({
  selector: 'app-admin-site',
  template: `<p style="padding:2rem;opacity:.6">Redirection vers Gestion des données...</p>`,
  imports: []
})
export class AdminSiteComponent implements OnInit {

  constructor(private readonly router: Router) {}

  ngOnInit(): void {
    void this.router.navigate(['..', 'data'], { replaceUrl: true });
  }
}
