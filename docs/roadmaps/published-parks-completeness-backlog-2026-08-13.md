# Backlog de complétude des parcs publiés — 2026-08-13

## Résultat

Audit initial effectué le **13 août 2026 à 07:26 CEST**, puis rafraîchi à **08:36 CEST**, avec le client technique `PARK_DATA_EDITOR`.

- 2 980 fiches ont été parcourues sur 60 pages.
- 190 parcs étaient publiés (`isVisible: true`).
- Les 190 scores ont été recalculés individuellement avec `Completeness`.
- 68 parcs atteignaient le niveau `Excellent` et satisfaisaient la condition de sortie lors du rafraîchissement de référence, avec un score strictement supérieur à 95.
- **124 parcs publiés avaient un score inférieur ou égal à 95 lors du rafraîchissement de référence** ; les 122 retraits validés et les 2 exceptions explicites enregistrées ci-dessous avaient entièrement vidé cette liste au 28 septembre 2026.
- Un inventaire différentiel ciblé sur l’Allemagne, effectué le **1er octobre 2026 à 18:53 CEST** sur les 60 pages de recherche, a contrôlé individuellement les 246 fiches allemandes et retrouvé 7 parcs publics au seuil de 95. BELANTIS a été réintégré puis retiré le **2 octobre 2026** après audit complet, publication des données et médias, score réel de **100 (108/108)** sans bloqueur et réconciliation réussie de son annonce Facebook existante. Le backlog actif contient donc désormais **6 parcs**.
- Aucun écart n’a été relevé entre le score détaillé et le score résumé par la recherche.

La cible vient de [la spécification de scoring](../codex-guidelines/data-quality-completeness-scoring.md) et le traitement de cette liste suit le [workflow du backlog des parcs publiés](../codex-guidelines/published-park-backlog-workflow.md). Le critère d’entrée est désormais un score inférieur ou égal à 95 et la condition de sortie un score strictement supérieur à 95. Le score ne remplace jamais l’audit éditorial complet de l’étape 9 ni l’absence de bloqueur de publication.

## Règle de suivi

Traiter d’abord le groupe `Publishable`, du score le plus faible vers le plus élevé, puis le groupe `Good` et enfin les éventuelles fiches à 95. À score égal, trier par nom. Le garnissage, la publication ciblée, le contrôle Facebook anti-doublon, le seuil minimal de 96 et le retrait cumulatif d’une ligne sont définis par le workflow lié ci-dessus. Ce document n’autorise aucune suppression ou aucun masquage des données publiques du parc.

## Priorité 1 — niveau `Publishable` (0)

| Score | Niveau | Parc | Pays | Statut | Audience | Points | Identifiant |
| ---: | --- | --- | --- | --- | --- | ---: | --- |

## Priorité 2 — niveau `Good` (0)

| Score | Niveau | Parc | Pays | Statut | Audience | Points | Identifiant |
| ---: | --- | --- | --- | --- | --- | ---: | --- |

## Priorité 3 — niveau `Excellent` au seuil (6)

| Score | Niveau | Parc | Pays | Statut | Audience | Points | Identifiant |
| ---: | --- | --- | --- | --- | --- | ---: | --- |
| 95 | Excellent | Bayern-Park | Allemagne | Operating | Regional | 106/109 | `b297b32c-0ac9-4e19-ab36-a5cbe07198b3` |
| 95 | Excellent | Europa-Park | Allemagne | Operating | International | 112/115 | `81643366-3581-44a0-8b9b-78d5dcd513cf` |
| 95 | Excellent | Miramar Weinheim | Allemagne | Operating | Regional | 105/108 | `7787637d-e506-4b9f-83fa-ac3aca8a969d` |
| 95 | Excellent | Movie Park Germany | Allemagne | Operating | International | 113/115 | `da14fdc3-bbda-4eb7-aa3a-0ec8daf05717` |
| 95 | Excellent | Phantasialand | Allemagne | Operating | International | 112/115 | `50555bb2-abd4-4c3a-b0d2-b2fa5f547c6e` |
| 95 | Excellent | Rulantica | Allemagne | Operating | International | 112/114 | `7b22baed-4cc2-434f-b911-5b4667c74f1e` |

## Exceptions explicitement acceptées

| Parc | Décision | Score courant | Lacunes acceptées | Publication Facebook |
| --- | --- | ---: | --- | --- |
| Al-Qidah Park (`3212fabf-2a98-4af0-9111-917461fccbb5`) | Exception utilisateur du 2026-08-13 à 17:26 Europe/Paris, après audit complet sans bloqueur | 94 (95/101) | Constructeur des attractions mécaniques et conditions d’accès non établis par les sources disponibles | `Published` — [publication](https://www.facebook.com/1285475681307050/posts/122109939327424431) |
| DraculaLand (`f81b3b4c-b1d7-45ae-a7ab-7d2ee8f7e059`) | Exception utilisateur du 2026-09-11 à 22:48 Europe/Paris, après audit complet sans bloqueur ; souplesse explicitement accordée pour un projet encore non construit | 93 (85/91) | Coordonnée générale encore approximative ; aucune localisation interne fiable pour les quarante parkItems annoncés ; conditions d’accès et constructeurs non publiés ; calendrier 2027–2028 provisoire ; une seule attraction possède une image individuelle exacte, tandis que les cinq vues générales sont des rendus conceptuels officiels | `Published` — [publication](https://www.facebook.com/1285475681307050/posts/122116820649424431) |

## Projets publiés à surveiller périodiquement

Cette liste conserve les projets publiés dont les données doivent être réexaminées après les prochaines annonces officielles. Une revue trimestrielle est indicative ; une annonce de chantier, un nouveau dossier investisseur, un plan révisé, une date d’ouverture consolidée ou la publication de règles d’accès déclenche une vérification anticipée. Le contrôle repasse par le workflow `PARK_DATA_EDITOR` et ne remplace pas un nouvel audit avant toute modification publique.

| Projet | Dernier audit | Prochaine revue indicative | Points à vérifier |
| --- | --- | --- | --- |
| DraculaLand (`f81b3b4c-b1d7-45ae-a7ab-7d2ee8f7e059`) | 2026-09-11 — score publié 93 (85/91), sans bloqueur | Décembre 2026, ou dès une nouvelle annonce officielle | Emplacement exact et périmètre du chantier ; coordonnées des parkItems ; constructeurs et modèles ; conditions d’accès ; calendrier d’ouverture ; évolutions du masterplan et de l’inventaire annoncé ; remplacement progressif des rendus par des photographies de chantier puis du site réel |
