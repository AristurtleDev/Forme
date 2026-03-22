using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Forme.Tests;

public class CharacterSetTests
{
    [Fact]
    public void Ascii_Contains95Codepoints()
    {
        Assert.Equal(95, CharacterSet.Ascii.Count);
    }

    [Fact]
    public void Ascii_StartsAtSpace()
    {
        Assert.Equal(32, CharacterSet.Ascii.Codepoints[0]);
    }

    [Fact]
    public void Ascii_EndsAtTilde()
    {
        Assert.Equal(126, CharacterSet.Ascii.Codepoints[CharacterSet.Ascii.Count - 1]);
    }

    [Fact]
    public void BasicLatin_Contains128Codepoints()
    {
        Assert.Equal(128, CharacterSet.BasicLatin.Count);
    }

    [Fact]
    public void Range_ContainsExpectedCount()
    {
        CharacterSet cs = CharacterSet.Range(65, 90);
        Assert.Equal(26, cs.Count);
    }

    [Fact]
    public void Range_ContainsCorrectValues()
    {
        CharacterSet cs = CharacterSet.Range(65, 68);
        int[] expected = { 65, 66, 67, 68 };
        Assert.Equal(expected, cs.Codepoints.ToArray());
    }

    [Fact]
    public void Range_SingleCodepoint()
    {
        CharacterSet cs = CharacterSet.Range(65, 65);
        Assert.Equal(1, cs.Count);
        Assert.Equal(65, cs.Codepoints[0]);
    }

    [Fact]
    public void Range_ThrowsWhenStartNegative()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CharacterSet.Range(-1, 10));
    }

    [Fact]
    public void Range_ThrowsWhenEndLessThanStart()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CharacterSet.Range(10, 5));
    }

    [Fact]
    public void FromString_Deduplicates()
    {
        CharacterSet cs = CharacterSet.FromString("aab");
        Assert.Equal(2, cs.Count);
    }

    [Fact]
    public void FromString_IsSorted()
    {
        CharacterSet cs = CharacterSet.FromString("zyx");
        int[] codepoints = cs.Codepoints.ToArray();
        Assert.Equal(codepoints.OrderBy(x => x), codepoints);
    }

    [Fact]
    public void FromString_ThrowsOnNull()
    {
        Assert.Throws<ArgumentException>(() => CharacterSet.FromString(null!));
    }

    [Fact]
    public void Combine_UnionsTwoSets()
    {
        CharacterSet a = CharacterSet.Range(65, 67);
        CharacterSet b = CharacterSet.Range(66, 70);
        CharacterSet combined = CharacterSet.Combine(a, b);
        Assert.Equal(6, combined.Count);
        Assert.Equal([65, 66, 67, 68, 69, 70], combined.Codepoints.ToArray());
    }

    [Fact]
    public void Combine_ThrowsOnNull()
    {
        Assert.Throws<ArgumentNullException>(() => CharacterSet.Combine(null!));
    }

    [Fact]
    public void IsEnumerable()
    {
        CharacterSet cs = CharacterSet.Range(65, 67);
        List<int> collected = new List<int>();
        foreach (int cp in cs)
        {
            collected.Add(cp);
        }
        Assert.Equal([65, 66, 67], collected);
    }
}
