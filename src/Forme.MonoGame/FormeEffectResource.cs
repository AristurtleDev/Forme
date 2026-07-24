// Copyright (c) Christopher Whitley (AristurtleDev). All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Reflection;
using MonoGame.Framework.Utilities;

namespace Forme.MonoGame;

internal static class FormeEffectResource
{
    private static volatile byte[]? s_bytecode;

    internal static byte[] GetBytecode()
    {
        if (s_bytecode != null)
        {
            return s_bytecode;
        }

        lock (typeof(FormeEffectResource))
        {
            if (s_bytecode != null)
            {
                return s_bytecode;
            }

            string extension = GetShaderExtension();
            string resourceName = $"Forme.MonoGame.Shaders.FormeShader.{extension}.mgfxo";

            Assembly assembly = typeof(FormeEffectResource).Assembly;

            Stream? stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                throw new InvalidOperationException(
                    $"Embedded shader resource '{resourceName}' was not found in assembly '{assembly.FullName}'. " +
                    "Ensure the .mgfxo file was compiled and embedded as an EmbeddedResource.");
            }

            using (stream)
            using (MemoryStream ms = new MemoryStream())
            {
                stream.CopyTo(ms);
                s_bytecode = ms.ToArray();
            }
        }

        return s_bytecode;
    }

    private static string GetShaderExtension()
    {
        switch (PlatformInfo.GraphicsBackend)
        {
            case GraphicsBackend.OpenGL:
                return "ogl";
            case GraphicsBackend.DirectX:
                return "dx11";
            case GraphicsBackend.DirectX12:
                return "dx12";
            case GraphicsBackend.Vulkan:
                return "vk";
            default:
                throw new NotSupportedException(
                    $"The MonoGame graphics backend '{PlatformInfo.GraphicsBackend}' is not supported by the embedded Forme shader resources.");
        }
    }
}
