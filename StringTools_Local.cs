namespace drewCo.Tools
{
  // ============================================================================================================================
  // TODO: These function(s) should be moved to the tools lib.
  public class StringTools_Local
  {
    enum EPadSide { Invalid = 0, Left, Right }

    // --------------------------------------------------------------------------------------------------------------------------
    public static string PadString(string input, int paddedLength)
    {
      int padSize = paddedLength - input.Length;
      if (padSize <= 0) { return input; }

      string padWith = new string(' ', padSize);

      var side = EPadSide.Right;
      switch (side)
      {
        case EPadSide.Left:
          return padWith + input;

        case EPadSide.Right:
          return input + padWith;

        default:
          throw new NotSupportedException();
      }
    }
  }

}
