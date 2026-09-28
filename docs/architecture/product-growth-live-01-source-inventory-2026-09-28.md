# LIVE-01 — Inventaire juridique et technique des sources live

> Décision du 28 septembre 2026.
>
> Statut : **source autorisée pour un spike interne borné**.
>
> Source pilote : **ThemeParks.wiki, API REST v1, offre gratuite**.
>
> Parc pilote technique : **Phantasialand**. Ce choix ne crée aucun mapping de
> production ; `LIVE-03` devra encore le vérifier humainement.

## 1. Résultat métier

Le programme LIVE peut commencer son socle de provenance et son spike interne avec
ThemeParks.wiki. La source autorise explicitement un site Web, gratuit ou
commercial, à présenter ses données à ses propres visiteurs, sous réserve d'une
attribution visible et de l'absence de redistribution du flux.

Cette décision autorise `LIVE-02` à définir le modèle de provenance et `LIVE-B` à
préparer un spike non public. Elle **n'autorise pas encore** :

- un affichage public ;
- un endpoint réutilisable comme flux de temps d'attente ;
- un export brut ou un miroir ;
- une ingestion multi-parcs ;
- une alerte, un historique public ou une prévision ;
- un mapping automatique fondé sur le nom ;
- le scraping des applications ou sites des parcs.

Avant `LIVE-D`, le canal servant le frontend devra être revu pour démontrer qu'il
reste une surface du produit destinée aux visiteurs et ne constitue pas un
« re-API » accessible à des logiciels tiers.

## 2. Sources officielles examinées

Les pages suivantes ont été relues le 28 septembre 2026 à 15:47 UTC. Les empreintes
portent sur les réponses UTF-8 reçues ce jour ; elles permettent de détecter une
modification lors de la prochaine revue, sans prétendre remplacer le texte
contractuel publié par son propriétaire.

| Document | URL canonique | SHA-256 observé |
|---|---|---|
| Conditions d'utilisation | <https://www.themeparks.wiki/terms> | `9de79351b8dd76118213d4ab41e5d50063a329235f68da5a5a4c62261440916d` |
| Offres et quotas | <https://www.themeparks.wiki/pricing> | `b11570d57f5a95c8f2d1feabe32984c7929a901539093162f9572317bffd39e6` |
| Fiabilité | <https://www.themeparks.wiki/reliability> | `53a6d1d17c19533cc8acf11f7d1c263b4fa9171f9afb1702df9cd42831b2d2c0` |
| Couverture | <https://www.themeparks.wiki/coverage> | `04b4c8cfc68fe5e9e8eddddf11fb8301ac4d84cc7520e6cdd427885eb2d5a867` |
| Contrat OpenAPI v1 | <https://api.themeparks.wiki/docs/v1.json> | `9be6d30cb9e43ffcfb9bb225af873a2850bbaf87dd6a9f465225e6b7c3540502` |

Version OpenAPI observée : `1.14.0+c249aaa`.

Les conditions disposent d'un journal de changements. Leur entrée la plus récente
au moment de la décision est datée du 27 septembre 2026. Une nouvelle empreinte ou
une nouvelle entrée de ce journal impose une revue avant d'élargir le périmètre.

## 3. Inventaire comparé

| Candidate | Droits lisibles | Technique | Décision |
|---|---|---|---|
| ThemeParks.wiki REST v1 | Usage personnel, éducatif et commercial autorisé avec attribution ; historique propre et analyses dérivées permis ; redistribution interdite | Contrat OpenAPI stable, identifiants annoncés stables, ETag, horodatage, statuts et temps d'attente | **Retenue pour le spike interne** |
| Queue-Times Real Time API | La page API autorise l'accès gratuit avec attribution, mais ne précise pas suffisamment la conservation, les dérivés, le retrait et la redistribution pour le besoin complet | JSON simple, rafraîchi toutes les cinq minutes | Écartée comme source principale tant que ces points ne sont pas clarifiés par écrit |
| Flux directs d'opérateurs ou applications officielles | Aucun contrat public commun identifié pour notre usage | Formats et protections propres à chaque opérateur | Écartés : aucun scraping ni contournement |
| Contributions de visiteurs | Consentement et modération à concevoir ; ne prouve pas le statut officiel | Fraîcheur et fiabilité insuffisantes pour le socle | Hors périmètre de la première phase |

La page Queue-Times consultée est
<https://queue-times.com/pages/api>. Son contenu a produit l'empreinte
`d8ffa29c39b726d25b85a2de02a7b75d4ef68e3d6d01124ebd2aad11e1ecc28e`
le 28 septembre 2026 à 15:47 UTC.

## 4. Fiche contractuelle de la source retenue

| Champ `LIVE-A` | Décision enregistrée |
|---|---|
| Propriétaire | Jamie Holding, juridiquement James Holding, entrepreneur individuel au Royaume-Uni opérant sous le nom ThemeParks.wiki |
| Type | Agrégateur tiers doté d'une API et de conditions qui autorisent explicitement l'intégration dans un produit |
| Conditions | Conditions ThemeParks.wiki consultées à l'URL et à l'empreinte de la section 2 |
| Attribution | Texte visible **Powered by ThemeParks.wiki**, lien vers `https://themeparks.wiki` |
| Fréquence permise | Polling REST au plus toutes les cinq minutes pour notre intégration, même si le contrat technique peut annoncer des mises à jour plus fréquentes |
| Quota gratuit observé | 300 requêtes REST par minute ; 600 requêtes historiques par heure avec une clé gratuite |
| Stockage historique | Enregistrement de notre propre historique depuis le live et analyses dérivées permis ; pas de copie autonome ou miroir redistribuable |
| Redistribution | Interdite sur les offres libre-service ; pas d'API, de flux, de widget tiers ni d'export massif de données source |
| Usage commercial | Autorisé dans le produit, avec attribution sur l'offre gratuite |
| Territoire | Aucune limite territoriale spécifique publiée ; opérateur établi au Royaume-Uni |
| Durée | Permission limitée, non exclusive, non transférable et révocable ; pas de durée minimale garantie |
| Contact | `hello@themeparks.wiki` ; incidents publics sur `https://status.themeparks.wiki` |
| Fiabilité | Offre gratuite en best effort, sans SLA ; exactitude, exhaustivité et fraîcheur non garanties |
| Coût du pilote | 0 USD par mois sur l'offre gratuite, sous les limites publiées |
| Retrait | Kill switch source, arrêt du scheduler, expiration immédiate du latest, purge des caches et application de la politique de conservation |
| Données personnelles | Les réponses de parc ne nécessitent aucune donnée visiteur. Une éventuelle clé est un secret d'exploitation ; le compte fournisseur relève de sa politique de confidentialité |

Cette analyse est proportionnée à un pilote gratuit et réversible. Elle ne vaut pas
avis juridique général. Une offre payante, une redistribution, un changement de
modèle économique ou une exposition à des tiers nécessite une nouvelle validation.

## 5. Compatibilité technique observée

### 5.1 Endpoints utiles au pilote

```text
GET /v1/destinations
GET /v1/entity/{id}
GET /v1/entity/{id}/children
GET /v1/entity/{id}/live
GET /v1/entity/{id}/schedule
```

Les endpoints d'historique ne sont pas utilisés par le spike `LIVE-B`. `LIVE-12`
devra réexaminer la politique, la rétention et le plan fournisseur avant toute
collecte historique durable.

### 5.2 Données disponibles

Le contrat observé fournit notamment :

- un identifiant externe stable ;
- le nom et le type d'entité ;
- le rattachement au parc ;
- un statut opérationnel ;
- des files typées, dont la valeur d'attente peut être absente ;
- `lastUpdated` pour mesurer l'âge réel ;
- le fuseau du parc dans les métadonnées ;
- des en-têtes `Cache-Control` et `ETag` ;
- un `429` et `Retry-After` lorsque la capacité est dépassée.

Le contrat indique aussi que de nouvelles valeurs d'enum peuvent apparaître. Toute
valeur inconnue doit devenir `Unknown`, jamais `Open`.

### 5.3 Vérification ponctuelle du parc pilote

Le 28 septembre 2026, l'annuaire public exposait :

```text
destination : Phantasialand
destination id : 0257ff9f-c73c-4855-b5b4-774755c4d146
park id : abb67808-61e3-49ef-996c-1b97ed64fac6
timezone : Europe/Berlin
live entities observed : 51
```

Un exemple d'attraction comportait simultanément un statut, une file `STANDBY`, un
temps d'attente et un `lastUpdated`. Cette vérification prouve la compatibilité du
contrat, pas la qualité permanente ni le futur mapping vers les entités internes.

## 6. Budget du spike `LIVE-B`

Le spike reste volontairement minuscule :

| Budget | Limite initiale |
|---|---|
| Sources actives | 1 |
| Parcs actifs | 1, Phantasialand |
| Polling | 1 lecture live toutes les 5 minutes au maximum |
| Fenêtre | Heures utiles du parc, avec marge bornée ; aucun polling nocturne continu |
| Charge quotidienne indicative | 144 lectures live pour une fenêtre de 12 heures |
| Concurrence | 1 poll actif pour la source |
| Historique | Aucun ; latest seulement |
| Public | Aucun affichage ni endpoint public au stade du spike |
| Secret | Aucun secret dans le dépôt ; configuration externe si une clé est créée |
| Arrêt | Kill switch opérationnel obligatoire avant activation du scheduler |

Les limites fournisseur sont des plafonds, pas des objectifs. Le scheduler respecte
`ETag`, `If-None-Match`, `Retry-After`, un timeout, un backoff borné et le budget
distinct de LIVE. Une réponse inchangée ne doit pas provoquer une nouvelle écriture.

## 7. Attribution et livraison au visiteur

L'interface publique future devra afficher, à proximité de la donnée :

```text
Powered by ThemeParks.wiki
```

Le texte est un lien vers `https://themeparks.wiki`. Il ne doit être ni masqué, ni
réduit à une page juridique éloignée de la donnée.

Le backend public ne devra pas devenir une copie générique du fournisseur. Avant
`LIVE-D`, une revue dédiée validera au minimum :

- une réponse limitée au contexte d'une page du produit ;
- aucun endpoint de masse, export, flux ou historique brut ;
- aucun schéma calqué pour permettre la reconstruction du feed ;
- CORS limité, rate limiting et cache court ;
- une attribution toujours rendue avec la donnée ;
- l'absence de promesse d'affiliation avec le parc ou la source ;
- la conformité explicite du canal first-party avec les conditions alors en vigueur.

En cas de doute, l'affichage public reste fermé jusqu'à clarification écrite du
fournisseur ou contrat adapté.

## 8. Fraîcheur et probité

ThemeParks.wiki distingue la disponibilité de son service de la fraîcheur des
données amont. Amusement Parks Fun doit donc calculer la fraîcheur à partir de
`lastUpdated`, et non de l'heure de notre requête.

Règles déjà figées pour les jalons suivants :

- `null` signifie inconnu ;
- `0` signifie zéro explicitement reçu ;
- fermé et zéro ne sont pas synonymes ;
- une donnée vieillissante porte un avertissement ;
- une donnée expirée n'est plus présentée comme live ;
- l'heure source, l'heure de réception et la source restent distinctes ;
- aucune moyenne ou prédiction ne masque les lacunes ;
- le produit ne doit jamais présenter la donnée comme officielle ou garantie.

Les seuils `Fresh`, `Aging`, `Stale` et `Expired` seront définis dans `LIVE-02` et
versionnés. Ils ne sont pas déduits arbitrairement du quota fournisseur.

## 9. Conditions d'arrêt et réexamen

La source est suspendue immédiatement si l'un des événements suivants survient :

- conditions ou quotas incompatibles ;
- retrait de la permission ou de l'accès ;
- attribution impossible à maintenir ;
- mapping devenu ambigu ou réutilisation d'un identifiant ;
- données durablement périmées ou incohérentes ;
- taux d'erreur, CPU, mémoire ou stockage au-dessus du budget ;
- impossibilité de distinguer `0`, inconnu, fermé et expiré ;
- canal public assimilable à une redistribution ;
- incident de sécurité, secret exposé ou comportement de type SSRF ;
- coût ou exploitation disproportionnés pour le VPS.

Une revue est obligatoire :

- avant `LIVE-D` ;
- avant le premier stockage historique ;
- avant une offre payante ;
- après changement des conditions, du tarif, du contrat OpenAPI ou du propriétaire ;
- au minimum tous les trois mois pendant le développement actif.

## 10. Décision d'architecture pour la suite

`LIVE-02` peut maintenant créer des primitives de domaine indépendantes du
fournisseur : source, politique d'usage, provenance, fraîcheur et états. Aucune
classe Core ne portera le nom ThemeParks.wiki et aucune valeur fournisseur ne
deviendra directement un enum métier.

L'adaptateur fournisseur restera en Infrastructure. L'Application orchestrera les
cas d'usage via des ports. Le WebAPI et Angular afficheront uniquement des contrats
normalisés, avec source et âge obligatoires. Cette séparation permet de suspendre ou
remplacer la source sans réécrire le domaine ni conserver deux systèmes concurrents.

## 11. Gate `LIVE-A`

- [x] propriétaire et contact identifiés ;
- [x] conditions et empreintes conservées ;
- [x] usage commercial autorisé pour le produit ;
- [x] attribution exacte définie ;
- [x] polling maximal et quotas documentés ;
- [x] stockage historique permis mais reporté ;
- [x] redistribution et export brut interdits ;
- [x] absence de scraping ;
- [x] retrait immédiat conçu ;
- [x] fiabilité et absence de garantie explicites ;
- [x] charge du pilote bornée ;
- [x] source de repli non nécessaire pour le spike, car l'état sûr est indisponible ;
- [ ] canal d'affichage public revalidé — requis seulement avant `LIVE-D` ;
- [ ] compte et acceptation de la version des conditions — requis avant l'usage d'une clé.

Conclusion : **`LIVE-A` est franchie pour un spike interne latest-only sur un parc.**
Elle ne vaut pas autorisation de généralisation publique.
