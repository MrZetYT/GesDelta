using GesDelta.Agents;

namespace GesDelta.Tests;

public class ContentHasherTests
{
    [Test]
    public void Sha256_KnownVector_Abc()
    {
        var result = ContentHasher.Sha256("abc");

        Assert.That(result, Is.EqualTo(
            "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad"));
    }

    [Test]
    public void Sha256_Cyrillic_IsStable()
    {
        var result = ContentHasher.Sha256("Привет");

        Assert.That(result, Is.EqualTo(
            "dd679c0b9fd408a04148aa7d30c9df393f67b7227f65693fffe0ed6d0f0ade59"));
    }

    [Test]
    public void Sha256_SameText_GivesSameHash()
    {
        Assert.That(ContentHasher.Sha256("Текст"),
            Is.EqualTo(ContentHasher.Sha256("Текст")));
    }

    [Test]
    public void Sha256_OneLetterDifference_GivesDifferentHash()
    {
        Assert.That(ContentHasher.Sha256("Текст"),
            Is.Not.EqualTo(ContentHasher.Sha256("Текс")));
    }

    [Test]
    public void Sha256_IsCaseSensitive()
    {
        // регистр текста не нормализуем (06, раздел 2)
        Assert.That(ContentHasher.Sha256("Abc"),
            Is.Not.EqualTo(ContentHasher.Sha256("abc")));
    }

    [Test]
    public void Sha256_Returns64LowercaseHexChars()
    {
        var result = ContentHasher.Sha256("любой текст");

        Assert.That(result, Has.Length.EqualTo(64));
        Assert.That(result, Is.EqualTo(result.ToLowerInvariant()));
        Assert.That(result, Does.Match("^[0-9a-f]{64}$"));
    }

    [Test]
    public void Sha256_Null_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => ContentHasher.Sha256(null!));
    }

    [Test]
    public void Hash_IgnoresDynamicDateLine()
    {
        var a = ContentHasher.Sha256(TextNormalizer.Normalize("A\nB"));
        var b = ContentHasher.Sha256(
            TextNormalizer.Normalize("A\nОбновлено 08.10.2026\nB"));

        Assert.That(b, Is.EqualTo(a));
    }
}