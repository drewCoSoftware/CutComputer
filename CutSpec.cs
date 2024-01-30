using System.Net.NetworkInformation;

namespace CutComputer
{

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

    // --------------------------------------------------------------------------------------------------------------------------
    /// <summary>
    /// This 'rotates' the part by 90 degrees.
    /// The effect is that the length and width are swapped.
    /// If we ever care about grain direction, this will have some implications
    /// as well.
    /// </summary>
    internal void Rotate90()
    {
      var w = this.Width;
      this.Width = this.Length;
      this.Length = w;
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

    public List<CutItem> Cuts { get; set; } = new List<CutItem>();

    /// <summary>
    /// The amount of material that is still avaialable for cuts.
    /// This can also be considered scrap.
    /// </summary>
    public decimal AvailableLength
    {
      get
      {
        decimal usedSize = (from x in Cuts select x.Length).Sum();

        // NOTE: Include kerfs....
        // NOTE: If the piece is perfectly sized, then we will have one less kerf.
        // We are not accounting for this scenario at this time since it is pretty rare in practice.
        // NOTE: We also aren't counting dust cuts, which should be optional (and their size should be optional too)
        decimal kerf = Cuts.Count * Program.KERF_WIDTH;


        decimal res = Length - (usedSize + kerf);
        return res;

      }
    }

    // --------------------------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Add the given part to this spec.
    /// This function will attempt to minimize the amount of waste.
    /// </summary>
    public bool AddPart(PlywoodPart part)
    {
      if (!CanFitPart(part)) { return false; }

      // Minimize the length of the cut.
      if (part.Length < part.Width)
      {
        if (this.Width >= part.Width)
        {
          Cuts.Add(new CutItem(part.Length, part.Width));
          return true;
        }
      }

      // The width of the part is less or equal to the length...
      // NOTE: This part is technically oriented by 90 degrees...
      // If we have to take grain considerations into account, this
      // will matter.
      Cuts.Add(new CutItem(part.Width, part.Length));
      return true;


    }

    // --------------------------------------------------------------------------------------------------------------------------
    public bool CanFitPart(PlywoodPart part)
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

      for (int i = 0; i < Cuts.Count; ++i)
      {
        var p = Cuts[i];
        current += (p.Length + kerfWidth * Math.Sign(i));

        res.Add(current);
      }

      return res;
    }
  }

}
