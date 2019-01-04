# SoulSeeker
An Asheron's Call PvP / macro assist plugin. Features include:
-Logout on PK detect, low comps (5 or less), 10% vitae
-Alt+F4 alternative logout
-friends
-sounds
-allegiance chat alert

Usage:
Edit monarch.cfg to contain ONE 10 digit GUID of your allegiance monarch or yourself.
Edit friends.cfg and add 10 digit GUIDs of non-allegiance members.
(GUID can be found with Virindi Tank by selecting player and typing /vt propertydump)

The Alt+F4 feature *requires* that line 21 in Thwargle.exe.config be changed to the following:
    <add key="NewGameTitle" value="%CHARACTER%" />

***Special thanks in no particular order go out to:***
Morosity,
LikeableLime,
Immortal Bob,
parad0x,
ChosenOne,
shark,
Pea,
Plus Ev,
Jkurs