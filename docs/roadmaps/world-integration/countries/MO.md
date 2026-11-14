# R.A.S. chinoise de Macao — backlog permanent

Relevé initial : 2026-10-22. Périmètre : Asie — périmètre national/territorial des sources.

Source de vérité : le fichier JSON du même pays. Ne jamais supprimer une entrée achevée ; utiliser le script de suivi pour enregistrer les intégrations et régénérer cette vue.

- Les fiches publiées sont un repère de présence, pas un audit de complétude effectué aujourd’hui. Leur date d’intégration antérieure reste inconnue.
- Les lignes sans fiche sont des dossiers candidats, à confirmer avant création ; un zéro ne prouve pas l’absence de lieux dans le pays.
- NotRelevant est conservé avec son motif hérité ; ce classement ne suffit pas à exclure définitivement un vrai lieu. Les doublons legacy ne sont pas comptés deux fois.
- Couverture : Pistes sourcées ; compléments locaux requis. Un pays sans ligne n’est jamais déclaré terminé.
- Recherche externe dédiée aux absences : 1 observations d’opérateurs et d’Aerophile, y compris références historiques. Comparer avec les fiches masquées ; qualifier le cycle de vie avant création.

| État éditorial | Nombre |
| --- | ---: |
| À intégrer | 0 |
| À qualifier | 3 |
| En cours | 0 |
| Bloqué | 0 |
| Intégration achevée | 1 |
| Publié, date inconnue | 0 |
| Hors périmètre enregistré | 0 |
| Doublon conservé | 0 |

Couverture : **Pistes sourcées ; compléments locaux requis**. Observations externes : **3**. Sources : Découverte complémentaire — site officiel, Wikipédia (découverte).

Pays proposé par rapprochement pour **0** fiches du site sans code pays au relevé ; **0** code(s) déjà renseigné(s) corrigé(s) sur source. Confirmation à effectuer avant modification de production.

Parcs : **0 publiés**, **0 masqués à intégrer**, **0 masqués NotRelevant à réexaminer**, **2 candidats sans fiche**.

Standalone : **1 publiées**, **0 masquées**, **0 candidates sans fiche**. Autres dossiers : **0 types à décider**, **1 rapprochements incertains**, **0 anciens parcs liés à une standalone**.

Revue standalone : **0 pistes dans des fiches Park masquées**, **0 dans des fiches Park publiées**, **1 dossiers sans ID dont l’identité ou le parent reste à résoudre**, **0 dossiers rattachés à un vrai parc**. Ces sous-ensembles ne doivent pas être ajoutés une seconde fois aux totaux précédents.

Chaque candidat passe par une qualification avant création. Le type proposé ne prouve pas l’autonomie d’une attraction. Les signaux de fermeture ou d’exploitation ci-dessous sont ceux des sources, à dater et à vérifier.

| ID stable | Lieu | Localité | Type / proposition | Présence relevée | État | Dernière intégration UTC | Affectation | Suite / sources |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| MO-candidate-0129b0d51125b7 | Flora Garden | — | Park | NotFound | À qualifier | Inconnue — non inventée | — | Confirmer identité, adresse, cycle de vie et source officielle ; rechercher à nouveau noms, alias et coordonnées avant création. [Wikipédia (découverte)](https://en.wikipedia.org/wiki/List_of_zoos_by_country#Macau) |
| MO-candidate-discovery-ed48ddea6705793a | Golden Reel | Studio City, Cotai, Macao | StandaloneAttraction | Published | Intégration achevée | 2026-11-14T19:16:42.686Z | — | Conserver la ligne et ses reçus ; vérifier les jours de fermeture du calendrier officiel avant de structurer les horaires et préciser les coordonnées de l’entrée au niveau 3, aile est. Qualification standalone : AlreadyStandalone. Grande roue fixe avec billet propre dans le complexe hôtelier Studio City, sans parc d’attractions parent ; fiche autonome créée et publiée après recherche de doublons. Signal source : Offre opérateur active au 14 novembre 2026, avec billet MOP 100 et horaires affichés.. [Exploitant — Golden Reel](https://www.studiocity-macau.com/en/attractions/golden-reel) · [Exploitant — adresse de Studio City](https://www.studiocity-macau.com/en/contact-us) · [Wikimedia Commons — Studio City Lucky 8](https://commons.wikimedia.org/wiki/File:Studio-City-Lucky-8.jpg) · [Wikimedia Commons — Golden Reel 2023](https://commons.wikimedia.org/wiki/File:Studio_City_Golden_Reel_26-02-2023.jpg) |
| MO-candidate-0503181def7dc8 | Macao Giant Panda Pavilion | — | Park | NotFound | À qualifier | Inconnue — non inventée | — | Confirmer identité, adresse, cycle de vie et source officielle ; rechercher à nouveau noms, alias et coordonnées avant création. [Wikipédia (découverte)](https://en.wikipedia.org/wiki/List_of_zoos_by_country#Macau) |
| MO-candidate-discovery-64f20a87c649ce | Skypark Macau Tower — Bungy / Skyjump | Macao | À qualifier ; proposé : StandaloneAttraction | PossibleExisting | À qualifier | Inconnue — non inventée | — | Rechercher Skypark Macau Tower, AJ Hackett Macau, Bungy et Skyjump dans Park et StandaloneAttraction avec le client officiel, puis décider si chaque billet correspond à une standalone distincte ou à un parkItem. Deux photos libres du Bungy sont vérifiées ; ne pas les attribuer au Skyjump. Recontrôler tarifs et horaires avant Preview/Apply. Dossier : evidence/standalone-skypark-qualification-20261114/audit.json. Qualification standalone : ParentToResolve. Deux activités fixes avec billets propres, dans la tour touristique. Leur modélisation et l’existence éventuelle d’une fiche legacy doivent être vérifiées par le client officiel ; les photos du Bungy ne représentent pas Skyjump. Installations référencées : Skypark Macau Tower — Bungy / Skyjump ; Macau Tower Bungy Jump ; Macau Tower Skyjump. Signal source : Catalogue officiel de Skypark Macau Tower consulté le 14 novembre 2026 : Bungy et Skyjump sont tarifés séparément ; exploitation effective à vérifier à la date d’intégration.. [Découverte complémentaire — site officiel](https://www.skyparkglobal.com/) · [Exploitant — activités et billets](https://www.skyparkmacau.com/activities) · [Exploitant — Bungy Jump](https://www.skyparkmacau.com/activities/bungy) · [Exploitant — Skyjump](https://www.skyparkmacau.com/activities/skyjump) · [Exploitant — adresse et horaires](https://www.skyparkmacau.com/park-info) · [Photo exacte du Bungy — Sylvain Kalache, CC BY 2.0](https://commons.wikimedia.org/wiki/File:Bungee_jumping_from_Macau_Tower,_2009_(3455431798).jpg) · [Photo de la zone Bungy — LN9267, CC BY-SA 4.0](https://commons.wikimedia.org/wiki/File:Macau_Tower_Level_61_Bungee_Jumping_16-03-2023(1).jpg) |
