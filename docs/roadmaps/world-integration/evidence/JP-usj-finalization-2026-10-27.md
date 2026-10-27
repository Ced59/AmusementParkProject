# Universal Studios Japan — preuve de reprise du 27 octobre 2026

Entrée permanente : `JP-park-6353f6a4-d951-4f0f-b99a-5a0804b642a9`. Parc : `6353f6a4-d951-4f0f-b99a-5a0804b642a9`.

## État constaté

Les données du parc sont publiques et vérifiées. La finalisation des quatre annonces Facebook reste bloquée par le contrôle visuel mobile : l’outil Windows a arrêté la capture car il ne pouvait pas identifier l’URL courante du navigateur avec assez de certitude. Aucun contrôle visuel réussi ni aucune exception humaine ne sont revendiqués. Aucune annonce Facebook USJ n’a été envoyée par cette reprise.

L’autorisation humaine de publication du parc avec ses lacunes a été donnée dans la conversation `01a14eb0-f600-7243-84f6-a79a4961d539`. La demande initiale autorise Facebook pour les articles. Le contrôle mobile, obligatoire avant l’annonce selon l’étape 9, reste à vérifier manuellement ou à lever par une exception explicite. Les brouillons officiels sont résolus, pas publiés.

## Couverture vérifiée

| Élément | Résultat |
| --- | --- |
| Fiches visibles et descriptions dans huit langues | 202 / 202 |
| Répartition | 68 attractions, 38 spectacles, 34 restaurants, 36 boutiques, 26 services |
| Zones | 11 |
| Statuts des attractions | 37 en activité, 2 fermées temporairement, 1 annoncée, 28 arrêtées ou retirées ; aucun statut non canonique |
| Images | 140 au total ; 135 publiées, dont un logo MACK préexistant ; 5 conservées privées |
| Images nouvelles publiées pendant l’intégration initiale | 134 ; aucun import ni remplacement pendant cette reprise |
| Attractions avec image publiable | 52 / 68 ; dont 36 / 37 en activité |
| Spectacles avec image publiable | 24 / 38 |
| Jalons actifs | 70 publiés, 0 brouillon, 0 bloqueur HIST ; 70 sourcés |
| Anciennes ressources rétractées | 154 conservées dans l’historique des révisions ; aucune réactivation ciblée |
| Articles | 4, chacun dans huit langues et au moins 250 mots de paragraphes par langue |
| Coordonnées précises | 105 / 202 fiches |
| Plan | 1 plan officiel thématique du 25e anniversaire, édition 2026 ; aucune prétention de plan général annuel |
| Complétude réellement publiée | 98 / 100, Excellent, 113 / 115 points, aucun bloqueur de publication |
| Preview final | 424 entités inchangées, 0 création, modification, suppression, erreur ou avertissement |

## Vérifications publiques

49 réponses anonymes validées : huit pages du parc, 32 versions localisées des quatre articles, deux fiches de parkItem, deux pages de frise et cinq lectures API publiques. Les contrôles portent sur HTTP 200, contenu HTML rendu, titres, canonique, Open Graph localisé, images d’articles et absence de GUID ou d’enum brut dans les textes français contrôlés. Les premiers et derniers paragraphes des articles sont présents dans le HTML initial.

`hasPublicHistory` est vrai dans le résumé public. La frise du parc et les historiques de trois parkItems renvoient des événements visibles. Les pages de frise conservent volontairement `noindex,follow` : le code SEO conditionne leur indexation à `hasDecisionSnapshots`, faux pour ce parc. Ce comportement existant n’a pas été modifié ; les pages du parc, fiches et articles contrôlés restent indexables.

## Mutation éditoriale et dates réelles

Une phrase du troisième paragraphe espagnol de l’article Curious George a été reformulée pour ne plus reprendre la même séquence que le résumé. Les dates, le sens, la structure, les sept autres langues et la visibilité sont conservés. Apply `f9d9b81a-a511-4734-85d3-7c7e3629c82a`, `appliedAtUtc: 2026-10-27T07:06:38.3309469Z`.

Cette mutation de contenu constitue la dernière intégration de données enregistrée : `2026-10-27T07:06:38.330Z`, à la précision milliseconde acceptée par le registre. La précision originale du reçu est conservée ci-dessus. La date de l’intégration initiale ne fait pas l’objet d’un événement reconstruit à partir du relevé, d’un audit ou d’un commit.

## Réparation de publication HIST

Le correctif équivalent à l’ancienne PR #918 était déjà fusionné dans [la PR #925](https://github.com/Ced59/AmusementParkProject/pull/925), déployée avec succès par le workflow `39579602609`. La PR #918 a été fermée comme redondante. Aucun nouveau code ni changement de version n’a été nécessaire pour cette reprise.

Les quatre lots ci-dessous rejouent les mêmes identités narratives depuis l’export limité à History après la correction espagnole. Preview et Apply ont été contrôlés : seules les restaurations canoniques attendues, sans changement des récits, périodes, sources ou images. Le fait de l’article espagnol était déjà publié après sa mutation ; les autres 69 ont été réparés. Ces dates de publication restent distinctes de la date d’intégration éditoriale ci-dessus.

| Lot | Opération Apply | Date UTC retournée | Révisions restaurées |
| --- | --- | --- | ---: |
| history-repair-01 | `a902b41c-ca7c-42d3-ac9b-b0152247f70e` | `2026-10-27T07:09:58.2505105Z` | 20 |
| history-repair-02 | `93f0b163-3de8-4bf5-8e22-22db9d651212` | `2026-10-27T07:11:12.2591605Z` | 20 |
| history-repair-03 | `1ff25b56-d444-4ada-a3c6-92d68d11e74c` | `2026-10-27T07:11:30.597349Z` | 20 |
| history-repair-04 | `9047e353-c0c1-4689-b1ac-ae6104200119` | `2026-10-27T07:11:48.5426366Z` | 9 |

## Articles et brouillons Facebook

- [Back to the Future à Osaka : les adieux de mai 2016](https://amusement-parks.fun/fr/park/6353f6a4-d951-4f0f-b99a-5a0804b642a9/universal-studios-japan/item/27ce05c4-9ca3-48cc-8bf5-ff7f24da7d04/back-to-the-future-the-ride/history/3ad17b38-4262-4229-a114-910a50acc0ec/back-to-the-future-osaka-adieux-2016) — brouillon bilingue officiel résolu ; image Open Graph conservée par défaut ; envoi en attente du contrôle mobile.
- [Spider-Man à Osaka : vingt ans d’aventures et un dernier adieu](https://amusement-parks.fun/fr/park/6353f6a4-d951-4f0f-b99a-5a0804b642a9/universal-studios-japan/item/a8f4c266-efcb-41df-aeeb-d8f167daca71/the-amazing-adventures-of-spider-man-the-ride/history/cdc2e83d-edec-4374-b996-f4e2f3d20443/spider-man-osaka-2004-2024) — brouillon bilingue officiel résolu ; image Open Graph conservée par défaut ; envoi en attente du contrôle mobile.
- [Universal Studios Japan : vingt-cinq ans de mondes à Osaka](https://amusement-parks.fun/fr/park/6353f6a4-d951-4f0f-b99a-5a0804b642a9/universal-studios-japan/history/97eb69d9-0449-405a-88d1-fc83f8afe383/universal-studios-japan-25-ans-mondes-osaka) — brouillon bilingue officiel résolu ; image Open Graph conservée par défaut ; envoi en attente du contrôle mobile.
- [Playing with Curious George : un dernier chapitre le 28 février 2027](https://amusement-parks.fun/fr/park/6353f6a4-d951-4f0f-b99a-5a0804b642a9/universal-studios-japan/item/81684726-d6af-42bd-b180-92d19ce5e8b8/playing-with-curious-george/history/38ebd69d-0bcd-40f3-8a54-351f789b5cef/playing-with-curious-george-final-2027) — brouillon bilingue officiel résolu ; image Open Graph conservée par défaut ; envoi en attente du contrôle mobile.

## Lacunes conservées, acceptées dans l’audit initial

Horaires complets non confirmés ; seuls le 18 octobre 2026 et le 1er janvier 2027 sont renseignés. Pas de plan général annuel complet ni d’archives de plans avec année et fichier vérifiés. Aucun fondateur unique attribué sans preuve. Dernière exploitation non datée précisément pour Animation Celebration, Backdraft et Terminator 2:3-D. Quatre pass annuels historiques de 2025 sont conservés ; les grilles plus anciennes comparables restent limitées.

97 fiches restent sans coordonnées précises. Cinq références liées n’ont pas de logo distinct importable établi. Les changements de participation de Comcast différés par l’intégration initiale n’ont pas été ajoutés dans cette reprise de publication. Le quota de trois photographies distinctes était atteint pour 16 des 90 fiches importantes lors de l’audit accepté ; cette reprise ne modifie aucun média. Les exceptions de formats courts concernent 49 fiches documentées lors de cet audit.

Les images historiques et illustrations gardent leurs légendes de contexte ; elles ne sont pas présentées comme des photographies d’un événement non documenté.

### Attractions sans image publiable

- Abby's Magical Garden — `5f663566-af48-43d9-9865-ee655ce405ee`.
- Abby's Magical Party — `d63307c9-4104-47d7-8549-bbef60fc8457`.
- Abby's Magical Tree — `4f8ae6d3-877a-4e1b-9de7-b027bcbae843`.
- Amity Boardwalk Games — `c681efc7-a634-47be-8950-0d406e9578f8`.
- Bert and Ernie's Wonder - The Sea — `815b9644-ee3b-4334-8ec5-b05854683095`.
- Big Bird's Big Nest — `bbad68af-4657-4864-95cf-5111e9556310`.
- Big Bird’s Climbing Nest — `1384022f-69e7-42a2-b379-0355d2490236`.
- Cookie Monster Slide — `b290f8aa-b335-47fb-97ac-00dbf081ff9f`.
- Elmo's Little Drive — `1c5af6eb-b12f-43dc-a99d-36ec2bf7f7c8`.
- Ernie's Rubber Duckie Race — `761a213b-4857-4acb-a340-3a00e0508565`.
- Grover's Construction Company — `ffe9e137-2ff0-4343-9005-0a78f0d311f6`.
- Moppy's Lucky Dance Party — `bb96a542-bf4c-4af8-aab6-48c955b01ac3`.
- Peppermint Patty's Stunt Slide — `d1762caa-f373-4234-ac6e-a8a9a7b99d3e`.
- Sesame's Big Drive — `430f38aa-7a90-4570-9940-6526ede9a1c9`.
- TV Production Tour — `18c3a0ea-8866-45ee-933a-6e3abef92eda`.
- Universal Studios Motion Picture Magic — `6ce26362-0db3-442b-aec0-960dea1f3549`.

### Spectacles sans image publiable

- Fantastic World — `3a951721-7d22-42b9-add9-534447ccf51f`.
- Happy Harmony Celebration — `f8c9ab3c-4554-4ef1-9c13-83ee1ef43c42`.
- Hollywood Magic — `9c7ad85a-18d7-4001-9cec-4eb5c1166931`.
- Hollywood Premier Parade — `c4cce90a-da21-446c-9c96-054ea6872331`.
- Magical Starlight Parade — `e0293511-0c33-460c-ab31-24d811ab64d5`.
- Monster Make-Up — `3376cd2e-2a6f-46ae-aaf2-764aa807dcf6`.
- Peter Pan's Neverland — `822b1fa1-a466-4461-b9ce-c19d938266e2`.
- Smiley’s Happy Halloween Greeting — `f4de7c7c-13cf-405e-a4ff-7f83b76d23d0`.
- Snoopy’s Action Stage — `491cce7f-890f-4050-a9ec-c92059e053b6`.
- SUPER NINTENDO WORLD Meet-Up — `bf46d621-e738-44d1-8758-b502c840e907`.
- The Wild Wild Wild West Stunt Show — `eb2a1faa-8ccd-40a5-863e-190fd4c98d32`.
- Toto & Friends — `26a25ffe-bbda-4997-8a97-4855e156ef96`.
- Universal Spectacle Night Parade: The Best of Hollywood — `cbb2d7e3-5731-4ef7-abab-798c82a63eb7`.
- WICKED — Land of Oz — `2b954551-7c35-444f-bd8e-5b407b2c6b16`.

### Images maintenues privées

- `0ba887d560644063aa51ec7db7c30573`.
- `2f792795a9ae4c8482951045ff349354`.
- `3e7d9eee78a94258a1ac69133f131374`.
- `678d2b2d6e8742869470c38f31e52ce0`.
- `cf9c188767954af79cf1f621c65739f9`.

## Traçabilité et reprise

Les exports complets, reçus bruts et données privées restent hors du dépôt. Les identifiants d’opérations ci-dessus permettent le rapprochement dans l’historique officiel. Les scripts et preuves de reprise sont conservés dans le répertoire de tâche local persistant, et non dans le dossier temporaire nettoyé. Aucun jeton ni identifiant de connexion n’est versionné.

Pertinence de la prochaine étape : aucune étape officielle ne suit l’étape 9. La réparation des données est vérifiée ; le contrôle visuel mobile et les quatre envois Facebook constituent la finalisation encore en attente.
