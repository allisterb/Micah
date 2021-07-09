#!/bin/bash

set -e 
cd src/Micah.Web/
dotnet build -c "Debug" $*
cd ../../