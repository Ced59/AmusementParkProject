# QUAL-05 — Instrumentation produit typée et versionnée

> Date : 29 septembre 2026  
> Statut : livré  
> Portée : `RANK`, `PASS`, `SHARE`, `FIT`, `WATCH`, `TRIP`, `HIST`, `LIVE`  
> Version de schéma : `1`

## Résultat métier

Le produit dispose désormais d'un langage de mesure commun. Une action comme
« visite terminée », « partage ouvert » ou « état live affiché » ne peut plus être
nommée et enrichie différemment par chaque écran. Les 54 événements autorisés et
leurs propriétés sont décrits par des unions TypeScript fermées puis contrôlés une
seconde fois à l'exécution.

Cette fondation ne déclenche pas de nouvelle collecte : elle migre les événements
Matomo PASS et SHARE déjà actifs vers un adaptateur unique et prépare les six
autres familles. FIT, WATCH et les métriques techniques existantes restent dans
leurs canaux first-party conformément à `QUAL-01` tant qu'une activation Matomo
n'est pas justifiée.

## Flux commun

```text
Action confirmée par une façade Angular
  └─ ProductAnalyticsEvent (union fermée)
       └─ PRODUCT_ANALYTICS_PORT
            └─ validation du nom, des clés et des valeurs
                 ├─ invalide : abandon local, aucune requête
                 └─ valide + navigateur + consentement
                      └─ pixel Matomo commun, sans identifiant métier
```

Le contrat sérialise une enveloppe logique stable :

```json
{
  "event": "ride_occurrence_added",
  "schemaVersion": 1,
  "properties": {
    "source": "authenticated",
    "countBucket": "two-to-five"
  }
}
```

L'adaptateur Matomo encode cette enveloppe dans les champs de catégorie, action
et libellé. Le libellé commence toujours par `schema-version=1`. Les valeurs sont
des catégories courtes ; aucun identifiant, texte libre, URL privée, date exacte
ou note exacte n'est accepté.

## Contrats par famille

| Famille | Événements v1 | État après migration |
|---|---:|---|
| RANK | 4 | vocabulaire réservé, pas de nouvelle émission |
| PASS | 12 | émissions existantes migrées |
| SHARE | 8 | émissions existantes migrées |
| FIT | 5 | vocabulaire aligné, agrégats first-party conservés |
| WATCH | 5 | vocabulaire aligné, agrégats/métriques conservés |
| TRIP | 8 | vocabulaire réservé, pas de nouvelle émission |
| HIST | 6 | vocabulaire réservé, pas de nouvelle émission |
| LIVE | 6 | vocabulaire réservé, métriques techniques conservées |

La valeur `methodVersion` n'accepte que les versions publiques courtes
`park-fit-AAAA-MM` ou `live-forecast-AAAA-MM`. Un hash interne ou une chaîne libre
est rejeté. `share_render_failed` conserve son contrat v1 (`recapType` seulement) ;
l'ajout d'une classe d'erreur modifiera explicitement le schéma au lieu de changer
silencieusement le sens de l'événement existant.

## Garanties techniques

- un seul port Angular et un seul adaptateur Matomo pour les événements produit ;
- aucun deuxième moteur ni couche de compatibilité héritée ;
- typage exhaustif des noms et propriétés à la compilation ;
- validation stricte à l'exécution des clés manquantes, supplémentaires et valeurs ;
- consentement facultatif et exclusion SSR conservés ;
- politique `no-referrer` commune ;
- garde CI interdisant les anciens ports, un second adaptateur et les requêtes
  Matomo construites depuis une feature ;
- tests de sérialisation, couverture des huit familles, consentement, SSR et refus
  des propriétés inconnues.

## Impacts et limites

- Aucun contrat HTTP, comportement métier, écran ou collection MongoDB n'est modifié.
- La navigation sans consentement est strictement identique.
- La version des libellés Matomo permet aux futurs tableaux de bord de distinguer
  proprement l'ancien encodage du schéma v1.
- La rétention Matomo de 180 jours décidée dans `QUAL-01` reste une responsabilité
  de configuration d'exploitation ; aucun stockage supplémentaire n'est créé.
- Les dashboards utiles et leurs alertes appartiennent au jalon `QUAL-06`.

## Vérification

- compilation TypeScript ciblée du contrat et de ses 54 définitions ;
- garde d'architecture exécutée sur 1 874 fichiers TypeScript ;
- tests Angular ciblés ajoutés pour le contrat et l'adaptateur commun ;
- anciens ports, adaptateurs et tests spécialisés supprimés ;
- CI de production enrichie avec le contrôle `architecture:product-analytics`.

