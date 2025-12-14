using System.Net.Http.Json;
using System.Text.Json;

namespace RigbySDK;

public class RigbySDKException : Exception
{
    public int? StatusCode { get; }
    public string? Body { get; }

    public RigbySDKException(string message, int? statusCode = null, string? body = null) : base(message)
    {
        StatusCode = statusCode;
        Body = body;
    }
}

public class RigbyClientOptions
{
    public string BaseUrl { get; set; } = "https://api.rigby.host";
    public TimeSpan HttpClientTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public HttpClient? HttpClient { get; set; }
}

public class HttpRigbyClient
{
    private readonly string _baseUrl;
    private readonly HttpClient _http;
    private readonly string _token;

    public HttpRigbyClient(string token, Action<RigbyClientOptions>? configure = null)
    {
        var options = new RigbyClientOptions();
        configure?.Invoke(options);

        _token = token;
        _baseUrl = options.BaseUrl.TrimEnd('/');
        _http = options.HttpClient ?? new HttpClient
        {
            Timeout = options.HttpClientTimeout
        };
    }

    public async Task<JsonElement> CallAsync(string[] path, object? data = null, CancellationToken ct = default)
    {
        if (path.Length == 0)
        {
            throw new RigbySDKException("Path is required");
        }

        var url = $"{_baseUrl}/{string.Join("/", path)}";
        var payload = new { json = data };

        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _token);

        using var response = await _http.SendAsync(request, ct).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw new RigbySDKException("Rigby API error", (int)response.StatusCode, body);
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            return JsonDocument.Parse("null").RootElement.Clone();
        }

        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("json", out var inner))
            {
                return inner.Clone();
            }
            return root.Clone();
        }
        catch (JsonException)
        {
            return JsonDocument.Parse($"{{\"text\": {JsonSerializer.Serialize(body)} }}").RootElement.Clone();
        }
    }
}

public class RigbyClient
{
    public HttpRigbyClient Client { get; }
    public GDPSRoutes GDPS { get; }
    public NotificationRoutes Notifications { get; }
    public UserRoutes User { get; }

    public RigbyClient(string token, Action<RigbyClientOptions>? configure = null)
    {
        Client = new HttpRigbyClient(token, configure);
        GDPS = new GDPSRoutes(Client);
        Notifications = new NotificationRoutes(Client);
        User = new UserRoutes(Client);
    }
}

public class NotificationRoutes
{
    private readonly HttpRigbyClient _client;
    public NotificationRoutes(HttpRigbyClient client) => _client = client;

    public Task<JsonElement> List(CancellationToken ct = default) =>
        _client.CallAsync(new[] { "rpc", "notifications", "list" }, null, ct);

    public Task<JsonElement> MarkAsRead(object data, CancellationToken ct = default) =>
        _client.CallAsync(new[] { "rpc", "notifications", "markAsRead" }, data, ct);

    public Task<JsonElement> MarkAllAsRead(CancellationToken ct = default) =>
        _client.CallAsync(new[] { "rpc", "notifications", "markAllAsRead" }, null, ct);

    public Task<JsonElement> Delete(object data, CancellationToken ct = default) =>
        _client.CallAsync(new[] { "rpc", "notifications", "delete" }, data, ct);
}

public class UserRoutes
{
    private readonly HttpRigbyClient _client;
    public UserRoutes(HttpRigbyClient client) => _client = client;

    public Task<JsonElement> Me(CancellationToken ct = default) =>
        _client.CallAsync(new[] { "rpc", "user", "me" }, null, ct);

    public Task<JsonElement> UpdateProfile(object data, CancellationToken ct = default) =>
        _client.CallAsync(new[] { "rpc", "user", "updateProfile" }, data, ct);

    public Task<JsonElement> ListSessions(CancellationToken ct = default) =>
        _client.CallAsync(new[] { "rpc", "user", "listSessions" }, null, ct);

    public Task<JsonElement> RevokeSession(object data, CancellationToken ct = default) =>
        _client.CallAsync(new[] { "rpc", "user", "revokeSession" }, data, ct);

    public Task<JsonElement> RevokeAllSessions(CancellationToken ct = default) =>
        _client.CallAsync(new[] { "rpc", "user", "revokeAllSessions" }, null, ct);

    public Task<JsonElement> DeleteAccount(CancellationToken ct = default) =>
        _client.CallAsync(new[] { "rpc", "user", "deleteAccount" }, new { confirmation = "delete" }, ct);
}

public class GDPSRoutes
{
    public ConfigRoutes Config { get; }
    public AnalyticsRoutes Analytics { get; }
    public GauntletsRoutes Gauntlets { get; }
    public LevelsRoutes Levels { get; }
    public MapPacksRoutes MapPacks { get; }
    public MasonLabRoutes MasonLab { get; }
    public MusicRoutes Music { get; }
    public PlayerRoutes Player { get; }
    public PlayersRoutes Players { get; }
    public PublicRoutes Public { get; }
    public QuestsRoutes Quests { get; }
    public RolesRoutes Roles { get; }
    public ServerRoutes Server { get; }

    public GDPSRoutes(HttpRigbyClient client)
    {
        Config = new ConfigRoutes(client);
        Analytics = new AnalyticsRoutes(client);
        Gauntlets = new GauntletsRoutes(client);
        Levels = new LevelsRoutes(client);
        MapPacks = new MapPacksRoutes(client);
        MasonLab = new MasonLabRoutes(client);
        Music = new MusicRoutes(client);
        Player = new PlayerRoutes(client);
        Players = new PlayersRoutes(client);
        Public = new PublicRoutes(client);
        Quests = new QuestsRoutes(client);
        Roles = new RolesRoutes(client);
        Server = new ServerRoutes(client);
    }
}

public class ConfigRoutes
{
    private readonly HttpRigbyClient _client;
    public ConfigRoutes(HttpRigbyClient client) => _client = client;

    public Task<JsonElement> Get(object data, CancellationToken ct = default) =>
        _client.CallAsync(new[] { "rpc", "gdps", "config", "get" }, data, ct);

    public Task<JsonElement> UpdateChests(object data, CancellationToken ct = default) =>
        _client.CallAsync(new[] { "rpc", "gdps", "config", "updateChest" }, data, ct);

    public Task<JsonElement> UpdateSecurity(object data, CancellationToken ct = default) =>
        _client.CallAsync(new[] { "rpc", "gdps", "config", "updateSecurity" }, data, ct);

    public Task<JsonElement> UpdateServer(object data, CancellationToken ct = default) =>
        _client.CallAsync(new[] { "rpc", "gdps", "config", "updateServer" }, data, ct);
}

public class AnalyticsRoutes
{
    private readonly HttpRigbyClient _client;
    public AnalyticsRoutes(HttpRigbyClient client) => _client = client;

    public Task<JsonElement> Overview(object data, CancellationToken ct = default) =>
        _client.CallAsync(new[] { "rpc", "gdps", "analytics", "overview" }, data, ct);
}

public class GauntletsRoutes
{
    private readonly HttpRigbyClient _client;
    public GauntletsRoutes(HttpRigbyClient client) => _client = client;

    public Task<JsonElement> List(object data, CancellationToken ct = default) =>
        _client.CallAsync(new[] { "rpc", "gdps", "gauntlets", "list" }, data, ct);

    public Task<JsonElement> Create(object data, CancellationToken ct = default) =>
        _client.CallAsync(new[] { "rpc", "gdps", "gauntlets", "create" }, data, ct);
}

public class LevelsRoutes
{
    private readonly HttpRigbyClient _client;
    public LevelsRoutes(HttpRigbyClient client) => _client = client;

    public Task<JsonElement> Search(object data, CancellationToken ct = default) =>
        _client.CallAsync(new[] { "rpc", "gdps", "levels", "search" }, data, ct);

    public Task<JsonElement> Get(object data, CancellationToken ct = default) =>
        _client.CallAsync(new[] { "rpc", "gdps", "levels", "details" }, data, ct);

    public Task<JsonElement> UpdateMetadata(object data, CancellationToken ct = default) =>
        _client.CallAsync(new[] { "rpc", "gdps", "levels", "updateMetadata" }, data, ct);

    public Task<JsonElement> UpdateMusic(object data, CancellationToken ct = default) =>
        _client.CallAsync(new[] { "rpc", "gdps", "levels", "updateMusic" }, data, ct);

    public Task<JsonElement> UpdateRating(object data, CancellationToken ct = default) =>
        _client.CallAsync(new[] { "rpc", "gdps", "levels", "updateRating" }, data, ct);
}

public class MapPacksRoutes
{
    private readonly HttpRigbyClient _client;
    public MapPacksRoutes(HttpRigbyClient client) => _client = client;

    public Task<JsonElement> List(object data, CancellationToken ct = default) =>
        _client.CallAsync(new[] { "rpc", "gdps", "mappacks", "list" }, data, ct);

    public Task<JsonElement> Create(object data, CancellationToken ct = default) =>
        _client.CallAsync(new[] { "rpc", "gdps", "mappacks", "create" }, data, ct);
}

public class MasonLabRoutes
{
    private readonly HttpRigbyClient _client;
    public MasonLabRoutes(HttpRigbyClient client) => _client = client;

    public Task<JsonElement> Get(object data, CancellationToken ct = default) =>
        _client.CallAsync(new[] { "rpc", "gdps", "masonlab", "get" }, data, ct);

    public Task<JsonElement> Save(object data, CancellationToken ct = default) =>
        _client.CallAsync(new[] { "rpc", "gdps", "masonlab", "save" }, data, ct);
}

public class MusicRoutes
{
    private readonly HttpRigbyClient _client;
    public MusicRoutes(HttpRigbyClient client) => _client = client;

    public Task<JsonElement> List(object data, CancellationToken ct = default) =>
        _client.CallAsync(new[] { "rpc", "gdps", "music", "list" }, data, ct);

    public Task<JsonElement> CreateFromNewgrounds(object data, CancellationToken ct = default) =>
        _client.CallAsync(new[] { "rpc", "gdps", "music", "createFromNewgrounds" }, data, ct);

    public Task<JsonElement> CreateFromUrl(object data, CancellationToken ct = default) =>
        _client.CallAsync(new[] { "rpc", "gdps", "music", "createFromUrl" }, data, ct);

    public Task<JsonElement> UpdateMetadata(object data, CancellationToken ct = default) =>
        _client.CallAsync(new[] { "rpc", "gdps", "music", "updateMetadata" }, data, ct);

    public Task<JsonElement> DeleteAll(object data, CancellationToken ct = default) =>
        _client.CallAsync(new[] { "rpc", "gdps", "music", "deleteAll" }, data, ct);

    public Task<JsonElement> ToggleBan(object data, CancellationToken ct = default) =>
        _client.CallAsync(new[] { "rpc", "gdps", "music", "toggleBan" }, data, ct);
}

public class PlayerSongsRoutes
{
    private readonly HttpRigbyClient _client;
    public PlayerSongsRoutes(HttpRigbyClient client) => _client = client;

    public Task<JsonElement> List(object data, CancellationToken ct = default) =>
        _client.CallAsync(new[] { "rpc", "gdps", "player", "songs", "list" }, data, ct);

    public Task<JsonElement> Create(object data, CancellationToken ct = default) =>
        _client.CallAsync(new[] { "rpc", "gdps", "player", "songs", "create" }, data, ct);

    public Task<JsonElement> CreateFromNewgrounds(object data, CancellationToken ct = default) =>
        _client.CallAsync(new[] { "rpc", "gdps", "player", "songs", "createFromNewgrounds" }, data, ct);

    public Task<JsonElement> CreateFromUrl(object data, CancellationToken ct = default) =>
        _client.CallAsync(new[] { "rpc", "gdps", "player", "songs", "createFromUrl" }, data, ct);
}

public class PlayerRoutes
{
    private readonly HttpRigbyClient _client;
    public PlayerSongsRoutes Songs { get; }
    public PlayerRoutes(HttpRigbyClient client)
    {
        _client = client;
        Songs = new PlayerSongsRoutes(client);
    }

    public Task<JsonElement> Login(object data, CancellationToken ct = default) =>
        _client.CallAsync(new[] { "rpc", "gdps", "player", "login" }, data, ct);

    public Task<JsonElement> Profile(object data, CancellationToken ct = default) =>
        _client.CallAsync(new[] { "rpc", "gdps", "player", "profile" }, data, ct);
}

public class PlayersRoutes
{
    private readonly HttpRigbyClient _client;
    public PlayersRoutes(HttpRigbyClient client) => _client = client;

    public Task<JsonElement> List(object data, CancellationToken ct = default) =>
        _client.CallAsync(new[] { "rpc", "gdps", "players", "list" }, data, ct);

    public Task<JsonElement> Assign(object data, CancellationToken ct = default) =>
        _client.CallAsync(new[] { "rpc", "gdps", "players", "assign" }, data, ct);
}

public class PublicRoutes
{
    private readonly HttpRigbyClient _client;
    public PublicRoutes(HttpRigbyClient client) => _client = client;

    public Task<JsonElement> Page(object data, CancellationToken ct = default) =>
        _client.CallAsync(new[] { "rpc", "gdps", "public", "page" }, data, ct);
}

public class QuestsRoutes
{
    private readonly HttpRigbyClient _client;
    public QuestsRoutes(HttpRigbyClient client) => _client = client;

    public Task<JsonElement> List(object data, CancellationToken ct = default) =>
        _client.CallAsync(new[] { "rpc", "gdps", "quests", "list" }, data, ct);

    public Task<JsonElement> Create(object data, CancellationToken ct = default) =>
        _client.CallAsync(new[] { "rpc", "gdps", "quests", "create" }, data, ct);

    public Task<JsonElement> Remove(object data, CancellationToken ct = default) =>
        _client.CallAsync(new[] { "rpc", "gdps", "quests", "remove" }, data, ct);
}

public class RolesRoutes
{
    private readonly HttpRigbyClient _client;
    public RolesRoutes(HttpRigbyClient client) => _client = client;

    public Task<JsonElement> List(object data, CancellationToken ct = default) =>
        _client.CallAsync(new[] { "rpc", "gdps", "roles", "list" }, data, ct);

    public Task<JsonElement> Create(object data, CancellationToken ct = default) =>
        _client.CallAsync(new[] { "rpc", "gdps", "roles", "create" }, data, ct);

    public Task<JsonElement> Update(object data, CancellationToken ct = default) =>
        _client.CallAsync(new[] { "rpc", "gdps", "roles", "update" }, data, ct);

    public Task<JsonElement> Remove(object data, CancellationToken ct = default) =>
        _client.CallAsync(new[] { "rpc", "gdps", "roles", "delete" }, data, ct);
}

public class ServerRoutes
{
    private readonly HttpRigbyClient _client;
    public ServerRoutes(HttpRigbyClient client) => _client = client;

    public Task<JsonElement> ListMembers(object data, CancellationToken ct = default) =>
        _client.CallAsync(new[] { "rpc", "gdps", "listMembers" }, data, ct);

    public Task<JsonElement> ListInvites(object data, CancellationToken ct = default) =>
        _client.CallAsync(new[] { "rpc", "gdps", "listInvites" }, data, ct);

    public Task<JsonElement> CreateInvite(object data, CancellationToken ct = default) =>
        _client.CallAsync(new[] { "rpc", "gdps", "createInvite" }, data, ct);

    public Task<JsonElement> RevokeInvite(object data, CancellationToken ct = default) =>
        _client.CallAsync(new[] { "rpc", "gdps", "revokeInvite" }, data, ct);

    public Task<JsonElement> Join(object data, CancellationToken ct = default) =>
        _client.CallAsync(new[] { "rpc", "gdps", "join" }, data, ct);

    public Task<JsonElement> UpdateMember(object data, CancellationToken ct = default) =>
        _client.CallAsync(new[] { "rpc", "gdps", "updateMember" }, data, ct);

    public Task<JsonElement> RemoveMember(object data, CancellationToken ct = default) =>
        _client.CallAsync(new[] { "rpc", "gdps", "removeMember" }, data, ct);

    public Task<JsonElement> Leave(object data, CancellationToken ct = default) =>
        _client.CallAsync(new[] { "rpc", "gdps", "leave" }, data, ct);

    public Task<JsonElement> List(CancellationToken ct = default) =>
        _client.CallAsync(new[] { "rpc", "gdps", "list" }, null, ct);

    public Task<JsonElement> MySrvs(CancellationToken ct = default) =>
        _client.CallAsync(new[] { "rpc", "gdps", "mysrvs" }, null, ct);

    public Task<JsonElement> CreateSrv(object data, CancellationToken ct = default) =>
        _client.CallAsync(new[] { "rpc", "gdps", "createsrv" }, data, ct);

    public Task<JsonElement> DeleteSrv(object data, CancellationToken ct = default) =>
        _client.CallAsync(new[] { "rpc", "gdps", "deletesrv" }, data, ct);

    public Task<JsonElement> GetInfo(object data, CancellationToken ct = default) =>
        _client.CallAsync(new[] { "rpc", "gdps", "getinfo" }, data, ct);

    public Task<JsonElement> TogglePublic(object data, CancellationToken ct = default) =>
        _client.CallAsync(new[] { "rpc", "gdps", "togglePublic" }, data, ct);

    public Task<JsonElement> SubmitExternal(object data, CancellationToken ct = default) =>
        _client.CallAsync(new[] { "rpc", "gdps", "submitExternal" }, data, ct);
}
