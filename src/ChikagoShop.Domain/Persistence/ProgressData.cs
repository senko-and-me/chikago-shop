namespace ChikagoShop.Domain.Persistence;

// Versioned wire DTO. Keep property names/defaults compatible with historical saves.
public sealed record ProgressData
{
	public int SaveVersion { get; set; } = 1;
	public bool UpgradePending { get; set; }
	public int SelectedUpgrade { get; set; }
	public int AtelierPriceLevel { get; set; }
	public int MusicPriceLevel { get; set; }
	public int CheckpointAtelierPriceLevel { get; set; }
	public int CheckpointMusicPriceLevel { get; set; }
	public int AtelierUpgradeLevel { get; set; }
	public int MusicUpgradeLevel { get; set; }
	public int CheckpointAtelierUpgradeLevel { get; set; }
	public int CheckpointMusicUpgradeLevel { get; set; }
	public double? DayRemainingSeconds { get; set; }
	public bool DayClosed { get; set; }
	public int Day { get; set; } = 1;
	public int Score { get; set; }

	public int EnergyRemaining { get; set; }

	public int Accepted { get; set; }
	public int Declined { get; set; }
	public int QuotaStage { get; set; }
	public int CheckpointDay { get; set; } = 1;
	public int CheckpointScore { get; set; }

	public int CheckpointEnergy { get; set; }

	public bool MusicWorkshopUnlocked { get; set; }
	public bool CheckpointMusicWorkshopUnlocked { get; set; }
	public int CheckpointAccepted { get; set; }
	public int CheckpointDeclined { get; set; }
	public bool Failed { get; set; }
	public bool Completed { get; set; }
	public string Status { get; set; } = "Продолжайте торги.";
	public bool TradeActive { get; set; }
	public int ItemIndex { get; set; } = -1;
	public int CustomerIndex { get; set; } = -1;
	public int Percentage { get; set; }
	public int RerollsRemaining { get; set; }
	public int[] StoredItems { get; set; } = [];
	public int[] CheckpointItems { get; set; } = [];
	public int[] ShowcaseItems { get; set; } = [];
	public int[] CheckpointShowcaseItems { get; set; } = [];
	public int ActiveShowcaseIndex { get; set; } = -1;
	public double BuyerCountdown { get; set; }
}
