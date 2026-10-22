# Premier lot standalone — États-Unis, 22 octobre 2026

Trois attractions permanentes à billetterie propre ont été qualifiées, créées, enrichies puis publiées sur le site et sur Facebook par le client officiel `PARK_DATA_EDITOR`. Il s’agit d’un premier lot du backlog mondial, pas de la clôture de l’inventaire. Les campagnes France et Portugal conservent leur périmètre.

Les recherches dans les parcs et les standalone, y compris les fiches masquées, n’ont pas retrouvé d’identité correspondante avant création. Les activités annexes et les autres coasters voisins n’ont pas été assimilés à ces fiches. Aucun parc artificiel ni doublon volontaire n’a été créé.

## Résultats et preuves

| Attraction | Audit | Dernière intégration de données (UTC) | Publication Facebook |
| --- | ---: | --- | --- |
| [Smoky Mountain Alpine Coaster](https://amusement-parks.fun/fr/attraction/54ad5095-bb99-4fdc-b54e-b52fd0d3dbc4/smoky-mountain-alpine-coaster) | [91/100](smoky-audit.json) | 2026-10-22T20:56:38.577Z | [Publiée](https://www.facebook.com/1285475681307050/posts/122124208023424431) — 2026-10-22T21:07:50.1322493Z |
| [Flathead Lake Alpine Coaster](https://amusement-parks.fun/fr/attraction/1b3144f7-2cdb-48b7-a3ca-daaa2f2f6213/flathead-lake-alpine-coaster) | [100/100](flathead-audit.json) | 2026-10-22T21:20:29.592Z | [Publiée](https://www.facebook.com/1285475681307050/posts/122124208893424431) — 2026-10-22T21:24:52.2115357Z |
| [The Branson Coaster](https://amusement-parks.fun/fr/attraction/72b0aadb-436f-47f0-a978-15610e577f4c/the-branson-coaster) | [100/100](branson-audit.json) | 2026-10-22T21:25:14.317Z | [Publiée](https://www.facebook.com/1285475681307050/posts/122124209451424431) — 2026-10-22T21:34:14.2529372Z |

Le score est celui du contrôle de complétude du site ; il ne prouve pas l’exhaustivité de toutes les informations historiques ou techniques. Aucun blocage de publication n’a été relevé. Les annonces utilisent le message bilingue et l’image résolus par défaut ; les trois réponses de publication portent le statut `Published` et un identifiant Facebook distinct.

| Couverture intégrée | Smoky Mountain | Flathead Lake | Branson |
| --- | ---: | ---: | ---: |
| Langues | 8 | 8 | 8 |
| Photographies propres | 2 | 3 | 3 |
| Conditions d’accès | 10 | 4 | 8 |
| Événements historiques | 3 | 1 | 3 |
| Articles | 1 | 0 | 1 |
| Jours couverts par le planning hebdomadaire | 7 | 7 | 7 |
| Dates avec horaires particuliers | 0 | 0 | 10 |
| Offres tarifaires | 0 | 6 | 14 |

Les noms, coordonnées, descriptions, références constructeur, médias, horaires, tarifs disponibles, conditions et histoire ont été contrôlés. Les récits, légendes et conditions existent en français, anglais, allemand, néerlandais, italien, espagnol, polonais et portugais. La référence Wiegand existante a été réutilisée sans modification globale.

## Sources et arbitrages

- **Smoky Mountain** : site officiel, FAQ et consignes, tourisme de Pigeon Forge, ouverture documentée par *Amusement Today*, accident de 2016 rapporté par WVLT. Les prix de 2013 et les montants secondaires non datés ont été écartés. Deux photographies officielles distinctes seulement ont été retenues.
- **Flathead Lake** : exploitant, grille de prix, horaires et consignes officiels, tourisme du Montana, *Daily Inter Lake* et KPAX. L’ouverture du 27 juin 2023 est établie par le reportage contemporain ; la date de mai présente dans RCDB n’a pas été reprise. Les six tarifs au guichet distinguent adultes/enfants et formules ; la formule d’une heure reste limitée à septembre–mai, sans année inventée.
- **Branson** : site officiel et pages tarifs, consignes et Halloween, *Amusement Today* pour juillet 2017, KY3 et procès-verbal de la commission du Missouri pour 2021. L’autorisation de reprise du 9 juillet 2021 n’est pas présentée comme une reprise commerciale constatée. Les quatorze tarifs au guichet distinguent espèces/carte et âges ; les dix vendredis et samedis d’octobre 2026 comportent les horaires de soirée à billet spécifique.

Les URL de toutes les sources retenues, les arbitrages et les lacunes figurent dans chaque audit. Les données techniques contradictoires, les dénominations juridiques d’exploitants non confirmées et les prix en ligne inaccessibles n’ont pas été inventés. Les photographies actuelles associées aux événements sont présentées comme du contexte, jamais comme des photographies d’accident ou d’inauguration.

## Vérifications et limites publiques

Les 24 pages d’attraction, les 24 pages d’historique et les 16 pages d’article ont répondu publiquement dans les huit langues. Les pages d’attraction ont été contrôlées à 390 px, sans débordement horizontal, et en français sur grand écran. Le corpus relu compte 1 336 chaînes localisées ; le contrôle n’a relevé ni langue manquante, ni paragraphe narratif dupliqué, ni défaut d’encodage ou jargon interne.

Les trois fiches apparaissent dans la recherche publique sans filtre, dans « Attractions isolées seules » et dans « Attractions + isolées » : [neuf recherches vérifiées](discovery-checks.json).

Après publication, la synchronisation de l’historique canonique a été vérifiée. Une nouvelle prévisualisation des graphes exportés n’a proposé aucune création, modification ou suppression (7, 7 et 9 éléments inchangés). Les reçus de données, de médias et de publication sont conservés séparément dans les fichiers `*-receipts.json` ; aucun jeton ni export complet n’est versionné.

Limites restantes :

- **Smoky Mountain : tarifs actuels non confirmés**, d’où un score de 91/100. Une troisième photographie officielle appropriée reste à rechercher.
- Les conditions d’accès sont enregistrées et auditées, mais le composant public standalone ne les affiche pas encore. Cette campagne ne modifie pas le code de ce composant.
- Le composant tarifaire affiche les libellés et les montants sans les notes détaillées. Les âges, moyens de paiement, limites des forfaits et saisonnalités essentiels ont été explicités dans les libellés ; certaines notes et réductions restent accessibles dans les données et auprès de l’exploitant.
- Le bloc d’horaires ne présente pas tout le calendrier futur. Les règles Halloween de Branson sont enregistrées ; le tarif de cette soirée spéciale n’a pas été établi.
- Les calendriers de Smoky Mountain et Flathead sont hebdomadaires : aucune période civile ou promesse d’ouverture en temps réel n’a été déduite.

## Suivi permanent

Les trois `entryId` et tous leurs événements d’intégration sont conservés dans [le registre des États-Unis](../../countries/US.json). Chaque ligne passe à `Integrated` avec présence `Published`, audit final et liens des publications. La clôture reprend la date du dernier véritable Apply de données ; les dates de publication, de vérification ou de commit restent séparées.

Les réservations sont libérées. Les lacunes restent dans l’audit et dans la prochaine action de la ligne pour une future mise à jour. Il ne faut pas republier les mêmes annonces Facebook.

**Aucune montée de version** : cette livraison au dépôt contient le backlog, ses preuves et les règles de suivi. `release-version.json` reste inchangé.
