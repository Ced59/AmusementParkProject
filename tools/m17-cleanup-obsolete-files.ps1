# M17 cleanup — à lancer depuis le dossier racine AmusementParkProject.
# Le script supprime uniquement des wrappers/doublons/artefacts devenus obsolètes par la centralisation UI/SCSS M17.

$ErrorActionPreference = 'Stop'

$pathsToRemove = @(
    # Artefacts de build Angular à ne pas conserver dans les sources
    'FRONT\AmusementPark\.angular',
    'FRONT\AmusementPark\out-tsc',

    # Wrappers publics remplacés par src/app/ui/*
    'FRONT\AmusementPark\src\app\components\public',

    # Doublons historiques : les vrais guards vivent dans src/app/core/guards
    'FRONT\AmusementPark\src\app\guards',

    # Doublons historiques : les vrais interceptors vivent dans src/app/core/http/interceptors
    'FRONT\AmusementPark\src\app\interceptors',

    # Anciennes helpers dupliquées, remplacées par src/app/shared/utils/*
    'FRONT\AmusementPark\src\app\commons',

    # Ancien callback Google OAuth non routé : le flow actif passe par Google Identity dans l’auth modal
    'FRONT\AmusementPark\src\app\components\login-register\signin-google',
    'FRONT\AmusementPark\src\app\features\auth\state\signin-google-state.facade.ts',

    # Modèle de refresh token inutilisé depuis le refresh HttpOnly cookie
    'FRONT\AmusementPark\src\app\data-access\auth\models\api\refresh-token-request.model.ts',

    # Anciennes sections détail parc non branchées, remplacées par park-detail-view + UI centralisée
    'FRONT\AmusementPark\src\app\features\public\parks\ui\park-hero-section.component.ts',
    'FRONT\AmusementPark\src\app\features\public\parks\ui\park-hero-section.component.html',
    'FRONT\AmusementPark\src\app\features\public\parks\ui\park-hero-section.component.scss',
    'FRONT\AmusementPark\src\app\features\public\parks\ui\park-practical-info-section.component.ts',
    'FRONT\AmusementPark\src\app\features\public\parks\ui\park-practical-info-section.component.html',
    'FRONT\AmusementPark\src\app\features\public\parks\ui\park-practical-info-section.component.scss',

    # Modèles logos historiques remplacés par la centralisation images/logo actuelle
    'FRONT\AmusementPark\src\app\models\parks\park-logo.ts',
    'FRONT\AmusementPark\src\app\models\parks\park-logo-viewmodel.ts',

    # Doublon API admin utilisateurs : source de vérité dans src/app/data-access/users
    'FRONT\AmusementPark\src\app\services\users\user-admin-api.service.ts',

    # Barrel display inutilisé : imports explicites conservés pour éviter un point d’entrée fantôme
    'FRONT\AmusementPark\src\app\shared\utils\display\index.ts',

    # Ancien socle SCSS non importé, remplacé par styles.scss + fichiers M01/M17 réellement consommés
    'FRONT\AmusementPark\src\styles\_patterns.scss',
    'FRONT\AmusementPark\src\styles\_primeng.scss',
    'FRONT\AmusementPark\src\styles\_theme-aware.scss',
    'FRONT\AmusementPark\src\styles\_themes.scss',
    'FRONT\AmusementPark\src\styles\_variables.scss',

    # Dossiers vides hérités de migrations précédentes
    'FRONT\AmusementPark\src\app\components\park-explorer',
    'FRONT\AmusementPark\src\app\components\shared\app-image',
    'FRONT\AmusementPark\src\app\components\shared\shared-image',
    'FRONT\AmusementPark\src\app\services\admin'
)

$removedCount = 0
foreach ($relativePath in $pathsToRemove) {
    $path = Join-Path -Path (Get-Location) -ChildPath $relativePath

    if (Test-Path -LiteralPath $path) {
        Remove-Item -LiteralPath $path -Recurse -Force
        Write-Host "Supprimé : $relativePath"
        $removedCount++
    }
    else {
        Write-Host "Déjà absent : $relativePath"
    }
}

Write-Host "M17 cleanup terminé. Entrées supprimées : $removedCount / $($pathsToRemove.Count)."
