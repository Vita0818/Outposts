import { LatexTokenType, tokenizeLatex } from "@bundle:com.vita0818.kikaria/entry/ets/model/KikariaLatexParser";
/** Convert a LaTeX math expression to Unicode for display. */
export function renderMathToUnicode(latex: string, isBlock: boolean = false): string {
    let s = latex.trim();
    // Strip block delimiters if present
    if (s.startsWith('$$') && s.endsWith('$$') && s.length >= 4) {
        s = s.substring(2, s.length - 2).trim();
    }
    // Normalize
    s = s.replace(/\\dfrac/g, '\\frac').replace(/\\tfrac/g, '\\frac');
    s = s.replace(/\\operatorname\{sgn\}/g, 'sgn');
    if (isBlock) {
        s = replaceBracedCommand(s, '\\operatorname', (name: string) => name);
    }
    s = replaceBracedPairCommand(s, ['\\dfrac', '\\tfrac', '\\frac'], (num: string, den: string) => `${renderMathToUnicode(num)} / ${renderMathToUnicode(den)}`);
    s = replaceBracedCommand(s, '\\sqrt', (v: string) => `√(${renderMathToUnicode(v)})`);
    s = replaceBracedCommand(s, '\\operatorname', (v: string) => v);
    s = replaceBracedCommand(s, '\\mathrm', (v: string) => v);
    s = replaceBracedCommand(s, '\\mathbf', (v: string) => v);
    s = replaceBracedCommand(s, '\\text', (v: string) => v);
    s = replaceBracedCommand(s, '\\bar', (v: string) => `${renderMathToUnicode(v)}̄`);
    // Superscript: x^{2} → x², subscript: x_{1} → x₁
    s = replaceScriptBraces(s, '^', superscriptDigit);
    s = replaceScriptBraces(s, '_', subscriptDigit);
    // LaTeX symbol → Unicode
    const syms: Record<string, string> = {
        '\\begin{cases}': '', '\\end{cases}': '', '\\begin{aligned}': '', '\\end{aligned}': '',
        '\\begin{matrix}': '', '\\end{matrix}': '',
        '\\\\': '\n', '&': '  ', '\\qquad': '  ', '\\quad': ' ', '\\,': ' ', '\\;': ' ',
        '\\:': ' ', '\\!': '', '\\left': '', '\\right': '',
        '\\Bigg': '', '\\bigg': '', '\\Big': '', '\\big': '',
        // Integrals
        '\\iiint': '∫∫∫', '\\iint': '∫∫', '\\int': '∫', '\\oint': '∮',
        // Sum, product, limit (with subscript rendering handled below)
        '\\sum': 'Σ', '\\prod': 'Π', '\\coprod': '∐',
        // Greek lowercase
        '\\alpha': 'α', '\\beta': 'β', '\\gamma': 'γ', '\\delta': 'δ',
        '\\epsilon': 'ε', '\\varepsilon': 'ε', '\\zeta': 'ζ', '\\eta': 'η',
        '\\theta': 'θ', '\\vartheta': 'ϑ', '\\iota': 'ι', '\\kappa': 'κ',
        '\\lambda': 'λ', '\\mu': 'μ', '\\nu': 'ν', '\\xi': 'ξ',
        '\\pi': 'π', '\\varpi': 'ϖ', '\\rho': 'ρ', '\\varrho': 'ϱ',
        '\\sigma': 'σ', '\\varsigma': 'ς', '\\tau': 'τ', '\\upsilon': 'υ',
        '\\phi': 'φ', '\\varphi': 'ϕ', '\\chi': 'χ', '\\psi': 'ψ', '\\omega': 'ω',
        // Greek uppercase
        '\\Gamma': 'Γ', '\\Delta': 'Δ', '\\Theta': 'Θ', '\\Lambda': 'Λ',
        '\\Xi': 'Ξ', '\\Pi': 'Π', '\\Sigma': 'Σ', '\\Upsilon': 'Υ',
        '\\Phi': 'Φ', '\\Psi': 'Ψ', '\\Omega': 'Ω',
        // Operators
        '\\partial': '∂', '\\nabla': '∇', '\\infty': '∞', '\\emptyset': '∅',
        '\\forall': '∀', '\\exists': '∃', '\\neg': '¬', '\\lnot': '¬',
        // Relations
        '\\neq': '≠', '\\ne': '≠', '\\leq': '≤', '\\le': '≤',
        '\\geq': '≥', '\\ge': '≥', '\\to': '→', '\\rightarrow': '→',
        '\\leftarrow': '←', '\\Rightarrow': '⇒', '\\Leftrightarrow': '⇔',
        '\\approx': '≈', '\\equiv': '≡', '\\sim': '∼', '\\propto': '∝',
        '\\subset': '⊂', '\\subseteq': '⊆', '\\supset': '⊃', '\\supseteq': '⊇',
        '\\in': '∈', '\\notin': '∉', '\\ni': '∋',
        '\\parallel': '∥', '\\perp': '⊥',
        // Binary ops
        '\\times': '×', '\\cdot': '·', '\\div': '÷', '\\pm': '±', '\\mp': '∓',
        '\\oplus': '⊕', '\\ominus': '⊖', '\\otimes': '⊗', '\\oslash': '⊘',
        '\\cup': '∪', '\\cap': '∩', '\\setminus': '\\',
        '\\land': '∧', '\\lor': '∨', '\\wedge': '∧', '\\vee': '∨',
        // Arrows
        '\\mapsto': '↦', '\\longmapsto': '⟼', '\\iff': '⟺',
        // Dots
        '\\cdots': '⋯', '\\ldots': '…', '\\vdots': '⋮', '\\ddots': '⋱',
        // Misc
        '\\angle': '∠', '\\triangle': '△', '\\square': '□', '\\Box': '□',
        '\\Re': 'ℜ', '\\Im': 'ℑ', '\\aleph': 'ℵ',
        '\\hbar': 'ℏ', '\\ell': 'ℓ', '\\wp': '℘',
        // Accents
        '\\hat': '̂', '\\tilde': '̃', '\\bar': '̄', '\\vec': '⃗', '\\dot': '̇',
        '\\widehat': '̂', '\\widetilde': '̃ '
    };
    for (const sym of Object.keys(syms)) {
        s = s.replace(new RegExp(escapeRegex(sym), 'g'), syms[sym]);
    }
    s = s.replace(/\\/g, '');
    return normalizeWhitespace(s);
}
/** Render a rich text string with math tokens converted. */
export function renderMathRichText(text: string): string {
    const tokens = tokenizeLatex(text);
    let result = '';
    for (const token of tokens) {
        switch (token.type) {
            case LatexTokenType.TEXT:
            case LatexTokenType.FALLBACK:
                result += token.value;
                break;
            case LatexTokenType.INLINE_MATH:
                result += renderMathToUnicode(token.body, false);
                break;
            case LatexTokenType.BLOCK_MATH:
                result += '\n' + renderMathToUnicode(token.body, true) + '\n';
                break;
        }
    }
    return result;
}
// --- helpers ---
class BracedGroup {
    value: string = '';
    nextIdx: number = 0;
}
function escapeRegex(s: string): string {
    return s.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
}
function normalizeWhitespace(s: string): string {
    const lines = s.split('\n');
    const normalized: string[] = [];
    for (let line of lines) {
        line = line.trim();
        while (line.includes('  ')) {
            line = line.replace(/  /g, ' ');
        }
        if (line.length === 0) {
            if (normalized.length > 0 && normalized[normalized.length - 1].length > 0) {
                normalized.push(line);
            }
        }
        else {
            normalized.push(line);
        }
    }
    return normalized.join('\n');
}
function replaceBracedCommand(text: string, cmd: string, transform: (v: string) => string): string {
    let result = '';
    let i = 0;
    while (i < text.length) {
        if (text.substring(i).startsWith(cmd)) {
            let cursor = i + cmd.length;
            // skip whitespace
            while (cursor < text.length && (text[cursor] === ' ' || text[cursor] === '\t')) {
                cursor++;
            }
            const group = extractBracedGroup(text, cursor);
            if (group) {
                result += transform(group.value);
                i = group.nextIdx;
                continue;
            }
        }
        result += text[i];
        i++;
    }
    return result;
}
function replaceBracedPairCommand(text: string, cmds: string[], transform: (a: string, b: string) => string): string {
    let result = '';
    let i = 0;
    while (i < text.length) {
        let matchedCmd = '';
        for (const cmd of cmds) {
            if (text.substring(i).startsWith(cmd)) {
                matchedCmd = cmd;
                break;
            }
        }
        if (matchedCmd.length > 0) {
            let cursor = i + matchedCmd.length;
            while (cursor < text.length && (text[cursor] === ' ' || text[cursor] === '\t')) {
                cursor++;
            }
            const g1 = extractBracedGroup(text, cursor);
            if (g1) {
                cursor = g1.nextIdx;
                while (cursor < text.length && (text[cursor] === ' ' || text[cursor] === '\t')) {
                    cursor++;
                }
                const g2 = extractBracedGroup(text, cursor);
                if (g2) {
                    result += transform(g1.value, g2.value);
                    i = g2.nextIdx;
                    continue;
                }
            }
        }
        result += text[i];
        i++;
    }
    return result;
}
/** Convert digit (0-9) or basic chars to Unicode superscript. */
function superscriptDigit(s: string): string {
    const map: Record<string, string> = {
        '0': '⁰', '1': '¹', '2': '²', '3': '³', '4': '⁴',
        '5': '⁵', '6': '⁶', '7': '⁷', '8': '⁸', '9': '⁹',
        '+': '⁺', '-': '⁻', '=': '⁼', '(': '⁽', ')': '⁾',
        'n': 'ⁿ', 'i': 'ⁱ'
    };
    let result = '';
    for (let i = 0; i < s.length; i++) {
        result += map[s[i]] !== undefined ? map[s[i]] : s[i];
    }
    return result;
}
/** Convert digit (0-9) or basic chars to Unicode subscript. */
function subscriptDigit(s: string): string {
    const map: Record<string, string> = {
        '0': '₀', '1': '₁', '2': '₂', '3': '₃', '4': '₄',
        '5': '₅', '6': '₆', '7': '₇', '8': '₈', '9': '₉',
        '+': '₊', '-': '₋', '=': '₌', '(': '₍', ')': '₎',
        'a': 'ₐ', 'e': 'ₑ', 'o': 'ₒ', 'x': 'ₓ',
        'i': 'ᵢ', 'j': 'ⱼ', 'n': 'ₙ'
    };
    let result = '';
    for (let i = 0; i < s.length; i++) {
        result += map[s[i]] !== undefined ? map[s[i]] : s[i];
    }
    return result;
}
/** Replace ^{...} and _{...} patterns with Unicode script digits. */
function replaceScriptBraces(text: string, scriptChar: string, transform: (s: string) => string): string {
    let result = '';
    let i = 0;
    while (i < text.length) {
        if (text[i] === scriptChar && i + 1 < text.length) {
            if (text[i + 1] === '{') {
                const group = extractBracedGroup(text, i + 1);
                if (group) {
                    result += transform(group.value);
                    i = group.nextIdx;
                    continue;
                }
            }
            // Single char script: x^2 or x_n
            if (i + 1 < text.length) {
                result += transform(text[i + 1]);
                i += 2;
                continue;
            }
        }
        result += text[i];
        i++;
    }
    return result;
}
function extractBracedGroup(text: string, start: number): BracedGroup | null {
    if (start >= text.length || text[start] !== '{') {
        return null;
    }
    let cursor = start + 1;
    const begin = cursor;
    let depth = 1;
    while (cursor < text.length) {
        if (text[cursor] === '{') {
            depth++;
        }
        else if (text[cursor] === '}') {
            depth--;
            if (depth === 0) {
                const result = new BracedGroup();
                result.value = text.substring(begin, cursor);
                result.nextIdx = cursor + 1;
                return result;
            }
        }
        cursor++;
    }
    return null;
}
