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


        decimal res = NominalLength -  (usedSize + kerf);
        return res;

      }
    }
  }

}
