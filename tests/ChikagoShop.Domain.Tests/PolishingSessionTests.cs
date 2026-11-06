using ChikagoShop.Domain.Crafting;
using NUnit.Framework;

namespace ChikagoShop.Domain.Tests;

public sealed class PolishingSessionTests
{
    private static PolishingSession Full(int width=100,int height=100) =>
        new(Enumerable.Repeat((byte)255,width*height).ToArray(),width,height);

    [Test]
    public void HoldingStillOrMovingWithoutAHeldPointerDoesNotClean()
    {
        var session=Full();
        session.BeginRub(40,40); session.MoveRub(60,40,8);
        Assert.That(session.Progress,Is.Zero);
        session.Start(); session.MoveRub(60,40,8);
        Assert.That(session.Progress,Is.Zero);
        session.BeginRub(40,40);
        for(int i=0;i<100;i++) session.MoveRub(40,40,8);
        Assert.That(session.Progress,Is.Zero);
        session.EndRub(); session.MoveRub(60,40,8);
        Assert.That(session.Progress,Is.Zero);
    }

    [Test]
    public void RepeatedRubbingRemovesDirtBeforeTheMatteFilm()
    {
        var session=Full(28,28); session.Start(); session.BeginRub(10,14);
        session.MoveRub(18,14,7);
        Assert.That(session.DirtProgress,Is.GreaterThan(0));
        Assert.That(session.Cells.Where(cell=>cell.Inside).All(cell=>cell.Film==1),Is.True);
        Assert.That(session.Complete,Is.False);
        for(int i=0;i<150 && !session.Complete;i++) session.MoveRub(i%2==0?10:18,14,7);
        Assert.That(session.Complete,Is.True);
        Assert.That(session.Progress,Is.GreaterThanOrEqualTo(.85));
        Assert.That(session.Cells.Any(cell=>cell.Film<1),Is.True);
        Assert.That(session.State,Is.EqualTo(EmbroideryState.Finished));
        Assert.That(session.Rank,Is.EqualTo("S"));
        double progress=session.Progress;
        session.Start(); session.MoveRub(15,14,7); session.Tick(100);
        Assert.That(session.Progress,Is.EqualTo(progress));
        Assert.That(session.RemainingSeconds,Is.EqualTo(10));
    }

    [Test]
    public void FasterMotionAndReversalIncreasePolishingPower()
    {
        var fast=Full(); var slow=Full();
        foreach(var session in new[]{fast,slow}) { session.Start(); session.BeginRub(40,50); }
        fast.MoveRub(60,50,7); slow.MoveRub(60,50,500);
        Assert.That(fast.Progress,Is.GreaterThan(slow.Progress));
        var continuous=Full(); var broken=Full();
        foreach(var session in new[]{continuous,broken}) { session.Start(); session.BeginRub(40,50); session.MoveRub(60,50,7); }
        broken.EndRub(); broken.BeginRub(60,50);
        continuous.MoveRub(40,50,7); broken.MoveRub(40,50,7);
        Assert.That(continuous.Progress,Is.GreaterThan(broken.Progress));
    }

    [Test]
    public void LargeCursorJumpsCannotClearAnArbitrarySweep()
    {
        var session=Full(600,480); session.Start(); session.BeginRub(10,240); session.MoveRub(590,240,7);
        Assert.That(session.Cells.Single(cell=>cell.X==297.5 && cell.Y==241.5).Dirt,Is.EqualTo(1));
        Assert.That(session.Progress,Is.LessThan(.03));
    }

    [Test]
    public void EdgeToleranceAndCooldownPreventRepeatedOutsidePenalties()
    {
        byte[] mask=new byte[100*100];
        for(int y=30;y<70;y++) for(int x=30;x<70;x++) mask[y*100+x]=255;
        var session=new PolishingSession(mask,100,100); session.Start();
        session.BeginRub(0,50);
        Assert.That(session.Misses,Is.Zero);
        session.MoveRub(-60,50,8);
        Assert.That(session.Misses,Is.EqualTo(1));
        session.Tick(.3); session.MoveRub(-80,50,8);
        Assert.That(session.Misses,Is.EqualTo(1));
        session.MoveRub(50,50,8); session.MoveRub(-60,50,8);
        Assert.That(session.Misses,Is.EqualTo(2));
        session.MoveRub(50,50,8); session.MoveRub(-60,50,8);
        Assert.That(session.Misses,Is.EqualTo(2));
    }

    [Test]
    public void TimeoutInvalidValuesAndMalformedMasksAreSafe()
    {
        var session=Full(); session.Tick(10);
        Assert.That(session.RemainingSeconds,Is.EqualTo(10));
        session.Start(); session.BeginRub(50,50);
        session.Tick(double.NaN); session.Tick(-1); session.Tick(double.PositiveInfinity);
        session.MoveRub(double.NaN,50,8); session.MoveRub(60,50,double.NaN);
        Assert.That(session.Progress,Is.Zero);
        session.Tick(10);
        Assert.That(session.State,Is.EqualTo(EmbroideryState.Finished));
        Assert.That(session.Rank,Is.EqualTo("D"));
        session.MoveRub(60,50,8);
        Assert.That(session.Progress,Is.Zero);
        Assert.Throws<ArgumentException>(()=>new PolishingSession(new byte[1]));
        Assert.Throws<ArgumentException>(()=>new PolishingSession(new byte[100],10,10));
    }
}
