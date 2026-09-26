# Backlog de complétude des parcs publiés — 2026-08-13

## Résultat

Audit initial effectué le **13 août 2026 à 07:26 CEST**, puis rafraîchi à **08:36 CEST**, avec le client technique `PARK_DATA_EDITOR`.

- 2 980 fiches ont été parcourues sur 60 pages.
- 190 parcs étaient publiés (`isVisible: true`).
- Les 190 scores ont été recalculés individuellement avec `Completeness`.
- 68 parcs atteignent le niveau `Excellent` : 66 satisfont la condition de sortie, avec un score strictement supérieur à 95, et 2 restent dans le backlog à 95.
- **124 parcs publiés avaient un score inférieur ou égal à 95 lors du rafraîchissement de référence** ; après 110 retraits validés et les 2 exceptions explicites enregistrées ci-dessous, le backlog actif contient **12 parcs** : 0 au niveau `Publishable`, 10 au niveau `Good` et 2 au niveau `Excellent`.
- Aucun écart n’a été relevé entre le score détaillé et le score résumé par la recherche.

La cible vient de [la spécification de scoring](../codex-guidelines/data-quality-completeness-scoring.md) et le traitement de cette liste suit le [workflow du backlog des parcs publiés](../codex-guidelines/published-park-backlog-workflow.md). Le critère d’entrée est désormais un score inférieur ou égal à 95 et la condition de sortie un score strictement supérieur à 95. Le score ne remplace jamais l’audit éditorial complet de l’étape 9 ni l’absence de bloqueur de publication.

## Règle de suivi

Traiter d’abord le groupe `Publishable`, du score le plus faible vers le plus élevé, puis le groupe `Good` et enfin les éventuelles fiches à 95. À score égal, trier par nom. Le garnissage, la publication ciblée, le contrôle Facebook anti-doublon, le seuil minimal de 96 et le retrait cumulatif d’une ligne sont définis par le workflow lié ci-dessus. Ce document n’autorise aucune suppression ou aucun masquage des données publiques du parc.

## Priorité 1 — niveau `Publishable` (0)

| Score | Niveau | Parc | Pays | Statut | Audience | Points | Identifiant |
| ---: | --- | --- | --- | --- | --- | ---: | --- |

## Priorité 2 — niveau `Good` (10)

| Score | Niveau | Parc | Pays | Statut | Audience | Points | Identifiant |
| ---: | --- | --- | --- | --- | --- | ---: | --- |
| 91 | `Good` | Bayside Fun Park | GB | `Operating` | `Local` | 94/103 | `58da350d-0bb4-461f-8f38-188bd59c13e1` |
| 91 | `Good` | La Récré des 3 Curés | FR | `Operating` | `Regional` | 89/98 | `9cebd5ae-dc2c-4c8c-a6a4-d2a2dda33d1c` |
| 92 | `Good` | AcroJungle Outdoor | FR | `Operating` | `Local` | 89/97 | `1566972b-fbf9-461c-ac92-5df9d6c3358e` |
| 93 | `Good` | Belli’s Mini-Freizeitpark | CH | `Operating` | `Local` | 95/102 | `73b59b2a-78e1-427d-900e-84946b617aeb` |
| 93 | `Good` | Denain Évasion | FR | `Operating` | `Local` | 95/102 | `acdd7664-cf2b-42c6-b05a-7f67ee307aee` |
| 93 | `Good` | Le Ch'ti Parc | FR | `Operating` | `Local` | 95/102 | `da07fda1-4b87-4142-85d2-23e4c0bbb585` |
| 93 | `Good` | Six Flags Qiddiya City | SA | `Operating` | `International` | 103/111 | `31da33cc-fc22-4abd-b474-217ae730a1ef` |
| 94 | `Good` | ABpark | LV | `Operating` | `National` | 103/109 | `153fb94d-ade2-4ff7-a245-bb40a022e355` |
| 94 | `Good` | BalatoniBob Szabadidőpark | HU | `Operating` | `Regional` | 95/101 | `eb2e46b1-7970-4eb5-b648-30a6d8ac290b` |
| 94 | `Good` | Bengtson's Pumpkin Farm | US | `Operating` | `Regional` | 92/98 | `430369d7-6665-4c3b-ae21-6121ef2a3733` |

## Priorité 3 — niveau `Excellent` au seuil (2)

| Score | Niveau | Parc | Pays | Statut | Audience | Points | Identifiant |
| ---: | --- | --- | --- | --- | --- | ---: | --- |
| 95 | `Excellent` | Babylon Park London | GB | `Operating` | `Regional` | 95/100 | `6c9557a4-c49c-4eb3-ace0-b1817927a0b3` |
| 95 | `Excellent` | Babylon Park Madrid | ES | `Operating` | `Regional` | 90/95 | `6d0efa82-473d-4bcd-a3fe-c839f1291917` |

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
