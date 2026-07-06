namespace Seanzo.LevelDesign
{
    // Local axis of a prefab, used to describe how a piece was authored.
    // PosZ is first so legacy serialized data (0) reads as the canonical facing.
    public enum AuthoredAxis
    {
        PosZ,
        PosY,
        PosX,
        NegX,
        NegY,
        NegZ
    }
}
