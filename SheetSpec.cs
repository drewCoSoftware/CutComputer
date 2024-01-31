namespace CutComputer
{
  // ============================================================================================================================
  /// <summary>
  /// Represents how we will cut sheet of plywood into constituent parts.
  /// This is like 'cutspec', but for a 2d object.
  /// </summary>
  public class SheetSpec
  {


    // --------------------------------------------------------------------------------
    public SheetSpec(decimal width_ = Program.SHEET_WIDTH, decimal length_ = Program.SHEET_LENGTH)
    {
      Width = width_;
      Length = length_;

      AvailableWidth = width_;
      AvailableLength = length_;
    }

    public decimal Width { get; private set; }
    public decimal Length { get; private set; }

    public decimal AvailableWidth { get; private set; }
    public decimal AvailableLength { get; private set; }

    /// <summary>
    /// All of the current strips and their constituent parts that we are going to cut
    /// this sheet into.
    /// NOTE: Each strip may have available length,etc. in it.
    /// NOTE: The order of the strips determines the available size of this sheet for
    /// subsequent strips to be cut from.
    /// For example, if the sheet is 48"x96" long, and we cut a 32" strip along its width,
    /// then the remaining available size is 48"x64".  This means that we won't be able
    /// to cut any length strips that are more than 64".
    /// </summary>
    public List<CutSpec> Strips { get; set; } = new List<CutSpec>();


    // --------------------------------------------------------------------------------
    /// <summary>
    /// Attempt to create a strip using the given data.
    /// If successful, a reference to it is returned, null otherwise.
    /// </summary>
    public CutSpec? CreateStrip(decimal stripSize, ECutOrientation orientation)
    {
      switch (orientation)
      {
        case ECutOrientation.Width:
          if (AvailableLength >= stripSize)
          {
            this.Strips.Add(new CutSpec()
            {
              CutOrientation = orientation,
              Length = AvailableWidth,
              Width = stripSize,
            });

            this.AvailableLength -= (stripSize + Program.KERF_WIDTH);
            return this.Strips[this.Strips.Count - 1];
          }
          break;

        case ECutOrientation.Length:
          if (Width >= stripSize)
          {
            this.Strips.Add(new CutSpec()
            {
              CutOrientation = orientation,
              Length = AvailableLength,
              Width = stripSize,
            });

            this.AvailableWidth -= (stripSize + Program.KERF_WIDTH);
            return this.Strips[this.Strips.Count - 1];
          }
          break;

        default:
          throw new ArgumentOutOfRangeException("Unsupported orientation!");
      }

      // Can't do it!
      return null;
    }


    // --------------------------------------------------------------------------------
    /// <summary>
    /// This determines if the given part can be cut from an existing strip on the sheet.
    /// This is typically used to reduce waste / scrap as smaller parts can sometimes
    /// fit.....
    /// </summary>
    /// <remarks>
    /// This function doesn't really go into how the part can fit on the strip, just that it can.
    /// </remarks>
    public bool CanAddPartToExistingStrip(PlywoodPart part)
    {
      foreach (CutSpec c in Strips)
      {
        if (c.CanFitPart(part)) { return true; }
      }

      return false;
    }


    // --------------------------------------------------------------------------------
    public bool AddPartToExistingStrip(PlywoodPart part)
    {
      foreach (var c in Strips)
      {
        if (c.CanFitPart(part))
        {
          c.AddPart(part);
          return true;
        }
      }

      return false;
    }

  }

}
