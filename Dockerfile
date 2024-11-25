# https://hub.docker.com/r/microsoft/dotnet-sdk
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build-env

WORKDIR /KanbanCord

COPY ["KanbanCord.Bot/", "KanbanCord.Bot/"]
COPY ["KanbanCord.Core/", "KanbanCord.Core/"]

RUN dotnet restore "KanbanCord.Bot/KanbanCord.Bot.csproj"
RUN dotnet build "KanbanCord.Bot/KanbanCord.Bot.csproj" -c Release -o /build


FROM build-env AS publish

RUN dotnet publish "KanbanCord.Bot/KanbanCord.Bot.csproj" -p:PublishSingleFile=true -r linux-musl-x64 --self-contained -c Release -o /publish


FROM alpine:latest

ARG application_version=Unknown

ENV APPLICATION_VERSION=$application_version

# Install required packages to make the compiled app work in alpine linux
RUN apk upgrade --no-cache && apk add --no-cache icu-libs

WORKDIR /src

COPY --from=publish /publish /src

CMD ["./KanbanCord.Bot"]