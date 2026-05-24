# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

COPY AmusementPark.sln ./
COPY API/Directory.Build.props API/Directory.Build.targets ./API/
COPY API/AmusementPark.Core/AmusementPark.Core.csproj ./API/AmusementPark.Core/
COPY API/AmusementPark.Application/AmusementPark.Application.csproj ./API/AmusementPark.Application/
COPY API/AmusementPark.Infrastructure/AmusementPark.Infrastructure.csproj ./API/AmusementPark.Infrastructure/
COPY API/AmusementPark.WebAPI/AmusementPark.WebAPI.csproj ./API/AmusementPark.WebAPI/

RUN dotnet restore ./API/AmusementPark.WebAPI/AmusementPark.WebAPI.csproj

COPY API ./API
RUN dotnet publish ./API/AmusementPark.WebAPI/AmusementPark.WebAPI.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true

RUN apk add --no-cache fontconfig ttf-dejavu \
    && addgroup -S amusementpark \
    && adduser -S amusementpark -G amusementpark
COPY --from=build --chown=amusementpark:amusementpark /app/publish ./

USER amusementpark
EXPOSE 8080
ENTRYPOINT ["dotnet", "AmusementPark.WebAPI.dll"]
