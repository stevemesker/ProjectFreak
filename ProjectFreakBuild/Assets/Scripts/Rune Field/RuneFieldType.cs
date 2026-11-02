namespace RuneFieldType
{
    //what the pointer is over on the 3D rune field. Used as RuneFieldType.Hover
    public enum Hover { Nothing, Rune, Node, Core }

    //how a zone looks on the 3D rune field, from the open zone: below it frozen, at it open, past it locked. Used as RuneFieldType.ZoneState
    public enum ZoneState { Open, Frozen, Locked }

    //which material a ring border's wall shows: the forward tear, the smoky one behind it, or nothing. Used as RuneFieldType.BorderLook
    public enum BorderLook { Off, Tear, Smoke }

    //which kind of access point opened the rune field. Advanced is the hub labs, Basic is everywhere else (dungeon entrances, before bosses, safe and vault floors)
    //None means the field scene was opened on its own (testing in the editor), which allows everything. Used as RuneFieldType.Access
    public enum Access { None, Basic, Advanced }
}
