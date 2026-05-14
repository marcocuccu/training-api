# BUILD STAGE
# Build the ASP.NET Core API by using official SDK image
# We give the name "build" to this stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# First, copy the .csproj into the container...
COPY ["Training/Training.csproj", "Training/"]
# ... and restore NuGet packages via .csproj
RUN dotnet restore "Training/Training.csproj"

# After, copy the rest of the repository into the container
COPY . .
# Move into API project, compile and publish A RELEASE VERSION
WORKDIR "/src/Training"
RUN dotnet publish "Training.csproj" -c Release -o /app/publish /p:UseAppHost=false


# RUNTIME STAGE
# aspnet image (instead of SDK) containing only the published app and ASP.NET runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Use the same HTTP port that modern .NET container images use by default
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# Copy the build from /app/publish into /app (.), so the final image contains only source code
COPY --from=build /app/publish .
# Execution, equivalent to "dotnet Training.dll" from /app folder.
ENTRYPOINT ["dotnet", "Training.dll"]