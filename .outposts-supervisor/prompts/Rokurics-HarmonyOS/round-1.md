MODEL_CHECK_RESULT
First check the current actual model before reading source, modifying files, building, or testing.
Normalize the model string by lowercasing, trimming, removing spaces/hyphens/underscores, and removing runtime/context/billing suffixes such as [1m], [200k], (api), or API Usage Billing. The normalized core model must be deepseekv4pro.
If it is not deepseekv4pro, stop immediately. Do not read source, modify files, build, or test. Output only MODEL_CHECK_RESULT: FAIL, CURRENT_MODEL, NORMALIZED_MODEL, STOP_REASON.
If it is deepseekv4pro, output MODEL_CHECK_RESULT: PASS, CURRENT_MODEL, NORMALIZED_MODEL: deepseekv4pro.

PATH_CHECK_RESULT
After the model check passes, immediately run pwd.
pwd must strictly equal /Users/vita/Vitemis/Outposts/Rokurics-HarmonyOS.
If it does not strictly match, stop immediately. Do not read source, modify files, build, or test. Output only PATH_CHECK_RESULT: FAIL, EXPECTED_PWD, ACTUAL_PWD, STOP_REASON.
If it matches, output PATH_CHECK_RESULT: PASS, EXPECTED_PWD, ACTUAL_PWD.

SOURCE_READONLY_CHECK
After model and path checks pass, confirm the readonly boundary:
Apple source readonly path: /Users/vita/Vitemis/Vela/Rokurics
Target readwrite path: /Users/vita/Vitemis/Outposts/Rokurics-HarmonyOS
Apple source may only be read and must never be modified. All writes must happen only inside the target path. Do not access unrelated directories. Do not commit, push, or create PRs.
Do not read or transmit obvious secrets, certificates, tokens, private keys, .env files, Keychain data, provisioning profiles, p12 files, ssh keys, or API keys.
If you cannot guarantee this boundary, stop immediately and output SOURCE_READONLY_CHECK: FAIL.
If you can guarantee it, output SOURCE_READONLY_CHECK: PASS.

Only after MODEL_CHECK_RESULT, PATH_CHECK_RESULT, and SOURCE_READONLY_CHECK all pass may you continue.

You are Claude Code, responsible for actual migration implementation for this one project.

PROJECT_NAME: Rokurics-HarmonyOS
PLATFORM: HarmonyOS
APPLE_SOURCE_READONLY_PATH: /Users/vita/Vitemis/Vela/Rokurics
TARGET_READWRITE_PATH: /Users/vita/Vitemis/Outposts/Rokurics-HarmonyOS

User goal:
The user is migrating the Apple version of the app to Android, HarmonyOS, and Windows. You are responsible only for this target project.

Your duties:
1. Deeply read the Apple source project and understand functionality, layout, interactions, visual style, data structures, storage logic, sync logic, permission logic, and platform differences.
2. Deeply read the target project.
3. Build an Apple parity checklist.
4. Complete the target project against the Apple version.
5. Fix compile errors, runtime errors, missing functionality, UI differences, missing interactions, and missing data links.
6. Run the necessary platform build, tests, and self-checks.
7. If the build fails, continue diagnosing and fixing.
8. If the build passes but parity is incomplete, continue completing parity.

Hard boundaries:
1. Never modify the Apple source project.
2. The target project is readwrite.
3. Do not access unrelated directories.
4. Do not read or transmit obvious secrets, certificates, tokens, private keys, .env files, Keychain data, provisioning profiles, p12 files, ssh keys, or API keys.
5. Do not commit, push, create PRs, merge PRs, or perform repository publication.
6. Do not ask the user to enumerate Apple behavior; infer it from the readonly Apple source.
7. Do not only fix compilation while ignoring feature completeness.
8. Do not only implement surface UI while ignoring real data flow, state, storage, sync, permissions, and error handling.

Final structured report must include:
MODEL_CHECK_RESULT
PATH_CHECK_RESULT
SOURCE_READONLY_CHECK
PROJECT_NAME
PLATFORM
APPLE_PARITY_CHECKLIST
CHANGED_FILES
BUILD_RESULT
TEST_RESULT
IMPLEMENTED_THIS_ROUND
REMAINING_GAPS
REGRESSION_RISKS
NEXT_ROUND_RECOMMENDATION
