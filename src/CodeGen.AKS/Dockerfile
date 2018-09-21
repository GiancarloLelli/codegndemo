FROM microsoft/dotnet:2.1-aspnetcore-runtime AS base
WORKDIR /app
EXPOSE 52677
EXPOSE 44324

FROM microsoft/dotnet:2.1-sdk AS build
WORKDIR /src
COPY ["CodeGen.AKS/CodeGen.AKS.csproj", "src/CodeGen.AKS/"]
RUN dotnet restore "src/CodeGen.AKS/CodeGen.AKS.csproj"
COPY . .
WORKDIR "src/src/CodeGen.AKS"
RUN dotnet build "CodeGen.AKS.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "CodeGen.AKS.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "CodeGen.AKS.dll"]