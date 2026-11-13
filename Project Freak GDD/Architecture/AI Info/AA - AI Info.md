Home for notes and rules about working with Claude on Project Freak.

**Rules**
[[Code Style Rules]]

**Tracking**
[[Known Issues]] - bugs and cleanup Claude has spotted but not fixed
[[Project Status]] - what's in progress and open work by chapter. Boards: [[Art Board]], [[Design Board]], [[Engineering Board]], [[Bugs Board]] (see Task board rules below)

**Learning**
[[Coding Concepts]] - C# and Unity features used in the code, explained, with where they show up

**Plans & To Do**
Plans and to-do lists for upcoming work live here. Once a piece is built, it moves into its system's note in Architecture.
[[AI Movement & Dungeon Loading Plan]] - AI movement, brain and dungeon loading (built)
[[Shade Forms Plan]] - tethered / released shade prefabs, tether, release and return abilities (code built, Unity setup to do)
[[Rune Field Overhaul Plan]] - rune field rebuild, core fragments and shade death rules (steps 1–8 built, step 8 is the 3D test)
[[Rune Field 3D Plan]] - 3D rune field test, overhaul step 8 (all 7 steps built and playtested Oct 11, 2026; Playtest 0 changes planned Oct 11, 2026)
[[Zone Visuals Plan]] - rune field zone looks: raised floors, the border tear and smoke, ring wall and crack tools (planned Oct 12, 2026)
[[Rune Field Clean-Up Plan]] - inspector clean-up after the 3D field won, and removing the old 2D field (planned and built Oct 28, 2026)
[[Rune Field Finish Plan]] - what's left before the 3D field is finished: placement preview, info popup, node states and links, shards, opening the field from access points (planned Oct 29, 2026, cards on the boards)
[[Save Data Plan]] - what a save file needs to hold (planned Oct 29, 2026, not started)
[[Fiend Combat Plan]] - Chapter 1 fiends from Popcorn to Elite: enemy attacks, one ability system for every unit, status effects, telegraphs, movers (planned Nov 9, 2026, cards on the boards)

---
## Task board rules
The full how-to (making cards, linking a feature, adding chapters and boards) is [[How To - Task Boards]]. `zzz Extras/Project Board.base` feeds the tables on [[Project Status]].

- **A card is a note** in one of the `Tasks` subfolders: `Art`, `Design`, `Engineering` or `Bugs`. Each subfolder has its board at the top of `Tasks` (Art Board, etc.). Moving a card to another board means moving the note to that folder
- New cards come from the **Task** template. The boards group by `status`, and dragging a card rewrites it
- **Statuses (spelled exactly):** To Do, In Progress, Done, Archived. A typo makes a new column. Archived cards are hidden from the kanban views and show on `Tasks/Archive` (all boards, for recaps and progress videos) and each board's **Archive** tab
- **priority** is 1 to 10, **1 is most important**. Cards sort by it inside every column, so 1s sit at the top. New cards start at 5
- **chapter** is a list, so a card can sit in two chapters (Rune Field Art is Chapter 5 Art and Chapter 3 Crafting). Names are written `Chapter <number> <name>` so they sort in order. The chapter list lives in [[Project Status]]: that's the one place to check names and add new ones
- **owner:** Steve, Claude, Both. **plan** links the plan note, **source** links where the task came from (Known Issues, a playtest log, etc.)
- **Linking a feature:** every card for one feature (art, design and engineering) sets `plan` to the feature's GDD or plan note. That note gets the **Feature Cards** template, a live table of all its cards grouped by board
- **needs** is a list of links to cards this one waits on (`needs: ["[[Radiant Gun models]]"]`). It shows on the board cards, and each card's **Waiting on this** table lists the cards that need it. It's a pointer, not a lock: nothing stops a card moving early
- **Known Issues stays a list.** A bug gets a card in `Bugs` only once it's scheduled. The line in Known Issues then links the card
- **Playtest logs stay logs.** Each item that gets acted on becomes a card, and the log line links it
- **Notes for the future is unchanged:** it marks what the code doesn't do yet. The boards track the work
- **Claude moves cards too:** when Claude finishes a task, it sets `status` (and `done` with the date when it's Done)
- **Completed Info** is the card's post mortem, filled in when it's Done: what was actually built, what changed from the plan, and anything the engineering side needs to know
- **Weekly review (Sundays):** go through every Done card (the **Weekly review** table on [[Project Status]]). For each one, read its Completed Info and:
  - **Show the card when asking about it:** its name, what it was, and the relevant lines from its What / Completed Info, so Steve has the context without opening it. The same goes for any note a small fix touches
  - **A card that says it still needs a Unity test** has usually been tested already: ask in one line, and archive once Steve confirms
  - **Default to making cards, not doing the work.** Follow-up work becomes a new card (or a line on an existing card that's already about it), with `source` linking the reviewed card. Nothing in the code or the architecture notes changes during the review
  - **Check for conflicts** before making a card: look at the open cards, plan notes and other Done cards in the same review. If a Completed Info is undone or changed by later work, say so and ask instead of making a card that fights it
  - **Small fixes** (a one-line doc correction, a renamed value, a note to tick off) don't get cards. Collect them and list them at the end of the review for Steve to OK, then make them. Claude decides what counts as small
  - Bugs go to [[Known Issues]] as usual
  - Then add a **Review** line under Completed Info saying where each item went (with links), and set `status: Archived` and `archived` to the date. Anything unclear gets asked before archiving
  - **Empty `done`:** fill in the review date and mention it, so Steve can set the real date (the Archive groups by `done`'s month)
  - **Empty Completed Info:** point it out and ask if there's anything to note before archiving. Small cards may not need one
  - **Look ahead:** go over the **Waiting on something** table on [[Project Status]] (cards whose `needs` aren't cleared), then pick the cards for the week together
  - **Don't comment on how long In Progress cards have taken.** The look ahead is about blockers and picks, not judging pace
  - **Recap:** finish with a recap note in `Tasks/Recaps` from the **Recap** template, named `===== Recap M-D-YY =====` (e.g. `===== Recap 10-31-26 =====`). It lists the cards finished (linked, one line each), highlights worth remembering for progress videos, the week's picks and what's blocked. Set its `chapter` to the chapters the week touched. It's archived from the start, so it shows on [[Archive]] at the top of its week
- The open Trello cards were brought over Oct 28, 2026. Each card links its Trello original

---
## How this connects to Claude
The code style rules also exist as a Claude skill saved to my account (**project-freak-code-style**). Claude loads the skill automatically when writing or reviewing Project Freak code.

This folder is a reference copy. Editing these notes does **not** change the skill. To change a rule, tell Claude what to change and save the updated skill card it sends, then update the note here to match.
