// services/csharp/Program.cs
using System.Net;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

string service = Environment.GetEnvironmentVariable("SERVICE_NAME") ?? "ms-csharp";

string connDefault =
    builder.Configuration.GetConnectionString("Default")
    ?? Environment.GetEnvironmentVariable("DB_CONN")
    ?? "Server=sqlserver,1433;Database=messages;User Id=sa;Password=YourStrong!Passw0rd;Encrypt=True;TrustServerCertificate=True;Connection Timeout=30";

// issuer = como o token foi emitido (localhost, fora da rede Docker)
// metadata = onde buscar a configuracao/chaves (nome interno do container)
string issuer = Environment.GetEnvironmentVariable("OIDC_ISSUER") ?? "http://localhost:8080/realms/lab";
string audience = Environment.GetEnvironmentVariable("OIDC_AUDIENCE") ?? "lab-api";
string metadata = Environment.GetEnvironmentVariable("OIDC_METADATA")
    ?? "http://keycloak:8080/realms/lab/.well-known/openid-configuration";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
  .AddJwtBearer(options =>
  {
      options.MetadataAddress = metadata;
      options.RequireHttpsMetadata = false; // ambiente de lab
      options.TokenValidationParameters = new TokenValidationParameters
      {
          ValidateAudience = true,
          ValidAudience = audience,
          ValidateIssuer = true,
          ValidIssuer = issuer
      };
  });
builder.Services.AddAuthorization();

// troca Database/Initial Catalog por 'master'
string ToMaster(string cs)
{
    var b = new SqlConnectionStringBuilder(cs);
    b.InitialCatalog = "master";
    return b.ToString();
}

async Task EnsureDatabaseAndTableAsync()
{
    // 1) cria DB se faltar
    await using (var cn = new SqlConnection(ToMaster(connDefault)))
    {
        await cn.OpenAsync();
        using var cmd = new SqlCommand(
            "IF DB_ID('messages') IS NULL CREATE DATABASE messages;", cn);
        await cmd.ExecuteNonQueryAsync();
    }

    // 2) cria tabela se faltar
    await using (var cn = new SqlConnection(connDefault))
    {
        await cn.OpenAsync();
        using var cmd = new SqlCommand(@"
            IF OBJECT_ID('dbo.messages','U') IS NULL
            BEGIN
              CREATE TABLE dbo.messages (
                id INT IDENTITY(1,1) PRIMARY KEY,
                text NVARCHAR(255) NOT NULL
              );
            END", cn);
        await cmd.ExecuteNonQueryAsync();
    }
}
await EnsureDatabaseAndTableAsync();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Json(new { status = "ok", service, hostname = Dns.GetHostName() }));

app.MapGet("/messages", async () =>
{
    var list = new List<object>();
    await using var cn = new SqlConnection(connDefault);
    await cn.OpenAsync();
    using var cmd = new SqlCommand("SELECT id, text FROM dbo.messages ORDER BY id", cn);
    using var rd = await cmd.ExecuteReaderAsync();
    while (await rd.ReadAsync()) list.Add(new { id = rd.GetInt32(0), text = rd.GetString(1) });
    return Results.Json(list);
});

app.MapPost("/messages", async (HttpRequest req) =>
{
    using var sr = new StreamReader(req.Body);
    var text = System.Text.Json.JsonDocument.Parse(await sr.ReadToEndAsync())
                 .RootElement.GetProperty("text").GetString() ?? "";

    await using var cn = new SqlConnection(connDefault);
    await cn.OpenAsync();
    using var cmd = new SqlCommand("INSERT INTO dbo.messages(text) OUTPUT INSERTED.id VALUES (@t)", cn);
    cmd.Parameters.AddWithValue("@t", text);
    var id = (int)await cmd.ExecuteScalarAsync();
    return Results.Json(new { id, text }, statusCode: 201);
}).RequireAuthorization();

app.Run("http://0.0.0.0:8080");
