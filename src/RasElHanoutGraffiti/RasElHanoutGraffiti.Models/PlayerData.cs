namespace RasElHanoutGraffiti.Models;

public sealed class PlayerData
{
	public string SteamId { get; set; } = "";

	public int Slot1 { get; set; }

	public int Slot2 { get; set; }

	public int Slot3 { get; set; }

	public int ActiveSlot { get; set; } = 1;

	public int UsesThisWeek { get; set; }

	public long WeekStart { get; set; }

	public int TotalUses { get; set; }

	public long LastSprayAt { get; set; }

	public int ActiveDefIndex => GetSlot(ActiveSlot);

	public int GetSlot(int slot)
	{
		return NormalizeSlot(slot) switch
		{
			2 => Slot2, 
			3 => Slot3, 
			_ => Slot1, 
		};
	}

	public void SetSlot(int slot, int defIndex)
	{
		switch (NormalizeSlot(slot))
		{
		case 2:
			Slot2 = defIndex;
			break;
		case 3:
			Slot3 = defIndex;
			break;
		default:
			Slot1 = defIndex;
			break;
		}
	}

	public void SetActiveSlot(int slot)
	{
		ActiveSlot = NormalizeSlot(slot);
	}

	public static int NormalizeSlot(int slot)
	{
		if (slot < 1 || slot > 3)
		{
			return 1;
		}
		return slot;
	}
}
