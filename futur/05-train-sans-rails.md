# 05 — C07 / Le Train sans rails : un voyage collectif fiable

[Retour](README.md) · Quatrième livraison, après C03 vérifié en production.

## 1. Promesse et périmètre

Un train **virtuel** se déplace de parc en parc par des liens prouvés du catalogue. Chaque étape explique la connexion qui l'a rendue possible. La communauté choisit la prochaine destination parmi des candidats valides ; le résultat crée une raison concrète de revenir.

Il ne s'agit pas d'un transport réel, d'un itinéraire routier, d'une réservation ou d'une promesse d'ouverture des parcs. Les distances cartographiques sont à vol d'oiseau. L'illustration du train est un élément d'interface original, pas un visuel généré par IA.

V1 : un voyage mondial public, une étape active, deux à trois destinations candidates lorsque les données le permettent, vote authentifié, clôture quotidienne fiable, journal des étapes, carte accessible et supports sociaux. Pas de chat, d'économie virtuelle, de votes payants, de collecte des réactions Facebook comme suffrages, ni de plusieurs voyages concurrents.

## 2. Parcours visiteurs et administrateur

Le visiteur ouvre la page du voyage depuis la navigation ou une publication : parc actuel, lien de l'étape précédente, résumé du parcours, heure de clôture et candidats. Il peut lire les preuves sans compte.

Pour voter, il utilise le compte du site et ses conditions existantes d'activation/non-blocage. Aucun login Facebook ou TikTok requis. Le choix est conservé lors d'une redirection vers la connexion, puis confirmé explicitement après retour. Un compte possède un seul vote courant par étape et peut le modifier jusqu'à l'échéance ; la confirmation indique la date/heure de clôture.

Les nombres par candidat restent cachés jusqu'à la clôture pour éviter une course au candidat déjà en tête. Le total des votants peut être affiché, sans identité. Le compte de l'utilisateur et son choix ne figurent jamais dans l'Open Graph partagé.

Après clôture : destination gagnante, résultat agrégé, règle de départage éventuellement utilisée, nouvelle étape et archive précédente. L'utilisateur peut partager « Voici les prochaines destinations possibles » ou « Le voyage arrive ici », pas une injonction à générer des comptes.

Admin minimale : prévisualiser les candidats et leurs preuves, voir le prochain cutoff, mettre le voyage en pause, invalider une étape problématique, reprendre un job interrompu, préparer les supports. Toute intervention est auditée. **Interdiction de changer discrètement les candidats ou les règles d'un vote ouvert.** En cas de source retirée, annuler explicitement l'étape concernée et communiquer la raison sans exposer la donnée masquée.

## 3. Construction des candidats

Réutiliser les ponts constructeur validés de C01, sans reprendre ses contraintes de puzzle non pertinentes. Chaque candidat doit être relié au parc courant par un pont complet et public. Garder les preuves et le dataset de la proposition.

Exclure le parc courant, les parcs non découvrables et les destinations visitées récemment. Fenêtre anti-répétition initiale : 30 étapes, configurable et versionnée. Favoriser des candidats variés par pays et constructeur, mais ne jamais fabriquer une connexion pour satisfaire cette variété.

Choisir jusqu'à trois candidats selon un score **éditorial déterministe**, expliqué en interne : preuve suffisante, nouveauté dans le voyage, diversité des relations, qualité du contenu public. Le score de DoppelPark peut être utilisé plus tard comme information de découverte, mais ne remplace pas une preuve de connexion.

Replis dans cet ordre : trois candidats valides → deux candidats → une destination unique annoncée sans faux vote → pause s'il n'en existe aucune. Une réduction de la fenêtre anti-répétition doit suivre une règle publiée et versionnée, jamais une décision cachée au milieu du vote. Ne pas requalifier le choix automatique en choix de la communauté.

Préparer les candidats suivants au moment utile à partir d'une version cohérente. Un changement de catalogue n'altère pas silencieusement un scrutin ouvert ; une indisponibilité publique de la preuve déclenche la procédure d'invalidation.

## 4. Temps, votes et état métier

Échéance nominale : **09:00 Europe/Paris**, enregistrée comme instant UTC pour chaque étape. Afficher la date complète et le fuseau/localisation utilisateur, pas seulement un compte à rebours. Calculer l'échéance via le fuseau, jamais `now + 24h` aux changements d'heure.

États proposés : `Preparing`, `VotingOpen`, `Closing`, `Closed`, `Cancelled`, `Paused`. Une étape close est immuable pour les résultats ; une correction ajoute un événement d'audit/publication, elle ne réécrit pas discrètement le gagnant historique.

Une mutation de vote est acceptée seulement si étape `VotingOpen`, utilisateur autorisé, candidat dans la liste figée, limite de capacité non atteinte et instant serveur strictement antérieur à `closesAtUtc`. L'horloge du client ne compte pas. Un clic à la dernière seconde peut être refusé s'il arrive après la clôture ; le message doit être clair.

Gagnant : plus grand nombre de votes acceptés. Égalité : ordre déterministe fixé avant ouverture, par exemple hash de `journeyId/stepNumber/candidateId/rulesVersion`, et annoncé dans les règles. Aucun avantage au vote le plus rapide. Zéro vote : choix automatique selon l'ordre préétabli, publiquement marqué « sélection automatique, aucun vote reçu ».

## 5. Atomicité : exigence de conception, pas détail facultatif

Il faut empêcher un vote accepté après le comptage, une double clôture et un double départ. Une lecture des votes suivie d'une écriture séparée de l'étape, sans garantie transactionnelle ou barrière atomique, est interdite.

Au cadrage, vérifier la topologie Mongo réellement déployée. Deux options sont acceptables, à choisir et prouver par tests :

**Option A — transactions déjà supportées.** Votes séparés avec unicité `(stepId, userId)`, clôture et barrière d'écriture dans une transaction/stratégie cohérente. Décrire précisément les conflits entre un changement de vote et la clôture, le read concern, la reprise et les index ; la simple mention « utiliser une transaction » ne suffit pas.

**Option B — agrégat mono-document borné pour la V1.** Stocker dans l'étape ouverte les choix par identifiant pseudonyme de compte, les compteurs et la version. Vote et clôture modifient le même document par opération atomique conditionnelle ; la clôture fige les compteurs et l'issue dans cette opération. Le mapping de votants reste strictement interne. Une limite de votants et de taille BSON est fixée avant ouverture, testée et affichée si atteinte ; proposition initiale maximale 5 000 votants/étape, à confirmer avec taille mesurée. Ne jamais dépasser la capacité ni répartir improvisément les votes entre documents sans garantie de clôture.

Le pseudonyme par étape peut être dérivé via un HMAC serveur d'identifiant de compte et d'étape ; il n'est pas anonyme tant qu'un rapprochement reste possible. Prévoir export/suppression et rétention. Aucun IP fingerprint ou identité sociale collectée. Après la durée nécessaire, conserver uniquement les agrégats réellement anonymisés et les preuves de résultat, selon le catalogue de confidentialité du projet.

L'option B évite d'imposer un nouveau service ou une refonte Mongo pour démarrer. La capacité limitée doit être un vrai choix produit assumé, jamais une limite cachée. Si aucune option ne fournit les garanties demandées, le concept reste bloqué ; ne pas livrer un vote faussement fiable.

## 6. Transition et reprise après incident

Clôturer l'étape produit un résultat immuable et une intention `AdvanceJourney`/outbox dans le même agrégat ou transaction. L'ID de l'étape suivante est déterministe `(journeyId, stepNumber+1)`. Un worker crée cette étape par upsert unique, puis avance le pointeur du voyage par compare-and-set. Les deux opérations sont rejouables. Un crash entre les deux ne crée pas deux étapes ; une réparation reprend l'intention persistée.

Une lease avec fencing empêche un ancien worker de conclure après perte de sa réservation. Le front ne choisit pas « la dernière étape créée » au hasard : il utilise la référence canonique du voyage et un état de transition explicite si nécessaire.

La publication sociale consomme un événement métier stable seulement après disponibilité réelle de la page suivante. Clé de déduplication : réseau + compte + voyage + numéro d'étape + type d'événement + locale. Changer le template d'image ne republie pas automatiquement le même événement.

Après indisponibilité prolongée : fermer l'étape en retard avec les votes légalement acceptés, ne pas inventer plusieurs étapes pour rattraper le calendrier, et ouvrir une nouvelle fenêtre raisonnable. Règle initiale : première échéance 09:00 Europe/Paris située au moins 12 heures après l'ouverture effective. Annoncer la reprise ; supprimer les annonces périmées en attente. La prochaine clôture peut donc être différée plutôt que proposer un vote de quelques minutes.

Si une source ou un candidat devient non publiable avant clôture : suspendre/annuler le scrutin, ne pas transférer les votes vers un autre candidat, documenter la correction et proposer un nouveau scrutin distinct. Si le retrait intervient après clôture, rendre l'archive neutre/partiellement retirée selon les droits et conserver la trace interne ; ne pas falsifier le vote historique.

## 7. Modèles et API proposés

`Journey` : ID, état, currentStepId, version, rulesVersion, politique horaire. `JourneyStep` : numéro unique, dataset, parc courant, candidats et preuves, opens/closesAtUtc, état, version, résultat, mécanisme de départage, intentions de transition. `JourneyVote` uniquement si option A ; sinon état de votes interne à l'agrégat B. `JourneyEvent`/outbox réutilisée si déjà présente.

| Route indicative | Responsabilité |
| --- | --- |
| `GET /api/discovery/journey/current` | étape publique, candidats, deadline, transition et total autorisé |
| `GET /api/discovery/journeys/{id}/steps?cursor=...` | journal paginé, résultats clos, sources publiables |
| `GET .../steps/{stepId}` | détail immuable/public ; pas d'identités ni résultats avant échéance |
| `GET .../steps/{stepId}/my-vote` | authentifié, réponse privée non cacheable |
| `PUT .../steps/{stepId}/my-vote` | candidat, version/clé idempotence ; validation atomique |
| `DELETE .../steps/{stepId}/my-vote` | retrait avant échéance, atomique et audité selon politique de confidentialité |
| `POST .../steps/{stepId}/share-artifacts` | support public sans choix individuel par défaut |
| routes admin via architecture existante | preview, pause, annulation, reprise ; rôles et audit obligatoires |

Les lectures publiques et privées ne partagent jamais la même clé de cache. Aucun endpoint public ne déclenche la clôture sur simple GET. Réponses de vote privées, CSRF et rate limits existants ; 409/étape close retourne l'état courant sans fabriquer une réussite.

## 8. Écrans, SEO et partage

Carte du trajet avec ordre des étapes, légende « voyage virtuel » et alternative sous forme de journal. Tracé entre coordonnées connues uniquement ; une étape sans coordonnées reste au journal et ne reçoit pas un point inventé. Pas de geocoding payant ni de calcul routier.

La page courante affiche SSR parc/étape/candidats/deadline et les preuves. Les scores détaillés de vote ne sont pas dans TransferState avant clôture. Le cache est borné par l'échéance et invalidé lors de la transition. Les archives très pauvres ne sont pas ajoutées automatiquement au sitemap ; favoriser une page de voyage et des étapes riches.

Trois supports : annonce d'arrivée, invitation à voter et récapitulatif hebdomadaire. Par défaut, le plan éditorial choisit **une seule publication principale par étape**, pas une rafale arrivée+vote+relance sur chaque réseau. La sélection dépend du plafond global interconcepts du document 06.

Facebook/Instagram : carte actuelle et candidats, lien vers le vote sur le site. TikTok/Reels/Shorts : trajet animé schématique, révélation de la destination puis choix suivants ; aucun comptage des commentaires comme votes. LinkedIn : récapitulatif ponctuel centré sur les connexions industrielles ; pas nécessairement chaque jour. Les votes sociaux externes ne sont jamais fusionnés au scrutin officiel.

## 9. Lots et recette

| Lot | Livrable | Acceptation |
| --- | --- | --- |
| C07-A | Règles éditoriales, audit graphe et stratégie d'atomicité | topologie vérifiée, mécanisme de vote prouvé sur fixture |
| C07-B | Journey/Step, candidats et état métier | zéro/un/deux/trois candidats, pas de liens inventés |
| C07-C | Vote/auth, clôture, transition/outbox et reprise | concurrence, cutoff, fencing, idempotence |
| C07-D | Page, journal, carte, preuves et administration | parcours mobile et accessibilité complets |
| C07-E | Supports, calendrier social et indicateurs | aucun doublon ou post périmé, repli export |
| C07-F | Déploiement puis cycle réel observé | ouverture→vote→clôture→étape suivante validé avant VERIFIED |

Tests indispensables : deux votes concurrents du même compte, changement/retrait au cutoff, compte bloqué, CSRF, résultat non exposé, égalité, zéro vote, capacité atteinte, candidat retiré, fermeture DST, perte de lease, crash avant/après clôture et avant/après avance du pointeur, double worker, timeout social ambigu, retard de 48 h, pause admin, reprise sans étapes fictives, suppression de données personnelles et purge des sessions.

Une simulation en horloge contrôlée complète la recette, mais ne remplace pas l'observation d'au moins un vrai cycle de production. Tant que ce cycle n'est pas observé, l'état reste DEPLOYED, pas VERIFIED. Les métriques suivent vue→vote, retour au résultat, participation aux étapes suivantes et exploration des fiches, en séparant votes humains authentifiés, sélections automatiques et trafic robot.
