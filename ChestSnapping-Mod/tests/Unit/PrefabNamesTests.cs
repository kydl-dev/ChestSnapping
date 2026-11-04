using System.Linq;
using ChestSnapping;

namespace ChestSnapping.Tests.Unit;

/// <summary>The prefab-name lists of the config (comma separated, case-insensitive).</summary>
[TestFixture]
public class PrefabNamesTests
{
    [Test]
    public void Parse_SplitsOnCommaAndSemicolon_TrimsAndDropsBlanks()
    {
        var set = PrefabNames.Parse(" a, b ;; c ,");
        Assert.That(set.OrderBy(x => x).ToArray(), Is.EqualTo(new[] { "a", "b", "c" }));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Parse_EmptyText_GivesEmptySet(string csv)
    {
        Assert.That(PrefabNames.Parse(csv), Is.Empty);
    }

    [Test]
    public void Parse_IsCaseInsensitive()
    {
        Assert.That(PrefabNames.Parse("Piece_Chest").Contains("piece_chest"), Is.True);
    }

    [TestCase("piece_chest_wood(Clone)", "piece_chest_wood")]
    [TestCase("piece_chest_wood", "piece_chest_wood")]
    [TestCase("(Clone)", "")]
    [TestCase(null, "")]
    public void StripClone_RemovesTheSuffixOnly(string input, string want)
    {
        Assert.That(PrefabNames.StripClone(input), Is.EqualTo(want));
    }

    [Test]
    public void Append_StartsAListOrExtendsIt()
    {
        Assert.That(PrefabNames.Append("", "x"), Is.EqualTo("x"));
        Assert.That(PrefabNames.Append("a, b,", "x"), Is.EqualTo("a, b, x"));
    }

    [Test]
    public void Remove_DropsTheNameCaseInsensitive_KeepsTheRest()
    {
        Assert.That(PrefabNames.Remove("a, B, c", "b"), Is.EqualTo("a, c"));
        Assert.That(PrefabNames.Remove("a", "a"), Is.EqualTo(""));
        Assert.That(PrefabNames.Remove(null, "a"), Is.EqualTo(""));
    }

    [Test]
    public void AppendThenRemove_RoundTrips()
    {
        string csv = PrefabNames.Append("one", "two");
        Assert.That(PrefabNames.Remove(csv, "two"), Is.EqualTo("one"));
    }
}
