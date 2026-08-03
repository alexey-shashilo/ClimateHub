# Theme Architecture

## Modes

Three appearance modes: `light`, `dark`, `system`.

- `light` — forces light theme
- `dark` — forces dark theme
- `system` — follows `prefers-color-scheme` media query

## Persistence

- Stored in `localStorage` under key `climate-hub:appearance`
- Values: `"light"`, `"dark"`, `"system"` (default)
- Never stores the resolved scheme — only the preference

## Resolution

`resolveScheme(mode) → ResolvedColorScheme`

- `light` → `"light"`
- `dark` → `"dark"`
- `system` → check `prefers-color-scheme`

## No-Flash Mechanism

An inline `<script>` in `index.html`:

1. Reads stored preference (or defaults to `"system"`)
2. Resolves to `"light"` or `"dark"`
3. Sets `data-mantine-color-scheme` and `<meta name="theme-color">`
4. Executes before first React render

## Runtime

`useAppearance()` hook provides:
- `mode` — current preference
- `resolved` — actual applied scheme (`"light"`/`"dark"`)
- `setMode()` — change preference
- `resetMode()` — revert to `system`

Uses `useSyncExternalStore` to avoid unnecessary re-renders.

## Mantine Integration

`ThemeProvider` wraps `MantineProvider` with `forceColorScheme` set from `useAppearance().resolved`.

## System Theme Listener

In `system` mode, a `matchMedia('prefers-color-scheme: dark')` listener updates the theme live.
Manual selection (`light`/`dark`) disables the listener effect.

## Chart Theme

`chartTheme.ts` exports `getChartTheme(scheme)` which returns colors for:
- text, grid, tooltip, axis labels
- series colors (temperature, humidity, co2)
- target and warning band fills

Used by `EnvironmentHistoryPage` to theme ECharts instances.

## Files

```
src/shared/theme/
├── theme.ts           — Mantine theme config
├── themeStorage.ts    — localStorage + resolve + apply
├── useAppearance.ts   — React hook + external store
├── ThemeSwitcher.tsx   — UI component (3-mode menu)
├── ThemeProvider.tsx   — App wrapper
├── chartTheme.ts      — ECharts color themes
└── tokens.ts          — Design token constants
```

## Testing

- `theme.test.ts` covers storage, resolution, edge cases
- No rendering tests needed for utility functions