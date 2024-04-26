#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/sdk:8.0-jammy AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY . .
RUN dotnet publish ServiceHost/ServiceHost.csproj -c $BUILD_CONFIGURATION -o /publish/servicehost /p:UseAppHost=false
RUN dotnet publish Services/CGI/CGIService.csproj -c $BUILD_CONFIGURATION -o /publish/cgi
RUN dotnet publish Services/Echo/EchoService.csproj -c $BUILD_CONFIGURATION -o /publish/echo
RUN dotnet publish Services/GRpc/GRpcAdapter/GRpcAdapter.csproj -c $BUILD_CONFIGURATION -o /publish/grpc

FROM mcr.microsoft.com/dotnet/runtime:8.0-jammy
# USER app
WORKDIR /services
COPY --from=build /publish/cgi cgi
COPY --from=build /publish/echo echo
COPY --from=build /publish/grpc grpc
WORKDIR /app
COPY --from=build /publish/servicehost .
ENTRYPOINT ["dotnet", "CloudWorker.ServiceHost.dll"]
