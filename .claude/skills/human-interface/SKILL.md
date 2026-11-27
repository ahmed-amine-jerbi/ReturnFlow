---
name: human-interface
description: How to build interfaces that look designed for the product by a person, not generated — the tells of "AI-made" UI and what to do instead, a working design system (type scale, spacing, color, radius, motion), real content, every UI state, accessibility and responsive checks, and how to verify the result in a browser. Use before building, restyling or reviewing any web or app UI, landing page, dashboard, form or component library. Pairs with the frontend-design skill for new visual direction.
---

# Human interface

Users and reviewers judge a product by its interface in seconds. Generated UIs share a look: the
same gradient hero, the same three feature cards, the same purple accent, emoji as icons. The goal
is a UI that clearly belongs to *this* product, and works for everyone using it.

## 1. Start from the product, not a template

Before writing UI, decide (and record in `docs/CONTEXT.md` under Style):
- **Who uses it and for what.** A back-office dashboard used 8 hours a day wants density, speed
  and calm. A consumer onboarding flow wants clarity and warmth. A developer tool wants precision.
- **Tone**, in three words (e.g. "precise, quiet, trustworthy").
- **One or two references** from real products in the same space, and what to take from each.
- **The existing system**: if the project already has tokens, components or a UI library (shadcn,
  MUI, Fluent, Tailwind config), extend them. Never introduce a second visual language.

For new visual direction, load the `frontend-design` skill as well.

## 2. A small, deliberate design system

Define tokens once (CSS variables or Tailwind theme) and use only them:
- **Type scale**: one or two families at most. A clear scale (e.g. 12 / 14 / 16 / 20 / 24 / 32),
  line-height ~1.4–1.6 for body, tighter for headings. Weight and size create hierarchy, not color.
- **Spacing**: a 4- or 8-pt scale. Consistent padding inside components; more space between groups
  than within them.
- **Color**: neutrals do most of the work. One primary accent used sparingly for the main action
  and focus. Semantic colors for success / warning / danger / info. Check contrast (WCAG AA: 4.5:1
  for body text, 3:1 for large text and UI boundaries). Design light and dark together if both ship.
- **Radius, borders, shadows**: one radius scale, subtle borders, shadows only to show elevation.
- **Icons**: one icon set (Lucide, Heroicons, Phosphor, Fluent), one stroke weight, sized to the
  text. Never emoji as UI icons.
- **Motion**: 150–250 ms, ease-out for entering, ease-in for leaving. Motion explains a change
  (where something came from or went). No bouncing, no looping decoration. Respect
  `prefers-reduced-motion`.

## 3. Tells of AI-generated UI — avoid them

| Tell | Do instead |
|---|---|
| Purple-to-blue gradient hero, glowing blobs, glassmorphism everywhere | A palette derived from the product's tone; flat surfaces with one intentional accent |
| "Three feature cards with an icon, title and two lines" as the default layout | Layout driven by the content: a table, a timeline, a comparison, a real screenshot |
| Emoji as icons or in headings (🚀 ✨ 💡) | One consistent icon set, or no icon |
| Marketing filler: "Seamless", "Unlock the power of", "Revolutionize", "All-in-one" | Specific, plain copy that says what the thing does for whom |
| Lorem ipsum, "John Doe", round fake numbers ($1,000, 100 users) | Realistic data: real-length names, messy numbers, long strings |
| Everything centered, everything the same size | Left-aligned reading flow, clear hierarchy, one focal point per screen |
| Every card with a shadow, a border and a gradient | Pick one way to separate content (space, border or elevation) |
| Random spacing (13px here, 22px there) | The spacing scale only |
| Oversized rounded corners on everything | One radius scale suited to the tone |
| Decorative animation on load, hover scale on every element | Motion only where it explains a change |
| Generic stock illustrations | Product screenshots, real data visualizations, or nothing |
| Inconsistent button styles across pages | One button component with a few variants |
| Dark mode as an inverted afterthought | Designed tokens for both themes |

## 4. Every state, every time

Each screen and component handles:
- **Loading**: skeletons that match the final layout for content; a spinner only for short actions.
  No layout jump when data arrives.
- **Empty**: what this is, why it's empty, and the next action ("No invoices yet. Create one").
- **Error**: what happened in human words, what the user can do, and a retry. Never a raw stack or
  `[object Object]`.
- **Partial and edge content**: very long names, missing images, 0 / 1 / many items, huge numbers,
  right-to-left text if i18n matters.
- **Disabled and pending actions**: a submit button shows progress and can't be double-submitted.
- **Success feedback** that's proportional: inline confirmation or a toast, not a modal.

## 5. Forms

- Labels always visible (placeholders aren't labels). Helper text for format rules.
- Validate on blur and submit, not on every keystroke. Errors next to the field, in words that say
  how to fix it.
- Correct input types and `autocomplete` attributes. Sensible tab order. Enter submits.
- Keep the user's input on error.

## 6. Accessibility (not optional)

- Semantic HTML first: `button` for actions, `a` for navigation, headings in order, `label` tied to
  inputs, landmarks (`header`, `nav`, `main`).
- Everything works with the keyboard; focus is always visible; dialogs trap and return focus.
- Images have meaningful `alt` (or empty `alt` when decorative). Icon-only buttons have an
  accessible name.
- Don't rely on color alone for meaning. Contrast meets AA.
- Hit targets at least 24×24 px (44×44 on touch).

## 7. Responsive

- Design mobile and desktop together; check at roughly 360, 768, 1280 and 1920 px wide.
- No horizontal scroll on the page. Tables become scrollable containers or stacked rows.
- Touch-friendly spacing on small screens; denser layouts allowed on large ones.

## 8. Copy

- Write like a person who knows the product: short, specific, active voice.
- Buttons say the action ("Send invoice"), not "Submit" or "OK".
- Consistent terms (the glossary in CONTEXT.md applies to UI copy too).
- Sentence case for headings and buttons unless the brand says otherwise.

## 9. Verify in the browser, not in your head

After any UI change, load it with the `browser-automation` skill (or Playwright) and actually look:
- console errors and failed network requests;
- screenshots at mobile and desktop widths, in light and dark if both ship;
- the loading, empty and error states (force them with mocked responses if needed:
  `postman:api-mocking` can stand in for the API);
- keyboard-only pass through the main flow.

Report what you checked and what you couldn't. Don't ask the user to look for you.

## 10. Checklist

- [ ] Uses only the project's tokens and components; no second visual language.
- [ ] Clear hierarchy and one focal point per screen; no item from the tells table.
- [ ] Real or realistic content.
- [ ] Loading, empty, error, edge and pending states handled.
- [ ] Keyboard, focus, labels, alt text, contrast checked.
- [ ] Checked at mobile and desktop widths in a browser, with screenshots looked at.
