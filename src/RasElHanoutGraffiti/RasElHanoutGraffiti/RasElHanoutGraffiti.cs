using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;
using RasElHanoutGraffiti.Models;

namespace RasElHanoutGraffiti;

public sealed class RasElHanoutGraffiti : BasePlugin, IPluginConfig<GraffitiConfig>
{
	public override string ModuleName => "RasElHanout Graffiti";

	public override string ModuleVersion => "4.3.0";

	public override string ModuleAuthor => "Maximus";

	public override string ModuleDescription => "Website-linked CS2 graffiti selector with three in-game slots";

	public GraffitiConfig Config { get; set; } = new GraffitiConfig();

	public GraffitiDatabase Db { get; private set; }

	public GraffitiManager Manager { get; private set; }

	public GraffitiMenu Menu { get; private set; }

	public IReadOnlyDictionary<int, GraffitiDef> Catalog { get; private set; } = new Dictionary<int, GraffitiDef>();

	public void OnConfigParsed(GraffitiConfig config)
	{
		Config = config;
	}

	public override void Load(bool hotReload)
	{
		ReloadCatalog();
		Db = new GraffitiDatabase(Config, base.Logger);
		Db.Initialize();
		Menu = new GraffitiMenu(this);
		Manager = new GraffitiManager(this, Menu);
		Manager.RegisterAll();
		if (hotReload)
		{
			foreach (CCSPlayerController item in from p in Utilities.GetPlayers()
				where p.IsValid && !p.IsBot
				select p)
			{
				Manager.WarmPlayer(item);
			}
		}
		base.Logger.LogInformation("[RasElHanoutGraffiti] v{Version} loaded with {Count} catalog entries.", ModuleVersion, Catalog.Count);
	}

	public override void Unload(bool hotReload)
	{
		Db?.Dispose();
	}

	public void ReloadCatalog()
	{
		string text = ResolveModulePath(Config.CatalogFile);
		if (!File.Exists(text))
		{
			Catalog = new Dictionary<int, GraffitiDef>();
			base.Logger.LogWarning("[RasElHanoutGraffiti] Catalog file missing: {Path}", text);
			return;
		}
		try
		{
			List<GraffitiDef> source = JsonSerializer.Deserialize<List<GraffitiDef>>(File.ReadAllText(text), new JsonSerializerOptions
			{
				PropertyNameCaseInsensitive = true
			}) ?? new List<GraffitiDef>();
			Catalog = (from item in source
				where item.DefIndex > 0
				group item by item.DefIndex).ToDictionary((IGrouping<int, GraffitiDef> group) => group.Key, (IGrouping<int, GraffitiDef> group) => group.First());
		}
		catch (Exception exception)
		{
			Catalog = new Dictionary<int, GraffitiDef>();
			base.Logger.LogError(exception, "[RasElHanoutGraffiti] Failed to load graffiti catalog.");
		}
	}

	public string ResolveModulePath(string path)
	{
		if (!Path.IsPathRooted(path))
		{
			return Path.Combine(base.ModuleDirectory, path.Replace('/', Path.DirectorySeparatorChar));
		}
		return path;
	}

	public GraffitiDef? FindGraffiti(int defIndex)
	{
		if (!Catalog.TryGetValue(defIndex, out GraffitiDef value))
		{
			return null;
		}
		return value;
	}
}
