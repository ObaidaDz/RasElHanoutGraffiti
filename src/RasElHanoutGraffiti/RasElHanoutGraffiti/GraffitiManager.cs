using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;
using RasElHanoutGraffiti.Models;

namespace RasElHanoutGraffiti;

public sealed class GraffitiManager
{
	private readonly RasElHanoutGraffiti _plugin;

	private readonly GraffitiMenu _menu;

	private readonly Dictionary<string, PlayerData> _cache = new Dictionary<string, PlayerData>();

	public GraffitiManager(RasElHanoutGraffiti plugin, GraffitiMenu menu)
	{
		_plugin = plugin;
		_menu = menu;
	}

	public void RegisterAll()
	{
		_plugin.RegisterEventHandler<EventPlayerConnectFull>(OnConnect);
		_plugin.RegisterEventHandler<EventPlayerDisconnect>(OnDisconnect);
		_plugin.RegisterListener<Listeners.OnEntityCreated>(OnEntityCreated);
		_plugin.AddCommand("css_graffiti", "Open the Ras El Hanout in-game graffiti selector.", CmdMenu);
		_plugin.AddCommand("css_graf", "Open the Ras El Hanout in-game graffiti selector.", CmdMenu);
		_plugin.AddCommand("css_grafsite", "Show the Ras El Hanout graffiti website picker.", CmdSite);
		_plugin.AddCommand("css_graffsite", "Show the Ras El Hanout graffiti website picker.", CmdSite);
		_plugin.AddCommand("css_spray", "Spray your selected Ras El Hanout graffiti.", CmdSpray);
		_plugin.AddCommand("css_s", "Spray your selected graffiti shortcut.", CmdSpray);
		_plugin.AddCommand("css_spraykey", "Bind T to Ras El Hanout graffiti spray.", CmdSprayKey);
		_plugin.AddCommand("css_bindspray", "Bind T to Ras El Hanout graffiti spray.", CmdSprayKey);
		_plugin.AddCommand("css_grafinfo", "Show your graffiti usage and cooldown.", CmdInfo);
		_plugin.AddCommand("css_graftop", "Show the top graffiti users.", CmdTop);
		_plugin.AddCommand("css_grafset", "Set active-slot graffiti by definition index.", CmdSet);
		_plugin.AddCommand("css_grafrand", "Pick a random graffiti for the active slot.", CmdRandom);
		_plugin.AddCommand("css_graffreset", "Reset weekly graffiti usage for a player.", CmdReset);
		_plugin.AddCommand("css_graffgive", "Give weekly graffiti usage back to a player.", CmdGive);
		_plugin.AddCommand("css_grafreload", "Reload the graffiti catalog.", CmdReload);
		if (_plugin.Config.InterceptSprayWheel)
		{
			_plugin.AddCommandListener("+spray_menu", OnSprayMenuStart);
			_plugin.AddCommandListener("spray_menu", OnSprayMenuStart);
			_plugin.AddCommandListener("-spray_menu", OnSprayMenuStop);
		}
	}

	public void WarmPlayer(CCSPlayerController player)
	{
		PlayerData orLoad = GetOrLoad(player.SteamID.ToString());
		RefreshWeek(orLoad);
		_plugin.Db.Save(orLoad);
	}

	public PlayerData LoadFresh(string steamId)
	{
		PlayerData playerData = _plugin.Db.Load(steamId, _plugin.Config.WeekResetDay);
		RefreshWeek(playerData);
		_cache[steamId] = playerData;
		return playerData;
	}

	public void ShowSite(CCSPlayerController player)
	{
		PlayerData playerData = LoadFresh(player.SteamID.ToString());
		string value = GraffitiMenu.DisplayName(_plugin.FindGraffiti(playerData.ActiveDefIndex)) ?? ((playerData.ActiveDefIndex > 0) ? $"#{playerData.ActiveDefIndex}" : "none");
		player.PrintToChat($"{Prefix()}Active slot: {ChatColors.Yellow}{playerData.ActiveSlot}{ChatColors.Default} | Selected: {ChatColors.Yellow}{value}");
		player.PrintToChat($"{Prefix()}Pick graffiti on the site: {ChatColors.LightBlue}{_plugin.Config.WebsiteGraffitiUrl}");
		player.PrintToChat($"{Prefix()}Press {ChatColors.Yellow}{SprayKeyLabel()}{ChatColors.Default} or type {ChatColors.Yellow}!spray{ChatColors.Default} after choosing.");
	}

	public bool TryBindSprayKey(CCSPlayerController player, bool print)
	{
		string text = NormalizeSprayKey(_plugin.Config.SprayKey);
		string text2 = "bind " + text + " \"css_spray\"";
		bool result = false;
		try
		{
			player.ExecuteClientCommand(text2);
			result = true;
		}
		catch (Exception exception)
		{
			_plugin.Logger.LogDebug(exception, "[RasElHanoutGraffiti] ExecuteClientCommand bind failed for {Player}.", player.PlayerName);
		}
		if (print)
		{
			player.PrintToChat($"{Prefix()}Trying to bind {ChatColors.Yellow}{text.ToUpperInvariant()}{ChatColors.Default} to {ChatColors.Yellow}!spray{ChatColors.Default}.");
			player.PrintToConsole("[Graffiti] If T still opens the CS2 wheel, run this once: " + text2);
			player.PrintToChat($"{Prefix()}If T still opens the CS2 wheel, open console and run: {ChatColors.Yellow}{text2}");
		}
		return result;
	}

	public string SprayKeyLabel()
	{
		return NormalizeSprayKey(_plugin.Config.SprayKey).ToUpperInvariant();
	}

	public bool IsVip(CCSPlayerController player)
	{
		return AdminManager.PlayerHasPermissions(player, _plugin.Config.VipFlag);
	}

	public bool IsAdmin(CCSPlayerController player)
	{
		return AdminManager.PlayerHasPermissions(player, _plugin.Config.AdminFlag);
	}

	private HookResult OnConnect(EventPlayerConnectFull @event, GameEventInfo _)
	{
		CCSPlayerController player = @event.Userid;
		if (player == null || !player.IsValid || player.IsBot)
		{
			return HookResult.Continue;
		}
		WarmPlayer(player);
		Server.NextWorldUpdate(delegate
		{
			if (player.IsValid)
			{
				if (_plugin.Config.AutoBindSprayKey)
				{
					TryBindSprayKey(player, print: false);
				}
				PlayerData orLoad = GetOrLoad(player.SteamID.ToString());
				string text = GraffitiMenu.DisplayName(_plugin.FindGraffiti(orLoad.ActiveDefIndex)) ?? ((orLoad.ActiveDefIndex > 0) ? $"#{orLoad.ActiveDefIndex}" : null);
				if (text != null)
				{
					player.PrintToChat($"{Prefix()}Slot {ChatColors.Yellow}{orLoad.ActiveSlot}{ChatColors.Default}: {ChatColors.Yellow}{text}{ChatColors.Default} - press {ChatColors.Yellow}{SprayKeyLabel()}{ChatColors.Default} or type {ChatColors.Yellow}!spray");
				}
				else
				{
					player.PrintToChat($"{Prefix()}No graffiti selected - type {ChatColors.Yellow}!graffiti{ChatColors.Default} or visit {ChatColors.LightBlue}{_plugin.Config.WebsiteGraffitiUrl}");
				}
			}
		});
		return HookResult.Continue;
	}

	private HookResult OnDisconnect(EventPlayerDisconnect @event, GameEventInfo _)
	{
		CCSPlayerController userid = @event.Userid;
		if (userid == null || !userid.IsValid)
		{
			return HookResult.Continue;
		}
		string key = userid.SteamID.ToString();
		if (_cache.TryGetValue(key, out PlayerData value))
		{
			_plugin.Db.Save(value);
			_cache.Remove(key);
		}
		return HookResult.Continue;
	}

	private void OnEntityCreated(CEntityInstance entity)
	{
		if (!_plugin.Config.ReplaceVanillaSprays)
		{
			return;
		}
		string designerName;
		try
		{
			designerName = entity.DesignerName;
		}
		catch (NativeException)
		{
			return;
		}
		if (designerName != "player_spray_decal")
		{
			return;
		}
		Server.NextWorldUpdate(delegate
		{
			try
			{
				CPlayerSprayDecal cPlayerSprayDecal = entity.As<CPlayerSprayDecal>();
				if (cPlayerSprayDecal.IsValid && cPlayerSprayDecal.AccountID != 0)
				{
					CCSPlayerController playerFromSteamId = Utilities.GetPlayerFromSteamId(cPlayerSprayDecal.AccountID);
					if (!(playerFromSteamId == null) && playerFromSteamId.IsValid && !playerFromSteamId.IsBot)
					{
						PlayerData orLoad = GetOrLoad(playerFromSteamId.SteamID.ToString());
						if (orLoad.ActiveDefIndex > 0)
						{
							cPlayerSprayDecal.Player = orLoad.ActiveDefIndex;
							Utilities.SetStateChanged(cPlayerSprayDecal, "CPlayerSprayDecal", "m_nPlayer");
						}
					}
				}
			}
			catch (NativeException)
			{
			}
		});
	}

	[CommandHelper(0, "", CommandUsage.CLIENT_ONLY)]
	private void CmdMenu(CCSPlayerController? player, CommandInfo _)
	{
		if (!(player == null) && player.IsValid && !player.IsBot)
		{
			TryBindSprayKey(player, print: false);
			_menu.Open(player);
		}
	}

	[CommandHelper(0, "", CommandUsage.CLIENT_ONLY)]
	private void CmdSite(CCSPlayerController? player, CommandInfo _)
	{
		if (!(player == null) && player.IsValid && !player.IsBot)
		{
			ShowSite(player);
		}
	}

	[CommandHelper(0, "", CommandUsage.CLIENT_ONLY)]
	private void CmdSpray(CCSPlayerController? player, CommandInfo _)
	{
		if (!(player == null) && player.IsValid && !player.IsBot)
		{
			TrySpray(player, printReason: true);
		}
	}

	[CommandHelper(0, "", CommandUsage.CLIENT_ONLY)]
	private void CmdSprayKey(CCSPlayerController? player, CommandInfo _)
	{
		if (!(player == null) && player.IsValid && !player.IsBot)
		{
			TryBindSprayKey(player, print: true);
		}
	}

	private HookResult OnSprayMenuStart(CCSPlayerController? player, CommandInfo _)
	{
		if (player == null || !player.IsValid || player.IsBot)
		{
			return HookResult.Continue;
		}
		TrySpray(player, printReason: true);
		return HookResult.Handled;
	}

	private HookResult OnSprayMenuStop(CCSPlayerController? player, CommandInfo _)
	{
		if (!(player == null) && player.IsValid && !player.IsBot)
		{
			return HookResult.Handled;
		}
		return HookResult.Continue;
	}

	[CommandHelper(0, "", CommandUsage.CLIENT_ONLY)]
	private void CmdInfo(CCSPlayerController? player, CommandInfo _)
	{
		if (!(player == null) && player.IsValid)
		{
			PlayerData orLoad = GetOrLoad(player.SteamID.ToString());
			RefreshWeek(orLoad);
			bool flag = IsAdmin(player);
			bool flag2 = IsVip(player);
			int weeklyLimit = GetWeeklyLimit(player);
			int cooldown = GetCooldown(player);
			int value = Math.Max(0, cooldown - (int)(UnixNow() - orLoad.LastSprayAt));
			string value2 = ((flag && _plugin.Config.AdminUnlimited) ? "unlimited" : Math.Max(0, weeklyLimit - orLoad.UsesThisWeek).ToString());
			player.PrintToChat($"{Prefix()}Active slot: {ChatColors.Yellow}{orLoad.ActiveSlot}{ChatColors.Default} | Weekly sprays left: {ChatColors.Yellow}{value2}{ChatColors.Default} | Cooldown: {ChatColors.Yellow}{value}s");
			for (int i = 1; i <= 3; i++)
			{
				int slot = orLoad.GetSlot(i);
				string value3 = GraffitiMenu.DisplayName(_plugin.FindGraffiti(slot)) ?? ((slot > 0) ? $"#{slot}" : "empty");
				player.PrintToChat($"{Prefix()}Slot {ChatColors.Yellow}{i}{ChatColors.Default}: {ChatColors.Yellow}{value3}");
			}
			player.PrintToChat($"{Prefix()}Total sprays: {ChatColors.Yellow}{orLoad.TotalUses}{ChatColors.Default} | Website: {ChatColors.LightBlue}{_plugin.Config.WebsiteGraffitiUrl}");
			if (flag2 && !flag)
			{
				player.PrintToChat($"{Prefix()}{ChatColors.Gold}VIP graffiti limits are active.");
			}
		}
	}

	[CommandHelper(0, "", CommandUsage.CLIENT_AND_SERVER)]
	private void CmdTop(CCSPlayerController? player, CommandInfo info)
	{
		List<(string, int)> topSprayers = _plugin.Db.GetTopSprayers(10);
		if (topSprayers.Count == 0)
		{
			Reply(player, info, "[Graffiti] No spray stats yet.");
			return;
		}
		Reply(player, info, $"{ChatColors.Gold}Top Graffiti Sprayers");
		int num = 1;
		foreach (var item2 in topSprayers)
		{
			string steamId = item2.Item1;
			int item = item2.Item2;
			string value = Utilities.GetPlayers().FirstOrDefault((CCSPlayerController p) => p.IsValid && p.SteamID.ToString() == steamId)?.PlayerName ?? steamId;
			Reply(player, info, $"{Prefix()}#{num++} {ChatColors.White}{value} {ChatColors.Gold}{item}");
		}
	}

	[CommandHelper(1, "<def_index>", CommandUsage.CLIENT_ONLY)]
	private void CmdSet(CCSPlayerController? player, CommandInfo info)
	{
		if (!(player == null) && player.IsValid)
		{
			if (!int.TryParse(info.GetArg(1), out var result) || result <= 0)
			{
				player.PrintToChat($"{Prefix()}{ChatColors.Red}Usage: !grafset <def_index>");
			}
			else
			{
				PlayerData playerData = LoadFresh(player.SteamID.ToString());
				_menu.SelectGraffiti(player, playerData.ActiveSlot, result);
			}
		}
	}

	[CommandHelper(0, "", CommandUsage.CLIENT_ONLY)]
	private void CmdRandom(CCSPlayerController? player, CommandInfo _)
	{
		if (player == null || !player.IsValid)
		{
			return;
		}
		if (_plugin.Catalog.Count == 0)
		{
			player.PrintToChat($"{Prefix()}{ChatColors.Red}No graffiti catalog loaded.");
			return;
		}
		List<GraffitiDef> list = _plugin.Catalog.Values.Where((GraffitiDef e) => !e.VipOnly || IsVip(player) || IsAdmin(player)).ToList();
		if (list.Count == 0)
		{
			player.PrintToChat($"{Prefix()}{ChatColors.Red}No graffiti available for you.");
			return;
		}
		GraffitiDef graffitiDef = list[Random.Shared.Next(list.Count)];
		PlayerData playerData = LoadFresh(player.SteamID.ToString());
		playerData.SetSlot(playerData.ActiveSlot, graffitiDef.DefIndex);
		_plugin.Db.Save(playerData);
		player.PrintToChat($"{Prefix()}Random slot {ChatColors.Yellow}{playerData.ActiveSlot}{ChatColors.Default}: {ChatColors.Yellow}{GraffitiMenu.DisplayName(graffitiDef)}{ChatColors.Default}. Press {ChatColors.Yellow}{SprayKeyLabel()}{ChatColors.Default} or type {ChatColors.Yellow}!spray{ChatColors.Default}.");
	}

	[CommandHelper(1, "<#userid|steamid>", CommandUsage.CLIENT_AND_SERVER)]
	private void CmdReset(CCSPlayerController? player, CommandInfo info)
	{
		if (player != null && !IsAdmin(player))
		{
			info.ReplyToCommand("[Graffiti] No access.");
			return;
		}
		CCSPlayerController cCSPlayerController = FindPlayer(info.GetArg(1));
		if (cCSPlayerController == null)
		{
			info.ReplyToCommand("[Graffiti] Player not found.");
			return;
		}
		string text = cCSPlayerController.SteamID.ToString();
		_plugin.Db.ResetWeeklyUses(text);
		if (_cache.TryGetValue(text, out PlayerData value))
		{
			value.UsesThisWeek = 0;
		}
		info.ReplyToCommand("[Graffiti] Reset weekly uses for " + cCSPlayerController.PlayerName + ".");
		cCSPlayerController.PrintToChat($"{Prefix()}{ChatColors.Green}Your weekly graffiti uses were reset by an admin.");
	}

	[CommandHelper(2, "<#userid|steamid> <amount>", CommandUsage.CLIENT_AND_SERVER)]
	private void CmdGive(CCSPlayerController? player, CommandInfo info)
	{
		if (player != null && !IsAdmin(player))
		{
			info.ReplyToCommand("[Graffiti] No access.");
			return;
		}
		CCSPlayerController cCSPlayerController = FindPlayer(info.GetArg(1));
		if (cCSPlayerController == null || !int.TryParse(info.GetArg(2), out var result) || result <= 0)
		{
			info.ReplyToCommand("[Graffiti] Usage: css_graffgive <#userid|steamid> <amount>");
			return;
		}
		string text = cCSPlayerController.SteamID.ToString();
		_plugin.Db.GiveBonusSprays(text, result);
		if (_cache.TryGetValue(text, out PlayerData value))
		{
			value.UsesThisWeek = Math.Max(0, value.UsesThisWeek - result);
		}
		info.ReplyToCommand($"[Graffiti] Gave {result} spray(s) back to {cCSPlayerController.PlayerName}.");
		cCSPlayerController.PrintToChat($"{Prefix()}{ChatColors.Green}+{result} weekly graffiti spray(s).");
	}

	[CommandHelper(0, "", CommandUsage.CLIENT_AND_SERVER)]
	private void CmdReload(CCSPlayerController? player, CommandInfo info)
	{
		if (player != null && !IsAdmin(player))
		{
			info.ReplyToCommand("[Graffiti] No access.");
			return;
		}
		_plugin.ReloadCatalog();
		info.ReplyToCommand($"[Graffiti] Reloaded {_plugin.Catalog.Count} graffiti entries.");
	}

	private bool TrySpray(CCSPlayerController player, bool printReason)
	{
		PlayerData playerData = LoadFresh(player.SteamID.ToString());
		RefreshWeek(playerData);
		if (playerData.ActiveDefIndex <= 0)
		{
			if (printReason)
			{
				player.PrintToChat($"{Prefix()}{ChatColors.Red}Choose graffiti first with !graffiti or the website.");
			}
			return false;
		}
		GraffitiDef graffitiDef = _plugin.FindGraffiti(playerData.ActiveDefIndex);
		if (graffitiDef != null && graffitiDef.VipOnly && !IsVip(player) && !IsAdmin(player))
		{
			if (printReason)
			{
				player.PrintToChat($"{Prefix()}{ChatColors.Red}This graffiti is VIP-only.");
			}
			return false;
		}
		long num = UnixNow();
		int cooldown = GetCooldown(player);
		if (num - playerData.LastSprayAt < cooldown)
		{
			if (printReason)
			{
				player.PrintToChat($"{Prefix()}{ChatColors.Red}Wait {cooldown - (int)(num - playerData.LastSprayAt)}s before spraying again.");
			}
			return false;
		}
		bool flag = IsAdmin(player) && _plugin.Config.AdminUnlimited;
		if (!flag)
		{
			int weeklyLimit = GetWeeklyLimit(player);
			if (playerData.UsesThisWeek >= weeklyLimit)
			{
				if (printReason)
				{
					player.PrintToChat($"{Prefix()}{ChatColors.Red}Weekly graffiti limit reached. VIP gets {_plugin.Config.WeeklyLimitVip}/week.");
				}
				return false;
			}
		}
		if (!CreateSprayDecal(player, playerData.ActiveDefIndex))
		{
			if (printReason)
			{
				player.PrintToChat($"{Prefix()}{ChatColors.Red}No valid spray surface in front of you.");
			}
			return false;
		}
		playerData.LastSprayAt = num;
		if (!flag)
		{
			_plugin.Db.IncrementUse(playerData);
		}
		else
		{
			_plugin.Db.Save(playerData);
		}
		string value = (flag ? "unlimited" : Math.Max(0, GetWeeklyLimit(player) - playerData.UsesThisWeek).ToString());
		if (printReason)
		{
			player.PrintToChat($"{Prefix()}Sprayed slot {ChatColors.Yellow}{playerData.ActiveSlot}{ChatColors.Default}: {ChatColors.Yellow}{GraffitiMenu.DisplayName(graffitiDef) ?? $"#{playerData.ActiveDefIndex}"}{ChatColors.Default}. Left this week: {ChatColors.Yellow}{value}");
		}
		return true;
	}

	private unsafe bool CreateSprayDecal(CCSPlayerController player, int defIndex)
	{
		if (!player.IsValid)
		{
			return false;
		}
		CCSPlayerPawn value = player.PlayerPawn.Value;
		if (value == null || !value.IsValid || value.LifeState != 0)
		{
			return false;
		}
		CCSPlayer_MovementServices cCSPlayer_MovementServices = value.MovementServices?.As<CCSPlayer_MovementServices>();
		if (cCSPlayer_MovementServices == null)
		{
			return false;
		}
		CGameTrace* ptr = stackalloc CGameTrace[1];
		if (!value.IsAbleToApplyGraffiti((nint)ptr) || ptr == (CGameTrace*)IntPtr.Zero)
		{
			return false;
		}
		CounterStrikeSharp.API.Modules.Utils.Vector vector = ToVector(ptr->EndPos);
		CounterStrikeSharp.API.Modules.Utils.Vector vector2 = ToVector(ptr->HitNormal);
		player.EmitSound("SprayCan.Shake");
		CPlayerSprayDecal cPlayerSprayDecal = Utilities.CreateEntityByName<CPlayerSprayDecal>("player_spray_decal");
		if (cPlayerSprayDecal == null || !cPlayerSprayDecal.IsValid)
		{
			return false;
		}
		cPlayerSprayDecal.EndPos.Add(vector);
		cPlayerSprayDecal.Start.Add(vector);
		cPlayerSprayDecal.Left.Add(cCSPlayer_MovementServices.Left);
		cPlayerSprayDecal.Normal.Add(vector2);
		cPlayerSprayDecal.AccountID = (uint)player.SteamID;
		cPlayerSprayDecal.Player = defIndex;
		cPlayerSprayDecal.DispatchSpawn();
		player.EmitSound("SprayCan.Paint");
		return true;
	}

	private PlayerData GetOrLoad(string steamId)
	{
		if (_cache.TryGetValue(steamId, out PlayerData value))
		{
			return value;
		}
		PlayerData playerData = _plugin.Db.Load(steamId, _plugin.Config.WeekResetDay);
		RefreshWeek(playerData);
		_cache[steamId] = playerData;
		return playerData;
	}

	private void RefreshWeek(PlayerData data)
	{
		long num = GraffitiDatabase.CurrentWeekStart(_plugin.Config.WeekResetDay);
		if (data.WeekStart < num)
		{
			data.UsesThisWeek = 0;
			data.WeekStart = num;
		}
	}

	private int GetWeeklyLimit(CCSPlayerController player)
	{
		if (!IsVip(player) && !IsAdmin(player))
		{
			return _plugin.Config.WeeklyLimitNormal;
		}
		return _plugin.Config.WeeklyLimitVip;
	}

	private int GetCooldown(CCSPlayerController player)
	{
		if (IsAdmin(player))
		{
			return _plugin.Config.SprayCooldownAdminSeconds;
		}
		if (!IsVip(player))
		{
			return _plugin.Config.SprayCooldownSeconds;
		}
		return _plugin.Config.SprayCooldownVipSeconds;
	}

	private static long UnixNow()
	{
		return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
	}

	private static CounterStrikeSharp.API.Modules.Utils.Vector ToVector(Vector3 vec)
	{
		return new CounterStrikeSharp.API.Modules.Utils.Vector(vec.X, vec.Y, vec.Z);
	}

	private static string NormalizeSprayKey(string? key)
	{
		if (string.IsNullOrWhiteSpace(key))
		{
			return "t";
		}
		string text = key.Trim().ToLowerInvariant();
		if (text.Any(char.IsWhiteSpace) || text.Contains(';') || text.Contains('"'))
		{
			return "t";
		}
		return text;
	}

	private static string Prefix()
	{
		return $" {ChatColors.Green}[Graffiti]{ChatColors.Default} ";
	}

	private static void Reply(CCSPlayerController? player, CommandInfo info, string message)
	{
		if (player != null)
		{
			player.PrintToChat(message);
		}
		else
		{
			info.ReplyToCommand(message);
		}
	}

	private static CCSPlayerController? FindPlayer(string arg)
	{
		if (arg.StartsWith("#") && int.TryParse(arg.Substring(1), out var result))
		{
			return Utilities.GetPlayerFromUserid(result);
		}
		return Utilities.GetPlayers().FirstOrDefault((CCSPlayerController player) => player.IsValid && !player.IsBot && player.SteamID.ToString() == arg);
	}
}
