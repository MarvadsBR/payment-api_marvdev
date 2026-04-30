# ── Build stage ────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["PaymentApi.csproj", "."]
RUN dotnet restore

COPY . .
RUN dotnet build -c Release -o /app/build

# ── Publish stage ───────────────────────────────────────────────────────────────
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish --no-restore

# ── Runtime stage ───────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "PaymentApi.dll"]
