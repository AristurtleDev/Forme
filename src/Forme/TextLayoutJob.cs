// Copyright (c) Christopher Whitley (AristurtleDev). All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Forme;

/// <summary>
/// Describes a full rich-text layout request.
/// </summary>
public sealed class TextLayoutJob
{
    /// <summary>
    /// Gets the full source text.
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// Gets the ordered style sections applied to <see cref="Text"/>.
    /// </summary>
    /// <remarks>
    /// For non-empty text, sections must cover the full source string contiguously from start to
    /// end with no gaps or overlaps.
    /// </remarks>
    public IReadOnlyList<TextSection> Sections { get; }

    /// <summary>
    /// Gets the global layout options applied to the job.
    /// </summary>
    public TextLayoutOptions LayoutOptions { get; }

    /// <summary>
    /// Initializes a new <see cref="TextLayoutJob"/>.
    /// </summary>
    /// <param name="text">The full source text.</param>
    /// <param name="sections">The ordered style sections applied to the text.</param>
    public TextLayoutJob(string text, IReadOnlyList<TextSection> sections)
        : this(text, sections, default)
    {
    }

    /// <summary>
    /// Initializes a new <see cref="TextLayoutJob"/>.
    /// </summary>
    /// <param name="text">The full source text.</param>
    /// <param name="sections">The ordered style sections applied to the text.</param>
    /// <param name="layoutOptions">The global layout options applied to the job.</param>
    public TextLayoutJob(string text, IReadOnlyList<TextSection> sections, TextLayoutOptions layoutOptions)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(sections);

        Text = text;
        LayoutOptions = layoutOptions;

        TextSection[] copy = new TextSection[sections.Count];
        for (int i = 0; i < sections.Count; i++)
        {
            copy[i] = sections[i];
        }

        ValidateSections(text.Length, copy);
        Sections = copy;
    }

    /// <summary>
    /// Creates a plain-text job containing one section that spans the entire source text.
    /// </summary>
    /// <param name="text">The source text.</param>
    /// <param name="format">The format applied to the full text.</param>
    public static TextLayoutJob CreatePlain(string text, TextFormat format)
    {
        return CreatePlain(text, format, default);
    }

    /// <summary>
    /// Creates a plain-text job containing one section that spans the entire source text.
    /// </summary>
    /// <param name="text">The source text.</param>
    /// <param name="format">The format applied to the full text.</param>
    /// <param name="layoutOptions">The global layout options applied to the job.</param>
    public static TextLayoutJob CreatePlain(string text, TextFormat format, TextLayoutOptions layoutOptions)
    {
        ArgumentNullException.ThrowIfNull(text);

        if (text.Length == 0)
        {
            return new TextLayoutJob(text, Array.Empty<TextSection>(), layoutOptions);
        }

        return new TextLayoutJob(text, new TextSection[] { new TextSection(0, text.Length, format) }, layoutOptions);
    }

    private static void ValidateSections(int textLength, IReadOnlyList<TextSection> sections)
    {
        if (sections.Count == 0)
        {
            if (textLength != 0)
            {
                throw new ArgumentException("Non-empty text layout jobs require at least one section.", nameof(sections));
            }

            return;
        }

        int previousEnd = 0;

        for (int i = 0; i < sections.Count; i++)
        {
            TextSection section = sections[i];

            if (i == 0 && section.TextStart != 0)
            {
                throw new ArgumentException("Text sections must start at the beginning of the source text.", nameof(sections));
            }

            if (section.TextEnd > textLength)
            {
                throw new ArgumentException("Text section extends past the end of the source text.", nameof(sections));
            }

            if (i > 0 && section.TextStart < previousEnd)
            {
                throw new ArgumentException("Text sections must be ordered and non-overlapping.", nameof(sections));
            }

            if (i > 0 && section.TextStart != previousEnd)
            {
                throw new ArgumentException("Text sections must cover the source text contiguously with no gaps.", nameof(sections));
            }

            previousEnd = section.TextEnd;
        }

        if (previousEnd != textLength)
        {
            throw new ArgumentException("Text sections must cover the full source text.", nameof(sections));
        }
    }
}
