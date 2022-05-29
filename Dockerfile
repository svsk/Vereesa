FROM mcr.microsoft.com/dotnet/aspnet:5.0-buster-slim AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:5.0-buster-slim AS build
WORKDIR /src
COPY ["Vereesa.ConsoleApp/Vereesa.ConsoleApp.csproj", "Vereesa.ConsoleApp/"]
COPY ["Vereesa.Awdeo/Vereesa.Awdeo.csproj", "Vereesa.Awdeo/"]
COPY ["Vereesa.Core/Vereesa.Core.csproj", "Vereesa.Core/"]
COPY ["Vereesa.Data/Vereesa.Data.csproj", "Vereesa.Data/"]
RUN dotnet restore "./Vereesa.ConsoleApp/Vereesa.ConsoleApp.csproj"

COPY ["Vereesa.ConsoleApp/.", "./Vereesa.ConsoleApp/"]
COPY ["Vereesa.Core/.", "./Vereesa.Core/"]
COPY ["Vereesa.Data/.", "./Vereesa.Data/"]
COPY ["Vereesa.Awdeo/.", "./Vereesa.Awdeo/"]

WORKDIR "Vereesa.ConsoleApp"
RUN dotnet build "Vereesa.ConsoleApp.csproj" --no-restore -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Vereesa.ConsoleApp.csproj" --no-restore -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Vereesa.ConsoleApp.dll"]