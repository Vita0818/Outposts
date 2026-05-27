/**
 * NavigationService - Shared NavPathStack to replace deprecated router.pushUrl/back.
 *
 * Usage:
 *   import { navPathStack, pushPage } from '../data/NavigationService'
 *   pushPage('ReviewPage', undefined)
 *   navPathStack.pop()
 *
 * Pages must be registered as @Builder NavDestination entries in the root Navigation.
 */

export const navPathStack: NavPathStack = new NavPathStack()

/** Typed route params to satisfy ArkTS strict mode (arkts-no-untyped-obj-literals). */
export class RouteParams {
  presetId?: string
  pointId?: string

  constructor(presetId?: string, pointId?: string) {
    this.presetId = presetId
    this.pointId = pointId
  }
}

/** Push a page by name. */
export function pushPage(name: string, param?: Object): void {
  if (param) {
    _routeParams = param as Record<string, Object>
  }
  navPathStack.pushPathByName(name, param, false)
}

/** Pop the top page from the stack. */
export function popPage(): void {
  navPathStack.pop()
}

/** Replace the entire stack with a single page name. */
export function replacePage(name: string, param?: Object): void {
  navPathStack.clear()
  navPathStack.pushPathByName(name, param, false)
}

// Route params bridge — for pages that need router.getParams() replacement.
let _routeParams: Record<string, Object> | undefined = undefined

export function setRouteParams(params: Object): void {
  _routeParams = params as Record<string, Object>
}

export function getRouteParams(): Record<string, Object> | undefined {
  return _routeParams
}

export function clearRouteParams(): void {
  _routeParams = undefined
}
