using System.Diagnostics.CodeAnalysis;
using SQLite;

namespace Nacionalidad.App.Data;

/// <summary>Conexión SQLite compartida (fichero en AppDataDirectory). Crea las tablas la primera vez.</summary>
public sealed class AppDatabase
{
    public const string FileName = "nacionalidad.db3";

    private const SQLiteOpenFlags Flags =
        SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache;

    private readonly string _path;
    private SQLiteAsyncConnection? _connection;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    public AppDatabase() : this(Path.Combine(FileSystem.Current.AppDataDirectory, FileName)) { }

    public AppDatabase(string path) => _path = path;

    // sqlite-net mapea las tablas por reflexión: se protegen sus propiedades del recorte (trimming) en Release.
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(AnswerRow))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(SessionRow))]
    public async Task<SQLiteAsyncConnection> GetAsync()
    {
        if (_connection is not null) return _connection;
        await _initLock.WaitAsync();
        try
        {
            if (_connection is not null) return _connection;
            var conn = new SQLiteAsyncConnection(_path, Flags);
            await conn.CreateTableAsync<AnswerRow>();
            await conn.CreateTableAsync<SessionRow>();
            _connection = conn;
            return conn;
        }
        finally
        {
            _initLock.Release();
        }
    }
}

[Table("answers")]
public sealed class AnswerRow
{
    [PrimaryKey, AutoIncrement]
    public long Id { get; set; }

    [Indexed]
    public string BankId { get; set; } = string.Empty;

    [Indexed]
    public string QuestionId { get; set; } = string.Empty;

    public string CategoryId { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public int Mode { get; set; }

    /// <summary>UTC en ticks: sqlite-net guarda DateTimeOffset de forma poco portable.</summary>
    public long AnsweredAtUtcTicks { get; set; }
}

[Table("sessions")]
public sealed class SessionRow
{
    [PrimaryKey]
    public string SessionId { get; set; } = string.Empty;

    [Indexed]
    public string BankId { get; set; } = string.Empty;

    public int Mode { get; set; }
    public string? BlueprintId { get; set; }
    public string? CategoryId { get; set; }
    public int Correct { get; set; }
    public int Total { get; set; }

    /// <summary>null = sin nota de corte; 1 = aprobado; 0 = suspenso.</summary>
    public int? Passed { get; set; }

    public long StartedAtUtcTicks { get; set; }
    public long FinishedAtUtcTicks { get; set; }
}
