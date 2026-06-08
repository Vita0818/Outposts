# Kikaria

Kikaria is a local-first iOS memorization assistant.

The app helps users import structured Markdown study materials, randomly review knowledge points by tag, reveal hints or full content when needed, and collect weak points into a reinforcement list.

## Platform

- iOS
- Swift
- SwiftUI
- Local storage only
- No account system
- No cloud sync
- No network dependency in v0.1

## v0.1 Goal

Build a simple local iOS app that supports:

1. Importing knowledge points from a Markdown file.
2. Parsing each knowledge point into:
   - title
   - tags
   - hint
   - content
3. Selecting one or more tags for review.
4. Randomly showing a knowledge point title.
5. Allowing the user to reveal the hint.
6. Allowing the user to reveal the full content.
7. Allowing the user to add a knowledge point to a reinforcement list.
8. Showing all reinforced knowledge points in a separate page.

## Design Principle

Keep the first version simple, stable, and local.

Do not add AI, OCR, login, cloud sync, statistics, subscription, or complex spaced repetition in v0.1.
