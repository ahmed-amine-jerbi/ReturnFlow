FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/ReturnFlow.Api/ReturnFlow.Api.csproj src/ReturnFlow.Api/
RUN dotnet restore src/ReturnFlow.Api/ReturnFlow.Api.csproj
COPY src/ src/
RUN dotnet publish src/ReturnFlow.Api/ReturnFlow.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER app
ENTRYPOINT ["dotnet", "ReturnFlow.Api.dll"]
