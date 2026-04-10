namespace ParkShare.Infrastructure.Configuration
{
    /// <summary>
    /// SQL Server database configuration fields.
    /// </summary>
    public class DatabaseConfig
    {
        public DbConnectionInfo ParkShareDb { get; set; } = new DbConnectionInfo();
    }

    /// <summary>
    /// SQL Server database configuration fields.
    /// </summary>
    public class DbConnectionInfo
    {
        /// <summary>
        /// SQL Server username.
        /// </summary>
        public string User { get; set; } = string.Empty;

        /// <summary>
        /// SQL Server password.
        /// </summary>
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// Server URL or host.
        /// </summary>
        public string ServerUrl { get; set; } = string.Empty;

        /// <summary>
        /// Initial database name.
        /// </summary>
        public string InitialDb { get; set; } = string.Empty;

        /// <summary>
        /// Connection string template, with placeholders.
        /// </summary>
        public string ConnectionString { get; set; } = string.Empty;
    }
}
