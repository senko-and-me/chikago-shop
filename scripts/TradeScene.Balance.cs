using System.Collections.Generic;
using ChikagoShop.Domain.Economy;
using Godot;

// Scene adapter for the independent balancing director and existing inventory state.
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
		foreach (int item in _storedItems) unsold += GetItemPrice(item);
		foreach (int item in _showcaseItems) if (item >= 0) unsold += GetItemPrice(item);
		if (ItemIndex >= 0 && (TradeActive || _activeShowcaseIndex >= 0))
			unsold += GetItemPrice(ItemIndex);
		double elapsed = _dayCycle.Session.Duration <= 0 ? 0 :
			1 - _dayCycle.Session.RemainingSeconds / _dayCycle.Session.Duration;
		director.Observe(Score, unsold, CheckpointScore,
			CurrentDay, stageStartDay, stageLastDay, elapsed, quota,
			DebugBalanceSettings.Number("hand.enabled") == 1);
	}

	public IReadOnlyDictionary<string, int> PreviewRarityWeights(string workshop)
	{
		// Preview the next roll without advancing the live director.
		var preview = new InvisibleHandDirector();
		preview.Restore(_invisibleHand.Pressure);
		ObserveInvisibleHand(preview);
		return WorkshopItemCatalog.RarityWeights(workshop, DayUpgrades, CraftRarityShift(workshop, preview));
	}

	private int CraftRarityShift(string workshop, InvisibleHandDirector director) =>
		director.RarityShift(WorkshopItemCatalog.RarityWeightTotal(workshop, DayUpgrades),
			DebugBalanceSettings.Number("hand.rarity"));

    // The polishing mini-game reveals the instrument before acquisition;
    // roll that prepared item through the same hidden rarity adjustment.
    public (int ItemIndex, string InstrumentKind) PreparePolishingItem()
    {
        RecalculateInvisibleHand();
        int hiddenShift = CraftRarityShift("music", _invisibleHand);
        return WorkshopItemCatalog.PreparePolishing(
            GD.RandRange(0, WorkshopItemCatalog.TotalWeight("music", DayUpgrades, hiddenShift) - 1),
            DayUpgrades, hiddenShift);
    }

}
