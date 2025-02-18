FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["eCommerce.API/eCommerce.API.csproj", "eCommerce.API/"]
RUN dotnet restore "eCommerce.API/eCommerce.API.csproj"

# Copy the rest of the code
COPY . .
WORKDIR "/src/eCommerce.API"

# Build and publish
RUN dotnet build "eCommerce.API.csproj" -c Release -o /app/build
RUN dotnet publish "eCommerce.API.csproj" -c Release -o /app/publish

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Create directory for Firebase credentials
RUN mkdir -p /app/config

# Expose port
EXPOSE 80
EXPOSE 443

ENTRYPOINT ["dotnet", "eCommerce.API.dll"]