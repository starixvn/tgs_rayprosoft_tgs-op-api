#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["nuget.config", "."]
COPY ["OperatorApi/OperatorApi.csproj", "OperatorApi/"]
COPY ["IdentityVerifiers/IdentityVerifiers.csproj", "IdentityVerifiers/"]
COPY ["Operator.Core/Operator.Core.csproj", "Operator.Core/"]
COPY ["Operations/Operations.csproj", "Operations/"]
COPY ["Operator.Client/Operator.Client.csproj", "Operator.Client/"]
COPY ["Operator.Repositories/Operator.Repositories.csproj", "Operator.Repositories/"]
RUN dotnet restore "./OperatorApi/./OperatorApi.csproj"
COPY . .
WORKDIR "/src/OperatorApi"
RUN dotnet build "./OperatorApi.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./OperatorApi.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TGS.Apis.OperatorApi.dll"]