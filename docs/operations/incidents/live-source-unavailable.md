# Source LIVE indisponible

## Déclenchement

Ouvrir l'incident après deux cycles consécutifs en échec ou lorsque la fraîcheur
sort de la politique affichée dans le panneau LIVE.

## Triage

Identifier source, parc, cible, dernière observation valide et incidents qualité.
Séparer panne fournisseur, mapping absent et rejet de qualité.

## Protection immédiate

Désactiver `live:public-experience` si l'impact public est global. Le repli conserve
les fiches parc et attraction sans attente, historique ni prévision.

## Diagnostic

Lire le panneau opérationnel LIVE, les cycles, mappings, fraîcheur, droits source
et incidents. Ne jamais forcer une observation rejetée dans l'historique.

## Rétablissement

Réparer la source ou le mapping par les actions auditées. Rejouer uniquement les
incidents prévus comme rejouables et conserver l'ordre chronologique.

## Vérification

Exiger deux cycles sains, une fraîcheur conforme et une lecture publique exacte
sur un parc et une attraction avant de relever le kill switch.

## Escalade

Escalader à `product-live` si les droits, la sémantique du fournisseur ou le
mapping sont ambigus. Garder le flag désactivé par défaut pendant l'ambiguïté.

## Preuves à conserver

Conserver source, scope, codes de diagnostic, fraîcheur, trace IDs et heures UTC.
Ne pas conserver de payload fournisseur inutile.
