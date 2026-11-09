# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY . .
RUN dotnet restore "SMTManagement.WebAPI/SMTManagement.WebAPI.csproj"
RUN dotnet publish "SMTManagement.WebAPI/SMTManagement.WebAPI.csproj" \
	--configuration Release \
	--output /app/publish \
	--no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

RUN mkdir -p /app/data
COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Container \
	ASPNETCORE_HTTP_PORTS=8080

EXPOSE 8080
ENTRYPOINT ["dotnet", "SMTManagement.WebAPI.dll"]
