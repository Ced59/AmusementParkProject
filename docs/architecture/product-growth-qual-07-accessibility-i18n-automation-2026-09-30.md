# QUAL-07 — Automatisation accessibilité et i18n

## Décision métier

Une évolution ne doit pas pouvoir être livrée si elle rend une action essentielle
muette pour un lecteur d’écran, dépend exclusivement de la souris ou oublie une
langue supportée. La vérification est donc exécutée dans la CI de chaque pull
request et de chaque livraison `master`, sans code ni coût au runtime.

## Périmètre livré

### Accessibilité statique des templates Angular

Le contrôle parcourt les templates HTML externes et les templates inline des
composants de production. Il bloque désormais toute nouvelle occurrence de :

- image sans attribut `alt`, y compris lorsque l’image devrait être décorative ;
- bouton ou lien actif sans texte, contenu projeté ou nom ARIA ;
- cible de clic HTML non native sans sémantique interactive et sans équivalent
  clavier ;
- template Angular que le parseur ne peut pas analyser.

Les composants de test et leurs helpers ne sont pas inclus dans l’inventaire
applicatif. Les composants personnalisés restent responsables de leur propre
contrat clavier afin d’éviter de juger leur accessibilité depuis un simple appel
de template.

### Cohérence des huit langues

La gate réutilise le compilateur et le validateur i18n existants. Elle vérifie :

- la reproductibilité des catalogues générés ;
- la présence des mêmes clés dans les huit langues ;
- les chaînes vides et les placeholders divergents ;
- les clés littérales utilisées mais absentes ;
- les nouvelles collisions de feuilles entre modules source, auparavant écrasées
  silencieusement par l’ordre de fusion.

## Dette historique bornée

Le premier inventaire conserve une baseline explicite de 28 constats
d’accessibilité et 24 collisions i18n, soit trois clés historiques présentes dans
les huit langues. Cette baseline n’autorise aucune nouvelle dette : une apparition
fait échouer la CI. Une résolution fait également échouer la baseline jusqu’à sa
mise à jour volontaire, afin que la diminution de dette soit versionnée et ne soit
pas masquée par un déplacement de ligne.

Les empreintes d’accessibilité utilisent le fichier, la règle, le contenu complet
normalisé de l’élément et son chemin structurel dans le template, jamais le numéro
de ligne. Une insertion de texte au-dessus d’un contrôle ne crée donc pas un faux
changement de dette, tandis qu’un élément identique déplacé vers une autre branche
ne peut pas remplacer silencieusement un constat résolu.

## Exploitation

Commandes locales :

```bash
npm run quality:accessibility-i18n:test
npm run quality:accessibility-i18n
```

Après correction réelle d’un constat historique, la baseline concernée peut être
réduite avec l’option `--update-baseline`, puis le diff doit être relu dans la PR.
Cette option ne doit jamais servir à accepter une nouvelle régression.

## Coût et limites

- aucune dépendance de production ni dépendance de développement supplémentaire ;
- aucune modification du bundle navigateur, de MongoDB ou de l’API ;
- analyse effectuée uniquement pendant le développement et la CI ;
- la gate statique ne remplace pas les tests manuels clavier/lecteur d’écran ni un
  audit WCAG complet ;
- le contraste, l’ordre de focus effectif, les annonces dynamiques et le rendu
  responsive restent couverts par des tests spécialisés et des revues visuelles.

## Rollback

Le rollback consiste à retirer l’étape CI et les scripts. Il ne modifie aucune
donnée et n’affecte aucun parcours public déjà déployé.
