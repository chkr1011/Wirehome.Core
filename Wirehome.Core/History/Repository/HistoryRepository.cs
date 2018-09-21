using System;
using System.IO;
using System.Text;
using MySql.Data.MySqlClient;
using Wirehome.Core.Storage;

namespace Wirehome.Core.History.Repository
{
    public class HistoryRepository
    {
        private readonly StorageService _storageService;
        private MySqlConnection _databaseConnection;

        public HistoryRepository(StorageService storageService)
        {
            _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
        }

        public void Initialize()
        {
            _databaseConnection = new MySqlConnection("Server=localhost;Uid=wirehome;Pwd=w1r3h0m3;SslMode=none");
            _databaseConnection.Open();

            var scriptFile = Path.Combine(_storageService.BinPath, "History", "Scripts", "Initialize.V1.sql");
            var script = File.ReadAllText(scriptFile, Encoding.UTF8);

            using (var command = _databaseConnection.CreateCommand())
            {
                command.CommandText = script;
                command.ExecuteNonQuery();
            }
        }

        public ComponentStatusRow GetComponentStatusRow(string componentUid, string statusUid)
        {
            using (var command = _databaseConnection.CreateCommand())
            {
                command.CommandText = @"
                    SELECT ID,Value,EndTimestamp
                    FROM History.ComponentStatus
                    WHERE ComponentUid=@C_UID AND StatusUid=@P_UID AND IsLatest=1";

                command.Parameters.AddWithValue("@C_UID", componentUid);
                command.Parameters.AddWithValue("@P_UID", statusUid);

                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return null;
                    }

                    var result = new ComponentStatusRow
                    {
                        ID = (uint)reader["ID"],
                        Value = (string)reader["Value"],
                        Timestamp = (DateTime)reader["EndTimestamp"]
                    };

                    return result;
                }
            }
        }

        public void UpdateComponentStatusRow(uint id, DateTime timestamp)
        {
            using (var command = _databaseConnection.CreateCommand())
            {
                command.CommandText = @"
                    UPDATE ComponentPropertyHistory
                    SET EndTimestamp=@ETS
                    WHERE ID=@ID";

                command.Parameters.AddWithValue("@ID", id);
                command.Parameters.AddWithValue("@ETS", timestamp);

                command.ExecuteNonQuery();
            }
        }

        public void InsertComponentStatusRow(string componentUid, string propertyUid, string value, DateTime timestamp)
        {
            using (var command = _databaseConnection.CreateCommand())
            {
                command.CommandText = @"
                    UPDATE ComponentPropertyHistory 
                    SET IsLatest=0
                    WHERE ComponentUid=@C_UID AND PropertyUid=@P_UID";

                command.Parameters.AddWithValue("@C_UID", componentUid);
                command.Parameters.AddWithValue("@P_UID", propertyUid);

                command.ExecuteNonQuery();
            }

            using (var command = _databaseConnection.CreateCommand())
            {
                command.CommandText = @"
                    INSERT INTO ComponentPropertyHistory 
                    (ComponentUid, PropertyUid, Value, IsLatest, BeginTimestamp, EndTimestamp)
                    VALUES
                    (@C_UID, @P_UID, @VALUE, 1, @BTS, @ETS)";

                command.Parameters.AddWithValue("@C_UID", componentUid);
                command.Parameters.AddWithValue("@P_UID", propertyUid);
                command.Parameters.AddWithValue("@VALUE", value);

                command.Parameters.AddWithValue("@BTS", timestamp);
                command.Parameters.AddWithValue("@ETS", timestamp);

                command.ExecuteNonQuery();
            }
        }
    }
}
