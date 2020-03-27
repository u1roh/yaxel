FROM mcr.microsoft.com/dotnet/core/aspnet:3.1
COPY yaxel-server/bin/Release/netcoreapp3.1/publish/ yaxel-server/
COPY yaxel-client/build/ yaxel-client/build/
COPY yaxel-user/ yaxel-user/
ENTRYPOINT ["dotnet", "yaxel-server/yaxel-server.dll"]
EXPOSE 80
