# Budget de lecture des invalidations SSR ciblées

Une invalidation par chemin ou préfixe doit vérifier les clés du cache disque.
La lecture intégrale de chaque fichier JSON recharge aussi son HTML, y compris
pour les pages étrangères à la modification. Avec plusieurs gigaoctets de cache
et des mises à jour rapprochées, cette boucle synchrone concurrence le rendu SSR
et les réponses publiques.

Le lecteur `disk-page-cache-invalidation-reader.ts` ouvre les fichiers un à un,
de façon asynchrone. Il examine au maximum 8 Kio pour reconnaître les champs
initiaux produits par le sérialiseur courant (`buildVersion`, `cacheKey`,
`statusCode`, puis `html`). Une clé non concernée ne nécessite aucune lecture
complète ni désérialisation du HTML.

Les entrées concernées conservent le traitement existant : suppression ou
conservation temporaire périmée, suivi des clés et rafraîchissement. Un ancien
format, un ordre différent, une clé trop longue ou un en-tête incomplet suit la
lecture complète existante. Les lots rendent la main même quand toutes leurs
entrées sont écartées. Les descripteurs sont fermés dans tous les cas.

La correction ne change ni les URL, ni les règles d’indexation, ni la durée du
cache, ni les limites de concurrence. Elle n’efface pas le cache à la livraison.
Les anciennes versions de fichiers restent soumises à la politique de version
déjà présente.

Tests ciblés : `node --test tools/disk-page-cache-invalidation-reader.test.mjs`
depuis `FRONT/AmusementPark` avec le Node de CI. Ils couvrent le budget de lecture,
les vrais fichiers, les clés échappées, les formats anciens et les erreurs. La
suite est intégrée à la CI de production.

Après livraison, comparer des fenêtres datées d’activité naturelle : lectures
disque du conteneur exact, invalidations, durées de rendu, saturation et réponses
503/504. Un redémarrage ou une charge différente interdit d’attribuer seul un
écart de compteurs au correctif. Les XML statiques suivent un parcours distinct ;
une ingestion Google doit toujours être confirmée dans Search Console.
