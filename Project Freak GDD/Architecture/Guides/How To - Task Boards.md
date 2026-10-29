## Overview
How to use the kanban task boards: making cards, moving them, priorities, chapters, linking cards for a feature, and adding a new chapter or board. The boards are Obsidian's built-in **Bases** Kanban view (Obsidian 1.14 or newer), so there are no plugins to keep up to date.

The short version of the rules (for Claude too) is **Task board rules** in [[AA - AI Info]]. The big-picture page is [[Project Status]].

Back to [[Architecture Atlas]]

**Contents**
- [[#Before You Start]]
- [[#How It Works]]
- [[#Making a Card]]
- [[#Working a Card]]
- [[#Priority and Sorting]]
- [[#Linking Cards for a Feature]]
- [[#Adding a Chapter]]
- [[#Adding a Board]]
- [[#Where Cards Come From]]
- [[#Troubleshooting]]

---
## Before You Start

### Words you'll see
| Word | What it means here |
| :--- | :--- |
| Card | One note in a `Tasks` subfolder. The note's properties (the box at the top) decide where it shows up |
| Board | A `.base` file at the top of `Tasks` (Art Board, Design Board, Engineering Board, Bugs Board). Each one shows the cards in its matching folder |
| View | A tab inside a board, picked from the menu at the board's top left: **By Priority**, **By Chapter**, **Chapter Table** |
| Column | One value of `status`: To Do, In Progress, Done |
| Chapter | Which part of the game a card belongs to. The list lives on [[Project Status]] |

### Where things live
| What | Where |
| :--- | :--- |
| The four boards | `Tasks/Art Board`, `Design Board`, `Engineering Board`, `Bugs Board` |
| The cards | `Tasks/Art`, `Tasks/Design`, `Tasks/Engineering`, `Tasks/Bugs` |
| Big-picture page | [[Project Status]] (chapter list, what's in progress, open work by chapter) |
| The data behind Project Status | `zzz Extras/Project Board.base`. Never needs opening, it only feeds the two tables on Project Status |
| Templates | `zzz Extras/Templates/Task` (a new card) and `Feature Cards` (the live table for a feature note) |

### Card properties
| Property | Values | What it does |
| :--- | :--- | :--- |
| `status` | To Do, In Progress, Done | Which column the card is in. Dragging the card changes it |
| `priority` | 1 to 10, **1 is most important** | Sort order inside a column. New cards start at 5 |
| `chapter` | One or more chapter names, e.g. `Chapter 1 Combat` | A list, so a card can be in two chapters |
| `owner` | Steve, Claude, Both | Who's doing it |
| `plan` | A link, e.g. `[[Weapons]]` | The feature or plan note the card belongs to |
| `source` | A link, e.g. `[[Known Issues]]` | Where the task came from |
| `needs` | Links to other cards | Cards this one waits on |
| `created` / `done` | Dates | Filled in when made / when finished |

---
## How It Works
```text
card note (Tasks/Art/Hazen Art.md)
   properties: status, priority, chapter ...
        │
        ├──► Art Board (shows everything in Tasks/Art)
        │       columns = status, order = priority (or chapter, then priority)
        │
        ├──► zzz Extras/Project Board ──► Project Status (in progress + open by chapter, every board)
        │
        └──► Feature Cards table on its plan note (any card that links the note)
```
A card is only a note. Every board, table and column is worked out from the card's folder and properties, so editing a property (by hand, by dragging, or by Claude) moves the card everywhere at once.

---
## Making a Card
1. Open the board it belongs on (Art Board, etc.)
2. Press **+** on the column it should start in (or **New** at the bottom of the column). The note is made in the right folder with that status
3. Name the note. The name is the card's title
4. If the note came up empty, run **Templates: Insert template** → **Task** to add the properties
5. Fill in `chapter` (type a few letters and pick from the list), `priority`, and `owner`. `plan`, `source` and `needs` only if they apply

*Or make it from the Tasks subfolder: right-click the folder → New note, then insert the Task template.*

---
## Working a Card
- **Start / finish it:** drag it to In Progress, then Done. Set `done` to the date when it's finished
- **Move it to another board:** drag the note into that board's folder in the file explorer (e.g. a design card that turned into engineering work goes into `Tasks/Engineering`)
- **Rename it:** rename the note. Links to it update on their own
- **Delete it:** delete the note. Usually it's better to leave it in Done so the history stays

---
## Priority and Sorting
- Change a card's `priority` to move it in its column. 1s sit at the top, 10s at the bottom
- Switch views from the menu at the top left of a board:
  - **By Priority**: each column sorted by priority
  - **By Chapter**: each column sorted by chapter, then by priority inside each chapter
  - **Chapter Table**: a table of the board's cards, grouped by chapter
- Columns can be narrowed in the view's settings (**Column width**) if they don't fit on screen

---
## Linking Cards for a Feature
For a feature that needs work on more than one board (say a weapon system with design, art and engineering cards):
1. Pick the feature's note: its GDD note (e.g. [[Weapons]]) or a plan note
2. In that note, run **Templates: Insert template** → **Feature Cards**. It adds a live table of every card that links the note, grouped by board
3. On each card for the feature, set `plan` to that note. The cards show up in the table right away
4. Where a card can't start until another is done, add that card to `needs` (type `[[` in the property and pick it). For example, "Weapon prefabs" in Engineering might need "Radiant Gun models" from Art

What shows where:
- The **board card** shows its `needs`
- Each card has a **Waiting on this** table at the bottom, listing the cards that need it
- The feature note's **Cards** table shows every card for the feature and its status

`needs` is a pointer, not a lock: nothing stops a card being moved before the cards it needs are done.

---
## Adding a Chapter
1. On [[Project Status]], add a row to the **Chapters** table, named `Chapter <number> <name>` (e.g. `Chapter 6 Dungeons`)
2. Use that exact name in a card's `chapter`

That's it: the By Chapter view, the Chapter Table and the Project Status tables pick it up on their own. The number keeps chapters in order. Once there's a Chapter 10 it would sort between 1 and 2, so switch every chapter to two digits then (Chapter 00, Chapter 01...). Claude can rename them all.

---
## Adding a Board
For a new kind of work (e.g. Audio):
1. Make a folder for its cards: `Tasks/Audio`
2. Copy an existing board (e.g. `Art Board.base`), rename the copy `Audio Board`
3. Open the copy, then open the filter (the **Filter** button at the top) and change the folder from `Tasks/Art` to `Tasks/Audio`
4. Add `[[Audio Board]]` to the **Boards** line on [[Project Status]], and the folder to **Task board rules** in [[AA - AI Info]]

Project Status covers every folder under `Tasks`, so its tables include the new board without changes.

---
## Where Cards Come From
- **New work:** make the card straight on its board
- **Bugs Claude spots** go in [[Known Issues]] first. A bug gets a card on the Bugs Board once it's going to be fixed, and the Known Issues line links the card
- **Playtest feedback** stays in its playtest log (e.g. [[Rune Field 3D Change Requests and to Do]]). Each item that gets worked on becomes a card, and the log line links it
- **Claude** makes and moves cards the same way, following **Task board rules** in [[AA - AI Info]]

---
## Troubleshooting
| Problem | Fix |
| :--- | :--- |
| A new column appeared (e.g. "To do" or "Doing") | A card's `status` is misspelled. Fix the spelling and the column goes away |
| A card isn't on any board | The note isn't inside one of the `Tasks` subfolders. Move it into the right one |
| A card is on the board but in the **None** column | Its `status` is empty. Set it or drag the card into a column |
| A card is missing from By Chapter / Chapter Table groups | Its `chapter` is empty or misspelled. Check it against the list on [[Project Status]] |
| A chapter shows up twice | Two spellings are in use. Fix the cards so they match the list exactly |
| The boards look sorted wrong after an edit | Open the view's **Sort** menu and check it's Priority ascending (By Priority) or Chapter then Priority (By Chapter) |
| Project Status tables are empty | Check `zzz Extras/Project Board.base` is still there; the tables come from it |
