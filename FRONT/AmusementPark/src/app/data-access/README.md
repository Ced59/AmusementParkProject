# data-access

Zone cible des accès HTTP structurés par domaine.

On y place progressivement :

- services API spécialisés ;
- modèles de contrat HTTP ;
- adaptateurs d'appel ;
- payloads techniques.

La logique d'écran et l'orchestration de page n'y ont pas leur place.

À partir de **P03**, les réponses paginées et contrats transverses doivent réutiliser en priorité
les types partagés de `@shared/models/contracts` au lieu de recréer des formes concurrentes.
