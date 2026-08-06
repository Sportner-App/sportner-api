# 06 — Social (Friendships, Posts, Feed)

Tables: `Friendships`, `Posts`, `PostMedia`, `PostLikes`, `PostComments`.

Domain: `src/Domain/Social/*`. Specs: `docs/database/16`–`20`.

Depends on: [01-identity.md](01-identity.md). Notifications side effects: [07-notifications.md](07-notifications.md).

There is **no Feed entity** — feed is a read model over posts + friendships + visibility/blocks.

---

## Progress

- [ ] Friendships (request / accept / reject / block / remove / list)
- [ ] Posts CRUD + media
- [ ] Likes
- [ ] Comments / replies (1-level)
- [ ] Feed / explore queries

---

## Controllers

| Controller | Base route |
| ---------- | ---------- |
| `FriendshipsController` | `/api/friendships` |
| `PostsController` | `/api/posts` |
| `CommentsController` | `/api/posts/{postId}/comments` |
| `FeedController` (optional) | `/api/feed` |

Like/unlike can live on `PostsController`.

---

## Features

### Friendships

| Status | Use case | Type | Endpoint | Domain / notes |
| ------ | -------- | ---- | -------- | -------------- |
| [ ] | `SendFriendRequest` | Command | `POST /api/friendships` | `Friendship.CreateRequest`. Bidirectional existence check before insert. No self; respect blocks. Notify `FriendRequest`. |
| [ ] | `AcceptFriendRequest` | Command | `POST /api/friendships/{id}/accept` | Notify `FriendAccepted`; bump both `FriendsCount`. Badge `FIRST_FRIEND`. |
| [ ] | `RejectFriendRequest` | Command | `POST /api/friendships/{id}/reject` | |
| [ ] | `BlockUser` | Command | `POST /api/friendships/block` | `Block`; sets `BlockedByUserId`. |
| [ ] | `RemoveFriendship` | Command | `DELETE /api/friendships/{id}` | Accepted → delete row; decrease friends counts. |
| [ ] | `ListFriends` | Query | `GET /api/friendships` | Accepted only. |
| [ ] | `ListPendingRequests` | Query | `GET /api/friendships/pending` | Incoming (and optionally outgoing). |

### Posts

| Status | Use case | Type | Endpoint | Domain / notes |
| ------ | -------- | ---- | -------- | -------------- |
| [ ] | `CreatePost` | Command | `POST /api/posts` | `Post.Create` + media via aggregate; `ValidatePublishable`; require `CanCreateContent`. Max 10 media. Increment `PostsCount`. Badge `FIRST_POST`. |
| [ ] | `UpdatePostContent` | Command | `PUT /api/posts/{id}` | Owner. |
| [ ] | `AddPostMedia` | Command | `POST /api/posts/{id}/media` | Storage upload + `AddMedia`. |
| [ ] | `RemovePostMedia` | Command | `DELETE /api/posts/{id}/media/{mediaId}` | Delete storage object too. |
| [ ] | `ReorderPostMedia` | Command | `PUT /api/posts/{id}/media/order` | |
| [ ] | `DeletePost` | Command | `DELETE /api/posts/{id}` | Physical delete; orchestrate comments/likes/reports/notifications + storage. DB cascade ≠ storage cleanup. Decrement `PostsCount`. |
| [ ] | `GetPostById` | Query | `GET /api/posts/{id}` | Include liked-by-me flag. |
| [ ] | `ListPostsByUser` | Query | `GET /api/users/{userId}/posts` | Cursor; visibility/block rules. |

### Likes

| Status | Use case | Type | Endpoint | Domain / notes |
| ------ | -------- | ---- | -------- | -------------- |
| [ ] | `LikePost` | Command | `POST /api/posts/{id}/likes` | `PostLike.Create`; unique; no self-like; `IncreaseLikeCount`; notify `PostLiked`. |
| [ ] | `UnlikePost` | Command | `DELETE /api/posts/{id}/likes` | Decrease count. |

### Comments

| Status | Use case | Type | Endpoint | Domain / notes |
| ------ | -------- | ---- | -------- | -------------- |
| [ ] | `CreateComment` | Command | `POST /api/posts/{postId}/comments` | Root: `CreateRoot`; notify post owner `PostCommented`. |
| [ ] | `CreateReply` | Command | `POST /api/posts/{postId}/comments/{parentId}/replies` | One nesting level; notify `CommentReplied`. |
| [ ] | `UpdateComment` | Command | `PUT /api/comments/{id}` | Owner. |
| [ ] | `DeleteComment` | Command | `DELETE /api/comments/{id}` | Physical delete; maintain `comment_count` / `reply_count` in app (parent Restrict). |
| [ ] | `ListComments` | Query | `GET /api/posts/{postId}/comments` | Cursor; lazy-load replies. |

### Feed (read models)

| Status | Use case | Type | Endpoint | Notes |
| ------ | -------- | ---- | -------- | ----- |
| [ ] | `GetHomeFeed` | Query | `GET /api/feed` | Posts from accepted friends (+ self), exclude blocks, cursor. |
| [ ] | `GetExploreFeed` | Query | `GET /api/feed/explore` | Public posts discovery; simple recency first. |

---

## Exit criteria

- [ ] Friendship state machine covered
- [ ] Post with media create/delete cleans storage
- [ ] Like/comment counters stay non-negative and accurate
- [ ] Home feed paginated
