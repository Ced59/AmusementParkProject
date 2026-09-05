# Roadmap 5.1 — Cartes officielles des parcs

Date de référence : 2026-09-05
Version cible : 5.1.0

## Objectif produit

Rassembler sur la page carte d’un parc deux lectures complémentaires : la carte interactive issue des parkItems géolocalisés et les plans réellement publiés par le parc. Les plans officiels doivent rester consultables par millésime afin de documenter leurs évolutions sans dépendre d’une URL distante qui peut être remplacée chaque saison.

## Lot 5.1.0

- modèle embarqué `ParkOfficialMap`, versionné par année, langue et format ;
- stockage MinIO dédié aux documents PDF, images, KML, KMZ et ZIP, sans détour par le catalogue ou le traitement des images, avec diffusion en flux et requêtes partielles ;
- upload contrôlé `PARK_DATA_EDITOR`, métadonnées de provenance et publication privée par défaut ;
- export, Preview et Apply des cartes dans tous les flux Park Graph Upsert, simples comme bulk ;
- deux onglets publics accessibles et réutilisation du même composant visuel sur la galerie d’images ;
- dernière année disponible sélectionnée automatiquement ;
- repli automatique sur les cartes officielles lorsqu’aucun parkItem n’a de coordonnées affichables ;
- disponibilité de la route dans les navigations et sitemaps dès qu’une carte officielle publique existe ;
- textes publics dans les huit langues et tests unitaires des règles sensibles.

## Déploiement des données

### Phase A — Édition actuelle

1. Dresser la liste des parcs visibles et de ceux en cours de complétion.
2. Chercher la carte officielle de l’année courante dans le site, l’application, la billetterie et l’espace presse du parc ou de l’exploitant.
3. Importer le fichier dans MinIO, compléter les huit titres et, pour les images, les huit textes alternatifs.
4. Contrôler l’upsert en Preview, conserver la carte privée, puis la publier uniquement dans le flux de publication explicitement autorisé.

Indicateurs : parcs audités, cartes actuelles trouvées, cartes importées, absences justifiées et fichiers refusés.

### Phase B — Archives annuelles

1. Remonter année par année dans les archives officielles et les captures archivées des pages officielles.
2. Conserver toutes les éditions dont l’année et la provenance peuvent être prouvées, sans plafond artificiel.
3. Dédupliquer par `year + languageCode + format` et documenter chaque période sans archive retrouvée.
4. Vérifier que le dernier millésime reste sélectionné et que les anciennes années sont navigables sur mobile et au clavier.

Indicateurs : nombre de millésimes par parc, année la plus ancienne, trous de couverture et taux de liens sources encore joignables.

### Phase C — Exploitation et durcissement

- mesurer le poids et la fréquence de consultation des fichiers ;
- prévoir une politique de rétention pour les binaires orphelins uniquement avec un inventaire, un délai de sécurité et une autorisation de suppression explicite ;
- étudier un écran d’administration dédié si le flux JSON contrôlé ne suffit plus aux éditeurs humains ;
- réutiliser le contrat public dans les futures applications mobiles sans dupliquer les règles de millésime.

## Garde-fous

- aucune carte communautaire ou capture d’un fournisseur cartographique ;
- aucune année inventée et aucune publication implicite ;
- aucune suppression d’une ancienne édition par omission d’un JSON merge ;
- aucune clé MinIO acceptée si elle ne correspond pas au parc et à l’ID de carte ;
- chaque upload utilise une clé MinIO immuable afin qu’une réimportation ne remplace jamais un fichier public avant l’upsert validé ;
- aucun SVG servi en ligne et `nosniff` sur les téléchargements ;
- le score historique de complétude n’est pas modifié rétroactivement en 5.1 : la couverture des cartes est publiée comme indicateur distinct et l’édition actuelle manquante reste une lacune éditoriale.

## Terminé lorsque

- les contrôles backend et frontend passent ;
- les exemples JSON font un round-trip sans perte ;
- une carte image, un PDF et un fichier téléchargeable sont couverts par les validations ;
- la sélection de l’année, le repli d’onglet et la navigation clavier sont testés ;
- le déploiement 5.1.0 est sain avant de commencer la campagne de données.
