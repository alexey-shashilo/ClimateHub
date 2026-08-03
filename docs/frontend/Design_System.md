# Design System

## Visual Principles

- Calm, modern engineering platform
- Information density without overload
- Semantic statuses, not just colors
- Professional, not "smart home" aesthetic
- Consistent light and dark themes

## Typography

- Font: system-ui with Inter preference
- Monospace: JetBrains Mono, Fira Code
- Numeric metrics use tabular figures
- Hierarchy: `h1`–`h6`, body, secondary, caption, metric

## Spacing

4/8/12/16/24/32/48/64 px system.

## Radii

| Token | Value |
|---|---|
| xs | 4px |
| sm | 8px |
| md | 12px |
| lg | 16px |
| xl | 20px |

## Surfaces

| Level | Light | Dark |
|---|---|---|
| 0 — Background | #f8f9fa | #1a1b1e |
| 1 — Card/surface | #ffffff | #25262b |
| 2 — Elevated | #ffffff | #2c2e33 |
| 3 — Modal/popover | #ffffff | #373a40 |

## Semantic Colors

- `statusNormal` — green
- `statusWarning` — amber/yellow
- `statusCritical` — red
- `statusOffline` — gray
- `statusStale` — orange
- `statusInformation` — blue
- `climate` — primary teal accent

## Chart Colors

- Temperature — warm orange
- Humidity — cyan
- CO₂ — purple
- Target band — semi-transparent green
- Warning band — semi-transparent amber

## Accessibility

- WCAG AA target
- Status represented by text + icon + color
- Visible keyboard focus
- Sufficient contrast in both themes
- `prefers-reduced-motion` support

## Components

See `@shared/components/` and `@shared/theme/`: