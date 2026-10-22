# Méthode, périmètre et limites du recensement mondial

Relevé du **22 octobre 2026**, construit en lecture seule. Aucun parc, contenu, média ou publication Facebook n’a été modifié par cet inventaire. Ce document remplace la première photographie européenne ; les identifiants déjà introduits sont conservés.

## Correction standalone après la première version

Le premier rapport sous-représentait les attractions autonomes. La [vérification dédiée](STANDALONE-AUDIT.md) ajoute **1 167 observations** de RCDB et de constructeurs/opérateurs, ainsi que des compteurs explicites pour les pistes encore rangées dans Park et les identités non résolues. Le registre courant contient **12 207 lignes** et **13 751 observations externes** ; **168 pays/territoires ont des entrées** et **82 restent sans observation externe retenue**. Les nombres de la première photographie ci-dessous sont conservés comme référence historique ; le résumé vivant et le rapport corrigé prévalent pour planifier le travail.

La correction ajoute aussi un rattachement documentaire RO → PL pour Skrzyczne, vérifié dans la billetterie officielle. Le code initial et l’identifiant stable sont conservés ; aucune correction de production n’a été appliquée.

## Relevé du site

Le client officiel `PARK_DATA_EDITOR` a parcouru séquentiellement **60 pages de SearchParks**, de 50 éléments au maximum : **2 983 identifiants Park uniques dans le monde**. Une erreur 500 sur la page 27 a été reprise après vérification du statut ; les 60 pages ont ensuite été contrôlées et le total annoncé est resté stable. `SearchStandaloneAttractions` a parcouru **2 pages : 62 identifiants uniques**. Les fiches masquées sont incluses, contrairement à une recherche publique.

Les 2 983 Park comprennent **307 fiches visibles et 2 676 masquées**. Les 62 standalone comprennent **61 visibles et une masquée**. **18 Park** correspondent aux `legacyParkId` de standalone présentes : ces traces sont conservées comme doublons, sans gonfler les parcs à intégrer. Les autres fiches Park masquées se répartissent entre **1 891 fiches à intégrer** et **767 classements NotRelevant à examiner séparément**. Un classement NotRelevant peut désigner un forain ou une ancienne identité ; il n’est pas une preuve définitive d’absence de lieu pertinent.

**346 fiches Park n’avaient pas de pays renseigné.** Le rapprochement avec les annuaires propose un pays pour **275** d’entre elles : le nom distinctif, les variantes, la localité et les sources retenues sont documentés dans `countryResolution`. `countryCodeAtInventory` conserve la valeur initiale. Ce rattachement est une hypothèse de classement documentaire à confirmer, sans modification de production. Les **71 fiches non résolues** restent dans [ZZ](countries/ZZ.md), notamment des noms génériques, exploitants mobiles ou navires. Les compteurs européens corrigent ainsi la première photographie limitée aux codes pays déjà renseignés.

Ces lectures constituent une photographie séquentielle, pas une transaction gelée. France et Portugal peuvent continuer à intégrer après le relevé. Avant chaque création, rechercher à nouveau identité, alias, adresse et anciennes fiches dans les deux collections via le client officiel. Aucune date de création, d’export, de publication, de commit ou de revue ne remplace la date réelle d’intégration.

## Périmètre géographique et éditorial

- **250 pays et territoires** : les 249 codes ISO 3166-1, plus Kosovo (`XK`), et une file technique distincte `ZZ` pour les pays non résolus. Ce découpage sert au suivi et ne prend pas position sur les souverainetés.
- Toutes les régions sont représentées, y compris dépendances britanniques, outre-mer, territoires insulaires et antarctiques. Les pays transcontinentaux et du Caucase sont groupés séparément pour conserver la continuité du premier rapport européen. Les sources couvrent ces pays entiers ; chaque fiche n’appartient qu’à un registre.
- **167 pays/territoires ont des entrées**. **83 n’ont pas d’observation externe retenue** ; leur fiche signale le besoin de recherche nationale ou locale, ou le résultat vide de l’annuaire consulté. Un zéro ne signifie jamais « aucun parc » ni « pays terminé ».
- Parcs d’attractions, à thème, aquatiques, familiaux, animaliers, aquariums permanents ; offres indoor et parcs d’aventure structurés à qualifier. Les lieux disparus et projets documentés restent présents avec un cycle de vie à établir.
- Attractions fixes autonomes séparées des parcs : luges, grandes roues, simulateurs de vol et autres installations pertinentes. Une attraction située dans un vrai parc demeure un parkItem ; un nom de station ne prouve pas l’existence d’un parc autonome.
- Forains, fabricants, événements temporaires, piscines sportives, musées sans offre de parc et simples jardins ne deviennent pas automatiquement des parcs. Les cas ambigus restent à qualifier.

## Sources de la première photographie (compléments dans la vérification standalone)

**12 584 observations externes** ont été recueillies avant rapprochement et dédoublonnage. Une observation n’est pas un nouvel établissement : plusieurs sources peuvent désigner le même lieu, et certains lieux sont déjà présents dans le site.

| Source | Observations | Usage et limite |
| --- | ---: | --- |
| [RCDB — monde et pays](https://rcdb.com/location.htm) | 6 650 | Toutes les pages de résultats des pays identifiés dans l’annuaire mondial ; le total concorde avec ses 4 774 lieux existants et 1 876 disparus. Spécialisé dans les lieux associés à des montagnes russes ; « park » peut désigner une installation de station ou une exposition. |
| [Wikipédia — parcs](https://en.wikipedia.org/wiki/List_of_amusement_parks), [zoos](https://en.wikipedia.org/wiki/List_of_zoos_by_country), [aquariums](https://en.wikipedia.org/wiki/List_of_aquaria), [grandes roues](https://en.wikipedia.org/wiki/List_of_Ferris_wheels) | 3 816 | Listes de parcs d’attractions et aquatiques des cinq continents, zoos et aquariums mondiaux, 137 grandes roues fixes. Découverte uniquement ; les roues transportables ne sont pas retenues. L’autonomie des roues par rapport à un parc reste à vérifier. |
| [Freizeitpark App — Allemagne](https://www.freizeitparkapp.de/en/parks/deutschland/) et [Suède](https://www.freizeitparkapp.de/en/parks/schweden/) | 488 | Découverte complémentaire européenne ; exclusion des marchés de Noël, fêtes foraines et hôtels seuls. |
| [EAZA — membres](https://www.eaza.net/membership/) | 348 | Établissements Zoo/Aquarium avec pays identifiable ; liens officiels et coordonnées de l’annuaire. L’adhésion ne certifie pas l’ouverture actuelle. |
| [WAZA — membres institutionnels](https://www.waza.org/members/waza-members/) | 293 | Membres institutionnels mondiaux ; associations, sociétés et personnes exclues. |
| Backlogs [France](../france-parks-integration-backlog-2026-09-29.md) et [Portugal](../portugal-parks-integration-backlog-2026-10-20.md) | 262 | Lignes actives rapprochées du relevé ; les résultats déjà visibles ne sont pas recréés comme travail restant. Les autorisations nationales sont préservées. |
| [AZA — accréditations](https://www.aza.org/current-accreditation-list) | 252 | Établissements américains et internationaux, avec leur pays réel ; l’État américain Georgia n’est pas assimilé à la Géorgie. |
| [AmusementForce — waterparks](https://www.amusementforce.com/waterparks) | 175 | Découverte mondiale de parcs aquatiques ; certains lieux sont fermés. |
| JAZA — [zoos](https://www.jaza.jp/about-jaza/structure/list-zoo) et [aquariums](https://www.jaza.jp/about-jaza/structure/list-aquarium) | 141 | Noms japonais conservés, mais exclus des créations supposées : rapprocher les alias latins et les candidats déjà présents avant décision. Aucune donnée personnelle du personnel collectée. |
| [BIAZA — membres](https://biaza.org.uk/members/all) | 127 | Royaume-Uni, Irlande et dépendances pertinentes ; équipements éducatifs à qualifier. |
| LAGO et Tempo Livre, opérateurs officiels | 16 | Centres aquatiques à qualifier ; une piscine sportive n’est pas présumée parc aquatique. |
| Sites officiels d’attractions autonomes | 9 | London Eye, High Roller, Singapore Flyer, Ain Dubai, Wheel of Brisbane, SkyWheel Myrtle Beach et Flyover à Chicago, Vancouver, Las Vegas. URL conservée par entrée. |
| Autres sources officielles complémentaires | 7 | Notamment Islande, Monaco, Saint-Marin et projet Universal britannique. |

Chaque dossier conserve ses URL et la date de consultation. « Site officiel référencé » ou « enregistré » désigne un lien fourni par le site ou un annuaire, pas un audit complet de ce domaine. Les listes secondaires ne certifient ni identité ni ouverture ; leurs descriptions ne sont pas reprises. Les liens privés d’API, réponses brutes, jetons et archive fournie restent hors de GitHub.

## Rapprochement et lecture des résultats

1. Dédupliquer les fiches du site par UUID, distinguer Park et StandaloneAttraction et relier les anciens parcs par `legacyParkId`.
2. Normaliser accents, ponctuation et variantes pour la recherche seulement, en préservant les noms d’origine. Recouper pays, localité, domaine officiel ou coordonnées disponibles. Des alias connus et migrations françaises ont été rapprochés explicitement.
3. Ne pas fusionner des noms génériques ou homonymes sur le seul texte. Les rapprochements incertains restent `PossibleExisting` avec les cibles à contrôler. Ce statut couvre aussi les noms japonais non rapprochés et les grandes roues dont l’appartenance à un parc reste inconnue ; ces dossiers ne gonflent pas les colonnes « sans fiche ».
4. Regrouper les observations qui désignent apparemment le même lieu, en conservant les implantations d’une chaîne et les différentes identités RCDB tant qu’une fusion n’est pas prouvée.
5. Les dossiers externes non rapprochés restent `ToQualify`. La proposition Park/Standalone oriente la recherche et n’autorise aucune création automatique. Un parc fermé peut nécessiter une intégration historique, pas une fiche Operating.
6. Une fiche visible est comptée « publiée », avec `PublishedDateUnknown`. Aucun audit éditorial individuel de complétude n’a été effectué pour ce recensement. Le compteur des intégrations avec reçu et audit enregistrés commence donc à zéro, sans nier le travail antérieur.

Le registre initial conserve **11 776 lignes** : les **3 045 fiches du site**, **8 666 dossiers externes actifs** et **65 entrées candidates rapprochées et conservées comme doublons (64 issues de la première phase européenne et une japonaise)**. Parmi les dossiers externes actifs : **7 703 candidats parcs sans correspondance**, **46 candidates standalone sans correspondance**, **69 types à décider** et **848 identités ou rattachements incertains**. Les compteurs vivants sont dans le [résumé](README.md).

**Ce recensement mondial n’est pas un certificat d’exhaustivité absolue.** La couverture varie fortement : les petits établissements locaux, projets récents, loisirs sans montagnes russes et pays peu représentés dans ces annuaires demandent encore une recherche locale. Les nombres de candidats mesurent une charge de qualification, pas un nombre certifié de créations distinctes.

## Continuer sans perdre l’historique

Résoudre d’abord pays manquants, homonymes, rattachements à un parc et migrations ; confronter les fiches masquées aux sources officielles ; compléter ensuite les annuaires touristiques nationaux/régionaux et les candidats externes. Chaque fiche nationale expose sa couverture, ses sources et sa prochaine action.

Ajouter chaque découverte avec un identifiant stable et des sources datées. **Ne jamais régénérer les JSON depuis une photographie en écrasant le registre** : fusionner par identifiant et conserver chaque entrée, date et événement. Un doublon passe à `Duplicate` avec `duplicateOf`, sans suppression. Les [règles de suivi](WORKFLOW.md), le validateur et la CI protègent cette conservation.
