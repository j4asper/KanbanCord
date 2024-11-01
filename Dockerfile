# https://hub.docker.com/r/microsoft/dotnet-sdk
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build-env

WORKDIR /BumpBuddy

COPY ["KanbanCord/", "KanbanCord/"]

RUN dotnet restore "KanbanCord/KanbanCord.csproj"
RUN dotnet build "KanbanCord/KanbanCord.csproj" -c Release -o /build


FROM build-env AS publish

RUN dotnet publish "KanbanCord/KanbanCord.csproj" -p:PublishSingleFile=false -r linux-musl-x64 -c Release -o /publish


# https://hub.docker.com/r/microsoft/dotnet-runtime
FROM mcr.microsoft.com/dotnet/runtime:8.0-alpine

ARG application_version=Unknown

ENV APPLICATION_VERSION=$application_version

WORKDIR /src

COPY --from=publish /publish /src

CMD ["./KanbanCord"]