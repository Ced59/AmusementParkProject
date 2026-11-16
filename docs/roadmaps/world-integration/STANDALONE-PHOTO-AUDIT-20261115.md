# Photographies des attractions standalone — contrôle mondial du 15 novembre 2026

Le contrôle de [toutes les 232 fiches standalone publiées du backlog](evidence/standalone-photo-coverage-20261115/audit.json) à **03:43:59 UTC** donne :

| Images propres publiées | Fiches |
| --- | ---: |
| Aucune | **6** |
| Une | **27** |
| Deux ou plus | **199** |
| Total | **232** |

La requête publique `/api/images/STANDALONE_ATTRACTION/{siteId}/STANDALONE_ATTRACTION?page=1&size=1` a répondu HTTP 200 pour chaque fiche ; son `pagination.totalItems` compte uniquement les images publiées appartenant à cette attraction. Un logo générique, une image du Park parent ou une image encore masquée ne satisfait pas la règle. Le relevé exclut les entrées techniques `Duplicate` et `Excluded`. Il mesure la **présence** des images ; la pertinence visuelle et la licence doivent encore être vérifiées image par image lors des revues éditoriales. Les deux images ajoutées à SkyHelix Sentosa et les deux vues historiques datées ajoutées à Skyline Luge Mont-Tremblant sont incluses dans ce total.

## Six fiches publiques sans photo propre

| Pays | Fiche | Suite à donner |
| --- | --- | --- |
| Émirats arabes unis | [The Dubai Balloon at Atlantis](https://amusement-parks.fun/fr/attraction/b5460e06-51ba-4b52-81bd-445d3f144f8d/the-dubai-balloon-at-atlantis) | Trouver une vue du ballon captif lui-même, vérifier les droits, puis contrôler la reprise des vols. |
| Autriche | [Speed-Gleit-Bahn](https://amusement-parks.fun/fr/attraction/e97f373c-a9b4-41a9-9111-92f29771fd49/speed-gleit-bahn) | Obtenir une vue de la piste de Stoaninger Alm, pas d’une autre luge, et vérifier la prochaine saison. |
| Canada | [Eagle’s Flight ZipTour](https://amusement-parks.fun/fr/attraction/1cb184a8-6579-4aae-88ba-365e15542a09/eagles-flight-ziptour-mount-washington) | Obtenir une vue exacte de la tyrolienne de Mount Washington et vérifier la reprise saisonnière. |
| Corée du Sud | [Arii Hills Zipwire](https://amusement-parks.fun/fr/attraction/56b41750-e5a1-4721-b4bf-225aaf8c3409/arii-hills-zipwire-jeongseon) | Obtenir une vue de la Zipwire, distincte du Skywalk voisin. |
| Corée du Sud | [Skyline Luge Busan](https://amusement-parks.fun/fr/attraction/b7da9b3e-fd8c-4dca-9a91-0542e557afec/skyline-luge-busan) | Obtenir une vue des pistes ou des luges du site Busan. |
| Corée du Sud | [Skyline Luge Tongyeong](https://amusement-parks.fun/fr/attraction/c4a7a4ae-bbb1-486f-aa6a-b362498628af/skyline-luge-tongyeong) | Obtenir une vue des pistes ou des luges du site Tongyeong. |

Chaque ligne nationale reste dans le backlog avec cette preuve et sa prochaine action. **Le contrôle documentaire ne modifie aucune date `lastDataIntegrationAt`** : la nouvelle date sera celle du reçu `ImportPhoto` ou de la dernière modification effective du média. Les recherches Wikimedia Commons par nom et autour des sites Busan et Tongyeong n’ont pas fourni de vue exacte réutilisable dans cette passe ; elles ne prouvent pas l’absence d’autres photos sous licence ou autorisation.

## Rattrapage du 15 novembre, 04:28 UTC

Deux [photos officielles de Skyline Luge Busan](evidence/standalone-kr-skyline-luge-busan-photos-20261115/audit.json), avec crédit et licence KOGL Type 1, ont été importées et publiées. Les huit pages linguistiques, les deux fichiers et la miniature sociale répondent en 200. L’annonce Facebook de la fiche existait déjà et a été conservée. **À la suite de ce seul rattrapage**, sur les mêmes 232 fiches : **5 sans photo**, **27 avec une**, **200 avec au moins deux**. Le tableau ci-dessus reste la photographie datée de 03:43:59 UTC ; la ligne Busan est désormais résolue.

## Rattrapage du 15 novembre, 14:11 UTC

Trois [photos exactes de Skyline Luge Tongyeong](evidence/standalone-kr-skyline-luge-tongyeong-photos-20261115/audit.json) sont publiées : station basse, télésiège et piste avec la baie. Hantabi attribue chaque image à Kim Ji-ho / Korea Tourism Organization avec son identifiant et la licence KOGL Type 1. La source de licence est secondaire ; une fiche KTO directe reste à rechercher. Les huit pages publiques, les trois fichiers et leurs huit jeux de textes alternatifs, légendes distinctes et crédits répondent en 200. Le score éditorial est 87/100 sans blocage. L’annonce Facebook du 23 octobre reste publiée. **Après ce rattrapage**, sur les 232 fiches de la mesure initiale : **4 sans photo**, **27 avec une**, **201 avec au moins deux**. Les trois standalone publiées plus tard dans la journée ne sont pas comprises dans cette base datée de 03:43:59 UTC.
