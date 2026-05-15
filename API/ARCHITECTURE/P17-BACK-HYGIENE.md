# P17 — Back — hygiène finale et cohérence transverse

## Objectif

Finaliser le durcissement back après les refactors structurels, sans changement fonctionnel volontaire :

- ne plus écrire directement dans la console depuis l'infrastructure ;
- éviter que les annulations applicatives soient transformées en erreurs métier génériques ;
- retirer les valeurs sensibles du seed admin de développement ;
- rendre les erreurs HTTP et la pagination plus cohérentes ;
- conserver des logs structurés côté serveur tout en gardant les réponses HTTP sobres.

## Décisions

### Logs structurés

Les dernières traces `Console.WriteLine` du back ont été remplacées par `ILogger<T>` :

- initialisation Mongo ;
- seed pays ;
- seed admin local ;
- pipeline Captain Coaster ;
- suppression d'objets MinIO ;
- exception handler global WebAPI.

### Catches

Les anciens `catch` nus ont été supprimés.

Les handlers applicatifs qui convertissent une erreur technique en `ApplicationResult` propagent maintenant explicitement les annulations via :

```csharp
catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
{
    throw;
}
```

Cela évite de classer un arrêt volontaire, une annulation HTTP ou un arrêt serveur comme une erreur métier générique.

### Seed admin local

La configuration de développement ne contient plus de mot de passe administrateur en dur.

Pour activer le seed admin en local, définir explicitement la valeur via user-secrets ou variable d'environnement, par exemple :

```bash
dotnet user-secrets set "Initialization:AdminUser:Password" "<mot-de-passe-local>" --project API/AmusementPark.WebAPI/AmusementPark.WebAPI.csproj
```

L'email de développement restant dans `appsettings.Development.json` est un placeholder non personnel :

```json
"Email": "admin@amusement-park.local"
```

### Erreurs HTTP

`ApplicationResultHttpExtensions` utilise maintenant le mapping central `ApplicationResultHttpMapper` au lieu de dupliquer la table de correspondance des statuts HTTP.

Les réponses gardent le contrat legacy minimal :

```json
{
  "statusCode": 400,
  "message": "..."
}
```

### Pagination DataSources

La pagination des résultats de comparaison DataSources reste volontairement zéro-based, car elle est alignée avec les tables PrimeNG côté front.

Une validation minimale bloque désormais les valeurs incohérentes :

- `page < 0` ;
- `pageSize <= 0`.

Le bornage métier existant côté provider est conservé pour ne pas casser l'usage actuel.

## Critères de fin

- aucune trace `Console.WriteLine`, `Debug.WriteLine` ou `Trace.WriteLine` côté back ;
- plus de `catch` nu dans les sources back ;
- les annulations ne sont plus avalées par les handlers applicatifs ;
- les secrets de seed admin ne sont plus présents dans `appsettings.Development.json` ;
- les exceptions globales sont journalisées côté serveur avec une réponse HTTP générique ;
- les contrats HTTP existants restent compatibles côté front.
