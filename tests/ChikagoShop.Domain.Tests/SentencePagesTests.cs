using ChikagoShop.Domain.Dialogue;
using NUnit.Framework;

namespace ChikagoShop.Domain.Tests;

public sealed class SentencePagesTests
{
    [Test]
    public void NextSentenceMovesAsAWholeAndTextIsPreserved()
    {
        const string text = "Привет. Как твои дела? Всё хорошо!";
        int end = SentencePages.End(text, 0, value => value.Length <= 16);
        Assert.That(text[..end], Is.EqualTo("Привет. "));
        int second = SentencePages.End(text, end, value => value.Length <= 16);
        Assert.That(text[end..second], Is.EqualTo("Как твои дела? "));
        Assert.That(text[..end] + text[end..second] + text[second..], Is.EqualTo(text));
    }

    [TestCase("Очень длинное предложение, которое нельзя разделить.")]
    [TestCase("*Френки оборачивается*")]
    [TestCase("... Прости, что?")]
    [TestCase("Он ответил: «Да!»")]
    public void SingleSentenceOrActionIsNeverCut(string text) =>
        Assert.That(SentencePages.End(text, 0, _ => false), Is.EqualTo(text.Length));
}
