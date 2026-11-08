using System;
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
			Day = CurrentDay,
			Score = Score,
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
			Accepted = Accepted,
			Declined = Declined,
			QuotaStage = QuotaStage,
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
			BuyerCountdown = _buyerCountdown
		};

		ProgressSaveStore.Save(SavePath, data);
	}

	private bool LoadProgress()
	{
		var data = ProgressSaveStore.Load(SavePath, EnergyBeforeMusicUnlock);
		if (data == null) return false;
		data = ProgressMigration.Prepare(data);

		MusicWorkshopUnlocked = data.MusicWorkshopUnlocked;
		CheckpointMusicWorkshopUnlocked = data.CheckpointMusicWorkshopUnlocked;

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
			data.CheckpointMusicPriceLevel);

		_storedItems.Clear();
		_checkpointItems.Clear();

		foreach (int index in data.StoredItems ?? [])
		{
			if (index >= 0 && index < Items.Count)
				_storedItems.Add(index);
		}

		foreach (int index in data.CheckpointItems ?? [])
		{
			if (index >= 0 && index < Items.Count)
				_checkpointItems.Add(index);
		}

		for (int i = 0;
			 i < Mathf.Min(
				 ShowcaseCapacity,
				 data.ShowcaseItems?.Length ?? 0);
			 i++)
		{
			if (data.ShowcaseItems![i] >= 0 &&
				data.ShowcaseItems[i] < Items.Count)
			{
				_showcaseItems[i] = data.ShowcaseItems[i];
			}
		}

		for (int i = 0;
			 i < Mathf.Min(
				 ShowcaseCapacity,
				 data.CheckpointShowcaseItems?.Length ?? 0);
			 i++)
		{
			if (data.CheckpointShowcaseItems![i] >= 0 &&
				data.CheckpointShowcaseItems[i] < Items.Count)
			{
				_checkpointShowcaseItems[i] =
					data.CheckpointShowcaseItems[i];
			}
		}

		MoveItemsOutOfLockedShowcases();

		_buyerCountdown = Array.Exists(
			_showcaseItems, item => item >= 0)
			? double.IsFinite(data.BuyerCountdown) &&
			  data.BuyerCountdown > 0 &&
			  data.BuyerCountdown <= 2
				? data.BuyerCountdown
				: NextBuyerDelay()
			: 0;

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

		CheckpointDay = data.CheckpointDay > 1
			? FirstDeadline + 1
			: 1;

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

		if (DayClosed)
			_buyerCountdown = 0;

		if (_inventoryUI != null)
			_inventoryUI.UpdateUI(_storedItems);

		RefreshShowcases();
		return true;
	}

}
