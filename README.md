# RasElHanout Graffiti

CS2 graffiti selector for CounterStrikeSharp.

I built this for Ras El Hanout DZ because the normal spray wheel felt too limited for a community server. Players can keep their own graffiti slots, pick from a big catalog, spray with a keybind or command, and keep their choices saved in MySQL.

The repo has two things:

- `addons` - ready package, drop it into your server.
- `src` - source code, for people who want to read it or build it themselves.

No live database config is included here. The plugin creates its own config file on first load.

## What It Does

- Three graffiti slots per player.
- In-game menu with `!graffiti` / `!graf`.
- Website picker link with `!grafsite`.
- Spray command with `!spray` / `!s`.
- Optional auto bind for the spray key, default is `T`.
- Replaces the vanilla CS2 spray wheel if enabled.
- Weekly spray limits for normal players.
- Higher VIP limits.
- Admin unlimited mode.
- Cooldowns for normal, VIP, and admin players.
- MySQL storage, so choices stay after map change/restart.
- Top sprayers command.
- Admin commands to reset/give weekly sprays.
- Catalog reload without rebuilding the plugin.

## Install

Download the release zip and copy the `addons` folder into your server `csgo` folder.

The final path should look like this:

```text
csgo/addons/counterstrikesharp/plugins/RasElHanoutGraffiti/
csgo/addons/counterstrikesharp/gamedata/RasElHanoutGraffiti.json
```

Load it:

```text
css_plugins load RasElHanoutGraffiti
```

First load will create:

```text
csgo/addons/counterstrikesharp/configs/plugins/RasElHanoutGraffiti/RasElHanoutGraffiti.json
```

Open that config, set your database info, then reload the plugin.

```text
css_plugins unload RasElHanoutGraffiti
css_plugins load RasElHanoutGraffiti
```

## Player Commands

```text
!graffiti
!graf
```

Open the slot menu.

```text
!grafsite
!graffsite
```

Show the website picker link.

```text
!spray
!s
```

Spray the selected graffiti.

```text
!spraykey
!bindspray
```

Try to bind the configured spray key.

```text
!grafinfo
```

Show active slot, weekly uses left, cooldown, and total sprays.

```text
!graftop
```

Show top graffiti users.

```text
!grafset <def_index>
```

Set the active slot by graffiti definition index.

```text
!grafrand
```

Pick a random graffiti for the active slot.

## Admin Commands

These use the configured `AdminFlag`.

```text
css_graffreset <#userid|steamid>
```

Reset a player's weekly uses.

```text
css_graffgive <#userid|steamid> <amount>
```

Give sprays back to a player for the current week.

```text
css_grafreload
```

Reload the catalog after editing `catalog/graffiti.json`.

## Main Config

The important fields:

```json
{
  "DatabaseHost": "127.0.0.1",
  "DatabasePort": 3306,
  "DatabaseName": "simpleadmin",
  "DatabaseUser": "cs2_manager",
  "DatabasePassword": "",
  "DatabaseTable": "reh_player_graffiti",
  "CatalogFile": "catalog/graffiti.json",
  "WebsiteGraffitiUrl": "https://raselhanoutdz.com/skins",
  "WeeklyLimitNormal": 8,
  "WeeklyLimitVIP": 35,
  "AdminUnlimited": true,
  "VipFlag": "@css/vip",
  "AdminFlag": "@css/root",
  "SprayCooldownSeconds": 45,
  "SprayCooldownVIPSeconds": 18,
  "SprayCooldownAdminSeconds": 3,
  "WeekResetDay": "Saturday",
  "ReplaceVanillaSprays": true,
  "InterceptSprayWheel": true,
  "AutoBindSprayKey": true,
  "SprayKey": "t"
}
```

## Catalog

The catalog is here:

```text
addons/counterstrikesharp/plugins/RasElHanoutGraffiti/catalog/graffiti.json
```

Each entry is loaded by definition index. If you edit the catalog while the server is online, run:

```text
css_grafreload
```

## Database

The plugin creates the table if it does not exist. It stores:

- SteamID
- three selected slots
- active slot
- weekly uses
- week start
- total sprays
- last spray time

It is small on purpose. I wanted it easy to move between servers without carrying a heavy panel or extra service.

## Notes

This was made for a live CS2 server, so the small things matter: cooldowns, weekly limits, VIP checks, admin recovery commands, and a reload command for the catalog. The package in Releases is ready to use, and the source is here because people asked for it.

Maximus
