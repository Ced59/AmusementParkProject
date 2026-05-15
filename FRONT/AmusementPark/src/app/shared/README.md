# shared

Couche de réutilisation transverse.

Elle accueille les composants partagés, utilitaires, helpers d'affichage, modèles communs
et petites briques réemployables entre plusieurs domaines.

Aucune logique métier spécifique à une feature ne doit s'y accumuler.

À partir de **P03**, les contrats communs officiels vivent dans `shared/models/contracts`
et les helpers de mapping génériques dans `shared/utils/mapping`.
