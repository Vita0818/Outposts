/**
 * SmokeTest.test.ets - hypium test suite for Kikaria data model and navigation.
 * Imports from main module and wraps SmokeTest assertions in describe/it/expect.
 */
import { describe, it, expect } from '@ohos/hypium'
import { runSmokeTests } from '../../main/ets/model/SmokeTest'
import { parseMarkdown, markdownTextFromPoints, KnowledgePoint, countdownDays, generateId } from '../../main/ets/model/KnowledgePoint'
import { navPathStack, RouteParams, pushPage } from '../../main/ets/data/NavigationService'
import { KnowledgePreset } from '../../main/ets/model/KnowledgePoint'
import { isTabletWidth, isPadLandscape, isPadPortrait, isTwoColumnCapable, pageHorizontalPadding, ipadPortraitListPageTopInset } from '../../main/ets/components/KikariaAdaptiveLayout'

export default function testSuite(): void {
  describe('KikariaParseMarkdown', () => {
    it('parseMarkdown_returns_correct_count', 0, () => {
      const md = `# Test\n\ntags: a\n\nhint:\nh\n\ncontent:\nc`
      expect(parseMarkdown(md).length).assertEqual(1)
    })

    it('parseMarkdown_empty_input_returns_empty', 0, () => {
      expect(parseMarkdown('').length).assertEqual(0)
    })

    it('parseMarkdown_malformed_input_returns_empty', 0, () => {
      expect(parseMarkdown('random text').length).assertEqual(0)
    })

    it('parseMarkdown_multiline_hint_and_content', 0, () => {
      const md = `# ML\n\ntags: t\n\nhint:\nLine1\nLine2\n\ncontent:\nC1\nC2\n`
      const pts = parseMarkdown(md)
      expect(pts.length).assertEqual(1)
      if (pts.length >= 1) {
        expect(pts[0].hint.includes('Line1')).assertEqual(true)
        expect(pts[0].content.includes('C2')).assertEqual(true)
      }
    })

    it('parseMarkdown_empty_tags', 0, () => {
      const md = `# NoTags\n\ntags:\n\nhint:\nH\n\ncontent:\nC\n`
      const pts = parseMarkdown(md)
      expect(pts.length).assertEqual(1)
      if (pts.length >= 1) {
        expect(pts[0].tags.length).assertEqual(0)
      }
    })

    it('parseMarkdown_multiple_points_with_separator', 0, () => {
      const md = `# P1\n\ntags: t1\n\nhint:\nh1\n\ncontent:\nc1\n\n---\n\n# P2\n\ntags: t2\n\nhint:\nh2\n\ncontent:\nc2`
      const pts = parseMarkdown(md)
      expect(pts.length).assertEqual(2)
    })

    it('parseMarkdown_single_point_no_separator', 0, () => {
      const md = `# Solo\n\ntags: s\n\nhint:\nsh\n\ncontent:\nsc\n`
      const pts = parseMarkdown(md)
      expect(pts.length).assertEqual(1)
      if (pts.length >= 1) {
        expect(pts[0].title).assertEqual('Solo')
      }
    })

    it('parseMarkdown_chinese_tags', 0, () => {
      const md = `# 中文\n\ntags: 数学, 物理\n\nhint:\n提示\n\ncontent:\n答案\n`
      const pts = parseMarkdown(md)
      expect(pts.length).assertEqual(1)
      if (pts.length >= 1) {
        expect(pts[0].tags.includes('数学')).assertEqual(true)
        expect(pts[0].tags.includes('物理')).assertEqual(true)
      }
    })

    it('parseMarkdown_tight_comma_tags', 0, () => {
      const md = `# Tight\n\ntags: a,b,c\n\nhint:\nh\n\ncontent:\nc`
      const pts = parseMarkdown(md)
      expect(pts.length).assertEqual(1)
      if (pts.length >= 1) {
        expect(pts[0].tags.length).assertEqual(3)
      }
    })
  })

  describe('KikariaRoundtrip', () => {
    it('markdownTextFromPoints_roundtrip', 0, () => {
      const kp = new KnowledgePoint(generateId(), 'RT', ['t1'], 'hint', 'content')
      const md = markdownTextFromPoints([kp])
      const reParsed = parseMarkdown(md)
      expect(reParsed.length).assertEqual(1)
    })

    it('unicode_roundtrip', 0, () => {
      const kp = new KnowledgePoint(generateId(), '中文测试', ['数学'], '中文提示', '中文答案')
      const md = markdownTextFromPoints([kp])
      const reParsed = parseMarkdown(md)
      expect(reParsed.length).assertEqual(1)
      if (reParsed.length >= 1) {
        expect(reParsed[0].title).assertEqual('中文测试')
      }
    })

    it('multi_point_roundtrip', 0, () => {
      const kp1 = new KnowledgePoint(generateId(), 'P1', ['t1'], 'h1', 'c1')
      const kp2 = new KnowledgePoint(generateId(), 'P2', ['t2'], 'h2', 'c2')
      const md = markdownTextFromPoints([kp1, kp2])
      const reParsed = parseMarkdown(md)
      expect(reParsed.length).assertEqual(2)
    })
  })

  describe('KikariaKnowledgePoint', () => {
    it('reinforcement_workflow', 0, () => {
      const kp = new KnowledgePoint(generateId(), 'KP', ['t'], 'h', 'c')
      expect(kp.reinforcementCount).assertEqual(0)
      kp.addReinforcement()
      expect(kp.reinforcementCount).assertEqual(1)
      kp.clearReinforcement()
      expect(kp.reinforcementCount).assertEqual(0)
    })

    it('mastered_toggle', 0, () => {
      const kp = new KnowledgePoint(generateId(), 'KP', ['t'], 'h', 'c')
      expect(kp.isMastered).assertEqual(false)
      kp.isMastered = true
      expect(kp.isMastered).assertEqual(true)
    })

    it('generateId_produces_unique_values', 0, () => {
      expect(generateId() !== generateId()).assertEqual(true)
    })

    it('multiple_reinforcement_then_clear', 0, () => {
      const kp = new KnowledgePoint(generateId(), 'KP', ['t'], 'h', 'c')
      kp.addReinforcement()
      kp.addReinforcement()
      kp.addReinforcement()
      expect(kp.reinforcementCount).assertEqual(3)
      expect(kp.isReinforced).assertEqual(true)
      kp.clearReinforcement()
      expect(kp.reinforcementCount).assertEqual(0)
      expect(kp.isReinforced).assertEqual(false)
    })

    it('isReinforced_follows_reinforcementCount', 0, () => {
      const kp = new KnowledgePoint(generateId(), 'KP', ['t'], 'h', 'c', false, false, Date.now(), Date.now(), 5)
      expect(kp.isReinforced).assertEqual(true)
    })
  })

  describe('KikariaCountdown', () => {
    it('countdownDays_future_positive', 0, () => {
      const future = Date.now() + 7 * 24 * 60 * 60 * 1000
      const days = countdownDays(future)
      expect(days !== null).assertEqual(true)
    })

    it('countdownDays_null_returns_null', 0, () => {
      expect(countdownDays(null)).assertEqual(null)
    })

    it('countdownDays_past_returns_zero', 0, () => {
      const past = Date.now() - 1 * 24 * 60 * 60 * 1000
      expect(countdownDays(past)).assertEqual(0)
    })

    it('countdownDays_today_returns_zero', 0, () => {
      const today = new Date()
      today.setHours(23, 59, 59, 999)
      expect(countdownDays(today.getTime())).assertEqual(0)
    })
  })

  describe('KikariaSmokeTestRunner', () => {
    it('runSmokeTests_all_pass', 0, () => {
      const result = runSmokeTests()
      expect(result.failed).assertEqual(0)
    })
  })

  describe('KikariaNavigationService', () => {
    it('RouteParams_constructor_presetId', 0, () => {
      const rp = new RouteParams('preset-123')
      expect(rp.presetId).assertEqual('preset-123')
      expect(rp.pointId === undefined).assertEqual(true)
    })

    it('RouteParams_constructor_pointId', 0, () => {
      const rp = new RouteParams(undefined, 'point-456')
      expect(rp.pointId).assertEqual('point-456')
      expect(rp.presetId === undefined).assertEqual(true)
    })

    it('RouteParams_constructor_both', 0, () => {
      const rp = new RouteParams('pid', 'ptid')
      expect(rp.presetId).assertEqual('pid')
      expect(rp.pointId).assertEqual('ptid')
    })

    it('navPathStack_not_null', 0, () => {
      expect(navPathStack !== null).assertEqual(true)
    })

    it('navPathStack_size_zero_initially', 0, () => {
      expect(navPathStack.size()).assertEqual(0)
    })
  })

  describe('KikariaKnowledgePointEdgeCases', () => {
    it('tags_empty_array', 0, () => {
      const kp = new KnowledgePoint(generateId(), 'T', [], 'h', 'c')
      expect(kp.tags.length).assertEqual(0)
    })

    it('long_content_preserved', 0, () => {
      const longText = 'A'.repeat(5000)
      const kp = new KnowledgePoint(generateId(), 'Long', ['t'], 'h', longText)
      expect(kp.content.length).assertEqual(5000)
    })

    it('createdAt_is_set', 0, () => {
      const before = Date.now()
      const kp = new KnowledgePoint(generateId(), 'T', ['t'], 'h', 'c')
      expect(kp.createdAt >= before).assertEqual(true)
    })

    it('updatedAt_tracks_changes', 0, () => {
      const kp = new KnowledgePoint(generateId(), 'T', ['t'], 'h', 'c')
      const before = Date.now()
      kp.isMastered = true
      expect(kp.updatedAt >= before).assertEqual(true)
    })
  })

  describe('KikariaKnowledgePreset', () => {
    it('knowledgePointCount_matches_parse', 0, () => {
      const md = `# P1\n\ntags: t1\n\nhint:\nh1\n\ncontent:\nc1\n\n---\n\n# P2\n\ntags: t2\n\nhint:\nh2\n\ncontent:\nc2`
      const p = new KnowledgePreset('id', 'P', 'S', 'D', 'C', md, false)
      expect(p.knowledgePointCount).assertEqual(2)
    })

    it('isBuiltIn_flag_preserved', 0, () => {
      const p = new KnowledgePreset('id', 'P', 'S', 'D', 'C', '', true)
      expect(p.isBuiltIn).assertEqual(true)
    })
  })

  describe('KikariaAdaptiveLayout', () => {
    it('isTabletWidth_returns_boolean', 0, () => {
      const val = isTabletWidth()
      // Environment-dependent; just verify type is boolean
      expect(typeof val === 'boolean').assertEqual(true)
    })

    it('isPadLandscape_returns_boolean', 0, () => {
      expect(typeof isPadLandscape() === 'boolean').assertEqual(true)
    })

    it('isPadPortrait_returns_boolean', 0, () => {
      expect(typeof isPadPortrait() === 'boolean').assertEqual(true)
    })

    it('isTwoColumnCapable_returns_boolean', 0, () => {
      expect(typeof isTwoColumnCapable() === 'boolean').assertEqual(true)
    })

    it('pageHorizontalPadding_returns_positive', 0, () => {
      expect(pageHorizontalPadding() >= 20).assertEqual(true)
    })

    it('ipadPortraitListPageTopInset_returns_non_negative', 0, () => {
      expect(ipadPortraitListPageTopInset() >= 0).assertEqual(true)
    })
  })

  describe('KikariaMarkdownEdgeCases', () => {
    it('parseMarkdown_no_title_returns_empty', 0, () => {
      const md = `tags: a\n\nhint:\nH\n\ncontent:\nC`
      expect(parseMarkdown(md).length).assertEqual(0)
    })

    it('parseMarkdown_with_code_blocks_in_content', 0, () => {
      const md = `# Code\n\ntags: dev\n\nhint:\nHint\n\ncontent:\n\`\`\`\ncode block\nline2\n\`\`\`\nAfter code`
      const pts = parseMarkdown(md)
      expect(pts.length).assertEqual(1)
      if (pts.length >= 1) {
        expect(pts[0].content.includes('```')).assertEqual(true)
        expect(pts[0].content.includes('code block')).assertEqual(true)
      }
    })

    it('parseMarkdown_three_points', 0, () => {
      const md = `# P1\n\ntags: a\n\nhint:\nh1\n\ncontent:\nc1\n\n---\n\n# P2\n\ntags: b\n\nhint:\nh2\n\ncontent:\nc2\n\n---\n\n# P3\n\ntags: c\n\nhint:\nh3\n\ncontent:\nc3`
      const pts = parseMarkdown(md)
      expect(pts.length).assertEqual(3)
    })

    it('markdownTextFromPoints_single_preserves_fields', 0, () => {
      const kp = new KnowledgePoint(generateId(), 'Title X', ['tagA', 'tagB'], 'Hint here', 'Content here')
      const md = markdownTextFromPoints([kp])
      expect(md.includes('Title X')).assertEqual(true)
      expect(md.includes('tagA, tagB')).assertEqual(true)
      expect(md.includes('Hint here')).assertEqual(true)
      expect(md.includes('Content here')).assertEqual(true)
    })
  })
}
