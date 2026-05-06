FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy everything from the context
COPY . .

# Use a shell command to find the project file and restore/publish it. 
# This handles cases where the project is in a subfolder.
RUN dotnet restore $(find . -name "*.csproj" -print -quit)
RUN dotnet publish $(find . -name "*.csproj" -print -quit) -c Release -o /app/publish /p:UseAppHost=false

# Final stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Render dynamic port support
ENV ASPNETCORE_URLS=http://+:10000
EXPOSE 10000

ENTRYPOINT ["dotnet", "ConnectHub.Authentication.dll"]
