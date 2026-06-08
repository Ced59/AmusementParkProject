# Cache front des documents SEO publics — 2026-06-08

## Problème constaté

Les documents SEO publics (`/sitemap.xml`, `/sitemaps/*.xml`, `/robots.txt`) étaient bien servis depuis le snapshot sitemap côté API, mais chaque requête publique passait encore par le front SSR puis l'API.

Même sans régénération complète du sitemap, un test de charge simple sur `/sitemap.xml` pouvait donc faire monter fortement le CPU de `amusementpark-api`.

## Correction

Le serveur SSR Express conserve maintenant un cache mémoire dédié aux documents SEO publics :

- `/robots.txt`
- `/sitemap.xml`
- `/sitemaps/:fileName`
- `/:fileName.txt` pour le fichier clé IndexNow

Le premier appel lit l'API et remplit le cache. Les appels suivants sont servis directement par le front SSR depuis un `Buffer` mémoire, sans nouvel appel API.

Le cache fonctionne pour `GET` et `HEAD`. Un `HEAD` froid remplit aussi le cache en lisant le document côté API en interne, puis renvoie seulement les en-têtes au client.

## Configuration

Variables front SSR :

```env
SSR_SEO_DOCUMENT_CACHE_SECONDS=3600
SSR_SEO_DOCUMENT_CACHE_MAX_ENTRIES=128
```

Pour désactiver ce cache :

```env
SSR_SEO_DOCUMENT_CACHE_SECONDS=0
```

## Invalidation

Le sitemap reste un snapshot généré depuis l'administration. La consultation publique ne régénère pas le sitemap.

Après une régénération admin, le cache front expirera automatiquement selon `SSR_SEO_DOCUMENT_CACHE_SECONDS`. Pour une prise en compte immédiate après une grosse régénération, redémarrer uniquement le container front vide aussi ce cache mémoire :

```bash
docker restart amusementpark-front
```

## Vérification

```bash
curl -I https://amusement-parks.fun/sitemap.xml
curl -I https://amusement-parks.fun/sitemap.xml
```

Le premier appel peut afficher :

```http
X-AmusementPark-SEO-Cache: MISS
```

Le second doit afficher :

```http
X-AmusementPark-SEO-Cache: HIT
```

Test de charge léger :

```bash
for i in {1..50}; do
  curl -sS -o /dev/null https://amusement-parks.fun/sitemap.xml
done
```

L'API ne doit plus monter fortement en CPU pour ce test une fois le cache chaud.
