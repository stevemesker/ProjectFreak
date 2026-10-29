---
status: To Do
priority: 5
chapter:
owner:
plan:
source:
needs:
created: {{date:YYYY-MM-DD}}
done:
---
## What
 

## Notes

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
