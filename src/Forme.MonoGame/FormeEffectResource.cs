// Copyright (c) Christopher Whitley (AristurtleDev). All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Reflection;
using Microsoft.Xna.Framework;

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
        Assembly frameworkAssembly = typeof(Game).Assembly;

        Type? shaderType = frameworkAssembly.GetType("Microsoft.Xna.Framework.Graphics.Shader");
        if (shaderType == null)
        {
            throw new InvalidOperationException(
                "Cannot locate Microsoft.Xna.Framework.Graphics.Shader in the MonoGame assembly.");
        }

        PropertyInfo? profileProperty = shaderType.GetProperty("Profile", BindingFlags.Public | BindingFlags.Static);

        if (profileProperty == null)
        {
            throw new InvalidOperationException("Cannot locate Shader.Profile static property in the MonoGame assembly.");
        }

        object? value = profileProperty.GetValue(null);
        if (value == null)
        {
            throw new InvalidOperationException("Shader.Profile returned null.");
        }

        int profile = (int)value;

        switch (profile)
        {
            case 0:
                return "ogl";
            case 1:
                return "dx11";
            default:
                throw new InvalidOperationException($"Unknown MonoGame shader profile value: {profile}.");
        }
    }
}
