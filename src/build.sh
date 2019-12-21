#!/bin/sh
if [ ! -e ".paket/paket" ]
then
    dotnet tool install paket --tool-path .paket
    .paket/paket init   
fi
.paket/paket restore
# dotnet restore
#dotnet build --no-restore
#dotnet test --no-restore
