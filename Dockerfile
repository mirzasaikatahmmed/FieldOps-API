FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY FieldOps.sln ./
COPY COMMON/FieldOps.COMMON.csproj COMMON/
COPY DAL/FieldOps.DAL.csproj DAL/
COPY BLL/FieldOps.BLL.csproj BLL/
COPY API/FieldOps.API.csproj API/

RUN dotnet restore API/FieldOps.API.csproj

COPY COMMON/ COMMON/
COPY DAL/ DAL/
COPY BLL/ BLL/
COPY API/ API/

# Restore again after source copy so host bin/obj cannot poison assets
RUN dotnet publish API/FieldOps.API.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "FieldOps.API.dll"]
