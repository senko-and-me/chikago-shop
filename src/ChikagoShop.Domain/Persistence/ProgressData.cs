namespace ChikagoShop.Domain.Persistence;

public sealed record DisplayStock(int Item, string Grade, int Rerolls);

public enum QuotaVisitStage { Unavailable, AwaitingVisitor, Dialogue, Rewarded }

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
	public double? DayRemainingSeconds { get; set; }
	public bool DayClosed { get; set; }
	// Saves without this additive field already had a running day.
	public bool DayStarted { get; set; } = true;
	public int Day { get; set; } = 1;
	public int Score { get; set; }


	public int Accepted { get; set; }
	public int Declined { get; set; }
	public int QuotaStage { get; set; }
	public double InvisibleHandPressure { get; set; }
	public double CheckpointInvisibleHandPressure { get; set; }
	public bool MafiaNoticePending { get; set; }
	public QuotaVisitStage QuotaVisit { get; set; }
	public QuotaVisitStage CheckpointQuotaVisit { get; set; }
	public int QuotaDialoguePage { get; set; }
	public bool QuotaDialogueRevealed { get; set; }
	public string QuotaDialogueLanguage { get; set; } = "";
	public int CheckpointDay { get; set; } = 1;
	public int CheckpointScore { get; set; }


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
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
	public int[]? StoredItems { get; set; }
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
	public int[]? CheckpointItems { get; set; }
    public DisplayStock[] DeferredDisplayItems { get; set; } = [];
    public DisplayStock[] CheckpointDeferredDisplayItems { get; set; } = [];
    public int[] ShowcaseRerolls { get; set; } = [];
    public int[] CheckpointShowcaseRerolls { get; set; } = [];
    public int? ReturnedOfferPercent { get; set; }
	public int[] ShowcaseItems { get; set; } = [];
	public int[] CheckpointShowcaseItems { get; set; } = [];
	public int ActiveShowcaseIndex { get; set; } = -1;
	public double? BuyerWaitRemaining { get; set; }
	public double? BuyerWaitInitial { get; set; }
	public bool WaitingBuyerPresent { get; set; }
	public bool WaitingBuyerMusic { get; set; }
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
	public string[]? StoredGrades { get; set; }
	public string[] ShowcaseGrades { get; set; } = [];
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
	public string[]? CheckpointGrades { get; set; }
	public string[] CheckpointShowcaseGrades { get; set; } = [];
	public string ActiveTradeGrade { get; set; } = "D";
}
