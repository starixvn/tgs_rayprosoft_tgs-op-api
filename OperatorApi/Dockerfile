ARG CI_REGISTRY_IMAGE

FROM $CI_REGISTRY_IMAGE:builder as deps

RUN mkdir -p /src/packages

FROM hub.nexdev.net/base/dotnet/core/sdk:8.0-alpine AS builder

ENV NUGET_PACKAGES=/src/packages
COPY --from=deps $NUGET_PACKAGES/. $NUGET_PACKAGES/

ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["nuget.config", "."]
COPY ["OperatorApi/OperatorApi.csproj", "OperatorApi/"]
COPY ["IdentityVerifiers/IdentityVerifiers.csproj", "IdentityVerifiers/"]
COPY ["Operator.Core/Operator.Core.csproj", "Operator.Core/"]
COPY ["Operations/Operations.csproj", "Operations/"]
COPY ["Operator.Client/Operator.Client.csproj", "Operator.Client/"]
COPY ["Operator.Repositories/Operator.Repositories.csproj", "Operator.Repositories/"]
RUN dotnet restore -v n -r linux-x64 "./OperatorApi/./OperatorApi.csproj"
COPY . .
WORKDIR "/src/OperatorApi"
RUN dotnet build "./OperatorApi.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM builder AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish -r linux-x64 --self-contained false "./OperatorApi.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM hub.lumigame.com/base/dotnet/core/aspnet:8.0-alpine-24h1.1 AS base
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TGS.Apis.OperatorApi.dll"]