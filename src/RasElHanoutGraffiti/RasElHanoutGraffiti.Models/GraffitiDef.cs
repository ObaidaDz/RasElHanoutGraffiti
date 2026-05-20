using System.Text.Json.Serialization;

namespace RasElHanoutGraffiti.Models;

public sealed class GraffitiDef
{
	[JsonPropertyName("id")]
	public string Id { get; set; } = "";

	[JsonPropertyName("name")]
	public string Name { get; set; } = "";

	[JsonPropertyName("market_hash_name")]
	public string MarketHashName { get; set; } = "";

	[JsonPropertyName("def_index")]
	public int DefIndex { get; set; }

	[JsonPropertyName("rarity")]
	public string Rarity { get; set; } = "Graffiti";

	[JsonPropertyName("rarity_color")]
	public string RarityColor { get; set; } = "#4f75ff";

	[JsonPropertyName("image")]
	public string Image { get; set; } = "";

	[JsonPropertyName("internal_name")]
	public string InternalName { get; set; } = "";

	[JsonPropertyName("collection")]
	public string Collection { get; set; } = "Graffiti";

	[JsonPropertyName("vip_only")]
	public bool VipOnly { get; set; }
}
