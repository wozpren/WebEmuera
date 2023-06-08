namespace WebEmuera.Data
{
    //
    // 摘要:
    //     Specifies style information applied to text.
    [Flags]
    public enum FontStyle
    {
        //
        // 摘要:
        //     Normal text.
        Regular = 0,
        //
        // 摘要:
        //     Bold text.
        Bold = 1,
        //
        // 摘要:
        //     Italic text.
        Italic = 2,
        //
        // 摘要:
        //     Underlined text.
        Underline = 4,
        //
        // 摘要:
        //     Text with a line through the middle.
        Strikeout = 8
    }
}
