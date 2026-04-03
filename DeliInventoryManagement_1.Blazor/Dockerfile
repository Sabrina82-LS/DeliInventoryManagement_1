FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["DeliInventoryManagement_1.Blazor/DeliInventoryManagement_1.Blazor.csproj", "DeliInventoryManagement_1.Blazor/"]
COPY ["DeliInventoryManagement_1.Api/DeliInventoryManagement_1.Api.csproj", "DeliInventoryManagement_1.Api/"]
RUN dotnet restore "DeliInventoryManagement_1.Blazor/DeliInventoryManagement_1.Blazor.csproj"
COPY . .
WORKDIR "/src/DeliInventoryManagement_1.Blazor"
RUN dotnet build -c Release -o /app/build
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "DeliInventoryManagement_1.Blazor.dll"]