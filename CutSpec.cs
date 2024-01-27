namespace CutComputer
{
  // ============================================================================================================================
  /// <summary>
  /// Description of how we are going to cut some piece of material.
  /// </summary>
  /// NOTE: Not really sure about this name.... we are describing essentially a piece of lumber (material) that will be cut
  /// into lengths...
  public class CutSpec
  {
    public decimal NominalLength { get; set; } = Program.NOMINAL_LENGTH;
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


        decimal res = NominalLength - (usedSize + kerf);
        return res;

      }
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
