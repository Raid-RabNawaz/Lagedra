# ── Stage 1: restore ──────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS restore
WORKDIR /src

# Copy only the files needed for restore (layer cache optimisation)
COPY Directory.Build.props Directory.Build.targets Directory.Packages.props ./
COPY Lagedra.sln ./

# Copy all .csproj files in their correct relative paths
COPY src/Lagedra.ApiGateway/Lagedra.ApiGateway.csproj src/Lagedra.ApiGateway/
COPY src/Lagedra.Worker/Lagedra.Worker.csproj         src/Lagedra.Worker/
COPY src/Lagedra.Auth/Lagedra.Auth.csproj             src/Lagedra.Auth/
COPY src/Lagedra.SharedKernel/Lagedra.SharedKernel.csproj src/Lagedra.SharedKernel/
COPY src/Lagedra.Infrastructure/Lagedra.Infrastructure.csproj src/Lagedra.Infrastructure/
COPY src/Lagedra.TruthSurface/Lagedra.TruthSurface.csproj src/Lagedra.TruthSurface/
COPY src/Lagedra.Compliance/Lagedra.Compliance.csproj src/Lagedra.Compliance/

# Module csproj files
COPY src/Lagedra.Modules/ActivationAndBilling/ActivationAndBilling.csproj         src/Lagedra.Modules/ActivationAndBilling/
COPY src/Lagedra.Modules/IdentityAndVerification/IdentityAndVerification.csproj   src/Lagedra.Modules/IdentityAndVerification/
COPY src/Lagedra.Modules/ListingAndLocation/ListingAndLocation.csproj             src/Lagedra.Modules/ListingAndLocation/
COPY src/Lagedra.Modules/StructuredInquiry/StructuredInquiry.csproj               src/Lagedra.Modules/StructuredInquiry/
COPY src/Lagedra.Modules/VerificationAndRisk/VerificationAndRisk.csproj           src/Lagedra.Modules/VerificationAndRisk/
COPY src/Lagedra.Modules/InsuranceIntegration/InsuranceIntegration.csproj         src/Lagedra.Modules/InsuranceIntegration/
COPY src/Lagedra.Modules/ComplianceMonitoring/ComplianceMonitoring.csproj         src/Lagedra.Modules/ComplianceMonitoring/
COPY src/Lagedra.Modules/Arbitration/Arbitration.csproj                           src/Lagedra.Modules/Arbitration/
COPY src/Lagedra.Modules/JurisdictionPacks/JurisdictionPacks.csproj               src/Lagedra.Modules/JurisdictionPacks/
COPY src/Lagedra.Modules/Evidence/Evidence.csproj                                 src/Lagedra.Modules/Evidence/
COPY src/Lagedra.Modules/Privacy/Privacy.csproj                                   src/Lagedra.Modules/Privacy/
COPY src/Lagedra.Modules/Notifications/Notifications.csproj                       src/Lagedra.Modules/Notifications/
COPY src/Lagedra.Modules/AntiAbuseAndIntegrity/AntiAbuseAndIntegrity.csproj       src/Lagedra.Modules/AntiAbuseAndIntegrity/
COPY src/Lagedra.Modules/ContentManagement/ContentManagement.csproj               src/Lagedra.Modules/ContentManagement/

RUN dotnet restore Lagedra.sln

# ── Stage 2: build ────────────────────────────────────────────────────────────
FROM restore AS build
COPY src/ src/
RUN dotnet build Lagedra.sln -c Release --no-restore

# ── Stage 3: publish API ───────────────────────────────────────────────────────
FROM build AS publish-api
RUN dotnet publish src/Lagedra.ApiGateway/Lagedra.ApiGateway.csproj \
    -c Release --no-build -o /publish/api

# ── Stage 4: publish Worker ───────────────────────────────────────────────────
FROM build AS publish-worker
RUN dotnet publish src/Lagedra.Worker/Lagedra.Worker.csproj \
    -c Release --no-build -o /publish/worker

# ── Stage 5: runtime image ────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Non-root user for security
RUN addgroup --system --gid 1001 appgroup && \
    adduser  --system --uid 1001 --ingroup appgroup appuser

# Copy published output (api by default; override entrypoint for worker)
COPY --from=publish-api --chown=appuser:appgroup /publish/api ./

USER appuser
EXPOSE 8080
ENTRYPOINT ["dotnet", "Lagedra.ApiGateway.dll"]
