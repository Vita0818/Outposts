/**
 * KikariaLatexParser - Tokenizes text for inline/block math delimiters.
 * Ported from KikariaLatexParser.swift + LatexToken.swift
 */
export enum LatexTokenType {
    TEXT = "text",
    INLINE_MATH = "inlineMath",
    BLOCK_MATH = "blockMath",
    FALLBACK = "fallback"
}
export class LatexToken {
    type: LatexTokenType = LatexTokenType.TEXT;
    value: string = '';
    body: string = '';
    constructor() { }
    static text(value: string): LatexToken {
        const t = new LatexToken();
        t.type = LatexTokenType.TEXT;
        t.value = value;
        t.body = '';
        return t;
    }
    static inlineMath(source: string, body: string): LatexToken {
        const t = new LatexToken();
        t.type = LatexTokenType.INLINE_MATH;
        t.value = source;
        t.body = body;
        return t;
    }
    static blockMath(source: string, body: string): LatexToken {
        const t = new LatexToken();
        t.type = LatexTokenType.BLOCK_MATH;
        t.value = source;
        t.body = body;
        return t;
    }
    static fallback(value: string): LatexToken {
        const t = new LatexToken();
        t.type = LatexTokenType.FALLBACK;
        t.value = value;
        t.body = '';
        return t;
    }
}
/** Tokenize text into LatexToken array. */
export function tokenizeLatex(text: string): LatexToken[] {
    return new LatexTextScanner(text).scan();
}
class LatexTextScanner {
    private chars: string[];
    private index: number = 0;
    private textBuffer: string = '';
    private tokens: LatexToken[] = [];
    constructor(text: string) {
        // Normalize newlines, convert to char array
        this.chars = text.replace(/\r\n/g, '\n').replace(/\r/g, '\n').split('').map(c => c);
    }
    scan(): LatexToken[] {
        while (this.index < this.chars.length) {
            if (this.startsWith('```', this.index)) {
                this.appendCodeSpan('```');
            }
            else if (this.chars[this.index] === '`') {
                this.appendCodeSpan('`');
            }
            else if (this.isEscapedDollar(this.index)) {
                this.textBuffer += '$';
                this.index += 2;
            }
            else if (this.chars[this.index] === '$') {
                this.scanMathToken();
            }
            else {
                this.textBuffer += this.chars[this.index];
                this.index += 1;
            }
        }
        this.flushText();
        return this.tokens;
    }
    private scanMathToken(): void {
        if (this.startsWith('$$', this.index)) {
            this.scanBlockMath();
        }
        else {
            this.scanInlineMath();
        }
    }
    private scanBlockMath(): void {
        const start = this.index;
        const closeIdx = this.closingDoubleDollar(start + 2);
        if (closeIdx < 0) {
            for (let i = start; i < this.chars.length; i++) {
                this.textBuffer += this.chars[i];
            }
            this.index = this.chars.length;
            return;
        }
        const body = this.chars.slice(start + 2, closeIdx).join('');
        const source = this.chars.slice(start, closeIdx + 2).join('');
        this.flushText();
        this.tokens.push(LatexToken.blockMath(source, body));
        this.index = closeIdx + 2;
    }
    private scanInlineMath(): void {
        const start = this.index;
        const closeIdx = this.closingSingleDollar(start + 1);
        if (closeIdx < 0) {
            for (let i = start; i < this.chars.length; i++) {
                this.textBuffer += this.chars[i];
            }
            this.index = this.chars.length;
            return;
        }
        const body = this.chars.slice(start + 1, closeIdx).join('');
        const source = this.chars.slice(start, closeIdx + 1).join('');
        this.flushText();
        this.tokens.push(LatexToken.inlineMath(source, body));
        this.index = closeIdx + 1;
    }
    private appendCodeSpan(fence: string): void {
        const start = this.index;
        this.index += fence.length;
        while (this.index < this.chars.length) {
            if (this.startsWith(fence, this.index)) {
                this.index += fence.length;
                this.textBuffer += this.chars.slice(start, this.index).join('');
                return;
            }
            this.index += 1;
        }
        this.textBuffer += this.chars.slice(start, this.chars.length).join('');
    }
    private closingDoubleDollar(startIdx: number): number {
        let i = startIdx;
        while (i < this.chars.length - 1) {
            if (this.startsWith('$$', i) && !this.isEscaped(i)) {
                return i;
            }
            i += 1;
        }
        return -1;
    }
    private closingSingleDollar(startIdx: number): number {
        let i = startIdx;
        while (i < this.chars.length) {
            if (this.chars[i] === '\n') {
                return -1;
            }
            if (this.chars[i] === '$' && !this.isEscaped(i) && !this.startsWith('$$', i)) {
                return i;
            }
            i += 1;
        }
        return -1;
    }
    private flushText(): void {
        if (this.textBuffer.length === 0) {
            return;
        }
        this.tokens.push(LatexToken.text(this.textBuffer));
        this.textBuffer = '';
    }
    private isEscapedDollar(idx: number): boolean {
        return idx + 1 < this.chars.length &&
            this.chars[idx] === '\\' &&
            this.chars[idx + 1] === '$';
    }
    private isEscaped(idx: number): boolean {
        let slashCount = 0;
        let i = idx - 1;
        while (i >= 0 && this.chars[i] === '\\') {
            slashCount += 1;
            i -= 1;
        }
        return slashCount % 2 === 1;
    }
    private startsWith(marker: string, startIdx: number): boolean {
        if (startIdx < 0 || startIdx + marker.length > this.chars.length) {
            return false;
        }
        for (let i = 0; i < marker.length; i++) {
            if (this.chars[startIdx + i] !== marker[i]) {
                return false;
            }
        }
        return true;
    }
}
