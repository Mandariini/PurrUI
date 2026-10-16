namespace PurrNet.UI
{
    /// <summary>
    /// The conventional role numbers used as <see cref="ColorInfo.color"/>.
    /// Values are serialized as ints in every prefab/scene, so they must never be
    /// renumbered or reused — only appended. A project's palette decides what each
    /// number is called, which colors it defines, and can add its own entries with
    /// numbers above the ones here.
    /// </summary>
    public enum ColorType
    {
        Accent = 0,
        Surface = 1,
        Success = 2,
        Warning = 3,
        Danger = 4,
        Background = 5,
        Black = 6,
        White = 7,
        Muted = 8
    }
}
