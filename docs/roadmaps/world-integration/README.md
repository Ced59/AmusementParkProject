# Inventaire mondial et backlog permanent — résumé par pays

Photographie initiale du 22 octobre 2026. **250 pays et territoires**, plus la file des pays non résolus ; **12297 lignes conservées**. France et Portugal inclus. Les JSON nationaux sont le registre permanent ; les vues et compteurs sont régénérés par `node tools/codex/integration-backlog.mjs render`.

**Lecture des chiffres :** « publiés » répond au repère des intégrations déjà visibles sur le site. Ce recensement n’a pas réaudité leur complétude et ne prétend pas connaître leur dernière date d’intégration. Les fiches masquées ordinaires nécessitent une intégration éditoriale complète. Les fiches masquées `NotRelevant` sont comptées séparément : beaucoup sont des exploitants forains ou des anciennes identités ; une source de vrai lieu peut rouvrir leur examen.

**Sans fiche = candidats sans correspondance trouvée dans le relevé**, pas créations validées. Confirmer les alias, l’adresse, le périmètre et le type avant création. Les rapprochements incertains et les types non résolus restent séparés. Les anciennes fiches déjà liées à une standalone ne gonflent pas la file des parcs.

Le recensement couvre les annuaires et sources décrits dans [la méthode et ses limites](METHODOLOGY.md). La couverture varie selon le pays : la colonne « observations externes » compte les références de découverte avant dédoublonnage, jamais des parcs supplémentaires. **Un zéro ne signifie ni absence de parcs ni pays terminé** ; les recherches locales restantes sont signalées dans chaque fiche. [Règles et commandes de suivi](WORKFLOW.md).

Compléments datés du relevé : [créations standalone candidates](CREATIONS-STANDALONE.md), [couverture des alpine / mountain coasters](ALPINE-COASTER-COVERAGE.md), [contrôle de la découverte externe](DISCOVERY-COVERAGE.md) et [prompts France / Portugal](PROMPTS-FR-PT.md). Le nombre de créations candidates ne représente pas le nombre mondial d’installations. Les JSON et les compteurs régénérés ci-dessous prévalent après les intégrations ultérieures.

**Pays manquants dans la base :** 275 fiches sont rattachées ici par rapprochement avec des sources, à confirmer pendant leur reprise. Ce classement documentaire ne modifie pas leur pays dans le site. 1 autre(s) fiche(s) ont un code pays renseigné mais corrigé ici sur source, avec leur identifiant stable conservé. Les cas non résolus restent dans [ZZ](countries/ZZ.md). Les compteurs européens corrigent donc la première photographie qui se limitait aux codes pays renseignés.

## Totaux mondiaux

| Type | Publiés¹ | Fiches masquées à intégrer | Candidats sans fiche |
| --- | ---: | ---: | ---: |
| Parcs | 307 | 1891 | 7566 |
| Standalone | 76 | 1 | 168 |

**Reprise standalone :** en plus des 168 candidates sans correspondance, **238 fiches Park masquées** et **20 fiches Park publiées** contiennent une piste standalone à requalifier ; **658 dossiers sans identifiant du site** demandent encore un rapprochement ou une décision sur leur rattachement à un parc. Les fiches Park sont des sous-ensembles des compteurs Park, les dossiers incertains un sous-ensemble des rapprochements : ne pas les additionner deux fois. **50 dossiers rattachés à un parc structuré** restent hors des créations standalone. [Vérification dédiée, sources et limites](STANDALONE-AUDIT.md).

À part : **767** Park masqués NotRelevant, **69** types à décider, **1367** rapprochements incertains et **18** anciens parcs déjà liés à une standalone. Ces catégories ne sont pas additionnées au nombre de créations certaines.

## Europe

### Parcs

| Pays / territoire | Publiés¹ | Masqués à intégrer | Masqués NotRelevant à réexaminer | Sans fiche : candidats parcs | Observations externes |
| --- | ---: | ---: | ---: | ---: | ---: |
| [Albanie](countries/AL.md) | 0 | 2 | 0 | 5 | 8 |
| [Allemagne](countries/DE.md) | 36 | 132 | 92 | 352 | 1057 |
| [Andorre](countries/AD.md) | 0 | 1 | 0 | 0 | 6 |
| [Autriche](countries/AT.md) | 2 | 44 | 7 | 38 | 185 |
| [Belgique](countries/BE.md) | 27 | 0 | 15 | 38 | 84 |
| [Biélorussie](countries/BY.md) | 0 | 11 | 0 | 5 | 25 |
| [Bosnie-Herzégovine](countries/BA.md) | 0 | 2 | 0 | 3 | 10 |
| [Bulgarie](countries/BG.md) | 0 | 6 | 2 | 19 | 31 |
| [Chypre](countries/CY.md) | 0 | 6 | 0 | 13 | 24 |
| [Croatie](countries/HR.md) | 0 | 5 | 7 | 20 | 24 |
| [Danemark](countries/DK.md) | 1 | 18 | 4 | 36 | 75 |
| [Espagne](countries/ES.md) | 1 | 41 | 89 | 90 | 170 |
| [Estonie](countries/EE.md) | 0 | 3 | 0 | 9 | 14 |
| [État de la Cité du Vatican](countries/VA.md) | 0 | 0 | 0 | 0 | 0 |
| [Finlande](countries/FI.md) | 0 | 12 | 5 | 26 | 61 |
| [France](countries/FR.md) | 94 | 31 | 101 | 195 | 616 |
| [Gibraltar](countries/GI.md) | 0 | 0 | 0 | 1 | 1 |
| [Grèce](countries/GR.md) | 0 | 12 | 3 | 23 | 45 |
| [Guernesey](countries/GG.md) | 0 | 0 | 0 | 0 | 0 |
| [Hongrie](countries/HU.md) | 2 | 15 | 12 | 53 | 102 |
| [Île de Man](countries/IM.md) | 0 | 0 | 0 | 1 | 1 |
| [Îles Åland](countries/AX.md) | 0 | 0 | 0 | 0 | 0 |
| [Îles Féroé](countries/FO.md) | 0 | 0 | 0 | 0 | 0 |
| [Irlande](countries/IE.md) | 0 | 7 | 1 | 16 | 34 |
| [Islande](countries/IS.md) | 0 | 0 | 0 | 2 | 6 |
| [Italie](countries/IT.md) | 5 | 99 | 139 | 81 | 280 |
| [Jersey](countries/JE.md) | 0 | 0 | 0 | 1 | 1 |
| [Kosovo](countries/XK.md) | 0 | 1 | 1 | 4 | 6 |
| [Lettonie](countries/LV.md) | 1 | 3 | 0 | 6 | 19 |
| [Liechtenstein](countries/LI.md) | 0 | 0 | 0 | 0 | 2 |
| [Lituanie](countries/LT.md) | 0 | 5 | 0 | 8 | 19 |
| [Luxembourg](countries/LU.md) | 2 | 0 | 0 | 2 | 4 |
| [Macédoine du Nord](countries/MK.md) | 0 | 0 | 0 | 5 | 5 |
| [Malte](countries/MT.md) | 0 | 1 | 0 | 4 | 6 |
| [Moldavie](countries/MD.md) | 0 | 3 | 0 | 3 | 7 |
| [Monaco](countries/MC.md) | 0 | 0 | 0 | 2 | 4 |
| [Monténégro](countries/ME.md) | 0 | 1 | 0 | 1 | 5 |
| [Norvège](countries/NO.md) | 0 | 8 | 4 | 16 | 41 |
| [Pays-Bas](countries/NL.md) | 18 | 14 | 37 | 63 | 149 |
| [Pologne](countries/PL.md) | 1 | 60 | 34 | 116 | 251 |
| [Portugal](countries/PT.md) | 5 | 8 | 10 | 106 | 176 |
| [Roumanie](countries/RO.md) | 1 | 23 | 2 | 36 | 95 |
| [Royaume-Uni](countries/GB.md) | 9 | 90 | 26 | 252 | 459 |
| [Saint-Marin](countries/SM.md) | 0 | 0 | 0 | 1 | 1 |
| [Serbie](countries/RS.md) | 1 | 4 | 0 | 19 | 30 |
| [Slovaquie](countries/SK.md) | 0 | 9 | 5 | 21 | 36 |
| [Slovénie](countries/SI.md) | 0 | 4 | 1 | 14 | 25 |
| [Suède](countries/SE.md) | 0 | 21 | 12 | 39 | 111 |
| [Suisse](countries/CH.md) | 1 | 27 | 14 | 31 | 108 |
| [Svalbard et Jan Mayen](countries/SJ.md) | 0 | 0 | 0 | 0 | 0 |
| [Tchéquie](countries/CZ.md) | 1 | 25 | 48 | 46 | 114 |
| [Ukraine](countries/UA.md) | 1 | 33 | 1 | 42 | 72 |
| **Total** | **209** | **787** | **672** | **1864** | **4605** |

### Attractions standalone et dossiers à trancher

| Pays | Standalone publiées¹ | Standalone masquées | Sans fiche : candidates standalone | Sans fiche : type à décider | Rapprochements incertains² | Anciens parcs liés à une standalone |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| [Albanie](countries/AL.md) | 0 | 0 | 0 | 0 | 1 | 0 |
| [Allemagne](countries/DE.md) | 16 | 0 | 24 | 48 | 86 | 15 |
| [Andorre](countries/AD.md) | 0 | 0 | 0 | 0 | 3 | 0 |
| [Autriche](countries/AT.md) | 1 | 0 | 1 | 0 | 21 | 1 |
| [Belgique](countries/BE.md) | 0 | 0 | 1 | 1 | 1 | 0 |
| [Biélorussie](countries/BY.md) | 0 | 0 | 0 | 0 | 5 | 0 |
| [Bosnie-Herzégovine](countries/BA.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Bulgarie](countries/BG.md) | 0 | 0 | 0 | 0 | 2 | 0 |
| [Chypre](countries/CY.md) | 0 | 0 | 0 | 0 | 2 | 0 |
| [Croatie](countries/HR.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Danemark](countries/DK.md) | 0 | 0 | 0 | 0 | 1 | 0 |
| [Espagne](countries/ES.md) | 0 | 0 | 1 | 0 | 6 | 0 |
| [Estonie](countries/EE.md) | 0 | 0 | 0 | 0 | 1 | 0 |
| [État de la Cité du Vatican](countries/VA.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Finlande](countries/FI.md) | 0 | 0 | 0 | 0 | 7 | 0 |
| [France](countries/FR.md) | 40 | 0 | 11 | 1 | 23 | 0 |
| [Gibraltar](countries/GI.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Grèce](countries/GR.md) | 0 | 0 | 0 | 0 | 1 | 0 |
| [Guernesey](countries/GG.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Hongrie](countries/HU.md) | 0 | 0 | 1 | 0 | 7 | 0 |
| [Île de Man](countries/IM.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Îles Åland](countries/AX.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Îles Féroé](countries/FO.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Irlande](countries/IE.md) | 0 | 0 | 0 | 0 | 1 | 0 |
| [Islande](countries/IS.md) | 0 | 0 | 1 | 2 | 0 | 0 |
| [Italie](countries/IT.md) | 2 | 0 | 2 | 1 | 23 | 2 |
| [Jersey](countries/JE.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Kosovo](countries/XK.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Lettonie](countries/LV.md) | 0 | 0 | 0 | 0 | 4 | 0 |
| [Liechtenstein](countries/LI.md) | 0 | 0 | 0 | 2 | 0 | 0 |
| [Lituanie](countries/LT.md) | 0 | 0 | 0 | 0 | 1 | 0 |
| [Luxembourg](countries/LU.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Macédoine du Nord](countries/MK.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Malte](countries/MT.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Moldavie](countries/MD.md) | 0 | 0 | 0 | 0 | 1 | 0 |
| [Monaco](countries/MC.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Monténégro](countries/ME.md) | 0 | 0 | 1 | 0 | 1 | 0 |
| [Norvège](countries/NO.md) | 0 | 0 | 0 | 0 | 3 | 0 |
| [Pays-Bas](countries/NL.md) | 0 | 0 | 3 | 0 | 6 | 0 |
| [Pologne](countries/PL.md) | 0 | 0 | 2 | 0 | 25 | 0 |
| [Portugal](countries/PT.md) | 2 | 1 | 1 | 0 | 0 | 0 |
| [Roumanie](countries/RO.md) | 1 | 0 | 1 | 0 | 7 | 0 |
| [Royaume-Uni](countries/GB.md) | 2 | 0 | 2 | 6 | 31 | 0 |
| [Saint-Marin](countries/SM.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Serbie](countries/RS.md) | 0 | 0 | 0 | 0 | 4 | 0 |
| [Slovaquie](countries/SK.md) | 0 | 0 | 1 | 0 | 3 | 0 |
| [Slovénie](countries/SI.md) | 0 | 0 | 0 | 0 | 1 | 0 |
| [Suède](countries/SE.md) | 1 | 0 | 0 | 3 | 6 | 0 |
| [Suisse](countries/CH.md) | 0 | 0 | 0 | 0 | 14 | 0 |
| [Svalbard et Jan Mayen](countries/SJ.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Tchéquie](countries/CZ.md) | 0 | 0 | 0 | 0 | 13 | 0 |
| [Ukraine](countries/UA.md) | 0 | 0 | 0 | 0 | 4 | 0 |
| **Total** | **65** | **1** | **53** | **64** | **315** | **18** |

Intégrations achevées avec preuve et audit enregistrés depuis la création du registre : **14**. Fiches créées depuis le relevé dont la visibilité reste à contrôler : **0**.

- France : 3 intégrations achevées et auditées, dont 0 fiches encore masquées.
- Portugal : 7 intégrations achevées et auditées, dont 0 fiches encore masquées.
- Roumanie : 1 intégration achevée et auditée, dont 0 fiches encore masquées.
- Royaume-Uni : 2 intégrations achevées et auditées, dont 0 fiches encore masquées.
- Suède : 1 intégration achevée et auditée, dont 0 fiches encore masquées.
### Pistes standalone à requalifier

| Pays | Dans une fiche Park masquée | Dans une fiche Park publiée | Sans ID : identité / parent à résoudre² | Dossiers rattachés à un vrai parc |
| --- | ---: | ---: | ---: | ---: |
| [Albanie](countries/AL.md) | 0 | 0 | 0 | 0 |
| [Allemagne](countries/DE.md) | 31 | 3 | 78 | 10 |
| [Andorre](countries/AD.md) | 0 | 0 | 3 | 1 |
| [Autriche](countries/AT.md) | 32 | 1 | 21 | 1 |
| [Belgique](countries/BE.md) | 0 | 1 | 1 | 0 |
| [Biélorussie](countries/BY.md) | 3 | 0 | 3 | 0 |
| [Bosnie-Herzégovine](countries/BA.md) | 2 | 0 | 0 | 0 |
| [Bulgarie](countries/BG.md) | 1 | 0 | 1 | 0 |
| [Chypre](countries/CY.md) | 0 | 0 | 0 | 0 |
| [Croatie](countries/HR.md) | 0 | 0 | 0 | 0 |
| [Danemark](countries/DK.md) | 0 | 0 | 0 | 0 |
| [Espagne](countries/ES.md) | 2 | 0 | 4 | 3 |
| [Estonie](countries/EE.md) | 0 | 0 | 1 | 0 |
| [État de la Cité du Vatican](countries/VA.md) | 0 | 0 | 0 | 0 |
| [Finlande](countries/FI.md) | 1 | 0 | 7 | 0 |
| [France](countries/FR.md) | 0 | 6 | 21 | 8 |
| [Gibraltar](countries/GI.md) | 0 | 0 | 0 | 0 |
| [Grèce](countries/GR.md) | 0 | 0 | 0 | 0 |
| [Guernesey](countries/GG.md) | 0 | 0 | 0 | 0 |
| [Hongrie](countries/HU.md) | 7 | 2 | 7 | 0 |
| [Île de Man](countries/IM.md) | 0 | 0 | 0 | 0 |
| [Îles Åland](countries/AX.md) | 0 | 0 | 0 | 0 |
| [Îles Féroé](countries/FO.md) | 0 | 0 | 0 | 0 |
| [Irlande](countries/IE.md) | 0 | 0 | 0 | 0 |
| [Islande](countries/IS.md) | 0 | 0 | 0 | 0 |
| [Italie](countries/IT.md) | 20 | 2 | 18 | 0 |
| [Jersey](countries/JE.md) | 0 | 0 | 0 | 0 |
| [Kosovo](countries/XK.md) | 0 | 0 | 0 | 0 |
| [Lettonie](countries/LV.md) | 3 | 0 | 4 | 0 |
| [Liechtenstein](countries/LI.md) | 0 | 0 | 0 | 0 |
| [Lituanie](countries/LT.md) | 3 | 0 | 1 | 0 |
| [Luxembourg](countries/LU.md) | 0 | 0 | 0 | 0 |
| [Macédoine du Nord](countries/MK.md) | 0 | 0 | 0 | 0 |
| [Malte](countries/MT.md) | 0 | 0 | 0 | 0 |
| [Moldavie](countries/MD.md) | 0 | 0 | 0 | 0 |
| [Monaco](countries/MC.md) | 0 | 0 | 0 | 0 |
| [Monténégro](countries/ME.md) | 1 | 0 | 1 | 0 |
| [Norvège](countries/NO.md) | 2 | 0 | 3 | 1 |
| [Pays-Bas](countries/NL.md) | 1 | 0 | 5 | 0 |
| [Pologne](countries/PL.md) | 17 | 0 | 21 | 0 |
| [Portugal](countries/PT.md) | 0 | 0 | 0 | 1 |
| [Roumanie](countries/RO.md) | 14 | 0 | 7 | 0 |
| [Royaume-Uni](countries/GB.md) | 2 | 0 | 19 | 0 |
| [Saint-Marin](countries/SM.md) | 0 | 0 | 0 | 0 |
| [Serbie](countries/RS.md) | 2 | 0 | 3 | 0 |
| [Slovaquie](countries/SK.md) | 3 | 0 | 3 | 0 |
| [Slovénie](countries/SI.md) | 4 | 0 | 1 | 0 |
| [Suède](countries/SE.md) | 3 | 0 | 4 | 0 |
| [Suisse](countries/CH.md) | 16 | 0 | 14 | 0 |
| [Svalbard et Jan Mayen](countries/SJ.md) | 0 | 0 | 0 | 0 |
| [Tchéquie](countries/CZ.md) | 15 | 1 | 11 | 0 |
| [Ukraine](countries/UA.md) | 1 | 0 | 2 | 0 |
| **Total** | **186** | **16** | **264** | **25** |

Les deux colonnes de fiches Park sont des sous-ensembles des totaux Park ; la colonne sans ID est un sous-ensemble des rapprochements incertains. Ce sont des dossiers à examiner, pas un nombre garanti d’attractions autonomes distinctes.

## Pays transcontinentaux et Caucase

Périmètre national entier des sources. Cette rubrique conserve la distinction géographique introduite dans le rapport européen et évite tout double comptage entre continents.

### Parcs

| Pays / territoire | Publiés¹ | Masqués à intégrer | Masqués NotRelevant à réexaminer | Sans fiche : candidats parcs | Observations externes |
| --- | ---: | ---: | ---: | ---: | ---: |
| [Arménie](countries/AM.md) | 0 | 1 | 0 | 8 | 12 |
| [Azerbaïdjan](countries/AZ.md) | 0 | 0 | 0 | 14 | 21 |
| [Géorgie](countries/GE.md) | 0 | 3 | 0 | 13 | 27 |
| [Kazakhstan](countries/KZ.md) | 0 | 1 | 0 | 46 | 55 |
| [Russie](countries/RU.md) | 3 | 42 | 1 | 201 | 289 |
| [Turquie](countries/TR.md) | 12 | 140 | 7 | 50 | 242 |
| **Total** | **15** | **187** | **8** | **332** | **646** |

### Attractions standalone et dossiers à trancher

| Pays | Standalone publiées¹ | Standalone masquées | Sans fiche : candidates standalone | Sans fiche : type à décider | Rapprochements incertains² | Anciens parcs liés à une standalone |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| [Arménie](countries/AM.md) | 0 | 0 | 0 | 0 | 1 | 0 |
| [Azerbaïdjan](countries/AZ.md) | 2 | 0 | 0 | 0 | 2 | 0 |
| [Géorgie](countries/GE.md) | 0 | 0 | 2 | 0 | 4 | 0 |
| [Kazakhstan](countries/KZ.md) | 0 | 0 | 0 | 0 | 3 | 0 |
| [Russie](countries/RU.md) | 0 | 0 | 1 | 0 | 31 | 0 |
| [Turquie](countries/TR.md) | 0 | 0 | 1 | 0 | 17 | 0 |
| **Total** | **2** | **0** | **4** | **0** | **58** | **0** |

Intégrations achevées avec preuve et audit enregistrés depuis la création du registre : **2**. Fiches créées depuis le relevé dont la visibilité reste à contrôler : **0**.

- Azerbaïdjan : 2 intégrations achevées et auditées, dont 0 fiches encore masquées.
### Pistes standalone à requalifier

| Pays | Dans une fiche Park masquée | Dans une fiche Park publiée | Sans ID : identité / parent à résoudre² | Dossiers rattachés à un vrai parc |
| --- | ---: | ---: | ---: | ---: |
| [Arménie](countries/AM.md) | 0 | 0 | 0 | 0 |
| [Azerbaïdjan](countries/AZ.md) | 0 | 0 | 2 | 0 |
| [Géorgie](countries/GE.md) | 0 | 0 | 3 | 0 |
| [Kazakhstan](countries/KZ.md) | 0 | 0 | 2 | 0 |
| [Russie](countries/RU.md) | 1 | 0 | 12 | 0 |
| [Turquie](countries/TR.md) | 10 | 0 | 8 | 1 |
| **Total** | **11** | **0** | **27** | **1** |

Les deux colonnes de fiches Park sont des sous-ensembles des totaux Park ; la colonne sans ID est un sous-ensemble des rapprochements incertains. Ce sont des dossiers à examiner, pas un nombre garanti d’attractions autonomes distinctes.

## Asie

### Parcs

| Pays / territoire | Publiés¹ | Masqués à intégrer | Masqués NotRelevant à réexaminer | Sans fiche : candidats parcs | Observations externes |
| --- | ---: | ---: | ---: | ---: | ---: |
| [Afghanistan](countries/AF.md) | 0 | 0 | 0 | 4 | 5 |
| [Arabie saoudite](countries/SA.md) | 4 | 33 | 1 | 92 | 166 |
| [Bahreïn](countries/BH.md) | 2 | 5 | 0 | 3 | 11 |
| [Bangladesh](countries/BD.md) | 0 | 0 | 0 | 28 | 33 |
| [Bhoutan](countries/BT.md) | 0 | 0 | 0 | 0 | 0 |
| [Brunei](countries/BN.md) | 0 | 0 | 0 | 3 | 4 |
| [Cambodge](countries/KH.md) | 0 | 1 | 0 | 12 | 18 |
| [Chine](countries/CN.md) | 4 | 201 | 3 | 1247 | 1898 |
| [Corée du Nord](countries/KP.md) | 0 | 1 | 0 | 6 | 11 |
| [Corée du Sud](countries/KR.md) | 0 | 12 | 0 | 67 | 112 |
| [Émirats arabes unis](countries/AE.md) | 4 | 25 | 3 | 53 | 117 |
| [Île Christmas](countries/CX.md) | 0 | 0 | 0 | 0 | 0 |
| [Îles Cocos](countries/CC.md) | 0 | 0 | 0 | 0 | 0 |
| [Inde](countries/IN.md) | 3 | 9 | 0 | 181 | 234 |
| [Indonésie](countries/ID.md) | 2 | 9 | 0 | 131 | 191 |
| [Irak](countries/IQ.md) | 1 | 3 | 0 | 57 | 72 |
| [Iran](countries/IR.md) | 0 | 3 | 0 | 88 | 121 |
| [Israël](countries/IL.md) | 0 | 3 | 1 | 45 | 61 |
| [Japon](countries/JP.md) | 7 | 66 | 1 | 200 | 507 |
| [Jordanie](countries/JO.md) | 0 | 0 | 0 | 30 | 31 |
| [Kirghizstan](countries/KG.md) | 0 | 0 | 0 | 12 | 12 |
| [Koweït](countries/KW.md) | 0 | 8 | 0 | 15 | 26 |
| [Laos](countries/LA.md) | 0 | 0 | 0 | 3 | 3 |
| [Liban](countries/LB.md) | 0 | 3 | 0 | 17 | 23 |
| [Malaisie](countries/MY.md) | 0 | 4 | 0 | 64 | 86 |
| [Maldives](countries/MV.md) | 0 | 0 | 0 | 1 | 1 |
| [Mongolie](countries/MN.md) | 0 | 0 | 0 | 2 | 2 |
| [Myanmar (Birmanie)](countries/MM.md) | 0 | 1 | 0 | 11 | 14 |
| [Népal](countries/NP.md) | 0 | 0 | 0 | 9 | 12 |
| [Oman](countries/OM.md) | 0 | 1 | 0 | 15 | 17 |
| [Ouzbékistan](countries/UZ.md) | 2 | 1 | 0 | 49 | 57 |
| [Pakistan](countries/PK.md) | 1 | 0 | 0 | 29 | 34 |
| [Philippines](countries/PH.md) | 1 | 4 | 0 | 75 | 99 |
| [Qatar](countries/QA.md) | 0 | 3 | 0 | 13 | 17 |
| [R.A.S. chinoise de Hong Kong](countries/HK.md) | 0 | 0 | 0 | 11 | 17 |
| [R.A.S. chinoise de Macao](countries/MO.md) | 0 | 0 | 0 | 2 | 3 |
| [Singapour](countries/SG.md) | 0 | 2 | 1 | 23 | 37 |
| [Sri Lanka](countries/LK.md) | 0 | 1 | 0 | 13 | 14 |
| [Syrie](countries/SY.md) | 0 | 0 | 0 | 7 | 9 |
| [Tadjikistan](countries/TJ.md) | 0 | 0 | 0 | 5 | 8 |
| [Taïwan](countries/TW.md) | 0 | 6 | 0 | 36 | 65 |
| [Territoires palestiniens](countries/PS.md) | 0 | 1 | 0 | 12 | 13 |
| [Thaïlande](countries/TH.md) | 0 | 3 | 0 | 134 | 161 |
| [Timor oriental](countries/TL.md) | 0 | 0 | 0 | 0 | 0 |
| [Turkménistan](countries/TM.md) | 0 | 0 | 0 | 11 | 12 |
| [Viêt Nam](countries/VN.md) | 3 | 16 | 2 | 72 | 143 |
| [Yémen](countries/YE.md) | 0 | 0 | 0 | 9 | 13 |
| **Total** | **34** | **425** | **12** | **2897** | **4490** |

### Attractions standalone et dossiers à trancher

| Pays | Standalone publiées¹ | Standalone masquées | Sans fiche : candidates standalone | Sans fiche : type à décider | Rapprochements incertains² | Anciens parcs liés à une standalone |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| [Afghanistan](countries/AF.md) | 0 | 0 | 0 | 0 | 1 | 0 |
| [Arabie saoudite](countries/SA.md) | 0 | 0 | 2 | 0 | 21 | 0 |
| [Bahreïn](countries/BH.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Bangladesh](countries/BD.md) | 0 | 0 | 0 | 0 | 2 | 0 |
| [Bhoutan](countries/BT.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Brunei](countries/BN.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Cambodge](countries/KH.md) | 0 | 0 | 1 | 0 | 4 | 0 |
| [Chine](countries/CN.md) | 0 | 0 | 28 | 0 | 333 | 0 |
| [Corée du Nord](countries/KP.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Corée du Sud](countries/KR.md) | 0 | 0 | 9 | 0 | 11 | 0 |
| [Émirats arabes unis](countries/AE.md) | 0 | 0 | 4 | 0 | 11 | 0 |
| [Île Christmas](countries/CX.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Îles Cocos](countries/CC.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Inde](countries/IN.md) | 0 | 0 | 0 | 0 | 3 | 0 |
| [Indonésie](countries/ID.md) | 0 | 0 | 0 | 0 | 34 | 0 |
| [Irak](countries/IQ.md) | 0 | 0 | 1 | 0 | 7 | 0 |
| [Iran](countries/IR.md) | 0 | 0 | 3 | 0 | 14 | 0 |
| [Israël](countries/IL.md) | 0 | 0 | 0 | 0 | 5 | 0 |
| [Japon](countries/JP.md) | 0 | 0 | 2 | 0 | 204 | 0 |
| [Jordanie](countries/JO.md) | 0 | 0 | 1 | 0 | 0 | 0 |
| [Kirghizstan](countries/KG.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Koweït](countries/KW.md) | 0 | 0 | 0 | 0 | 2 | 0 |
| [Laos](countries/LA.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Liban](countries/LB.md) | 0 | 0 | 1 | 0 | 2 | 0 |
| [Malaisie](countries/MY.md) | 1 | 0 | 1 | 0 | 2 | 0 |
| [Maldives](countries/MV.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Mongolie](countries/MN.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Myanmar (Birmanie)](countries/MM.md) | 0 | 0 | 0 | 0 | 2 | 0 |
| [Népal](countries/NP.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Oman](countries/OM.md) | 0 | 0 | 0 | 0 | 1 | 0 |
| [Ouzbékistan](countries/UZ.md) | 0 | 0 | 0 | 0 | 3 | 0 |
| [Pakistan](countries/PK.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Philippines](countries/PH.md) | 0 | 0 | 0 | 0 | 7 | 0 |
| [Qatar](countries/QA.md) | 0 | 0 | 0 | 0 | 1 | 0 |
| [R.A.S. chinoise de Hong Kong](countries/HK.md) | 0 | 0 | 0 | 0 | 2 | 0 |
| [R.A.S. chinoise de Macao](countries/MO.md) | 0 | 0 | 0 | 0 | 1 | 0 |
| [Singapour](countries/SG.md) | 1 | 0 | 2 | 0 | 2 | 0 |
| [Sri Lanka](countries/LK.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Syrie](countries/SY.md) | 0 | 0 | 1 | 0 | 0 | 0 |
| [Tadjikistan](countries/TJ.md) | 0 | 0 | 0 | 0 | 1 | 0 |
| [Taïwan](countries/TW.md) | 0 | 0 | 3 | 0 | 8 | 0 |
| [Territoires palestiniens](countries/PS.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Thaïlande](countries/TH.md) | 0 | 0 | 0 | 2 | 4 | 0 |
| [Timor oriental](countries/TL.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Turkménistan](countries/TM.md) | 0 | 0 | 0 | 0 | 1 | 0 |
| [Viêt Nam](countries/VN.md) | 0 | 0 | 4 | 0 | 22 | 0 |
| [Yémen](countries/YE.md) | 0 | 0 | 0 | 0 | 4 | 0 |
| **Total** | **2** | **0** | **63** | **2** | **715** | **0** |

Intégrations achevées avec preuve et audit enregistrés depuis la création du registre : **2**. Fiches créées depuis le relevé dont la visibilité reste à contrôler : **0**.

- Malaisie : 1 intégration achevée et auditée, dont 0 fiches encore masquées.
- Singapour : 1 intégration achevée et auditée, dont 0 fiches encore masquées.
### Pistes standalone à requalifier

| Pays | Dans une fiche Park masquée | Dans une fiche Park publiée | Sans ID : identité / parent à résoudre² | Dossiers rattachés à un vrai parc |
| --- | ---: | ---: | ---: | ---: |
| [Afghanistan](countries/AF.md) | 0 | 0 | 0 | 0 |
| [Arabie saoudite](countries/SA.md) | 0 | 0 | 4 | 0 |
| [Bahreïn](countries/BH.md) | 0 | 0 | 0 | 0 |
| [Bangladesh](countries/BD.md) | 0 | 0 | 1 | 0 |
| [Bhoutan](countries/BT.md) | 0 | 0 | 0 | 0 |
| [Brunei](countries/BN.md) | 0 | 0 | 0 | 0 |
| [Cambodge](countries/KH.md) | 0 | 0 | 3 | 0 |
| [Chine](countries/CN.md) | 1 | 0 | 109 | 6 |
| [Corée du Nord](countries/KP.md) | 0 | 0 | 0 | 0 |
| [Corée du Sud](countries/KR.md) | 0 | 0 | 6 | 0 |
| [Émirats arabes unis](countries/AE.md) | 1 | 0 | 6 | 0 |
| [Île Christmas](countries/CX.md) | 0 | 0 | 0 | 0 |
| [Îles Cocos](countries/CC.md) | 0 | 0 | 0 | 0 |
| [Inde](countries/IN.md) | 0 | 0 | 0 | 0 |
| [Indonésie](countries/ID.md) | 0 | 0 | 1 | 0 |
| [Irak](countries/IQ.md) | 0 | 0 | 3 | 0 |
| [Iran](countries/IR.md) | 0 | 0 | 12 | 0 |
| [Israël](countries/IL.md) | 0 | 0 | 4 | 0 |
| [Japon](countries/JP.md) | 0 | 0 | 51 | 3 |
| [Jordanie](countries/JO.md) | 0 | 0 | 0 | 0 |
| [Kirghizstan](countries/KG.md) | 0 | 0 | 0 | 0 |
| [Koweït](countries/KW.md) | 0 | 0 | 0 | 0 |
| [Laos](countries/LA.md) | 0 | 0 | 0 | 0 |
| [Liban](countries/LB.md) | 0 | 0 | 0 | 0 |
| [Malaisie](countries/MY.md) | 0 | 0 | 1 | 0 |
| [Maldives](countries/MV.md) | 0 | 0 | 0 | 0 |
| [Mongolie](countries/MN.md) | 0 | 0 | 0 | 0 |
| [Myanmar (Birmanie)](countries/MM.md) | 0 | 0 | 0 | 0 |
| [Népal](countries/NP.md) | 0 | 0 | 0 | 0 |
| [Oman](countries/OM.md) | 0 | 0 | 0 | 0 |
| [Ouzbékistan](countries/UZ.md) | 0 | 0 | 2 | 0 |
| [Pakistan](countries/PK.md) | 0 | 0 | 0 | 0 |
| [Philippines](countries/PH.md) | 0 | 0 | 6 | 0 |
| [Qatar](countries/QA.md) | 0 | 0 | 0 | 0 |
| [R.A.S. chinoise de Hong Kong](countries/HK.md) | 0 | 0 | 2 | 0 |
| [R.A.S. chinoise de Macao](countries/MO.md) | 0 | 0 | 1 | 0 |
| [Singapour](countries/SG.md) | 0 | 0 | 2 | 0 |
| [Sri Lanka](countries/LK.md) | 0 | 0 | 0 | 0 |
| [Syrie](countries/SY.md) | 0 | 0 | 0 | 0 |
| [Tadjikistan](countries/TJ.md) | 0 | 0 | 1 | 0 |
| [Taïwan](countries/TW.md) | 0 | 0 | 6 | 0 |
| [Territoires palestiniens](countries/PS.md) | 0 | 0 | 0 | 0 |
| [Thaïlande](countries/TH.md) | 0 | 0 | 4 | 0 |
| [Timor oriental](countries/TL.md) | 0 | 0 | 0 | 0 |
| [Turkménistan](countries/TM.md) | 0 | 0 | 1 | 0 |
| [Viêt Nam](countries/VN.md) | 1 | 0 | 16 | 4 |
| [Yémen](countries/YE.md) | 0 | 0 | 0 | 0 |
| **Total** | **3** | **0** | **242** | **13** |

Les deux colonnes de fiches Park sont des sous-ensembles des totaux Park ; la colonne sans ID est un sous-ensemble des rapprochements incertains. Ce sont des dossiers à examiner, pas un nombre garanti d’attractions autonomes distinctes.

## Afrique

### Parcs

| Pays / territoire | Publiés¹ | Masqués à intégrer | Masqués NotRelevant à réexaminer | Sans fiche : candidats parcs | Observations externes |
| --- | ---: | ---: | ---: | ---: | ---: |
| [Afrique du Sud](countries/ZA.md) | 0 | 14 | 4 | 31 | 50 |
| [Algérie](countries/DZ.md) | 1 | 29 | 1 | 9 | 41 |
| [Angola](countries/AO.md) | 0 | 1 | 0 | 0 | 1 |
| [Bénin](countries/BJ.md) | 0 | 0 | 0 | 0 | 0 |
| [Botswana](countries/BW.md) | 0 | 2 | 0 | 0 | 3 |
| [Burkina Faso](countries/BF.md) | 0 | 0 | 0 | 0 | 0 |
| [Burundi](countries/BI.md) | 0 | 0 | 0 | 0 | 0 |
| [Cameroun](countries/CM.md) | 0 | 0 | 0 | 1 | 1 |
| [Cap-Vert](countries/CV.md) | 0 | 0 | 0 | 0 | 0 |
| [Comores](countries/KM.md) | 0 | 0 | 0 | 0 | 0 |
| [Congo-Brazzaville](countries/CG.md) | 0 | 0 | 0 | 0 | 0 |
| [Congo-Kinshasa](countries/CD.md) | 0 | 1 | 0 | 0 | 1 |
| [Côte d’Ivoire](countries/CI.md) | 0 | 1 | 0 | 1 | 2 |
| [Djibouti](countries/DJ.md) | 0 | 0 | 0 | 0 | 0 |
| [Égypte](countries/EG.md) | 1 | 23 | 2 | 57 | 92 |
| [Érythrée](countries/ER.md) | 0 | 0 | 0 | 0 | 0 |
| [Eswatini](countries/SZ.md) | 0 | 0 | 0 | 0 | 0 |
| [Éthiopie](countries/ET.md) | 1 | 0 | 1 | 2 | 4 |
| [Gabon](countries/GA.md) | 0 | 0 | 0 | 1 | 1 |
| [Gambie](countries/GM.md) | 0 | 0 | 0 | 0 | 0 |
| [Ghana](countries/GH.md) | 0 | 1 | 0 | 2 | 3 |
| [Guinée](countries/GN.md) | 0 | 0 | 0 | 0 | 0 |
| [Guinée équatoriale](countries/GQ.md) | 0 | 0 | 0 | 0 | 0 |
| [Guinée-Bissau](countries/GW.md) | 0 | 0 | 0 | 0 | 0 |
| [Kenya](countries/KE.md) | 0 | 6 | 0 | 4 | 12 |
| [La Réunion](countries/RE.md) | 0 | 0 | 0 | 1 | 1 |
| [Lesotho](countries/LS.md) | 0 | 1 | 0 | 0 | 1 |
| [Liberia](countries/LR.md) | 0 | 0 | 0 | 0 | 0 |
| [Libye](countries/LY.md) | 0 | 4 | 0 | 3 | 9 |
| [Madagascar](countries/MG.md) | 0 | 1 | 0 | 3 | 4 |
| [Malawi](countries/MW.md) | 0 | 1 | 0 | 1 | 2 |
| [Mali](countries/ML.md) | 0 | 0 | 0 | 1 | 1 |
| [Maroc](countries/MA.md) | 0 | 14 | 1 | 15 | 38 |
| [Maurice](countries/MU.md) | 0 | 2 | 0 | 2 | 7 |
| [Mauritanie](countries/MR.md) | 0 | 0 | 0 | 0 | 0 |
| [Mayotte](countries/YT.md) | 0 | 0 | 0 | 0 | 0 |
| [Mozambique](countries/MZ.md) | 0 | 0 | 0 | 3 | 3 |
| [Namibie](countries/NA.md) | 0 | 0 | 0 | 0 | 0 |
| [Niger](countries/NE.md) | 0 | 0 | 0 | 0 | 0 |
| [Nigeria](countries/NG.md) | 2 | 16 | 1 | 20 | 43 |
| [Ouganda](countries/UG.md) | 0 | 2 | 0 | 1 | 4 |
| [République centrafricaine](countries/CF.md) | 0 | 0 | 0 | 0 | 0 |
| [Rwanda](countries/RW.md) | 1 | 0 | 0 | 0 | 1 |
| [Sahara occidental](countries/EH.md) | 0 | 0 | 0 | 0 | 0 |
| [Sainte-Hélène](countries/SH.md) | 0 | 0 | 0 | 0 | 0 |
| [Sao Tomé-et-Principe](countries/ST.md) | 0 | 0 | 0 | 0 | 0 |
| [Sénégal](countries/SN.md) | 0 | 1 | 0 | 1 | 2 |
| [Seychelles](countries/SC.md) | 0 | 0 | 0 | 0 | 0 |
| [Sierra Leone](countries/SL.md) | 0 | 0 | 0 | 0 | 0 |
| [Somalie](countries/SO.md) | 0 | 1 | 0 | 1 | 2 |
| [Soudan](countries/SD.md) | 0 | 4 | 0 | 0 | 5 |
| [Soudan du Sud](countries/SS.md) | 0 | 0 | 0 | 0 | 0 |
| [Tanzanie](countries/TZ.md) | 0 | 1 | 0 | 3 | 4 |
| [Tchad](countries/TD.md) | 0 | 0 | 0 | 0 | 0 |
| [Terres australes françaises](countries/TF.md) | 0 | 0 | 0 | 0 | 0 |
| [Territoire britannique de l’océan Indien](countries/IO.md) | 0 | 0 | 0 | 0 | 0 |
| [Togo](countries/TG.md) | 0 | 1 | 0 | 0 | 1 |
| [Tunisie](countries/TN.md) | 0 | 12 | 2 | 8 | 24 |
| [Zambie](countries/ZM.md) | 0 | 2 | 0 | 1 | 3 |
| [Zimbabwe](countries/ZW.md) | 0 | 1 | 0 | 0 | 1 |
| **Total** | **6** | **142** | **12** | **172** | **367** |

### Attractions standalone et dossiers à trancher

| Pays | Standalone publiées¹ | Standalone masquées | Sans fiche : candidates standalone | Sans fiche : type à décider | Rapprochements incertains² | Anciens parcs liés à une standalone |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| [Afrique du Sud](countries/ZA.md) | 0 | 0 | 0 | 0 | 2 | 0 |
| [Algérie](countries/DZ.md) | 0 | 0 | 0 | 0 | 1 | 0 |
| [Angola](countries/AO.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Bénin](countries/BJ.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Botswana](countries/BW.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Burkina Faso](countries/BF.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Burundi](countries/BI.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Cameroun](countries/CM.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Cap-Vert](countries/CV.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Comores](countries/KM.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Congo-Brazzaville](countries/CG.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Congo-Kinshasa](countries/CD.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Côte d’Ivoire](countries/CI.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Djibouti](countries/DJ.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Égypte](countries/EG.md) | 0 | 0 | 0 | 0 | 4 | 0 |
| [Érythrée](countries/ER.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Eswatini](countries/SZ.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Éthiopie](countries/ET.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Gabon](countries/GA.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Gambie](countries/GM.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Ghana](countries/GH.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Guinée](countries/GN.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Guinée équatoriale](countries/GQ.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Guinée-Bissau](countries/GW.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Kenya](countries/KE.md) | 0 | 0 | 0 | 0 | 1 | 0 |
| [La Réunion](countries/RE.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Lesotho](countries/LS.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Liberia](countries/LR.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Libye](countries/LY.md) | 0 | 0 | 0 | 0 | 1 | 0 |
| [Madagascar](countries/MG.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Malawi](countries/MW.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Mali](countries/ML.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Maroc](countries/MA.md) | 0 | 0 | 0 | 0 | 4 | 0 |
| [Maurice](countries/MU.md) | 0 | 0 | 0 | 0 | 1 | 0 |
| [Mauritanie](countries/MR.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Mayotte](countries/YT.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Mozambique](countries/MZ.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Namibie](countries/NA.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Niger](countries/NE.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Nigeria](countries/NG.md) | 0 | 0 | 0 | 1 | 1 | 0 |
| [Ouganda](countries/UG.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [République centrafricaine](countries/CF.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Rwanda](countries/RW.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Sahara occidental](countries/EH.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Sainte-Hélène](countries/SH.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Sao Tomé-et-Principe](countries/ST.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Sénégal](countries/SN.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Seychelles](countries/SC.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Sierra Leone](countries/SL.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Somalie](countries/SO.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Soudan](countries/SD.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Soudan du Sud](countries/SS.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Tanzanie](countries/TZ.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Tchad](countries/TD.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Terres australes françaises](countries/TF.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Territoire britannique de l’océan Indien](countries/IO.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Togo](countries/TG.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Tunisie](countries/TN.md) | 0 | 0 | 0 | 0 | 2 | 0 |
| [Zambie](countries/ZM.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Zimbabwe](countries/ZW.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| **Total** | **0** | **0** | **0** | **1** | **17** | **0** |

Intégrations achevées avec preuve et audit enregistrés depuis la création du registre : **0**. Fiches créées depuis le relevé dont la visibilité reste à contrôler : **0**.

### Pistes standalone à requalifier

| Pays | Dans une fiche Park masquée | Dans une fiche Park publiée | Sans ID : identité / parent à résoudre² | Dossiers rattachés à un vrai parc |
| --- | ---: | ---: | ---: | ---: |
| [Afrique du Sud](countries/ZA.md) | 1 | 0 | 1 | 0 |
| [Algérie](countries/DZ.md) | 0 | 0 | 0 | 0 |
| [Angola](countries/AO.md) | 0 | 0 | 0 | 0 |
| [Bénin](countries/BJ.md) | 0 | 0 | 0 | 0 |
| [Botswana](countries/BW.md) | 0 | 0 | 0 | 0 |
| [Burkina Faso](countries/BF.md) | 0 | 0 | 0 | 0 |
| [Burundi](countries/BI.md) | 0 | 0 | 0 | 0 |
| [Cameroun](countries/CM.md) | 0 | 0 | 0 | 0 |
| [Cap-Vert](countries/CV.md) | 0 | 0 | 0 | 0 |
| [Comores](countries/KM.md) | 0 | 0 | 0 | 0 |
| [Congo-Brazzaville](countries/CG.md) | 0 | 0 | 0 | 0 |
| [Congo-Kinshasa](countries/CD.md) | 0 | 0 | 0 | 0 |
| [Côte d’Ivoire](countries/CI.md) | 0 | 0 | 0 | 0 |
| [Djibouti](countries/DJ.md) | 0 | 0 | 0 | 0 |
| [Égypte](countries/EG.md) | 0 | 0 | 0 | 0 |
| [Érythrée](countries/ER.md) | 0 | 0 | 0 | 0 |
| [Eswatini](countries/SZ.md) | 0 | 0 | 0 | 0 |
| [Éthiopie](countries/ET.md) | 0 | 0 | 0 | 0 |
| [Gabon](countries/GA.md) | 0 | 0 | 0 | 0 |
| [Gambie](countries/GM.md) | 0 | 0 | 0 | 0 |
| [Ghana](countries/GH.md) | 0 | 0 | 0 | 0 |
| [Guinée](countries/GN.md) | 0 | 0 | 0 | 0 |
| [Guinée équatoriale](countries/GQ.md) | 0 | 0 | 0 | 0 |
| [Guinée-Bissau](countries/GW.md) | 0 | 0 | 0 | 0 |
| [Kenya](countries/KE.md) | 0 | 0 | 1 | 0 |
| [La Réunion](countries/RE.md) | 0 | 0 | 0 | 0 |
| [Lesotho](countries/LS.md) | 0 | 0 | 0 | 0 |
| [Liberia](countries/LR.md) | 0 | 0 | 0 | 0 |
| [Libye](countries/LY.md) | 0 | 0 | 0 | 0 |
| [Madagascar](countries/MG.md) | 0 | 0 | 0 | 0 |
| [Malawi](countries/MW.md) | 0 | 0 | 0 | 0 |
| [Mali](countries/ML.md) | 0 | 0 | 0 | 0 |
| [Maroc](countries/MA.md) | 0 | 0 | 0 | 1 |
| [Maurice](countries/MU.md) | 0 | 0 | 1 | 1 |
| [Mauritanie](countries/MR.md) | 0 | 0 | 0 | 0 |
| [Mayotte](countries/YT.md) | 0 | 0 | 0 | 0 |
| [Mozambique](countries/MZ.md) | 0 | 0 | 0 | 0 |
| [Namibie](countries/NA.md) | 0 | 0 | 0 | 0 |
| [Niger](countries/NE.md) | 0 | 0 | 0 | 0 |
| [Nigeria](countries/NG.md) | 0 | 0 | 0 | 0 |
| [Ouganda](countries/UG.md) | 0 | 0 | 0 | 0 |
| [République centrafricaine](countries/CF.md) | 0 | 0 | 0 | 0 |
| [Rwanda](countries/RW.md) | 0 | 0 | 0 | 0 |
| [Sahara occidental](countries/EH.md) | 0 | 0 | 0 | 0 |
| [Sainte-Hélène](countries/SH.md) | 0 | 0 | 0 | 0 |
| [Sao Tomé-et-Principe](countries/ST.md) | 0 | 0 | 0 | 0 |
| [Sénégal](countries/SN.md) | 0 | 0 | 0 | 0 |
| [Seychelles](countries/SC.md) | 0 | 0 | 0 | 0 |
| [Sierra Leone](countries/SL.md) | 0 | 0 | 0 | 0 |
| [Somalie](countries/SO.md) | 0 | 0 | 0 | 0 |
| [Soudan](countries/SD.md) | 0 | 0 | 0 | 0 |
| [Soudan du Sud](countries/SS.md) | 0 | 0 | 0 | 0 |
| [Tanzanie](countries/TZ.md) | 0 | 0 | 0 | 0 |
| [Tchad](countries/TD.md) | 0 | 0 | 0 | 0 |
| [Terres australes françaises](countries/TF.md) | 0 | 0 | 0 | 0 |
| [Territoire britannique de l’océan Indien](countries/IO.md) | 0 | 0 | 0 | 0 |
| [Togo](countries/TG.md) | 0 | 0 | 0 | 0 |
| [Tunisie](countries/TN.md) | 0 | 0 | 1 | 0 |
| [Zambie](countries/ZM.md) | 0 | 0 | 0 | 0 |
| [Zimbabwe](countries/ZW.md) | 0 | 0 | 0 | 0 |
| **Total** | **1** | **0** | **4** | **2** |

Les deux colonnes de fiches Park sont des sous-ensembles des totaux Park ; la colonne sans ID est un sous-ensemble des rapprochements incertains. Ce sont des dossiers à examiner, pas un nombre garanti d’attractions autonomes distinctes.

## Amérique du Nord, centrale et Caraïbes

### Parcs

| Pays / territoire | Publiés¹ | Masqués à intégrer | Masqués NotRelevant à réexaminer | Sans fiche : candidats parcs | Observations externes |
| --- | ---: | ---: | ---: | ---: | ---: |
| [Anguilla](countries/AI.md) | 0 | 0 | 0 | 0 | 0 |
| [Antigua-et-Barbuda](countries/AG.md) | 0 | 0 | 0 | 0 | 0 |
| [Aruba](countries/AW.md) | 0 | 0 | 0 | 0 | 0 |
| [Bahamas](countries/BS.md) | 0 | 0 | 0 | 5 | 6 |
| [Barbade](countries/BB.md) | 0 | 0 | 0 | 0 | 0 |
| [Belize](countries/BZ.md) | 0 | 0 | 0 | 1 | 1 |
| [Bermudes](countries/BM.md) | 0 | 0 | 0 | 2 | 3 |
| [Canada](countries/CA.md) | 0 | 25 | 2 | 84 | 180 |
| [Costa Rica](countries/CR.md) | 0 | 0 | 0 | 14 | 15 |
| [Cuba](countries/CU.md) | 0 | 0 | 0 | 10 | 12 |
| [Curaçao](countries/CW.md) | 0 | 0 | 0 | 0 | 0 |
| [Dominique](countries/DM.md) | 0 | 0 | 0 | 0 | 0 |
| [États-Unis](countries/US.md) | 38 | 233 | 21 | 1526 | 2621 |
| [Grenade](countries/GD.md) | 0 | 0 | 0 | 0 | 0 |
| [Groenland](countries/GL.md) | 0 | 0 | 0 | 0 | 0 |
| [Guadeloupe](countries/GP.md) | 0 | 0 | 0 | 0 | 0 |
| [Guatemala](countries/GT.md) | 0 | 1 | 0 | 11 | 16 |
| [Haïti](countries/HT.md) | 1 | 0 | 0 | 2 | 5 |
| [Honduras](countries/HN.md) | 0 | 0 | 0 | 4 | 6 |
| [Îles Caïmans](countries/KY.md) | 0 | 0 | 0 | 0 | 0 |
| [Îles Turques-et-Caïques](countries/TC.md) | 0 | 0 | 0 | 0 | 0 |
| [Îles Vierges britanniques](countries/VG.md) | 0 | 0 | 0 | 0 | 0 |
| [Îles Vierges des États-Unis](countries/VI.md) | 0 | 0 | 0 | 0 | 0 |
| [Jamaïque](countries/JM.md) | 0 | 1 | 0 | 3 | 8 |
| [Martinique](countries/MQ.md) | 0 | 0 | 0 | 0 | 0 |
| [Mexique](countries/MX.md) | 2 | 6 | 1 | 87 | 139 |
| [Montserrat](countries/MS.md) | 0 | 0 | 0 | 0 | 0 |
| [Nicaragua](countries/NI.md) | 0 | 0 | 0 | 1 | 1 |
| [Panama](countries/PA.md) | 0 | 0 | 0 | 5 | 5 |
| [Pays-Bas caribéens](countries/BQ.md) | 0 | 0 | 0 | 0 | 0 |
| [Porto Rico](countries/PR.md) | 0 | 0 | 0 | 1 | 2 |
| [République dominicaine](countries/DO.md) | 0 | 0 | 0 | 7 | 10 |
| [Saint-Barthélemy](countries/BL.md) | 0 | 0 | 0 | 0 | 0 |
| [Saint-Christophe-et-Niévès](countries/KN.md) | 0 | 0 | 0 | 0 | 0 |
| [Saint-Martin](countries/MF.md) | 0 | 0 | 0 | 0 | 0 |
| [Saint-Martin (partie néerlandaise)](countries/SX.md) | 0 | 0 | 0 | 0 | 1 |
| [Saint-Pierre-et-Miquelon](countries/PM.md) | 0 | 0 | 0 | 0 | 0 |
| [Saint-Vincent-et-les Grenadines](countries/VC.md) | 0 | 0 | 0 | 0 | 0 |
| [Sainte-Lucie](countries/LC.md) | 0 | 0 | 0 | 1 | 1 |
| [Salvador](countries/SV.md) | 0 | 0 | 0 | 3 | 4 |
| [Trinité-et-Tobago](countries/TT.md) | 0 | 0 | 0 | 3 | 3 |
| **Total** | **41** | **266** | **24** | **1770** | **3039** |

### Attractions standalone et dossiers à trancher

| Pays | Standalone publiées¹ | Standalone masquées | Sans fiche : candidates standalone | Sans fiche : type à décider | Rapprochements incertains² | Anciens parcs liés à une standalone |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| [Anguilla](countries/AI.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Antigua-et-Barbuda](countries/AG.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Aruba](countries/AW.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Bahamas](countries/BS.md) | 0 | 0 | 1 | 0 | 0 | 0 |
| [Barbade](countries/BB.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Belize](countries/BZ.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Bermudes](countries/BM.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Canada](countries/CA.md) | 1 | 0 | 4 | 0 | 12 | 0 |
| [Costa Rica](countries/CR.md) | 0 | 0 | 0 | 0 | 1 | 0 |
| [Cuba](countries/CU.md) | 0 | 0 | 0 | 0 | 2 | 0 |
| [Curaçao](countries/CW.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Dominique](countries/DM.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [États-Unis](countries/US.md) | 5 | 0 | 39 | 2 | 185 | 0 |
| [Grenade](countries/GD.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Groenland](countries/GL.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Guadeloupe](countries/GP.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Guatemala](countries/GT.md) | 0 | 0 | 0 | 0 | 3 | 0 |
| [Haïti](countries/HT.md) | 0 | 0 | 0 | 0 | 1 | 0 |
| [Honduras](countries/HN.md) | 0 | 0 | 0 | 0 | 2 | 0 |
| [Îles Caïmans](countries/KY.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Îles Turques-et-Caïques](countries/TC.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Îles Vierges britanniques](countries/VG.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Îles Vierges des États-Unis](countries/VI.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Jamaïque](countries/JM.md) | 0 | 0 | 0 | 0 | 1 | 0 |
| [Martinique](countries/MQ.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Mexique](countries/MX.md) | 0 | 0 | 1 | 0 | 8 | 0 |
| [Montserrat](countries/MS.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Nicaragua](countries/NI.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Panama](countries/PA.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Pays-Bas caribéens](countries/BQ.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Porto Rico](countries/PR.md) | 0 | 0 | 1 | 0 | 0 | 0 |
| [République dominicaine](countries/DO.md) | 0 | 0 | 0 | 0 | 3 | 0 |
| [Saint-Barthélemy](countries/BL.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Saint-Christophe-et-Niévès](countries/KN.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Saint-Martin](countries/MF.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Saint-Martin (partie néerlandaise)](countries/SX.md) | 0 | 0 | 0 | 0 | 1 | 0 |
| [Saint-Pierre-et-Miquelon](countries/PM.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Saint-Vincent-et-les Grenadines](countries/VC.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Sainte-Lucie](countries/LC.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Salvador](countries/SV.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Trinité-et-Tobago](countries/TT.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| **Total** | **6** | **0** | **46** | **2** | **219** | **0** |

Intégrations achevées avec preuve et audit enregistrés depuis la création du registre : **6**. Fiches créées depuis le relevé dont la visibilité reste à contrôler : **0**.

- Canada : 1 intégration achevée et auditée, dont 0 fiches encore masquées.
- États-Unis : 5 intégrations achevées et auditées, dont 0 fiches encore masquées.
### Pistes standalone à requalifier

| Pays | Dans une fiche Park masquée | Dans une fiche Park publiée | Sans ID : identité / parent à résoudre² | Dossiers rattachés à un vrai parc |
| --- | ---: | ---: | ---: | ---: |
| [Anguilla](countries/AI.md) | 0 | 0 | 0 | 0 |
| [Antigua-et-Barbuda](countries/AG.md) | 0 | 0 | 0 | 0 |
| [Aruba](countries/AW.md) | 0 | 0 | 0 | 0 |
| [Bahamas](countries/BS.md) | 0 | 0 | 0 | 0 |
| [Barbade](countries/BB.md) | 0 | 0 | 0 | 0 |
| [Belize](countries/BZ.md) | 0 | 0 | 0 | 0 |
| [Bermudes](countries/BM.md) | 0 | 0 | 0 | 0 |
| [Canada](countries/CA.md) | 5 | 0 | 9 | 0 |
| [Costa Rica](countries/CR.md) | 0 | 0 | 0 | 0 |
| [Cuba](countries/CU.md) | 0 | 0 | 0 | 0 |
| [Curaçao](countries/CW.md) | 0 | 0 | 0 | 0 |
| [Dominique](countries/DM.md) | 0 | 0 | 0 | 0 |
| [États-Unis](countries/US.md) | 29 | 3 | 76 | 5 |
| [Grenade](countries/GD.md) | 0 | 0 | 0 | 0 |
| [Groenland](countries/GL.md) | 0 | 0 | 0 | 0 |
| [Guadeloupe](countries/GP.md) | 0 | 0 | 0 | 0 |
| [Guatemala](countries/GT.md) | 0 | 0 | 0 | 0 |
| [Haïti](countries/HT.md) | 0 | 1 | 1 | 0 |
| [Honduras](countries/HN.md) | 0 | 0 | 0 | 0 |
| [Îles Caïmans](countries/KY.md) | 0 | 0 | 0 | 0 |
| [Îles Turques-et-Caïques](countries/TC.md) | 0 | 0 | 0 | 0 |
| [Îles Vierges britanniques](countries/VG.md) | 0 | 0 | 0 | 0 |
| [Îles Vierges des États-Unis](countries/VI.md) | 0 | 0 | 0 | 0 |
| [Jamaïque](countries/JM.md) | 1 | 0 | 1 | 0 |
| [Martinique](countries/MQ.md) | 0 | 0 | 0 | 0 |
| [Mexique](countries/MX.md) | 0 | 0 | 6 | 1 |
| [Montserrat](countries/MS.md) | 0 | 0 | 0 | 0 |
| [Nicaragua](countries/NI.md) | 0 | 0 | 0 | 0 |
| [Panama](countries/PA.md) | 0 | 0 | 0 | 0 |
| [Pays-Bas caribéens](countries/BQ.md) | 0 | 0 | 0 | 0 |
| [Porto Rico](countries/PR.md) | 0 | 0 | 0 | 0 |
| [République dominicaine](countries/DO.md) | 0 | 0 | 1 | 0 |
| [Saint-Barthélemy](countries/BL.md) | 0 | 0 | 0 | 0 |
| [Saint-Christophe-et-Niévès](countries/KN.md) | 0 | 0 | 0 | 0 |
| [Saint-Martin](countries/MF.md) | 0 | 0 | 0 | 0 |
| [Saint-Martin (partie néerlandaise)](countries/SX.md) | 0 | 0 | 1 | 0 |
| [Saint-Pierre-et-Miquelon](countries/PM.md) | 0 | 0 | 0 | 0 |
| [Saint-Vincent-et-les Grenadines](countries/VC.md) | 0 | 0 | 0 | 0 |
| [Sainte-Lucie](countries/LC.md) | 0 | 0 | 0 | 0 |
| [Salvador](countries/SV.md) | 0 | 0 | 0 | 0 |
| [Trinité-et-Tobago](countries/TT.md) | 0 | 0 | 0 | 0 |
| **Total** | **35** | **4** | **95** | **6** |

Les deux colonnes de fiches Park sont des sous-ensembles des totaux Park ; la colonne sans ID est un sous-ensemble des rapprochements incertains. Ce sont des dossiers à examiner, pas un nombre garanti d’attractions autonomes distinctes.

## Amérique du Sud

### Parcs

| Pays / territoire | Publiés¹ | Masqués à intégrer | Masqués NotRelevant à réexaminer | Sans fiche : candidats parcs | Observations externes |
| --- | ---: | ---: | ---: | ---: | ---: |
| [Argentine](countries/AR.md) | 0 | 2 | 0 | 47 | 55 |
| [Bolivie](countries/BO.md) | 0 | 0 | 0 | 3 | 4 |
| [Brésil](countries/BR.md) | 0 | 19 | 0 | 245 | 311 |
| [Chili](countries/CL.md) | 0 | 3 | 0 | 21 | 30 |
| [Colombie](countries/CO.md) | 0 | 3 | 0 | 52 | 60 |
| [Équateur](countries/EC.md) | 0 | 1 | 0 | 10 | 13 |
| [Guyana](countries/GY.md) | 0 | 0 | 0 | 1 | 1 |
| [Guyane française](countries/GF.md) | 0 | 0 | 0 | 2 | 2 |
| [Îles Malouines](countries/FK.md) | 0 | 0 | 0 | 0 | 0 |
| [Paraguay](countries/PY.md) | 0 | 0 | 0 | 4 | 4 |
| [Pérou](countries/PE.md) | 0 | 2 | 0 | 27 | 41 |
| [Suriname](countries/SR.md) | 0 | 0 | 0 | 1 | 1 |
| [Uruguay](countries/UY.md) | 0 | 0 | 0 | 5 | 5 |
| [Venezuela](countries/VE.md) | 0 | 6 | 0 | 24 | 37 |
| **Total** | **0** | **36** | **0** | **442** | **564** |

### Attractions standalone et dossiers à trancher

| Pays | Standalone publiées¹ | Standalone masquées | Sans fiche : candidates standalone | Sans fiche : type à décider | Rapprochements incertains² | Anciens parcs liés à une standalone |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| [Argentine](countries/AR.md) | 0 | 0 | 0 | 0 | 1 | 0 |
| [Bolivie](countries/BO.md) | 0 | 0 | 0 | 0 | 1 | 0 |
| [Brésil](countries/BR.md) | 0 | 0 | 0 | 0 | 18 | 0 |
| [Chili](countries/CL.md) | 0 | 0 | 0 | 0 | 2 | 0 |
| [Colombie](countries/CO.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Équateur](countries/EC.md) | 0 | 0 | 0 | 0 | 1 | 0 |
| [Guyana](countries/GY.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Guyane française](countries/GF.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Îles Malouines](countries/FK.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Paraguay](countries/PY.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Pérou](countries/PE.md) | 0 | 0 | 0 | 0 | 2 | 0 |
| [Suriname](countries/SR.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Uruguay](countries/UY.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Venezuela](countries/VE.md) | 0 | 0 | 0 | 0 | 4 | 0 |
| **Total** | **0** | **0** | **0** | **0** | **29** | **0** |

Intégrations achevées avec preuve et audit enregistrés depuis la création du registre : **0**. Fiches créées depuis le relevé dont la visibilité reste à contrôler : **0**.

### Pistes standalone à requalifier

| Pays | Dans une fiche Park masquée | Dans une fiche Park publiée | Sans ID : identité / parent à résoudre² | Dossiers rattachés à un vrai parc |
| --- | ---: | ---: | ---: | ---: |
| [Argentine](countries/AR.md) | 0 | 0 | 1 | 0 |
| [Bolivie](countries/BO.md) | 0 | 0 | 0 | 0 |
| [Brésil](countries/BR.md) | 1 | 0 | 10 | 1 |
| [Chili](countries/CL.md) | 0 | 0 | 1 | 0 |
| [Colombie](countries/CO.md) | 0 | 0 | 0 | 0 |
| [Équateur](countries/EC.md) | 0 | 0 | 0 | 0 |
| [Guyana](countries/GY.md) | 0 | 0 | 0 | 0 |
| [Guyane française](countries/GF.md) | 0 | 0 | 0 | 0 |
| [Îles Malouines](countries/FK.md) | 0 | 0 | 0 | 0 |
| [Paraguay](countries/PY.md) | 0 | 0 | 0 | 0 |
| [Pérou](countries/PE.md) | 0 | 0 | 0 | 0 |
| [Suriname](countries/SR.md) | 0 | 0 | 0 | 0 |
| [Uruguay](countries/UY.md) | 0 | 0 | 0 | 0 |
| [Venezuela](countries/VE.md) | 0 | 0 | 0 | 0 |
| **Total** | **1** | **0** | **12** | **1** |

Les deux colonnes de fiches Park sont des sous-ensembles des totaux Park ; la colonne sans ID est un sous-ensemble des rapprochements incertains. Ce sont des dossiers à examiner, pas un nombre garanti d’attractions autonomes distinctes.

## Océanie

### Parcs

| Pays / territoire | Publiés¹ | Masqués à intégrer | Masqués NotRelevant à réexaminer | Sans fiche : candidats parcs | Observations externes |
| --- | ---: | ---: | ---: | ---: | ---: |
| [Australie](countries/AU.md) | 2 | 13 | 2 | 55 | 131 |
| [Fidji](countries/FJ.md) | 0 | 0 | 0 | 1 | 1 |
| [Guam](countries/GU.md) | 0 | 0 | 0 | 4 | 4 |
| [Île Norfolk](countries/NF.md) | 0 | 0 | 0 | 0 | 0 |
| [Îles Cook](countries/CK.md) | 0 | 0 | 0 | 0 | 0 |
| [Îles Mariannes du Nord](countries/MP.md) | 0 | 0 | 0 | 0 | 0 |
| [Îles Marshall](countries/MH.md) | 0 | 0 | 0 | 0 | 0 |
| [Îles mineures éloignées des États-Unis](countries/UM.md) | 0 | 0 | 0 | 0 | 0 |
| [Îles Pitcairn](countries/PN.md) | 0 | 0 | 0 | 0 | 0 |
| [Îles Salomon](countries/SB.md) | 0 | 0 | 0 | 0 | 0 |
| [Kiribati](countries/KI.md) | 0 | 0 | 0 | 0 | 0 |
| [Micronésie](countries/FM.md) | 0 | 0 | 0 | 0 | 0 |
| [Nauru](countries/NR.md) | 0 | 0 | 0 | 0 | 0 |
| [Niue](countries/NU.md) | 0 | 0 | 0 | 0 | 0 |
| [Nouvelle-Calédonie](countries/NC.md) | 0 | 0 | 0 | 2 | 2 |
| [Nouvelle-Zélande](countries/NZ.md) | 0 | 1 | 0 | 24 | 33 |
| [Palaos](countries/PW.md) | 0 | 0 | 0 | 1 | 1 |
| [Papouasie-Nouvelle-Guinée](countries/PG.md) | 0 | 0 | 0 | 2 | 2 |
| [Polynésie française](countries/PF.md) | 0 | 0 | 0 | 0 | 0 |
| [Samoa](countries/WS.md) | 0 | 0 | 0 | 0 | 0 |
| [Samoa américaines](countries/AS.md) | 0 | 0 | 0 | 0 | 0 |
| [Tokelau](countries/TK.md) | 0 | 0 | 0 | 0 | 0 |
| [Tonga](countries/TO.md) | 0 | 0 | 0 | 0 | 0 |
| [Tuvalu](countries/TV.md) | 0 | 0 | 0 | 0 | 0 |
| [Vanuatu](countries/VU.md) | 0 | 0 | 0 | 0 | 0 |
| [Wallis-et-Futuna](countries/WF.md) | 0 | 0 | 0 | 0 | 0 |
| **Total** | **2** | **14** | **2** | **89** | **174** |

### Attractions standalone et dossiers à trancher

| Pays | Standalone publiées¹ | Standalone masquées | Sans fiche : candidates standalone | Sans fiche : type à décider | Rapprochements incertains² | Anciens parcs liés à une standalone |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| [Australie](countries/AU.md) | 1 | 0 | 1 | 0 | 12 | 0 |
| [Fidji](countries/FJ.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Guam](countries/GU.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Île Norfolk](countries/NF.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Îles Cook](countries/CK.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Îles Mariannes du Nord](countries/MP.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Îles Marshall](countries/MH.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Îles mineures éloignées des États-Unis](countries/UM.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Îles Pitcairn](countries/PN.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Îles Salomon](countries/SB.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Kiribati](countries/KI.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Micronésie](countries/FM.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Nauru](countries/NR.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Niue](countries/NU.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Nouvelle-Calédonie](countries/NC.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Nouvelle-Zélande](countries/NZ.md) | 0 | 0 | 1 | 0 | 2 | 0 |
| [Palaos](countries/PW.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Papouasie-Nouvelle-Guinée](countries/PG.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Polynésie française](countries/PF.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Samoa](countries/WS.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Samoa américaines](countries/AS.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Tokelau](countries/TK.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Tonga](countries/TO.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Tuvalu](countries/TV.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Vanuatu](countries/VU.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Wallis-et-Futuna](countries/WF.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| **Total** | **1** | **0** | **2** | **0** | **14** | **0** |

Intégrations achevées avec preuve et audit enregistrés depuis la création du registre : **1**. Fiches créées depuis le relevé dont la visibilité reste à contrôler : **0**.

- Australie : 1 intégration achevée et auditée, dont 0 fiches encore masquées.
### Pistes standalone à requalifier

| Pays | Dans une fiche Park masquée | Dans une fiche Park publiée | Sans ID : identité / parent à résoudre² | Dossiers rattachés à un vrai parc |
| --- | ---: | ---: | ---: | ---: |
| [Australie](countries/AU.md) | 1 | 0 | 12 | 2 |
| [Fidji](countries/FJ.md) | 0 | 0 | 0 | 0 |
| [Guam](countries/GU.md) | 0 | 0 | 0 | 0 |
| [Île Norfolk](countries/NF.md) | 0 | 0 | 0 | 0 |
| [Îles Cook](countries/CK.md) | 0 | 0 | 0 | 0 |
| [Îles Mariannes du Nord](countries/MP.md) | 0 | 0 | 0 | 0 |
| [Îles Marshall](countries/MH.md) | 0 | 0 | 0 | 0 |
| [Îles mineures éloignées des États-Unis](countries/UM.md) | 0 | 0 | 0 | 0 |
| [Îles Pitcairn](countries/PN.md) | 0 | 0 | 0 | 0 |
| [Îles Salomon](countries/SB.md) | 0 | 0 | 0 | 0 |
| [Kiribati](countries/KI.md) | 0 | 0 | 0 | 0 |
| [Micronésie](countries/FM.md) | 0 | 0 | 0 | 0 |
| [Nauru](countries/NR.md) | 0 | 0 | 0 | 0 |
| [Niue](countries/NU.md) | 0 | 0 | 0 | 0 |
| [Nouvelle-Calédonie](countries/NC.md) | 0 | 0 | 0 | 0 |
| [Nouvelle-Zélande](countries/NZ.md) | 0 | 0 | 2 | 0 |
| [Palaos](countries/PW.md) | 0 | 0 | 0 | 0 |
| [Papouasie-Nouvelle-Guinée](countries/PG.md) | 0 | 0 | 0 | 0 |
| [Polynésie française](countries/PF.md) | 0 | 0 | 0 | 0 |
| [Samoa](countries/WS.md) | 0 | 0 | 0 | 0 |
| [Samoa américaines](countries/AS.md) | 0 | 0 | 0 | 0 |
| [Tokelau](countries/TK.md) | 0 | 0 | 0 | 0 |
| [Tonga](countries/TO.md) | 0 | 0 | 0 | 0 |
| [Tuvalu](countries/TV.md) | 0 | 0 | 0 | 0 |
| [Vanuatu](countries/VU.md) | 0 | 0 | 0 | 0 |
| [Wallis-et-Futuna](countries/WF.md) | 0 | 0 | 0 | 0 |
| **Total** | **1** | **0** | **14** | **2** |

Les deux colonnes de fiches Park sont des sous-ensembles des totaux Park ; la colonne sans ID est un sous-ensemble des rapprochements incertains. Ce sont des dossiers à examiner, pas un nombre garanti d’attractions autonomes distinctes.

## Territoires antarctiques

### Parcs

| Pays / territoire | Publiés¹ | Masqués à intégrer | Masqués NotRelevant à réexaminer | Sans fiche : candidats parcs | Observations externes |
| --- | ---: | ---: | ---: | ---: | ---: |
| [Antarctique](countries/AQ.md) | 0 | 0 | 0 | 0 | 0 |
| [Géorgie du Sud-et-les Îles Sandwich du Sud](countries/GS.md) | 0 | 0 | 0 | 0 | 0 |
| [Île Bouvet](countries/BV.md) | 0 | 0 | 0 | 0 | 0 |
| [Îles Heard-et-MacDonald](countries/HM.md) | 0 | 0 | 0 | 0 | 0 |
| **Total** | **0** | **0** | **0** | **0** | **0** |

### Attractions standalone et dossiers à trancher

| Pays | Standalone publiées¹ | Standalone masquées | Sans fiche : candidates standalone | Sans fiche : type à décider | Rapprochements incertains² | Anciens parcs liés à une standalone |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| [Antarctique](countries/AQ.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Géorgie du Sud-et-les Îles Sandwich du Sud](countries/GS.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Île Bouvet](countries/BV.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| [Îles Heard-et-MacDonald](countries/HM.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| **Total** | **0** | **0** | **0** | **0** | **0** | **0** |

Intégrations achevées avec preuve et audit enregistrés depuis la création du registre : **0**. Fiches créées depuis le relevé dont la visibilité reste à contrôler : **0**.

### Pistes standalone à requalifier

| Pays | Dans une fiche Park masquée | Dans une fiche Park publiée | Sans ID : identité / parent à résoudre² | Dossiers rattachés à un vrai parc |
| --- | ---: | ---: | ---: | ---: |
| [Antarctique](countries/AQ.md) | 0 | 0 | 0 | 0 |
| [Géorgie du Sud-et-les Îles Sandwich du Sud](countries/GS.md) | 0 | 0 | 0 | 0 |
| [Île Bouvet](countries/BV.md) | 0 | 0 | 0 | 0 |
| [Îles Heard-et-MacDonald](countries/HM.md) | 0 | 0 | 0 | 0 |
| **Total** | **0** | **0** | **0** | **0** |

Les deux colonnes de fiches Park sont des sous-ensembles des totaux Park ; la colonne sans ID est un sous-ensemble des rapprochements incertains. Ce sont des dossiers à examiner, pas un nombre garanti d’attractions autonomes distinctes.

## Pays à déterminer

### Parcs

| Pays / territoire | Publiés¹ | Masqués à intégrer | Masqués NotRelevant à réexaminer | Sans fiche : candidats parcs | Observations externes |
| --- | ---: | ---: | ---: | ---: | ---: |
| [Pays à déterminer](countries/ZZ.md) | 0 | 34 | 37 | 0 | 0 |
| **Total** | **0** | **34** | **37** | **0** | **0** |

### Attractions standalone et dossiers à trancher

| Pays | Standalone publiées¹ | Standalone masquées | Sans fiche : candidates standalone | Sans fiche : type à décider | Rapprochements incertains² | Anciens parcs liés à une standalone |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| [Pays à déterminer](countries/ZZ.md) | 0 | 0 | 0 | 0 | 0 | 0 |
| **Total** | **0** | **0** | **0** | **0** | **0** | **0** |

Intégrations achevées avec preuve et audit enregistrés depuis la création du registre : **0**. Fiches créées depuis le relevé dont la visibilité reste à contrôler : **0**.

### Pistes standalone à requalifier

| Pays | Dans une fiche Park masquée | Dans une fiche Park publiée | Sans ID : identité / parent à résoudre² | Dossiers rattachés à un vrai parc |
| --- | ---: | ---: | ---: | ---: |
| [Pays à déterminer](countries/ZZ.md) | 0 | 0 | 0 | 0 |
| **Total** | **0** | **0** | **0** | **0** |

Les deux colonnes de fiches Park sont des sous-ensembles des totaux Park ; la colonne sans ID est un sous-ensemble des rapprochements incertains. Ce sont des dossiers à examiner, pas un nombre garanti d’attractions autonomes distinctes.

¹ Publié ne signifie pas « audité et complet aujourd’hui ». Date historique inconnue tant qu’aucune preuve datée n’est retrouvée ; la date du recensement ne la remplace pas.

² Ces dossiers peuvent désigner une fiche déjà présente ou un alias d’un candidat, notamment pour les noms japonais non encore rapprochés de leurs noms latins, ou une attraction dont le rattachement à un vrai parc reste à décider. Ils sont exclus des colonnes « sans fiche » pour éviter de présenter des doublons possibles comme de nouvelles créations.

## Relais France et Portugal

Les campagnes nationales doivent relire [FR](countries/FR.md) et [PT](countries/PT.md), rapprocher leurs derniers résultats du relevé, puis réserver les entrées par conversation ou branche avant le prochain lot. Les consignes de leurs anciens backlogs et de `AGENTS.md` renvoient vers ce registre. Il n’existe pas ici de canal de notification directe d’une autre conversation ChatGPT : ce relais versionné doit être repris dans ces conversations.

Une intégration achevée reste dans le registre avec son identifiant stable, sa date UTC réelle, sa preuve et son audit. Chaque intégration partielle ultérieure ajoute un événement ; aucune ligne ni aucun événement antérieur n’est effacé.
