# Guide reproductible d’audit SEO de production

Ce guide décrit comment auditer de façon exhaustive le SEO public, le crawl, le
rendu SSR, les caches et les dépendances d’exploitation d’AmusementPark. Il
remplace les audits ponctuels contenant des résultats datés.

Le guide contient uniquement la méthode, les commandes, les éléments à vérifier
et les critères d’acceptation. Les résultats d’un audit doivent rester dans un
dossier temporaire ou dans un rapport daté séparé. Ils ne doivent jamais contenir
de secret, de clé, de jeton, de cookie, d’adresse privée ou de contenu de fichier
`.env`.

## 1. Principes de sécurité et de reproductibilité

Un audit standard est en lecture seule. Les actions qui purgent un cache,
déclenchent un warmup, redémarrent un conteneur, modifient une variable ou
soumettent un sitemap nécessitent une autorisation explicite et ne font pas
partie du parcours par défaut.

Sur la production :

- garder une concurrence de crawl à `1` ou `2` au maximum ;
- respecter au moins 2,1 secondes entre deux requêtes Ahrefs ;
- commencer par un échantillon d’une URL par famille avant d’élargir ;
- ne jamais lancer en parallèle un crawl large, un warmup et un déploiement ;
- arrêter le crawl si des `429`, `502`, `503`, OOM ou rejets de file apparaissent ;
- utiliser des identifiants et chemins fournis par variables, jamais des valeurs
  d’infrastructure écrites dans la documentation ;
- conserver l’heure UTC de début et de fin, la version déployée et le SHA Git ;
- ne pas confondre absence de résultat SEO et défaut technique de crawl.

Les commandes ci-dessous supposent PowerShell 7, Git, Node.js, `curl.exe` et
OpenSSH. Les commandes VPS supposent en plus un accès SSH autorisé. `jq` est
facultatif et n’est utilisé que pour rendre certains JSON plus lisibles.

## 2. Préparer la session

Exécuter depuis la racine du dépôt :

```powershell
$ErrorActionPreference = 'Stop'

$env:SEO_BASE_URL = 'https://amusement-parks.fun'
$env:SEO_EXPECTED_HOST = ([uri]$env:SEO_BASE_URL).Host
$env:SEO_SITEMAP_URL = "$($env:SEO_BASE_URL.TrimEnd('/'))/sitemap.xml"

# Facultatif : requis uniquement pour la partie VPS.
$env:SEO_SSH_TARGET = ''
$env:SEO_SSH_IDENTITY = ''

$auditStamp = (Get-Date).ToUniversalTime().ToString('yyyyMMddTHHmmssZ')
$auditDir = Join-Path ([IO.Path]::GetTempPath()) "amusementpark-seo-audit-$auditStamp"
New-Item -ItemType Directory -Path $auditDir | Out-Null
$nullDevice = if ($IsWindows) { 'NUL' } else { '/dev/null' }

"Audit directory: $auditDir"
```

Vérifier les outils et figer le contexte :

```powershell
git status --short --branch
git rev-parse HEAD
git log -1 --format='%H%n%cI%n%s'
node --version
curl.exe --version | Select-Object -First 1
ssh -V
Get-Date -AsUTC -Format o
```

Si la partie VPS est prévue, vérifier la configuration sans énumérer le contenu
du dossier SSH :

```powershell
if ([string]::IsNullOrWhiteSpace($env:SEO_SSH_TARGET)) {
    throw 'SEO_SSH_TARGET is required for VPS checks.'
}

if (-not (Test-Path -LiteralPath $env:SEO_SSH_IDENTITY)) {
    throw 'SEO_SSH_IDENTITY does not point to an existing identity file.'
}

ssh -o BatchMode=yes -o ConnectTimeout=10 `
    -i $env:SEO_SSH_IDENTITY `
    $env:SEO_SSH_TARGET `
    'whoami && hostname && date -u'
```

## 3. DNS, domaine canonique, TLS et redirections

Relever les entrées publiques :

```powershell
Resolve-DnsName $env:SEO_EXPECTED_HOST -Type A
Resolve-DnsName $env:SEO_EXPECTED_HOST -Type AAAA -ErrorAction SilentlyContinue
Resolve-DnsName "www.$($env:SEO_EXPECTED_HOST)" -Type A -ErrorAction SilentlyContinue
Resolve-DnsName "www.$($env:SEO_EXPECTED_HOST)" -Type AAAA -ErrorAction SilentlyContinue
Resolve-DnsName "www.$($env:SEO_EXPECTED_HOST)" -Type CNAME -ErrorAction SilentlyContinue
```

Vérifier les quatre entrées HTTP/HTTPS et leur chaîne de redirection :

```powershell
$originCandidates = @(
    "https://$($env:SEO_EXPECTED_HOST)/",
    "https://www.$($env:SEO_EXPECTED_HOST)/",
    "http://$($env:SEO_EXPECTED_HOST)/",
    "http://www.$($env:SEO_EXPECTED_HOST)/"
)

foreach ($url in $originCandidates) {
    "=== $url ==="
    curl.exe -sS -I -L --max-redirs 5 --max-time 30 $url
}
```

Points à contrôler :

- le domaine canonique répond en HTTPS ;
- HTTP redirige vers HTTPS ;
- `www` redirige définitivement vers le domaine canonique ;
- il n’existe ni boucle ni chaîne de redirections inutile ;
- aucune entrée AAAA cassée n’expose les robots à un chemin IPv6 défaillant ;
- le certificat couvre les hôtes servis et n’est ni expiré ni proche de
  l’expiration ;
- HSTS, CSP, `X-Content-Type-Options`, `Referrer-Policy` et
  `Permissions-Policy` sont présents selon la politique du projet ;
- les réponses ne divulguent pas de détail interne inutile.

Pour inspecter le certificat :

```powershell
'' |
    openssl s_client `
        -connect "$($env:SEO_EXPECTED_HOST):443" `
        -servername $env:SEO_EXPECTED_HOST `
        -showcerts 2>$null |
    openssl x509 -noout -subject -issuer -dates -ext subjectAltName
```

## 4. `robots.txt`

Récupérer le fichier avec un navigateur et les principaux robots de recherche :

```powershell
$robotAgents = [ordered]@{
    Browser = 'Mozilla/5.0'
    Googlebot = 'Mozilla/5.0 (compatible; Googlebot/2.1; +http://www.google.com/bot.html)'
    Bingbot = 'Mozilla/5.0 (compatible; bingbot/2.0; +http://www.bing.com/bingbot.htm)'
    YandexBot = 'Mozilla/5.0 (compatible; YandexBot/3.0; +http://yandex.com/bots)'
    AhrefsBot = 'Mozilla/5.0 (compatible; AhrefsBot/7.0; +http://ahrefs.com/robot/)'
    AhrefsSiteAudit = 'Mozilla/5.0 (compatible; AhrefsSiteAudit/6.1; +http://ahrefs.com/robot/site-audit)'
}

foreach ($entry in $robotAgents.GetEnumerator()) {
    $output = Join-Path $auditDir "robots-$($entry.Key).txt"
    curl.exe -sS --max-time 30 `
        -A $entry.Value `
        -D "$output.headers" `
        -o $output `
        "$($env:SEO_BASE_URL.TrimEnd('/'))/robots.txt"
}
```

Afficher les résultats utiles :

```powershell
Get-ChildItem -LiteralPath $auditDir -Filter 'robots-*.headers' |
    ForEach-Object {
        "=== $($_.Name) ==="
        Get-Content -LiteralPath $_.FullName |
            Select-String -Pattern 'HTTP/|content-type:|content-length:|cache-control:|age:'
    }

Get-Content -LiteralPath (Join-Path $auditDir 'robots-Googlebot.txt')
```

Points à contrôler :

- statut `200` et type `text/plain` ;
- une seule déclaration du sitemap canonique ;
- autorisation des pages publiques et des images publiques utiles ;
- exclusion de `/api/`, des routes admin, compte et authentification ;
- règles explicites et cohérentes pour Ahrefs ;
- robots de recherche, assistants et aperçus conformes à la politique du projet ;
- robots d’entraînement exclus selon la décision produit ;
- aucun chemin localisé sensible oublié ;
- aucun caractère, BOM ou HTML parasite.

Comparer la politique exposée avec la politique source :

```powershell
rg -n "RobotFamily|robotFamilyMatchers|coldRenderRobotFamilies|socialPreviewRobotFamilies" `
    FRONT/AmusementPark/src/server/ssr/robot-ssr-policy.ts
rg -n "robots.txt|User-agent|Disallow|Crawl-delay|Sitemap:" `
    API FRONT docs deploy
```

## 5. Sitemap principal et chemins historiques

Contrôler le sitemap canonique avec plusieurs agents :

```powershell
foreach ($entry in $robotAgents.GetEnumerator()) {
    "=== $($entry.Key) ==="
    curl.exe -sS -I -L --max-time 30 `
        -A $entry.Value `
        -H 'Accept: application/xml,text/xml;q=0.9,*/*;q=0.1' `
        -H 'Accept-Encoding: identity' `
        $env:SEO_SITEMAP_URL
}
```

Contrôler les variantes de domaine et de protocole :

```powershell
$sitemapCandidates = @(
    "https://$($env:SEO_EXPECTED_HOST)/sitemap.xml",
    "https://www.$($env:SEO_EXPECTED_HOST)/sitemap.xml",
    "http://$($env:SEO_EXPECTED_HOST)/sitemap.xml",
    "http://www.$($env:SEO_EXPECTED_HOST)/sitemap.xml"
)

foreach ($url in $sitemapCandidates) {
    "=== $url ==="
    curl.exe -sS -I -L --max-redirs 5 --max-time 30 $url
}
```

Contrôler les chemins historiques ou fréquemment mal soumis. Adapter la liste
si l’historique du projet évolue :

```powershell
$legacySitemapCandidates = @(
    '/sitemaps/sitemap.xml',
    '/sitemaps/static-en.xml',
    '/sitemaps/parks-fr.xml',
    '/sitemap-001.xml',
    '/sitemap-400.xml',
    '/fr/sitemap',
    '/park-zones-fr.xml'
)

foreach ($path in $legacySitemapCandidates) {
    $url = "$($env:SEO_BASE_URL.TrimEnd('/'))$path"
    "=== $url ==="
    curl.exe -sS -I --max-time 30 -A $robotAgents.Googlebot $url
}
```

Points à contrôler :

- sitemap canonique en `200` avec un type XML ;
- même contenu accessible aux robots autorisés ;
- redirections historiques intentionnelles vers le chemin canonique ;
- chemins inventés ou obsolètes en `404`, jamais en faux `200` XML ;
- `/fr/sitemap` identifié comme page HTML, pas comme sitemap XML ;
- politique de cache adaptée et contenu non tronqué.

## 6. Crawl exhaustif de tous les sitemaps XML

Le script PowerShell suivant télécharge l’index puis tous les sitemaps enfants,
parse réellement le XML et produit deux fichiers temporaires :

- `sitemap-audit.json` : synthèse et anomalies ;
- `sitemap-pages.txt` : toutes les URL publiques déclarées.

Il ne télécharge pas les pages HTML.

```powershell
$sitemapAuditScript = Join-Path $auditDir 'audit-sitemaps.ps1'

@'
param(
    [Parameter(Mandatory = $true)]
    [string]$SitemapUrl,
    [Parameter(Mandatory = $true)]
    [string]$ExpectedHost,
    [Parameter(Mandatory = $true)]
    [string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'
$userAgent = 'Mozilla/5.0 (compatible; Googlebot/2.1; +http://www.google.com/bot.html)'
$xmlNamespace = 'http://www.sitemaps.org/schemas/sitemap/0.9'
$maxUrlsPerSitemap = 50000
$maxUncompressedBytes = 50MB
$now = (Get-Date).ToUniversalTime()

function Get-XmlResponse([string]$Uri) {
    $response = Invoke-WebRequest `
        -Uri $Uri `
        -Method Get `
        -MaximumRedirection 5 `
        -TimeoutSec 60 `
        -UserAgent $userAgent `
        -Headers @{ Accept = 'application/xml,text/xml;q=0.9,*/*;q=0.1' }

    $contentType = [string]$response.Headers['Content-Type']
    if ($response.StatusCode -ne 200) {
        throw "$Uri returned HTTP $($response.StatusCode)."
    }
    if ($contentType -notmatch '(application|text)/(.+\+)?xml') {
        throw "$Uri returned unexpected Content-Type '$contentType'."
    }

    try {
        [xml]$xml = $response.Content
    }
    catch {
        throw "$Uri returned malformed XML: $($_.Exception.Message)"
    }

    [pscustomobject]@{
        Uri = $Uri
        Response = $response
        Xml = $xml
        Bytes = [Text.Encoding]::UTF8.GetByteCount([string]$response.Content)
    }
}

function Select-NamespacedNodes(
    [xml]$Xml,
    [string]$XPath
) {
    $namespaceManager = [Xml.XmlNamespaceManager]::new($Xml.NameTable)
    $namespaceManager.AddNamespace('sm', $xmlNamespace)
    @($Xml.SelectNodes($XPath, $namespaceManager))
}

$index = Get-XmlResponse $SitemapUrl
$sitemapNodes = Select-NamespacedNodes $index.Xml '//sm:sitemap/sm:loc'
$childUrls = @($sitemapNodes | ForEach-Object { $_.InnerText.Trim() })

$anomalies = [Collections.Generic.List[object]]::new()
$childSummaries = [Collections.Generic.List[object]]::new()
$allPageUrls = [Collections.Generic.List[string]]::new()
$allPageUrlSet = [Collections.Generic.HashSet[string]]::new(
    [StringComparer]::OrdinalIgnoreCase
)

if ($childUrls.Count -eq 0) {
    $anomalies.Add([pscustomobject]@{
        Kind = 'empty-index'
        Url = $SitemapUrl
        Detail = 'No child sitemap was found.'
    })
}

if (($childUrls | Sort-Object -Unique).Count -ne $childUrls.Count) {
    $anomalies.Add([pscustomobject]@{
        Kind = 'duplicate-child-sitemap'
        Url = $SitemapUrl
        Detail = 'The sitemap index contains duplicate child URLs.'
    })
}

foreach ($childUrl in $childUrls) {
    $childUri = [uri]$childUrl
    if ($childUri.Scheme -ne 'https' -or $childUri.Host -ne $ExpectedHost) {
        $anomalies.Add([pscustomobject]@{
            Kind = 'invalid-child-origin'
            Url = $childUrl
            Detail = 'Child sitemap must use HTTPS and the canonical host.'
        })
    }

    $child = Get-XmlResponse $childUrl
    $urlNodes = Select-NamespacedNodes $child.Xml '//sm:url'
    $namespaceManager = [Xml.XmlNamespaceManager]::new($child.Xml.NameTable)
    $namespaceManager.AddNamespace('sm', $xmlNamespace)
    $urls = @($urlNodes | ForEach-Object {
        $_.SelectSingleNode('sm:loc', $namespaceManager).InnerText.Trim()
    })
    $lastmods = @($urlNodes | ForEach-Object {
        $_.SelectSingleNode('sm:lastmod', $namespaceManager)
    })

    if ($urls.Count -gt $maxUrlsPerSitemap) {
        $anomalies.Add([pscustomobject]@{
            Kind = 'too-many-urls'
            Url = $childUrl
            Detail = "$($urls.Count) URLs exceed the protocol limit."
        })
    }
    if ($child.Bytes -gt $maxUncompressedBytes) {
        $anomalies.Add([pscustomobject]@{
            Kind = 'sitemap-too-large'
            Url = $childUrl
            Detail = "$($child.Bytes) bytes exceed the uncompressed limit."
        })
    }
    if (($urls | Sort-Object -Unique).Count -ne $urls.Count) {
        $anomalies.Add([pscustomobject]@{
            Kind = 'duplicate-url-in-child'
            Url = $childUrl
            Detail = 'The child sitemap contains duplicate URLs.'
        })
    }

    $missingLastmod = 0
    $invalidLastmod = 0
    foreach ($lastmodNode in $lastmods) {
        if ($null -eq $lastmodNode -or [string]::IsNullOrWhiteSpace($lastmodNode.InnerText)) {
            $missingLastmod++
            continue
        }

        $parsedLastmod = [datetimeoffset]::MinValue
        if (-not [datetimeoffset]::TryParse($lastmodNode.InnerText, [ref]$parsedLastmod)) {
            $invalidLastmod++
            continue
        }
        if ($parsedLastmod.UtcDateTime -gt $now.AddMinutes(5)) {
            $anomalies.Add([pscustomobject]@{
                Kind = 'future-lastmod'
                Url = $childUrl
                Detail = $lastmodNode.InnerText
            })
        }
    }

    foreach ($pageUrl in $urls) {
        $pageUri = [uri]$pageUrl
        if ($pageUri.Scheme -ne 'https' -or $pageUri.Host -ne $ExpectedHost) {
            $anomalies.Add([pscustomobject]@{
                Kind = 'invalid-page-origin'
                Url = $pageUrl
                Detail = 'Page URL must use HTTPS and the canonical host.'
            })
        }
        if (-not [string]::IsNullOrEmpty($pageUri.Query) -or
            -not [string]::IsNullOrEmpty($pageUri.Fragment)) {
            $anomalies.Add([pscustomobject]@{
                Kind = 'query-or-fragment'
                Url = $pageUrl
                Detail = 'Sitemap page URLs must not contain a query or fragment.'
            })
        }
        if (-not $allPageUrlSet.Add($pageUrl)) {
            $anomalies.Add([pscustomobject]@{
                Kind = 'duplicate-page-across-sitemaps'
                Url = $pageUrl
                Detail = $childUrl
            })
        }
        $allPageUrls.Add($pageUrl)
    }

    $childSummaries.Add([pscustomobject]@{
        Url = $childUrl
        Status = $child.Response.StatusCode
        Bytes = $child.Bytes
        UrlCount = $urls.Count
        MissingLastmod = $missingLastmod
        InvalidLastmod = $invalidLastmod
        SampleUrl = if ($urls.Count -gt 0) { $urls[0] } else { $null }
    })
}

$pagesPath = Join-Path $OutputDirectory 'sitemap-pages.txt'
$samplePath = Join-Path $OutputDirectory 'html-sample-one-per-sitemap.txt'
$reportPath = Join-Path $OutputDirectory 'sitemap-audit.json'

$allPageUrls | Set-Content -LiteralPath $pagesPath -Encoding utf8
$childSummaries |
    Where-Object { -not [string]::IsNullOrWhiteSpace($_.SampleUrl) } |
    ForEach-Object { $_.SampleUrl } |
    Set-Content -LiteralPath $samplePath -Encoding utf8

[pscustomobject]@{
    GeneratedAtUtc = (Get-Date).ToUniversalTime().ToString('o')
    SitemapUrl = $SitemapUrl
    IndexStatus = $index.Response.StatusCode
    IndexBytes = $index.Bytes
    ChildSitemapCount = $childUrls.Count
    PageUrlCount = $allPageUrls.Count
    UniquePageUrlCount = $allPageUrlSet.Count
    Children = $childSummaries
    Anomalies = $anomalies
} | ConvertTo-Json -Depth 8 |
    Set-Content -LiteralPath $reportPath -Encoding utf8

"Report: $reportPath"
"Pages: $pagesPath"
"Sample: $samplePath"
"Anomalies: $($anomalies.Count)"

if ($anomalies.Count -gt 0) {
    exit 1
}
'@ | Set-Content -LiteralPath $sitemapAuditScript -Encoding utf8

& $sitemapAuditScript `
    -SitemapUrl $env:SEO_SITEMAP_URL `
    -ExpectedHost $env:SEO_EXPECTED_HOST `
    -OutputDirectory $auditDir
```

Après exécution, contrôler aussi :

```powershell
$sitemapReport = Get-Content `
    -LiteralPath (Join-Path $auditDir 'sitemap-audit.json') `
    -Raw |
    ConvertFrom-Json

$sitemapReport |
    Select-Object IndexStatus, ChildSitemapCount, PageUrlCount, UniquePageUrlCount
$sitemapReport.Anomalies
$sitemapReport.Children |
    Sort-Object UrlCount -Descending |
    Select-Object -First 20 Url, UrlCount, Bytes, MissingLastmod, InvalidLastmod
```

Critères d’acceptation :

- index et tous les enfants en `200` XML ;
- XML bien formé ;
- aucun enfant ou URL en doublon ;
- aucune URL hors HTTPS ou hors hôte canonique ;
- aucune query string ni fragment ;
- limites protocolaires respectées ;
- `lastmod` valide lorsqu’il est présent, jamais dans le futur ;
- toutes les familles et langues attendues représentées ;
- volume expliqué par famille, sans croissance anormale ou boucle combinatoire.

## 7. Échantillonnage HTML et matrice de routes

L’audit doit couvrir au minimum une URL froide puis chaude pour chacune des
familles suivantes lorsqu’elles existent dans le sitemap :

1. page statique ;
2. parc ;
3. horaires de parc ;
4. historique de parc ;
5. article historique ;
6. images de parc ;
7. liste des lieux d’un parc ;
8. zone de parc ;
9. élément de parc ;
10. images d’un élément ;
11. vidéos d’un élément ;
12. référence publique ;
13. page technique publique ;
14. attraction autonome.

Il faut aussi couvrir :

- chaque langue réellement servie ;
- une page avec et sans image principale ;
- une page avec données riches et une page minimale ;
- une entité inexistante ;
- une route admin, profil et authentification ;
- un endpoint API protégé et une image inexistante ;
- une URL canonique et une variante de slug erronée ;
- une ancienne route disposant d’une redirection.

Le fichier généré à l’étape précédente contient une URL par sitemap enfant :

```powershell
Get-Content -LiteralPath (Join-Path $auditDir 'html-sample-one-per-sitemap.txt') |
    Measure-Object
```

Pour un premier passage, créer un fichier réduit à une URL représentative par
famille et par langue. Pour un audit large, conserver une URL par sitemap enfant.
Un crawl de toutes les URL doit être planifié séparément : avec un délai de deux
secondes, plusieurs dizaines de milliers d’URL demandent plus d’une journée.

## 8. Audit automatisé du HTML robot

Le script suivant prend le fichier d’URL produit plus haut, respecte un délai et
écrit un enregistrement JSON par réponse. Par défaut, utiliser uniquement
Googlebot sur l’échantillon large. Exécuter la matrice multi-agents sur les
quatorze familles, pas sur toutes les URL.

```powershell
$env:SEO_AUDIT_URL_FILE = Join-Path $auditDir 'html-sample-one-per-sitemap.txt'
$env:SEO_AUDIT_OUTPUT_FILE = Join-Path $auditDir 'html-googlebot.ndjson'
$env:SEO_AUDIT_DELAY_MS = '2100'
$env:SEO_AUDIT_USER_AGENTS = @(
    [pscustomobject]@{
        name = 'Googlebot'
        value = $robotAgents.Googlebot
        expectNoJs = $true
    }
) | ConvertTo-Json -Compress

@'
import { appendFileSync, readFileSync, writeFileSync } from 'node:fs';

const urlFile = process.env.SEO_AUDIT_URL_FILE;
const outputFile = process.env.SEO_AUDIT_OUTPUT_FILE;
const delayMs = Number.parseInt(process.env.SEO_AUDIT_DELAY_MS ?? '2100', 10);
const agents = JSON.parse(process.env.SEO_AUDIT_USER_AGENTS ?? '[]');
const urls = [...new Set(
  readFileSync(urlFile, 'utf8')
    .split(/\r?\n/)
    .map((value) => value.trim())
    .filter(Boolean)
)];

writeFileSync(outputFile, '', 'utf8');

for (const agent of agents) {
  for (const url of urls) {
    const startedAt = performance.now();
    let record;

    try {
      const response = await fetch(url, {
        redirect: 'manual',
        headers: {
          accept: 'text/html,application/xhtml+xml',
          'accept-encoding': 'identity',
          'cache-control': 'no-cache',
          'user-agent': agent.value
        },
        signal: AbortSignal.timeout(90000)
      });
      const html = await response.text();
      const elapsedMs = Math.round(performance.now() - startedAt);
      const title = matchText(html, /<title\b[^>]*>([\s\S]*?)<\/title>/i);
      const description = findMeta(html, 'name', 'description');
      const robots = findMeta(html, 'name', 'robots')?.toLowerCase() ?? '';
      const canonical = findLink(html, 'canonical');
      const hreflangs = findHreflangs(html);
      const ogTitle = findMeta(html, 'property', 'og:title');
      const ogDescription = findMeta(html, 'property', 'og:description');
      const ogUrl = findMeta(html, 'property', 'og:url');
      const ogLocale = findMeta(html, 'property', 'og:locale');
      const expectedOgLocale = resolveExpectedOpenGraphLocale(response.url);
      const h1Count = (html.match(/<h1\b/gi) ?? []).length;
      const invalidJsonLd = findJsonLd(html).filter((value) => !value.valid);
      const executableScripts = countExecutableScripts(html);
      const appRootHasContent = /<app-root\b[^>]*>[\s\S]*\S[\s\S]*<\/app-root>/i.test(html)
        && !/<app-root\b[^>]*>\s*<\/app-root>/i.test(html);
      const errors = [];
      const warnings = [];

      if (response.status !== 200) errors.push(`HTTP ${response.status}`);
      if (response.status >= 300 && response.status < 400) {
        errors.push(`unexpected redirect to ${response.headers.get('location') ?? '(missing location)'}`);
      }
      if (!response.headers.get('content-type')?.includes('text/html')) {
        errors.push('Content-Type is not HTML');
      }
      if (response.headers.get('x-amusementpark-seo-ready') !== 'true') {
        errors.push('SEO-ready header is not true');
      }
      if (!title || title.length < 8) errors.push('missing or short title');
      if (!description || description.length < 30) errors.push('missing or short description');
      if (!robots.includes('index') || robots.includes('noindex')) {
        errors.push(`unexpected robots meta: ${robots || '(missing)'}`);
      }
      if (!canonical) {
        errors.push('missing canonical');
      } else {
        const canonicalUrl = new URL(canonical, url);
        const finalPageUrl = new URL(response.url);
        if (canonicalUrl.href !== finalPageUrl.href) {
          errors.push(`canonical mismatch: ${canonicalUrl.href}`);
        }
        if (canonicalUrl.search || canonicalUrl.hash) errors.push('canonical has query or fragment');
      }
      if (!hreflangs.some((entry) => entry.language === 'x-default')) {
        errors.push('missing x-default hreflang');
      }
      if (new Set(hreflangs.map((entry) => entry.language)).size !== hreflangs.length) {
        errors.push('duplicate hreflang');
      }
      if (!ogTitle || !ogDescription || !ogUrl || !ogLocale) {
        errors.push('incomplete Open Graph metadata');
      }
      if (expectedOgLocale && ogLocale !== expectedOgLocale) {
        errors.push(`Open Graph locale mismatch: expected ${expectedOgLocale}, received ${ogLocale}`);
      }
      if (h1Count !== 1) warnings.push(`H1 count is ${h1Count}`);
      if (!appRootHasContent) errors.push('empty SSR app-root');
      if (invalidJsonLd.length > 0) errors.push('invalid JSON-LD');
      if (agent.expectNoJs && executableScripts > 0) {
        errors.push(`${executableScripts} executable scripts in no-js robot HTML`);
      }

      record = {
        agent: agent.name,
        url,
        finalUrl: response.url,
        status: response.status,
        elapsedMs,
        contentType: response.headers.get('content-type'),
        buildVersion: response.headers.get('x-amusementpark-build-version'),
        ssrMode: response.headers.get('x-amusementpark-ssr-mode'),
        ssrCache: response.headers.get('x-amusementpark-ssr-cache'),
        seoReady: response.headers.get('x-amusementpark-seo-ready'),
        seoReadyReason: response.headers.get('x-amusementpark-seo-ready-reason'),
        robotFamily: response.headers.get('x-amusementpark-robot-family'),
        robotHtml: response.headers.get('x-amusementpark-robot-html'),
        title,
        description,
        robots,
        canonical,
        hreflangs,
        ogTitle,
        ogDescription,
        ogUrl,
        ogLocale,
        expectedOgLocale,
        h1Count,
        jsonLdCount: findJsonLd(html).length,
        executableScripts,
        errors,
        warnings
      };
    } catch (error) {
      record = {
        agent: agent.name,
        url,
        elapsedMs: Math.round(performance.now() - startedAt),
        errors: [error instanceof Error ? error.message : String(error)],
        warnings: []
      };
    }

    appendFileSync(outputFile, `${JSON.stringify(record)}\n`, 'utf8');
    console.log(`${record.errors.length === 0 ? 'OK' : 'FAIL'} ${agent.name} ${url}`);
    await new Promise((resolve) => setTimeout(resolve, delayMs));
  }
}

function decodeHtml(value) {
  return value
    .replaceAll('&amp;', '&')
    .replaceAll('&quot;', '"')
    .replaceAll('&#39;', "'")
    .replaceAll('&lt;', '<')
    .replaceAll('&gt;', '>')
    .replaceAll('&nbsp;', ' ');
}

function getAttribute(tag, name) {
  const escaped = name.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
  const match = new RegExp(`\\s${escaped}\\s*=\\s*(['"])(.*?)\\1`, 'i').exec(tag);
  return match?.[2] ?? '';
}

function matchText(html, regex) {
  const match = regex.exec(html);
  return match ? decodeHtml(match[1].replace(/<[^>]+>/g, ' ').replace(/\s+/g, ' ').trim()) : null;
}

function findMeta(html, key, expected) {
  const tag = (html.match(/<meta\b[^>]*>/gi) ?? [])
    .find((candidate) => getAttribute(candidate, key).toLowerCase() === expected);
  return tag ? decodeHtml(getAttribute(tag, 'content').trim()) : null;
}

function findLink(html, rel) {
  const tag = (html.match(/<link\b[^>]*>/gi) ?? [])
    .find((candidate) => getAttribute(candidate, 'rel')
      .toLowerCase()
      .split(/\s+/)
      .includes(rel));
  return tag ? decodeHtml(getAttribute(tag, 'href').trim()) : null;
}

function findHreflangs(html) {
  return (html.match(/<link\b[^>]*>/gi) ?? [])
    .filter((tag) => getAttribute(tag, 'rel').toLowerCase().split(/\s+/).includes('alternate'))
    .map((tag) => ({
      language: getAttribute(tag, 'hreflang').toLowerCase(),
      href: decodeHtml(getAttribute(tag, 'href').trim())
    }))
    .filter((entry) => entry.language && entry.href);
}

function findJsonLd(html) {
  return [...html.matchAll(
    /<script\b[^>]*type\s*=\s*(['"])application\/ld\+json\1[^>]*>([\s\S]*?)<\/script>/gi
  )].map((match) => {
    try {
      JSON.parse(match[2]);
      return { valid: true };
    } catch (error) {
      return { valid: false, error: error instanceof Error ? error.message : String(error) };
    }
  });
}

function countExecutableScripts(html) {
  return (html.match(/<script\b[^>]*>/gi) ?? [])
    .filter((tag) => getAttribute(tag, 'type').toLowerCase() !== 'application/ld+json')
    .length;
}

function resolveExpectedOpenGraphLocale(url) {
  const language = new URL(url).pathname.split('/').filter(Boolean)[0]?.toLowerCase();
  return {
    en: 'en_US',
    fr: 'fr_FR',
    es: 'es_ES',
    de: 'de_DE',
    it: 'it_IT',
    pl: 'pl_PL',
    nl: 'nl_NL',
    pt: 'pt_PT'
  }[language] ?? null;
}
'@ | node --input-type=module -
```

Analyser le fichier sans modifier la production :

```powershell
$htmlAudit = Get-Content -LiteralPath $env:SEO_AUDIT_OUTPUT_FILE |
    ForEach-Object { $_ | ConvertFrom-Json }

$htmlAudit |
    Group-Object status, seoReady, ssrMode, ssrCache |
    Sort-Object Count -Descending |
    Select-Object Count, Name

$htmlAudit |
    Where-Object { $_.errors.Count -gt 0 } |
    Select-Object agent, url, status, elapsedMs, errors, warnings
```

Pour la matrice multi-agents, remplacer le fichier d’URL par l’échantillon des
quatorze familles et utiliser :

```powershell
$env:SEO_AUDIT_USER_AGENTS = @(
    [pscustomobject]@{ name = 'Googlebot'; value = $robotAgents.Googlebot; expectNoJs = $true }
    [pscustomobject]@{ name = 'Bingbot'; value = $robotAgents.Bingbot; expectNoJs = $true }
    [pscustomobject]@{ name = 'YandexBot'; value = $robotAgents.YandexBot; expectNoJs = $true }
    [pscustomobject]@{ name = 'AhrefsBot'; value = $robotAgents.AhrefsBot; expectNoJs = $true }
    [pscustomobject]@{
        name = 'GoogleAgent-Mariner'
        value = 'Mozilla/5.0 GoogleAgent-Mariner/1.0'
        expectNoJs = $false
    }
    [pscustomobject]@{
        name = 'OAI-SearchBot'
        value = 'Mozilla/5.0 (compatible; OAI-SearchBot/1.0; +https://openai.com/searchbot)'
        expectNoJs = $true
    }
    [pscustomobject]@{
        name = 'ChatGPT-User'
        value = 'Mozilla/5.0 ChatGPT-User/1.0'
        expectNoJs = $true
    }
    [pscustomobject]@{
        name = 'Claude-SearchBot'
        value = 'Mozilla/5.0 Claude-SearchBot/1.0'
        expectNoJs = $true
    }
    [pscustomobject]@{
        name = 'PerplexityBot'
        value = 'Mozilla/5.0 PerplexityBot/1.0'
        expectNoJs = $true
    }
) | ConvertTo-Json -Compress
```

Réexécuter ensuite le bloc Node. Les agents d’entraînement interdits doivent être
testés séparément contre `robots.txt`; ne pas les inclure dans un crawl de pages.

## 9. Smoke test no-JavaScript existant

Le dépôt fournit déjà un contrôle du HTML SSR sans JavaScript exécutable. Depuis
`FRONT/AmusementPark` :

```powershell
$env:PUBLIC_BASE_URL = $env:SEO_BASE_URL
$env:SEO_SMOKE_PATHS = '/en/home,/en/parks,/fr/parks,/en/about,/en/privacy'
$env:SEO_SMOKE_USER_AGENT = $robotAgents.Bingbot
$env:SEO_SMOKE_MIN_BODY_TEXT_LENGTH = '500'
npm run seo:ssr-smoke
```

Répéter avec Googlebot, YandexBot et Ahrefs sur l’échantillon court. Mariner fait
exception : il doit être reconnu comme robot et recevoir du SSR froid, mais
conserver son JavaScript interactif.

## 10. Passage froid, passage chaud et résilience

Choisir une URL représentative par famille. Vérifier trois passages successifs :

```powershell
$testUrl = ''
if ([string]::IsNullOrWhiteSpace($testUrl)) {
    throw 'Choose a public sitemap URL.'
}

1..3 | ForEach-Object {
    "=== PASS $_ ==="
    curl.exe -sS -o $nullDevice -D - --max-time 90 `
        -A $robotAgents.Googlebot `
        -H 'Accept: text/html' `
        -w "status=%{http_code} total=%{time_total}s bytes=%{size_download}`n" `
        $testUrl |
        Select-String -Pattern 'HTTP/|X-AmusementPark-|Cache-Control:|Age:|status='
    Start-Sleep -Seconds 3
}
```

Contrôler :

- premier passage en `200` avec rendu SSR ou cache stale fiable ;
- passages suivants en `200` avec cache hit lorsque la politique le prévoit ;
- aucune transition `502`/`503` puis `200` uniquement au second passage ;
- canonical, robots et métadonnées identiques entre froid et chaud ;
- temps froid et chaud consignés séparément ;
- stale servi en cas d’erreur transitoire lorsqu’un document fiable existe ;
- absence de réponse SEO-ready faussement positive sur un HTML incomplet.

Ne pas purger le cache pour simuler le froid sans autorisation. Préférer une URL
publique nouvellement créée ou jamais demandée, identifiée dans le sitemap.

## 11. Routes non indexables, erreurs et API

Construire une matrice sans identifiants réels sensibles :

```powershell
$nonIndexablePaths = @(
    '/fr/admin',
    '/fr/profile',
    '/fr/forgot-password',
    '/fr/reset-password',
    '/fr/not-found',
    '/fr/park/00000000-0000-0000-0000-000000000000/parc-introuvable'
)

foreach ($path in $nonIndexablePaths) {
    $url = "$($env:SEO_BASE_URL.TrimEnd('/'))$path"
    "=== $url ==="
    curl.exe -sS -D - -o (Join-Path $auditDir 'route-body.html') `
        --max-time 90 `
        -A $robotAgents.Googlebot `
        $url |
        Select-String -Pattern 'HTTP/|Content-Type:|X-Robots-Tag:|X-AmusementPark-'
    Get-Content -LiteralPath (Join-Path $auditDir 'route-body.html') |
        Select-String -Pattern '<meta[^>]+robots|<link[^>]+canonical|<title'
}
```

API :

```powershell
curl.exe -sS -I --max-time 30 `
    -A $robotAgents.Googlebot `
    "$($env:SEO_BASE_URL.TrimEnd('/'))/api/parks"

curl.exe -sS -I --max-time 30 `
    -A $robotAgents.Googlebot `
    "$($env:SEO_BASE_URL.TrimEnd('/'))/api/images/binary/nonexistent"
```

Critères :

- une entité publique inexistante retourne un vrai `404`, un HTML SSR utile et
  `noindex,follow`, sans canonical trompeur ;
- les pages admin, compte et authentification ne sont jamais indexables ;
- une route technique ne devient pas indexable par erreur ;
- les API restent bloquées par `robots.txt` et conservent leurs protections ;
- les erreurs ne divulguent pas de détail interne ;
- une variante de slug redirige vers le canonical ou expose un canonical stable,
  selon la politique de route.

## 12. Métadonnées, contenu et données structurées

Sur chaque famille et langue, vérifier :

- attribut `lang` correct ;
- titre unique, localisé et descriptif ;
- meta description localisée et non vide ;
- exactement un H1 principal pertinent ;
- canonical absolu, stable, sans query ni fragment ;
- hreflang uniquement pour les pages réellement servies, avec `x-default` ;
- aucune locale Open Graph anglaise sur une route française ;
- `og:title`, `og:description`, `og:url`, `og:type`, `og:locale` et image si
  fiable ;
- Twitter Cards cohérentes ;
- JSON-LD parseable, fiable et adapté à la page ;
- `BreadcrumbList` contextuel sur les pages profondes ;
- noms réels dans les breadcrumbs, jamais de libellé générique évitable ;
- absence de contenu mince, dupliqué ou uniquement composé de navigation ;
- textes alternatifs utiles sur les images informatives ;
- pas de données structurées inventées ;
- commentaires ou avis structurés uniquement lorsqu’ils existent et sont
  publics ;
- aucune donnée admin ou personnelle dans le HTML public.

Extraire les JSON-LD d’une page :

```powershell
$pageUrl = ''
$htmlPath = Join-Path $auditDir 'jsonld-page.html'
curl.exe -sS --max-time 90 -A $robotAgents.Googlebot -o $htmlPath $pageUrl

$html = Get-Content -LiteralPath $htmlPath -Raw
$matches = [regex]::Matches(
    $html,
    '<script\b[^>]*type=["'']application/ld\+json["''][^>]*>([\s\S]*?)</script>',
    [Text.RegularExpressions.RegexOptions]::IgnoreCase
)

$matches | ForEach-Object {
    $_.Groups[1].Value | ConvertFrom-Json
}
```

Comparer les titres et descriptions du relevé large pour détecter les doublons :

```powershell
$htmlAudit |
    Where-Object { -not [string]::IsNullOrWhiteSpace($_.title) } |
    Group-Object title |
    Where-Object Count -gt 1 |
    Sort-Object Count -Descending |
    Select-Object Count, Name

$htmlAudit |
    Where-Object { -not [string]::IsNullOrWhiteSpace($_.description) } |
    Group-Object description |
    Where-Object Count -gt 1 |
    Sort-Object Count -Descending |
    Select-Object Count, Name
```

Un doublon n’est pas automatiquement un bug : l’analyse doit distinguer les
alternates légitimes, les variantes de langue et les pages réellement
concurrentes.

## 13. État du VPS, conteneurs et ressources

Toutes les commandes de cette section sont en lecture seule :

```powershell
ssh -i $env:SEO_SSH_IDENTITY $env:SEO_SSH_TARGET `
    "docker ps --format 'table {{.Names}}\t{{.Image}}\t{{.Status}}\t{{.Ports}}'"

ssh -i $env:SEO_SSH_IDENTITY $env:SEO_SSH_TARGET `
    'uptime; free -m; df -h /; df -i /; docker stats --no-stream'

ssh -i $env:SEO_SSH_IDENTITY $env:SEO_SSH_TARGET `
    "docker inspect amusementpark-front --format 'Memory={{.HostConfig.Memory}} MemorySwap={{.HostConfig.MemorySwap}} Restart={{.HostConfig.RestartPolicy.Name}} Restarts={{.RestartCount}} State={{.State.Status}} Health={{if .State.Health}}{{.State.Health.Status}}{{end}}'"

ssh -i $env:SEO_SSH_IDENTITY $env:SEO_SSH_TARGET `
    "docker inspect amusementpark-api --format 'Restarts={{.RestartCount}} State={{.State.Status}} Health={{if .State.Health}}{{.State.Health.Status}}{{end}}'"
```

Contrôler :

- conteneurs attendus actifs et sains ;
- aucune boucle de redémarrage ;
- marge RAM, CPU, disque et inodes ;
- limite mémoire Node cohérente avec `NODE_OPTIONS` et la limite Docker ;
- swap et stratégie de redémarrage connus ;
- espace suffisant pour le cache SSR, Docker, MongoDB et les logs ;
- aucune saturation pendant un crawl contrôlé.

## 14. Configuration SSR et cache sans exposer de secrets

Lister uniquement les variables autorisées :

```powershell
ssh -i $env:SEO_SSH_IDENTITY $env:SEO_SSH_TARGET @'
docker exec amusementpark-front sh -lc "
  printenv |
  grep -E '^(NODE_OPTIONS|SSR_(RENDER|PAGE_CACHE|DISK_PAGE_CACHE|STALE_PAGE_CACHE|TARGETED_REFRESH|TECHNICAL_STATS|ROBOT_NO_JS_HTML)_)' |
  sort
"
'@
```

Mesurer le cache disque :

```powershell
ssh -i $env:SEO_SSH_IDENTITY $env:SEO_SSH_TARGET @'
docker exec amusementpark-front sh -lc '
  cache_dir="${SSR_DISK_PAGE_CACHE_DIR:-/var/cache/amusementpark-ssr}"
  printf "directory=%s\n" "$cache_dir"
  du -sh "$cache_dir"
  find "$cache_dir" -type f | wc -l
  find "$cache_dir" -type f -printf "%s\n" |
    sort -n |
    awk "
      {sizes[NR]=\$1; total+=\$1}
      END {
        if (NR == 0) { print \"files=0\"; exit }
        p50=sizes[int((NR-1)*0.50)+1]
        p95=sizes[int((NR-1)*0.95)+1]
        p99=sizes[int((NR-1)*0.99)+1]
        printf \"files=%d total=%d p50=%d p95=%d p99=%d max=%d\n\", NR, total, p50, p95, p99, sizes[NR]
      }
    "
'
'@
```

Vérifier que le plafond du cache garde une marge opérationnelle explicite. Ne
jamais dimensionner le cache jusqu’à consommer tout le disque disponible.

## 15. Logs SSR, erreurs API et redémarrages

Choisir une fenêtre UTC :

```powershell
$env:SEO_LOG_SINCE = '24h'
```

Compter et extraire les signaux critiques :

```powershell
ssh -i $env:SEO_SSH_IDENTITY $env:SEO_SSH_TARGET @"
docker logs --since '$($env:SEO_LOG_SINCE)' --timestamps amusementpark-front 2>&1 |
grep -E 'FATAL ERROR|heap out of memory|Reached heap limit|429 Too Many Requests|SSR-BOT-UNAVAILABLE|blocked-not-seo-ready|render queue|queue full|ECONNREFUSED|ETIMEDOUT' || true
"@

ssh -i $env:SEO_SSH_IDENTITY $env:SEO_SSH_TARGET @"
docker logs --since '$($env:SEO_LOG_SINCE)' --timestamps amusementpark-front 2>&1 |
grep -c -E 'FATAL ERROR|heap out of memory|Reached heap limit' || true
"@

ssh -i $env:SEO_SSH_IDENTITY $env:SEO_SSH_TARGET @"
docker logs --since '$($env:SEO_LOG_SINCE)' --timestamps amusementpark-front 2>&1 |
grep -c '429 Too Many Requests' || true
"@

ssh -i $env:SEO_SSH_IDENTITY $env:SEO_SSH_TARGET @"
docker logs --since '$($env:SEO_LOG_SINCE)' --timestamps amusementpark-front 2>&1 |
grep -c 'Angular SSR server listening' || true
"@
```

Répéter sur une fenêtre couvrant les derniers déploiements. Corréler chaque OOM,
redémarrage, 429, 502 ou 503 avec :

- heure du déploiement ;
- warmup actif ;
- crawl externe ;
- pression CPU/RAM ;
- taux de miss du cache ;
- file de rendu ;
- endpoint API concerné.

Ne pas conclure qu’un sitemap est défaillant lorsqu’un proxy a simplement perdu
son upstream pendant un redémarrage.

## 16. Forwarded headers et rate limiting interne SSR

Relever uniquement les variables non secrètes utiles :

```powershell
ssh -i $env:SEO_SSH_IDENTITY $env:SEO_SSH_TARGET @'
docker exec amusementpark-api printenv |
grep -E '^(AllowedHosts|ForwardedHeaders__|RateLimiting__)' |
sort
'@
```

Vérifier les implémentations :

```powershell
rg -n "RateLimit|AddRateLimiter|X-AmusementPark-Internal-SSR|ForwardedHeaders" `
    API FRONT deploy docs .github -S

Get-Content `
    API/AmusementPark.WebAPI/DependencyInjection/RateLimitingServiceCollectionExtensions.cs

Get-Content `
    FRONT/AmusementPark/src/app/core/http/backends/server-api-base-url.backend.ts
```

Points à contrôler :

- les appels du SSR utilisent l’URL réseau interne prévue ;
- le marqueur interne ne peut pas être usurpé depuis Internet ;
- les forwarded headers ne transforment pas une requête interne en requête
  soumise au petit quota public ;
- aucun bypass global ou public n’affaiblit le rate limiting ;
- les endpoints de lecture nécessaires au SSR ne produisent pas de 429 pendant
  un audit à débit contrôlé ;
- les quotas auth restent stricts et indépendants.

## 17. Warmup SSR continu

Inspecter uniquement les variables de warmup :

```powershell
ssh -i $env:SEO_SSH_IDENTITY $env:SEO_SSH_TARGET @'
cd /opt/amusementpark
grep -E '^SSR_WARMUP_' .env |
sed -E 's/(TOKEN|SECRET|PASSWORD|KEY)=.*/\1=[REDACTED]/I' |
sort
'@
```

Contrôler le service, les processus et le verrou :

```powershell
ssh -i $env:SEO_SSH_IDENTITY $env:SEO_SSH_TARGET `
    "systemctl status amusementpark-ssr-warmup.service --no-pager || true"

ssh -i $env:SEO_SSH_IDENTITY $env:SEO_SSH_TARGET `
    "ps -eo pid,ppid,etimes,cmd | grep -E 'warmup-ssr-cache|ssr-warmup' | grep -v grep || true"

ssh -i $env:SEO_SSH_IDENTITY $env:SEO_SSH_TARGET @'
cd /opt/amusementpark/warmup
find . -maxdepth 2 -type f \( -name '*.log' -o -name '*.csv' \) -printf '%TY-%Tm-%TdT%TH:%TM:%TSZ %p\n' |
sort -r |
head -20
'@
```

Inspecter le dernier journal sans supposer son nom :

```powershell
ssh -i $env:SEO_SSH_IDENTITY $env:SEO_SSH_TARGET @'
cd /opt/amusementpark/warmup
latest="$(find . -maxdepth 2 -type f -name '*.log' -printf '%T@ %p\n' | sort -nr | head -1 | cut -d" " -f2-)"
if [ -n "$latest" ]; then
  grep -E 'configuration|finished|failed|status=429|status=502|status=503|seo=false|locked' "$latest" || true
fi
'@
```

Contrôler :

- un seul cycle actif ;
- verrou effectif et reprise après erreur ;
- concurrence et pause adaptées au VPS ;
- sélection bornée et explicable ;
- validation robot active ;
- aucun échec masqué par un lancement en arrière-plan ;
- artefacts retenus pendant la durée prévue puis purgés ;
- aucun OOM, 429, 502 ou 503 corrélé au warmup ;
- warmup hors chemin critique du déploiement.

Le déclenchement manuel suivant est volontairement exclu de l’audit en lecture
seule. Ne l’exécuter qu’après autorisation, avec un fichier d’URL borné :

```powershell
# À exécuter sur le VPS uniquement après autorisation explicite.
cd /opt/amusementpark
SSR_WARMUP_URL_FILE=/chemin/autorise/urls.txt \
SSR_WARMUP_MAX_URLS=20 \
SSR_WARMUP_CONCURRENCY=1 \
SSR_WARMUP_SLEEP_SECONDS=2.1 \
SSR_WARMUP_FAIL_IF_LOCKED=true \
./scripts/warmup-ssr-cache.sh
```

## 18. Statistiques techniques SSR

Inspecter la présence et la rétention :

```powershell
ssh -i $env:SEO_SSH_IDENTITY $env:SEO_SSH_TARGET @'
docker exec amusementpark-front sh -lc '
  stats_dir="${SSR_DISK_PAGE_CACHE_DIR:-/var/cache/amusementpark-ssr}/technical-stats"
  printf "directory=%s\n" "$stats_dir"
  find "$stats_dir" -maxdepth 1 -type f -name "bucket-*.json" -printf "%TY-%Tm-%Td %s %f\n" |
    sort
'
'@
```

Copier les buckets dans le dossier temporaire si une analyse locale est
nécessaire :

```powershell
$statsArchive = Join-Path $auditDir 'technical-stats.tar.gz'
ssh -i $env:SEO_SSH_IDENTITY $env:SEO_SSH_TARGET @'
docker exec amusementpark-front sh -lc '
  stats_dir="${SSR_DISK_PAGE_CACHE_DIR:-/var/cache/amusementpark-ssr}/technical-stats"
  tar -C "$stats_dir" -czf - bucket-*.json
'
'@ > $statsArchive
```

Pour chaque jour et famille de robot, relever :

- réponses totales ;
- SEO-ready et non SEO-ready ;
- hit mémoire, hit disque, stale, miss et rendu ;
- indisponibilité SSR et cache-only miss ;
- rejets de file ;
- statuts 2xx, 404, 429, 502 et 503 ;
- temps p50, p95 et p99 ;
- volume par Google, Bing, Yandex, Ahrefs et autres familles importantes.

Les compteurs non SEO-ready, indisponibilité, file pleine, 429, 502, 503 et OOM
doivent être nuls ou expliqués par un incident connu.

## 19. Disponibilité pendant un déploiement

Ce test doit être planifié pendant un déploiement autorisé. Lancer avant la
bascule et arrêter après la stabilisation :

```powershell
$availabilityUrl = "$($env:SEO_BASE_URL.TrimEnd('/'))/fr"
$availabilityLog = Join-Path $auditDir 'deployment-availability.ndjson'

1..300 | ForEach-Object {
    $startedAt = Get-Date
    $status = curl.exe -sS -o $nullDevice --max-time 10 `
        -A $robotAgents.Googlebot `
        -w '%{http_code}' `
        $availabilityUrl

    [pscustomobject]@{
        AtUtc = $startedAt.ToUniversalTime().ToString('o')
        Status = $status
    } |
        ConvertTo-Json -Compress |
        Add-Content -LiteralPath $availabilityLog -Encoding utf8

    Start-Sleep -Seconds 1
}
```

Critère : aucun `502`, `503`, reset ou timeout pendant la bascule. Vérifier aussi
que la version passe atomiquement de l’ancienne à la nouvelle et que le proxy ne
vise jamais un conteneur non prêt.

## 20. Outils webmaster et crawlers externes

Les contrôles manuels complètent les commandes techniques :

- Google Search Console : propriété canonique, sitemap canonique unique, date de
  dernière lecture, erreurs de récupération, pages indexées/exclues, crawl et
  inspection d’URL ;
- Bing Webmaster Tools : sitemap, crawl, inspection et erreurs HTTP ;
- Yandex Webmaster : sitemap, exclusions et réponses robot ;
- Ahrefs : crawl postérieur au dernier correctif, comparaison par famille d’URL,
  statuts et profondeur ;
- Core Web Vitals et Lighthouse : échantillon mobile/desktop par famille ;
- logs : comparer les User-Agents réels aux familles reconnues par le serveur.

Ne pas supprimer massivement des familles du sitemap ou ajouter `noindex` sur la
base d’un seul rapport ancien. Croiser les impressions, clics, crawl réel,
qualité du contenu et coût SSR sur une fenêtre représentative.

## 21. Critères de sortie

L’audit technique est réussi lorsque :

- domaine, TLS et redirections sont canoniques ;
- `robots.txt` est cohérent avec la politique produit ;
- index et tous les sitemaps enfants sont valides et accessibles ;
- aucune URL sitemap n’est dupliquée, hors domaine ou invalide ;
- chaque famille et langue échantillonnée répond correctement ;
- les URL indexables renvoient `200`, `index,follow` et `seoReady=true` ;
- les entités absentes renvoient un vrai `404` et `noindex` ;
- canonical, hreflang, Open Graph et JSON-LD sont cohérents ;
- Googlebot, Bingbot, YandexBot et Ahrefs reçoivent un HTML SSR exploitable ;
- le no-JavaScript robot ne contient pas de script exécutable, sauf exception
  interactive explicitement décidée ;
- aucun OOM, 429, 502, 503, rejet de file ou warmup concurrent n’est observé ;
- cache, disque, RAM et temps de rendu gardent une marge mesurée ;
- le déploiement ne produit aucune coupure ;
- les soumissions webmaster ne contiennent que les sitemaps voulus.

## 22. Modèle de compte rendu

Un rapport daté doit rester court et séparer les faits des hypothèses :

```markdown
# Audit SEO production — AAAA-MM-JJ

## Contexte

- Période UTC :
- SHA / version :
- Auditeur :
- Périmètre :
- Limites :

## Synthèse

- Statut global :
- Risques P0 :
- Risques P1 :
- Dette P2 :

## Mesures

| Zone | Échantillon | Succès | Échecs | Preuve temporaire |
|---|---:|---:|---:|---|

## Anomalies reproductibles

### Titre

- URL ou famille :
- User-Agent :
- Commande :
- Résultat attendu :
- Résultat observé :
- Première occurrence UTC :
- Corrélation VPS :

## Actions proposées

| Priorité | Action | Critère d’acceptation | PR/issue |
|---|---|---|---|

## Vérification après correction

- Commandes rejouées :
- Résultat :
- Version déployée :
```

Ne jamais coller de fichier `.env`, de cookie, de jeton, de clé SSH ou de logs
contenant des données personnelles dans un rapport versionné.
