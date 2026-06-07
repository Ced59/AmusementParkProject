# Admin Fast Edition guidelines

Ce document fixe les regles de relecture des evolutions M25 et suivantes pour accelerer l'administration sans creer une deuxieme architecture.

## Regles absolues

- Garder les composants Angular concentres sur l'UI: aucun appel HTTP direct, aucune orchestration metier lourde dans les templates.
- Passer par des facades et des ports de data-access pour les flux rapides, comme les ecrans admin existants.
- Reutiliser les composants admin existants quand ils conviennent au besoin reel: etats de page, listes filtrees, pagination, barres de sauvegarde, import JSON, images de reference.
- Creer des contrats HTTP dedies aux flux rapides au lieu d'alourdir `ParkItemDto`, `ParkItemCreateDto` ou `ParkItemUpdateDto`.
- Centraliser les valeurs par defaut de creation rapide cote Application, puis les exposer au front par mapping ou contrat explicite quand un endpoint est ajoute.
- Garder les nouveaux modules admin en lazy/admin scope. Rien du workbench ne doit entrer dans le bundle public initial.
- Conserver les validations, l'autorisation admin, l'audit des actions sensibles et les regles de domaine existantes.
- Ajouter des tests sur les mappers, facades ou policies des que le flux modifie une donnee envoyee a l'API.

## Frontieres

- `features/admin/park-items/workbench` porte l'orchestration du workbench et ses modeles front.
- `components/admin/park-items/workbench` porte uniquement des composants reutilisables du workbench quand ils deviennent necessaires.
- `data-access` reste proprietaire des appels HTTP concrets.
- `Application/Features/ParkItems` reste proprietaire des valeurs par defaut et des regles metier.
- `WebAPI/Contracts/ParkItems` expose les DTO rapides, sans faire glisser les details d'UI dans le domaine.

## Checklist de revue

- Le flux rapide respecte-t-il les ports/facades au lieu d'injecter directement un service concret dans un composant?
- Le contrat rapide est-il distinct du DTO complet?
- Les valeurs par defaut ne sont-elles pas dupliquees dans plusieurs composants Angular?
- Le comportement public/SEO/SSR reste-t-il inchange?
- Les actions mutantes restent-elles admin-only et auditables?
- Les tests couvrent-ils les mappings qui transforment les donnees saisies rapidement?
