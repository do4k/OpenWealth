using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OpenWealth.Api.Data;
using OpenWealth.Api.Models;
using OpenWealth.Api.Services;

namespace OpenWealth.Tests;

public sealed class HouseholdServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private readonly HouseholdService _service;
    private readonly User _alice;
    private readonly User _bob;

    public HouseholdServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection).Options);
        _db.Database.EnsureCreated();
        _service = new HouseholdService(_db);

        _alice = NewUser("alice@example.com", "Alice");
        _bob = NewUser("bob@example.com", "Bob");
        _db.Users.AddRange(_alice, _bob);
        _db.SaveChanges();
    }

    private static User NewUser(string email, string name) => new()
    {
        Id = Guid.NewGuid(), Email = email, DisplayName = name, PasswordHash = "x",
    };

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task InviteRequiresExistingAccount()
    {
        var error = await Assert.ThrowsAsync<HouseholdError>(
            () => _service.InviteAsync(_alice.Id, "nobody@example.com"));
        Assert.Contains("owned by the person it describes", error.Message);
    }

    [Fact]
    public async Task CannotInviteSelf()
    {
        await Assert.ThrowsAsync<HouseholdError>(
            () => _service.InviteAsync(_alice.Id, _alice.Email));
    }

    [Fact]
    public async Task InviteAcceptFlowLinksBothSides()
    {
        var link = await _service.InviteAsync(_alice.Id, _bob.Email);

        var bobView = await _service.GetViewAsync(_bob.Id);
        Assert.Single(bobView.InvitesReceived);
        Assert.Empty(bobView.Members);

        await _service.RespondAsync(_bob.Id, link.Id, accept: true);

        Assert.Equal("Bob", Assert.Single((await _service.GetViewAsync(_alice.Id)).Members).DisplayName);
        Assert.Equal("Alice", Assert.Single((await _service.GetViewAsync(_bob.Id)).Members).DisplayName);
        Assert.Equal([_bob.Id], await _service.AcceptedPartnerIdsAsync(_alice.Id));
    }

    [Fact]
    public async Task InviterCannotAcceptOwnInvite()
    {
        var link = await _service.InviteAsync(_alice.Id, _bob.Email);
        await Assert.ThrowsAsync<HouseholdError>(
            () => _service.RespondAsync(_alice.Id, link.Id, accept: true));
    }

    [Fact]
    public async Task DecliningRemovesTheInvite()
    {
        var link = await _service.InviteAsync(_alice.Id, _bob.Email);
        await _service.RespondAsync(_bob.Id, link.Id, accept: false);

        Assert.Empty((await _service.GetViewAsync(_alice.Id)).InvitesSent);
        Assert.Empty(await _db.HouseholdLinks.ToListAsync());
    }

    [Fact]
    public async Task DuplicateInvitesAreRejectedInBothDirections()
    {
        await _service.InviteAsync(_alice.Id, _bob.Email);
        await Assert.ThrowsAsync<HouseholdError>(() => _service.InviteAsync(_alice.Id, _bob.Email));
        await Assert.ThrowsAsync<HouseholdError>(() => _service.InviteAsync(_bob.Id, _alice.Email));
    }

    [Fact]
    public async Task EitherSideCanUnlink()
    {
        var link = await _service.InviteAsync(_alice.Id, _bob.Email);
        await _service.RespondAsync(_bob.Id, link.Id, accept: true);

        // Bob (the invitee) dissolves the link
        await _service.UnlinkAsync(_bob.Id, link.Id);

        Assert.Empty((await _service.GetViewAsync(_alice.Id)).Members);
        Assert.Empty(await _service.AcceptedPartnerIdsAsync(_alice.Id));
    }

    [Fact]
    public async Task StrangersCannotTouchALink()
    {
        var mallory = NewUser("mallory@example.com", "Mallory");
        _db.Users.Add(mallory);
        await _db.SaveChangesAsync();

        var link = await _service.InviteAsync(_alice.Id, _bob.Email);
        await Assert.ThrowsAsync<HouseholdError>(
            () => _service.RespondAsync(mallory.Id, link.Id, accept: true));
        await Assert.ThrowsAsync<HouseholdError>(
            () => _service.UnlinkAsync(mallory.Id, link.Id));
    }

    [Fact]
    public async Task PendingInviteGrantsNoDataAccess()
    {
        await _service.InviteAsync(_alice.Id, _bob.Email);
        Assert.Empty(await _service.AcceptedPartnerIdsAsync(_alice.Id));
        Assert.Empty(await _service.AcceptedPartnerIdsAsync(_bob.Id));
    }
}
