# 06 — Diffusion multicanal sans LLM et sans nouvel abonnement

[Retour](README.md) · [Sources officielles et limites de vérification](09-sources-et-audit.md)

## 1. Séparer quatre capacités

1. **Préparer** un contenu : faits, légende, images, storyboard, fichier vidéo si le rendu est disponible. Automatisable localement et déterministe.
2. **Partager comme visiteur** : ouvrir la feuille de partage du téléphone, copier le lien, télécharger un support, ouvrir un dialogue du réseau. Action volontaire ; ce n'est pas une publication automatique au nom du visiteur.
3. **Publier comme Amusement-Parks.Fun** : envoyer vers un compte/Page autorisé via API officielle, avec secrets serveur, permissions, politique éditoriale et historique.
4. **Planifier/mesurer/réconcilier** : préparer les créneaux, respecter les limites, suivre le résultat réel et corriger un envoi incertain.

L'interface d'administration doit afficher ces capacités séparément : `ExportOnly`, `ReadyForManualPublish`, `ApiConfigured`, `AuthorizationRequired`, `Enabled`, `Blocked`, `OutcomeUnknown`. Ne pas afficher « automatisé » quand seul le fichier a été généré.

**Interdit partout : LLM, modèle local génératif, embeddings, synthèse vocale payante, traduction à l'exécution, abonnement de planification ou navigateur robotisé pour contourner une API.** L'intervention de Codex dans le développement/review n'est pas une fonction de génération en production.

## 2. Matrice des canaux

Les règles évoluent : revalider la documentation et les droits du compte au moment d'implémenter un connecteur. Les formats ci-dessous sont des sorties internes proposées, pas des maxima contractuels des plateformes.

| Canal | Depuis le site, pour un visiteur | Compte/Page de marque | Repli sans nouvel accès |
| --- | --- | --- | --- |
| Facebook | Lien avec OG, partage natif/dialogue disponible, téléchargement | Réutiliser `SocialPublishing` et Page API déjà intégrés ; autorisation explicite par politique | Lien + légende + image prête à poster sur la Page |
| Instagram | Partage de fichier si accepté par l'appareil, téléchargement ; ne pas promettre un bouton web universel de publication | API de publication pour compte professionnel éligible, login et permissions adaptés ; preview puis container/publish | JPEG vertical/carrousel + légende, publication dans l'application ou outils natifs disponibles |
| TikTok | Export vertical ou partage fichier pris en charge ; créateur garde le contrôle | **Pas de Direct Post promis pour un simple outil interne du site** : les guidelines excluent cet usage ; un vrai produit grand public demanderait une étude/audit séparés | MP4 ou images + légende éditable + checklist ; publication manuelle/native |
| LinkedIn | Partage d'URL ou copier lien/texte, visuel professionnel | Posts API selon accès de l'application et droits du membre/organisation ; adapter le scope au compte cible | Visuel/carrousel d'images et texte prêt, sans promettre accès organisation |
| YouTube Shorts | Télécharger la vidéo ; pas de diffusion au compte du visiteur sans autorisation | Upload API seulement avec OAuth, quotas et conditions réellement vérifiés ; optional | MP4, titre, description et sous-titres pour upload natif |
| Mastodon | Lien/texte/image ; choix de l'instance par l'utilisateur, pas d'URL arbitraire appelée par le serveur | API de l'instance autorisée, scopes, limites et idempotence applicables | Export + copier ; aucun hébergement d'instance nouvelle exigé |
| Bluesky | Copier lien/texte, intent seulement après vérification du contrat officiel | Adaptateur envisagé, conditions/auth/quota à vérifier ; pas requis pour la V1 | Carte + texte + lien |
| WhatsApp / Telegram / messages / email | Feuille native ou copier lien ; pas d'accès au carnet d'adresses | Pas d'envoi marketing automatisé en V1 | Lien et fichier téléchargé |
| Discord | Partage du lien et aperçu | Webhook d'un canal explicitement autorisé, URL traitée comme secret, mentions désactivées par défaut | Export / copier lien |
| RSS/Atom | S'abonner aux découvertes éditoriales publiques | Flux généré par le site, pas de compte tiers nécessaire | Consultation du flux/page d'archives |
| X / Pinterest / autres | Export universel, lien à copier | Étude ultérieure seulement si accès pertinent et coût nul confirmé | Aucun connecteur payant ni accès supposé |

Meta propose plusieurs parcours d'authentification Instagram ; ne pas imposer une Page Facebook à tous les parcours par habitude. Les permissions diffèrent selon le login. Les médias doivent être accessibles à la plateforme pendant l'import, mais aucune URL ne doit exposer un jeton ni un fichier privé non publié. Les limites officielles doivent être lues au runtime quand possible, sinon configurées/versionnées après vérification ; ne pas recopier un ancien quota dans le code.

Pour TikTok, une application non auditée a des restrictions de visibilité et les règles demandent un contrôle explicite du créateur. Les gabarits destinés à une éventuelle API conforme doivent exclure les filigranes promotionnels imposés. La V1 du site privilégie donc un export propre et une publication native, sans investissement dans un connecteur probablement inéligible. La génération du support reste automatique même lorsque l'envoi final est manuel.

## 3. Supports communs, identité et lisibilité

Chaque concept doit fournir le même contrat de supports, avec un contenu propre au concept :

| Sortie | Dimension de travail proposée | Usage |
| --- | --- | --- |
| `link-card` | 1200×630 JPEG/PNG | OG, Facebook, LinkedIn, messageries |
| `portrait-card` | 1080×1350 JPEG | Feed vertical et cartes de carrousel |
| `square-card` | 1080×1080 JPEG | Repli compatible et réseaux carrés |
| `vertical-story` | 1080×1920 JPEG | Story, export mobile et fond vidéo |
| `short-video` | 1080×1920 MP4, 12–20 s visés | Reels/TikTok/Shorts, après validation du moteur et des contraintes du réseau |
| `publication-pack` | archive bornée + manifest JSON + texte UTF-8 | Usage manuel sans abonnement ni recherche de fichiers |

Tous les formats utilisent la charte effectivement présente dans le site, pas un thème parallèle. Une carte doit être identifiable en petit : un message principal, deux noms au maximum dans l'accroche, un chiffre ou une question, une hiérarchie visuelle nette. Les réserves essentielles restent lisibles ; une note microscopique ne corrige pas un titre trompeur.

Prévoir variante sans logo/filigrane/QR promotionnel imposé lorsqu'un canal le demande, variante de marque lorsque permis, et variante sans photographie si droits insuffisants. Les dimensions et zones de sécurité des vidéos sont configurables et testées avec les interfaces des apps ; ne pas cacher le chiffre important derrière leur barre d'actions.

Ne pas inclure des fichiers de polices dans les exports utilisateur. Employer les ressources/licences déjà autorisées pour le rendu, vérifier le droit d'incorporation en image/vidéo, et conserver les notices nécessaires côté projet.

## 4. Gabarits factuels par concept

| Concept | Accroche / contenu | CTA | Ce qui ne doit jamais sortir |
| --- | --- | --- | --- |
| C01 | Départ/arrivée, édition, résultat en ponts/indices | Jouer au même défi | Solution avant clôture, identifiant personnel, faux meilleur score mondial |
| C02 | Constructeur(s), retraits certains, périmètre, date et couverture | Tester la simulation | « Le parc fermerait », total sans dénominateur, statut actuel déduit d'une date absente |
| C03 | Deux parcs, indice de composition, raisons et différence | Comparer les données | Probabilité de satisfaction, causalité historique, certitude malgré faible couverture |
| C07 | Étape actuelle, candidats ou résultat, deadline, voyage virtuel | Voter sur le site / découvrir l'étape | Choix individuel, résultat avant clôture, vote Facebook compté comme suffrage |

Les textes sont produits depuis des clés de traduction et faits structurés, avec pluriels, accords prévus, échappement et longueurs contrôlées. Les gabarits sont relus dans les huit langues lors du développement. Ils ne remplacent pas les descriptions éditoriales du catalogue et ne génèrent pas de faux récits.

Le site permet huit locales ; cela n'autorise pas huit posts simultanés sur le même compte. Par défaut, conserver le style bilingue FR/EN existant sur la Page Facebook, avec une seule publication ; choisir une langue par support lorsque l'espace est limité. Les autres locales restent disponibles à l'export et aux liens entrants. La politique de compte précise langue, fréquence et audience.

Les hashtags proviennent d'une liste éditoriale bornée par réseau et concept. Pas de hashtags « tendance » inventés ni de génération de noms d'événements absents des données. Une légende peut être modifiée manuellement, mais un changement de chiffre exige un recalcul ou une nouvelle validation, pas une divergence visuel/texte.

## 5. Moteur de rendu déterministe

Réutiliser d'abord la génération d'images sociales existante ; inspecter ses librairies/licences avant d'en ajouter. Le moteur reçoit un view model validé, un format, une locale et une version de template. Il ne reçoit pas d'URL à capturer, d'HTML utilisateur, de commande shell ni d'expression de dessin libre.

Option privilégiée : SVG/HTML contrôlé puis rasterisation avec les outils déjà disponibles, ou bibliothèque serveur compatible avec l'environnement existant. Ne pas imposer un navigateur headless par requête. Les vidéos peuvent être composées de frames et transitions déterministes via un moteur local comme FFmpeg **si sa disponibilité, sa licence, ses codecs et sa charge sont validés** ; ce n'est pas une dépendance supposée installée.

Le rendu vidéo est hors requête HTTP, un seul job lourd à la fois, avec bornes durée/résolution/taille et timeout. Pas de téléchargement de clips externes, de musique commerciale ajoutée automatiquement, de voix synthétique ou de génération IA. Un export silencieux sous-titré est utilisable ; le créateur choisit éventuellement une musique autorisée dans l'application de destination.

Si le budget vidéo n'est pas démontré au cadrage, livrer images, storyboard numéroté et légende comme repli explicite. Documenter l'écart de capacité avant développement et dans la recette ; ne pas déclarer un MP4 livré alors que seul un storyboard existe. Cette limitation ne doit pas bloquer les images et le partage de liens.

Cache : hash de view model canonique + templateVersion + format + locale + médias/révisions. Un changement de modèle de rendu crée une nouvelle URL média ; le lien de page reste stable. Concurrence : un artefact déjà en cours n'est pas recalculé par dix visiteurs. Stockage et purge distinguent supports éditoriaux conservés et exports personnels temporaires.

## 6. Droits des médias et sécurité des rendus

La publication d'une photo sur le site ne prouve pas qu'on peut la republier transformée sur chaque réseau. Pour tout média : source, auteur, licence/autorisation, attribution, usage commercial et transformations permis, périmètre de redistribution. Un droit inconnu déclenche le visuel typographique, pas une supposition favorable.

Respecter les obligations d'attribution dans l'export et la légende lorsque nécessaires. Un logo constructeur/parc n'est pas automatiquement libre ; ne pas impliquer un partenariat. Ne pas utiliser des images de victimes ou d'accidents pour rendre un jeu plus accrocheur.

Pour les cartes : pas de capture massive de tuiles Leaflet tierces. Utiliser les coordonnées pour une carte schématique interne ou un fond déjà autorisé, avec sa licence. Les noms et preuves sont recontrôlés juste avant publication.

Les moteurs ne lisent que des médias de stockage autorisés. Limiter dimensions décodées, taille de fichier, MIME, temps CPU et nombre de frames. Échapper les noms avec `&`, guillemets et caractères RTL ; empêcher SVG/HTML actif, path traversal, SSRF, injection shell et decompression bombs. Tester noms très longs et emojis, sans couper des caractères composés.

## 7. Partage depuis le navigateur

Ordre de repli : `navigator.share` si disponible et autorisé ; `navigator.canShare({files})` pour les fichiers ; lien/dialogue spécifique seulement s'il est réellement supporté ; copier le lien ; télécharger le support et copier la légende. Aucun SDK social lourd n'est chargé pour afficher le bouton.

La feuille native doit être ouverte à la suite d'une activation utilisateur. Préparer l'artefact **avant** le clic final lorsque son calcul est long, puis montrer « Partager le fichier prêt » ; attendre un rendu réseau long dans le clic peut perdre l'activation transitoire. Ne jamais appeler l'API navigateur côté SSR.

La cible exacte dépend du navigateur et des apps installées. L'interface ne promet pas « publication dans une Story Instagram » si elle ne peut qu'ouvrir le partage système. Une annulation est neutre. Une promesse résolue par l'API signifie partage remis au système, pas preuve qu'un post social a été publié ; l'analytique distingue ces événements.

URL de partage : origine publique autorisée, langue choisie, identité de scénario/défi/paire/étape. Paramètres de campagne non personnels, whitelistés ; canonical sans tracking. Pas de jeton anonyme, email, ID utilisateur, choix de vote ou open redirect. Un QR éventuel encode seulement cette URL et n'est pas imposé aux formats qui l'interdisent.

## 8. SSR et aperçu Facebook réellement attrayant

Titre, description et image uniques à la ressource, rendus dans le HTML initial. `og:type`, `og:url`, `og:locale`, dimensions/type d'image et métadonnées compatibles avec les conventions du dépôt. Image accessible en HTTPS sans cookie ni URL temporaire trop courte ; la sécurité des retraits prime sur le cache.

Conserver la mécanique existante de canonical, de résolution des cibles, de `facebook-image` et des variantes `social-preview-v2` pour les publications actuelles. Ajouter un type de cible de découverte explicite ; **ne pas faire passer une carte synthétique pour une image appartenant à un parc**. Les règles actuelles de validation d'ImageId restent strictes pour les cibles Park/ParkItem.

Les liens de marque vers la fonctionnalité utilisent l'origine du site. La Page publie `message` + lien canonique selon l'adaptateur existant. L'aperçu choisi est contrôlé par le contenu SSR autorisé, pas par un paramètre arbitraire permettant de récupérer une image privée.

Recette : lecture du HTML brut avec user-agent normal et social, vérification d'une seule image OG pertinente, requête HTTP de l'image, locale, code 200/404/410 approprié, absence de spoiler, absence d'URL de secret, compatibilité avec les caches du SSR. Le débogueur officiel peut être utilisé manuellement ; ne pas promettre une purge instantanée du cache Facebook.

## 9. Chaîne de publication de marque

Étendre le mécanisme existant, sans changer son comportement Park/ParkItem :

`événement métier stable → candidat éditorial → faits et droits vérifiés → rendu → brouillon → validation/politique autorisée → planification → envoi → réconciliation → résultat archivé`.

Un `PublicationEnvelope` logique contient concept/eventId, cible canonique, réseau/compte/locale, faits et dataset, assets et licences, templateVersion, notBeforeUtc, expiresAtUtc, policyVersion, approbation et statut. Les tokens sont des secrets de configuration référencés, jamais une propriété sérialisée du brouillon.

Déduplication **logique** : `(network, accountId, concept, eventId, publicationKind, locale)` unique. Le hash de rendu identifie une rendition, pas un nouvel événement à publier. Changer le format ou relancer un job ne produit pas une seconde annonce. Un repost volontaire est une action explicite, auditée, hors retry.

États proposés à mapper sur l'existant : `Draft`, `AwaitingApproval`, `Scheduled`, `Sending`, `Published`, `RetryableFailure`, `OutcomeUnknown`, `Blocked`, `Cancelled`, `Expired`. Ne pas casser les anciens statuts, contrats et interfaces d'administration ; faire une extension compatible avec migration testée.

Après réponse 429/limite de quota : respecter Retry-After et différer. Après erreur d'auth : bloquer le compte et demander renouvellement, sans boucle de retry. Après timeout ambigu : conserver l'ID distant si reçu, rechercher/réconcilier seulement ce que l'API permet de prouver ; si impossible, demander vérification manuelle. Ne jamais promettre un exactly-once universel sur des APIs externes.

Les webhooks vérifient signature et rejouabilité ; ils ne peuvent pas importer arbitrairement les posts d'un autre compte. Les liens externes et erreurs ne révèlent pas de token. Une publication ratée ne rollback jamais une étape du Train ni un résultat de jeu.

## 10. Autorisation, calendrier et automatisme progressif

Tout nouveau couple compte/réseau/concept commence en **préparation seule**. Le propriétaire doit pouvoir autoriser une politique précise : comptes cibles, types de posts, langues, créneaux, plafond, niveau de validation, date d'effet, arrêt d'urgence. Le merge du code ne l'active pas.

Trois niveaux :

- N0 : génération et export automatiques, envoi manuel ; disponible sans credential.
- N1 : brouillon préparé et planifié, clic administrateur de validation avant l'envoi officiel.
- N2 : envoi autonome des seuls événements couverts par une politique préautorisée, après tests réussis et publication pilote expressément autorisée.

Plafond éditorial initial proposé : **un post de feed par jour et par compte, tous concepts confondus**, plus éventuellement des formats secondaires explicitement autorisés. Pas huit posts pour huit langues, pas un post par réussite visiteur ou chaque paire DoppelPark. Ce plafond produit est distinct des quotas techniques des plateformes et doit être configurable.

La disponibilité quotidienne de PARKLINKS et du Train sur le site ne signifie pas deux posts quotidiens automatiques sur chaque réseau. Un sélecteur éditorial déterministe arbitre : actualité de l'événement, nouveauté, qualité du support, réserve de contenus, absence de doublon, fréquence récente du concept. Proposition hebdomadaire quand les quatre concepts sont actifs : deux défis PARKLINKS, un scénario C02, une paire C03, trois étapes/récaps C07 ; ajuster selon les mesures, pas selon une prétendue recette virale.

Les drafts ont une péremption : un appel à voter après cutoff est expiré et remplacé seulement par un nouvel événement de résultat validé. Une correction importante de source bloque les contenus concernés. Après une panne prolongée, ne pas vider toute la file en rafale.

## 11. Administration et exploitation

Étendre l'écran des publications existant avec filtres concept/réseau/état, calendrier, aperçu des supports, faits/limites, droits, locale, cible, expiration, logs corrélés, bouton exporter, valider, annuler et réconcilier. Afficher la différence entre « prêt », « envoyé » et « confirmé publié ».

Un écran de capacités expose la configuration manquante sans afficher de secret. Arrêt d'urgence par réseau et global ; stopper les nouveaux envois et les jobs réservés avant leur appel distant lorsque possible. Conserver les historiques et IDs externes. Aucun retry automatique si issue inconnue.

## 12. Tests et mesure de diffusion

Tests de contrats avec publishers factices ; zéro réseau réel dans les tests CI. Réponses succès, quota, auth expirée, média refusé, timeout avant/après acceptation, webhook doublé, post supprimé depuis le réseau, reprise après crash. Vérifier rétrocompatibilité des publications de parcs existantes.

Tests de rendu : accents huit langues, chiffres/pluriels, très longs noms, photographie manquante ou révoquée, élément caché, transparence, recadrage et marges, no-spoiler, vidéo sans audio, taille/temps mémoire. Contrôle visuel humain sur petit écran en plus des snapshots.

Événements mesurés via l'analytique existante et le consentement applicable : share_opened, link_copied, asset_downloaded, native_share_returned, outbound_social_dialog_opened, landing_from_campaign, concept_started/completed. Ne pas inventer `facebook_post_published` depuis un bouton de partage visiteur. Les statistiques de marque distinguent préparation, validation, envoi confirmé, clics entrants et retours ; les API d'insights restent facultatives si les permissions manquent.
