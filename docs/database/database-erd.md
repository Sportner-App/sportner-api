# Database ERD

This document describes the logical relationships between all database tables.

The database is organized by bounded contexts following Domain Driven Design principles.

---

# Modules

Identity

- users
- user_profiles
- sports
- user_sports
- user_statistics
- user_sessions
- user_devices
- user_saved_locations

Events

- events
- event_participants
- event_waitlist

Messaging

- conversations
- conversation_members
- messages

Reviews

- reviews

Social

- friendships
- user_blocks
- posts
- post_media
- post_likes
- post_comments
- notifications
- notification_settings

Gamification

- badges
- user_badges

Moderation

- reports
- report_reasons

---

# Entity Relationships

## Users

users

├── user_profiles (1 : 1)

├── user_statistics (1 : 1)

├── notification_settings (1 : N)

├── user_devices (1 : N)

├── user_sessions (1 : N)

├── user_saved_locations (1 : N)

├── user_sports (1 : N)

├── events (1 : N)

├── event_participants (1 : N)

├── event_waitlist (1 : N)

├── conversations (Owner - Future)

├── conversation_members (1 : N)

├── messages (1 : N)

├── reviews (Reviewer)

├── reviews (Reviewed)

├── friendships (Requester)

├── friendships (Addressee)

├── user_blocks (Blocker)

├── user_blocks (Blocked)

├── posts (1 : N)

├── post_likes (1 : N)

├── post_comments (1 : N)

├── notifications (Recipient)

├── notifications (Actor)

├── user_badges (1 : N)

├── reports (Reporter)

└── reports (Moderator)

---

## Sports

sports

├── user_sports

└── events

---

## Events

events

├── event_participants

├── event_waitlist

├── conversations

└── reviews

---

## Conversations

conversations

└── conversation_members

---

## Posts

posts

├── post_media

├── post_likes

├── post_comments

└── reports

---

## Comments

post_comments

├── post_comments (Self Reference)

├── users (reply_to_user_id)

└── reports

---

## Messages

messages

├── conversations (reference by conversation_id)

├── messages (Reply)

└── reports

---

## Badges

badges

└── user_badges

---

## Reports

report_reasons

└── reports

---

# Aggregate Roots

Identity

User

↓

user_profiles

↓

user_statistics

↓

user_devices

↓

user_sessions

↓

user_saved_locations

↓

user_sports

---

Events

Event

↓

event_participants

↓

event_waitlist

↓

conversation

↓

reviews

---

Messaging

Conversation

↓

conversation_members

Message (separate aggregate root)

↓

references Conversation

↓

messages (self-reply)

---

Social

Post

↓

post_media

↓

post_likes

↓

post_comments

---

Gamification

Badge

↓

user_badges

---

Moderation

Report

↓

report_reasons

---

# Cross Module Relationships

User

↓

creates

↓

Event

↓

owns

↓

Conversation

↓

contains

↓

ConversationMember

Message

↓

references

↓

Conversation

---

User

↓

creates

↓

Post

↓

owns

↓

Media

↓

receives

↓

Likes

↓

receives

↓

Comments

↓

creates

↓

Notifications

---

User

↓

joins

↓

Event

↓

can review

↓

User

---

User

↓

earns

↓

Badge

---

User

↓

reports

↓

User / Event / Post / Comment / Review / Message

---

# Cardinality Summary

users 1 ---- 1 user_profiles

users 1 ---- 1 user_statistics

users 1 ---- N user_devices

users 1 ---- N user_sessions

users 1 ---- N user_saved_locations

users 1 ---- N user_sports

sports 1 ---- N user_sports

sports 1 ---- N events

users 1 ---- N events

events 1 ---- N event_participants

events 1 ---- N event_waitlist

events 1 ---- 1 conversations

conversations 1 ---- N conversation_members

conversations 1 ---- N messages

messages 1 ---- N messages (Replies)

users 1 ---- N posts

posts 1 ---- N post_media

posts 1 ---- N post_likes

posts 1 ---- N post_comments

post_comments 1 ---- N post_comments (Replies)

users 1 ---- N notifications

users 1 ---- N friendships
users 1 ---- N user_blocks (blocker)
users 1 ---- N user_blocks (blocked)

badges 1 ---- N user_badges

users 1 ---- N user_badges

report_reasons 1 ---- N reports

---

# Notes

- All primary keys use UUID.
- All timestamps use TIMESTAMPTZ.
- Soft delete is not used.
- Media files are stored in Supabase Storage.
- Aggregate boundaries are enforced by backend business logic.
- Entity Framework configurations are implemented using Fluent API.
- Refresh tokens are stored only in `user_sessions`.
- Notification delivery is controlled by `notification_settings`.
- Reports use a polymorphic relationship through `entity_type` and `entity_id`.
