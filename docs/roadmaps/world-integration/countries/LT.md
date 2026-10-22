# Lituanie — backlog permanent

Relevé initial : 2026-10-22. Périmètre : Europe — périmètre national/territorial des sources.

Source de vérité : le fichier JSON du même pays. Ne jamais supprimer une entrée achevée ; utiliser le script de suivi pour enregistrer les intégrations et régénérer cette vue.

- 5 fiches étaient sans pays dans le site : rattachement par sources, à confirmer, sans modification de production.
- Les fiches publiées sont un repère de présence, pas un audit de complétude effectué aujourd’hui. Leur date d’intégration antérieure reste inconnue.
- Les lignes sans fiche sont des dossiers candidats, à confirmer avant création ; un zéro ne prouve pas l’absence de lieux dans le pays.
- NotRelevant est conservé avec son motif hérité ; ce classement ne suffit pas à exclure définitivement un vrai lieu. Les doublons legacy ne sont pas comptés deux fois.
- Couverture : Pistes sourcées ; compléments locaux requis. Un pays sans ligne n’est jamais déclaré terminé.

| État éditorial | Nombre |
| --- | ---: |
| À intégrer | 5 |
| À qualifier | 9 |
| En cours | 0 |
| Bloqué | 0 |
| Intégration achevée | 0 |
| Publié, date inconnue | 0 |
| Hors périmètre enregistré | 0 |
| Doublon conservé | 5 |

Couverture : **Pistes sourcées ; compléments locaux requis**. Observations externes : **14**. Sources : EAZA, RCDB, Wikipédia (découverte).

Pays proposé par rapprochement pour **5** fiches du site sans code pays au relevé ; confirmation à effectuer avant modification de production.

Parcs : **0 publiés**, **5 masqués à intégrer**, **0 masqués NotRelevant à réexaminer**, **8 candidats sans fiche**.

Standalone : **0 publiées**, **0 masquées**, **0 candidates sans fiche**. Autres dossiers : **0 types à décider**, **1 rapprochements incertains**, **0 anciens parcs liés à une standalone**.

Chaque candidat passe par une qualification avant création. Le type proposé ne prouve pas l’autonomie d’une attraction. Les signaux de fermeture ou d’exploitation ci-dessous sont ceux des sources, à dater et à vérifier.

| ID stable | Lieu | Localité | Type / proposition | Présence relevée | État | Dernière intégration UTC | Affectation | Suite / sources |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| LT-candidate-7dbda822e7770c | Druskininkų Vandens Parkas | Druskininkai | Park | NotFound | À qualifier | Inconnue — non inventée | — | Confirmer identité, adresse, cycle de vie et source officielle ; rechercher à nouveau noms, alias et coordonnées avant création. [Wikipédia (découverte)](https://en.wikipedia.org/wiki/List_of_water_parks_in_Europe#Lithuania) |
| LT-candidate-4c8afac788c344 | Kalitos Kalnas | Anykščiai , Utena | Park | NotFound | Doublon conservé | Inconnue — non inventée | — | Identité rapprochée pendant l’extension mondiale : conserver cette trace et suivre LT-park-6f147184-3e39-4207-8423-35c88a400959. Ne pas créer de doublon. Signal source : Operating. Cible : LT-park-6f147184-3e39-4207-8423-35c88a400959. [RCDB](https://rcdb.com/13248.htm) |
| LT-park-6f147184-3e39-4207-8423-35c88a400959 | Kalitos Kalnas | — | Park | Hidden | À intégrer | Inconnue — non inventée | — | Pays non renseigné dans le site au relevé ; rattachement proposé par source à confirmer. Intégration éditoriale complète : la fiche masquée est une coquille, pas une intégration achevée. Signal source : Operating. [Relevé du site — méthode](https://github.com/Ced59/AmusementParkProject/blob/master/docs/roadmaps/world-integration/METHODOLOGY.md) · [RCDB](https://rcdb.com/13248.htm) |
| LT-candidate-215e4051736bc5 | Liepkalnis | Vilnius | Park | NotFound | Doublon conservé | Inconnue — non inventée | — | Identité rapprochée pendant l’extension mondiale : conserver cette trace et suivre LT-park-b7c63cbc-80c1-454e-afcd-ffd41a7bcb76. Ne pas créer de doublon. Signal source : Operating. Cible : LT-park-b7c63cbc-80c1-454e-afcd-ffd41a7bcb76. [RCDB](https://rcdb.com/20361.htm) |
| LT-park-b7c63cbc-80c1-454e-afcd-ffd41a7bcb76 | Liepkalnis | — | Park | Hidden | À intégrer | Inconnue — non inventée | — | Pays non renseigné dans le site au relevé ; rattachement proposé par source à confirmer. Intégration éditoriale complète : la fiche masquée est une coquille, pas une intégration achevée. Signal source : Operating. [Relevé du site — méthode](https://github.com/Ced59/AmusementParkProject/blob/master/docs/roadmaps/world-integration/METHODOLOGY.md) · [RCDB](https://rcdb.com/20361.htm) |
| LT-candidate-66488cda2270e7 | Lietuvos Zoologijos Sodas (Kaunas Zoo) | — | Park | NotFound | À qualifier | Inconnue — non inventée | — | Confirmer identité, adresse, cycle de vie et source officielle ; rechercher à nouveau noms, alias et coordonnées avant création. [EAZA](https://www.eaza.net/membership/) · [Site officiel référencé](https://www.zoosodas.lt) |
| LT-candidate-3e9cebb8a48c54 | Lithuanian Sea Museum | Klaipėda | Park | NotFound | À qualifier | Inconnue — non inventée | — | Confirmer identité, adresse, cycle de vie et source officielle ; rechercher à nouveau noms, alias et coordonnées avant création. [Wikipédia (découverte)](https://en.wikipedia.org/wiki/List_of_aquaria#Lithuania) |
| LT-candidate-2f55168e2fe6b4 | Lithuanian Zoo | — | Park | NotFound | À qualifier | Inconnue — non inventée | — | Confirmer identité, adresse, cycle de vie et source officielle ; rechercher à nouveau noms, alias et coordonnées avant création. [Wikipédia (découverte)](https://en.wikipedia.org/wiki/List_of_zoos_by_country#Lithuania) |
| LT-candidate-5464edde20d57d | Palanga Sky Wheel | Palanga , Lithuania | À qualifier ; proposé : StandaloneAttraction | PossibleExisting | À qualifier | Inconnue — non inventée | — | Vérifier si cette grande roue est un parkItem dans un parc ou une attraction autonome ; ne pas la compter comme création indépendante avant résolution. Identifier l’attraction fixe et prouver son autonomie ; rechercher son ancien parc et ses parkItems avant création ou migration. [Wikipédia (découverte)](https://en.wikipedia.org/wiki/List_of_Ferris_wheels#Fixed_Ferris_wheels) |
| LT-candidate-c2962f1e7206c1 | Pramogu Kalnas | Birštonas , Kaunas | Park | NotFound | Doublon conservé | Inconnue — non inventée | — | Identité rapprochée pendant l’extension mondiale : conserver cette trace et suivre LT-park-5e5eaeea-abe0-4c15-93bd-1c6ea8efd69a. Ne pas créer de doublon. Signal source : Operating. Cible : LT-park-5e5eaeea-abe0-4c15-93bd-1c6ea8efd69a. [RCDB](https://rcdb.com/20411.htm) |
| LT-park-5e5eaeea-abe0-4c15-93bd-1c6ea8efd69a | Pramogu Kalnas | — | Park | Hidden | À intégrer | Inconnue — non inventée | — | Pays non renseigné dans le site au relevé ; rattachement proposé par source à confirmer. Intégration éditoriale complète : la fiche masquée est une coquille, pas une intégration achevée. Signal source : Operating. [Relevé du site — méthode](https://github.com/Ced59/AmusementParkProject/blob/master/docs/roadmaps/world-integration/METHODOLOGY.md) · [RCDB](https://rcdb.com/20411.htm) |
| LT-candidate-16b8c90819553a | Raubonių Pramogų Parkas | Raubonys , Panevėžys | Park | NotFound | Doublon conservé | Inconnue — non inventée | — | Identité rapprochée pendant l’extension mondiale : conserver cette trace et suivre LT-park-9e86c6c3-c1cc-4091-962a-bbbb2ca917b1. Ne pas créer de doublon. Signal source : Operating. Cible : LT-park-9e86c6c3-c1cc-4091-962a-bbbb2ca917b1. [RCDB](https://rcdb.com/21535.htm) |
| LT-park-9e86c6c3-c1cc-4091-962a-bbbb2ca917b1 | Raubonių Pramogų Parkas | — | Park | Hidden | À intégrer | Inconnue — non inventée | — | Pays non renseigné dans le site au relevé ; rattachement proposé par source à confirmer. Intégration éditoriale complète : la fiche masquée est une coquille, pas une intégration achevée. Signal source : Operating. [Relevé du site — méthode](https://github.com/Ced59/AmusementParkProject/blob/master/docs/roadmaps/world-integration/METHODOLOGY.md) · [RCDB](https://rcdb.com/21535.htm) |
| LT-candidate-e0b5b66b83418c | UNO Parks Druskininkai | — | Park | NotFound | À qualifier | Inconnue — non inventée | — | Confirmer identité, adresse, cycle de vie et source officielle ; rechercher à nouveau noms, alias et coordonnées avant création. [Wikipédia (découverte)](https://en.wikipedia.org/wiki/List_of_amusement_parks_in_Europe#Lithuania) |
| LT-candidate-9ac779692aa8b6 | UNO Parks Kaunas | — | Park | NotFound | À qualifier | Inconnue — non inventée | — | Confirmer identité, adresse, cycle de vie et source officielle ; rechercher à nouveau noms, alias et coordonnées avant création. [Wikipédia (découverte)](https://en.wikipedia.org/wiki/List_of_amusement_parks_in_Europe#Lithuania) |
| LT-candidate-4a85306c73e217 | UNO Parks Vilnius | — | Park | NotFound | À qualifier | Inconnue — non inventée | — | Confirmer identité, adresse, cycle de vie et source officielle ; rechercher à nouveau noms, alias et coordonnées avant création. [Wikipédia (découverte)](https://en.wikipedia.org/wiki/List_of_amusement_parks_in_Europe#Lithuania) |
| LT-candidate-4926d758155066 | Vaikų Pasaulis | Elektrenai , Vilnius | Park | NotFound | Doublon conservé | Inconnue — non inventée | — | Identité rapprochée pendant l’extension mondiale : conserver cette trace et suivre LT-park-70b7a082-aac3-4f13-b32b-940d153bc2b9. Ne pas créer de doublon. Signal source : SBNO. Cible : LT-park-70b7a082-aac3-4f13-b32b-940d153bc2b9. [RCDB](https://rcdb.com/5645.htm) |
| LT-park-70b7a082-aac3-4f13-b32b-940d153bc2b9 | Vaikų Pasaulis | — | Park | Hidden | À intégrer | Inconnue — non inventée | — | Pays non renseigné dans le site au relevé ; rattachement proposé par source à confirmer. Intégration éditoriale complète : la fiche masquée est une coquille, pas une intégration achevée. Signal source : SBNO. [Relevé du site — méthode](https://github.com/Ced59/AmusementParkProject/blob/master/docs/roadmaps/world-integration/METHODOLOGY.md) · [RCDB](https://rcdb.com/5645.htm) |
| LT-candidate-1583534a6ab51a | Vichy Vandens Parkas | Vilnius | Park | NotFound | À qualifier | Inconnue — non inventée | — | Confirmer identité, adresse, cycle de vie et source officielle ; rechercher à nouveau noms, alias et coordonnées avant création. [Wikipédia (découverte)](https://en.wikipedia.org/wiki/List_of_water_parks_in_Europe#Lithuania) |
