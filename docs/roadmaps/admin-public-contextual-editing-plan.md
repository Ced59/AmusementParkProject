# Plan d'action - edition contextuelle admin sur pages publiques

Date de cadrage : 2026-06-21

## Objectif

Permettre a un administrateur authentifie de parcourir les pages publiques comme un visiteur, puis d'activer volontairement un mode edition qui rend certains blocs de donnees editables depuis la page courante.

Le mode edition doit proposer, selon le bloc :

- une edition par formulaire quand le bloc est simple et deja couvert par les contrats admin existants ;
- un export JSON borne au bloc selectionne ;
- une preview puis un apply JSON borne au bloc selectionne ;
- un ajout cible d'enfant ou de reference avec tous les ids necessaires au rattachement.

L'experience visiteur, le SEO, le SSR, le cache public et le bundle public initial ne doivent pas etre modifies par defaut.

## Constat repo utile

- Les facades publiques utilisent deja `anonymousHttpOptions()` pour ignorer le token et lire les donnees comme un visiteur non authentifie.
- Les endpoints publics ont deja une logique serveur permettant a un admin de voir certaines donnees non visibles si la requete est authentifiee.
- Les endpoints `admin/localized-content` couvrent deja un import JSON partiel par entite.
- Les endpoints `admin/park-graph-upserts` couvrent deja preview, apply, historique et export d'un graphe de parc.
- L'invalidation des caches publics sait deja cibler les entites modifiees par les imports et les upserts.
- Les composants et facades admin existants doivent rester le point de depart pour les formulaires, mappers, validations et ports.

Ces bases doivent etre reutilisees, mais pas exposees telles quelles dans les pages publiques sans garde-fous.

## Principes invariants

1. Mode visiteur par defaut
   - Une session admin ne doit pas changer automatiquement les donnees lues par les pages publiques.
   - Le rendu initial reste celui d'un visiteur non authentifie.
   - Les appels publics restent anonymes tant qu'un mode de vue admin n'est pas choisi explicitement.

2. Admin lazy et isole
   - Aucun composant d'edition, editeur JSON, formulaire admin lourd ou logique d'orchestration admin ne doit entrer dans le bundle public initial.
   - La toolbar admin et les drawers d'edition doivent etre charges a la demande et seulement cote navigateur.

3. Bloc borne
   - Chaque action cible un bloc declare par un contrat explicite.
   - Le JSON exporte ou applique ne contient que le bloc selectionne et les references indispensables.
   - Les champs hors scope du bloc doivent etre refuses par le backend.

4. Localisation complete des upserts JSON
   - Un bloc contenant des champs localises doit pouvoir etre exporte, previsualise et applique avec toutes les langues supportees par le site.
   - Le JSON borne doit fournir les ids et les codes langue necessaires pour rattacher chaque variante localisee au bon bloc.
   - Une mise a jour multilingue ne doit pas supprimer silencieusement une langue absente du JSON ; l'absence doit etre refusee, ignoree explicitement ou traitee par une option claire selon le contrat du bloc.
   - Les erreurs de validation doivent indiquer la langue concernee et rester lisibles sur mobile.

5. Preview avant mutation JSON
   - Tout upsert JSON contextuel passe par une preview.
   - L'apply doit reutiliser les validations metier, l'audit admin et l'invalidation cache existants.

6. Ajout cible non dangereux
   - Toute creation depuis une page publique doit etre rattachee par ids explicites.
   - Les contenus publicables crees rapidement restent caches ou `ToReview` par defaut lorsque le domaine le permet.

7. Cache et SEO proteges
   - Une reponse authentifiee admin ne doit jamais etre stockee dans le cache public.
   - Le mode edition ne doit pas modifier les balises SEO, les canoniques, les hreflang, le robots/noindex ou le HTML SSR visiteur.

8. Roles explicites
   - Les vues "visiteur non authentifie", "visiteur role user", "moderateur" et "admin" sont des modes de simulation d'affichage.
   - Seul le role admin donne acces aux actions d'edition.
   - Les roles simules ne remplacent jamais l'autorisation serveur reelle.

9. Responsive obligatoire a chaque jalon
   - Chaque jalon doit etre utilisable entierement sur telephone.
   - Une fonctionnalite admin contextuelle n'est pas livrable si elle fonctionne seulement sur desktop.
   - Les overlays, drawers, toolbars, formulaires, previews JSON, tableaux de changements et boutons de bloc doivent avoir un comportement mobile dedie.
   - Les controles tactiles doivent rester assez grands, lisibles, accessibles au clavier quand pertinent et sans debordement horizontal.
   - Chaque PR doit decrire la verification mobile realisee.

## Exigence mobile de chaque jalon

Le but operationnel est de pouvoir administrer depuis un telephone. Pour cette raison, chaque jalon ci-dessous a une definition de done responsive implicite :

- toolbar utilisable au pouce, sans masquer les actions publiques essentielles ;
- selection de bloc lisible sur petit ecran, sans hover obligatoire ;
- drawer ou plein ecran mobile plutot qu'une modale desktop compressee ;
- formulaire sauvegardable sans perdre les champs quand le clavier mobile s'ouvre ;
- JSON import/export lisible avec actions fixes ou faciles a retrouver ;
- preview de changements consultable en liste verticale, pas seulement en tableau large ;
- boutons d'ajout et d'edition avec zones tactiles stables ;
- aucun debordement horizontal sur les pages publiques ;
- retour clair au mode visiteur.

Aucun jalon ne doit etre merge si son flux principal n'est pas completement praticable sur mobile.

## Modele de bloc cible

Chaque bloc editable devra etre declare dans un registre frontend et reconnu par un contrat backend.

Champs minimum :

- `blockType` : identifiant stable du bloc, par exemple `park.hero`, `park.description`, `park.practical`, `parkItem.description`.
- `entityType` : type d'entite principale, par exemple `park`, `parkItem`, `parkZone`, `image`.
- `entityId` : id de l'entite principale.
- `parentIds` : ids necessaires au rattachement, par exemple `parkId`, `zoneId`, `itemId`, `ownerId`.
- `jsonScope` : liste des champs exportables et importables pour ce bloc.
- `localizedScopes` : liste des champs localises et des langues attendues quand le bloc contient du contenu multilingue.
- `capabilities` : `exportJson`, `previewJson`, `applyJson`, `editForm`, `addChild`, `openFullAdmin`.
- `refreshStrategy` : rechargement local du bloc, rechargement de la facade, ou navigation.
- `riskLevel` : faible, moyen ou eleve pour guider les tests et la review.

Exemple conceptuel :

```json
{
  "blockType": "park.description",
  "entityType": "park",
  "entityId": "park-123",
  "parentIds": {
    "parkId": "park-123"
  },
  "jsonScope": [
    "descriptions"
  ],
  "localizedScopes": [
    {
      "field": "descriptions",
      "requiredLanguages": [
        "fr",
        "en",
        "de",
        "nl",
        "it",
        "es",
        "pl",
        "pt"
      ]
    }
  ],
  "capabilities": [
    "exportJson",
    "previewJson",
    "applyJson",
    "editForm",
    "openFullAdmin"
  ],
  "refreshStrategy": "reload-current-public-facade",
  "riskLevel": "medium"
}
```

## Jalons

### Jalon 1 - Socle de vue admin sans mutation

But : donner a l'admin une presence controlee sur les pages publiques sans changer les donnees visiteur.

Livrables :

- Toolbar admin chargee en lazy sur le layout public et visible uniquement pour un admin authentifie.
- Etat frontend de vue : `anonymousVisitor`, `userVisitor`, `moderatorVisitor`, `adminPreview`.
- Mode edition desactive par defaut.
- Les pages publiques continuent a utiliser `anonymousHttpOptions()` par defaut.
- Tests frontend montrant que la toolbar est absente hors admin et que les appels publics restent anonymes par defaut.
- Layout mobile de la toolbar valide sur telephone, avec actions tactiles accessibles.

Fin fonctionnelle :

- L'admin peut naviguer comme un visiteur lambda.
- Il peut changer de vue dans la toolbar.
- Aucun bouton d'edition n'apparait tant que le mode edition n'est pas active.
- Un visiteur non connecte ne telecharge pas les modules d'edition.

Risques a verifier :

- Pas de changement SSR.
- Pas de metadata admin.
- Pas de regression header/mobile.

### Jalon 2 - Registre de blocs et selection passive

But : rendre les blocs selectionnables sans mutation.

Livrables :

- Registre frontend de blocs editables.
- Directive ou composant wrapper leger pour annoter les blocs publics.
- Premiere page pilote : fiche parc.
- Blocs pilotes : hero, description, infos pratiques.
- Drawer de diagnostic affichant type de bloc, ids, capacites et lien vers l'edition admin complete.

Fin fonctionnelle :

- En mode edition, les blocs pilotes affichent une action "editer".
- Cliquer un bloc ouvre un panneau de contexte.
- Aucun appel de mutation n'est possible dans ce jalon.
- Le meme parcours fonctionne au tactile sans hover et sans debordement.

Risques a verifier :

- Les wrappers ne doivent pas casser le layout public.
- Les wrappers ne doivent pas etre visibles dans le DOM visiteur hors admin.
- Les ids exposes doivent etre uniquement ceux deja necessaires a l'admin.

### Jalon 3 - Export JSON borne au bloc

But : permettre de telecharger l'etat actuel d'un bloc selectionne.

Livrables :

- Contrats Application/WebAPI d'export contextualise.
- Endpoint admin dedie, par exemple `GET admin/contextual-blocks/{blockType}/{entityId}/export`.
- Export pour `park.description` et `park.practical`.
- JSON contenant le bloc et les ids de rattachement strictement necessaires.
- JSON contenant les variantes localisees du bloc quand le bloc est multilingue, avec codes langue explicites.
- Tests backend sur le bornage et les champs exportes.
- Service data-access admin et facade frontend dediee.

Fin fonctionnelle :

- Depuis la fiche parc publique, l'admin selectionne le bloc description.
- Il telecharge un JSON centre sur ce bloc.
- Le JSON ne contient pas le graphe complet du parc.
- Un bloc localise exporte toutes les langues attendues par le contrat du bloc.
- Le telechargement et les actions d'export restent accessibles sur telephone.

Risques a verifier :

- Ne pas exposer de champs admin sensibles inutiles.
- Ne pas reutiliser un export complet puis filtrer uniquement cote front.

### Jalon 4 - Preview JSON borne

But : valider un JSON de bloc sans appliquer la mutation.

Livrables :

- Endpoint admin `preview` dedie.
- Validation des champs autorises par `blockType`.
- Validation des variantes localisees attendues par le `blockType`, avec erreurs par langue.
- Mapping vers les commandes existantes quand le scope correspond deja a `LocalizedContent` ou `ParkGraphUpsert`.
- Resultat de preview lisible : champs modifies, warnings, erreurs, bloc cible.
- Tests backend pour refus des champs hors scope.
- UI JSON avec erreurs non destructives.

Fin fonctionnelle :

- L'admin modifie le JSON d'un bloc description.
- La preview indique les changements attendus.
- Un champ hors scope est refuse avant apply.
- Une variante localisee manquante ou mal rattachee est signalee avant apply.
- La preview est lisible en vertical sur telephone.

Risques a verifier :

- La preview ne doit pas invalider les caches publics.
- Le JSON invalide ne doit jamais vider le contenu courant.

### Jalon 5 - Apply JSON borne et refresh de page

But : appliquer une mutation JSON bornee et mettre a jour la page courante.

Livrables :

- Endpoint admin `apply` dedie.
- Audit admin avec `blockType`, `entityType`, `entityId`.
- Invalidation cache ciblee par entite impactee.
- Refresh frontend de la facade publique courante apres succes.
- Gestion d'erreur conservant le JSON saisi.
- Tests backend d'audit/invalidation et tests frontend de refresh.

Fin fonctionnelle :

- L'admin applique un JSON valide sur un bloc pilote.
- La page publique se met a jour sans reload complet.
- Le visiteur normal ne voit aucun changement d'interface.
- Le flux JSON complet reste utilisable avec clavier mobile ouvert.

Risques a verifier :

- Une reponse admin ne doit pas etre servie depuis le cache public.
- Les pages SSR publiques doivent etre invalidees seulement quand l'apply reussit.

### Jalon 6 - Formulaire contextuel pour blocs simples

But : eviter le JSON pour les corrections simples.

Livrables :

- Drawer formulaire pour blocs simples.
- Reutilisation des mappers, validators, facades et ports admin existants.
- Premier scope formulaire : descriptions de parc puis descriptions de park item.
- Bouton "ouvrir l'edition complete" pour les blocs non couverts.
- Tests des mappers et facades contextuelles.

Fin fonctionnelle :

- L'admin modifie une description depuis la page publique par formulaire.
- Les memes validations que l'admin classique s'appliquent.
- La page courante se rafraichit apres sauvegarde.
- Le formulaire est concu mobile-first et conserve les donnees en cas d'erreur.

Risques a verifier :

- Ne pas dupliquer des formulaires lourds dans le public initial.
- Ne pas mettre d'orchestration metier dans les composants publics.

### Jalon 7 - Ajout cible d'entites enfants

But : creer depuis le contexte public sans perdre le rattachement.

Livrables :

- Actions d'ajout selon bloc : zone dans parc, item dans parc/zone, image sur parc/item, condition d'acces sur item.
- Contrats de creation contenant les ids parents explicites.
- Valeurs par defaut non publicables quand pertinent.
- Preview ou confirmation quand l'ajout a un impact public.
- Tests backend et frontend par type d'ajout.

Fin fonctionnelle :

- Depuis un parc public, l'admin ajoute un item rattache au parc ou a une zone.
- L'item est cree avec les valeurs admin sures.
- La page publique ne publie pas accidentellement du contenu incomplet.
- Le flux d'ajout est completement praticable depuis telephone.

Risques a verifier :

- Collision de noms et doublons.
- Ajout dans le mauvais parc ou la mauvaise zone.
- Publication involontaire.

### Jalon 8 - Extension aux pages publiques principales

But : couvrir progressivement les surfaces utiles.

Ordre recommande :

1. Fiche parc.
2. Fiche park item.
3. Liste items d'un parc.
4. Pages zones.
5. Galeries images.
6. Videos et references seulement si les besoins sont confirmes.

Regle :

- Une PR ne doit ajouter qu'un petit ensemble de blocs.
- Chaque bloc doit avoir son contrat, ses tests et son comportement de refresh.

Fin fonctionnelle :

- Les pages principales disposent de blocs editables coherents.
- L'admin garde le parcours visiteur.
- Le visiteur ne voit aucune difference.
- Chaque nouvelle page couverte est validee sur mobile avant merge.

### Jalon 9 - Simulation de roles robuste

But : rendre les vues par role utiles sans affaiblir la securite.

Livrables :

- Contrat frontend clair entre vue simulee et droits reels.
- Eventuel header/query interne admin pour demander une lecture "comme role X" si necessaire.
- Backend qui refuse toute simulation non admin.
- Cache bypass ou policy no-store pour les lectures authentifiees/simulees.
- Tests de non fuite cache.

Fin fonctionnelle :

- L'admin peut comparer le rendu anonyme, user, moderateur et admin.
- Les actions d'edition restent uniquement disponibles en role admin reel.
- La selection de vue reste claire et rapide sur telephone.

Risques a verifier :

- Ne jamais confondre role simule et role autorise.
- Ne pas stocker une reponse simulee dans un cache partage.

### Jalon 10 - Durcissement final

But : rendre le dispositif exploitable en production.

Livrables :

- Tests cache API : les requetes avec `Authorization` restent non cachees.
- Tests SSR/SEO : pas de metadata admin et pas de contenu hidden rendu visiteur.
- Tests architecture : facades/ports respectes.
- Tests responsive admin overlay sur mobile.
- Tests ou verification documentee du parcours mobile principal de chaque jalon.
- Documentation operateur courte.
- Checklist de review dediee.

Fin fonctionnelle :

- Le workflow est stable de bout en bout.
- Les risques visiteur, SEO, SSR et cache sont couverts par tests.
- La documentation permet de reprendre le chantier plus tard.
- Les flux critiques sont valides sur telephone.

## Endpoints probables a ajouter

Ces noms sont indicatifs et devront etre ajustes au style final de l'API.

- `GET admin/contextual-blocks/{blockType}/{entityId}/export`
- `POST admin/contextual-blocks/{blockType}/{entityId}/preview`
- `POST admin/contextual-blocks/{blockType}/{entityId}/apply`
- `GET admin/contextual-blocks/capabilities`

Les endpoints doivent rester dans WebAPI, deleguer a Application et ne jamais manipuler directement Mongo.

## Frontend cible

Structure probable :

- `features/admin/contextual-editing`
  - state/facades
  - models
  - mappers
  - overlay/drawer components
  - JSON editor shell
- `data-access/admin/contextual-blocks-api.service.ts`
- `ui/layouts/public-admin-toolbar`
- wrappers legers cote public, sans orchestration metier.

Regle de chargement :

- Le layout public peut connaitre l'existence d'un point d'extension admin.
- Les composants lourds d'edition doivent etre importes dynamiquement.

## Backend cible

Structure probable :

- `Application/Features/ContextualEditing`
  - queries export/capabilities
  - commands preview/apply
  - block registry
  - scope validators
  - mappers vers commandes existantes
- `WebAPI/Contracts/ContextualEditing`
- `WebAPI/Controllers/ContextualEditingController`

Regle :

- Les commandes existantes restent la source de verite pour les validations metier.
- Le contextual editing orchestre et borne, il ne remplace pas les services admin existants.

## Tests minimum par PR

Pour tout jalon touchant le backend :

- tests Application sur le scope du bloc ;
- tests WebAPI sur l'autorisation admin ;
- tests d'erreur pour champ hors scope ;
- tests d'invalidation cache si mutation.

Pour tout jalon touchant le frontend :

- tests facade/mapper ;
- test que le mode visiteur reste anonyme ;
- test que l'overlay admin n'apparait pas hors admin ;
- test responsive si un drawer ou une toolbar est ajoute.
- verification mobile documentee du parcours principal du jalon.

Pour tout jalon touchant SEO/SSR/cache :

- test de non-indexation admin si une route ou un etat technique apparait ;
- test cache pour `Authorization` et pour les modes simules ;
- test que la page visiteur initiale garde ses donnees publiques.

## Definition of done globale

La fonctionnalite complete est consideree terminee seulement quand :

- l'admin peut parcourir les pages publiques comme un visiteur ;
- l'admin peut activer/desactiver le mode edition ;
- chaque bloc expose ses ids et son scope ;
- les exports JSON sont bornes au bloc ;
- les blocs localises sont exportables et upsertables avec toutes leurs langues attendues ;
- les applies JSON passent par preview ;
- les formulaires contextuels reutilisent les patterns admin existants ;
- les ajouts ciblent explicitement leurs parents ;
- les caches publics ne stockent jamais de vues admin ;
- le SEO/SSR visiteur reste inchange ;
- le bundle public initial ne contient pas les modules admin lourds ;
- chaque jalon est entierement utilisable sur telephone ;
- les tests couvrent les garde-fous principaux.

## Ordre de PR recommande

1. Documentation de cadrage et version.
2. Toolbar admin passive et mode de vue.
3. Registre de blocs passif sur fiche parc.
4. Export JSON borne pour `park.description`.
5. Preview/apply JSON borne pour `park.description`.
6. Formulaire contextuel pour `park.description`.
7. Extension `parkItem.description`.
8. Ajout cible park item depuis parc/zone.
9. Simulation de roles cote lecture.
10. Durcissement cache/SSR/SEO et documentation operateur.

Chaque PR doit rester mergeable seule et apporter un etat fonctionnel sans effet de bord visiteur.
