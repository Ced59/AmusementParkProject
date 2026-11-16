# 08 — Plan de livraison séquentiel et registre

[Retour](README.md)

## 1. Ordre et dépendances

```text
Roadmap documentaire (aucun produit activé)
    ↓ demande explicite d'implémentation + cadrage exhaustif
C02 Effet papillon ── merge + déploiement + recette VERIFIED
    ↓ nouvelle demande / poursuite explicitement autorisée, un concept seulement
C01 PARKLINKS ────── merge + déploiement + recette VERIFIED
    ↓
C03 DoppelPark ───── merge + déploiement + recette VERIFIED
    ↓
C07 Train sans rails ─ merge + déploiement + cycle réel + recette VERIFIED
```

L'agent termine la demande d'implémentation après le concept retenu. Il n'enchaîne pas automatiquement toute la roadmap parce que le précédent vient de passer au vert. Une autorisation future peut préciser le prochain concept, mais ne supprime pas les gates.

Les dépendances techniques réelles sont plus limitées que cet ordre produit : C03 ne dépend pas du jeu C01 pour calculer ses scores, mais la séquence empêche de disperser les travaux ; C07 réutilise effectivement les preuves de relations de C01 et la chaîne de diffusion éprouvée. Aucune dépendance artificielle ne doit être introduite dans le code pour reproduire l'ordre du document.

## 2. Lot zéro documentaire — cette roadmap

Périmètre : créer un seul dossier `futur/`, écrire les spécifications et règles d'exécution, ajouter un renvoi ciblé dans les consignes du dépôt. Aucun code produit, aucun changement de secret, aucune modification de données du catalogue, aucun envoi social.

Cette modification de backlog/règles bénéficie de l'exemption de release déjà prévue par `AGENTS.md`. Ne pas incrémenter une release applicative uniquement pour ce dossier. Vérifier néanmoins la CI et les reviews de la PR documentaire, et observer le déploiement si le workflow existant s'exécute sur son merge.

## 3. Carte des lots à réaliser

Les lots ci-dessous sont des unités de revue, pas une obligation de créer une PR par ligne. Regrouper uniquement des changements cohérents et testables ; scinder une PR trop volumineuse. Tous les lots du concept actif sont livrés avant le suivant.

| Ordre | Lot | Résultat attendu | Dépendance / gate |
| --- | --- | --- | --- |
| 1 | C02-A | Cadrage visible + audit de couverture et modèles | demande d'implémentation, master actuel inspecté |
| 2 | C02-B | Projection minimale, normalisation et ensembles de simulation | fixtures et invariants de retrait |
| 3 | C02-C | API/scénarios/cache/pagination | aucune mutation du catalogue, accès sûrs |
| 4 | C02-D | Parcours mobile/SSR, avant-après et preuves | état incomplet et zéro effet gérés |
| 5 | C02-E | Carte sociale, exports, intégration du publisher existant | draft-only par défaut, droits et réserves |
| 6 | C02-F | Tests, reviews, merge, déploiement et activation | C02 VERIFIED, arrêt de l'agent |
| 7 | C01-A | Nouveau cadrage + densité du graphe | C02 VERIFIED, demande pour C01 |
| 8 | C01-B | Pool/édition quotidienne, solveur borné | absence de défis triviaux systématiques |
| 9 | C01-C | Tentatives/indices/validation/idempotence | pas de fuite de solution ni score client |
| 10 | C01-D | Jeu accessible, tutoriel, reprise et preuves | parcours anonyme complet |
| 11 | C01-E | Résultat sans spoiler, archives et supports | publication édition active/close distinguée |
| 12 | C01-F | Reviews/déploiement/recette | C01 VERIFIED, arrêt |
| 13 | C03-A | Nouveau cadrage, cohorte, poids et seuils | C01 VERIFIED, demande pour C03 |
| 14 | C03-B | Empreintes, score et contributions | symétrie, inconnus, cohorte figée |
| 15 | C03-C | Calcul borné, top-K et API | filtre lointain appliqué au bon niveau |
| 16 | C03-D | Comparaisons et supports | couverture/légende exactes, SSR utile |
| 17 | C03-E | Reviews/déploiement/validation éditoriale | C03 VERIFIED, arrêt |
| 18 | C07-A | Nouveau cadrage + stratégie d'atomicité prouvée | C03 VERIFIED, demande pour C07 |
| 19 | C07-B | Étapes/candidats/états | liens vérifiés, repli à zéro candidat |
| 20 | C07-C | Votes/clôture/transition/reprise | concurrence, fencing et crashs couverts |
| 21 | C07-D | Voyage/journal/carte/admin/supports | aucun vote externe compté, pas de spoiler |
| 22 | C07-E/F | Reviews, déploiement et cycle réel | C07 VERIFIED après vraie clôture |

## 4. Minimum partage/diffusion dans chaque concept

Ce lot n'est jamais repoussé à « après les quatre fonctionnalités » : chaque V1 doit pouvoir être découverte depuis un lien externe, afficher une page SSR appropriée, proposer partage natif/copie/téléchargement, produire ses supports factuels et un brouillon/export exploitable.

Les connecteurs réutilisables sont ajoutés quand ils servent le concept courant et que leurs prérequis sont disponibles. Facebook existant est prioritaire ; Instagram et LinkedIn peuvent être configurés progressivement. TikTok export est une voie de livraison prévue, pas un connecteur promis sans audit. Les autres plateformes ont au minimum les formats universels ; leurs APIs sont optionnelles et ne bloquent pas le cœur du produit.

La préparation est automatisable dès le départ. L'envoi automatique N2 n'est activé qu'après autorisation du compte et validation opérationnelle. Aucune absence de token n'autorise une publication via navigateur ou une copie de secret dans le dépôt.

## 5. Registre initial — à maintenir avec des faits

| Concept | État | Cadrage | PR / merge SHA | Déploiement | Recette | Blocage / décision |
| --- | --- | --- | --- | --- | --- | --- |
| C02 | PLANNED | Non commencé | — | — | — | Attendre la demande d'implémentation |
| C01 | PLANNED | Non commencé | — | — | — | C02 non VERIFIED |
| C03 | PLANNED | Non commencé | — | — | — | C01 non VERIFIED |
| C07 | PLANNED | Non commencé | — | — | — | C03 non VERIFIED |

**Concept actif : aucun.** La présente PR de documentation ne change pas ce registre en IMPLEMENTING.

Lors d'un démarrage, ajouter date/heure UTC, branche et responsable/agent à l'entrée du concept actif. Ne pas réclamer une exclusivité sur le reste du projet : les intégrations de catalogue ou correctifs indépendants peuvent continuer, mais doivent respecter les contraintes de ressources et éviter les mêmes fichiers/branches.

## 6. Gabarit de dossier de cadrage

```markdown
# Cxx — Cadrage avant implémentation
## Autorisation et état du précédent
## Master inspecté et écarts avec la roadmap
## Fonctionnalité exhaustive et hors périmètre
## Parcours/écrans/états/accessibilité/langues
## Données, couverture, preuves et droits
## Règles/algorithmes/exemples et limites
## Architecture et fichiers concernés
## Persistance/index/concurrence/idempotence
## API et sécurité/vie privée
## SSR/SEO/cache/retrait des sources
## Partage/diffusion : N0, N1, N2 par réseau
## Coût et performances : budgets et mesure
## Tests, lots, migrations, déploiement et rollback
## Risques, arbitrages et décision de démarrage
```

## 7. Gabarit de PR d'implémentation

```markdown
## Concept et lot
Cxx — ... ; aucun autre concept implémenté.
## Pourquoi / parcours livré
...
## Choix techniques et réutilisations
...
## Données, droits et effets de bord
...
## Tests réellement exécutés
...
## Flags, migration, release et déploiement
...
## Diffusion
Exports disponibles : ...
Publication réelle : désactivée / politique explicitement autorisée : ...
LLM/API payante/nouveau service : aucun.
## Risques / limites / éléments non vérifiés
...
## Fichiers supprimés
Aucun / liste justifiée.
## Recette attendue après merge
...
```

## 8. Arbitrages à trancher au cadrage, pas à masquer

Couverture réelle des données ; statuts d'attractions normalisables ; droits de redistribution des médias ; volumétrie et ressources libres ; capacité de rendu vidéo ; mécanisme actuel de jobs/invalidation ; topologie Mongo et atomicité des votes ; comptes et permissions sociaux réellement accessibles ; règles de conservation des progressions/votes ; seuils éditoriaux et fréquence de publication.

Pour chacun, Codex propose une décision raisonnable et un repli réalisable sans coût. Le besoin d'un seuil configurable ne justifie pas d'inventer les données qui le satisfont. Le besoin d'une permission ne justifie pas de dégrader la sécurité. Le besoin d'un composant commun ne justifie pas de démarrer le concept suivant.

## 9. Extensions explicitement hors V1

Classement mondial de PARKLINKS, graphes historiques multirelations sans preuve temporelle, simulation économique de l'effet papillon, personnalisation psychologique de DoppelPark, votes sociaux agrégés, plusieurs trains concurrents, chat communautaire, notifications marketing automatiques et connecteurs payants.

Ces pistes peuvent rester documentées. Elles ne doivent pas servir à retarder une V1 complète, à étendre la PR en cours sans accord ou à introduire une dépendance à un LLM.
