# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files first (layer caching)
COPY src/AcquirerFlow.slnx ./src/
COPY src/Services/AcquirerFlow.Authorization/AcquirerFlow.Authorization.Domain/*.csproj ./src/Services/AcquirerFlow.Authorization/AcquirerFlow.Authorization.Domain/
COPY src/Services/AcquirerFlow.Authorization/AcquirerFlow.Authorization.Application/*.csproj ./src/Services/AcquirerFlow.Authorization/AcquirerFlow.Authorization.Application/
COPY src/Services/AcquirerFlow.Authorization/AcquirerFlow.Authorization.Adapters.Driven/*.csproj ./src/Services/AcquirerFlow.Authorization/AcquirerFlow.Authorization.Adapters.Driven/
COPY src/Services/AcquirerFlow.Authorization/AcquirerFlow.Authorization.Adapters.Driving/*.csproj ./src/Services/AcquirerFlow.Authorization/AcquirerFlow.Authorization.Adapters.Driving/
COPY src/Services/AcquirerFlow.Authorization/AcquirerFlow.Authorization.CrossCutting/*.csproj ./src/Services/AcquirerFlow.Authorization/AcquirerFlow.Authorization.CrossCutting/
COPY src/Shared/AcquirerFlow.Contracts/*.csproj ./src/Shared/AcquirerFlow.Contracts/
COPY src/Shared/AcquirerFlow.Infrastructure/*.csproj ./src/Shared/AcquirerFlow.Infrastructure/
# Stub other service csproj (needed by slnx/Infrastructure references)
COPY src/Services/AcquirerFlow.Capture/AcquirerFlow.Capture.Domain/*.csproj ./src/Services/AcquirerFlow.Capture/AcquirerFlow.Capture.Domain/
COPY src/Services/AcquirerFlow.Settlement/AcquirerFlow.Settlement.Domain/*.csproj ./src/Services/AcquirerFlow.Settlement/AcquirerFlow.Settlement.Domain/
COPY src/Services/AcquirerFlow.Reconciliation/AcquirerFlow.Reconciliation.Domain/*.csproj ./src/Services/AcquirerFlow.Reconciliation/AcquirerFlow.Reconciliation.Domain/

RUN dotnet restore src/Services/AcquirerFlow.Authorization/AcquirerFlow.Authorization.Adapters.Driving/AcquirerFlow.Authorization.Adapters.Driving.csproj

# Copy everything and build
COPY . .
RUN dotnet publish src/Services/AcquirerFlow.Authorization/AcquirerFlow.Authorization.Adapters.Driving \
    -c Release -o /app/publish --no-restore

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AcquirerFlow.Authorization.Adapters.Driving.dll"]
