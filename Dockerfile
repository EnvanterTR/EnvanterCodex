FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source
COPY global.json ./
COPY src/EnvanterTR.Web/EnvanterTR.Web.csproj src/EnvanterTR.Web/
RUN dotnet restore src/EnvanterTR.Web/EnvanterTR.Web.csproj
COPY src/EnvanterTR.Web/ src/EnvanterTR.Web/
RUN dotnet publish src/EnvanterTR.Web/EnvanterTR.Web.csproj -c Release --no-restore -o /out \
    && chmod -R a+rX /out

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/* \
    && mkdir -p /var/lib/envantertr/company-media /var/lib/envantertr/data-protection-keys \
    && chown -R $APP_UID:$APP_UID /var/lib/envantertr
COPY --from=build /out ./
USER $APP_UID
HEALTHCHECK --interval=30s --timeout=5s --start-period=60s --retries=3 \
    CMD curl --fail --silent --show-error http://127.0.0.1:8080/health/ready || exit 1
ENTRYPOINT ["dotnet", "EnvanterTR.Web.dll"]
