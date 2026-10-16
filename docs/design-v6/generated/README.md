# Inventaire P00 généré

- Source : 9492817ac24d2436f257ddf38661d8aed3ea6097 ; source UI inchangée dans la correction de scénario compte/public
- Routes terminales, redirections et écrans développés : **148**
- Composants Angular de présentation recensés : **293**
- Interactions et champs explicitement recensés : **2057**
- Feuilles SCSS globales et locales recensées : **268**
- Blocs de styles inline recensés : **2**
- Templates de composants manquants : **0**

## Routes par famille

| Famille | Nombre |
|---|---:|
| account | 26 |
| admin | 58 |
| layout | 2 |
| park | 19 |
| parkitem | 10 |
| public | 29 |
| root | 1 |
| standalone | 3 |

## Composants par périmètre

| Périmètre | Nombre |
|---|---:|
| app | 3 |
| features/admin | 84 |
| features/auth | 6 |
| features/collections | 2 |
| features/profile | 41 |
| features/public | 83 |
| features/watchlists | 5 |
| shared | 39 |
| ui | 30 |

Les CSV voisins sont la source exhaustive. Ils doivent être régénérés après chaque intégration de `origin/master` et avant P22.
Les conditions et événements sont des preuves de source Angular, pas des scénarios exécutés ni des validations fonctionnelles.
Les composants de tests (`test-helpers`, `testing`, `fixtures`, `__tests__`, `*.spec.ts`) sont exclus du périmètre de production.
