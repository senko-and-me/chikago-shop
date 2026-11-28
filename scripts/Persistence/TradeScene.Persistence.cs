using System;
using System.Collections.Generic;
using Godot;
using ChikagoShop.Domain.Persistence;

// Maps scene state to/from the wire DTO. No file or migration rules here.
public partial class TradeScene
{
	private void SaveProgress()
	{
		if (string.IsNullOrEmpty(SavePath))
			return;

		var data = new ProgressData
		{
			SaveVersion = ProgressJson.CurrentVersion,
            DeferredDisplayItems = _deferredStock.ToArray(),
            CheckpointDeferredDisplayItems = _checkpointStock.ToArray(),
            ShowcaseRerolls = (int[])_showcaseRerolls.Clone(),
            CheckpointShowcaseRerolls = (int[])_checkpointShowcaseRerolls.Clone(),
            ReturnedOfferPercent = _returnedOfferPercent,
			Day = CurrentDay,
			Score = Score + (Crafting?.RefundablePrice ?? 0),
			EnergyRemaining = EnergyRemaining,
			UpgradePending = _dayCycle.Upgrades.Pending,
			SelectedUpgrade = (int)_dayCycle.Upgrades.Selected,
			AtelierUpgradeLevel = _dayCycle.Upgrades.AtelierLevel,
			MusicUpgradeLevel = _dayCycle.Upgrades.MusicLevel,
			CheckpointAtelierUpgradeLevel =
				_dayCycle.CheckpointAtelierLevel,
			CheckpointMusicUpgradeLevel =
				_dayCycle.CheckpointMusicLevel,
			AtelierPriceLevel =
				DayUpgrades.AtelierPriceLevel,
			MusicPriceLevel =
				DayUpgrades.MusicPriceLevel,
			CheckpointAtelierPriceLevel =
				_dayCycle.CheckpointAtelierPriceLevel,
			CheckpointMusicPriceLevel =
				_dayCycle.CheckpointMusicPriceLevel,
			DayRemainingSeconds = DayRemainingSeconds,
			DayClosed = DayClosed,
			DayStarted = _dayCycle.Started,
			Accepted = Accepted,
			Declined = Declined,
			QuotaStage = QuotaStage,
			InvisibleHandPressure = _invisibleHand.Pressure,
			CheckpointInvisibleHandPressure = _checkpointInvisibleHandPressure,
			MafiaNoticePending = MafiaNoticePending,
			QuotaVisit = _quotaVisit,
			CheckpointQuotaVisit = _checkpointQuotaVisit,
			QuotaDialoguePage = _quotaDialoguePage,
			QuotaDialogueRevealed = _quotaDialogueRevealed,
			QuotaDialogueLanguage = _quotaDialogueLanguage,
			CheckpointDay = CheckpointDay,
			CheckpointScore = CheckpointScore,
			CheckpointEnergy = CheckpointEnergy,
			MusicWorkshopUnlocked = MusicWorkshopUnlocked,
			CheckpointMusicWorkshopUnlocked =
				CheckpointMusicWorkshopUnlocked,
			CheckpointAccepted = CheckpointAccepted,
			CheckpointDeclined = CheckpointDeclined,
			Failed = Failed,
			Completed = Completed,
			Status = StatusText,
			TradeActive = TradeActive,
			ItemIndex = ItemIndex,
			CustomerIndex = CustomerIndex,
			Percentage = Percentage,
			RerollsRemaining = RerollsRemaining,
			StoredItems = _storedItems.ToArray(),
			CheckpointItems = _checkpointItems.ToArray(),
			ShowcaseItems = (int[])_showcaseItems.Clone(),
			CheckpointShowcaseItems =
				(int[])_checkpointShowcaseItems.Clone(),
			ActiveShowcaseIndex = _activeShowcaseIndex,
			BuyerWaitRemaining = _buyerWaitRemaining,
			BuyerWaitInitial = _buyerWaitInitial,
			WaitingBuyerPresent = _waitingBuyer,
			WaitingBuyerItemKind = _waitingBuyer ? Items[_waitingBuyerItem].Kind : null,
			WaitingBuyerMusic = _waitingBuyer && Items[_waitingBuyerItem].WorkshopId == "music",
			StoredGrades = _storedGrades.ToArray(),
			ShowcaseGrades = (string[])_showcaseGrades.Clone(),
			CheckpointGrades = _checkpointGrades.ToArray(),
			CheckpointShowcaseGrades = (string[])_checkpointShowcaseGrades.Clone(),
			ActiveTradeGrade = _activeTradeGrade
		};

		ProgressSaveStore.Save(SavePath, data);
	}

	private bool LoadProgress()
	{
		var data = ProgressSaveStore.Load(SavePath, EnergyBeforeMusicUnlock);
		if (data == null) return false;
		data = ProgressMigration.Prepare(data);

		MusicWorkshopUnlocked = ProgressMigration.StoryWorkshopUnlocked(data.MusicWorkshopUnlocked,
			data.Day, data.QuotaStage, data.QuotaVisit, FirstDeadline);
		CheckpointDay = data.CheckpointDay > 1 ? FirstDeadline + 1 : 1;
		int checkpointQuotaStage = CheckpointDay > FirstDeadline ? 1 : 0;
		CheckpointMusicWorkshopUnlocked = ProgressMigration.StoryWorkshopUnlocked(data.CheckpointMusicWorkshopUnlocked,
			CheckpointDay, checkpointQuotaStage, data.CheckpointQuotaVisit, FirstDeadline);

		_dayCycle.Restore(
			data.DayRemainingSeconds,
			data.DayClosed,
			data.UpgradePending,
			(ChikagoShop.Domain.Days.DayUpgrade)data.SelectedUpgrade,
			data.Day,
			data.AtelierUpgradeLevel,
			data.MusicUpgradeLevel,
			data.CheckpointAtelierUpgradeLevel,
			data.CheckpointMusicUpgradeLevel,
			data.AtelierPriceLevel,
			data.MusicPriceLevel,
			data.CheckpointAtelierPriceLevel,
			data.CheckpointMusicPriceLevel,
			data.DayStarted);

		_storedItems.Clear();
		_storedGrades.Clear();
		_checkpointItems.Clear();
		_checkpointGrades.Clear();
		_activeShowcaseIndex =
			data.ActiveShowcaseIndex >= 0 &&
			data.ActiveShowcaseIndex < ShowcaseCapacity
				? data.ActiveShowcaseIndex
				: -1;

		CurrentDay = Mathf.Clamp(data.Day, 1, FinalDay);
		Score = Mathf.Max(0, data.Score);

		EnergyRemaining = ProgressMigration.Energy(
			data.SaveVersion, data.EnergyRemaining, EnergyCapacity, MusicWorkshopUnlocked);

		Accepted = Mathf.Max(0, data.Accepted);
		Declined = Mathf.Max(0, data.Declined);
		QuotaStage = Mathf.Clamp(data.QuotaStage, 0, 2);
		_invisibleHand.Restore(data.InvisibleHandPressure);
		_checkpointInvisibleHandPressure = double.IsFinite(data.CheckpointInvisibleHandPressure)
			? Math.Clamp(data.CheckpointInvisibleHandPressure, -1, 1) : 0;
		MafiaNoticePending = data.MafiaNoticePending;
		_quotaVisit = ProgressMigration.RestoreQuotaVisit(data.Day, data.QuotaStage, data.QuotaVisit, FirstDeadline);
		_checkpointQuotaVisit = ProgressMigration.RestoreQuotaVisit(CheckpointDay,
			checkpointQuotaStage, data.CheckpointQuotaVisit, FirstDeadline);
		_quotaDialoguePage = data.QuotaDialoguePage;
		_quotaDialogueRevealed = data.QuotaDialogueRevealed;
		_quotaDialogueLanguage = data.QuotaDialogueLanguage;

		CheckpointScore =
			Mathf.Max(0, data.CheckpointScore);

		int checkpointEnergyCapacity =
			CheckpointMusicWorkshopUnlocked
				? EnergyAfterMusicUnlock
				: EnergyBeforeMusicUnlock;

		CheckpointEnergy = Math.Clamp(ProgressMigration.Energy(
			data.SaveVersion, data.CheckpointEnergy, checkpointEnergyCapacity,
			CheckpointMusicWorkshopUnlocked), 0, checkpointEnergyCapacity);

		CheckpointAccepted =
			Mathf.Max(0, data.CheckpointAccepted);

		CheckpointDeclined =
			Mathf.Max(0, data.CheckpointDeclined);

		Failed = data.Failed;
		Completed = data.Completed;
		StatusText = data.Status;
		ItemIndex = data.ItemIndex;
		CustomerIndex = ProgressMigration.CustomerIndex(data);

		Percentage =
			Mathf.Clamp(data.Percentage, -30, 30);

		RerollsRemaining = ProgressMigration.Rerolls(data,
			ItemIndex >= 0 && ItemIndex < Items.Count ? GetItemRerollLimit(ItemIndex) : -1);

		TradeActive = data.TradeActive;
		_activeTradeGrade = NormalizeRank(data.ActiveTradeGrade);
		// Existing buyers carry their original patience across balance changes.
		_buyerWaitInitial = data.BuyerWaitInitial is double initial && double.IsFinite(initial) && initial is >= 1 and <= 600
			? initial : BuyerWaitDuration;
		_buyerWaitRemaining = data.SaveVersion >= 15 && data.BuyerWaitRemaining is double remaining && double.IsFinite(remaining)
			? Math.Clamp(remaining, 0, _buyerWaitInitial) : _buyerWaitInitial;
		_waitingBuyer = data.SaveVersion >= 16 && data.WaitingBuyerPresent && !TradeActive &&
			!data.DayClosed && data.DayStarted && !Failed && !Completed &&
			CustomerIndex >= 0 && CustomerIndex < _customer.GetAppearanceCount();

		if (ItemIndex < 0 ||
			ItemIndex >= Items.Count ||
			CustomerIndex < 0 ||
			CustomerIndex >= _customer.GetAppearanceCount() ||
			Failed ||
			Completed)
		{
			TradeActive = false;
		}

		if (!TradeActive)
			_activeShowcaseIndex = -1;

        _returnedOfferPercent = data.ReturnedOfferPercent is int percent ? Math.Clamp(percent, -30, 30) : null;
        RestoreDisplays(data.ShowcaseItems, data.ShowcaseGrades, data.ShowcaseRerolls,
            data.StoredItems, data.StoredGrades, data.DeferredDisplayItems,
            _showcaseItems, _showcaseGrades, _showcaseRerolls, _deferredStock,
            MusicWorkshopUnlocked, restoreActive: true, savedWithReservedSlot: data.SaveVersion >= 20);
        RestoreDisplays(data.CheckpointShowcaseItems, data.CheckpointShowcaseGrades, data.CheckpointShowcaseRerolls,
            data.CheckpointItems, data.CheckpointGrades, data.CheckpointDeferredDisplayItems,
            _checkpointShowcaseItems, _checkpointShowcaseGrades, _checkpointShowcaseRerolls, _checkpointStock,
            CheckpointMusicWorkshopUnlocked, restoreActive: false);

		_waitingBuyerItem = _waitingBuyer ? RestoreWaitingBuyerRequest(data.WaitingBuyerItemKind, data.WaitingBuyerMusic) : -1;

		if (_inventoryUI != null)
			_inventoryUI.UpdateUI(_storedItems);

		RefreshShowcases();
		return true;
	}

    private void RestoreDisplays(int[] entries, string[] ranks, int[] savedRerolls,
        int[] legacy, string[] legacyRanks, DisplayStock[] deferred,
        int[] items, string[] grades, int[] rerolls, System.Collections.Generic.List<DisplayStock> overflow,
        bool musicOpen, bool restoreActive, bool savedWithReservedSlot = false)
    {
        Array.Fill(items, -1);
        Array.Fill(grades, "D");
        Array.Fill(rerolls, -1);
        overflow.Clear();
        for (int slot = 0; slot < (entries?.Length ?? 0); slot++)
        {
            int item = entries[slot];
            if (item < 0 || item >= Items.Count) continue;
            var stock = new DisplayStock(item, ranks != null && slot < ranks.Length ? ranks[slot] : "D",
                savedRerolls != null && slot < savedRerolls.Length ? savedRerolls[slot] : -1);
            if (slot < ShowcaseCapacity && DisplayWorkshop(slot) == Items[item].WorkshopId &&
                (slot < InitialShowcaseCapacity || musicOpen))
                PutDisplay(slot, stock, items, grades, rerolls);
            else if (!savedWithReservedSlot || !restoreActive || !TradeActive || slot != _activeShowcaseIndex || item != ItemIndex)
                overflow.Add(stock);
        }

        if (restoreActive && TradeActive)
        {
            int slot = _activeShowcaseIndex;
            bool compatible = slot >= 0 && DisplayWorkshop(slot) == Items[ItemIndex].WorkshopId &&
                (slot < InitialShowcaseCapacity || musicOpen);
            if (!compatible || items[slot] >= 0 && (!savedWithReservedSlot || items[slot] != ItemIndex))
                slot = FreeDisplay(Items[ItemIndex].WorkshopId, items, musicOpen);
            _activeShowcaseIndex = slot;
            if (slot >= 0) PutDisplay(slot, new DisplayStock(ItemIndex, _activeTradeGrade, RerollsRemaining),
                items, grades, rerolls);
        }

        for (int i = 0; i < (legacy?.Length ?? 0); i++)
            if (legacy[i] >= 0 && legacy[i] < Items.Count)
                overflow.Add(new DisplayStock(legacy[i], legacyRanks != null && i < legacyRanks.Length ? legacyRanks[i] : "D", -1));
        foreach (var stock in deferred ?? [])
            if (stock != null && stock.Item >= 0 && stock.Item < Items.Count)
                overflow.Add(stock);
        FillDeferredDisplays(overflow, items, grades, rerolls, musicOpen);
    }

}
