# M18.4 — Content Security Policy en Report-Only

## C'est quoi une CSP ?

Une Content Security Policy est un en-tête HTTP envoyé au navigateur pour limiter ce qu'une page a le droit de charger ou d'exécuter.

Exemples de règles :

- scripts autorisés uniquement depuis le site, Google Identity et Matomo ;
- styles autorisés depuis le site ;
- images autorisées depuis le site, l'API images, les données inline nécessaires et les domaines HTTPS ;
- iframes autorisées uniquement pour Google Identity ;
- objets/plugins interdits ;
- intégration en iframe du site interdite via `frame-ancestors 'none'`.

L'objectif est de réduire l'impact d'une injection XSS ou d'un chargement externe inattendu.

## Pourquoi Report-Only maintenant ?

M18.4 active volontairement `Content-Security-Policy-Report-Only`, pas encore `Content-Security-Policy`.

En Report-Only :

- le navigateur n'empêche pas encore le chargement ;
- il envoie un rapport lorsqu'une ressource violerait la politique ;
- on peut ajuster la politique sans casser l'authentification Google, Matomo, Leaflet/OpenStreetMap, les images ou Angular.

Le passage en blocage réel est prévu à M18.5, après analyse des rapports.

## Où la politique est-elle appliquée ?

### Front Nginx

Les pages Angular servies par le container front reçoivent :

```http
Content-Security-Policy-Report-Only: ...; report-uri /api/security/csp-report
```

Le fichier concerné est :

- `FRONT/AmusementPark/nginx/snippets/content-security-policy-report-only.conf`

Les rapports arrivent donc sur le domaine public via `/api/security/csp-report`, puis sont proxifiés vers l'API.

### API ASP.NET Core

L'API dispose aussi d'une configuration CSP :

- `Security:ContentSecurityPolicy:Enabled`
- `Security:ContentSecurityPolicy:ReportOnly`
- `Security:ContentSecurityPolicy:ReportUri`
- `Security:ContentSecurityPolicy:Directives`

Les fichiers concernés sont :

- `API/AmusementPark.WebAPI/Configuration/ContentSecurityPolicySettings.cs`
- `API/AmusementPark.WebAPI/Security/ContentSecurityPolicyHeaderBuilder.cs`
- `API/AmusementPark.WebAPI/DependencyInjection/ContentSecurityPolicyServiceCollectionExtensions.cs`
- `API/AmusementPark.WebAPI/DependencyInjection/ContentSecurityPolicyApplicationBuilderExtensions.cs`

Quand l'API est appelée derrière le front Nginx, les headers CSP API sont masqués sur `/api/` pour éviter les doublons. Le front reste la source principale de CSP pour les pages publiques.

## Endpoint de collecte des rapports

Endpoint anonyme volontairement exposé :

```http
POST /security/csp-report
```

Depuis le domaine public, il est atteint via :

```http
POST /api/security/csp-report
```

Ce endpoint ne modifie aucune donnée métier. Il loggue uniquement un résumé technique : document concerné, directive violée, ressource bloquée, fichier source, ligne, IP et user-agent.

Le corps de requête est limité à 16 Ko.

## Sources autorisées au départ

La politique initiale est volontairement pragmatique pour ne pas casser la MVP :

| Usage | Sources prévues |
|---|---|
| Application Angular | `'self'` |
| Google Identity | `https://accounts.google.com`, `https://apis.google.com` |
| Matomo | `https://matomo.cedric-caudron.com` |
| Polices locales | `'self'`, `data:` |
| Images | `'self'`, `data:`, `blob:`, `https:`, `http://localhost:*` |
| API / appels XHR | `'self'`, localhost dev, Google Identity, Matomo |
| Frames | `'self'`, Google Identity |
| Workers | `'self'`, `blob:` |

`'unsafe-inline'` reste temporairement autorisé pour les scripts/styles afin d'éviter une casse immédiate avec Angular, PrimeNG, Google Identity ou certains styles runtime. Il faudra le réduire plus tard si le site peut passer à des nonces/hashes.

## Test local possible

Oui, mais avec une nuance importante.

### Via `ng serve`

`ng serve` ne sert pas la configuration Nginx du container front. Il ne permet donc pas de valider l'en-tête CSP front réel.

Ce que l'on peut vérifier quand même :

- l'endpoint API `POST /security/csp-report` ;
- les headers API si l'API est appelée directement ;
- les logs générés par un rapport manuel.

Exemple de test manuel :

```bash
curl -k -X POST https://localhost:44391/security/csp-report \
  -H "Content-Type: application/csp-report" \
  --data '{"csp-report":{"document-uri":"https://localhost/test","violated-directive":"script-src","effective-directive":"script-src","blocked-uri":"https://example.invalid/script.js","source-file":"https://localhost/main.js","line-number":12}}'
```

### Via Docker front local

Pour tester le vrai header front, il faut lancer le front construit dans son container Nginx, ou un équivalent local qui utilise `FRONT/AmusementPark/nginx/default.conf`.

Contrôle attendu :

```bash
curl -I http://127.0.0.1:8080/
```

La réponse doit contenir :

```http
Content-Security-Policy-Report-Only: ...
```

Ensuite, ouvrir les pages clés dans le navigateur et surveiller les logs API :

- home ;
- liste parcs ;
- détail parc ;
- détail attraction/item ;
- login Google ;
- carte Leaflet ;
- pages avec images API ;
- bannière consentement / Matomo.

## Critères de validation M18.4

- Les pages publiques servent une CSP Report-Only.
- La navigation Angular n'est pas cassée.
- Google Identity n'est pas cassé.
- Leaflet/OpenStreetMap n'est pas cassé.
- Les images API continuent de s'afficher.
- Matomo ne génère pas de violation bloquante inattendue après consentement.
- Les violations éventuelles apparaissent dans les logs via `SecurityReportsController`.

## Passage M18.5 — à reprendre impérativement après déploiement réel

M18.5 ne doit pas être oublié. Il est simplement différé tant que le site n'est pas testé sur le vrai chemin de production : domaine HTTPS, Nginx Proxy Manager, container front Nginx, API derrière `/api`, Google OAuth, Matomo, Leaflet/OpenStreetMap et images API.

À la première mise en ligne staging/prod, garder `CSP_REPORT_ONLY=true`, parcourir les pages clés, analyser les rapports CSP, puis planifier le passage en mode enforce.

Avant d'activer le mode enforce :

1. collecter les rapports en local/staging ;
2. supprimer les sources inutiles ;
3. ajouter uniquement les sources réellement nécessaires ;
4. vérifier que les rapports restants ne correspondent pas à des fonctionnalités légitimes ;
5. remplacer `Content-Security-Policy-Report-Only` par `Content-Security-Policy`.
