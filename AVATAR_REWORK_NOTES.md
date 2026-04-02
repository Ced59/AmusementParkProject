# Avatar rework notes

## Included
- Social avatar import now goes through the existing image pipeline (compression + MinIO + Mongo image metadata + link as current image).
- User avatar upload from profile is implemented with a reusable shared drag & drop dialog component.
- Existing image endpoints are reused for profile upload/linking.
- Image link/current/delete endpoints now allow a normal USER to manage their own avatar images.
- `User.AvatarUrl` is synchronized from current avatar image metadata.
- `PUT /users/{id}` no longer clears `AvatarUrl` when the front does not send it.
- `PUT /users/{id}` now returns `AvatarUrl` too.
- Topbar now reloads the current user profile from the API so avatar changes are reflected immediately.

## Main added files
- `API/Services/Services/Implementations/Images/UserAvatarService.cs`
- `API/Services/Services/Interfaces/Images/IUserAvatarService.cs`
- `API/Services/Services/Models/Images/ImageSaveRequest.cs`
- `FRONT/AmusementPark/src/app/components/shared/owner-image-upload-dialog/*`

## Notes
- Front TypeScript typecheck was run successfully with `tsc --noEmit`.
- Full Angular build could not be run in this Linux container because the uploaded `node_modules` contains the Windows esbuild binary.
- .NET build could not be run here because `dotnet` is not installed in the container.
