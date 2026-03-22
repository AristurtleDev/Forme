// Copyright (c) Christopher Whitley (AristurtleDev). All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.IO;
using Microsoft.Xna.Framework.Content.Pipeline;

namespace Forme.MonoGame.Content.Pipeline;

/// <summary>
/// Imports a pre-processed <c>.forme</c> file as raw bytes for processing by
/// <see cref="FormeFontProcessor"/>.
/// </summary>
[ContentImporter(".forme", DisplayName = "Forme File Importer", DefaultProcessor = "FormeFontProcessor")]
public sealed class FormeFileImporter : ContentImporter<byte[]>
{
    /// <summary>
    /// Reads the .forme file at <paramref name="filename"/> and returns its raw bytes.
    /// </summary>
    public override byte[] Import(string filename, ContentImporterContext context)
    {
        return File.ReadAllBytes(filename);
    }
}
