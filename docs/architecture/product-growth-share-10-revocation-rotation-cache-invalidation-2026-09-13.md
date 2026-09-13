# SHARE-10 — Révocation, rotation et invalidation des caches

Date : 13 septembre 2026
Version : 5.3.2

## Résultat métier

Le propriétaire d'un classement, d'une visite, d'une année ou d'un passeport
partagé dispose des mêmes actions partout :

- **rendre privé** coupe immédiatement le lien public ;
- **remplacer le lien** invalide l'ancienne adresse et fournit une nouvelle
  adresse pour le même contenu public approuvé ;
- modifier le contenu reste une opération distincte qui exige un nouvel aperçu.

La garantie d'arrêt ne repose jamais sur un cache. Une lecture publique doit
retrouver le jeton opaque actif dans `SharePublication`; un ancien jeton ne peut
donc plus résoudre la publication dès que l'écriture MongoDB a réussi.

## Autorité unique

`SharePublicationLifecycleService` orchestre les deux transitions déjà définies
par le domaine `SharePublication`. Il est appelé par les commandes centrales :

```text
POST   /me/shares/{publicationId}/rotate-link
DELETE /me/shares/{publicationId}
```

Les anciennes routes spécialisées de révocation restent momentanément acceptées
pour la compatibilité des clients déployés. Elles ne constituent pas un second
système : leur handler résout la source typée puis délègue au même service de
cycle de vie. Les clients Angular utilisent uniquement les routes centrales.

## Rotation sans dérive de contenu

Les récapitulatifs de visite, d'année et de passeport sont des snapshots publics.
Leur rotation suit cet ordre :

```text
propriétaire
    │ demande une rotation
    ▼
relecture de SharePublication par propriétaire + identifiant
    │
    ├─ source modifiée ? ──► refus : nouvel aperçu obligatoire
    │
    ▼
clonage du snapshot public exact : version N → N+1
    │ échec ou snapshot absent
    ├──────────────────────► refus, ancien lien inchangé
    ▼
remplacement atomique du jeton et de la version de publication
    │ collision de jeton
    ├──────────────────────► nouvelle tentative bornée
    ▼
ancien jeton invalide, nouveau jeton actif
```

Le clonage ne relit ni la visite, ni le passeport, ni un commentaire privé. Il
vérifie la version source, la politique, l'empreinte et la version du snapshot
avant de recopier le DTO public déjà validé. Le classement personnel, qui est
résolu depuis sa source publique versionnée et ne possède pas de snapshot, suit
la même transition de jeton sans clonage.

Une seconde lecture de la version source après l'écriture ferme la petite fenêtre
de concurrence. Si la source a changé pendant la rotation, le nouveau lien est
révoqué avant de renvoyer un échec.

## Révocation

La révocation effectue un remplacement optimiste borné au propriétaire. Elle :

1. incrémente la version publique et la version de persistance ;
2. passe la visibilité à `Private` ;
3. supprime le jeton public actif ;
4. conserve la trace temporelle de révocation ;
5. programme la convergence des caches dérivés.

Une publication déjà révoquée reste idempotente. Une publication d'un autre
propriétaire est volontairement indiscernable d'une publication absente.

## Convergence des caches

L'écriture autoritative et la purge sont découplées :

```text
écriture SharePublication réussie
        │
        ├────────► réponse métier réussie
        │
        ▼
file interne coalescée par publication
        │
        ├─► éviction du cache mémoire des images sociales
        │
        └─► purge SSR des huit langues
              ├─ ancien jeton
              └─ nouveau jeton, lors d'une rotation
                    │
                    └─ échec non confirmé : nouvelle tentative
```

Les chemins SSR sont calculés selon le type de publication et envoyés avec
`allowStale=false` et `refresh=false`. Une indisponibilité du moteur SSR ne remet
jamais un lien révoqué en service : toutes les lectures publiques continuent de
valider le jeton contre l'autorité centrale.

Une republication purge à la fois l'ancien jeton suspendu et le nouveau jeton.
Si une visite source est supprimée, la même file invalide son récapitulatif ainsi
que les rendus propriétaire, annuel et passeport susceptibles d'en dépendre. La
relecture publique de la source reste la barrière d'accès autoritative pendant la
convergence des caches.

Les réseaux sociaux externes peuvent conserver une image qu'ils ont déjà copiée.
Cette limite extérieure est indiquée dans les huit langues. Amusement Parks ne
sert toutefois plus l'image ni la page associée à l'ancien jeton.

## Frontend et responsive

Les façades Angular dépendent de leurs ports de partage, jamais du service HTTP
concret. L'identifiant interne de publication est utilisé uniquement pour appeler
la commande propriétaire et n'est pas affiché. Après rotation, l'état local reçoit
immédiatement le nouveau lien retourné par le serveur.

Les groupes d'actions acceptent le retour à la ligne et deviennent pleine largeur
sur les petits écrans. Les notes de cache autorisent la césure des chaînes longues
et aucun contrôle n'impose de largeur minimale supérieure au viewport de 320 px.

## Preuves automatisées

- clonage du snapshot exact avant rotation ;
- refus si la source approuvée a changé ;
- remplacement atomique du jeton et incrément des versions ;
- révocation toujours réussie malgré une file de purge indisponible ;
- purge de l'ancien et du nouveau lien lors d'une republication ;
- purge des récapitulatifs affectés lors de la suppression d'une visite source ;
- isolation stricte par propriétaire ;
- purge des seize routes localisées ancien/nouveau par type ;
- éviction du rendu social en mémoire ;
- contrats HTTP authentifiés et mapping de la nouvelle version ;
- appels Angular centraux, encodage des identifiants et mise à jour des façades ;
- contrôles d'architecture façade/port et une classe par fichier.

## Schéma MongoDB

Aucune migration MongoDB n'est nécessaire pour SHARE-10. Les champs utilisés
(`shareToken`, `status`, `visibility`, `publicationVersion`, `version`,
`revokedAtUtc`) existaient déjà dans la publication centrale. La rotation ajoute
un snapshot à la version suivante dans les collections de snapshots existantes ;
les index et contrats persistés ne changent pas.
