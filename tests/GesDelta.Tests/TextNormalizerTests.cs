using GesDelta.Agents;

namespace GesDelta.Tests;

public class TextNormalizerTests
{
    [TestCase("a  b", "a b")]
    [TestCase("a\r\nb", "a b")]
    [TestCase("a\n\n\n\nb", "a\n\nb")]
    [TestCase("  a  \n\n  b  ", "a\n\nb")]
    [TestCase("a\u00A0b", "a b")]
    [TestCase("ABC abc", "ABC abc")]
    [TestCase("Слово NBSP тут", "Слово NBSP тут")]
    [TestCase("", "")]
    [TestCase("   \n  ", "")]
    public void Normalize_Whitespace_AsExpected(string input, string expected)
    {
        Assert.That(TextNormalizer.Normalize(input), Is.EqualTo(expected));
    }

    [TestCase("Текст\n\nОбновлено 08.10.2026\n\nЕщё", "Текст\n\nЕщё")]
    [TestCase("Updated: 2026-10-08", "")]
    [TestCase("Обновлено 08.10.2026 14:30", "")]
    [TestCase("Обновлено API в версии 2", "Обновлено API в версии 2")]
    [TestCase("Заметка: обновлено 3 раза", "Заметка: обновлено 3 раза")]
    [TestCase("Текст\n\n1234 просмотра", "Текст")]
    [TestCase("10 комментариев", "")]
    [TestCase("5 просмотров вперёд", "5 просмотров вперёд")]
    public void Normalize_DynamicBlocks_AreRemovedOnlyWhenWholeLine(string input, string expected)
    {
        Assert.That(TextNormalizer.Normalize(input), Is.EqualTo(expected));
    }

    [TestCase("Текст\n\nЧитать далее", "Текст")]
    [TestCase("Текст\n\nчитать далее...", "Текст")]
    [TestCase("Читать далее", "")]
    [TestCase("Читать далее\n\nТекст", "Читать далее\n\nТекст")]
    [TestCase("Читать далее\n\nТекст\n\nЧитать далее", "Читать далее\n\nТекст")]
    [TestCase("Рекомендую читать далее", "Рекомендую читать далее")]
    public void Normalize_ReadMoreTail_IsRemovedOnlyAsLastParagraph(string input, string expected)
    {
        Assert.That(TextNormalizer.Normalize(input), Is.EqualTo(expected));
    }

    [TestCase("a  b\r\n\r\n\r\nc")]
    [TestCase("Текст\n\nОбновлено 08.10.2026\n\nЕщё\n\nЧитать далее")]
    [TestCase("  \u00A0 x \u00A0 ")]
    [TestCase("")]
    public void Normalize_AppliedTwice_GivesSameResult(string input)
    {
        var once = TextNormalizer.Normalize(input);

        Assert.That(TextNormalizer.Normalize(once), Is.EqualTo(once));
    }

    [Test]
    public void Normalize_DynamicLineAdded_DoesNotChangeResult()
    {
        var before = TextNormalizer.Normalize("Статья");
        var after = TextNormalizer.Normalize("Статья\n\nОбновлено 08.10");

        Assert.That(after, Is.EqualTo(before));
    }
    
    [Test]
    public void Normalize_DynamicDateInsideParagraph_DoesNotSplitParagraph()
    {
        var without = TextNormalizer.Normalize("A\nB");
        var with = TextNormalizer.Normalize("A\nОбновлено 08.10.2026\nB");

        Assert.That(with, Is.EqualTo(without));
    }
}