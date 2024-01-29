using System.Net.NetworkInformation;

namespace CutComputer
{

  // ============================================================================================================================
  /// <summary>
  /// Represents how we will cut sheet of plywood into constituent parts.
  /// This is like 'cutspec', but for a 2d object.
  /// </summary>
  public class SheetSpec
  {
    public decimal Width { get; set; } = Program.SHEET_WIDTH;
    public decimal Height { get; set; } = Program.SHEET_HEIGHT;


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


    /// <summary>
    /// This determines if the given part can be cut from an existing strip on the sheet.
    /// This is typically used to reduce waste / scrap as smaller parts can sometimes
    /// fit.....
    /// </summary>
    /// <remarks>
    /// This function doesn't really go into how the part can fit on the strip, just that it can.
    /// </remarks>
    public bool CanPartBeCutFromExistingStrip(PlywoodPart part)
    {
      foreach (CutSpec c in Strips)
      {
        if (c.CanFitPart(part)) { return true; }  
      }

      return false;
    }

  }

  // ============================================================================================================================
  // NOTE: This class name isn't really correct, but for our example we are considering how to determine
  // how much plywood to buy and how to chop it up for a given set of parts.
  // The approach is similar to how we are doing 'CutSpec' for 2x4s
  public class PlywoodPart
  {
    public decimal Thickness { get; private set; } = 0.5m;

    public string Name { get; private set; }
    public decimal Width { get; private set; }
    public decimal Length { get; private set; }
    public int Quantity { get; private set; }

    // --------------------------------------------------------------------------------------------------------------------------
    public PlywoodPart(string name_, decimal width_, decimal length_, int quantity_)
    {
      Name = name_;
      Width = width_;
      Length = length_;
      Quantity = quantity_;
    }

  }


  // ============================================================================================================================
  public enum ECutOrientation
  {
    /// <summary>
    /// Cut orientatino is not specified, or does not apply.
    /// </summary>
    None = 0,
    /// <summary>
    /// We cut along the width (x) direction.
    /// </summary>
    Width,
    /// <summary>
    /// We cut along the length (y) direction.
    /// </summary>
    Length,
    /// <summary>
    ///  We cut along the height (thickness/z) direction.
    /// </summary>
    Height,
  }

  // ============================================================================================================================
  /// <summary>
  /// Description of how we are going to cut some piece of material.
  /// </summary>
  /// NOTE: Not really sure about this name.... we are describing essentially a piece of lumber (material) that will be cut
  /// into lengths...
  public class CutSpec
  {
    public decimal Length { get; set; } = Program.NOMINAL_LENGTH;
    public decimal Width { get; set; } = Program._2x4_WIDTH;
    public decimal Height { get; set; } = Program._2x4_HEIGHT;

    public ECutOrientation CutOrientation { get; set; } = ECutOrientation.None;

    public List<CutItem> Parts { get; set; } = new List<CutItem>();

    /// <summary>
    /// The amount of material that is still avaialable for cuts.
    /// This can also be considered scrap.
    /// </summary>
    public decimal AvailableLength
    {
      get
      {
        decimal usedSize = (from x in Parts select x.Length).Sum();

        // NOTE: Include kerfs....
        // NOTE: If the piece is perfectly sized, then we will have one less kerf.
        // We are not accounting for this scenario at this time since it is pretty rare in practice.
        // NOTE: We also aren't counting dust cuts, which should be optional (and their size should be optional too)
        decimal kerf = Parts.Count * Program.KERF_WIDTH;


        decimal res = Length - (usedSize + kerf);
        return res;

      }
    }

    // --------------------------------------------------------------------------------------------------------------------------
    internal bool CanFitPart(PlywoodPart part)
    {
      // If the width or length of the part exceeds both dimensions of this spec,
      // then it can't fit!
      if (part.Width > AvailableLength && part.Width > Width)
      {
        return false;
      }
      if (part.Length > AvailableLength && part.Length > Width)
      {
        return false;
      }

      return true;
      throw new NotImplementedException();
    }


    /// <summary>
    /// Return a list of all lengths that can be marked out on your board for easier cutting.
    /// These measurements take the kerf of your cutting tool into account.
    /// </summary>
    internal List<decimal> GetCutMeasurements(decimal kerfWidth)
    {

      decimal current = 0.0m;
      var res = new List<decimal>();

      for (int i = 0; i < Parts.Count; ++i)
      {
        var p = Parts[i];
        current += (p.Length + kerfWidth * Math.Sign(i));

        res.Add(current);
      }

      return res;
    }
  }

}
