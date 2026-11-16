# Futur — expériences de découverte et de fidélisation

> Roadmap de réalisation pour Amusement-Parks.Fun. Version initiale : 16 novembre 2026.
> **Documenter n'est pas implémenter.** Les quatre concepts ci-dessous restent à réaliser. Le merge de cette roadmap n'autorise ni leur activation, ni une publication sur un réseau social.

## L'objectif

Donner une raison de découvrir le catalogue, de partager une expérience personnelle et de revenir, même sans préparer une sortie. Les résultats proviennent exclusivement des données publiques du site et de calculs explicables. Ni l'originalité mondiale, ni un gain de trafic ne sont présentés comme démontrés : ce sont des hypothèses produit à tester.

Périmètre demandé, avec conservation des numéros du brainstorming :

| ID | Expérience | Promesse utilisateur | Boucle de retour |
| --- | --- | --- | --- |
| C01 | PARKLINKS | Relier deux parcs par des connexions prouvées | Défi quotidien, résultat sans spoiler, nouvelles découvertes |
| C02 | L'effet papillon | Explorer le catalogue en retirant virtuellement un constructeur | Scénarios comparables, carte avant/après, découverte hebdomadaire |
| C03 | DoppelPark | Trouver des parcs étonnamment proches par leur composition | Comparaisons expliquées, découverte d'une destination, mises à jour significatives |
| C07 | Le Train sans rails | Choisir collectivement le prochain parc d'un voyage virtuel | Vote, résultat du lendemain, journal des étapes |

**Ordre de réalisation recommandé et adopté par cette roadmap : C02 → C01 → C03 → C07.** C02 permet de valider une première expérience complète, la qualité des projections et la chaîne de partage avec un risque algorithmique limité. C01 apporte ensuite le graphe réutilisable par C07. C03 réutilise les projections et les supports de diffusion. C07 arrive après validation des mécanismes de calcul, de publication et d'exploitation.

Le propriétaire peut changer cet ordre explicitement. Un changement d'ordre ne supprime jamais le verrou de livraison séquentielle.

## Règles non négociables

1. **Avant chaque implémentation**, Codex remet au propriétaire un descriptif fonctionnel ET technique exhaustif du concept retenu. Voir [le contrat de travail](00-regles-codex.md). Une liste de fichiers ou un résumé de quelques lignes ne suffit pas.
2. **Un seul concept actif à la fois.** Le suivant ne commence qu'après merge, déploiement réussi et recette de production documentée du précédent. Une branche mergée, un job lancé ou un flag encore désactivé ne prouvent pas la livraison.
3. Anticiper les suivants est permis uniquement par des contrats sobres et des composants réellement utilisés par le concept courant. Pas d'implémentation cachée du suivant, pas de plateforme générale préalable.
4. **Aucun LLM dans le produit, les jobs, les traductions à l'exécution, les visuels, les vidéos ou la diffusion.** Pas d'API de génération, d'embeddings, de modèle local, de fallback IA, ni de clé fournisseur. Codex reste un outil de développement/review, pas une dépendance d'exploitation.
5. Aucun nouvel abonnement, service payant, achat de crédits, GPU ou hausse d'infrastructure autorisée implicitement. Les nouvelles bibliothèques doivent avoir une licence compatible et un coût d'exploitation acceptable. Si le VPS ne suffit pas, réduire la fréquence, la résolution ou le périmètre ; ne pas engager une dépense.
6. Réutiliser la publication sociale, l'authentification, les ports, le SSR, les médias, la localisation et l'analytique existants. Ne pas contourner les règles `PARK_DATA_EDITOR` pour enrichir les données.
7. Une donnée inconnue reste inconnue. Une relation informatique n'est pas une causalité historique. Toute carte sociale conserve les réserves nécessaires même sortie de son contexte.
8. Préparer automatiquement les contenus ne vaut jamais autorisation de les publier. La politique de publication par compte, réseau, concept et fréquence doit être expressément activée par le propriétaire.

## Lecture dans l'ordre

| Document | Contenu |
| --- | --- |
| [00 — Contrat Codex](00-regles-codex.md) | Cadrage obligatoire, verrou interconcepts, PR/review/merge/déploiement, autorisations |
| [01 — Socle technique](01-socle-technique.md) | Audit réel du dépôt, projections, qualité, snapshots, frontières, sécurité, performances |
| [02 — PARKLINKS](02-parklinks.md) | Parcours, graphe, défis, validation, score, API, UX, backlog et tests |
| [03 — Effet papillon](03-effet-papillon.md) | Simulation non destructive, ensembles, dénominateurs, scénarios et partage |
| [04 — DoppelPark](04-doppelpark.md) | Empreintes, score explicable, couverture, classement et validation |
| [05 — Train sans rails](05-train-sans-rails.md) | États, votes, clôture atomique, reprise, candidats et journal |
| [06 — Diffusion](06-diffusion-multicanal.md) | Partage visiteur, Page Facebook, Instagram, TikTok, LinkedIn, exports et publication |
| [07 — Recette et exploitation](07-recette-et-exploitation.md) | Matrice de tests, budgets, métriques, activation, rollback et preuves |
| [08 — Plan de livraison](08-plan-de-livraison.md) | Lots par concept, dépendances, critères de passage, registre d'avancement |
| [09 — Sources et audit](09-sources-et-audit.md) | Fichiers inspectés, limites de l'audit et documentations officielles |

Les chemins, types, routes et collections proposés sont des **cibles de conception**, pas des éléments déjà présents. Codex doit vérifier l'état courant de `origin/master` avant de les créer et adapter les noms aux conventions existantes. Les seuils chiffrés sont des choix initiaux à éprouver, pas des mesures déjà obtenues.

## État initial des livraisons

| Concept | État | PR d'implémentation | SHA déployé | Recette production | Autorisation sociale |
| --- | --- | --- | --- | --- | --- |
| C02 | PLANNED | — | — | — | Désactivée |
| C01 | PLANNED, dépend de C02 | — | — | — | Désactivée |
| C03 | PLANNED, dépend de C01 | — | — | — | Désactivée |
| C07 | PLANNED, dépend de C03 | — | — | — | Désactivée |

Aucun concept n'est actif à la création de ces documents. Ne pas remplacer ces tirets par des intentions : uniquement par des preuves observées. Mettre à jour le registre du document 08 au fil des livraisons.

## Instruction prête à donner à Codex

```text
Implémente le prochain concept autorisé de futur/README.md, et seulement celui-ci.
Lis AGENTS.md puis les documents 00, 01, 06, 07, 08 et la fiche du concept.
Vérifie que le précédent est mergé, réellement déployé et validé en production.
Avant toute modification applicative, présente-moi le descriptif fonctionnel
et technique exhaustif demandé par futur/00-regles-codex.md et consigne-le
avec les écarts constatés par rapport à la roadmap.
Réalise ensuite les lots du seul concept retenu, avec tests, PR en français,
traitement des reviews dont Codex, merge et contrôle du déploiement.
Aucun LLM ni service payant dans la fonctionnalité ou la diffusion.
Ne publie rien sur un réseau social sans politique expressément autorisée.
Termine par les preuves de recette et les limites ; ne commence pas le concept suivant.
```

## Ce que cette roadmap ne fait pas

Elle ne lance aucun développement produit, ne crée aucun nouveau compte social, ne change aucun secret, ne publie aucun contenu, ne modifie aucun parc et ne promet aucune disponibilité d'API tierce. Elle organise une réalisation sérieuse, progressive et mesurable, dans **un seul dossier `futur/`**.
