# 01 — Socle technique, qualité et réutilisation

[Retour](README.md) · [Sources de l'audit](09-sources-et-audit.md)

## 1. Point de départ constaté

Audit documentaire/code sur `master` au SHA `439b49170b1cfff972f2f33e128f1849fc469a60`. Ce n'est ni un export de production, ni une mesure de couverture des données.

- Backend réparti entre Core, Application, Infrastructure et WebAPI ; règles métier pures dans Core, orchestration via handlers/services/ports.
- Front dans `FRONT/AmusementPark`, Angular 21.2.25 dans le package inspecté, SSR, Leaflet, ngx-translate, Matomo et `qrcode` déjà référencés. La CI inspectée cible .NET 10 et Node 24 ; ne pas figer d'anciennes versions mémorisées.
- `Park` contient notamment `CountryCode`, `Type`, `Status`, `FounderId`, `OperatorId`, `IsVisible` et `IsPubliclyDiscoverable()`.
- `ParkItem` contient `ParkId`, `ZoneId`, `Category`, `Type`, **`Subtype` libre**, `AttractionDetails`, `IsVisible`, le statut de revue et les informations de qualité.
- `AttractionDetails` expose un **seul `ManufacturerId`**, un `Model` libre, un `Status` libre, des dates optionnelles, des mesures et booléens nullables. Ne pas inventer une association multiconstructeurs déjà disponible, une ontologie des modèles ou un statut normalisé universel.
- La publication Facebook est déjà encapsulée dans `Features/SocialPublishing`, avec service, ports, publisher, historique et réconciliation. Étendre ce mécanisme ; ne pas créer un deuxième client Meta indépendant.

## 2. Audit de données exigé au début de chaque concept

Produire un rapport interne, sans dump public de données sensibles : nombres de parcs/attractions publics éligibles, couverture des constructeurs résolus, modèles/types exploitables, statuts normalisables, dates, coordonnées, sources, images réutilisables et langues.

Calculer la couverture avec un numérateur et un dénominateur explicites, par pays/type de parc et globalement. Distinguer « aucun élément connu », « absence explicitement documentée » et « inventaire incomplet ». L'absence d'enfants en base ne prouve pas qu'un parc ne possède aucune attraction.

Pour chaque champ : emplacement exact dans le modèle, port d'accès existant, politique de publication, valeur inconnue, règle de normalisation, risque de biais, caractère obligatoire/optionnel. S'il manque une donnée essentielle, réduire le périmètre ou ouvrir une dette documentée ; ne pas lancer un scraping massif ou contourner `PARK_DATA_EDITOR`.

Les jeux de tests sont synthétiques et n'exigent pas de secrets de production. Un audit par accès applicatif autorisé ne change jamais la visibilité ni les données sources.

## 3. Projection de découverte

Nom logique proposé : `DiscoveryDataset`. Il s'agit d'une projection reconstruisible de faits déjà publiables, pas d'une seconde vérité métier du catalogue.

Enveloppe minimale : `datasetId`, `schemaVersion`, `algorithmVersion`, `policyVersion`, `createdAtUtc`, `sourceWatermark`, `lifecycleScope`, `coverageSummary`, `state` (`Building`, `Ready`, `Retired`, `Invalidated`). Les relations portent les identifiants sources, le type de relation, les IDs de preuves et leurs révisions, éventuellement un intervalle de validité démontré.

Le `sourceWatermark` doit représenter une vue cohérente. Ne pas supposer que plusieurs lectures paginées forment automatiquement un snapshot Mongo. Au cadrage, vérifier la topologie Mongo et les capacités existantes : snapshot transactionnel si déjà supporté et raisonnable ; sinon capture des révisions, lecture bornée puis contrôle de stabilité et rejet/reprise si des sources ont changé. Ne pas imposer une migration de topologie juste pour cette roadmap.

Construction : lire les projections via ports, normaliser, valider, écrire dans une nouvelle version, vérifier cohérence et couverture, puis basculer atomiquement un pointeur `CurrentReadyDataset`. Ne jamais exposer un dataset partiellement construit. Un échec garde l'ancienne version sûre et produit une alerte interne.

Stocker les partitions séparément : ne pas mettre tout le graphe ou toutes les empreintes dans un document BSON géant. Les seuils de taille doivent rester très en dessous de la limite Mongo. Les jobs parcourent des pages et respectent les annulations ; les résultats lourds peuvent être des artefacts de stockage existant plutôt que des réponses HTTP massives.

**Implémentation progressive :** C02 crée uniquement sa projection nécessaire. C01 enrichit avec les relations du graphe. C03 ajoute les empreintes. C07 réutilise ces faits sans importer un moteur de recommandation supplémentaire.

## 4. Publication et retrait des sources

Le filtre doit composer les règles publiques réelles du parc, du parkItem, du référentiel et du média. `IsVisible=true` seul ne remplace pas `IsPubliclyDiscoverable()` ni la politique d'accès d'une ressource. Un parc visible ne rend pas tous ses éléments visibles. Une fermeture historique n'est pas une autorisation de publier un enregistrement masqué.

Un snapshot garantit la reproductibilité du calcul, **pas le droit perpétuel de republier une source**. Le retrait/masquage d'une entité ou d'un média invalide les résultats dépendants, leurs versions SSR, images sociales, exports et publications encore en attente. Vérifier la sécurité de visibilité au moment de servir et de publier, avec cache d'autorisation borné et mécanisme d'invalidation explicite ; en cas d'incertitude, ne pas servir l'artefact concerné.

Conserver en interne la preuve technique d'un résultat retiré, mais retourner un état public neutre sans nom/image sensible. Une ancienne URL de partage ne doit pas ressusciter une fiche cachée. Les posts externes déjà publiés nécessitent une action de correction/retrait, à tracer ; leur effacement distant ne peut être garanti.

## 5. Normalisation et preuve

Les IDs priment sur les noms. Ne jamais fusionner deux constructeurs parce que leurs noms se ressemblent. Utiliser une table d'alias validée uniquement pour les libellés libres ; versionner cette table. Un modèle libre identique chez deux constructeurs n'est pas automatiquement la même famille de machines.

Les statuts sont classés dans une projection explicite : `KnownOperating`, `KnownClosed`, `Announced`, `Unknown`, avec la source du mapping. Un `ClosingDate=null`, un statut par défaut ou l'absence de mention de fermeture ne suffisent pas à établir l'ouverture actuelle. Le mode « historique » décrit des associations connues, pas nécessairement simultanées.

Pas d'inférence de confort, accessibilité, intensité, sécurité ou qualité depuis la hauteur, le constructeur ou un score de similarité. Les mesures sont converties avec règles testées, et une valeur absente reste `null`, jamais `0`.

Les faits calculés gardent une liste de contributions : IDs, valeur utilisée, normalisation appliquée, cohorte et date de référence. Les textes publics d'explication sont courts et compréhensibles ; les détails internes d'audit ne sont pas injectés dans les descriptions du catalogue.

## 6. Architecture cible

| Couche | Responsabilités proposées | Interdits |
| --- | --- | --- |
| Core | ensembles de simulation, graphe admissible, score/éligibilité, règles de vote/clôture, value objects | Mongo, HTTP, rendu, secrets, horloge système cachée |
| Application | queries/commands, orchestration des projections, validation, ports de stockage/horloge/rendu/publication | calcul métier dupliqué dans les handlers, client Meta concret |
| Infrastructure | repositories/index Mongo, stockage MinIO, jobs/leases, rendu déterministe, adaptateurs sociaux | décider les gagnants ou la similarité dans un repository |
| WebAPI | contrats HTTP, auth, limites, mapping, headers | logique de calcul dans les controllers |
| Front | routes lazy, composants, facades injectant des ports, présentation des preuves, partage | calcul autoritaire de score ou vote, secret fournisseur |

Emplacements indicatifs : `Core/Domain/Discovery`, `Application/Features/Discovery` et sous-features nommées, adaptateurs sous Infrastructure selon l'organisation réelle. Une classe C#/TypeScript par fichier, y compris fixtures et tests. Ne pas multiplier les façades vides ou les wrappers sans rôle.

Interfaces proposées seulement à leur premier usage : `IDiscoveryDatasetReader`, `IDiscoveryProjectionStore`, `IDiscoveryClock`, `IShareArtifactRenderer`. Réutiliser `ISocialPublisher`/le service existant plutôt qu'un port concurrent. Les noms exacts sont confirmés au cadrage.

## 7. Jobs fiables et bornés

Pas de dépendance à une page ouverte ni à un timer Angular pour produire un défi ou clôturer un vote. Employer les mécanismes de jobs déjà présents ; à défaut, un worker .NET avec lease Mongo suffit si ses garanties sont testées. Pas de nouveau broker.

Chaque travail possède une clé déterministe, un état, un nombre de tentatives, une échéance, un `leaseUntilUtc` et un token de fencing. Une acquisition compare-and-set empêche deux workers de publier le même résultat ; l'écriture finale vérifie encore le fencing token. Un ancien worker dont la lease a expiré ne peut pas écraser le nouveau.

La génération de l'artefact et l'envoi social sont des travaux différents. Une erreur Meta n'annule jamais un score, un vote ou une étape. Après un timeout d'envoi dont l'issue est inconnue : état `OutcomeUnknown`, réconciliation, jamais retry aveugle. Voir document 06.

## 8. Contrats de partage

`ShareArtifact` proposé : ID opaque, concept, identifiant métier public, snapshot/dataset/algorithme, locale, templateVersion, audience, mode spoiler, URLs canoniques autorisées, références des médias et droits, statut, dates, hash du rendu, propriétaire optionnel.

Le client ne fournit jamais des statistiques libres, un HTML arbitraire, une URL distante à capturer ou un chemin de fichier. Il fournit des IDs/options bornés ; le serveur recalcule/relit le résultat autoritaire. Les artefacts visiteurs sont créés à la demande, avec quotas et expiration documentée ; les supports éditoriaux sont limités aux sujets sélectionnés.

Les URLs stables utilisent l'identité métier de la page ; la variante média inclut le hash et la locale. Ne pas faire varier aléatoirement `og:image` à chaque appel. Un JPEG/PNG rendu doit être lisible sans cookie par les crawlers autorisés, tout en respectant le retrait des sources.

## 9. Front, SSR, accessibilité et langues

Créer une entrée de navigation découverte légère, pas quatre bundles dans la home. Chaque concept est lazy ; carte et export riche sont chargés à la demande. Détruire proprement Leaflet lors de la navigation ; aucun accès à `window`, `navigator` ou au canvas côté SSR.

Le HTML initial contient titre, description, faits utiles, liens de preuves et métadonnées de partage. Un robot social n'attend pas le JavaScript. Respecter les routes et huit langues publiques effectives du dépôt (`fr`, `en`, `es`, `de`, `it`, `nl`, `pt`, `pl`, à reconfirmer dans la configuration lors du développement). Tutoiement en français, pluriels, nombres et dates localisés ; les IDs de défis et les résultats ne dépendent pas de la langue.

Les résultats privés/personnels et combinaisons arbitraires ne sont pas indexés ; les pages éditoriales utiles le sont sélectivement. Pas de millions de pages de paires/scénarios dans le sitemap. Canonical et hreflang n'annoncent que des pages réellement servies. Ajouter breadcrumbs visibles et JSON-LD contextuel.

Alternative textuelle complète aux cartes/graphes, navigation clavier, focus, messages live mesurés, contrastes et reduced-motion. Vérifier 320/360/390 px, tablette et desktop, y compris noms longs et polonais/allemand. Aucun débordement horizontal global.

## 10. Sécurité, confidentialité et coût

Les endpoints publics de lecture ne divulguent ni sources internes, ni notes administratives, ni jetons. Les mutations anonymes explicitement permises sont limitées et isolées ; votes de C07 authentifiés. Réutiliser auth/cookies/CSRF/rate limiting et l'analytique consentie du dépôt. Ajouter les nouvelles données personnelles au catalogue de confidentialité existant avec rétention, export et suppression.

Aucune collecte IP durable pour fabriquer une identité de joueur, aucun fingerprinting, aucune connexion sociale nécessaire pour partager un lien. Les notifications/email éventuels sont hors V1 sauf consentement et budget existants vérifiés.

Budgets initiaux à mesurer : pas de calcul complet dans une requête publique ; réponse de synthèse compressée visée < 100 Ko hors médias ; ≤ 20 résultats par page ; un job CPU lourd à la fois ; charge bornée et interrompable ; aucune nouvelle dépendance lourde dans le bundle initial. Les seuils détaillés et la procédure de réduction de charge figurent dans le document 07.
