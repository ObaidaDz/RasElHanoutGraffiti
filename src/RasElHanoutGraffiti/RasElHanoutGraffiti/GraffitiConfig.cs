using System.Collections.Generic;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Core;

namespace RasElHanoutGraffiti;

public sealed class GraffitiConfig : BasePluginConfig
{
	[JsonPropertyName("DatabaseHost")]
	public string DatabaseHost { get; set; } = "127.0.0.1";

	[JsonPropertyName("DatabasePort")]
	public int DatabasePort { get; set; } = 3306;

	[JsonPropertyName("DatabaseName")]
	public string DatabaseName { get; set; } = "simpleadmin";

	[JsonPropertyName("DatabaseUser")]
	public string DatabaseUser { get; set; } = "cs2_manager";

	[JsonPropertyName("DatabasePassword")]
	public string DatabasePassword { get; set; } = "";

	[JsonPropertyName("DatabaseTable")]
	public string DatabaseTable { get; set; } = "reh_player_graffiti";

	[JsonPropertyName("CatalogFile")]
	public string CatalogFile { get; set; } = "catalog/graffiti.json";

	[JsonPropertyName("WebsiteGraffitiUrl")]
	public string WebsiteGraffitiUrl { get; set; } = "https://raselhanoutdz.com/skins";

	[JsonPropertyName("WeeklyLimitNormal")]
	public int WeeklyLimitNormal { get; set; } = 8;

	[JsonPropertyName("WeeklyLimitVIP")]
	public int WeeklyLimitVip { get; set; } = 35;

	[JsonPropertyName("AdminUnlimited")]
	public bool AdminUnlimited { get; set; } = true;

	[JsonPropertyName("VipFlag")]
	public string VipFlag { get; set; } = "@css/vip";

	[JsonPropertyName("AdminFlag")]
	public string AdminFlag { get; set; } = "@css/root";

	[JsonPropertyName("SprayCooldownSeconds")]
	public int SprayCooldownSeconds { get; set; } = 45;

	[JsonPropertyName("SprayCooldownVIPSeconds")]
	public int SprayCooldownVipSeconds { get; set; } = 18;

	[JsonPropertyName("SprayCooldownAdminSeconds")]
	public int SprayCooldownAdminSeconds { get; set; } = 3;

	[JsonPropertyName("WeekResetDay")]
	public string WeekResetDay { get; set; } = "Saturday";

	[JsonPropertyName("ReplaceVanillaSprays")]
	public bool ReplaceVanillaSprays { get; set; } = true;

	[JsonPropertyName("InterceptSprayWheel")]
	public bool InterceptSprayWheel { get; set; } = true;

	[JsonPropertyName("AutoBindSprayKey")]
	public bool AutoBindSprayKey { get; set; } = true;

	[JsonPropertyName("SprayKey")]
	public string SprayKey { get; set; } = "t";

	[JsonPropertyName("MenuGraffitiLimit")]
	public int MenuGraffitiLimit { get; set; }

	[JsonPropertyName("FeaturedGraffitiDefIndexes")]
	public List<int> FeaturedGraffitiDefIndexes { get; set; } = new List<int>();
}
