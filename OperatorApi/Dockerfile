#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
ARG BuildConfiguration=Release
RUN --mount=type=secret,id=nuget,dst=/pat cp /pat ~/.nuget/NuGet/NuGet.Config
RUN dotnet nuget remove source 'Microsoft Visual Studio Offline Packages' || true
WORKDIR /src
COPY ["nuget.config", "."]
COPY ["src/Apis/OperatorApi/OperatorApi.csproj", "src/Apis/OperatorApi/"]
COPY ["src/Core/DataServices.Tgs/DataServices.Tgs.csproj", "src/Core/DataServices.Tgs/"]
COPY ["src/Core/Models/Models.csproj", "src/Core/Models/"]
COPY ["src/Core/Infrastructures/Infrastructures.csproj", "src/Core/Infrastructures/"]
COPY ["src/Core/DataServices.Base/DataServices.Base.csproj", "src/Core/DataServices.Base/"]
COPY ["src/Repositories/Operator.Repositories/Operator.Repositories.csproj", "src/Repositories/Operator.Repositories/"]
COPY ["src/Services/Operator.Core/Operator.Core.csproj", "src/Services/Operator.Core/"]
COPY ["src/Services/Bucket/PlayerBucket.Core.csproj", "src/Services/Bucket/"]
COPY ["src/Services/Wallet.Core/Wallet.Core.csproj", "src/Services/Wallet.Core/"]
COPY ["src/Clients/Promotion.Client/Promotion.Client.csproj", "src/Clients/Promotion.Client/"]
COPY ["src/Services/Operations/Operations.csproj", "src/Services/Operations/"]
COPY ["src/Repositories/Wallet.Repositories/Wallet.Repositories.csproj", "src/Repositories/Wallet.Repositories/"]
COPY ["src/Libraries/IdentityVerifiers/IdentityVerifiers.csproj", "src/Libraries/IdentityVerifiers/"]
COPY ["src/Repositories/PlayerBucket.Repositories/PlayerBucket.Repositories.csproj", "src/Repositories/PlayerBucket.Repositories/"]
RUN dotnet restore "src/Apis/OperatorApi/OperatorApi.csproj"
COPY . .
WORKDIR "/src/src/Apis/OperatorApi"
RUN dotnet build "OperatorApi.csproj" -c ${BuildConfiguration} -o /app/build

FROM build AS publish
ARG BuildConfiguration
RUN dotnet publish "OperatorApi.csproj" -c ${BuildConfiguration} -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TGS.Apis.OperatorApi.dll"]