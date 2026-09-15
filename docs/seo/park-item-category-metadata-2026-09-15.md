# Métadonnées des lieux selon leur catégorie

Le titre anglais des fiches appelait tous les lieux « Attraction guide », y compris les services. Le JSON-LD employait également `TouristAttraction` pour tous les parkItems hors parcs conceptuels. La catégorie déjà reçue par le contrat public était affichée correctement mais n’était pas transmise au générateur SEO.

Le modèle de vue conserve désormais `ParkItemCategory` sans déduire la catégorie d’un nom ou d’un libellé traduit. Le titre anglais devient « Guide to… ». Un helper de présentation choisit le type JSON-LD depuis cette catégorie :

| Catégorie | Type |
|---|---|
| Attraction | `TouristAttraction` |
| Restaurant, y compris les snacks | `FoodEstablishment` |
| Hotel | `LodgingBusiness` |
| Shop | `Store` |
| Service | `Place` |
| Animal, Show, Transport, Other, valeur absente ou inconnue | `Thing` |

Ces choix conservateurs suivent les définitions de [TouristAttraction](https://schema.org/TouristAttraction), [FoodEstablishment](https://schema.org/FoodEstablishment), [LodgingBusiness](https://schema.org/LodgingBusiness), [Store](https://schema.org/Store), [Place](https://schema.org/Place) et [Thing](https://schema.org/Thing). Les catégories ambiguës n’établissent pas à elles seules un événement, une entreprise ou un lieu fixe.

Les statuts conceptuels du parc parent (`Planned`, `UnderConstruction`, `Cancelled`) restent prioritaires et produisent `Thing`. Ce type générique contient seulement le nom, l’URL et la description lorsqu’elle existe : les propriétés de lieu ou de produit ne lui sont plus attribuées. Le fil d’Ariane JSON-LD conserve le contexte du parc.

La propriété [`manufacturer`](https://schema.org/manufacturer), documentée pour `Product`, n’est plus attribuée au JSON-LD de ces lieux. Une attraction ne devient pas un produit pour conserver ce champ. Le nom du constructeur et ses informations restent disponibles dans les données et la fiche visible.

La correction utilise les données déjà chargées, sans requête API supplémentaire, nouveau calcul sur le corpus ou dépendance. Le contenu éditorial, les valeurs techniques, les routes, la visibilité, les canonical, les alternates, les règles d’indexabilité et les sitemaps XML ne changent pas. Cette correction sémantique ne démontre ni ne résout à elle seule un refus d’indexation ou une attente de récupération de sitemap.

Les tests couvrent le mapping de catégorie à travers un changement de langue sans relecture API, les catégories explicites et inconnues, les statuts conceptuels, les propriétés génériques de `Thing`, l’absence de `manufacturer` sur les entités de lieu, les huit locales et la conservation des métadonnées et du fil d’Ariane.
