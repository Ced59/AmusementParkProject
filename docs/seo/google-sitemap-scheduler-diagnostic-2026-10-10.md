# Diagnostic de récupération des sitemaps par Google

## Résultat

L'origine publique livre correctement les sitemaps. Le diagnostic ne met en
évidence ni blocage de Googlebot, ni XML invalide, ni troncature actuelle. En
revanche, aucune récupération autonome d'un sitemap XML par Google n'apparaît
dans la fenêtre de journaux vérifiée. Le problème restant se situe donc avant la
lecture du contenu : le service de sitemaps de Google n'a pas lancé de nouvelle
récupération observable.

Ce constat ne prouve pas la cause interne chez Google et ne permet pas de
déclarer le traitement rétabli. Il écarte en revanche la nécessité de modifier à
nouveau les routes, le type MIME ou le format XML sans nouvelle trace contraire.

## Preuves du 9 octobre 2026

Les contrôles publics de l'index XML, du petit sitemap texte français et de
`robots.txt` donnent les mêmes corps avec un navigateur ordinaire et avec un
User-Agent Googlebot : statut `200`, type attendu, longueur exacte et empreinte
SHA-256 identique. L'index est un `sitemapindex` bien formé qui référence 232
enfants uniques. `robots.txt` annonce l'index XML et le sitemap texte.

Les réponses XML possèdent des validateurs `Last-Modified` et `ETag`. Une
requête conditionnelle valide reçoit `304`. Les accès HTTP, HTTPS, IPv4 et les
redirections vers l'hôte canonique sont cohérents. Le snapshot servi par le
domaine public est identique au snapshot statique courant en production.

L'analyse des accès du proxy du 28 septembre au 9 octobre, après validation des
adresses dans les plages officielles Google, distingue deux comportements :

- le Googlebot autonome a demandé `robots.txt` 133 fois et les plans HTML
  localisés 10 fois, toujours avec une réponse `200` ;
- il n'a demandé ni `/sitemap.xml`, ni un sitemap XML enfant, ni le petit
  sitemap texte ;
- `Google-InspectionTool`, déclenché par un test utilisateur, a bien reçu
  l'index et plusieurs documents enfants en `200`.

La réussite du test actif prouve que Google peut joindre les documents, mais ne
prouve pas que son ordonnanceur autonome les a traités. Inversement, l'absence
de requête autonome empêche d'attribuer le statut Search Console actuel à un
rejet du corps XML par le serveur.

## Signal `lastmod` corrigé

L'index attribuait auparavant la date de génération courante aux sections dont
aucune URL ne possède de date de modification connue. Les sections statiques
pouvaient ainsi paraître nouvelles chaque jour alors que leur contenu était
inchangé. Ce signal artificiel est supprimé : en l'absence de date fiable,
`lastmod` est désormais omis. Les sections qui disposent de dates réelles
conservent la plus récente.

Google recommande d'employer `lastmod` seulement lorsqu'il décrit de manière
cohérente une modification significative. Cette correction améliore la qualité
du signal de crawl ; elle ne peut pas, à elle seule, forcer une récupération ni
garantir l'indexation.

## Suite opérationnelle

1. Lire une seule fois l'état courant par l'API Search Console et conserver
   `isPending`, `lastSubmitted`, `lastDownloaded`, le type et les compteurs.
2. Si l'index reste non traité après le déploiement, effectuer une seule nouvelle
   soumission de l'URL canonique `/sitemap.xml` ; ne pas multiplier les variantes
   ni les soumissions quotidiennes.
3. Corréler l'heure de cette soumission avec les accès du proxy provenant des
   plages officielles Google. Une requête autonome suivie d'un `200` déplacerait
   le diagnostic vers le traitement Google ; une erreur HTTP observée fournirait
   au contraire une nouvelle piste serveur précise.
4. Suivre séparément les exclusions URL par URL, le HTML SSR et la demande de
   recherche. Un sitemap accepté reste un signal de découverte, pas une garantie
   d'indexation ni de classement.

Le diagnostic ne fournit aucune preuve que des textes localisés naturellement
proches seraient la cause du défaut de récupération. Une réécriture massive ne
doit pas être engagée sans signal URL précis. La documentation Google distingue
les versions réellement traduites des doublons dont le contenu principal reste
dans la même langue.

## Références

- [Rapport Sitemaps](https://support.google.com/webmasters/answer/7451001?hl=fr)
- [Ressource Sitemaps de l'API Search Console](https://developers.google.com/webmaster-tools/v1/sitemaps)
- [Créer et envoyer un sitemap](https://developers.google.com/search/docs/crawling-indexing/sitemaps/build-sitemap?hl=fr)
- [Versions localisées d'une page](https://developers.google.com/search/docs/specialty/international/localized-versions?hl=fr)
