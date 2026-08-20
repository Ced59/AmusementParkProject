# Fiabilisation de la livraison des sitemaps à Google

## Constat de production

Google Search Console signale depuis la première soumission que `https://amusement-parks.fun/sitemap.xml` est impossible à récupérer ou à lire, avec zéro page découverte. Bing et Yandex parcourent pourtant les mêmes documents.

L'audit croisé du code, de MongoDB, du proxy et des journaux de production a établi les points suivants :

- le sitemap canonique répond en `200`, sans redirection, en HTTP/1.0, HTTP/1.1 et HTTP/2 ;
- le type est `application/xml`, le corps est UTF-8 et le XML est bien formé ;
- `robots.txt` autorise le document et ne déclare que l'URL canonique ;
- le certificat TLS, la résolution IPv4 et l'accès général de Googlebot sont fonctionnels ;
- aucune règle Nginx, pare-feu ou protection par User-Agent ne bloque Google ;
- Googlebot a reçu l'index racine en `200` le 28 juillet, mais aucun sous-sitemap n'a ensuite été demandé par le Googlebot d'indexation ;
- aucune requête Google identifiable vers l'index canonique n'a atteint Nginx le 7 août, date de la soumission GSC affichée ;
- le snapshot servi le 7 août contenait environ 84 400 URLs réparties entre 496 petits fichiers ;
- le snapshot du 20 août contenait 86 352 URLs, 512 fichiers et un index racine de 62 689 octets, très proche de l'ancien seuil de troncature observé autour de 64 Kio ;
- de vrais Googlebots continuent aussi à demander l'ancienne URL `/sitemaps.xml`, qui répondait en `404` ;
- une génération automatique invalidait le cache XML API, mais pas le cache XML frontend sans expiration.

Le problème ne correspond donc pas à une indisponibilité générale. Les deux défauts structurels sont un graphe de découverte inutilement fragmenté et une publication non atomique du snapshot à travers les caches. L'ancienne URL encore parcourue constitue un bruit supplémentaire mesurable.

## Corrections

- Porter le bloc opérationnel de 200 à 1 000 URLs par fichier. Sur le corpus audité, l'index doit passer de 512 à environ 184 enfants tout en restant très loin des limites de 50 000 URLs et 50 Mio non compressés par fichier.
- Invalider d'abord le cache XML API puis le cache XML du frontend SSR après toute génération automatique réussie.
- Rediriger définitivement `/sitemaps.xml` vers `/sitemap.xml` afin que les anciennes découvertes Google ne finissent plus en `404`.
- Conserver les URL enfants canoniques directement à la racine et ne jamais les faire passer par une redirection.

## Validation après déploiement

La validation finale doit être exécutée seulement après la régénération manuelle du snapshot :

1. vérifier que la génération n'a demandé ni soumis aucune URL à IndexNow ;
2. télécharger l'index depuis Internet et depuis le VPS avec un User-Agent Googlebot ;
3. parser chaque enfant, contrôler les statuts, types, longueurs, hôtes, doublons et limites protocolaires ;
4. vérifier qu'un enfant supérieur à 64 Kio est livré intégralement, avec un `Content-Length` exact ;
5. vérifier la redirection permanente de `/sitemaps.xml` et le `404` des chemins réellement inventés ;
6. resoumettre l'URL canonique dans GSC, puis corréler l'heure de lecture avec les journaux Nginx remis en état.

La réussite de `curl` et du parseur XML prouve la conformité de l'origine. Le statut GSC ne peut être déclaré résolu qu'après une nouvelle lecture effectuée par Google.
