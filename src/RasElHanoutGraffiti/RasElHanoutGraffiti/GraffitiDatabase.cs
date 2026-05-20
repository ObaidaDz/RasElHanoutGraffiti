using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using RasElHanoutGraffiti.Models;

namespace RasElHanoutGraffiti;

public sealed class GraffitiDatabase : IDisposable
{
	private readonly GraffitiConfig _config;

	private readonly ILogger _logger;

	private readonly string _connectionString;

	public GraffitiDatabase(GraffitiConfig config, ILogger logger)
	{
		_config = config;
		_logger = logger;
		MySqlConnectionStringBuilder mySqlConnectionStringBuilder = new MySqlConnectionStringBuilder
		{
			Server = config.DatabaseHost,
			Port = (uint)config.DatabasePort,
			Database = config.DatabaseName,
			UserID = config.DatabaseUser,
			Password = config.DatabasePassword,
			CharacterSet = "utf8mb4",
			Pooling = true,
			MaximumPoolSize = 8u,
			ConnectionTimeout = 5u,
			DefaultCommandTimeout = 8u
		};
		_connectionString = mySqlConnectionStringBuilder.ConnectionString;
	}

	public void Initialize()
	{
		using MySqlConnection connection = Open();
		Execute(connection, "CREATE TABLE IF NOT EXISTS `" + _config.DatabaseTable + "` (\n    `steam_id` VARCHAR(32) NOT NULL,\n    `slot_1` INT NOT NULL DEFAULT 0,\n    `slot_2` INT NOT NULL DEFAULT 0,\n    `slot_3` INT NOT NULL DEFAULT 0,\n    `active_slot` TINYINT NOT NULL DEFAULT 1,\n    `uses_this_week` INT NOT NULL DEFAULT 0,\n    `week_start` BIGINT NOT NULL DEFAULT 0,\n    `total_uses` INT NOT NULL DEFAULT 0,\n    `last_spray_at` BIGINT NOT NULL DEFAULT 0,\n    `updated_at` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,\n    PRIMARY KEY (`steam_id`),\n    KEY `idx_total_uses` (`total_uses`)\n) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;");
		HashSet<string> columns = GetColumns(connection);
		EnsureColumn(connection, columns, "slot_1", "INT NOT NULL DEFAULT 0");
		EnsureColumn(connection, columns, "slot_2", "INT NOT NULL DEFAULT 0");
		EnsureColumn(connection, columns, "slot_3", "INT NOT NULL DEFAULT 0");
		EnsureColumn(connection, columns, "active_slot", "TINYINT NOT NULL DEFAULT 1");
		EnsureColumn(connection, columns, "uses_this_week", "INT NOT NULL DEFAULT 0");
		EnsureColumn(connection, columns, "week_start", "BIGINT NOT NULL DEFAULT 0");
		EnsureColumn(connection, columns, "total_uses", "INT NOT NULL DEFAULT 0");
		EnsureColumn(connection, columns, "last_spray_at", "BIGINT NOT NULL DEFAULT 0");
		columns = GetColumns(connection);
		if (columns.Contains("def_index"))
		{
			Execute(connection, "UPDATE `" + _config.DatabaseTable + "`\nSET `slot_1` = `def_index`\nWHERE `slot_1` = 0 AND `def_index` > 0;");
			TryExecute(connection, "ALTER TABLE `" + _config.DatabaseTable + "` DROP COLUMN `def_index`;");
		}
	}

	public PlayerData Load(string steamId, string resetDay)
	{
		using MySqlConnection mySqlConnection = Open();
		using MySqlCommand mySqlCommand = mySqlConnection.CreateCommand();
		mySqlCommand.CommandText = "SELECT `steam_id`, `slot_1`, `slot_2`, `slot_3`, `active_slot`,\n       `uses_this_week`, `week_start`, `total_uses`, `last_spray_at`\nFROM `" + _config.DatabaseTable + "`\nWHERE `steam_id` = @steam_id\nLIMIT 1;";
		mySqlCommand.Parameters.AddWithValue("@steam_id", steamId);
		using MySqlDataReader mySqlDataReader = mySqlCommand.ExecuteReader();
		if (!mySqlDataReader.Read())
		{
			return new PlayerData
			{
				SteamId = steamId,
				WeekStart = CurrentWeekStart(resetDay)
			};
		}
		return new PlayerData
		{
			SteamId = mySqlDataReader.GetString(0),
			Slot1 = mySqlDataReader.GetInt32(1),
			Slot2 = mySqlDataReader.GetInt32(2),
			Slot3 = mySqlDataReader.GetInt32(3),
			ActiveSlot = PlayerData.NormalizeSlot(mySqlDataReader.GetInt32(4)),
			UsesThisWeek = mySqlDataReader.GetInt32(5),
			WeekStart = mySqlDataReader.GetInt64(6),
			TotalUses = mySqlDataReader.GetInt32(7),
			LastSprayAt = mySqlDataReader.GetInt64(8)
		};
	}

	public void Save(PlayerData data)
	{
		data.SetActiveSlot(data.ActiveSlot);
		using MySqlConnection mySqlConnection = Open();
		using MySqlCommand mySqlCommand = mySqlConnection.CreateCommand();
		mySqlCommand.CommandText = "INSERT INTO `" + _config.DatabaseTable + "`\n    (`steam_id`, `slot_1`, `slot_2`, `slot_3`, `active_slot`,\n     `uses_this_week`, `week_start`, `total_uses`, `last_spray_at`)\nVALUES\n    (@steam_id, @slot_1, @slot_2, @slot_3, @active_slot,\n     @uses_this_week, @week_start, @total_uses, @last_spray_at)\nON DUPLICATE KEY UPDATE\n    `slot_1` = VALUES(`slot_1`),\n    `slot_2` = VALUES(`slot_2`),\n    `slot_3` = VALUES(`slot_3`),\n    `active_slot` = VALUES(`active_slot`),\n    `uses_this_week` = VALUES(`uses_this_week`),\n    `week_start` = VALUES(`week_start`),\n    `total_uses` = VALUES(`total_uses`),\n    `last_spray_at` = VALUES(`last_spray_at`);";
		AddPlayerParameters(mySqlCommand, data);
		mySqlCommand.ExecuteNonQuery();
	}

	public void IncrementUse(PlayerData data)
	{
		data.UsesThisWeek++;
		data.TotalUses++;
		Save(data);
	}

	public void ResetWeeklyUses(string steamId)
	{
		using MySqlConnection mySqlConnection = Open();
		using MySqlCommand mySqlCommand = mySqlConnection.CreateCommand();
		mySqlCommand.CommandText = "UPDATE `" + _config.DatabaseTable + "`\nSET `uses_this_week` = 0\nWHERE `steam_id` = @steam_id;";
		mySqlCommand.Parameters.AddWithValue("@steam_id", steamId);
		mySqlCommand.ExecuteNonQuery();
	}

	public void GiveBonusSprays(string steamId, int amount)
	{
		using MySqlConnection mySqlConnection = Open();
		using MySqlCommand mySqlCommand = mySqlConnection.CreateCommand();
		mySqlCommand.CommandText = "UPDATE `" + _config.DatabaseTable + "`\nSET `uses_this_week` = GREATEST(0, `uses_this_week` - @amount)\nWHERE `steam_id` = @steam_id;";
		mySqlCommand.Parameters.AddWithValue("@steam_id", steamId);
		mySqlCommand.Parameters.AddWithValue("@amount", amount);
		mySqlCommand.ExecuteNonQuery();
	}

	public List<(string SteamId, int Total)> GetTopSprayers(int limit)
	{
		List<(string, int)> list = new List<(string, int)>();
		using MySqlConnection mySqlConnection = Open();
		using MySqlCommand mySqlCommand = mySqlConnection.CreateCommand();
		mySqlCommand.CommandText = "SELECT `steam_id`, `total_uses`\nFROM `" + _config.DatabaseTable + "`\nWHERE `total_uses` > 0\nORDER BY `total_uses` DESC\nLIMIT @limit;";
		mySqlCommand.Parameters.AddWithValue("@limit", limit);
		using MySqlDataReader mySqlDataReader = mySqlCommand.ExecuteReader();
		while (mySqlDataReader.Read())
		{
			list.Add((mySqlDataReader.GetString(0), mySqlDataReader.GetInt32(1)));
		}
		return list;
	}

	public static long CurrentWeekStart(string resetDay = "Saturday")
	{
		DayOfWeek result;
		DayOfWeek dayOfWeek = (Enum.TryParse<DayOfWeek>(resetDay, ignoreCase: true, out result) ? result : DayOfWeek.Saturday);
		DateTime utcNow = DateTime.UtcNow;
		int num = (7 + utcNow.DayOfWeek - dayOfWeek) % 7;
		return new DateTimeOffset(utcNow.AddDays(-num).Date, TimeSpan.Zero).ToUnixTimeSeconds();
	}

	private MySqlConnection Open()
	{
		MySqlConnection mySqlConnection = new MySqlConnection(_connectionString);
		try
		{
			mySqlConnection.Open();
			return mySqlConnection;
		}
		catch (Exception exception)
		{
			mySqlConnection.Dispose();
			_logger.LogError(exception, "[RasElHanoutGraffiti] Database connection failed.");
			throw;
		}
	}

	private HashSet<string> GetColumns(MySqlConnection connection)
	{
		using MySqlCommand mySqlCommand = connection.CreateCommand();
		mySqlCommand.CommandText = "SHOW COLUMNS FROM `" + _config.DatabaseTable + "`;";
		using MySqlDataReader mySqlDataReader = mySqlCommand.ExecuteReader();
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		while (mySqlDataReader.Read())
		{
			hashSet.Add(mySqlDataReader.GetString(0));
		}
		return hashSet;
	}

	private void EnsureColumn(MySqlConnection connection, HashSet<string> columns, string column, string definition)
	{
		if (!columns.Contains(column))
		{
			Execute(connection, $"ALTER TABLE `{_config.DatabaseTable}` ADD COLUMN `{column}` {definition};");
			columns.Add(column);
		}
	}

	private static void Execute(MySqlConnection connection, string sql)
	{
		using MySqlCommand mySqlCommand = connection.CreateCommand();
		mySqlCommand.CommandText = sql;
		mySqlCommand.ExecuteNonQuery();
	}

	private void TryExecute(MySqlConnection connection, string sql)
	{
		try
		{
			Execute(connection, sql);
		}
		catch (Exception exception)
		{
			_logger.LogWarning(exception, "[RasElHanoutGraffiti] Optional database migration failed: {Sql}", sql);
		}
	}

	private static void AddPlayerParameters(MySqlCommand command, PlayerData data)
	{
		command.Parameters.AddWithValue("@steam_id", data.SteamId);
		command.Parameters.AddWithValue("@slot_1", data.Slot1);
		command.Parameters.AddWithValue("@slot_2", data.Slot2);
		command.Parameters.AddWithValue("@slot_3", data.Slot3);
		command.Parameters.AddWithValue("@active_slot", data.ActiveSlot);
		command.Parameters.AddWithValue("@uses_this_week", data.UsesThisWeek);
		command.Parameters.AddWithValue("@week_start", data.WeekStart);
		command.Parameters.AddWithValue("@total_uses", data.TotalUses);
		command.Parameters.AddWithValue("@last_spray_at", data.LastSprayAt);
	}

	public void Dispose()
	{
	}
}
