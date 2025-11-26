FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["DevOpsProject/DevOpsProject.CommunicationControl.API.csproj", "DevOpsProject.CommunicationControl.API/"]
RUN dotnet restore "DevOpsProject.CommunicationControl.API/DevOpsProject.CommunicationControl.API.csproj"

COPY . .

WORKDIR "/src/DevOpsProject"
RUN dotnet publish "DevOpsProject.CommunicationControl.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://0.0.0.0:8080
ENV RUNNING_IN_DOCKER=true

ENTRYPOINT ["dotnet", "DevOpsProject.CommunicationControl.API.dll"]
