# Rough script for quick build-and-release to a particular env
docker build -t ntbscontainerregistry.azurecr.io/temporarily-unavailable ./temporarily-unavailable/
docker push ntbscontainerregistry.azurecr.io/temporarily-unavailable