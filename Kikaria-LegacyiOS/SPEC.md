# Kikaria v0.1 Specification

## 1. Product Summary

Kikaria is an iOS app for assisted memorization.

Users import a Markdown file containing structured knowledge points. During review, the app randomly displays knowledge point titles. Users can choose to view a hint or the full content. After viewing the content, users can mark the knowledge point as needing reinforcement.

## 2. Markdown Import Format

Each knowledge point is separated by a line containing only:

---

Each knowledge point uses the following format:

# Knowledge Point Title

tags: tag1, tag2, tag3

hint:
This is the hint text.

content:
This is the full content text.

## 3. Example Markdown

# Limit Preservation of Sign

tags: Calculus, Limit, Basic

hint:
If the limit is positive, the function value is positive nearby.

content:
If lim f(x) = A and A > 0, then f(x) > 0 in some sufficiently small neighborhood.

---

# Rolle's Theorem

tags: Calculus, Mean Value Theorem

hint:
Continuous on closed interval, differentiable on open interval, equal endpoint values.

content:
If f is continuous on [a,b], differentiable on (a,b), and f(a)=f(b), then there exists ξ in (a,b) such that f'(ξ)=0.

## 4. Data Model

### KnowledgePoint

Fields:

- id
- title
- tags
- hint
- content
- isReinforced
- createdAt
- updatedAt

## 5. Main Screens

### Home Screen

The home screen should show:

- App name: Kikaria
- Import Markdown button
- Tag list
- Reinforcement list entry

### Tag Selection Screen

The user can select one or more tags.

After selecting tags, the user can start random review.

### Review Screen

The review screen shows:

- Knowledge point title
- Button: Show Hint
- Button: Show Content
- Button: Add to Reinforcement
- Button: Next Random Point

Initial state:

- Hint is hidden.
- Content is hidden.

After tapping Show Hint:

- Hint becomes visible.

After tapping Show Content:

- Content becomes visible.
- Add to Reinforcement becomes available.

### Reinforcement Screen

The reinforcement screen shows all knowledge points marked as needing reinforcement.

The user can:

- View the title
- View hint
- View content
- Remove from reinforcement list

## 6. Technical Requirements

- Use Swift.
- Use SwiftUI.
- Use local storage.
- Do not use third-party libraries in v0.1.
- Do not add network features.
- Do not add login.
- Do not add cloud sync.
- Keep the project easy to build in Xcode.

## 7. Development Rules for Codex

When modifying this project:

1. Do not create git commits automatically.
2. Do not introduce third-party dependencies.
3. Keep the app buildable after each task.
4. Prefer simple SwiftUI code over complex architecture.
5. If a change is large, split it into small steps.
6. After modifying code, run a build check if possible.
