FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /app

# csproj and restore dependencies
COPY WorkflowEngine.csproj ./
RUN dotnet restore

# everything else and build
COPY . ./
RUN dotnet publish -c Release -o out

# runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# the built application
COPY --from=build-env /app/out .

# directory for data file
RUN mkdir -p /app/data

# Expose port
EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "WorkflowEngine.dll"]