using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Domain.Common.Enums;

namespace Sportner.API.Hubs;

/// <summary>
/// Conversation realtime hub for Event / Direct / Group.
/// Membership is always checked against <c>ConversationMembers</c>.
/// </summary>
[Authorize]
public sealed class ConversationHub : Hub
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<ConversationHub> _logger;

    public ConversationHub(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        ILogger<ConversationHub> logger)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _logger = logger;
    }

    public static string GroupName(Guid conversationId) => $"conversation:{conversationId:D}";

    public async Task JoinConversation(Guid conversationId)
    {
        if (_currentUser.UserId is not { } userId)
        {
            throw new HubException("Authentication is required.");
        }

        await EnsureMemberAsync(conversationId, userId);

        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            GroupName(conversationId),
            Context.ConnectionAborted);

        _logger.LogDebug(
            "User {UserId} joined conversation group {ConversationId}.",
            userId,
            conversationId);
    }

    public Task LeaveConversation(Guid conversationId) =>
        Groups.RemoveFromGroupAsync(
            Context.ConnectionId,
            GroupName(conversationId),
            Context.ConnectionAborted);

    /// <summary>
    /// Ephemeral typing indicator — not persisted. Client should stop after ~3s locally.
    /// </summary>
    public async Task Typing(Guid conversationId)
    {
        if (_currentUser.UserId is not { } userId)
        {
            throw new HubException("Authentication is required.");
        }

        await EnsureMemberAsync(conversationId, userId);

        await Clients.OthersInGroup(GroupName(conversationId))
            .SendAsync(
                "UserTyping",
                new { conversationId, userId },
                Context.ConnectionAborted);
    }

    private async Task EnsureMemberAsync(Guid conversationId, Guid userId)
    {
        var isMember = await _dbContext.ConversationMembers.AsNoTracking()
            .AnyAsync(
                member =>
                    member.ConversationId == conversationId
                    && member.UserId == userId
                    && member.LeftAt == null,
                Context.ConnectionAborted);

        if (!isMember)
        {
            _logger.LogWarning(
                "User {UserId} tried to use conversation {ConversationId} without membership.",
                userId,
                conversationId);

            throw new HubException("You are not a member of this conversation.");
        }

        var conversation = await _dbContext.Conversations.AsNoTracking()
            .Where(candidate => candidate.Id == conversationId)
            .Select(candidate => new { candidate.Type })
            .FirstOrDefaultAsync(Context.ConnectionAborted);

        if (conversation?.Type is ConversationType.Direct)
        {
            var peerId = await _dbContext.ConversationMembers.AsNoTracking()
                .Where(member =>
                    member.ConversationId == conversationId
                    && member.UserId != userId
                    && member.LeftAt == null)
                .Select(member => member.UserId)
                .FirstOrDefaultAsync(Context.ConnectionAborted);

            if (peerId != Guid.Empty)
            {
                var blocked = await _dbContext.UserBlocks.AsNoTracking()
                    .AnyAsync(
                        block =>
                            (block.BlockerUserId == userId && block.BlockedUserId == peerId)
                            || (block.BlockerUserId == peerId && block.BlockedUserId == userId),
                        Context.ConnectionAborted);

                if (blocked)
                {
                    throw new HubException("You are not a member of this conversation.");
                }
            }
        }
    }
}
