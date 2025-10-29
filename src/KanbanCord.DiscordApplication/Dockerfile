# https://mcr.microsoft.com/en-us/artifact/mar/dotnet/sdk
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build-env

WORKDIR /KanbanCord

COPY ["KanbanCord.DiscordApplication/", "KanbanCord.DiscordApplication/"]
COPY ["KanbanCord.Core/", "KanbanCord.Core/"]
COPY ["Directory.Build.props", "Directory.Build.props"]
COPY ["Directory.Packages.props", "Directory.Packages.props"]

ARG application_version=1.0.0

RUN dotnet publish "KanbanCord.DiscordApplication/KanbanCord.DiscordApplication.csproj" -r linux-musl-x64 --self-contained -c Release -o /publish -p:Version=$application_version


FROM alpine:latest

RUN apk upgrade --no-cache && apk add --no-cache icu-libs

WORKDIR /src

COPY --from=build-env /publish /src

CMD ["./KanbanCord.DiscordApplication"]

HEALTHCHECK CMD wget --quiet --tries=1 --spider http://localhost:5000/health || exit 1