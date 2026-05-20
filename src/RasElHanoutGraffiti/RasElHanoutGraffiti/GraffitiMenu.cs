using System;
using System.Collections.Generic;
using System.Linq;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Utils;
using RasElHanoutGraffiti.Models;

namespace RasElHanoutGraffiti;

public sealed class GraffitiMenu
{
	private static readonly string[] ColorSuffixes = new string[19]
	{
		"Battle Green", "Bazooka Pink", "Blood Red", "Brick Red", "Cash Green", "Desert Amber", "Dust Brown", "Frog Green", "Jungle Green", "Monarch Blue",
		"Monster Purple", "Princess Pink", "Shark White", "SWAT Blue", "Tiger Orange", "Tracer Yellow", "Violent Violet", "War Pig Pink", "Wire Blue"
	};

	private readonly RasElHanoutGraffiti _plugin;

	public GraffitiMenu(RasElHanoutGraffiti plugin)
	{
		_plugin = plugin;
	}

	public void Open(CCSPlayerController player)
	{
		if (_plugin.Catalog.Count == 0)
		{
			player.PrintToChat($"{Prefix()}{ChatColors.Red}No graffiti catalog loaded.");
			return;
		}
		PlayerData playerData = _plugin.Manager.LoadFresh(player.SteamID.ToString());
		CenterHtmlMenu centerHtmlMenu = new CenterHtmlMenu("Ras El Hanout Graffiti", _plugin)
		{
			PostSelectAction = PostSelectAction.Close,
			ExitButton = true
		};
		centerHtmlMenu.AddMenuOption($"Active slot: {playerData.ActiveSlot}", delegate
		{
		}, disabled: true);
		for (int num = 1; num <= 3; num++)
		{
			int slotCopy = num;
			GraffitiDef entry = _plugin.FindGraffiti(playerData.GetSlot(num));
			string text = $"Slot {num}: {DisplayName(entry) ?? "Empty"}";
			if (num == playerData.ActiveSlot)
			{
				text = "* " + text;
			}
			centerHtmlMenu.AddMenuOption(text, delegate(CCSPlayerController target, ChatMenuOption _)
			{
				OpenPicker(target, slotCopy);
			});
		}
		centerHtmlMenu.AddMenuOption("Open website picker", delegate(CCSPlayerController target, ChatMenuOption _)
		{
			_plugin.Manager.ShowSite(target);
		});
		MenuManager.OpenCenterHtmlMenu(_plugin, player, centerHtmlMenu);
	}

	public void OpenPicker(CCSPlayerController player, int slot)
	{
		slot = PlayerData.NormalizeSlot(slot);
		PlayerData playerData = _plugin.Manager.LoadFresh(player.SteamID.ToString());
		playerData.SetActiveSlot(slot);
		_plugin.Db.Save(playerData);
		List<GraffitiDef> menuEntries = GetMenuEntries(player);
		if (menuEntries.Count == 0)
		{
			player.PrintToChat($"{Prefix()}{ChatColors.Red}No graffiti available for you.");
			return;
		}
		int slot2 = playerData.GetSlot(slot);
		CenterHtmlMenu centerHtmlMenu = new CenterHtmlMenu($"Graffiti Slot {slot}", _plugin)
		{
			PostSelectAction = PostSelectAction.Close,
			ExitButton = true
		};
		centerHtmlMenu.AddMenuOption($"Slot {slot}: {DisplayName(_plugin.FindGraffiti(slot2)) ?? "Empty"}", delegate
		{
		}, disabled: true);
		centerHtmlMenu.AddMenuOption("Back to slots", delegate(CCSPlayerController target, ChatMenuOption _)
		{
			Open(target);
		});
		foreach (GraffitiDef item in menuEntries)
		{
			int defIndex = item.DefIndex;
			string text = DisplayName(item) ?? $"#{defIndex}";
			if (BaseGraffitiName(_plugin.FindGraffiti(slot2)?.Name) == BaseGraffitiName(item.Name))
			{
				text = "* " + text;
			}
			centerHtmlMenu.AddMenuOption(text, delegate(CCSPlayerController target, ChatMenuOption _)
			{
				SelectGraffiti(target, slot, defIndex);
			});
		}
		MenuManager.OpenCenterHtmlMenu(_plugin, player, centerHtmlMenu);
	}

	public void SelectGraffiti(CCSPlayerController player, int slot, int defIndex)
	{
		if (player.IsValid && !player.IsBot)
		{
			slot = PlayerData.NormalizeSlot(slot);
			GraffitiDef graffitiDef = _plugin.FindGraffiti(defIndex);
			if (graffitiDef == null)
			{
				player.PrintToChat($"{Prefix()}{ChatColors.Red}That graffiti is not in the catalog.");
				return;
			}
			if (graffitiDef.VipOnly && !_plugin.Manager.IsVip(player) && !_plugin.Manager.IsAdmin(player))
			{
				player.PrintToChat($"{Prefix()}{ChatColors.Red}That graffiti is VIP-only.");
				return;
			}
			PlayerData playerData = _plugin.Manager.LoadFresh(player.SteamID.ToString());
			playerData.SetSlot(slot, graffitiDef.DefIndex);
			playerData.SetActiveSlot(slot);
			_plugin.Db.Save(playerData);
			_plugin.Manager.TryBindSprayKey(player, print: false);
			player.PrintToChat($"{Prefix()}Slot {ChatColors.Yellow}{slot}{ChatColors.Default} selected {ChatColors.Yellow}{DisplayName(graffitiDef)}{ChatColors.Default}. Press {ChatColors.Yellow}{_plugin.Manager.SprayKeyLabel()}{ChatColors.Default} or type {ChatColors.Yellow}!spray{ChatColors.Default}.");
		}
	}

	public static string? DisplayName(GraffitiDef? entry)
	{
		if (entry != null)
		{
			return BaseGraffitiName(entry.Name);
		}
		return null;
	}

	private List<GraffitiDef> GetMenuEntries(CCSPlayerController player)
	{
		bool canUseVip = _plugin.Manager.IsVip(player) || _plugin.Manager.IsAdmin(player);
		List<GraffitiDef> list = (from @group in _plugin.Catalog.Values.Where((GraffitiDef entry) => entry.DefIndex > 0 && (!entry.VipOnly || canUseVip)).GroupBy<GraffitiDef, string>((GraffitiDef entry) => BaseGraffitiName(entry.Name), StringComparer.OrdinalIgnoreCase)
			select (from entry in @group
				orderby HasColorSuffix(entry.Name) ? 1 : 0, PreferredColorRank(entry.Name), entry.DefIndex
				select entry).First()).OrderBy<GraffitiDef, string>((GraffitiDef entry) => BaseGraffitiName(entry.Name), StringComparer.OrdinalIgnoreCase).ToList();
		if (_plugin.Config.MenuGraffitiLimit > 0)
		{
			list = list.Take(_plugin.Config.MenuGraffitiLimit).ToList();
		}
		return list;
	}

	private static string BaseGraffitiName(string? name)
	{
		if (string.IsNullOrWhiteSpace(name))
		{
			return "";
		}
		string[] colorSuffixes = ColorSuffixes;
		foreach (string text in colorSuffixes)
		{
			string text2 = " (" + text + ")";
			if (name.EndsWith(text2, StringComparison.OrdinalIgnoreCase))
			{
				int length = text2.Length;
				return name.Substring(0, name.Length - length);
			}
		}
		return name;
	}

	private static bool HasColorSuffix(string name)
	{
		return ColorSuffixes.Any((string suffix) => name.EndsWith(" (" + suffix + ")", StringComparison.OrdinalIgnoreCase));
	}

	private static int PreferredColorRank(string name)
	{
		for (int i = 0; i < ColorSuffixes.Length; i++)
		{
			if (name.EndsWith(" (" + ColorSuffixes[i] + ")", StringComparison.OrdinalIgnoreCase))
			{
				return i;
			}
		}
		return -1;
	}

	private static string Prefix()
	{
		return $" {ChatColors.Green}[Graffiti]{ChatColors.Default} ";
	}
}
