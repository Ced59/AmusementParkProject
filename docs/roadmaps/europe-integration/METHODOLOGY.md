# Méthode, périmètre et limites du recensement

Relevé du **22 octobre 2026**, construit en lecture seule. Aucun parc, contenu, média ou publication Facebook n’a été modifié par cet inventaire.

## Ce qui a été mesuré

Le client officiel `PARK_DATA_EDITOR` a parcouru séquentiellement **60 pages de SearchParks, de 50 éléments au maximum : 2 983 identifiants uniques mondiaux**. Une erreur 500 sur la page 27 a été reprise après vérification du statut ; les 60 pages ont ensuite été contrôlées. Le total annoncé est resté stable. La recherche dédiée `SearchStandaloneAttractions` du client actuel a parcouru **2 pages, 62 identifiants uniques**. Les fiches masquées sont donc incluses, contrairement à une recherche publique.

Dans les 51 pays retenus : **1 832 fiches Park et 62 fiches StandaloneAttraction**. **18 Park** correspondent à des `legacyParkId` de standalone présentes : leurs traces restent enregistrées, mais sont séparées du total des parcs à intégrer. Les 62 standalone comptent 61 fiches visibles et une masquée ; les 1 832 Park comptent 224 fiches visibles et 1 608 masquées, dont les anciens parcs liés à une standalone. Les lignes `NotRelevant` sont séparées et ne sont pas assimilées automatiquement à des parcs à créer.

Les lectures sont une photographie séquentielle, pas une transaction gelée de production. Les campagnes France et Portugal peuvent continuer à intégrer après le relevé. Avant chaque création, rechercher de nouveau l’identité, ses alias et l’adresse dans les deux collections via le client officiel. Aucune date de création, d’export, de publication, de commit ou de revue n’est utilisée comme date de dernière intégration.

## Périmètre

- 45 pays européens au sens du tableau principal, France et Portugal inclus ; Chypre incluse. Les sources nationales peuvent inclure des territoires insulaires ou ultramarins : ce n’est pas un découpage géographique au niveau de chaque coordonnée.
- 6 pays dans une extension séparée : Arménie, Azerbaïdjan, Géorgie, Kazakhstan, Russie, Turquie. Leur recensement national inclut des lieux hors d’Europe géographique et ne doit pas être additionné silencieusement au total européen principal.
- Parcs d’attractions, à thème, aquatiques, familiaux, animaliers et aquariums permanents ; parcs d’aventure et offres indoor structurées à qualifier ; lieux disparus ou projets documentés conservés avec leur cycle de vie à établir.
- Attractions fixes autonomes séparées des parcs. Une luge de station peut relever d’une standalone ; une attraction dans un vrai parc demeure un parkItem. Un nom de domaine skiable ne suffit pas à créer un parc.
- Les forains, fabricants, expositions temporaires, piscines purement sportives, musées sans offre de parc et simples jardins ne deviennent pas automatiquement des parcs. Les candidats ambigus restent à qualifier.
- Les dépendances de la Couronne britannique (Jersey, Guernesey, île de Man), Gibraltar et autres territoires hors des 51 codes ne font pas l’objet de fiches nationales dans ce premier registre. Ce point reste une extension explicite à traiter, pas une preuve de couverture.

## Sources consultées

| Source | Usage et limite |
| --- | --- |
| [RCDB — pays](https://rcdb.com/location.htm) et toutes les pages nationales `r.htm?ot=3&ol=…` | 1 646 observations de lieux dans le périmètre, France et Portugal compris. Annuaire spécialisé actuel et historique, limité aux lieux associés à des montagnes russes ; son unité « park » peut être une attraction de station ou une exposition. |
| [EAZA — membres](https://www.eaza.net/membership/) | 338 établissements de catégorie Zoo/Aquarium dans les pays retenus. Nom, site officiel référencé et coordonnées facilitent le rapprochement. L’adhésion ne prouve pas à elle seule le statut d’exploitation courant. |
| [BIAZA — membres](https://biaza.org.uk/members/all) | 125 observations britanniques et irlandaises ; les établissements éducatifs restent à qualifier. Les dépendances hors périmètre sont séparées. |
| [Freizeitpark App — Allemagne](https://www.freizeitparkapp.de/en/parks/deutschland/) et [Suède](https://www.freizeitparkapp.de/en/parks/schweden/) | 488 observations retenues ; exclusions des marchés de Noël, fêtes foraines et hôtels seuls. Lieux aquatiques, animaliers et alpins inclus. |
| [Parcs aquatiques européens](https://en.wikipedia.org/wiki/List_of_water_parks_in_Europe), [parcs d’attractions européens](https://en.wikipedia.org/wiki/List_of_amusement_parks_in_Europe), [zoos par pays](https://en.wikipedia.org/wiki/List_of_zoos_by_country), [aquariums](https://en.wikipedia.org/wiki/List_of_aquaria) | Listes secondaires de découverte, jamais preuve suffisante d’ouverture ou d’identité ; pas de reprise de leurs descriptions. |
| [AmusementForce — waterparks](https://www.amusementforce.com/waterparks) | 91 observations européennes ; certains lieux sont fermés : vérification officielle indispensable. |
| Backlogs nationaux [France](../france-parks-integration-backlog-2026-09-29.md) et [Portugal](../portugal-parks-integration-backlog-2026-10-20.md) | 262 lignes actives rapprochées du nouveau relevé. Les anciennes lignes déjà retirées ne sont pas recréées comme travail restant lorsque la fiche publique ou standalone est retrouvée. Les autorisations propres à ces campagnes sont préservées. |
| Sources officielles complémentaires | Islande, Monaco, Saint-Marin, projet Universal britannique, opérateurs aquatiques ; URL conservée dans chaque dossier concerné. |

Chaque ligne conserve ses URL et la date de consultation. Les références « site officiel enregistré » sont des liens présents dans le site ou un annuaire : elles ne signifient pas que toutes leurs pages ont été auditées pendant cet inventaire. Les liens privés d’API, réponses brutes, jetons et archive fournie ne sont pas déposés dans GitHub.

## Rapprochement et chiffres

1. Dédupliquer les fiches du site par UUID ; séparer Park et StandaloneAttraction ; relier les anciens parcs par `legacyParkId`.
2. Normaliser accents, ponctuation et variantes de noms pour la recherche seulement ; préserver le nom d’origine. Recouper pays, localité, domaine officiel ou coordonnées quand disponibles. Des alias connus et des migrations françaises ont été rapprochés explicitement.
3. Les noms génériques et homonymes ne sont pas fusionnés sur le seul nom. Les rapprochements textuels incertains restent `PossibleExisting`, avec l’UUID et le nom à contrôler ; ils ne figurent pas dans les colonnes sans fiche.
4. Regrouper les observations externes qui désignent apparemment le même lieu. Conserver les différentes implantations d’une chaîne et les différentes identités RCDB tant qu’une fusion n’est pas prouvée.
5. Les dossiers externes non rapprochés restent `ToQualify`. La proposition Park/Standalone guide la recherche ; elle n’autorise pas une création automatique. Un parc fermé peut nécessiter une intégration historique, pas une fiche Operating.
6. Une fiche publiée est comptée comme déjà publiée, avec `PublishedDateUnknown`. Aucun appel individuel de complétude ni audit éditorial complet n’a été effectué pour ce recensement. Le compteur des intégrations avec preuve et audit dans le nouveau registre commence donc à zéro, sans nier le travail déjà réalisé.

**Ce recensement n’est pas un certificat d’exhaustivité absolue.** Il couvre entièrement les pages d’annuaires parcourues, mais les petits sites locaux, installations saisonnières, nouveaux projets et lieux sans présence dans ces sources peuvent manquer. Les noms, changements d’enseigne et périmètres aquatiques demandent encore une qualification individuelle. Les totaux des candidats sont une charge de recherche, pas le nombre certifié de créations à faire.

## Continuer le recensement

En priorité : résoudre les rapprochements incertains et les migrations, confronter les parcs masqués aux sources officielles, compléter les annuaires touristiques nationaux/régionaux pour les petits parcs, puis valider les candidats externes. Ajouter toute découverte avec un identifiant stable, ses sources et un état explicite. Ne jamais régénérer les JSON depuis une nouvelle photographie en écrasant le registre : fusionner par identifiant et préserver chaque entrée, date et événement. Un candidat doublon passe à `Duplicate` avec `duplicateOf`, sans suppression.
