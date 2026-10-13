namespace RuneFieldType
{
    //what the pointer is over on the 3D rune field. Used as RuneFieldType.Hover
    public enum Hover { Nothing, Rune, Node, Core }

    //how a zone looks on the 3D rune field, from the open zone: below it frozen, at it open, past it locked. Used as RuneFieldType.ZoneState
    public enum ZoneState { Open, Frozen, Locked }
}
