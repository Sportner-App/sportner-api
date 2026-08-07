# 06 — Social (Friendships, Posts, Feed)

Tables: `Friendships`, `Posts`, `PostMedia`, `PostLikes`, `PostComments`.

Domain: `src/Domain/Social/*`. Specs: `docs/database/16`–`20`.

Depends on: [01-identity.md](01-identity.md). Notifications side effects: [07-notifications.md](07-notifications.md).

There is **no Feed entity** — feed is a read model over posts + friendships + visibility/blocks.

---

## Progress

- [x] Friendships (request / accept / reject / block / remove / list)
- [x] Posts CRUD + media
- [x] Likes
- [x] Comments / replies (1-level)
- [x] Feed / explore queries

---

## Controllers

| Controller | Base route |
| ---------- | ---------- |
| `FriendshipsController` | `/api/friendships` |
| `PostsController` | `/api/posts` |
| `UserPostsController` | `/api/users/{userId}/posts` |
| `CommentsController` | `/api/posts/{postId}/comments` |
| `CommentActionsController` | `/api/comments/{id}` |
| `FeedController` | `/api/feed` |

---

## Features

### Friendships

| Status | Use case | Type | Endpoint | Domain / notes |
| ------ | -------- | ---- | -------- | -------------- |
| [x] | `SendFriendRequest` | Command | `POST /api/friendships` | Bidirectional existence check; blocked → 403; rejected row replaced. Notify `FriendRequest`. |
| [x] | `AcceptFriendRequest` | Command | `POST /api/friendships/{id}/accept` | Addressee only; bumps both `FriendsCount`; `FIRST_FRIEND`; `FriendAccepted`. |
| [x] | `RejectFriendRequest` | Command | `POST /api/friendships/{id}/reject` | Addressee only. |
| [x] | `BlockUser` | Command | `POST /api/friendships/block` | Creates relationship if missing; decreases friends counts when leaving Accepted. |
| [x] | `RemoveFriendship` | Command | `DELETE /api/friendships/{id}` | Accepted only; physical delete; decrease both counts. |
| [x] | `ListFriends` | Query | `GET /api/friendships` | Accepted; paginated. |
| [x] | `ListPendingRequests` | Query | `GET /api/friendships/pending?outgoing=` | Incoming default; optional outgoing. |

### Posts

| Status | Use case | Type | Endpoint | Domain / notes |
| ------ | -------- | ---- | -------- | -------------- |
| [x] | `CreatePost` | Command | `POST /api/posts` | Multipart content + files; `ValidatePublishable`; `PostsCount++`; `FIRST_POST`. |
| [x] | `UpdatePostContent` | Command | `PUT /api/posts/{id}` | Owner. |
| [x] | `AddPostMedia` | Command | `POST /api/posts/{id}/media` | `post-media` bucket. |
| [x] | `RemovePostMedia` | Command | `DELETE /api/posts/{id}/media/{mediaId}` | DB then best-effort storage delete. |
| [x] | `ReorderPostMedia` | Command | `PUT /api/posts/{id}/media/order` | |
| [x] | `DeletePost` | Command | `DELETE /api/posts/{id}` | Cascades likes/comments in app; storage cleanup; `PostsCount--`. |
| [x] | `GetPostById` | Query | `GET /api/posts/{id}` | `likedByMe`; blocked authors forbidden. |
| [x] | `ListPostsByUser` | Query | `GET /api/users/{userId}/posts` | Cursor; block rules. |

### Likes

| Status | Use case | Type | Endpoint | Domain / notes |
| ------ | -------- | ---- | -------- | -------------- |
| [x] | `LikePost` | Command | `POST /api/posts/{id}/likes` | No self-like; unique; notify `PostLiked`. |
| [x] | `UnlikePost` | Command | `DELETE /api/posts/{id}/likes` | |

### Comments

| Status | Use case | Type | Endpoint | Domain / notes |
| ------ | -------- | ---- | -------- | -------------- |
| [x] | `CreateComment` | Command | `POST /api/posts/{postId}/comments` | Root; notify `PostCommented`. |
| [x] | `CreateReply` | Command | `POST /api/posts/{postId}/comments/{parentId}/replies` | One nesting level; notify `CommentReplied`. |
| [x] | `UpdateComment` | Command | `PUT /api/comments/{id}` | Owner. |
| [x] | `DeleteComment` | Command | `DELETE /api/comments/{id}` | Deletes replies with root; maintains counters. |
| [x] | `ListComments` | Query | `GET /api/posts/{postId}/comments` | Root comments cursor; `replyCount` on each. |

### Feed

| Status | Use case | Type | Endpoint | Notes |
| ------ | -------- | ---- | -------- | ----- |
| [x] | `GetHomeFeed` | Query | `GET /api/feed` | Self + accepted friends; exclude blocks; cursor. |
| [x] | `GetExploreFeed` | Query | `GET /api/feed/explore` | Recency; exclude blocks. |

---

## Exit criteria

- [x] Friendship state machine covered
- [x] Post with media create/delete cleans storage (best-effort after commit)
- [x] Like/comment counters stay non-negative and accurate
- [x] Home feed paginated
