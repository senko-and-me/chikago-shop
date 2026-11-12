using System;
using ChikagoShop.Domain.Dialogue;
using NUnit.Framework;

namespace ChikagoShop.Domain.Tests;

public sealed class DialogueSessionTests
{
    [Test]
    public void LinesKeepTheirOrderAndCompletionIsEmittedOnlyOnce()
    {
        var session = DialogueSession.FromJson("""[{"speaker":"Френки","text":"Привет"},{"speaker":"Кастелло","text":"Пари"}]""");
        Assert.That(session.Count, Is.EqualTo(2));
        Assert.That(session.Current!.Speaker, Is.EqualTo("Френки"));
        Assert.That(session.Advance(), Is.False);
        Assert.That(session.Current!.Text, Is.EqualTo("Пари"));
        Assert.That(session.Advance(), Is.True);
        Assert.That(session.IsComplete, Is.True);
        Assert.That(session.Current, Is.Null);
        Assert.That(session.Advance(), Is.False);
        Assert.That(session.Index, Is.EqualTo(2));
    }

    [TestCase("[]")]
    [TestCase("null")]
    [TestCase("[null]")]
    [TestCase("[{\"speaker\":null,\"text\":\"x\"}]")]
    [TestCase("[{\"speaker\":\"Френки\"}]")]
    public void InvalidContentCannotStart(string json) =>
        Assert.Throws<ArgumentException>(() => DialogueSession.FromJson(json));

    [Test]
    public void NarrationCanHaveNoSpeaker() =>
        Assert.That(DialogueSession.FromJson("""[{"speaker":"","text":"Френки оборачивается"}]""").Current!.Speaker,
            Is.Empty);

    [Test]
    public void OptionalMusicCuesFollowLinesAndOldContentStaysCompatible()
    {
        var session = DialogueSession.FromJson("""[{"speaker":"Френки","text":"Привет"},{"speaker":"Кастелло","text":"По делу.","musicCue":"thoughtful"},{"speaker":"Кастелло","text":"Не хочу слышать.","musicCue":"sinister"}]""");
        Assert.That(session.Current!.MusicCue, Is.Null);
        session.Advance();
        Assert.That(session.Current!.MusicCue, Is.EqualTo("thoughtful"));
        session.Advance();
        Assert.That(session.Current!.MusicCue, Is.EqualTo("sinister"));
    }
}
