# AmusementPark — Pack de guidelines pour Codex

Branche : `docs/codex-guidelines-20260630`  
Date : 2026-07-01
Projet : `amusement-parks.fun`

Ce dossier sert de contexte de travail pour Codex. Il centralise les règles éditoriales et techniques à appliquer lors des tâches liées aux JSON upsert, aux descriptions publiques et aux articles.

## Entrée recommandée

- `park-data-integration-orchestrator.md` : fichier à donner à ChatGPT/Codex pour intégrer un parc de bout en bout sans saturer le contexte. Il impose le parcours par étapes, l’export initial, l’export actualisé avant chaque nouvelle étape, les limites de lots et les fichiers de règles à lire selon l’étape.

## Documents disponibles

- `park-data-integration-orchestrator.md` : orchestrateur principal du parcours complet.
- `park-graph-upsert-enums.md` : liste des enums et valeurs autorisées dans les JSON Park Graph Upsert.
- `park-data-integration-steps/00-intake-and-export.md` : cadrage, pertinence, export et découpage anti-saturation.
- `park-data-integration-steps/01-park-core-upsert.md` : identité du parc, dates principales, coordonnées, statut, exploitant et fondateur.
- `park-data-integration-steps/02-zones-upsert.md` : zones officielles et structure de visite.
- `park-data-integration-steps/03-park-items-inventory-upsert.md` : inventaire des parkItems, dates, statuts, références et rattachements.
- `park-data-integration-steps/04-rich-descriptions-localization.md` : descriptions longues, naturelles et localisées dans les 8 langues.
- `park-data-integration-steps/05-images-and-reference-enrichment.md` : images importables, logos, crédits, biographies et références.
- `park-data-integration-steps/06-opening-hours-and-named-events.md` : horaires, exceptions datées et événements nommés.
- `park-data-integration-steps/07-history-timelines-and-articles.md` : histoire du parc, histoire des parkItems et articles rattachés.
- `park-data-integration-steps/08-final-audit-and-publication.md` : audit final avant publication.

## Ordre de lecture conseillé pour Codex

1. Lire ce `README.md`.
2. Lire `park-data-integration-orchestrator.md` pour une intégration complète de parc.
3. Lire le fichier d’étape correspondant dans `park-data-integration-steps/`.

## Règles globales non négociables

- Toujours vérifier la pertinence de l’entité pour `amusement-parks.fun` avant d’enrichir ou de formater un JSON.
- Ne jamais enrichir artificiellement une entité douteuse.
- Ne pas se limiter aux coasters : référencer les attractions, zones, restaurants, boutiques, hôtels, parkings, services, points d’accès, spectacles fixes, animaux/enclos et autres contenus visiteurs nommables quand ils sont fiables.
- Les descriptions doivent être naturelles, spécifiques au lieu, agréables à lire, orientées visiteur et non mécaniques.
- Ne jamais écrire de formulations du type “ce que ça apporte à la journée”, “au groupe”, “comment l’intégrer dans la journée” ou “quand cela devient utile”.
- Ne pas mettre les conditions d’accès, restrictions, tailles, tarifs ou informations purement techniques dans les descriptions : ces données doivent aller dans les champs JSON prévus.
- Les conditions d’accès de chaque attraction doivent être recherchées systématiquement et intégrées dans `items[].attractionDetails.accessConditions[]` quand elles sont fiables.
- Les enums utilisées dans un JSON upsert doivent venir de `park-graph-upsert-enums.md`, avec les valeurs canoniques exactes.
- Les dates ne doivent jamais être inventées. Si seule l’année d’ouverture ou de fermeture est fiable, renseigner l’année seule dans le JSON ; ne jamais fabriquer un `01-01` ou un premier jour de mois.
- Les images externes doivent pointer vers une URL HTTP(S) publique que l’importeur peut télécharger et reconnaître comme image réelle. Un CDN est accepté s’il renvoie bien des octets d’image importables.
- Une image ne doit jamais être livrée si son propriétaire n’est pas résolu. Un warning Preview du type `Remote image ignored: owner could not be resolved` est une erreur de livrable à corriger avant import.
- Tout `manufacturerKey`, `zoneKey`, `operatorKey`, `founderKey` ou `ownerKey` utilisé doit être résolu dans le même JSON ou par une identité existante sûre.
- Les `zoneKey` et `manufacturerKey` sont des causes fréquentes d’erreurs : tout JSON qui les utilise doit embarquer les zones minimales et constructeurs minimaux nécessaires quand l’export actualisé ne prouve pas déjà leur existence.
- Une alerte Preview de clé non résolue bloque le livrable. Corriger le JSON, fournir une version corrigée et ne pas passer au lot ou à l’étape suivante tant que la Preview signale l’erreur.
- Les horaires, dates d’ouverture et événements datés doivent être vérifiés avec des sources actuelles et ne doivent pas être mélangés aux tarifs si les tarifs ne sont pas implémentés.
- Les libellés et raisons visibles dans le calendrier doivent être réservés aux événements nommés, exceptions datées ou informations temporaires utiles. Ne jamais y répéter des commentaires généraux sur tous les jours normaux.
- Les articles doivent apporter une vraie valeur éditoriale, avec des sources vérifiées, et ne doivent pas devenir des fiches techniques déguisées.
- Les événements et articles historiques doivent être rédigés pour les visiteurs, sans phrases d’audit interne, justification de méthode, “repère documentaire prudent” ou formulation mécanique sur la présence confirmée d’un élément.
- Les sources d’articles et d’événements doivent être des URL HTTP(S) valides et joignables au moment de la génération. Ne jamais livrer de source en 404, 410, erreur serveur, soft-404 ou URL inventée.
- Pour une intégration complète, ne jamais enchaîner deux étapes sans export actualisé du parc après l’application de l’étape précédente.
- Les JSON upsert doivent rester bornés : une étape, un lot cohérent, aucune copie massive de l’export complet si seules quelques entités changent.
- Chaque livraison de JSON upsert doit inclure un récap visible avant le fichier : ce qui est ajouté, corrigé, masqué ou conservé, le périmètre exact du lot, un compteur d’avancement traité/total et le reste à traiter avant l’étape suivante.

## Anciennes guidelines

Les anciennes guidelines séparées JSON, descriptions et articles ont été consolidées dans l’orchestrateur et les fichiers d’étapes. Ne pas recréer de règles parallèles : toute évolution doit enrichir le fichier d’étape concerné.

## Usage attendu

Quand Codex travaille sur le projet, il doit citer ou appliquer ces fichiers comme règles de référence, puis produire des changements cohérents avec le style validé pour AmusementPark.
