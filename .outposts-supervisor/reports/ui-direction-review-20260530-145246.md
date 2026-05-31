# UI Direction Review State

RUN_ID: 20260530-145246
MODE: UI_DIRECTION_REVIEW

## Trigger

User feedback:

"有改，但是没改到点子上，方向也不对。"

This feedback overrides qwen scores, build results, test results, and any READY_FOR_USER_REVIEW style self-assessment.

## Scope

Projects for direction review:

- Kikaria-Android
- Rokurics-Android

## Rules

- Do not continue automatic UI iteration.
- Do not mark READY_FOR_USER_REVIEW.
- Do not run normal UI modification without structure diagnosis.
- Do not modify code during direction review.
- Do not run build/test during direction review.
- Do not delete or overwrite visual evidence.
- Do not clean workspace.
- Do not commit/push/PR.

## Sessions

- Kikaria-Android: direction review completed in visible Terminal window 45371. No implementation/build/test.
- Rokurics-Android: direction review completed in visible Terminal window 45372. No implementation/build/test.

## Notes

Old Terminal windows may contain unsubmitted Round 2 text from prior reporting. They must not be used for new task input to avoid accidental submission. New visible Terminal.app windows will be used for review prompts.

## Kikaria-Android Direction Review Summary

- User feedback accepted as controlling: "有改，但是没改到点子上，方向也不对。"
- qwen reviewed reference and actual screenshots. It identified Home, Today's Overview, Review History, Review, and Answer-related reference/actual comparisons.
- Wrong direction diagnosis: previous round improved visible polish, but did not fix the core Apple structure.
- Structural differences:
  - Answer screen reference has a real content card with answer/body text; actual had empty space where content should be.
  - Review reference uses two semantic chips, while actual used three category tags.
  - Reference Review/Answer screens do not show the progress bar that actual shows.
  - Actual includes a "q->" shortcut legend not present in reference.
- Visual focus differences:
  - Reference Home main button is muted and subordinate to hierarchy; actual button became too dominant.
  - Reference Answer screen visual weight is on content card; actual weight falls to bottom buttons because content is missing.
- Style differences:
  - Reference title typography is serif-like; actual uses sans-serif.
  - Reference teal is muted; actual cyan is too saturated.
- Keep: Home card layout, Review bottom button pair, Answer bottom action bar shape, dark palette.
- Replace: 3-category Review tags, empty Answer area, Review/Answer progress bar, "q->" hint, monogram avatar.
- Requires UI restructure: YES.
- Proposed next fix: start with Answer content rendering card, then semantic Review chips, remove progress bar / shortcut hint, desaturate Home button, replace avatar.

## Rokurics-Android Direction Review Summary

- User feedback accepted as controlling: "有改，但是没改到点子上，方向也不对。"
- qwen reviewed reference screenshots IMG_4653-4660 covering Home, Library, Recording Detail, AI Chat, Mac Connection, and Settings.
- Actual screenshots included Home, Library, and AI Chat; some detail/settings captures were rejected or not reliable.
- Wrong direction diagnosis: previous work applied a global Android-style bottom navigation to an iOS design whose information architecture is page-specific navigation stacks.
- Core structural problem:
  - iOS Home is a hub with a dock; Android turned the dock into global persistent navigation.
  - iOS Library is folder card grid with breadcrumb; Android still shows empty state.
  - iOS AI Chat has conversation area and input bar; Android shows a welcome placeholder and bottom tab, missing the product-critical input entry.
  - Recording detail structure is missing or not aligned.
- Visual focus difference: Android emphasizes navigation everywhere; iOS emphasizes content cards, folder/list surfaces, and input/action areas.
- Style direction difference: Android remains flatter and weaker in hierarchy; iOS uses glass card layers, mixed typography, and consistent teal accents.
- Keep: Home large plus button, dark green background, teal accent, dock visual idea.
- Replace: global BottomNavigationView model, empty Library, placeholder AI page, missing Recording Detail structure.
- Requires UI restructure: YES.
- Proposed next fix:
  - P1: replace global bottom nav with Home-only dock plus page-specific navigation stack.
  - P2: implement real content surfaces first: Library grid, AI input bar, Recording Detail.
  - P3: only then tune glass, typography, icons, and bubble placement.
- Implemented this round: NONE.
- Build/test this round: NOT_RUN / NOT_RUN.
