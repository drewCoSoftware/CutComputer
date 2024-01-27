using System.Xml.Linq;

namespace CutComputer
{


  internal class Program
  {
    /// <summary>
    /// Nominal length of a dimensional 2x4
    /// </summary>
    public const decimal NOMINAL_LENGTH = 96.0m;

    /// <summary>
    /// How much loss from each cut?
    /// </summary>
    public const decimal KERF_WIDTH = 0.125m;

    // --------------------------------------------------------------------------------------------------------------------------
    static void Main(string[] args)
    {
      Console.WriteLine("Hello, World!");

      var cuts = new List<CutItem>()
      {
        new CutItem(58, 2),
        new CutItem(10, 2),
        new CutItem(16.5m, 6),
        new CutItem(60.5m, 2),
        new CutItem(41, 2),
        new CutItem(25, 2),
        new CutItem(11.5m, 2),
        new CutItem(10.5m, 2),
        new CutItem(8.5m, 2),
        new CutItem(12, 2),
        new CutItem(20.5m, 10)
      };

      int sum = (from x in cuts select x.Quantity).Sum();
      Console.WriteLine($"There are {sum} total pieces in the cut list!");


      CutList cutList = ComputeCutList(cuts);

      Console.WriteLine("Cut List:");
      Console.WriteLine("----------------------");
      Console.WriteLine();

      int index = 0;
      foreach (var spec in cutList.AllSpecs)
      {
        var useParts = ConsolidateParts(spec);

        Console.WriteLine($"Spec {index}:");
        Console.WriteLine($"Name\tLength\tQty");
        ++index;

        foreach (var x in useParts.Parts)
        {
          Console.WriteLine($"{x.Name}\t{x.Length}\t{x.Quantity}");
        }


      }

    }

    // --------------------------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Consolidate all parts that have the same name + length into a single part.
    /// </summary>
    private static CutSpec ConsolidateParts(CutSpec spec)
    {
      var res = new CutSpec()
      {
        NominalLength = spec.NominalLength,
      };
      var used = new HashSet<CutItem>();

      // We want to group all of the parts together....
      // NOTE: This algo isn't super efficient and loops over used items many, many times.
      foreach (var x in spec.Parts)
      {
        if (used.Contains(x)) { continue; }
        used.Add(x);

        int useCount = x.Quantity;
        foreach (var y in spec.Parts)
        {
          if (used.Contains(y)) { continue; }
          if (y.Name == x.Name && y.Length == x.Length)
          {
            used.Add(y);
            useCount += y.Quantity;
          }
        }

        res.Parts.Add(new CutItem(x.Length, useCount)
        {
          Name = x.Name
        });

      }

      return res;
    }

    // --------------------------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Given the list of items, this will compute the number of boards, and how they should be cut.
    /// The goal of the algorithm is to minimize the total amount of scrap.
    /// </summary>
    private static CutList ComputeCutList(List<CutItem> cuts)
    {

      // All of the items that we have already included in the final cut list....
      var used = new HashSet<CutItem>();
      var toPlace = new List<CutItem>();
      foreach (var cut in cuts)
      {
        // Decompose....
        for (int i = 0; i < cut.Quantity; i++)
        {
          toPlace.Add(new CutItem(cut.Length));
        }
      }
      var specList = new List<CutSpec>();


      // Go over all of the items in the list, longest first and try to find a home for them.
      // If we don't have any spare space, or available boards, throw a new one onto the pile.
      foreach (var item in toPlace)
      {
        if (item.Length > Program.NOMINAL_LENGTH)
        {
          throw new NotSupportedException($"There is no support for Cut Items that exceed the nominal length!");
        }

        bool found = false;
        foreach (var a in specList)
        {
          if (a.AvailableLength >= item.Length)
          {
            found = true;
            a.Parts.Add(item);

            //            used.Add(item);
            break;
          }
        }

        if (!found)
        {
          // Add a new board!
          var board = new CutSpec();
          board.Parts.Add(item);
          specList.Add(board);
        }

      }

      var res = new CutList()
      {
        AllSpecs = specList
      };
      return res;

    }
  }

  // ============================================================================================================================
  public class CutList
  {
    /// <summary>
    /// How do we compose all of our cuts...
    /// NOTE: 'specs' and 'allspecs' doesn't really make perfect sense since each 'spec' represents a board....
    /// </summary>
    public List<CutSpec> AllSpecs { get; set; } = new List<CutSpec>();
  }


  // ============================================================================================================================
  /// <summary>
  /// Some cut that we want to make from a nominally sized piece of lumber.
  /// Let's assume a 2x4 for now.
  /// </summary>
  public class CutItem
  {
    // --------------------------------------------------------------------------------------------------------------------------
    public CutItem(decimal length, int count = 1)
    {
      this.Length = length;
      this.Quantity = count;
    }

    public string? Name { get; set; } = null;
    public int Quantity { get; set; } = 1;
    public decimal Length { get; set; }
  }

}
