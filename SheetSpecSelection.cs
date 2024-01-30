namespace CutComputer
{

  internal partial class Program
  {
    class SheetSpecSelection
    {
      public SheetSpec Spec { get; set; }
      public ECutOrientation Orientation { get; set; }
      
      /// <summary>
      /// Given the orientation, how many strips can we get from the spec?
      /// </summary>
      public int StripCount { get; set; }

      /// <summary>
      /// Given all of the strips, what is the total available size on the spec?
      /// </summary>
      public decimal AvailableSize { get; set; }
    }
  }

}
