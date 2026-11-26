using ChikagoShop.Domain.Economy;
using Godot;

// Scene adapter for the independent balancing director and displayed and deferred stock.
public partial class TradeScene
{
	public void RecalculateInvisibleHand() => ObserveInvisibleHand(_invisibleHand);

	private void ObserveInvisibleHand(InvisibleHandDirector director)
	{
		if (_dayCycle == null || Failed || Completed) return;
		int stageStartDay = QuotaStage == 0 ? 1 : FirstDeadline + 1;
		int stageLastDay = QuotaStage == 0 ? FirstDeadline : FinalDay;
		int quota = QuotaStage == 0 ? FirstQuota : SecondQuota;
		double unsold = 0;
		foreach (var stock in _deferredStock) unsold += GetItemPrice(stock.Item);
		foreach (int item in _showcaseItems) if (item >= 0) unsold += GetItemPrice(item);
		if (ItemIndex >= 0 && TradeActive && _activeShowcaseIndex < 0)
			unsold += GetItemPrice(ItemIndex);
		double elapsed = _dayCycle.Session.Duration <= 0 ? 0 :
			1 - _dayCycle.Session.RemainingSeconds / _dayCycle.Session.Duration;
		director.Observe(Score, unsold, CheckpointScore,
			CurrentDay, stageStartDay, stageLastDay, elapsed, quota,
			DebugBalanceSettings.Number("hand.enabled") == 1);
	}

    public (int ItemIndex, string InstrumentKind) PreparePolishingItem()
    {
        int index = WorkshopItemCatalog.Roll("music", _rng.RandiRange(0, WorkshopItemCatalog.Count("music") - 1));
        return (index, Items[index].Kind);
    }
}
