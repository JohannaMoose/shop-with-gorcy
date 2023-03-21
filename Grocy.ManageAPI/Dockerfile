#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Grocy.ManageAPI/Grocy.ManageAPI.csproj", "Grocy.ManageAPI/"]
RUN dotnet restore "Grocy.ManageAPI/Grocy.ManageAPI.csproj"
COPY . .
WORKDIR "/src/Grocy.ManageAPI"
RUN dotnet build "Grocy.ManageAPI.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Grocy.ManageAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Grocy.ManageAPI.dll"]