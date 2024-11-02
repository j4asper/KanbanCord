# https://hub.docker.com/r/microsoft/dotnet-sdk
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build-env

WORKDIR /BumpBuddy

COPY ["KanbanCord/", "KanbanCord/"]

RUN dotnet restore "KanbanCord/KanbanCord.csproj"
RUN dotnet build "KanbanCord/KanbanCord.csproj" -c Release -o /build


FROM build-env AS publish

RUN dotnet publish "KanbanCord/KanbanCord.csproj" -p:PublishSingleFile=true -r linux-musl-x64 --self-contained -c Release -o /publish


FROM alpine:latest

ARG application_version=Unknown

ENV APPLICATION_VERSION=$application_version

# Install required packages to make the compiled app work in alpine linux
RUN apk upgrade --no-cache && apk add --no-cache libgcc icu-libs

WORKDIR /src

COPY --from=publish /publish /src

CMD ["./KanbanCord"]