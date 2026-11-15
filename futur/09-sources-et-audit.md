# 09 — Sources, audit initial et points à revalider

[Retour](README.md)

## 1. Nature des preuves

Cette roadmap est une proposition de conception fondée sur une lecture ciblée du dépôt et de documentations officielles. Elle ne constitue pas un audit exhaustif de production, un test de permissions sociales, une mesure de performance ou une preuve d'originalité mondiale des concepts.

Base de lecture initiale : dépôt `Ced59/AmusementParkProject`, branche `master`, commit `439b49170b1cfff972f2f33e128f1849fc469a60`. Les noms et versions ci-dessous sont ceux des fichiers inspectés, pas des valeurs garanties pour un futur développement. Refaire l'audit sur `origin/master` au démarrage de chaque concept.

## 2. Fichiers du dépôt inspectés

| Référence | Ce qu'elle établit / pourquoi la relire |
| --- | --- |
| [`AGENTS.md`](../AGENTS.md) | Architecture, une classe par fichier, contraintes Git, responsive, huit langues, règles PR/review/déploiement et publication sociale |
| [`Park.cs`](../API/AmusementPark.Core/Domain/Parks/Park.cs) | IDs fondateur/exploitant, visibilité, statut, dates, pays et `IsPubliclyDiscoverable()` |
| [`ParkItem.cs`](../API/AmusementPark.Core/Domain/Parks/ParkItem.cs) | Relation parc/zone, catégorie/type, sous-type libre, visibilité et données de qualité |
| [`AttractionDetails.cs`](../API/AmusementPark.Core/Domain/Parks/AttractionDetails.cs) | Constructeur unique, modèle/statut libres, source, dates et caractéristiques nullables |
| [`package.json`](../FRONT/AmusementPark/package.json) | Angular 21.2.25, SSR, Leaflet, Matomo, qrcode et scripts de qualité au snapshot inspecté |
| [`production.yml`](../.github/workflows/production.yml) | Workflow CI/CD existant ; .NET 10 et Node 24 dans son environnement inspecté ; vérifier ses jobs effectifs au merge |
| [`facebook-page-publishing.md`](../docs/deploy/facebook-page-publishing.md) | Publication Page, permissions/configuration, OG, cibles, images, historique, webhooks et réconciliation |
| [`SocialPublicationService.cs`](../API/AmusementPark.Application/Features/SocialPublishing/Services/SocialPublicationService.cs) | Orchestration existante, ports/repository/publishers, retries et réconciliation à étendre plutôt que dupliquer |

L'audit n'a pas interrogé la base publique/privée pour chiffrer la couverture. Aucun des anciens volumes de parcs/attractions du brainstorming n'est repris comme un fait actuel. Aucune image n'a été déclarée réutilisable sans examen de sa licence. Aucun compte Instagram, TikTok, LinkedIn ou autre n'a été présumé connecté.

## 3. Références officielles de diffusion

Liens consultés pendant la préparation de cette version ; les conditions et routes peuvent évoluer. Avant d'implémenter un adaptateur, noter dans son dossier de cadrage la date de vérification, la version d'API retenue, le compte cible, les capacités réellement accordées et le repli.

| ID | Référence officielle | Décision de conception associée |
| --- | --- | --- |
| S01 | [W3C — Web Share API](https://www.w3.org/TR/web-share/) | Partage initié par l'utilisateur ; les cibles sont contrôlées par l'environnement, pas garanties par le site |
| S02 | [MDN — Web Share API](https://developer.mozilla.org/en-US/docs/Web/API/Web_Share_API) | Détection de capacité, contexte HTTPS, activation et fallback ; aucune publication sociale déduite d'un simple retour navigateur |
| S03 | [Meta — Page Feed](https://developers.facebook.com/docs/graph-api/reference/page/feed/) | Publication de Page avec permissions adaptées ; réutilisation de l'intégration existante |
| S04 | [Meta — Instagram Content Publishing](https://developers.facebook.com/documentation/instagram-platform/content-publishing) | Compte professionnel, variantes de login/permissions, médias accessibles et étapes container/publication |
| S05 | [TikTok — Content Sharing Guidelines](https://developers.tiktok.com/docs/en/content-sharing-guidelines) | Outil interne de publication sur ses propres comptes non accepté par Direct Post ; contrôle créateur et restrictions de branding |
| S06 | [TikTok — Direct Post, démarrage](https://developers.tiktok.com/docs/en/content-posting-api-get-started) | Audit et restrictions avant diffusion publique ; aucun automatisme public promis sans accès conforme |
| S07 | [LinkedIn — Posts API](https://learn.microsoft.com/en-us/linkedin/marketing/community-management/shares/posts-api) | Publication selon produit/API, scope et rôle de compte ; revalidation de la version obligatoire |
| S08 | [YouTube — videos.insert](https://developers.google.com/youtube/data/reference/rest/v3/videos/insert) | Upload OAuth et contraintes de l'API ; ne pas figer un ancien quota ou une ancienne règle d'audit |
| S09 | [Mastodon — statuses](https://docs.joinmastodon.org/methods/statuses/) | Publication sur une instance autorisée ; utiliser ses capacités et limites réelles |
| S10 | [Discord — webhooks](https://docs.discord.com/developers/resources/webhook) | Diffusion vers un canal autorisé, URL/token secret et contrat de webhook |
| S11 | [OpenAI — règles de review Codex](https://developers.openai.com/blog/custom-code-review-rules-for-codex) | Consignes ciblées dans AGENTS.md et review GitHub ; ce n'est pas une API LLM ajoutée au produit |

Les pages officielles Bluesky consultées ont redirigé vers `bsky.network` sans contenu exploitable dans la lecture disponible. En conséquence, aucune permission, limite ou méthode d'authentification Bluesky n'est affirmée comme vérifiée ici. Export/copie restent le repli ; documenter l'API avec une source officielle lisible avant toute implémentation.

Les documentations de plateformes peuvent présenter des incohérences entre sections ou versions, notamment sur les quotas. Cette roadmap choisit délibérément de ne pas coder de quota tiers chiffré dans ses contrats. Utiliser les réponses de capacité/rate-limit lorsque disponibles et la documentation de la version retenue.

## 4. Ce qui relève de choix produit, pas de faits externes

L'ordre C02→C01→C03→C07, les formats de travail 1200×630/1080×1350/1080×1920, les poids 0,60/0,40, seuils de couverture, distances, nombre de candidats, heure de clôture, plafond éditorial, budgets de latence et capacité initiale de vote sont des **propositions de cette roadmap**. Les tester et les versionner ; ne pas les attribuer aux plateformes ni à des données mesurées.

Les APIs, collections, interfaces et classes portant les noms Discovery/Parklinks/Butterfly/DoppelPark/Journey sont des contrats proposés. Seules les références de la section 2 sont établies comme existantes au snapshot inspecté. Les captures ou exemples numériques synthétiques ne doivent pas devenir du contenu public présenté comme réel.

## 5. Journal de décisions initial

| Décision | Raison | Réexamen possible |
| --- | --- | --- |
| Un dossier `futur/`, documents français | Demande explicite ; aucun dossier par langue | Organisation interne seulement, pas duplication linguistique |
| Documentation seule à ce stade | Demande de roadmap et merge, pas de réalisation simultanée | Nouvelle instruction d'implémentation |
| C02 en premier | Simplicité du calcul et validation précoce du partage | Propriétaire peut changer l'ordre avant démarrage |
| Pas de LLM / pas de nouveau coût | Contrainte impérative | Pas de dérogation implicite |
| Score DoppelPark explicable V1 | Champs structurés réellement disponibles, éviter les biais des champs libres | Nouveau profil versionné après audit |
| TikTok via exports en V1 | Restriction officielle des usages internes Direct Post | Produit conforme différent + audit explicite, pas contournement |
| Publication sociale désactivée par défaut | Préparer n'est pas autoriser l'envoi | Politique de compte expressément activée |
| Votes du Train uniquement sur le site | Identité, unicité et cutoff contrôlables | Aucun import de réactions sociales prévu |
| Préserver inconnus et visibilité actuelle | Fiabilité et respect du périmètre public | Jamais transformés silencieusement en certitudes |

## 6. Critère de mise à jour documentaire

Après chaque cadrage ou livraison : mettre à jour les écarts et le registre du document 08, sans réécrire les preuves anciennes. Si une plateforme change ses règles, mettre à jour sa capacité et son repli avant d'activer un nouveau flux. Une source désormais inaccessible doit être signalée ; ne pas la remplacer par une affirmation de mémoire.
