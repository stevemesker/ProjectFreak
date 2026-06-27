All hand held weapons utilize the ITriggerable interface in CombatInterface. They will have the following functions:

| Type          | Description                                                                                                                                                                |
| :------------ | :------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| SetUpWeapon   | used for initializing the held weapon so it knows its stats and who the user is so self attacking won't happen                                                             |
| updateStats   | used to add bonuses and effects that could happen after selecting the weapon                                                                                               |
| TriggerAttack | the trigger button is pressed, begin activation of the weapon. Takes in the stat point appropriate for the weapon type as well as any elemental effects that must be added |
| ReleaseAttack | the trigger button is released, do any final effects and reset the weapon                                                                                                  |
