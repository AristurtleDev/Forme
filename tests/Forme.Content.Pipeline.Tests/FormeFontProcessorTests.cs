using System;
using System.IO;
using System.Reflection;
using Forme.MonoGame.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Graphics;
using Xunit;

namespace Forme.Content.Pipeline.Tests;

public sealed class FormeFontProcessorTests
{
    private static byte[] LoadEmbeddedTtf()
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        using Stream stream = assembly.GetManifestResourceStream("Inter-Regular.ttf")!;
        using MemoryStream ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }

    private static byte[] BuildFormeBytes(byte[] ttfData)
    {
        FormeFont font = FormeFont.FromTtf(ttfData, CharacterSet.Ascii);
        using MemoryStream ms = new MemoryStream();
        font.Save(ms);
        return ms.ToArray();
    }

    [Fact]
    public void Process_TtfWithAsciiCharset_Produces95Glyphs()
    {
        byte[] ttfData = LoadEmbeddedTtf();
        FormeFontProcessor processor = new FormeFontProcessor();
        processor.CharacterSet = "ASCII";

        FormeFontContent result = processor.Process(ttfData, new StubProcessorContext());

        Assert.Equal(95, result.Glyphs.Count);
    }

    [Fact]
    public void Process_TtfWithRangeCharset_ProducesCorrectGlyphCount()
    {
        byte[] ttfData = LoadEmbeddedTtf();
        FormeFontProcessor processor = new FormeFontProcessor();
        processor.CharacterSet = "65-90";

        FormeFontContent result = processor.Process(ttfData, new StubProcessorContext());

        Assert.Equal(26, result.Glyphs.Count);
    }

    [Fact]
    public void Process_TtfWithBasicLatinCharset_ProducesGlyphs()
    {
        byte[] ttfData = LoadEmbeddedTtf();
        FormeFontProcessor processor = new FormeFontProcessor();
        processor.CharacterSet = "BasicLatin";

        FormeFontContent result = processor.Process(ttfData, new StubProcessorContext());

        Assert.True(result.Glyphs.Count > 0);
    }

    [Fact]
    public void Process_TtfWithLiteralStringCharset_ProducesMatchingGlyphs()
    {
        byte[] ttfData = LoadEmbeddedTtf();
        FormeFontProcessor processor = new FormeFontProcessor();
        processor.CharacterSet = "ABC";

        FormeFontContent result = processor.Process(ttfData, new StubProcessorContext());

        Assert.Equal(3, result.Glyphs.Count);
    }

    [Fact]
    public void Process_FormeFile_LoadsDataAndIgnoresCharset()
    {
        byte[] ttfData = LoadEmbeddedTtf();
        byte[] formeData = BuildFormeBytes(ttfData);
        FormeFontProcessor processor = new FormeFontProcessor();
        processor.CharacterSet = "65-90";

        FormeFontContent result = processor.Process(formeData, new StubProcessorContext());

        // .forme file was built from ASCII, so it has 95 glyphs regardless of CharacterSet
        Assert.Equal(95, result.Glyphs.Count);
    }

    [Fact]
    public void Process_SetsMetricsFromFont()
    {
        byte[] ttfData = LoadEmbeddedTtf();
        FormeFontProcessor processor = new FormeFontProcessor();
        processor.CharacterSet = "ASCII";

        FormeFontContent result = processor.Process(ttfData, new StubProcessorContext());

        Assert.True(result.Metrics.UnitsPerEm > 0);
        Assert.True(result.Metrics.Ascent > 0);
        Assert.True(result.Metrics.Descent < 0);
    }

    [Fact]
    public void Process_PopulatesTextureData()
    {
        byte[] ttfData = LoadEmbeddedTtf();
        FormeFontProcessor processor = new FormeFontProcessor();
        processor.CharacterSet = "ASCII";

        FormeFontContent result = processor.Process(ttfData, new StubProcessorContext());

        Assert.True(result.CurveTextureWidth > 0);
        Assert.True(result.CurveTextureHeight > 0);
        Assert.True(result.CurveTextureData.Length > 0);
        Assert.True(result.BandTextureWidth > 0);
        Assert.True(result.BandTextureHeight > 0);
        Assert.True(result.BandTextureData.Length > 0);
    }

    [Fact]
    public void Process_EmptyCharacterSet_ThrowsInvalidContentException()
    {
        byte[] ttfData = LoadEmbeddedTtf();
        FormeFontProcessor processor = new FormeFontProcessor();
        processor.CharacterSet = "";

        Assert.Throws<InvalidContentException>(() => processor.Process(ttfData, new StubProcessorContext()));
    }

    [Fact]
    public void Process_DefaultCharacterSet_IsAscii()
    {
        FormeFontProcessor processor = new FormeFontProcessor();

        Assert.Equal("ASCII", processor.CharacterSet);
    }

    private sealed class StubProcessorContext : ContentProcessorContext
    {
        public override string BuildConfiguration => string.Empty;
        public override string IntermediateDirectory => string.Empty;
        public override ContentBuildLogger Logger => null!;
        public override ContentIdentity SourceIdentity => null!;
        public override string OutputDirectory => string.Empty;
        public override string OutputFilename => string.Empty;
        public override string ProjectDirectory => string.Empty;
        public override OpaqueDataDictionary Parameters => new OpaqueDataDictionary();
        public override TargetPlatform TargetPlatform => TargetPlatform.DesktopGL;
        public override GraphicsProfile TargetProfile => GraphicsProfile.HiDef;

        public override void AddDependency(string filename) { }
        public override void AddOutputFile(string filename) { }

        [Obsolete]
        public override TOutput BuildAndLoadAsset<TInput, TOutput>(ExternalReference<TInput> sourceAsset, string processorName, OpaqueDataDictionary processorParameters, string importerName)
        {
            throw new NotSupportedException();
        }

        public override TOutput BuildAndLoadAsset<TInput, TOutput>(ExternalReference<TInput> sourceAsset, IContentImporter importer, IContentProcessor processor)
        {
            throw new NotSupportedException();
        }

        [Obsolete]
        public override ExternalReference<TOutput> BuildAsset<TInput, TOutput>(ExternalReference<TInput> sourceAsset, string processorName, OpaqueDataDictionary processorParameters, string importerName, string assetName)
        {
            throw new NotSupportedException();
        }

        public override ExternalReference<TOutput> BuildAsset<TInput, TOutput>(ExternalReference<TInput> sourceAsset, IContentImporter importer, IContentProcessor processor, string? assetName)
        {
            throw new NotSupportedException();
        }

        [Obsolete]
        public override TOutput Convert<TInput, TOutput>(TInput input, string processorName, OpaqueDataDictionary processorParameters)
        {
            throw new NotSupportedException();
        }

        public override TOutput Convert<TInput, TOutput>(TInput input, IContentProcessor processor)
        {
            throw new NotSupportedException();
        }
    }
}
