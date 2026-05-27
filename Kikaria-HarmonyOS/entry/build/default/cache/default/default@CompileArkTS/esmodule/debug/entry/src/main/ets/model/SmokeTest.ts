import { KnowledgePoint, KnowledgePreset, parseMarkdown, markdownTextFromPoints, countdownDays, generateId, ordinalSuffix } from "@bundle:com.vita0818.kikaria/entry/ets/model/KnowledgePoint";
export interface SmokeTestResult {
    passed: number;
    failed: number;
    failures: string[];
}
class TestRunner {
    passed: number = 0;
    failed: number = 0;
    failures: string[] = [];
    check(name: string, condition: boolean): void {
        if (condition) {
            this.passed++;
        }
        else {
            this.failed++;
            this.failures.push(`FAIL: ${name}`);
        }
    }
    result(): SmokeTestResult {
        return { passed: this.passed, failed: this.failed, failures: this.failures };
    }
}
export function runSmokeTests(): SmokeTestResult {
    const t = new TestRunner();
    // Test 1: parseMarkdown basic parsing
    const md = `# Test Point 1

tags: math, calculus

hint:
This is a hint

content:
This is the answer content

---

# Test Point 2

tags: physics

hint:
Another hint here

content:
More answer text here
`;
    const points = parseMarkdown(md);
    t.check('parseMarkdown returns 2 points', points.length === 2);
    t.check('point title parsed', points.length >= 1 && points[0].title === 'Test Point 1');
    t.check('point tags parsed', points.length >= 1 && points[0].tags.length === 2 &&
        points[0].tags[0] === 'math' && points[0].tags[1] === 'calculus');
    t.check('point hint parsed', points.length >= 1 && points[0].hint === 'This is a hint');
    t.check('point content parsed', points.length >= 1 && points[0].content === 'This is the answer content');
    // Test 2: markdownTextFromPoints roundtrip
    if (points.length >= 1) {
        const regenerated = markdownTextFromPoints([points[0]]);
        const reParsed = parseMarkdown(regenerated);
        t.check('markdownTextFromPoints roundtrip', reParsed.length === 1 &&
            reParsed[0].title === 'Test Point 1');
    }
    // Test 3: parseMarkdown handles empty input
    t.check('parseMarkdown empty input', parseMarkdown('').length === 0);
    // Test 4: parseMarkdown handles malformed input
    t.check('parseMarkdown malformed input returns empty', parseMarkdown('Just some random text\nwithout proper markers').length === 0);
    // Test 5: parseMarkdown handles missing sections
    t.check('parseMarkdown missing sections returns empty', parseMarkdown('# Just a title\n\nNo hint or content here').length === 0);
    // Test 6: KnowledgePoint reinforcement
    const kp = new KnowledgePoint(generateId(), 'Test', ['tag1'], 'hint', 'content');
    t.check('KP initial isReinforced=false', !kp.isReinforced);
    t.check('KP initial reinforcementCount=0', kp.reinforcementCount === 0);
    kp.addReinforcement();
    t.check('KP after addReinforcement isReinforced=true', kp.isReinforced);
    t.check('KP after addReinforcement count=1', kp.reinforcementCount === 1);
    kp.addReinforcement();
    t.check('KP after 2nd addReinforcement count=2', kp.reinforcementCount === 2);
    kp.clearReinforcement();
    t.check('KP after clearReinforcement isReinforced=false', !kp.isReinforced);
    t.check('KP after clearReinforcement count=0', kp.reinforcementCount === 0);
    // Test 7: KnowledgePoint mastered
    t.check('KP initial isMastered=false', !kp.isMastered);
    kp.isMastered = true;
    t.check('KP isMastered toggle', kp.isMastered);
    // Test 8: countdownDays
    const futureDate = Date.now() + 7 * 24 * 60 * 60 * 1000;
    const days = countdownDays(futureDate);
    t.check('countdownDays future non-null', days !== null);
    t.check('countdownDays future is 6 or 7', days === 6 || days === 7);
    const pastDays = countdownDays(Date.now() - 7 * 24 * 60 * 60 * 1000);
    t.check('countdownDays past returns 0', pastDays === 0);
    t.check('countdownDays null returns null', countdownDays(null) === null);
    // Test 9: generateId unique
    t.check('generateId unique', generateId() !== generateId());
    // Test 10: parseMarkdown multiline hint and content
    const mlMd = `# Multiline Test

tags: lang

hint:
Line one of hint.
Line two of hint.

Even a blank line above this one.

content:
Answer line one.
Answer line two.

Final paragraph of answer.
`;
    const mlPoints = parseMarkdown(mlMd);
    t.check('parseMarkdown multiline returns 1 point', mlPoints.length === 1);
    if (mlPoints.length >= 1) {
        t.check('multiline hint contains first line', mlPoints[0].hint.includes('Line one of hint.'));
        t.check('multiline hint contains second line', mlPoints[0].hint.includes('Line two of hint.'));
        t.check('multiline hint contains blank line separator', mlPoints[0].hint.includes('Even a blank line'));
        t.check('multiline content contains first line', mlPoints[0].content.includes('Answer line one.'));
        t.check('multiline content contains final paragraph', mlPoints[0].content.includes('Final paragraph of answer.'));
    }
    // Test 11: parseMarkdown with special characters (Markdown syntax literals)
    const scMd = `# Special *Chars* _Test_

tags: markdown, special

hint:
This hint has **bold** and *italic* and \`inline code\`.

content:
Answer with <angle brackets> and [square brackets] and (parens).
Also has a URL: https://example.com/path?q=1&v=2
`;
    const scPoints = parseMarkdown(scMd);
    t.check('parseMarkdown special chars returns 1 point', scPoints.length === 1);
    if (scPoints.length >= 1) {
        t.check('special chars title preserved', scPoints[0].title.includes('*Chars*') && scPoints[0].title.includes('_Test_'));
        t.check('special chars hint has bold marker', scPoints[0].hint.includes('**bold**'));
        t.check('special chars hint has backtick', scPoints[0].hint.includes('`inline code`'));
        t.check('special chars content has angle brackets', scPoints[0].content.includes('<angle brackets>'));
        t.check('special chars content has URL', scPoints[0].content.includes('https://example.com'));
    }
    // Test 12: parseMarkdown with empty tags
    const emptyTagsMd = `# Empty Tags

tags:

hint:
Just a hint.

content:
Just some content.
`;
    const emptyTagsPoints = parseMarkdown(emptyTagsMd);
    t.check('parseMarkdown empty tags returns 1 point', emptyTagsPoints.length === 1);
    if (emptyTagsPoints.length >= 1) {
        t.check('empty tags array is empty', emptyTagsPoints[0].tags.length === 0);
    }
    // Test 13: parseMarkdown with tags having spaces around commas
    const spacedTagsMd = `# Spaced Tags

tags:  alpha , beta , gamma

hint:
Test hint.

content:
Test content.
`;
    const spacedPoints = parseMarkdown(spacedTagsMd);
    t.check('parseMarkdown spaced tags returns 1 point', spacedPoints.length === 1);
    if (spacedPoints.length >= 1) {
        t.check('spaced tags trimmed correctly', spacedPoints[0].tags.length >= 2);
    }
    // Test 14: parseMarkdown Unicode (Chinese/Japanese/emoji) roundtrip
    const unicodeMd = `# Unicode テスト 🎉

tags: 数学, 物理, αβγ

hint:
提示：这是中文提示内容。
日本語のヒントも含まれています。
Emoji: 🎯⭐📚

content:
答案：E = mc² 是爱因斯坦的质能方程。
日本語の答え：これは答えです。
中文、日本語、English 混合。
`;
    const uniPoints = parseMarkdown(unicodeMd);
    t.check('parseMarkdown unicode returns 1 point', uniPoints.length === 1);
    if (uniPoints.length >= 1) {
        t.check('unicode title preserved', uniPoints[0].title.includes('テスト'));
        t.check('unicode title has emoji', uniPoints[0].title.includes('🎉'));
        t.check('unicode tags Chinese', uniPoints[0].tags.length >= 1 && uniPoints[0].tags.includes('数学'));
        t.check('unicode tags Greek', uniPoints[0].tags.length >= 2 && uniPoints[0].tags.includes('αβγ'));
        t.check('unicode hint contains Chinese', uniPoints[0].hint.includes('中文提示内容'));
        t.check('unicode hint contains Japanese', uniPoints[0].hint.includes('日本語'));
        t.check('unicode content contains formula', uniPoints[0].content.includes('E = mc²'));
        t.check('unicode content mixed script', uniPoints[0].content.includes('English'));
        // Roundtrip
        const rt = markdownTextFromPoints([uniPoints[0]]);
        const rtParsed = parseMarkdown(rt);
        t.check('unicode roundtrip', rtParsed.length === 1 && rtParsed[0].title.includes('テスト'));
    }
    // Test 15: parseMarkdown single point without separator
    const singleMd = `# Solo Point

tags: solo

hint:
Solo hint.

content:
Solo answer.
`;
    const singlePoints = parseMarkdown(singleMd);
    t.check('parseMarkdown single point no separator', singlePoints.length === 1);
    if (singlePoints.length >= 1) {
        t.check('single point title', singlePoints[0].title === 'Solo Point');
        t.check('single point tags', singlePoints[0].tags.length === 1 && singlePoints[0].tags[0] === 'solo');
    }
    // Test 16: parseMarkdown hint-only tags (no spaces between commas)
    const tightTagsMd = `# Tight Tags

tags: a,b,c,d

hint:
Hint.

content:
Content.
`;
    const tightPoints = parseMarkdown(tightTagsMd);
    t.check('parseMarkdown tight tags returns 1 point', tightPoints.length === 1);
    if (tightPoints.length >= 1) {
        t.check('tight tags count 4', tightPoints[0].tags.length === 4);
    }
    // Test 17: parseMarkdown with trailing whitespace on section markers
    const trailingWsMd = `# Trailing WS Title

tags:   ws1, ws2

hint:
Hint with trailing marker space.

content:
Content with trailing marker space.
`;
    const wsPoints = parseMarkdown(trailingWsMd);
    t.check('parseMarkdown trailing whitespace', wsPoints.length === 1);
    if (wsPoints.length >= 1) {
        t.check('trailing ws title trimmed', wsPoints[0].title === 'Trailing WS Title');
        t.check('trailing ws hint parsed', wsPoints[0].hint.length > 0);
        t.check('trailing ws content parsed', wsPoints[0].content.length > 0);
    }
    // Test 18: markdownTextFromPoints with multiple points produces correct separator
    if (points.length >= 2) {
        const multiMd = markdownTextFromPoints(points);
        t.check('markdownTextFromPoints multi includes separator', multiMd.includes('\n\n---\n\n'));
    }
    // Test 19: markdownTextFromPoints round-trip
    const rtMd = markdownTextFromPoints(points);
    const rtParsed = parseMarkdown(rtMd);
    t.check('roundtrip preserves count', rtParsed.length === points.length);
    // Test 20: addReinforcement increments count
    const kpA = new KnowledgePoint('a1', 'A', ['math'], 'h', 'c');
    t.check('initial reinforcementCount 0', kpA.reinforcementCount === 0);
    t.check('initial isReinforced false', !kpA.isReinforced);
    const rc = kpA.addReinforcement();
    t.check('addReinforcement returns 1', rc === 1);
    t.check('isReinforced after add', kpA.isReinforced);
    // Test 21: clearReinforcement resets
    kpA.clearReinforcement();
    t.check('clear sets count 0', kpA.reinforcementCount === 0);
    t.check('clear sets isReinforced false', !kpA.isReinforced);
    // Test 22: negative reinforcement in constructor clamped
    const kpNeg = new KnowledgePoint('neg', 'N', [], 'h', 'c', false, false, 0, 0, -5, null);
    t.check('negative reinforcementCount clamped', kpNeg.reinforcementCount === 0);
    // Test 23: countdownDays edge cases
    t.check('countdownDays null returns null', countdownDays(null) === null);
    const future = Date.now() + 7 * 86400000;
    const fDays = countdownDays(future);
    t.check('countdownDays future positive', fDays !== null && (fDays as number) >= 0);
    const past = Date.now() - 7 * 86400000;
    t.check('countdownDays past returns 0', countdownDays(past) === 0);
    // Test 24: generateId unique
    const idA = generateId();
    const idB = generateId();
    t.check('generateId unique', idA !== idB);
    // Test 25: KnowledgePreset knowledgePointCount
    const tp = new KnowledgePreset('tp', 'TP', 's', 'd', 'cat', md, false);
    t.check('preset kpCount > 0', tp.knowledgePointCount > 0);
    t.check('preset kpCount matches parse', tp.knowledgePointCount === points.length);
    // Test 26: ordinalSuffix coverage
    t.check('ordinal 1st', ordinalSuffix(1) === 'st');
    t.check('ordinal 2nd', ordinalSuffix(2) === 'nd');
    t.check('ordinal 3rd', ordinalSuffix(3) === 'rd');
    t.check('ordinal 11th', ordinalSuffix(11) === 'th');
    t.check('ordinal 21st', ordinalSuffix(21) === 'st');
    // Test 27: empty/edge parseMarkdown
    t.check('parseMarkdown empty', parseMarkdown('').length === 0);
    t.check('parseMarkdown separators only', parseMarkdown('---\n---').length === 0);
    return t.result();
}
export function runSmokeTestsAndLog(): boolean {
    const result = runSmokeTests();
    console.info(`[Kikaria Smoke] ${result.passed} passed, ${result.failed} failed`);
    for (const f of result.failures) {
        console.error(`[Kikaria Smoke] ${f}`);
    }
    return result.failed === 0;
}
