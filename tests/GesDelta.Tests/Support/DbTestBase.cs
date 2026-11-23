using GesDelta.Core.Enums;
using GesDelta.Core.Models;
using GesDelta.Storage;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace GesDelta.Tests.Support;

public abstract class DbTestBase
{
    private PostgreSqlContainer _pg = null!;
    private DbContextOptions<GesDeltaDbContext> _options = null!;

    [OneTimeSetUp]
    public async Task StartDb()
    {
        _pg = new PostgreSqlBuilder().WithImage("postgres:17").Build();
        await _pg.StartAsync();

        _options = new DbContextOptionsBuilder<GesDeltaDbContext>()
            .UseNpgsql(_pg.GetConnectionString())
            .UseSnakeCaseNamingConvention()
            .Options;

        await using var db = NewDb();
        await db.Database.MigrateAsync();
    }

    [OneTimeTearDown]
    public async Task StopDb() => await _pg.DisposeAsync();

    protected GesDeltaDbContext NewDb() => new(_options);

    protected sealed record Seed(long TopicId, long SourceId, long RunId);

    protected async Task<Seed> SeedAsync(string sourceName = "s")
    {
        await using var db = NewDb();
        var topic = new Topic { Name = "t" };
        var source = new Source
        {
            Topic = topic, Name = sourceName, Type = SourceType.Rss,
            Url = "https://example.com/feed"
        };
        var run = new Run
        {
            Topic = topic, StartedAt = DateTime.UtcNow,
            Trigger = RunTrigger.Manual, Status = RunStatus.Running
        };
        db.AddRange(topic, source, run);
        await db.SaveChangesAsync();
        return new Seed(topic.Id, source.Id, run.Id);
    }

    protected async Task<long> AddRunAsync(long topicId)
    {
        await using var db = NewDb();
        var run = new Run
        {
            TopicId = topicId, StartedAt = DateTime.UtcNow,
            Trigger = RunTrigger.Scheduled, Status = RunStatus.Succeeded
        };
        db.Runs.Add(run);
        await db.SaveChangesAsync();
        return run.Id;
    }

    protected async Task<long> AddSourceAsync(long topicId, string name)
    {
        await using var db = NewDb();
        var source = new Source
        {
            TopicId = topicId, Name = name, Type = SourceType.Rss,
            Url = "https://example.com/other"
        };
        db.Sources.Add(source);
        await db.SaveChangesAsync();
        return source.Id;
    }
}