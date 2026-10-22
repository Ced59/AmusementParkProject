# Monténégro — backlog permanent

Relevé initial : 2026-10-22. Périmètre : Europe — périmètre national/territorial des sources.

Source de vérité : le fichier JSON du même pays. Ne jamais supprimer une entrée achevée ; utiliser le script de suivi pour enregistrer les intégrations et régénérer cette vue.

- 1 fiches étaient sans pays dans le site : rattachement par sources, à confirmer, sans modification de production.
- Les fiches publiées sont un repère de présence, pas un audit de complétude effectué aujourd’hui. Leur date d’intégration antérieure reste inconnue.
- Les lignes sans fiche sont des dossiers candidats, à confirmer avant création ; un zéro ne prouve pas l’absence de lieux dans le pays.
- NotRelevant est conservé avec son motif hérité ; ce classement ne suffit pas à exclure définitivement un vrai lieu. Les doublons legacy ne sont pas comptés deux fois.
- Couverture : Pistes sourcées ; compléments locaux requis. Un pays sans ligne n’est jamais déclaré terminé.
- Revue standalone du 22 octobre : 2 observations supplémentaires d’installations, avant dédoublonnage. Le constructeur ou RCDB ne prouve pas l’autonomie par rapport à un parc.
- Recherche externe dédiée aux absences : 1 observations d’opérateurs et d’Aerophile, y compris références historiques. Comparer avec les fiches masquées ; qualifier le cycle de vie avant création.

| État éditorial | Nombre |
| --- | ---: |
| À intégrer | 1 |
| À qualifier | 3 |
| En cours | 0 |
| Bloqué | 0 |
| Intégration achevée | 0 |
| Publié, date inconnue | 0 |
| Hors périmètre enregistré | 0 |
| Doublon conservé | 1 |

Couverture : **Pistes sourcées ; compléments locaux requis**. Observations externes : **5**. Sources : Aerophile — références constructeur, RCDB, RCDB — Mountain Coaster, Wiegand — références constructeur, Wikipédia (découverte).

Pays proposé par rapprochement pour **1** fiches du site sans code pays au relevé ; **0** code(s) déjà renseigné(s) corrigé(s) sur source. Confirmation à effectuer avant modification de production.

Parcs : **0 publiés**, **1 masqués à intégrer**, **0 masqués NotRelevant à réexaminer**, **1 candidats sans fiche**.

Standalone : **0 publiées**, **0 masquées**, **1 candidates sans fiche**. Autres dossiers : **0 types à décider**, **1 rapprochements incertains**, **0 anciens parcs liés à une standalone**.

Revue standalone : **1 pistes dans des fiches Park masquées**, **0 dans des fiches Park publiées**, **1 dossiers sans ID dont l’identité ou le parent reste à résoudre**, **0 dossiers rattachés à un vrai parc**. Ces sous-ensembles ne doivent pas être ajoutés une seconde fois aux totaux précédents.

Chaque candidat passe par une qualification avant création. Le type proposé ne prouve pas l’autonomie d’une attraction. Les signaux de fermeture ou d’exploitation ci-dessous sont ceux des sources, à dater et à vérifier.

| ID stable | Lieu | Localité | Type / proposition | Présence relevée | État | Dernière intégration UTC | Affectation | Suite / sources |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| ME-candidate-discovery-f3bcd76cc9b9ff | Aerobar Kotor | Lovćen, Kotor | À qualifier ; proposé : StandaloneAttraction | NotFound | À qualifier | Inconnue — non inventée | — | Installation documentée sur un site touristique ou dédié ; aucun rapprochement établi avec une fiche du site. Confirmer identité, autonomie et cycle de vie avant création. Comparer les noms, alias, adresse, coordonnées et anciennes identités dans SearchParks et SearchStandaloneAttractions, sans filtre de visibilité ; élargir hors pays si nécessaire. Ne pas présenter une référence historique comme une exploitation actuelle. Qualification standalone : IndependentCandidate. Installation documentée sur un site touristique ou dédié ; aucun rapprochement établi avec une fiche du site. Confirmer identité, autonomie et cycle de vie avant création. Installations référencées : Aerobar Kotor. Signal source : Référence constructeur datée 2026 ; exploitation actuelle et permanence à vérifier.. [Aerophile — références constructeur](https://www.aerophile.com/en/clients/aerobar-kotor/) · [Aerophile — références constructeur — annuaire](https://www.aerophile.com/en/clients/) |
| ME-candidate-standalone-d54c8880740709 | Alpine Coaster Wiegand — Lovcen | — | À qualifier ; proposé : StandaloneAttraction | PossibleExisting | À qualifier | Inconnue — non inventée | — | Installation fixe documentée ; identité exacte, appartenance à un vrai parc ou autonomie encore à résoudre. Ne pas compter comme création standalone certaine. Rechercher noms, adresse et coordonnées dans Park et StandaloneAttraction via le client officiel avant toute décision. Qualification standalone : ParentToResolve. Installation fixe documentée ; identité exacte, appartenance à un vrai parc ou autonomie encore à résoudre. Ne pas compter comme création standalone certaine. Installations référencées : Lovcen. [Wiegand — références constructeur](https://www.wiegandslide.com/en/products/alpine-coaster/references.html?wgrls=381#location-id-2177) |
| ME-candidate-a68e4666c5e6a1 | Aqua park Mediteran | Bečići | Park | NotFound | À qualifier | Inconnue — non inventée | — | Confirmer identité, adresse, cycle de vie et source officielle ; rechercher à nouveau noms, alias et coordonnées avant création. [Wikipédia (découverte)](https://en.wikipedia.org/wiki/List_of_water_parks_in_Europe#Montenegro) |
| ME-candidate-8494836087bd41 | Kotor Cable Car | Kavač , Kotor | Park | NotFound | Doublon conservé | Inconnue — non inventée | — | Identité rapprochée pendant l’extension mondiale : conserver cette trace et suivre ME-park-e82ce40d-f7b9-41cc-bbde-128ecce04308. Ne pas créer de doublon. Signal source : Operating. Cible : ME-park-e82ce40d-f7b9-41cc-bbde-128ecce04308. [RCDB](https://rcdb.com/21529.htm) |
| ME-park-e82ce40d-f7b9-41cc-bbde-128ecce04308 | Kotor Cable Car | — | Park | Hidden | À intégrer | Inconnue — non inventée | — | Piste d’attraction autonome dans un domaine touristique ou sur un site dédié. Proposition à confirmer avec l’exploitant et la recherche des anciennes identités ; aucune migration automatique. Fiche Park existante : vérifier une migration vers StandaloneAttraction plutôt que créer un doublon. Rechercher noms, adresse et coordonnées dans Park et StandaloneAttraction via le client officiel avant toute décision. Qualification standalone : IndependentCandidate. Piste d’attraction autonome dans un domaine touristique ou sur un site dédié. Proposition à confirmer avec l’exploitant et la recherche des anciennes identités ; aucune migration automatique. Installations référencées : Alpine Coaster. Signal source : Operating. [Relevé du site — méthode](https://github.com/Ced59/AmusementParkProject/blob/master/docs/roadmaps/world-integration/METHODOLOGY.md) · [RCDB](https://rcdb.com/21529.htm) · [RCDB — Mountain Coaster](https://rcdb.com/21530.htm) |
