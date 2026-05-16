# Mobile-Friendly Output Guidelines

This project is operated via remote control from a mobile device. All output, responses, and formatting should be optimized for small screens.

## Formatting Rules

### Keep it short
- Prefer 1-3 sentence answers over paragraphs
- No preamble, no trailing summaries
- One idea per line where possible

### Line width
- Wrap text at ~40 characters when practical
- Avoid long unbroken strings (paths, URLs) on their own line
- Break tables into bullet lists — tables don't render well on narrow screens

### Code blocks
- Keep code snippets short (under 20 lines when shown inline)
- Avoid horizontal scrolling — break long lines
- For long files, summarize instead of pasting

### Lists over prose
- Use bullets for any enumeration
- Indent sparingly (max 2 levels)
- Numbered lists only when order matters

### Headers
- Use `##` and `###` only — skip `#` and `####+`
- Short header text (under 30 chars)

## Interaction Style

### Confirmations
- Ask short yes/no questions
- Offer numbered choices (1, 2, 3) for quick taps
- Avoid open-ended "what would you like?" prompts

### Progress updates
- One short sentence per step
- No verbose narration of tool calls
- State results, not process

### Errors
- Lead with the problem in one line
- Follow with one-line fix suggestion
- Skip stack traces unless asked

## What to Avoid

- Wide ASCII tables or diagrams
- Multi-column layouts
- Long file paths shown inline — use file_path:line format compactly
- Emoji decoration (only if explicitly requested)
- Trailing "let me know if..." closers
