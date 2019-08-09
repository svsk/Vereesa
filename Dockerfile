FROM microsoft/dotnet:2.1-sdk AS build
WORKDIR /app

# copy csproj and restore as distinct layers
COPY Vereesa.ConsoleApp/*.csproj ./Vereesa.ConsoleApp/
COPY Vereesa.Core/*.csproj ./Vereesa.Core/
COPY Vereesa.Data/*.csproj ./Vereesa.Data/
RUN dotnet restore ./Vereesa.ConsoleApp/Vereesa.ConsoleApp.csproj

# copy and build everything else
COPY Vereesa.ConsoleApp/. ./Vereesa.ConsoleApp/
COPY Vereesa.Core/. ./Vereesa.Core/
COPY Vereesa.Data/. ./Vereesa.Data/
RUN dotnet publish ./Vereesa.ConsoleApp/Vereesa.ConsoleApp.csproj -c Release -o out

FROM microsoft/dotnet:2.1-runtime AS runtime
COPY --from=build /app/Vereesa.ConsoleApp/out .
ENTRYPOINT ["dotnet", "Vereesa.ConsoleApp.dll"]