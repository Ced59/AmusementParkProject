# Vérification et correction du recensement standalone

Reprise du **22 octobre 2026**, après le constat d’un nombre de candidates mondiales trop faible. Cette correction porte sur le backlog documentaire : aucune attraction n’a été créée, migrée ou publiée dans le site.

## Ce qui était sous-estimé

Le relevé API comportait bien **62 fiches StandaloneAttraction : 61 publiées et une masquée**. Cette mesure de présence reste inchangée. En revanche, les **46 candidates sans correspondance** du premier rapport ne constituaient pas un inventaire mondial suffisant :

- la découverte s’appuyait surtout sur des annuaires de parcs ; les références constructeurs de luges, tyroliennes et autres installations fixes n’avaient pas été parcourues systématiquement ;
- la proposition de type reposait en partie sur le nom du lieu : une station, un domaine touristique ou un opérateur pouvait rester classé Park, même quand la cible éditoriale pertinente était une attraction autonome ;
- les identités incertaines et rattachements à un parc étaient exclus du compteur « sans fiche », sans compteur standalone de qualification qui rende ce travail visible.

Le contrôle RCDB a trouvé **505 mountain coasters dans 491 lieux, répartis entre 54 pays**. Ces 505 observations se rattachaient, avant correction, à 259 fiches Park masquées, 143 candidats Park sans correspondance, 18 observations dans des Park publiés, 58 dans des standalone publiées et 27 autres dossiers. Ces nombres comptent des **installations sources**, pas des fiches distinctes : un lieu peut contenir plusieurs installations.

## Résultat corrigé

| Mesure | Nombre | Sens |
| --- | ---: | --- |
| Standalone déjà publiées | 61 | Présence API ; complétude non réauditée ici |
| Standalone masquées existantes | 1 | Intégration éditoriale à reprendre |
| Candidates standalone sans correspondance trouvée | 182 | Identité et autonomie à confirmer avant création |
| Pistes standalone dans des Park masqués | 239 | Sous-ensemble des fiches Park ; examiner une requalification ou migration |
| Pistes standalone dans des Park publiés | 20 | Sous-ensemble des fiches Park publiées ; préserver la visibilité et qualifier le modèle |
| Dossiers sans ID du site : identité ou parent à résoudre | 658 | Peuvent recouper une fiche existante, un autre candidat ou une attraction interne |
| Dossiers rattachés à un vrai parc | 50 | Restent dans le travail de parkItems, exclus des créations standalone |

Les quatre catégories de recherche standalone représentent **1099 dossiers à examiner**. **Ce n’est pas 1099 attractions autonomes distinctes à créer.** Les rapprochements peuvent encore réduire ce nombre ; certains dossiers seront conservés comme doublons ou rattachés à un parc. Les 259 fiches Park et les 658 dossiers incertains sont déjà inclus dans les compteurs généraux correspondants : ne pas les additionner une seconde fois au total mondial.

Le registre mondial passe à **12 296 lignes conservées**, issues de **13 884 observations externes** au total. Il conserve toutes les 11 776 entrées de la première version, sans inventer de date d’intégration. La correction documentée de Skrzyczne de RO vers PL garde même son ancien `entryId` ; son candidat polonais devient un doublon conservé.

## Sources ajoutées et contrôle de couverture

| Source parcourue | Observations ajoutées | Contrôle et limite |
| --- | ---: | --- |
| [RCDB — Mountain Coaster](https://rcdb.com/r.htm?cs=279&ot=2) | 505 | 22 pages, 505 URL d’attraction uniques ; chaque parent retrouvé dans les registres. « Amusement Park » dans RCDB ne décide pas le modèle éditorial du site. |
| [Wiegand — Alpine Coaster](https://www.wiegandslide.com/en/products/alpine-coaster/references.html) | 352 | Toutes les listes nationales, nombres concordants avec le total affiché ; les références peuvent désigner une ville ou une modernisation. |
| [Wiegand — Summer Toboggan Run](https://www.wiegandslide.com/en/products/summer-toboggan-run/references.html) | 247 | Toutes les listes nationales ; la présence d’une luge ne prouve pas son autonomie ni son ouverture actuelle. |
| [Sunkid — projets Mountain Coaster](https://www.sunkidworld.com/en/products/mountain-coaster-20/) | 12 | Projets sélectionnés sur la page produit ; pas une prétention de couverture de toutes les installations Sunkid. |
| [Skyline Luge — destinations](https://www.skylineluge.com/) | 8 | Sites visitables présentés par l’opérateur ; hôtels et projet de Swansea exclus de cette liste d’exploitation. |
| [ZipRider — références](https://ziprider.com/) | 30 | 31 liens de lieux parcourus ; le système d’évacuation industriel de Cape Canaveral est exclu du périmètre visitable. Le fabricant annonce davantage d’installations que cette liste publique. |
| [Brogent — i-Ride](https://www.brogent.com/en/product-i-ride.html) | 6 | Chaque page projet lue ; Voletarium et Sky Voyager rattachés à leurs vrais parcs. |
| [Intamin — Super Gyro Tower](https://www.intamin.com/product/super-gyro-tower/) | 7 | Références et rattachements examinés ; Melaka est localisé en Malaisie par le [site officiel](https://www.menaratamingsari.com/), malgré une étiquette « China » incorrecte dans la liste constructeur. |
| [Aerophile — clients](https://www.aerophile.com/en/clients/) | 92 | 100 références lues ; huit événements temporaires, usages non visitables ou projets de durabilité incertaine écartés de cette collecte. Références historiques conservées comme telles ; aucune ouverture actuelle déduite. |
| Exploitants complémentaires | 19 | Flyover, Wings Over Washington, Zip World, Limite Zero, Donauturm, Euromast, A’DAM, 360 CHICAGO, XLine, Jais, Skyline et Skypark ; rapprochement avec les parents et fiches existantes. |
| ADG, archives Sunkid et exploitant Datanla | 22 | [Contrôle alpine / mountain coasters](ALPINE-COASTER-COVERAGE.md) : toutes rapprochées de dossiers existants, aucune création supplémentaire. |
| **Total ajouté avant rapprochement** | **1 300** | Ces listes se recoupent, notamment RCDB et Wiegand. |

Les 137 grandes roues fixes et les sources d’opérateurs déjà présentes dans la première version sont reprises dans les compteurs de qualification. Les roues transportables restent exclues. Une grande roue située dans un vrai parc n’est pas présumée autonome.

## Recherche de fiches entièrement absentes

La [liste des créations candidates](CREATIONS-STANDALONE.md) distingue les **182 dossiers standalone sans correspondance**, dont **63 dossiers d’alpine / mountain coasters** documentés par RCDB ou les constructeurs. Ce sont des sous-ensembles du backlog, pas des entrées à ajouter une seconde fois.

Le dernier complément de recherche a confronté 111 observations supplémentaires au relevé complet : **85 nouvelles entrées** et des preuves ajoutées aux dossiers déjà présents. Certaines références concernent des installations historiques ; elles ne sont pas présentées comme ouvertes aujourd’hui. Limite Zero, entre Espagne et Portugal, possède une seule entrée sous le pays de départ, signalée aussi dans le registre portugais. Les [contrôles de cette collecte](DISCOVERY-COVERAGE.md) conservent les motifs d’écartement et les limites.

## Rapprochements et règles

- Conserver chaque entrée préexistante. Ajouter les noms d’installations, URL, famille et éventuelles année/localité sources dans `standaloneAssessment.attractions` ; ne pas confondre ces données historiques avec une date d’Apply.
- Priorité aux liens exacts de parents RCDB, puis aux noms/alias non ambigus. Un nom de ville constructeur peut être rapproché si une unique installation concorde par localité **et** année ; cette méthode est conservée dans la preuve. Un domaine opérateur partagé ne suffit pas à fusionner plusieurs sites.
- Les références constructeur non résolues deviennent des dossiers `PossibleExisting`, avec leurs cibles possibles ; leur existence matérielle ne prouve ni une nouvelle identité distincte ni l’autonomie.
- `IndependentCandidate` propose une cible autonome à confirmer ; `ParentToResolve` conserve une question ouverte ; `InsidePark` exclut la création standalone ; `AlreadyStandalone` relie une fiche autonome déjà présente.
- Une intégration réelle résout le type avec le reçu ; le suivi conserve les références et retire les dossiers résolus de la file de requalification. Aucune migration API n’est autorisée par ce seul inventaire.

**Limite restante :** cette reprise corrige une sous-représentation mesurable, mais ne certifie pas toutes les standalone du monde. Les petites installations indépendantes, constructeurs absents de ces listes, tours, grandes roues locales, nouvelles ouvertures et offres fixes sans annuaire centralisé demandent encore une recherche nationale. Un zéro national n’est pas une preuve d’exhaustivité.

## Résumé pays par pays — standalone et qualification

Les tableaux complets de parcs et standalone restent dans [README.md](README.md). Ici figurent les pays avec au moins une fiche autonome ou un dossier de qualification.

| Pays | Standalone publiées | Masquées | Candidates sans correspondance | Dans Park masqués | Dans Park publiés | Sans ID : à résoudre | Dans un vrai parc |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| [Afrique du Sud](countries/ZA.md) | 0 | 0 | 0 | 1 | 0 | 1 | 0 |
| [Allemagne](countries/DE.md) | 16 | 0 | 24 | 31 | 3 | 78 | 10 |
| [Andorre](countries/AD.md) | 0 | 0 | 0 | 0 | 0 | 3 | 1 |
| [Arabie saoudite](countries/SA.md) | 0 | 0 | 2 | 0 | 0 | 4 | 0 |
| [Argentine](countries/AR.md) | 0 | 0 | 0 | 0 | 0 | 1 | 0 |
| [Australie](countries/AU.md) | 0 | 0 | 2 | 1 | 0 | 12 | 2 |
| [Autriche](countries/AT.md) | 1 | 0 | 2 | 32 | 1 | 21 | 1 |
| [Azerbaïdjan](countries/AZ.md) | 0 | 0 | 2 | 0 | 0 | 2 | 0 |
| [Bahamas](countries/BS.md) | 0 | 0 | 1 | 0 | 0 | 0 | 0 |
| [Bangladesh](countries/BD.md) | 0 | 0 | 0 | 0 | 0 | 1 | 0 |
| [Belgique](countries/BE.md) | 0 | 0 | 1 | 0 | 1 | 1 | 0 |
| [Biélorussie](countries/BY.md) | 0 | 0 | 0 | 3 | 0 | 3 | 0 |
| [Bosnie-Herzégovine](countries/BA.md) | 0 | 0 | 0 | 2 | 0 | 0 | 0 |
| [Brésil](countries/BR.md) | 0 | 0 | 0 | 1 | 0 | 10 | 1 |
| [Bulgarie](countries/BG.md) | 0 | 0 | 0 | 1 | 0 | 1 | 0 |
| [Cambodge](countries/KH.md) | 0 | 0 | 1 | 0 | 0 | 3 | 0 |
| [Canada](countries/CA.md) | 0 | 0 | 5 | 5 | 0 | 9 | 0 |
| [Chili](countries/CL.md) | 0 | 0 | 0 | 0 | 0 | 1 | 0 |
| [Chine](countries/CN.md) | 0 | 0 | 28 | 1 | 0 | 109 | 6 |
| [Corée du Sud](countries/KR.md) | 0 | 0 | 9 | 0 | 0 | 6 | 0 |
| [Espagne](countries/ES.md) | 0 | 0 | 1 | 2 | 0 | 4 | 3 |
| [Estonie](countries/EE.md) | 0 | 0 | 0 | 0 | 0 | 1 | 0 |
| [Finlande](countries/FI.md) | 0 | 0 | 0 | 1 | 0 | 7 | 0 |
| [France](countries/FR.md) | 40 | 0 | 11 | 0 | 6 | 21 | 8 |
| [Géorgie](countries/GE.md) | 0 | 0 | 2 | 0 | 0 | 3 | 0 |
| [Haïti](countries/HT.md) | 0 | 0 | 0 | 0 | 1 | 1 | 0 |
| [Hongrie](countries/HU.md) | 0 | 0 | 1 | 7 | 2 | 7 | 0 |
| [Indonésie](countries/ID.md) | 0 | 0 | 0 | 0 | 0 | 1 | 0 |
| [Irak](countries/IQ.md) | 0 | 0 | 1 | 0 | 0 | 3 | 0 |
| [Iran](countries/IR.md) | 0 | 0 | 3 | 0 | 0 | 12 | 0 |
| [Islande](countries/IS.md) | 0 | 0 | 1 | 0 | 0 | 0 | 0 |
| [Israël](countries/IL.md) | 0 | 0 | 0 | 0 | 0 | 4 | 0 |
| [Italie](countries/IT.md) | 2 | 0 | 2 | 20 | 2 | 18 | 0 |
| [Jamaïque](countries/JM.md) | 0 | 0 | 0 | 1 | 0 | 1 | 0 |
| [Japon](countries/JP.md) | 0 | 0 | 2 | 0 | 0 | 51 | 3 |
| [Jordanie](countries/JO.md) | 0 | 0 | 1 | 0 | 0 | 0 | 0 |
| [Kazakhstan](countries/KZ.md) | 0 | 0 | 0 | 0 | 0 | 2 | 0 |
| [Kenya](countries/KE.md) | 0 | 0 | 0 | 0 | 0 | 1 | 0 |
| [Lettonie](countries/LV.md) | 0 | 0 | 0 | 3 | 0 | 4 | 0 |
| [Liban](countries/LB.md) | 0 | 0 | 1 | 0 | 0 | 0 | 0 |
| [Lituanie](countries/LT.md) | 0 | 0 | 0 | 3 | 0 | 1 | 0 |
| [Malaisie](countries/MY.md) | 0 | 0 | 2 | 0 | 0 | 1 | 0 |
| [Maroc](countries/MA.md) | 0 | 0 | 0 | 0 | 0 | 0 | 1 |
| [Maurice](countries/MU.md) | 0 | 0 | 0 | 0 | 0 | 1 | 1 |
| [Mexique](countries/MX.md) | 0 | 0 | 1 | 0 | 0 | 6 | 1 |
| [Monténégro](countries/ME.md) | 0 | 0 | 1 | 1 | 0 | 1 | 0 |
| [Norvège](countries/NO.md) | 0 | 0 | 0 | 2 | 0 | 3 | 1 |
| [Nouvelle-Zélande](countries/NZ.md) | 0 | 0 | 1 | 0 | 0 | 2 | 0 |
| [Ouzbékistan](countries/UZ.md) | 0 | 0 | 0 | 0 | 0 | 2 | 0 |
| [Pays-Bas](countries/NL.md) | 0 | 0 | 3 | 1 | 0 | 5 | 0 |
| [Philippines](countries/PH.md) | 0 | 0 | 0 | 0 | 0 | 6 | 0 |
| [Pologne](countries/PL.md) | 0 | 0 | 2 | 17 | 0 | 21 | 0 |
| [Porto Rico](countries/PR.md) | 0 | 0 | 1 | 0 | 0 | 0 | 0 |
| [Portugal](countries/PT.md) | 2 | 1 | 1 | 0 | 0 | 0 | 1 |
| [R.A.S. chinoise de Hong Kong](countries/HK.md) | 0 | 0 | 0 | 0 | 0 | 2 | 0 |
| [R.A.S. chinoise de Macao](countries/MO.md) | 0 | 0 | 0 | 0 | 0 | 1 | 0 |
| [Roumanie](countries/RO.md) | 0 | 0 | 2 | 14 | 0 | 7 | 0 |
| [Royaume-Uni](countries/GB.md) | 0 | 0 | 3 | 3 | 0 | 19 | 0 |
| [Russie](countries/RU.md) | 0 | 0 | 1 | 1 | 0 | 12 | 0 |
| [République dominicaine](countries/DO.md) | 0 | 0 | 0 | 0 | 0 | 1 | 0 |
| [Saint-Martin (partie néerlandaise)](countries/SX.md) | 0 | 0 | 0 | 0 | 0 | 1 | 0 |
| [Serbie](countries/RS.md) | 0 | 0 | 0 | 2 | 0 | 3 | 0 |
| [Singapour](countries/SG.md) | 0 | 0 | 3 | 0 | 0 | 2 | 0 |
| [Slovaquie](countries/SK.md) | 0 | 0 | 1 | 3 | 0 | 3 | 0 |
| [Slovénie](countries/SI.md) | 0 | 0 | 0 | 4 | 0 | 1 | 0 |
| [Suisse](countries/CH.md) | 0 | 0 | 0 | 16 | 0 | 14 | 0 |
| [Suède](countries/SE.md) | 0 | 0 | 0 | 3 | 0 | 4 | 0 |
| [Syrie](countries/SY.md) | 0 | 0 | 1 | 0 | 0 | 0 | 0 |
| [Tadjikistan](countries/TJ.md) | 0 | 0 | 0 | 0 | 0 | 1 | 0 |
| [Taïwan](countries/TW.md) | 0 | 0 | 3 | 0 | 0 | 6 | 0 |
| [Tchéquie](countries/CZ.md) | 0 | 0 | 0 | 15 | 1 | 11 | 0 |
| [Thaïlande](countries/TH.md) | 0 | 0 | 0 | 0 | 0 | 4 | 0 |
| [Tunisie](countries/TN.md) | 0 | 0 | 0 | 0 | 0 | 1 | 0 |
| [Turkménistan](countries/TM.md) | 0 | 0 | 0 | 0 | 0 | 1 | 0 |
| [Turquie](countries/TR.md) | 0 | 0 | 1 | 10 | 0 | 8 | 1 |
| [Ukraine](countries/UA.md) | 0 | 0 | 0 | 1 | 0 | 2 | 0 |
| [Viêt Nam](countries/VN.md) | 0 | 0 | 4 | 1 | 0 | 16 | 4 |
| [Émirats arabes unis](countries/AE.md) | 0 | 0 | 4 | 1 | 0 | 6 | 0 |
| [États-Unis](countries/US.md) | 0 | 0 | 44 | 29 | 3 | 76 | 5 |

## Rapprochement de la campagne Portugal

La mise à jour nationale [#941](https://github.com/Ced59/AmusementParkProject/pull/941), publiée pendant cette recherche, est conservée : quatre dossiers supplémentaires, 837 événements de suivi, 12 dates réelles de dernière intégration et sept clôtures auditées. Le recensement lui-même n’a réalisé aucun Apply. Les dates inconnues des autres dossiers restent inconnues. Le registre consolidé contient 12 296 lignes.
