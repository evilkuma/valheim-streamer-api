#!/bin/bash

# build.sh
VALHEIM_PATH="${1:-/home/steam/valheim-server}"
OUTPUT_DIR="${2:-./Build}"

echo "=== Сборка Valheim Streamer API ==="

export VALHEIM_INSTALL="$VALHEIM_PATH"

mkdir -p "$OUTPUT_DIR"

cd src
dotnet clean
dotnet build -c Release
if [ $? -ne 0 ]; then
    echo "Ошибка сборки!"
    exit 1
fi
cd ..

echo ""
echo "=== Сборка завершена успешно! ==="
echo "Файлы в: $OUTPUT_DIR"
