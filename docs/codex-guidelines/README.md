# AmusementPark — Pack de guidelines pour Codex

Branche : `docs/codex-guidelines-20260630`  
Date : 2026-06-30  
Projet : `amusement-parks.fun`

Ce dossier sert de contexte de travail pour Codex. Il centralise les règles éditoriales et techniques à appliquer lors des tâches liées aux JSON upsert, aux descriptions publiques et aux articles.

## Entrée recommandée

- `park-data-integration-orchestrator.md` : fichier à donner à ChatGPT/Codex pour intégrer un parc de bout en bout sans saturer le contexte. Il impose le parcours par étapes, l’export initial, l’export actualisé avant chaque nouvelle étape, les limites de lots et les fichiers de règles à lire selon l’étape.

## Documents disponibles

- `park-graph-upsert-json-guideline-r10.md` : règles de génération, correction et enrichissement des Park Graph Upsert JSON.
- `description-guidelines-r2.md` : charte obligatoire pour les descriptions publiques des parcs, zones, parkItems et références.
- `articles-guideline-r2-live-sources.md` : règles éditoriales pour les articles historiques, de visite, médias et actualités durables.
- `park-data-integration-steps/00-intake-and-export.md` : cadrage, pertinence, export et découpage anti-saturation.
- `park-data-integration-steps/01-park-core-upsert.md` : identité du parc, dates principales, coordonnées, statut, exploitant et fondateur.
- `park-data-integration-steps/02-zones-upsert.md` : zones officielles et structure de visite.
- `park-data-integration-steps/03-park-items-inventory-upsert.md` : inventaire des parkItems, dates, statuts, références et rattachements.
- `park-data-integration-steps/04-rich-descriptions-localization.md` : descriptions longues, naturelles et localisées dans les 8 langues.
- `park-data-integration-steps/05-images-and-reference-enrichment.md` : images directes, logos, crédits, biographies et références.
- `park-data-integration-steps/06-opening-hours-and-named-events.md` : horaires, exceptions datées et événements nommés.
- `park-data-integration-steps/07-history-timelines-and-articles.md` : histoire du parc, histoire des parkItems et articles rattachés.
- `park-data-integration-steps/08-final-audit-and-publication.md` : audit final avant publication.

## Ordre de lecture conseillé pour Codex

1. Lire ce `README.md`.
2. Lire `park-data-integration-orchestrator.md` pour une intégration complète de parc.
3. Lire le fichier d’étape correspondant dans `park-data-integration-steps/`.
4. Lire `park-graph-upsert-json-guideline-r10.md` avant toute modification JSON.
5. Lire `description-guidelines-r2.md` avant toute rédaction ou réécriture de description.
6. Lire `articles-guideline-r2-live-sources.md` avant toute création ou modification d’article.

## Règles globales non négociables

- Toujours vérifier la pertinence de l’entité pour `amusement-parks.fun` avant d’enrichir ou de formater un JSON.
- Ne jamais enrichir artificiellement une entité douteuse.
- Ne pas se limiter aux coasters : référencer les attractions, zones, restaurants, boutiques, hôtels, parkings, services, points d’accès, spectacles fixes, animaux/enclos et autres contenus visiteurs nommables quand ils sont fiables.
- Les descriptions doivent être naturelles, spécifiques au lieu, agréables à lire, orientées visiteur et non mécaniques.
- Ne jamais écrire de formulations du type “ce que ça apporte à la journée”, “au groupe”, “comment l’intégrer dans la journée” ou “quand cela devient utile”.
- Ne pas mettre les conditions d’accès, restrictions, tailles, tarifs ou informations purement techniques dans les descriptions : ces données doivent aller dans les champs JSON prévus.
- Les images externes doivent être des liens directs vers de vrais fichiers image téléchargeables, sans proxy CDN, preview, watermark ou page HTML.
- Tout `manufacturerKey`, `zoneKey`, `operatorKey`, `founderKey` ou `ownerKey` utilisé doit être résolu dans le même JSON ou par une identité existante sûre.
- Les horaires, dates d’ouverture et événements datés doivent être vérifiés avec des sources actuelles et ne doivent pas être mélangés aux tarifs si les tarifs ne sont pas implémentés.
- Les articles doivent apporter une vraie valeur éditoriale, avec des sources vérifiées, et ne doivent pas devenir des fiches techniques déguisées.
- Pour une intégration complète, ne jamais enchaîner deux étapes sans export actualisé du parc après l’application de l’étape précédente.
- Les JSON upsert doivent rester bornés : une étape, un lot cohérent, aucune copie massive de l’export complet si seules quelques entités changent.

## Usage attendu

Quand Codex travaille sur le projet, il doit citer ou appliquer ces fichiers comme règles de référence, puis produire des changements cohérents avec le style validé pour AmusementPark.
