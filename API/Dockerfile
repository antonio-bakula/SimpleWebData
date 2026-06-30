# ---- build ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY SimpleWebData.csproj .
RUN dotnet restore
COPY . .
RUN dotnet publish SimpleWebData.csproj -c Release -o /app /p:UseAppHost=false

# ---- runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
# App sluša HTTP na 8080 (TLS terminira nginx)
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "SimpleWebData.dll"]
