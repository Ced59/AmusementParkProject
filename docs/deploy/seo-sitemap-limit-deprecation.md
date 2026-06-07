# Dépréciation de la limite sitemap dynamique

La variable `SEO_MAX_DYNAMIC_URLS_PER_TYPE` est dépréciée et ne doit plus être utilisée.

La génération sitemap doit référencer toutes les pages publiques visibles, sans plafond métier arbitraire. La seule limite conservée est la limite protocolaire de 50 000 URLs par fichier sitemap, gérée automatiquement par découpage de sections.

À faire côté environnement existant : supprimer `SEO_MAX_DYNAMIC_URLS_PER_TYPE` des fichiers `.env` réels si elle existe encore sur le VPS ou dans GitHub Actions.
