# Partage révoqué encore visible

## Déclenchement

Un seul accès confirmé à un partage après révocation suffit. Traiter également
toute réponse privée servie depuis un cache public.

## Triage

Relever le type de partage, l'heure de révocation, le statut HTTP, les en-têtes de
cache et le trace ID sans copier le contenu partagé.

## Protection immédiate

Suspendre la publication concernée, invalider son cache public et bloquer sa
résolution. Ne pas désactiver les autres partages sans preuve d'un défaut global.

## Diagnostic

Vérifier que le résolveur exige publié, visible, non suspendu et non révoqué avant
toute lecture du propriétaire. Contrôler les caches navigateur, edge et SSR.

## Rétablissement

Corriger l'autorité unique de publication ou l'invalidation. Ne jamais ajouter un
adaptateur qui laisserait deux systèmes de visibilité coexister.

## Vérification

Tester l'ancien jeton depuis deux clients : il doit échouer sans lire le
propriétaire. Vérifier ensuite un partage actif indépendant.

## Escalade

Escalader immédiatement à `product-sharing` et `privacy-operations` si du contenu
privé a été exposé ; appliquer aussi le runbook d'incident de confidentialité.

## Preuves à conserver

Conserver empreinte non réversible du jeton, statuts, en-têtes, heures UTC et trace
IDs. Ne jamais coller le jeton ni le contenu dans un ticket.
