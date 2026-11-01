---
status: Done
priority: 3
chapter:
  - Chapter 0 Game Flow
  - Chapter 3 Crafting
owner: Steve
plan: "[[Rune Field Finish Plan]]"
source: "[[Rune Field 3D Change Requests and to Do]]"
needs:
created: 2026-10-29
done:
---
## What
- Where access points go: the hub lab (it moves halfway through the game), one right before each boss, rare mid-dungeon safe rooms
- What the hub can do that a dungeon **field kit** can't (ideas: shard slotting, rune crafting, switching shades)
- The dungeon lamp's story and Hazen's lines when finding one ("A keeper must have left this here before the purge", "Master said keepers liked to hide these in plain sight")
## Completed Info
[[Rune Field Access Point]]s will be located in the [[Hub Scene]], [[Dungeon Entrance]]s, before bosses at the end of the dungeon, and [[Safe Floor]]s. The [[Hub Scene]] access point will be the only one that allows the player to edit runes for all shade slots. Rune crafting and all other inventory/equipment mechanics are done via other crafting areas which probably won't be in dungeon level areas. When we add more mechanics specific to runes and shade slots we'll decide if they'll be accessible by all access points or just the hub ones

see design entries linked in this info page. Will need to run through and format it and discuss any changes to the architecture

## Notes
- From the rune field finish talk, Oct 29, 2026. Decisions in [[Rune Field Finish Plan]]

## Waiting on this
Cards that link to this one, usually through their `needs`.
```base
filters:
  and:
    - file.inFolder("Tasks")
    - file.hasLink(this.file)
views:
  - type: table
    name: Waiting on this
    order:
      - file.name
      - file.folder
      - status
      - owner
```
