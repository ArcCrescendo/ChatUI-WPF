using Microsoft.Data.Sqlite;
using System.Data;
using WpfApp1.Models;
using System.IO;
using System.Windows;

namespace WpfApp1.Services
{
    public class DatabaseService
    {
        private readonly string _connectionString;

        public DatabaseService()
        {
            var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "chat.db");
            _connectionString = $"Data Source={dbPath}";
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                connection.Open();

                var command = connection.CreateCommand();
                command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS ChatHistories (
                        Id TEXT PRIMARY KEY,
                        Title TEXT,
                        Timestamp TEXT
                    );

                    CREATE TABLE IF NOT EXISTS Messages (
                        Id TEXT PRIMARY KEY,
                        ChatHistoryId TEXT,
                        Content TEXT,
                        IsUser INTEGER,
                        Timestamp TEXT,
                        FOREIGN KEY(ChatHistoryId) REFERENCES ChatHistories(Id)
                    );";
                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"初始化数据库时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }

        public async Task SaveChatHistory(ChatHistory chatHistory)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                var command = connection.CreateCommand();
                command.Transaction = transaction;

                // 保存或更新聊天历史
                command.CommandText = @"
                    INSERT OR REPLACE INTO ChatHistories (Id, Title, Timestamp)
                    VALUES ($id, $title, $timestamp)";
                command.Parameters.AddWithValue("$id", chatHistory.Id);
                command.Parameters.AddWithValue("$title", chatHistory.Title);
                command.Parameters.AddWithValue("$timestamp", chatHistory.Timestamp.ToString("O"));
                command.ExecuteNonQuery();

                // 删除旧消息
                command.CommandText = "DELETE FROM Messages WHERE ChatHistoryId = $chatId";
                command.Parameters.Clear();
                command.Parameters.AddWithValue("$chatId", chatHistory.Id);
                command.ExecuteNonQuery();

                // 保存新消息
                foreach (var message in chatHistory.Messages)
                {
                    command.CommandText = @"
                        INSERT INTO Messages (Id, ChatHistoryId, Content, IsUser, Timestamp)
                        VALUES ($id, $chatId, $content, $isUser, $timestamp)";
                    command.Parameters.Clear();
                    command.Parameters.AddWithValue("$id", Guid.NewGuid().ToString());
                    command.Parameters.AddWithValue("$chatId", chatHistory.Id);
                    command.Parameters.AddWithValue("$content", message.Content);
                    command.Parameters.AddWithValue("$isUser", message.IsUser ? 1 : 0);
                    command.Parameters.AddWithValue("$timestamp", message.Timestamp.ToString("O"));
                    command.ExecuteNonQuery();
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public List<ChatHistory> LoadChatHistories()
        {
            var histories = new List<ChatHistory>();
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM ChatHistories ORDER BY Timestamp DESC";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var history = new ChatHistory
                {
                    Id = reader.GetString(0),
                    Title = reader.GetString(1),
                    Timestamp = DateTime.Parse(reader.GetString(2))
                };
                histories.Add(history);
            }

            // 加载每个历史记录的消息
            foreach (var history in histories)
            {
                command = connection.CreateCommand();
                command.CommandText = "SELECT * FROM Messages WHERE ChatHistoryId = $chatId ORDER BY Timestamp";
                command.Parameters.AddWithValue("$chatId", history.Id);

                using var messageReader = command.ExecuteReader();
                while (messageReader.Read())
                {
                    var message = new ChatMessage(
                        messageReader.GetString(2),
                        messageReader.GetInt32(3) == 1)
                    {
                        Timestamp = DateTime.Parse(messageReader.GetString(4))
                    };
                    history.Messages.Add(message);
                }
            }

            return histories;
        }

        public async Task DeleteChatHistory(string chatId)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                var command = connection.CreateCommand();
                command.Transaction = transaction;

                // 先删除消息
                command.CommandText = "DELETE FROM Messages WHERE ChatHistoryId = $chatId";
                command.Parameters.AddWithValue("$chatId", chatId);
                await command.ExecuteNonQueryAsync();

                // 再删除聊天历史
                command.CommandText = "DELETE FROM ChatHistories WHERE Id = $chatId";
                command.Parameters.Clear();
                command.Parameters.AddWithValue("$chatId", chatId);
                await command.ExecuteNonQueryAsync();

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }
} 