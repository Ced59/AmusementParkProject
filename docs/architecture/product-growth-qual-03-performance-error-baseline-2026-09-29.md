# QUAL-03 — Baseline performance et erreurs

Date de référence : 29 septembre 2026. Version publique mesurée : `5.4.21`.

## 1. Résultat métier

Le produit possède désormais un point zéro reproductible avant les prochains
jalons qualité. Une évolution ne peut plus augmenter silencieusement le poids du
front : la CI compare chaque build aux budgets versionnés. Les principaux parcours
publics peuvent être sondés avec une méthode stable qui sépare succès,
redirections, erreurs client, erreurs serveur, expirations et pannes de transport.

Les logs API utilisent les mêmes familles, des identifiants d'événements stables
et le modèle de route plutôt que les identifiants présents dans l'URL. Ils ne
recopient plus la query string ni le user-agent. La baseline reste donc exploitable
sans collecter ces métadonnées inutiles.

## 2. Périmètre et non-objectifs

Ce jalon mesure :

- quatre entrées publiques françaises : accueil, liste des parcs, classements et
  Park Fit ;
- la santé de l'API et son contrat public de capacités ;
- le poids brut du chargement initial, du plus gros chunk différé, de tout le
  JavaScript navigateur et des feuilles de style ;
- la famille de résultat HTTP et les erreurs réseau de chaque échantillon.

Il ne prétend pas mesurer :

- les Core Web Vitals d'utilisateurs réels ;
- un rendu froid sans cache, une connexion mobile bridée ou un appareil bas de
  gamme ;
- le CPU, la RAM, le disque ou les percentiles internes du VPS ;
- un taux d'erreur produit représentatif à partir de trente requêtes synthétiques.

Ces mesures demandent respectivement l'instrumentation typée et les tableaux de
pilotage de `QUAL-05`/`QUAL-06`, puis l'alerting de `QUAL-09`. La présente tranche
n'invente donc aucune donnée absente.

## 3. Mesure publique de référence

Le relevé a été exécuté depuis le poste de développement contre
`https://amusement-parks.fun`, avec un échauffement puis cinq requêtes séquentielles
par cible, une pause de 150 ms, un timeout de 10 s et le percentile par rang le
plus proche. Le comportement normal du cache public est conservé.

| Cible | Mode observé | p50 | p95 | Taille maximale | Résultat |
|---|---:|---:|---:|---:|---:|
| `/fr/home` | `SSR_CACHE_HIT` | 24,89 ms | 31,10 ms | 322 160 o | 5/5 succès |
| `/fr/parks` | `SSR_CACHE_HIT` | 32,14 ms | 38,13 ms | 336 541 o | 5/5 succès |
| `/fr/rankings` | `SSR_CACHE_HIT` | 16,51 ms | 23,49 ms | 73 581 o | 5/5 succès |
| `/fr/park-fit` | `CSR_FALLBACK` | 13,30 ms | 16,77 ms | 1 507 o | 5/5 succès |
| `/api/health` | API | 16,13 ms | 26,43 ms | 2 989 o | 5/5 succès |
| `/api/public/capabilities` | API | 19,47 ms | 24,21 ms | 51 o | 5/5 succès |

Les trente réponses portent le statut `200`. Ce résultat prouve uniquement la
disponibilité de ces cibles pendant la fenêtre de mesure. Le repli CSR de Park Fit
est une limite connue : la page reste fonctionnelle, mais elle ne bénéficie pas du
même HTML SSR immédiatement utile que les trois autres pages sondées.

## 4. Baseline et budgets du bundle

Le relevé du dernier build disponible au moment de la tranche donne :

| Indicateur brut non compressé | Référence | Budget bloquant |
|---|---:|---:|
| chargement initial JS + CSS | 890 657 o | 1 000 000 o |
| plus gros chunk JavaScript différé | 215 023 o | 300 000 o |
| JavaScript navigateur total | 6 685 643 o | 7 200 000 o |
| feuilles de style totales | 217 580 o | 250 000 o |

Le budget Angular historique de 10 Mo ne protégeait pas réellement le visiteur.
Il est remplacé par un plafond initial de 1 Mo et un plafond de 150 ko pour une
feuille de style de composant. Le contrôle complémentaire lit `index.csr.html`,
parcourt les imports JavaScript statiques, additionne les vrais actifs initiaux et
vérifie aussi les chunks différés et le volume total. Les valeurs sont brutes pour
rester déterministes entre CI ; elles
ne prétendent pas représenter le transfert compressé sur le réseau.

## 5. Taxonomie d'erreurs API

Chaque requête journalisée porte désormais :

| Élément | Valeurs |
|---|---|
| `StatusFamily` | `2xx`, `3xx`, `4xx`, `5xx` |
| `Outcome` | `Success`, `Redirection`, `ClientError`, `ServerError` |
| `EventId 5100` | requête terminée lorsque la journalisation exhaustive est activée |
| `EventId 5101` | requête plus lente que le seuil configuré |
| `EventId 5102` | erreur serveur |

Le modèle de route, par exemple `/parks/{parkId}`, remplace le chemin concret.
Lorsque le routage ne fournit pas de modèle, le sanitizer existant masque les
jetons d'invitation. Le trace ID reste disponible pour corréler une erreur avec les
Problem Details sans exposer l'identifiant métier, la query string ou le
user-agent.

## 6. Reproduction

Depuis `FRONT/AmusementPark` :

```bash
npm run test:performance-tools
npm run build -- --configuration production
npm run performance:bundle
npm run performance:baseline
```

`PERFORMANCE_BASE_URL` permet de viser un environnement différent. Le sondeur ne
fait jamais partie de la CI afin que la disponibilité d'un service externe au job
ne bloque pas un commit. L'option `--enforce` est réservée à un contrôle volontaire
des seuils réseau. Le contrôle de bundle, local et déterministe, bloque en revanche
chaque CI après le build de production.

## 7. Architecture, données et rollback

- Le middleware HTTP reste dans WebAPI ; aucune règle métier ne migre dans la
  couche de transport.
- Les outils de build et de sonde restent dans le frontend et n'entrent dans aucun
  bundle navigateur.
- Aucun package, écran, endpoint, collection MongoDB ou migration n'est ajouté.
- Le contrôle est compatible avec la règle une classe par fichier.
- Un rollback retire les nouveaux contrôles CI et restaure l'ancien format de log ;
  aucune donnée n'est à convertir ou supprimer.

## 8. Suites explicites

1. `QUAL-05` doit faire converger les événements produit vers les helpers typés,
   sans confondre analytics et métriques techniques.
2. `QUAL-06` doit agréger latences, erreurs et signaux SSR dans des tableaux de
   pilotage répondant à des questions opérationnelles précises.
3. `QUAL-09` doit définir les seuils d'alerte, la fenêtre, le propriétaire et le
   runbook associés.
4. Le repli CSR de Park Fit et la taille totale des chunks restent des axes de
   réduction mesurés, pas des incidents déclarés sans preuve d'impact utilisateur.
