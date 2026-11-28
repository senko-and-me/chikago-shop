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
		for (int slot = 0; slot < _showcaseItems.Length; slot++)
		{
			int item = _showcaseItems[slot];
			if (item >= 0 && slot != _pendingCraftSlot)
				unsold += GetItemPrice(item) + QualityBonus(_showcaseGrades[slot]);
		}
		if (ItemIndex >= 0 && TradeActive && _activeShowcaseIndex < 0)
			unsold += GetItemPrice(ItemIndex) + QualityBonus(_activeTradeGrade);
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

	private int CraftRarityShift(string workshop, InvisibleHandDirector director) => 0;

    // Compatibility accessor; selected crafting never rolls a replacement instrument.
    public (int ItemIndex, string InstrumentKind) PreparePolishingItem()
    {
        int index = Crafting.SelectedIndex;
        if (index < 0 || WorkshopItemCatalog.Items[index].WorkshopId != "music")
            throw new System.InvalidOperationException("Choose an instrument in the catalog first.");
        return (index, WorkshopItemCatalog.Items[index].Kind);
    }

}
