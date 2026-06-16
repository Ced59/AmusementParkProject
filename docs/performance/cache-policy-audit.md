# Audit de la politique de cache — AmusementPark

> Portée : inventaire de **toutes** les couches de cache de l'application, identification des
> incohérences, et explication de la cause racine du « flicker » observé après une mise à jour
> admin (les infos fraîches s'affichent 1–2 s puis reviennent à l'état pré‑maj).
>
> **Statut** : les correctifs **A, B, C, D et F sont implémentés** dans ce livrable (voir
> §7 pour le détail fichier par fichier). Le correctif **E** concerne le reverse‑proxy edge
> (Nginx Proxy Manager / openresty), configuré **hors dépôt** : il est fourni sous forme de
> snippet à appliquer côté NPM (§7). Le réchauffage SEO de `warmup-ssr-cache.sh` (robots.txt,
> sitemap index et **toutes** les sections) reste en place comme protection cold‑start Googlebot.
>
> ⚠️ Code non compilé/non testé dans cet environnement : builder l'API (`dotnet build`) et le
> front (`npm ci && npm run build`), exécuter les tests, et vérifier en DevTools l'absence de
> re‑fetch `/api/parks/{id}/detail-summary` à l'hydratation avant déploiement.

---

## 1. Résumé exécutif

Le symptôme « frais → revient au périmé » n'est pas un bug isolé : c'est la conséquence directe
de **deux mécanismes** qui se combinent, sur fond de **caches non coordonnés**.

1. **Le transfer‑cache HTTP d'Angular ne matche pas entre SSR et client.** En SSR, l'URL d'API est
   réécrite en absolu interne (`http://api:8080/...`) ; côté navigateur elle reste relative
   (`/api/...`). Les clés diffèrent → la donnée transférée par le SSR n'est **jamais réutilisée**,
   et le composant **re‑fetch** à l'hydratation.
2. **Ce re‑fetch est servi périmé par le navigateur.** Les endpoints publics renvoient
   `Cache-Control: public, max-age=60, s-maxage=300, stale-while-revalidate=300`. Le
   `stale-while-revalidate` autorise le navigateur à **afficher immédiatement la copie périmée**
   d'une visite précédente, qui écrase le rendu SSR frais → le flicker.

S'y ajoutent des incohérences de fraîcheur structurelles : l'**OutputCache de l'API n'est jamais
évincé sur une écriture de parc**, le **page cache SSR a un TTL de 24 h sans éviction**, et un
**cache edge (openresty/NPM) sert ~4 h** alors que l'origine annonce 5 min.

C'est exactement l'intuition formulée : la donnée fraîche est bien récupérée et transmise, mais
une couche de cache « reprend le dessus » derrière. L'intérêt du cache n'est pas en cause ; c'est
le **manque de coordination** (TTL hétérogènes + absence d'invalidation sur écriture) qui crée
l'incohérence.

---

## 2. Inventaire des couches de cache

| # | Couche | Localisation | Portée | TTL / durée | Clé | Invalidation sur écriture |
|---|--------|--------------|--------|-------------|-----|----------------------------|
| 1 | Cache HTTP navigateur | `Cache-Control` posé par l'API (`PublicHttpCacheHeadersMiddleware`/`Filter`) et par le SSR | par client | `/parks*` : `max-age=60, s-maxage=300, swr=300` ; SEO docs : `max-age=600, s-maxage=3600, swr=3600` ; pages SSR : `max-age=60, swr=600` | URL + `Vary: Accept-Language` | impossible (détenu par le client) |
| 2 | Transfer‑cache Angular | `provideClientHydration()` (défaut, transfer‑cache activé) | par chargement de page | le temps de l'hydratation | méthode + **URL** + params | n/a — **cassé** (cf. §3) |
| 3 | Page cache SSR | `server.ts` (`pageCache` mémoire **+ disque**) | par URL publique | `SSR_PAGE_CACHE_SECONDS=86400` (**24 h**), 2000 entrées, disque 4 Go | `proto://host + originalUrl` | **aucune** |
| 4 | Cache SEO SSR | `server.ts` (`seoDocumentCache`) | robots/sitemap | `SSR_SEO_DOCUMENT_CACHE_SECONDS=0` (pas d'expiration temporelle) | **chemin uniquement** ; supprime `Vary`, réécrit `Cache-Control` selon `SSR_SEO_DOCUMENT_BROWSER_CACHE_CONTROL` | invalidation SEO |
| 5 | OutputCache API (.NET) | `AddApiOutputCaching` | endpoints GET anonymes | `PublicSeoDocuments` 6 h ; `PublicDataShort` 5 min ; `PublicDataMedium` 30 min ; `PublicReferenceData` 6 h | `Host, X-Forwarded-Host, X-Forwarded-Proto (+ Accept-Language pour data)` + query | **SEO uniquement** (`EvictByTagAsync(PublicSeoTag)` dans `AdminSeoSitemapsController`). `PublicDataTag` / `PublicReferenceDataTag` : **jamais évincés** |
| 6 | Cache edge openresty / NPM | reverse‑proxy frontal | par URL | observé `age ≈ 4 h` (≠ `max-age=300` origine) | config NPM | purge manuelle / au déploiement uniquement |

Note : pas de service worker (vérifié — aucune trace `provideServiceWorker`/`ngsw`), ce qui retire
une couche de complexité potentielle.

---

## 3. Cause racine du flicker (détail)

### 3.1 Le transfer‑cache rate à cause de la réécriture d'URL

`environment.prod.ts` :

```
apiBaseUrl:    '/api/'              // utilisé côté navigateur
ssrApiBaseUrl: 'http://api:8080/'  // utilisé côté SSR
```

`ServerApiBaseUrlInterceptor` (server‑only) réécrit, pendant le SSR, `/api/...` →
`http://api:8080/...`. Le transfer‑cache d'Angular sérialise la réponse **sous la clé de l'URL
réellement émise**, c'est‑à‑dire l'**absolue interne**. À l'hydratation, le navigateur émet la même
requête en **relatif** (`/api/parks/{id}/detail-summary`) : la clé ne correspond pas, le
transfer‑cache **manque**, et `ParkDetailStateFacade.loadPark()` part en réseau.

> `ParkDetailPageComponent.ngOnInit` s'abonne à `route.paramMap` (qui émet immédiatement) et
> appelle `loadPark(id)`. Ce code s'exécute aussi côté client à l'hydratation → re‑fetch
> systématique tant que le transfer‑cache ne couvre pas la requête.

### 3.2 Le re‑fetch est servi périmé

`/parks/{id}/detail-summary` correspond à la règle `/parks` de `PublicHttpCacheHeadersMiddleware` :
`public, max-age=60, s-maxage=300, stale-while-revalidate=300`.

Si l'utilisateur a consulté la page parc **avant** la mise à jour (ce qui est le scénario typique :
on édite puis on va voir la page), le navigateur détient une copie. Au re‑fetch d'hydratation, le
`stale-while-revalidate` lui fait **servir cette copie périmée immédiatement** (revalidation en
arrière‑plan) → le DOM frais rendu par le SSR est remplacé par le périmé. C'est le flash observé.

« Vider le cache front » supprime la copie périmée → plus de retour en arrière. « Pas toujours »
car les couches **3, 5 et 6** peuvent aussi être périmées (cf. §4).

### 3.3 Confirmation en 30 s

DevTools → onglet **Réseau**, charger une page parc. Si une requête
`/api/parks/{id}/detail-summary` part **côté client** au chargement (en plus du SSR), le
transfer‑cache rate. Regarder son entête `Cache-Control` et la mention `(disk cache)` / `from
cache` : si elle revient du cache navigateur, c'est la source du flicker.

---

## 4. Incohérences identifiées

1. **Transfer‑cache inopérant** (clé absolue SSR vs relative client) → re‑fetch à chaque
   hydratation. Annule l'intérêt du transfer‑cache et ouvre la porte au flicker.
2. **`stale-while-revalidate` côté navigateur sur du contenu éditable** (`/parks*`) → la copie
   périmée est rendue instantanément côté client. Déclencheur direct du flash.
3. **OutputCache « data » jamais évincé sur écriture** → après une maj admin, l'API peut servir le
   parc périmé pendant **5 à 30 min** (`PublicDataShort`/`PublicDataMedium`). Seul le tag SEO est
   évincé.
4. **Page cache SSR à 24 h sans éviction** → le HTML d'une page parc peut rester périmé jusqu'à
   **24 h** après une modification, mémoire **et disque**.
5. **Cache edge openresty ~4 h non corrélé à l'origine** (`age=14652` observé alors que
   `max-age=300`) → contenu périmé jusqu'à ~4 h, et **masque** les régénérations (sitemap inclus).
6. **Désalignement SEO** : cache SEO SSR (1 h) < OutputCache SEO API (6 h) → multiplie les miss à
   froid sur les documents SEO (contribue au cold‑start lent de Googlebot).
7. **`Vary` supprimé** sur les documents SEO par le cache SSR : sans danger tant que ces documents
   sont mono‑variant, mais fragilise toute mutualisation par un proxy en amont.

---

## 5. Recommandations (par effet décroissant) — non implémentées

> Principe directeur : **deux régimes**. Contenu *éditable* (détail parc) → fraîcheur prioritaire,
> caches courts et évincés sur écriture. Contenu *stable* (documents SEO, référentiels) → caches
> longs, évincés explicitement à la (re)génération.

### A. Réparer le transfer‑cache (supprime le re‑fetch, donc le flicker à la source)
Faire en sorte que l'URL vue par le transfer‑cache soit **identique** en SSR et côté client.
Options : router l'appel API interne sans modifier `request.url` (garder le path `/api/...` et
résoudre l'origine interne au niveau transport), ou configurer `withHttpTransferCache({...})` avec
une clé stable. Objectif : la donnée récupérée au SSR est **réutilisée** à l'hydratation, sans
second appel réseau.

### B. Retirer le SWR client sur le contenu éditable
Pour `/parks/{id}/detail-summary` (et endpoints « fraîcheur sensible »), retirer
`stale-while-revalidate` côté navigateur — voire passer en `no-cache` / `max-age=0,
must-revalidate` — tout en conservant éventuellement un `s-maxage` pour le CDN. Cela empêche le
navigateur d'écraser un rendu frais par une copie périmée.

### C. Évincer l'OutputCache API sur écriture
À la sauvegarde d'un parc / d'une référence, appeler `EvictByTagAsync(PublicDataTag)` /
`EvictByTagAsync(PublicReferenceDataTag)`, comme c'est déjà fait pour `PublicSeoTag`. Respecter la
clean archi : exposer un **port d'invalidation** (Application) déclenché depuis le command handler,
l'implémentation Infrastructure/WebAPI s'appuyant sur `IOutputCacheStore`.

### D. Évincer (ou raccourcir) le page cache SSR sur écriture
Prévoir une invalidation ciblée par URL de parc lors d'une maj (signal/endpoint interne déclenché
après éviction OutputCache), ou réduire fortement le TTL des routes détail (s'appuyer sur le
warmup + un SWR court). 24 h sans éviction est inadapté à du contenu éditable.

### E. Maîtriser le cache edge openresty
Aligner sa durée sur les TTL d'origine (respecter `max-age`/`s-maxage`) **ou** ajouter une purge
ciblée au déploiement et après chaque maj/régénération. Le `age` à ~4 h vs `max-age=300` indique un
`proxy_cache` qui ignore l'origine.

### F. Aligner et purger les caches SEO
Conserver les documents SEO en cache serveur jusqu'a invalidation explicite, et **purger les
deux + l'edge** après chaque régénération de sitemap (en complément du warmup déjà câblé).

---

## 6. Chaîne de fraîcheur cible (vue d'ensemble)

```
Écriture admin (parc)
   └─ évince OutputCache data (C)         → API fraîche immédiatement
   └─ évince page cache SSR de l'URL (D)  → HTML SSR frais au prochain hit
   └─ purge edge de l'URL (E)             → edge frais
Rendu public
   └─ SSR rend frais (caches amont évincés)
   └─ transfer‑cache réutilisé (A)        → pas de re‑fetch client
   └─ pas de SWR client sur le détail (B) → pas d'écrasement par du périmé
```

Une fois A+B en place, le flicker disparaît ; C+D+E+F suppriment les fenêtres de péremption
résiduelles après une mise à jour.

---

## 7. Statut d'implémentation (ce livrable)

### A — Transfer‑cache réparé ✅
- `FRONT/AmusementPark/src/app/core/http/backends/server-api-base-url.backend.ts` (nouveau) :
  `ServerApiBaseUrlBackend` (HttpBackend) réécrit l'URL vers l'origine interne **au niveau
  transport**, donc **après** le calcul de la clé du transfer‑cache. La clé reste `/api/...`,
  identique SSR/navigateur → la donnée SSR est réutilisée à l'hydratation (plus de re‑fetch).
- `app.config.server.ts` : fournit `{ provide: HttpBackend, useClass: ServerApiBaseUrlBackend }`
  et retire l'intercepteur SSR.
- Supprimés : `core/http/interceptors/server-api-base-url.interceptor.ts` (+ spec). Remplacés par
  `backends/server-api-base-url.backend.spec.ts` (fake écrit à la main).

### B — SWR retiré sur le contenu éditable ✅
- `PublicHttpCacheHeadersMiddleware.cs` (et `PublicHttpCacheHeadersFilter.cs` par cohérence) :
  `stale-while-revalidate` mis à 0 sur `/public-stats/home`, `/parks*`, `/park-zones`,
  `/park-items`, `/search`, `/countries`, `/attraction-manufacturers`, `/park-founders`,
  `/park-operators`. Conservé sur `/images` (immuable par id) et les documents SEO.

### C — Éviction OutputCache sur écriture ✅
- `OutputCaching/PublicCacheScope.cs`, `InvalidatesPublicCacheAttribute.cs`,
  `InvalidatePublicCachesFilter.cs` (nouveaux) : filtre global qui, sur une écriture réussie
  (POST/PUT/PATCH/DELETE) d'un contrôleur annoté, évince le tag OutputCache correspondant.
- Filtre enregistré globalement dans `HttpApiServiceCollectionExtensions.AddHttpApi`.
- Contrôleurs annotés : Data → Parks, ParkItems, ParkZones, Images, ParkGraphUpserts,
  LocalizedContent ; ReferenceData → ParkFounders, ParkOperators, AttractionManufacturers,
  AttractionAccessConditionTypes. (Countries n'a aucune écriture → non annoté.)

### D — Invalidation du cache de pages SSR sur écriture ✅
- `Application/Ports/ISsrPageCacheInvalidator.cs` (port).
- `Infrastructure/Services/Ssr/HttpSsrPageCacheInvalidator.cs` (+ `Configuration/Ssr/SsrSettings.cs`) :
  POST interne authentifié par jeton ; toute erreur est avalée (n'échoue jamais l'écriture métier).
  Enregistré dans `InfrastructureServiceCollectionExtensions` (settings + HttpClient nommé 3 s + service).
- `FRONT/AmusementPark/server.ts` : endpoint `POST /internal/cache/invalidate` (jeton
  `X-AmusementPark-Cache-Token`, 404 si non configuré) qui vide `pageCache` + `seoDocumentCache`
  + cache disque.
- Le filtre C déclenche aussi cette invalidation SSR.
- `compose.prod.yml` + `.env.production.example` : `SSR_CACHE_INVALIDATION_TOKEN`,
  `SSR_INTERNAL_BASE_URL`, `Ssr__InternalBaseUrl`, `Ssr__CacheInvalidationToken`.
- Note : l'invalidation est volontairement **globale** (vide tout le cache de pages). Les écritures
  admin sont rares et le warmup recharge les routes critiques ; correctness > efficacité.

### F — Alignement des caches SEO ✅
- `compose.prod.yml` + `.env.production.example` : `SSR_SEO_DOCUMENT_CACHE_SECONDS=0`, donc les documents SEO restent en cache mémoire jusqu'a l'invalidation SEO déclenchée par une nouvelle génération.

### E — Cache edge openresty / NPM (à appliquer côté Nginx Proxy Manager, hors dépôt)
Le proxy frontal sert ~4 h (`age` observé) alors que l'origine annonce `s-maxage=300`. Aligner
l'edge sur l'origine (ou désactiver son cache et laisser l'origine décider). Dans la config
*Advanced* de l'hôte NPM :

```nginx
# Respecter le Cache-Control de l'origine (ne pas prolonger au-delà de s-maxage).
proxy_cache_revalidate on;
proxy_cache_use_stale off;
# Ne pas ignorer les directives de fraîcheur émises par l'API/SSR.
proxy_ignore_headers off;

# Purge ciblée d'une page après mise à jour (nécessite le module ngx_cache_purge) :
# location ~ /purge(/.*) {
#   allow 127.0.0.1; allow <réseau interne>; deny all;
#   proxy_cache_purge ssr_cache $scheme$request_method$host$1;
# }
```

À défaut de purge, purger le cache NPM au déploiement reste acceptable, mais l'invalidation
fine (D) ne traversera pas l'edge tant que celui‑ci sert du `stale`.
