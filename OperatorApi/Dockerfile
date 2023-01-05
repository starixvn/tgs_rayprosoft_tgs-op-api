#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
ARG BuildConfiguration=Release
WORKDIR /src
COPY ["nuget.config", "."]
COPY ["OperatorApi/OperatorApi.csproj", "OperatorApi/"]
COPY ["IdentityVerifiers/IdentityVerifiers.csproj", "IdentityVerifiers/"]
COPY ["Operator.Core/Operator.Core.csproj", "Operator.Core/"]
COPY ["Operations/Operations.csproj", "Operations/"]
COPY ["Operator.Repositories/Operator.Repositories.csproj", "Operator.Repositories/"]
RUN dotnet restore "OperatorApi/OperatorApi.csproj"
COPY . .
WORKDIR "/src/OperatorApi"
RUN dotnet build "OperatorApi.csproj" -c $BuildConfiguration -o /app/build

FROM build AS publish
ARG BuildConfiguration
RUN dotnet publish "OperatorApi.csproj" -c $BuildConfiguration -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TGS.Apis.OperatorApi.dll"]