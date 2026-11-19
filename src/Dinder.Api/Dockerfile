FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source
COPY Directory.Build.props global.json ./
COPY src/Dinder.Api/Dinder.Api.csproj src/Dinder.Api/
RUN --mount=type=secret,id=build_ca,target=/usr/local/share/ca-certificates/dinder-build.crt \
    if [ -f /usr/local/share/ca-certificates/dinder-build.crt ]; then update-ca-certificates; fi \
    && dotnet restore src/Dinder.Api/Dinder.Api.csproj
COPY src/Dinder.Api/ src/Dinder.Api/
RUN dotnet publish src/Dinder.Api/Dinder.Api.csproj -c Release --no-restore -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
RUN mkdir -p /home/app/.aspnet/DataProtection-Keys && chown -R app:app /home/app/.aspnet
COPY --from=build /app ./
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "Dinder.Api.dll"]
