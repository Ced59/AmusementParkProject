# Amélioration du backlog mondial : audit P0 → P3 — 15 novembre 2026

> Nature du travail : corrections documentaires sourcées, sans changement de production. Le registre `countries/*.json` reste l'unique source de vérité. Le présent document est un état de suivi, non une nouvelle liste concurrente de parcs.

## Résultats appliqués dans cette branche

| Indicateur | Avant la reprise | Après la reprise | Signification |
| --- | ---: | ---: | --- |
| Entrées conservées dans le registre | 12 343 | 12 349 | 6 nouvelles pistes sourcées, aucune entrée supprimée |
| Pays/territoires avec observation externe | 168 | 172 | 4 territoires initialement vides documentés |
| Pays/territoires sans observation | 82 | 78 | Ne signifie pas zéro parc |
| Pays inconnu ZZ | 71 | 69 | 2 fiches Park existantes relocalisées documentalement |
| Park candidats sans fiche dans le relevé | 7 551 | 7 550 | Une candidature GT redondante sortie de la file ; pas de création garantie |
| Rapprochements incertains `PossibleExisting` | 1 273 | 1 276 | 3 cas GT résolus, 6 nouvelles pistes explicitement non vérifiées via API |
| Parcs masqués ordinaires restant à intégrer | 1 871 | 1 871 | Les deux parcs déplacés restent masqués |
| Classements hérités NotRelevant | 767 | 767 | Aucun classement récrit sans audit |
| Intégrations achevées avec audit | 204 | 204 | Aucun Apply / publication dans ce lot |

### Corrections confirmées de la file ZZ

| Fiche existante | Identifiant conservé | Pays documentaire | Source primaire |
| --- | --- | --- | --- |
| Xetulul | `ZZ-park-eaa15c7a-2544-4f36-99c7-17f3205ac685` | GT — Guatemala | [IRTRA](https://irtra.org.gt/parques/?pid=54) |
| Xejuyup | `ZZ-park-ac45c555-083f-41af-b99e-686ca20441a0` | GT — Guatemala | [IRTRA](https://irtra.org.gt/parques/?pid=93) |

Les quatre autres dossiers GT associés aux mêmes lieux sont désormais des traces `Duplicate` pointant vers ces identifiants stables. Les UUID de production et dates d'Apply n'ont pas changé. Une vérification réelle via `PARK_DATA_EDITOR` reste indispensable avant intégration ou correction des pays de production.

### Découverte locale dans des territoires sans observation

| Territoire | Dossier de qualification ajouté | Sources |
| --- | --- | --- |
| PF — Polynésie française | Rainbow Park Tahiti | [Exploitant](https://www.rainbowpark.pf/) · [Tahiti Tourisme](https://tahititourisme.pf/experiences/les-spots-a-ne-pas-manquer/accrobranche-rainbow-park-tahiti-pirae-fr-4300565/) |
| PF — Polynésie française | Tiki Parc Moorea | [Tahiti Tourisme](https://www.tahititourisme.fr/equipement/tiki-parc-moorea/) |
| MQ — Martinique | Mangofil | [Sud Martinique Tourisme](https://sud-martinique-tourisme.com/equipement/mangofil/) · [Martinique Tourisme](https://www.martinique.org/fr/que-faire/sports-et-decouvertes-terre/mangofil) |
| GP — Guadeloupe | Le Tapeur — Parc aventure des Mamelles | [Exploitant](https://www.letapeur.fr/) · [Zoo partenaire](https://www.zoodeguadeloupe.com/le-tapeur) |
| GP — Guadeloupe | Akropark | [Exploitant](https://www.akropark.fr/) |
| VU — Vanuatu | Eden on the River | [Exploitant](https://www.edenvanuatu.com/) · [Office national du tourisme](https://www.vanuatu.travel/en/eden-on-the-river) |

Ces six dossiers sont `ToQualify` / `PossibleExisting`, non des créations `NotFound` confirmées. Le type Park doit encore être arbitré face aux modèles `StandaloneAttraction` et `parkItem`. Leur présence est documentée ; la complétude du territoire ne l'est pas.

## P0 — Couverture à compléter

**78 territoires** restent sans observation retenue. Liste exhaustive par code ISO/Kosovo, calculée sur le résumé régénéré :

| Région | Territoires à documenter | Codes |
| --- | ---: | --- |
| Europe | 5 | VA, GG, AX, FO, SJ |
| Asie | 4 | BT, CX, CC, TL |
| Afrique | 28 | BJ, BF, BI, CV, KM, CG, DJ, ER, SZ, GM, GN, GQ, GW, LR, MR, YT, NA, NE, CF, EH, SH, ST, SC, SL, SS, TD, TF, IO |
| Amérique du Nord, centrale et Caraïbes | 19 | AI, AG, AW, BB, CW, DM, GD, GL, KY, TC, VG, VI, MS, BQ, BL, KN, MF, PM, VC |
| Amérique du Sud | 1 | FK |
| Océanie | 17 | NF, CK, MP, MH, UM, PN, SB, KI, FM, NR, NU, WS, AS, TK, TO, TV, WF |
| Territoires antarctiques | 4 | AQ, GS, BV, HM |

Pour chacun : sources nationales et locales, exploitants permanents, établissements ouverts ou historiques, critères d'exclusion, dates de consultation, éventuels alias et absence de doublon. Un simple annuaire RCDB vide ne suffit pas à clore le pays.

**69 dossiers ZZ** subsistent, dont 32 fiches Park masquées ordinaires et 37 entrées masquées NotRelevant. Les noms seuls (City Park, SkyPark, etc.) ne constituent pas une preuve de pays. Tout déplacement doit préserver `entryId`, `siteId`, `legacySiteIds`, les sources et les événements, puis régénérer le résumé et les vues nationales.

## P1 — Fiabilisation avant toute création

1. Réduire les **1 276** identités `PossibleExisting` par comparaison UUID, alias, adresse, coordonnées, ancien parc et rattachement `parkItems` ; ne pas fusionner sur le nom seulement.
2. Résoudre les **72** types de candidats non déterminés, et les pistes de requalification standalone déjà comptées dans leurs catégories d'origine.
3. Pour chaque candidat, consigner localisation, source primaire indépendante et, si possible, seconde famille de preuve ; vérifier le cycle de vie auprès du site officiel.
4. Les 7 pays représentatifs examinés à l'audit initial (FR/US/CN/DE/JP/BR/IN) comptaient **3 524 dossiers sur 3 929 avec une seule source enregistrée**. Cet indicateur est un échantillon, non une mesure globale.

## P2 — Qualifier puis intégrer, sans création en masse

1. Les **7 550** candidats Park `NotFound` sont des *pistes d'absence au relevé du 22 octobre* ; ils ne peuvent devenir une création sans recherche fraîche via le client officiel.
2. Traiter en priorité les **1 871** Park déjà masqués pertinents, ainsi que **767** NotRelevant à réexaminer seulement lorsqu'une nouvelle preuve de lieu fixe apparaît ; résoudre les doublons avant toute nouvelle création.
3. Les **120** candidates standalone sans fiche ne représentent pas toutes les installations autonomes mondiales. Reprendre aussi les sous-ensembles dans les Park existants et les dossiers parents incertains sans les additionner deux fois.
4. Seules les intégrations complètes avec vraie preuve d'Apply/import, audit final et autorisation de publication suivent le workflow de production. **Cette branche ne lance aucun Apply ni Facebook.**

## P3 — Suivi reproductible et fraîcheur

La commande de diagnostic ajoute des contrôles non destructifs : territoires sans observation, source en un seul domaine, localité absente, fraîcheur des dates de consultation, statut et identité incertains, collisions d'UUID, files P0/P1/P2 exportables.

~~~bash
node --test tools/codex/world-integration-quality.test.mjs
node tools/codex/world-integration-quality.mjs --format markdown --as-of 2026-11-15 --output /tmp/world-backlog-quality.md
node tools/codex/world-integration-quality.mjs --format csv --scope triage --as-of 2026-11-15 --output /tmp/world-backlog-triage.csv
node tools/codex/integration-backlog.mjs validate
~~~

Ces rapports sont des vues *calculées* sur les JSON, et ne prétendent pas qu'une nouvelle recherche Web ou un audit d'exploitation a eu lieu pour chacun des pays. Le script n'altère ni les historiques ni les compteurs du registre. Les vrais événements de révision nationale doivent être consignés avec sources primaires et date, avant qu'un pays soit qualifié de couvert.

## Travail restant explicitement ouvert

La campagne complète P0 → P3 ne peut être déclarée terminée sur la seule base de ces corrections : il reste 78 pays sans observation, 69 dossiers ZZ, 1 276 rapprochements incertains, 72 types à décider, des milliers de sources à contrôler et des milliers de sites à qualifier. Les tâches opérationnelles nécessitant le client `PARK_DATA_EDITOR` et les vérifications site par site restent distinctes de ce PR documentaire.
