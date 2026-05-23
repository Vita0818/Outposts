# Kikaria Android Kotlin String Interpolation Compile Repair Pass

This is a focused compile repair pass for Kikaria-Android.

Do not perform UI reconstruction in this pass.

Do not migrate new screens in this pass.

Do not add features in this pass.

Do not redesign the app in this pass.

The current repeated compiler errors are caused by invalid Kotlin string interpolation inside Markdown or LaTeX example text.

The visible example is in:

Kikaria-Android/app/src/main/java/com/vita0818/kikaria/ui/guide/MarkdownFormatGuideScreen.kt

around line 148, where a raw triple-quoted Kotlin string contains LaTeX text such as:

\$f(x)=x^2\$

This is wrong in Kotlin raw strings.

In Kotlin, raw triple-quoted strings still treat $ as string interpolation. Backslash does not escape $ inside raw strings.

Therefore text like:

\$f(x)=x^2\$
$x$
$t$
$2x$
$\lim_{x \to 0}$

may be parsed as Kotlin interpolation and produce compiler errors such as:

Unresolved reference: f
Unresolved reference: x
Unresolved reference: t
Unresolved reference: get

Your task is to fix all such Kotlin string interpolation problems across Kikaria-Android.

All writes must stay under Kikaria-Android.

Do not modify files outside Kikaria-Android.

Do not modify source Kikaria repository files.

Do not modify Forgis runtime files.

Do not modify GitHub workflow files.

Required files to inspect first:

Kikaria-Android/app/src/main/java/com/vita0818/kikaria/ui/guide/MarkdownFormatGuideScreen.kt
Kikaria-Android/app/src/main/java/com/vita0818/kikaria/ui/scope/ScopeSelectionScreen.kt
Kikaria-Android/app/src/main/java/com/vita0818/kikaria/ui/settings/SettingsScreen.kt

Then search all Kotlin files under Kikaria-Android for suspicious Markdown or LaTeX string content containing dollar signs.

Search for patterns including:

\$
$f
$x
$t
$2
${
\\$
LaTeX
lim
frac
sin
cos
Markdown
CodeBlock

Fix policy:

1. In Kotlin raw triple-quoted strings, do not use backslash to escape dollar signs.
2. Replace literal dollar signs that should appear in user-facing text with Kotlin-safe syntax:
   ${'$'}
3. For example, a desired user-facing string:
   $f(x)=x^2$
   should be represented in Kotlin as:
   ${'$'}f(x)=x^2${'$'}
4. A desired user-facing string:
   \lim_{x \to 0} \frac{\sin x}{x}=1
   does not require escaping unless it is wrapped in dollar signs.
5. If the text is inside a normal Kotlin string, \$ is acceptable, but prefer consistency and avoid mixing unsafe raw-string interpolation.
6. Do not remove the LaTeX examples just to hide the compiler error.
7. Preserve the intended Markdown and LaTeX guide content.
8. If a multiline raw string contains many LaTeX dollar delimiters, either:
   - replace every literal $ with ${'$'}, or
   - convert it into a normal escaped string if that is clearer.
9. Do not introduce new unresolved references.
10. Do not leave partially broken examples.

After fixing MarkdownFormatGuideScreen.kt, run or request compilation.

Preferred command:

cd Kikaria-Android && ./gradlew :app:compileDebugKotlin --no-daemon --stacktrace

If the runner cannot execute Gradle, inspect compiler logs and continue repairing the next compiler error.

After each compile attempt, fix the first remaining compiler error.

Continue until compileDebugKotlin passes or until the first remaining compiler error is clearly reported.

Important:

Do not stop after fixing only MarkdownFormatGuideScreen.kt if ScopeSelectionScreen.kt or SettingsScreen.kt still has compile errors.

Do not claim build success unless compileDebugKotlin actually passes.

Before final summary, use git_status and git_diff.

Final summary must include:

1. Kotlin files inspected
2. Kotlin files modified
3. All string interpolation errors fixed
4. The exact reason the old code failed
5. The exact compile command run
6. Whether compileDebugKotlin passed
7. If compilation still fails, the first remaining compiler error
8. Confirmation that all writes stayed inside Kikaria-Android
