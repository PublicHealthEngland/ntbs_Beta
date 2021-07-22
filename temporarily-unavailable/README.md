# Temporarily unavailable page app
This is a very simple dotnet app with a single static page to be used as a placeholder on the real urls while ntbs is
undergoing maintenance. It is NTBS-branded to allow future users to confidently test future network access to the app.

## Building the app
The temporarily-unavailable folder contains a Dockerfile for building the coming-soon image. Due to the simplicity of it, no
versioning system is used for the images.

We use the same repository for the image as the main app - see `publishTemporarilyUnavailablePageImage.ps1` script for publishing
the image.

## Deploying
The kubernetes definition files for the PHE environment include a set of resources to deploy the app. See comments in
those files for choosing which app (temporarily-unavailable or ntbs-service) are being exposed on which url.