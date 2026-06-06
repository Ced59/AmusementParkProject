# M21-M22 — SEO avancé, sitemap dynamique et IndexNow

## Objectif

Cette brique remplace le sitemap seed par une génération backend maîtrisée, pilotable depuis l'administration, avec :

- un sitemap index public à `/sitemap.xml` ;
- des sitemaps sectionnés par type **et par langue** sous `/sitemaps/*.xml` ;
- une page admin `Administration > SEO & sitemaps` ;
- un historique de génération ;
- des statistiques par section ;
- une intégration IndexNow/Bing désactivée par défaut et pilotable depuis l'admin.

La génération reste volontairement côté backend : le sitemap doit refléter les règles de publication métier, pas l'état du build Angular.

## Organisation publique des sitemaps

| URL | Contenu |
| --- | --- |
| `/sitemap.xml` | Sitemap index listant les sitemaps sectionnés. |
| `/sitemaps/static-fr.xml`, `/sitemaps/static-en.xml`, etc. | Pages publiques statiques de la langue concernée : home, parks, about, privacy. |
| `/sitemaps/parks-fr.xml`, `/sitemaps/parks-en.xml`, etc. | Pages publiques de parcs visibles dans la langue concernée : détail parc + liste des éléments du parc. |
| `/sitemaps/park-items-fr.xml`, `/sitemaps/park-items-en.xml`, etc. | Pages publiques des éléments visibles rattachés à un parc visible dans la langue concernée. |
| `/sitemaps/references-fr.xml`, `/sitemaps/references-en.xml`, etc. | Références publiques dans la langue concernée : exploitants, fondateurs, constructeurs. |
| `/robots.txt` | Référence `/sitemap.xml` et bloque les chemins admin/auth/account. |
| `/{indexNowKey}.txt` | Fichier de preuve IndexNow si IndexNow est activé et configuré. |

## Règles d'inclusion

Une URL n'est incluse que si elle est publique et éditorialement exploitable.

- Les parcs doivent être visibles, avoir un nom, un identifiant, et ne pas être marqués `NotRelevant`.
- Les park items doivent être visibles, avoir un nom, un identifiant, un parc parent visible, et ne pas être marqués `NotRelevant`.
- Les exploitants et constructeurs marqués `NotRelevant` sont exclus.
- Les pages admin, profil, auth, reset password et chemins techniques ne sont jamais inclus.
- Les URL sont générées pour les langues configurées dans `Seo:SupportedLanguages`.
- Chaque fichier sitemap ne contient qu'une seule langue, afin de garder un découpage lisible et de faciliter les diagnostics Search Console/Bing Webmaster Tools par langue.
- Les fichiers vides ne sont pas ajoutés au sitemap index.

## Génération et persistance

La génération est orchestrée par `SeoSitemapGenerationOrchestrator` :

1. collecte des URLs par provider (`ISitemapSectionProvider`) ;
2. déduplication par chemin ;
3. découpage des URLs par langue configurée ;
4. écriture XML des sections type + langue ;
5. écriture du sitemap index ;
6. persistance du snapshot courant ;
7. écriture d'une entrée d'historique ;
8. soumission IndexNow optionnelle.

Le dernier snapshot est stocké dans MongoDB dans la collection `seoSitemapSnapshots`.
L'historique est stocké dans `seoSitemapGenerationHistory`.
Les réglages admin sont stockés dans `seoSitemapSettings`.

## SEO métier front

La brique front ajoute aussi les éléments M21 suivants :

- `SeoRouteData` centralisé ;
- title et meta description ;
- canonical ;
- robots `index/noindex` selon route publique, admin, compte ou 404 ;
- alternate `hreflang` + `x-default` ;
- Open Graph : `og:site_name`, `og:title`, `og:description`, `og:url`, `og:type`, `og:locale`, `og:image` ;
- Twitter Cards : `twitter:card`, `twitter:title`, `twitter:description`, `twitter:image` ;
- JSON-LD `BreadcrumbList` ;
- JSON-LD métier prudent `AmusementPark` pour les pages parc et `TouristAttraction` pour les pages park item.

Les données structurées ne sont générées que depuis les données déjà présentes dans les view models publics. Aucune propriété métier sensible ou incertaine n'est inventée.

## Page admin

Route front :

```txt
/{lang}/admin/seo-sitemaps
```

Endpoints API protégés admin :

```txt
GET  /admin/seo/sitemaps/overview
GET  /admin/seo/sitemaps/settings
PUT  /admin/seo/sitemaps/settings
POST /admin/seo/sitemaps/generate
GET  /admin/seo/sitemaps/history?page=1&size=20
```

La page affiche :

- statut runtime de génération ;
- dernière génération ;
- total d'URLs ;
- stats par section ;
- liens publics vers les sitemaps ;
- réglages IndexNow ;
- historique des générations ;
- bouton de génération avec ou sans IndexNow.

## IndexNow / Bing

IndexNow est désactivé par défaut.

Réglages pilotables depuis l'admin :

- activation globale ;
- soumission après génération manuelle ;
- soumission après génération automatique ;
- clé IndexNow ;
- emplacement du fichier de clé ;
- endpoints utilisés.

Endpoints par défaut :

```txt
https://api.indexnow.org/indexnow
https://www.bing.com/indexnow
```

Quand la clé est configurée et IndexNow activé, l'API expose automatiquement le fichier texte `/{key}.txt` à la racine publique. Le contenu du fichier est la clé elle-même.

## Extension future

Pour ajouter un nouveau type de sitemap, créer un nouveau provider :

```csharp
public sealed class CountriesSitemapSectionProvider : ISitemapSectionProvider
{
    public string Key => "countries";
    public string FileName => "countries.xml";
    public string DisplayName => "Pays";

    public Task<IReadOnlyCollection<SitemapUrlEntry>> GetUrlsAsync(
        SitemapGenerationContext context,
        CancellationToken cancellationToken)
    {
        // Retourner uniquement les URLs publiques validées.
    }
}
```

Puis l'enregistrer dans `ApplicationServiceCollectionExtensions`.
