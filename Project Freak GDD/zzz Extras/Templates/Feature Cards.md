## Cards
Every card that links this note (through `plan`, `needs` or anywhere in its text), grouped by board.
```base
filters:
  and:
    - file.inFolder("Tasks")
    - file.hasLink(this.file)
views:
  - type: table
    name: Cards
    groupBy:
      property: file.folder
      direction: ASC
    order:
      - file.name
      - status
      - owner
      - needs
```
