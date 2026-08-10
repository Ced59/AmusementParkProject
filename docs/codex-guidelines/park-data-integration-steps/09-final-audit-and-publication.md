# Étape 9 — Audit final et préparation publication

Objectif : vérifier que l’intégration complète est cohérente, fiable, localisée et publiable.

## Export requis

Immédiatement avant de commencer cet audit, obtenir un export complet frais après toutes les étapes appliquées. Dans les deux modes, il s’agit du seul export complet obligatoire du parcours de complétion 0 à 9. Une autorisation de publication donnée plus tard ouvre le contrôle distinct décrit à la fin de cette étape.

## Audit de pertinence

Vérifier :

- le parc est bien dans le périmètre `amusement-parks.fun` ;
- les parkItems sont réellement rattachés au parc ou à son histoire ;
- aucune entité douteuse n’a été enrichie artificiellement ;
- les éléments historiques fermés restent visibles quand ils sont utiles.

## Niveau de qualité attendu

Le résultat d’une commande de complétude vise le niveau `Excellent` du scoring, sans bloqueur de publication. Pour un parc majeur, viser 100 % des critères applicables et expliquer toute valeur inférieure. Pour un parc plus petit, fermé ou en projet, le même niveau de rigueur s’applique sur un périmètre naturellement plus réduit.

Un score élevé ne remplace jamais cet audit. Il est interdit de rendre un critère artificiellement non applicable, d’omettre une entité connue ou de publier un texte faible pour améliorer le score. Une lacune résiduelle n’est acceptable que si la donnée ou l’image reste introuvable après une recherche réelle et si cette limite est précisément documentée.

Même avec un score de 100, l’étape 9 reste incomplète tant que le corpus éditorial n’a pas été relu dans son ensemble. Les contrôles de présence ne détectent ni un paragraphe de secours répété, ni une traduction littérale faible, ni un conseil d’itinéraire injecté dans des dizaines de fiches.

Cette relecture est effectuée et assumée par Codex dans chacune des huit langues. Une sortie de traduction automatique non réécrite, une validation limitée à des motifs interdits ou une simple comparaison de longueurs bloque la fin de l’étape 9, même lorsque tous les champs sont présents.

## Tableau de couverture obligatoire

Produire les numérateurs, dénominateurs et identifiants manquants pour :

- attractions totales, puis attractions actuelles, annoncées/en construction et définitivement fermées ;
- attractions avec un `attractionDetails.status` lifecycle canonique, et liste des identifiants portant encore une valeur non canonique ;
- attractions avec 8 descriptions naturelles ;
- attractions avec au moins une image fidèle ;
- logo officiel présent, courant et sans watermark ajouté ;
- image principale du parc ;
- jalons historiques avec sources et image contextualisée ;
- articles avec sources joignables, localisations attendues et image contextualisée ;
- attractions définitivement fermées avec statut, période, description, image et jalon applicables ;
- conditions d’accès recherchées pour chaque attraction concernée ;
- tarifs actuels : devise, billets d’entrée, pass annuels et offres de parking, ou non-applicabilité/lacune sourcée ;
- lacunes restantes avec familles de sources consultées et raison de l’absence.

Le tableau est fondé sur le dernier export, pas sur les intentions des lots précédents. Tout écart inexpliqué déclenche une reprise ciblée de l’étape concernée.

## Audit JSON

Vérifier :

- `documentType`, `schemaVersion` et `mode` cohérents ;
- aucune section massive inutile ;
- aucune suppression implicite ;
- aucune donnée existante fiable écrasée ;
- `park.status` est l’une des six valeurs canoniques et correspond aux sources ;
- chaque `items[].attractionDetails.status` renseigné appartient au vocabulaire lifecycle contrôlé `Operating`, `UnderConstruction`, `TemporarilyClosed`, `ClosedDefinitively`, `Removed`, `Planned`, `Unknown` ;
- aucune transformation historique, aucun alias localisé ni aucun résumé du « dernier fait connu » n’est utilisé comme statut d’attraction ;
- un projet créé reste masqué par défaut tant que la revue et la décision de publication ne sont pas terminées ;
- `openingHours` est absent lorsque le statut du parc n’est pas `Operating` ;
- `pricing` est absent lorsque le statut du parc n’est pas `Operating` ;
- les dates ou périodes partielles fiables ont été conservées sans jour ou mois inventé ;
- toutes les clés sont résolues ;
- toutes les dates complètes sont sourcées ;
- les années seules fiables sont renseignées comme années seules, sans `01-01` ni premier jour de mois inventé ;
- `park.audienceClassification` est renseigné avec une valeur canonique, sauf reprise volontaire d’un parc legacy explicitement listé comme correction restante ;
- les notes expliquent les incertitudes.
- aucun champ obligatoire n’est cassé ;
- toute grille `pricing` contient au moins une offre actuelle vérifiée et utilise la propriété canonique, pas l’alias legacy `parkPricing` ;
- aucun doublon constructeur, exploitant ou fondateur n’est créé ;
- les données existantes fiables sont préservées en mode `merge`.
- toutes les valeurs enum utilisées existent dans `park-graph-upsert-enums.md` ;
- aucun alias legacy ou nombre enum n’est utilisé.

### Audit bloquant — statut lifecycle des attractions

Le backend conserve `AttractionDetails.Status` sous forme de chaîne et peut préserver une valeur inconnue pour des raisons de compatibilité. **Le fait qu’un JSON soit accepté techniquement ne valide donc pas la sémantique du statut.** L’étape 9 doit contrôler le contenu du champ indépendamment du Preview.

1. Lister toutes les valeurs distinctes de `items[].attractionDetails.status` avec leur nombre d’occurrences et les IDs concernés.
2. Considérer comme valides uniquement `Operating`, `UnderConstruction`, `TemporarilyClosed`, `ClosedDefinitively`, `Removed`, `Planned`, `Unknown`.
3. Traiter toute autre valeur comme un bloqueur de publication tant que sa signification n’a pas été reclassée.
4. Les alias de langue ou de source se convertissent vers le lifecycle canonique : par exemple `Annoncé` / `Announced` → `Planned`, `Ouvert` → `Operating`, sans conserver l’alias comme valeur stockée.
5. Les transformations historiques ne se convertissent pas en nouveau statut inventé : elles doivent être préservées dans la timeline.

Cas legacy à détecter explicitement, sans s’y limiter : `Retracké`, `Retracked`, `Délocalisé`, `Relocalisé`, `Relocated`, `Rénové`, `Refurbished`, `Rehab`, `Reconstruit`, `Rebuilt`, `Renommé`, `Renamed`, `Rethemé`, `Rethemed`, `Démonté`, `Dismantled`, `Stocké`, `Stored`, `Vendu`, `Sold`, `Transféré`, `Transferred`, `Réinstallé`, `Reinstalled`, `Remplacé`, `Replaced`, `Démoli`, `Demolished`.

Pour chaque cas trouvé :

- déterminer le vrai état courant depuis les sources et l’état physique/exploité de l’attraction ;
- produire une **reprise ciblée de l’étape 3** pour corriger `attractionDetails.status` ;
- vérifier si le fait historique existe déjà avec le bon propriétaire, le bon type, une période fiable et des sources ;
- sinon produire une **reprise ciblée de l’étape 8** qui crée le ou les événements adéquats ;
- ne jamais corriger le statut en faisant disparaître l’information historique.

Règles de cohérence minimales :

- retrack/refurbishment terminé + attraction rouverte → `Operating` avec événement `Retrack`, `Refurbishment` ou `Rehab` ;
- transformation en cours avec attraction fermée au public → généralement `TemporarilyClosed` avec événement correspondant ;
- renommage ou retheming d’une attraction toujours ouverte → `Operating` et événement `Rename`, `ThemeChange` ou `StoryChange` ;
- attraction transférée hors du parc, démontée, stockée ou démolie → `Removed` dans le parc d’origine et événement(s) de relocalisation/démontage/stockage/transfert/démolition ;
- attraction définitivement arrêtée mais encore présente → `ClosedDefinitively` et `DefinitiveClosure` ;
- ancienne attraction remplacée → `ClosedDefinitively` ou `Removed` selon sa présence réelle et `Replacement`; le remplaçant est une autre entité avec son propre lifecycle.

Tant qu’une valeur non canonique reste dans l’export sans exception métier explicitement validée par une évolution du code et des guidelines, la décision finale est **non prêt pour publication**.

## Audit contenu public

Vérifier :

- descriptions naturelles et spécifiques ;
- 8 langues présentes sur les lots complets ;
- pas de restrictions, tarifs, horaires ou notes admin dans les descriptions ;
- pas de formulations interdites ;
- titres et résumés historiques lisibles ;
- articles utiles et non redondants.
- les textes ne contiennent pas “upsert”, “SEO”, “contenu public” ou autre jargon interne.
- les événements et articles ne contiennent pas “repère documentaire prudent”, “présence publique confirmée”, justification de méthode, note d’audit ou formulation mécanique équivalente.
- les restrictions, tailles, horaires, dates, tarifs et coordonnées sont absents des descriptions narratives.
- les textes alternatifs, légendes et descriptions d’images sont naturels et éditoriaux ; ils ne contiennent aucune formulation technique, mécanique, justificative ou liée à l’outil d’import.
- les descriptions, timelines et articles ne déroulent ni tracé, ni rotations, ni accélérations, ni principe de fonctionnement et ne réinjectent pas vitesse, durée, capacité ou nombre de sièges et de véhicules depuis les données structurées.

### Audit transversal anti-gabarit

Auditer ensemble, dans chacune des huit langues : descriptions du parc, zones, parkItems, titres et résumés d’histoire, titres, sous-titres, résumés et paragraphes d’articles, descriptions d’images, textes alternatifs et légendes.

- normaliser le HTML et comparer les corps après retrait des titres, noms d’entités et balises de mise en forme ;
- signaler les paragraphes identiques et les familles de phrases quasi identiques, même lorsque le `<h2>` ou le nom injecté diffère ;
- rechercher les conseils d’itinéraire, les classements internes, les descriptions de « rôle dans la journée », les pauses suggérées entre files et tout remplissage de catégorie ;
- vérifier qu’aucune langue n’est plus générique, plus technique ou moins contextualisée que les autres ;
- rechercher par langue les familles de termes liées aux rails, voies, véhicules, sièges, structures, rotations, accélérations et trajectoires, puis relire manuellement chaque groupe dense. Un terme concret isolé peut être légitime ; une accumulation ou une succession opératoire est bloquante.
- rechercher les nombres et unités de vitesse, durée, capacité ou comptage dans les descriptions et articles ; ne conserver que ceux dont la valeur historique ou éditoriale est démontrée.
- relire manuellement les groupes détectés et corriger l’étape 4, 5 ou 7 correspondante avant publication ;
- après correction, contrôler la réponse Apply, mettre à jour l’état consolidé puis exécuter un Preview d’idempotence qui ne doit contenir aucune mutation ; ne relancer un export complet que si une incohérence précise reste impossible à résoudre par ces preuves ou par une lecture ciblée.

### Audit de profondeur et de mise en forme

Pour chaque langue, produire des mesures sur le texte visible après décodage HTML et retrait des balises : minimum, 10e percentile, médiane, 90e percentile et maximum. Les calculer séparément pour le parc, les zones, les parkItems par catégorie, les résumés de timeline et les articles.

- contrôler le nombre de `<h2>`, `<h3>`, `<p>`, listes et blocs d’article, pas seulement la présence d’une valeur HTML ;
- pour un parc majeur, reprendre toute description de parc qui reste une courte introduction plate au lieu du contrat `3 h2 / 5 p` de l’étape 4 ;
- reprendre tout parkItem publiable réduit à `1 h2 / 1 p`, ou nettement sous la bande indicative de 120 à 200 mots sans exception de source documentée ;
- signaler une langue dont le nombre de blocs diffère ou dont la longueur s’effondre par rapport aux sept autres, puis vérifier qu’aucun fait n’a disparu ;
- reprendre les résumés de timeline qui répètent seulement le titre ou restent sous la bande indicative de 25 à 55 mots sans exposer la portée du fait ;
- reprendre les articles durables sous 250 mots ou sans trois sections développées, et les articles de synthèse majeurs qui ne couvrent pas plusieurs périodes ou angles ;
- refuser les blocs vides, les intertitres génériques et tout texte ajouté uniquement pour atteindre un nombre.

Joindre ces distributions et les exceptions justifiées au tableau de couverture. Un score de 100 avec une médiane de descriptions très basse ou une structure uniforme `1 h2 / 1 p` reste un échec de l’étape 9.

Les crédits d’images restent exclus de la comparaison stylistique lorsqu’ils portent légitimement l’auteur, la source ou la licence. Les références globales sont auditées, mais une correction qui affecterait d’autres parcs doit devenir un lot transversal explicite au lieu d’être appliquée silencieusement dans le parc courant.

### Audit articles historiques

Pour chaque article publié ou prêt à publier, vérifier :

- titre spécifique et lisible sur mobile ;
- sous-titre naturel, sans formule générique ;
- sous-titre spécifique au sujet et non partagé comme gabarit avec les autres articles du parc ;
- résumé éditorial utile, pas une note de méthode ;
- paragraphes fluides, factuels et non redondants ;
- profondeur conforme au sujet : au moins 250 mots et trois sections développées pour un article durable de parc majeur, ou exception courte explicitement justifiée ;
- aucune phrase défensive ou méta du type “l’article n’a pas pour but”, “sans dramatisation”, “image contextuelle faute de mieux”, “source faible”, “repère documentaire prudent” ;
- aucune répétition de la description du parc ou du parkItem ;
- les termes sensibles sont nécessaires, sourcés et formulés sobrement ;
- les légendes décrivent l’image affichée et son rapport au sujet, sans justifier l’absence d’une autre image ;
- les incidents ou accidents retenus sur un parkItem ont un article associé et une photo contextualisée quand une image acceptable est trouvable.

Si un article semble écrit comme une note d’audit, une justification de prudence ou une réponse au reviewer, l’étape 9 doit exiger un JSON de correction ciblé avant publication.

## Audit conditions d’accès

Vérifier :

- chaque attraction a été contrôlée pour les conditions d’accès ;
- les conditions trouvées sont dans `items[].attractionDetails.accessConditions[]` ;
- les types et unités utilisent les enums canoniques ;
- les conditions avec accompagnement sont distinguées des tailles ou âges minimum simples ;
- les conditions absentes sont justifiées par une absence de source, pas par un oubli.

## Audit références

Vérifier :

- chaque constructeur lié à un item important a une biographie ou une limite de source documentée ;
- chaque fondateur lié au parc a une biographie ou une limite de source documentée ;
- chaque exploitant lié au parc a une description, des dates ou informations utiles quand elles sont sourçables ;
- les références existantes validées n’ont pas été écrasées ;
- aucun constructeur, exploitant ou fondateur doublon n’a été créé.

## Audit images

Vérifier :

- URLs externes techniquement importables par le flux remote image ;
- propriétaires résolus ;
- aucun warning Preview du type `Remote image ignored: owner could not be resolved` ;
- chaque image de parkItem possède une entrée `items[]` correspondante dans son JSON de livraison ;
- chaque image d’exploitant, de fondateur ou de constructeur possède la référence correspondante dans son JSON de livraison ;
- aucun `ownerKey` basé sur une URL, un nom de fichier, un dossier de galerie ou une valeur devinée ;
- alt texts et crédits localisés ;
- descriptions, alt texts et légendes décrivent la scène et son contexte plutôt que le statut officiel, l’import ou la catégorie technique du média ;
- pas de page HTML, preview non téléchargeable, image trompeuse ou watermark non autorisé ;
- images historiques correctement contextualisées.
- logo officiel actuel distinct de la photo principale, marqué comme logo courant et contrôlé dans l’export ;
- au moins une image fidèle par attraction actuelle, annoncée, en construction ou définitivement fermée quand elle est trouvable ;
- chaque fichier inspecté visuellement, sans watermark ou logo incrusté d’un site tiers ;
- chaque absence d’image justifiée par une recherche réelle et non par un simple oubli ;
- chaque jalon et article illustré par une image contextualisée quand elle est trouvable ;
- aucune image secondaire ou historique n’a remplacé par défaut une meilleure image courante.
- toutes les images importées pendant la commande de complétude restent en `isPublished: false` avant l’autorisation explicite.

### Audit images utilisées dans les articles

Pour chaque article avec image :

- l’image existe déjà dans l’export ou est créée dans le même JSON ;
- l’article utilise `imageId` quand l’image vient de l’export ;
- `imageKey` est réservé aux images créées dans le même JSON avec un `key` stable ;
- les `images[].key` restent uniques après suppression des espaces de bord et comparaison sans tenir compte de la casse ;
- chaque `mainImageKey`, `imageKey` ou valeur de `imageKeys` correspond exactement à une unique `images[].key` du même JSON ;
- un Preview sans avertissement n’est pas considéré comme une validation de ces clés d’images ;
- la légende est localisée dans les langues du lot ;
- la légende ne contient pas de justification technique, juridique ou documentaire ;
- pour incident/accident, une photo réelle non graphique et juridiquement réutilisable doit être privilégiée si disponible ;
- si l’image est seulement contextuelle, elle doit situer le lieu ou l’attraction sans faire croire qu’elle montre l’événement.

## Audit horaires et événements

Vérifier :

- horaires sourcés et récents pour `Operating` uniquement ;
- aucun CTA, calendrier ou donnée « ouvert maintenant » pour `Planned`, `UnderConstruction`, `TemporarilyClosed`, `ClosedDefinitively` ou `Cancelled` ;
- aucun tarif n’est stocké dans `openingHours`, ses libellés ou ses raisons ;
- événements nommés seulement ;
- pas de “ouverture estivale” générique transformée en événement ;
- labels et raisons localisés ;
- `openingHours.labels` et `openingHours.reasons` réservés aux événements nommés, exceptions datées ou informations temporaires vraiment utiles ;
- aucun commentaire général répété sur tous les jours normaux du calendrier ;
- fermetures exceptionnelles distinctes des fermetures définitives.

## Audit tarifs

Pour un parc `Operating`, vérifier la section `pricing` exportée de bout en bout :

- `parkId` correspond au parc audité et `currencyCode` contient trois lettres majuscules ;
- `sourceUrl`, `purchaseUrl` et les liens spécifiques pointent vers les pages officielles pertinentes ;
- `lastVerifiedAtUtc` correspond à la vérification réelle et la grille est encore actuelle ;
- les nombres de billets, pass annuels et offres de parking correspondent au registre consolidé ;
- chaque code est stable et unique dans sa collection ;
- chaque billet comporte une `audienceCategory` et des libellés publics compréhensibles ;
- chaque pass possède un nom localisé ; chaque parking possède un libellé localisé ;
- chaque offre possède au moins un prix en ligne ou au guichet sans duplication supposée entre les canaux ;
- `Fixed`, `Range` et `Dynamic` respectent leurs champs, leurs bornes et l’interdiction des montants négatifs ;
- les périodes ne sont pas inversées et les saisons correspondent aux sources ;
- les conditions localisées conservent les restrictions qui changent réellement l’offre ;
- les notes ne contiennent aucune consigne d’audit ou information interne ;
- l’export `Pricing` peut être réinjecté en Preview sans erreur et sans perte fonctionnelle.

Pour tout autre statut, la présence d’une grille actuelle est un bloqueur. Produire une correction ciblée de l’étape 7 sans présenter ces données au public. Une grille absente pour un parc `Operating` n’est acceptable qu’après recherche réelle et avec une lacune explicitement documentée.

## Audit histoire

Vérifier :

- timeline du parc cohérente ;
- timeline des parkItems majeurs cohérente ;
- relocalisations rattachées au bon propriétaire ;
- les transformations qui existaient à tort dans `attractionDetails.status` ont été conservées sous forme de vrais événements et ne se sont pas perdues pendant la correction ;
- articles seulement quand il existe un vrai angle ;
- résumés d’événements écrits pour les visiteurs, pas comme des notes d’audit documentaire ;
- sources présentes sur les événements importants ;
- toutes les URLs de sources d’articles et d’événements répondent au moment de l’audit ;
- aucune source ne pointe vers une 404, 410, erreur serveur, soft-404, page d’accueil de remplacement ou URL inventée ;
- les archives utilisées sont consultables et correspondent bien au contenu cité.
- pour un parc majeur ou historiquement riche, la timeline couvre les grandes périodes, transformations et fermetures documentables ;
- les annonces récentes à effet durable ont été vérifiées et disposent d’un article lorsqu’un développement éditorial est justifié ;
- l’inventaire des attractions définitivement fermées et la timeline se recoupent sans omission emblématique inexpliquée ;
- aucune ouverture, réouverture, fermeture temporaire ou fermeture définitive de parkItem n’est dupliquée dans la timeline du parc, car ces événements sont portés par la timeline du parkItem concerné ;
- chaque jalon visible et article possède une image contextualisée trouvable ou une exception documentée.

### Audit résolution history

Avant publication :

- aucun événement `ParkItem` ne doit dépendre uniquement de `itemKey` ou `parkItemKey` ;
- les événements de parkItem existants doivent contenir `ownerId`, `parkItemId`, `itemId` et `contextParkId` explicites ;
- aucun article ne doit référencer une image par `imageKey` si cette clé n’est pas créée ou enregistrée dans le même JSON ;
- Preview doit retourner 0 erreur et 0 warning bloquant avant Apply ;
- les corrections d’articles après audit restent des reprises ciblées de l’étape 8, pas une nouvelle étape.

## Décision publication

Garder `adminReviewStatus: "ToReview"` tant qu’une relecture humaine reste nécessaire.

La commande `Complète le parc <nom>` s’arrête ici. Elle ne vaut jamais autorisation de publication. Présenter le tableau de couverture, les corrections éventuelles et une décision `prêt pour publication` ; attendre une demande explicite telle que `Publie le parc <nom>`.

Ne pas masquer un parc déjà public pendant un enrichissement courant. Toute dépublication ou masquage d’un contenu public exige une instruction spécifique, sauf correction de sécurité ou obligation légale traitée hors de ce parcours.

Ne passer `isVisible` à `true` que pour les entités :

- pertinentes ;
- sourcées ;
- correctement localisées ;
- sans warning bloquant ;
- prêtes pour le public.

### Après autorisation explicite de publication

1. Avant toute mutation de visibilité, obtenir un nouvel export complet frais du parc par la voie autorisée du mode courant. En mode Codex, contrôler d’abord l’état global des opérations puis utiliser le client `PARK_DATA_EDITOR`. En mode ChatGPT guidé, demander ce nouvel export à l’utilisateur par la surface d’administration prévue, attendre son fichier et ne jamais appeler `park-data-editor/*`. Rapprocher ensuite cet export de l’état audité et des réponses des éventuelles corrections ciblées, puis rejouer tous les contrôles bloquants sur l’état réellement en ligne. Toute différence inexpliquée suspend la publication jusqu’à une correction ciblée et une nouvelle validation. Cet export appartient au flux de publication séparément autorisé et ne remet pas en cause l’unique export complet obligatoire du parcours de complétion 0 à 9.
2. Publier de façon ciblée les images validées, puis les articles et contenus dépendants prêts, pendant que le nouveau parc reste masqué. Réutiliser les IDs d’images exportés ; ne pas réimporter les fichiers.
3. Vérifier les statuts, descriptions, images courantes et sources des parkItems publiables. Ne pas rendre visible un item legacy inconnu au seul motif que la consigne dit « tout publier ».
4. Passer le parc à `Validated` et visible en dernier.
5. Contrôler anonymement la fiche publique, le logo, les attractions, les historiques et les articles dans les langues prises en charge.
6. Recontrôler le score et lancer un Preview d’idempotence : aucune modification inattendue ne doit rester.

Une défaillance d’une annonce sociale ou d’un service périphérique ne doit pas être confondue avec l’échec de publication des données. Rapporter les deux résultats séparément et ne jamais appeler une route d’administration non autorisée pour compenser.

### Publication Facebook demandée explicitement

La publication du parc ou de ses contenus ne vaut pas demande de publication Facebook. Si l’utilisateur la demande séparément, Codex utilise exclusivement le client `PARK_DATA_EDITOR` : `ResolveFacebookPublication` fournit d’abord le texte automatique et le carrousel paginé des images éligibles, puis `PublishFacebook` envoie le lien.

- sans `Message`, conserver le texte bilingue automatique résolu pour la fiche parc, le parkItem, la vidéo ou la page ;
- avec un texte fourni par l’utilisateur, transmettre ce texte personnalisé ;
- sans `ImageId`, conserver l’image et les règles Open Graph actuelles ;
- avec une image, employer uniquement un ID public renvoyé pour cette même cible : image `PARK` du parc ou image `PARK_ITEM` du parkItem ;
- ne pas publier une seconde fois l’annonce automatique de première mise en visibilité d’un parc sans instruction explicite.

## Sortie attendue

Produire :

- une liste de corrections restantes ;
- ou un dernier JSON upsert ciblé ;
- ou une décision “prêt pour publication” avec risques résiduels.
- toujours le tableau de couverture chiffré et le registre final des lacunes.

Ne pas ouvrir un nouveau chantier de fond à cette étape. Les améliorations non bloquantes deviennent des lots séparés.

À la fin de la réponse, ajouter `Pertinence de la prochaine étape` et indiquer qu’aucune étape officielle ne suit l’étape 9. Dire que le parcours 0 à 9 est terminé ou lister les corrections ciblées restantes. Ne pas proposer une nouvelle étape de workflow : si une correction appartient à une étape déjà parcourue, la nommer comme reprise ciblée de cette étape.
