# Changelog

All notable changes to this project are documented in this file.
The format is based on Keep a Changelog, and this project adheres to Semantic Versioning.

## [Unreleased]

## [1.1.0] - 2026-10-30
### Added
- The explorer and the outline can each be collapsed. Their titles stay visible.
- Application icon (`app_icon/icon.png`): executable, taskbar and title bar. The multi-size `Assets/app.ico` is generated from the PNG.
- Mermaid diagrams in the preview (flowchart, class diagram and the other Mermaid types), rendered offline with an embedded Mermaid 11.17.2. A block with a syntax error stays as plain text.
- Outline pane ("Plan") under the explorer: heading tree of the open document, updated as you type. A click selects the heading in the editor and scrolls the preview to it.
- Links to a heading of the same document (`[text](#heading)`) now scroll the preview to it.

### Changed
- View menu entry "Explorer" renamed "Explorer and outline" (the entry still toggles the whole left pane).
- The preview WebView has scripting enabled (required by Mermaid). Page scripts stay impossible: the CSP is unchanged (`default-src 'none'`) and raw HTML is still disabled.

### Fixed
- Maximizing the window left the preview at the size of the restored window.
- Choosing a heading in the outline did not move the open view to that heading.
- The left pane could not be resized.

## [1.0.0] - 2026-10-27
### Added
- Initial release.
