//
//  LatexToken.swift
//  Kikaria
//
//  Created by Codex on 2026/5/10.
//

import Foundation

enum KikariaLatexToken: Equatable {
    case text(String)
    case inlineMath(source: String, body: String)
    case blockMath(source: String, body: String)
    case fallback(String)
}
