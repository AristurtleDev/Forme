#!/bin/bash

SHADERS_DIR="./Shaders"
MGFXC="dotnet mgfxc"

if [ ! -d "$SHADERS_DIR" ]; then
    echo "Error: Directory $SHADERS_DIR not found."
    exit 1
fi

for file in "$SHADERS_DIR"/*.fx; do
    if [ -f "$file" ]; then
        filename=$(basename "$file" .fx)

        echo "Compiling $filename (OpenGL)..."
        $MGFXC "$SHADERS_DIR/$filename.fx" "$SHADERS_DIR/$filename.ogl.mgfxo" /Profile:OpenGL

        echo "Compiling $filename (DirectX 11)..."
        $MGFXC "$SHADERS_DIR/$filename.fx" "$SHADERS_DIR/$filename.dx11.mgfxo" /Profile:DirectX_11
    fi
done

read -p "Press enter to continue"
