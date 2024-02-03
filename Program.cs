using drewCo.Tools;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Xml.Linq;
using static CutComputer.Program;

namespace CutComputer
{

  // ============================================================================================================================
  internal partial class Program
  {
    /// <summary>
    /// Nominal length of a dimensional 2x4
    /// </summary>
    public const decimal NOMINAL_LENGTH = 96.0m;

    /// <summary>
    /// How much loss from each cut?
    /// </summary>
    public const decimal KERF_WIDTH = 0.125m;

    public const decimal SHEET_WIDTH = 48.0m;
    public const decimal SHEET_LENGTH = 96.0m;

    public const decimal _2x4_WIDTH = 3.5m;
    public const decimal _2x4_HEIGHT = 1.5m;

    // --------------------------------------------------------------------------------------------------------------------------
    static void Main(string[] args)
    {

      ComputeAndPrint2x4CutList();
      return;


      // NOTE: Cut lists should be broken up into groups based on the nominal size.

      // Theory:  What we are really doing is taking a n-dimensional chunk of material,
      // and breaking it up smaller peices where n-1 dimensions are fixed.  Once we get it
      // down to one free dimension we can create those cut lists:
      // For example, i have a sheet of plywood.  it has w/l/h dimensinos.  Since one
      // of those dimensions is fixed (thickness) we can then treat it like a 2-dimensional
      // surface (3-1 = 2)
      // From the 2d surface, we can cut it into "strips" which then fixes another dimension,
      // which is the width.  So let's say a 10" strip from 1/2" ply.  Now I have a 'board'
      // that is 10" x 96" (or 48, etc.) x 1/2" which I may cut parts out of, each of
      // which have their own length.
      // This is analogous to getting lengths from 2x4 lumber, which has its fixed dimensions
      // of 1.5"x3/5" for its width/thickness(height).

      var drawerParts = new List<PlywoodPart>()
      {
        new PlywoodPart("B,C,E Front/Back", 15,16.5m, 6),
        new PlywoodPart("F Front/Back", 8.5m, 6, 2),
        new PlywoodPart("D Front/Back", 8.5m,8, 2),
        new PlywoodPart("A Front/Back", 10.5m,9, 2),

        new PlywoodPart("B,C,E Sides", 16.5m, 26, 6),
        new PlywoodPart("F Sides", 6,26, 2),
        new PlywoodPart("D Sides", 8,26, 2),
        new PlywoodPart("A Sides", 9,26, 2),

        new PlywoodPart("B,C,E Face", 17,18, 3),
        new PlywoodPart("F Face", 10.5m,7.5m, 1),
        new PlywoodPart("D Face", 10.5m,7.5m, 1),
        new PlywoodPart("A Face", 12.5m, 10.5m, 1),

      };



      // NOTE: If we had some exsting bits of plywood, we could pass them
      // into the function for consideration in the search space.
      // Flagging them ahead of time as 'scrap' would be useful in an organizational sense.
      var plywoodCuts = ComputeCutList(drawerParts);
      PrintCutList(plywoodCuts);

      // SUPER TODO:
      // Leverage ShapeEngine or similar to create an interactive display!
      // --> Side note... could ShapeEngine data/code be applied to HTML/TS?

    }

    // --------------------------------------------------------------------------------------------------------------------------
    private static void PrintCutList(List<SheetSpec> sheetSpecs)
    {
      int partNumber = 0;

      int index = 0;
      foreach (var s in sheetSpecs)
      {
        ++index;

        Console.WriteLine($"Sheet #{index}:");
        Console.WriteLine("-------------------------------------");

        Console.WriteLine();
        Console.WriteLine("Strips:");

        int stripNumber = 0;
        foreach (var strip in s.Strips)
        {
          ++stripNumber;
          Console.WriteLine();

          string fmt = null;
          switch (strip.CutOrientation)
          {
            case ECutOrientation.Width:
              fmt = $"Strip {stripNumber}: (W) {strip.Width}";
              break;
            case ECutOrientation.Length:
              fmt = $"Strip {stripNumber}: (L) {strip.Width}";
              break;
            default:
              throw new ArgumentOutOfRangeException("Unsupported orientation!");
          }
          Console.WriteLine(fmt);
          Console.WriteLine("--------------------------------");

          var formatter = new TextFormatter();

          // Print all of the parts for a given strip....
          foreach (var c in strip.Cuts)
          {
            ++partNumber;

            string star = (c.Width != strip.Width) ? " *" : string.Empty;
            formatter.AddLine($"{partNumber}:", c.Name, c.Length, c.Width + star);
          }

          string partsList = formatter.Print();
          Console.Write(partsList);
        }

      }
    }

    // --------------------------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Given a number of plywood parts, this will determine how to cut them all out of full sheets
    /// of plywood.
    /// </summary>
    private static List<SheetSpec> ComputeCutList(List<PlywoodPart> srcParts)
    {
      var res = new List<SheetSpec>();

      Dictionary<decimal, List<PlywoodPart>> dimGroups = GroupByDimension(srcParts);
      List<decimal> bySize = (from x in dimGroups.Keys select x).OrderByDescending(x => x).ToList();

      // Beginning with the longest part, let's try to chop up a sheet.
      foreach (var dim in bySize)
      {
        List<PlywoodPart> allParts = UngroupParts(dimGroups[dim]);

        PlaceParts(res, allParts);
      }

      return res;
    }

    // --------------------------------------------------------------------------------------------------------------------------
    /// <summary>
    /// NOTE: All of the parts should have a common dimension, length.
    /// </summary>
    private static void PlaceParts(List<SheetSpec> allSheets, List<PlywoodPart> toPlace)
    {
      decimal stripSize = ValidateLengths(toPlace);


      // Step 1.  Hunt through the existing sheets to see if we can fit any
      // of the parts.  If so, great!  If not, we will proceed.
      var usedParts = new HashSet<PlywoodPart>();

      foreach (var sheet in allSheets)
      {
        foreach (var p in toPlace)
        {
          if (sheet.AddPartToExistingStrip(p))
          {
            usedParts.Add(p);
            break;
          }
        }
      }
      toPlace.RemoveAll(x => usedParts.Contains(x));


      // Step 2. We need to create a new strip on an existing sheet
      // to accommodate the parts.
      while (toPlace.Count > 0)
      {
        usedParts.Clear();

        CutSpec? curSpec = null;
        foreach (var sheet in allSheets)
        {
          // NOTE: This code can be combined with step one.....
          // Add whatever parts we can, and 
          // NOTE: This code can be converted into something more like
          // 'sheet.createoptimalstrip()'.  Such a function could be
          // sensitive to orientation / grain direction too.
          var sel = ComputeSheetSelection(sheet, stripSize);
          if (sel.StripCount > 0)
          {
            if (AnyPartFits(toPlace, sel))
            {
              // NOTE: We still need to check to see if any of the parts can actually fit onto the strip before we create it!
              curSpec = sheet.CreateStrip(stripSize, sel.Orientation);
              break;
            }
          }
        }

        if (curSpec == null)
        {
          // No sheet can accommodate a strip can can fit any of the parts.
          // We will have to go on and create a new sheet to use.
          break;
        }

        // Add as many parts as we can to the strip...
        foreach (var p in toPlace)
        {
          // This will effectively fit at least one part on the strip.
          // We could optimize it so that it has as little waste as
          // possible, but that is a math problem for another day.
          if (curSpec.AddPart(p))
          {
            usedParts.Add(p);
          }
        }
        toPlace.RemoveAll(x => usedParts.Contains(x));
      }


      // Step 3.  If there are any parts left to place, we need to create a new
      // sheet.  We will then call this function recursively until all of the parts
      // are placed.
      // The new sheet is placed at the beginning of the list so that we
      if (toPlace.Count > 0)
      {
        var newSheet = new SheetSpec();
        var useSheets = new List<SheetSpec>() { newSheet };

        // Recursive call....
        PlaceParts(useSheets, toPlace);

        // Append the created sheets to the final set...
        allSheets.AddRange(useSheets);
      }

    }

    // ------------------------------------------------------------------------------------
    private static decimal ValidateLengths(List<PlywoodPart> toPlace)
    {
      if (toPlace.Count == 0)
      {
        throw new InvalidOperationException("Please provide at least one part!");
      }

      var lengths = (from x in toPlace
                     select x.Length).Distinct().ToArray();
      if (lengths.Length > 1)
      {
        throw new InvalidOperationException("Orient the parts so they all have the same length!");
      }

      return lengths[0];
    }

    // ------------------------------------------------------------------------------------
    private static bool AnyPartFits(List<PlywoodPart> toPlace, SheetSpecSelection sel)
    {
      return toPlace.Any(x =>
      {
        switch (sel.Orientation)
        {
          case ECutOrientation.Width:
            return x.Length <= sel.AvailableSize;
          case ECutOrientation.Length:
            return x.Width <= sel.AvailableSize;
          default:
            throw new NotSupportedException($"Unsupported orientation: {sel.Orientation}!");
        }
      });
    }

    // --------------------------------------------------------------------------------------------------------------------------
    private static SheetSpecSelection ComputeSheetSelection(SheetSpec spec, decimal stripSize)
    {
      // How many strips can we cut in the length direction?
      int itemsPerWidthStrip = (int)Math.Floor(spec.AvailableWidth / stripSize);
      decimal sizeByWidth = itemsPerWidthStrip * spec.AvailableLength;

      // What if we tile width-wise?
      int itemsByLengthStrip = (int)Math.Floor(spec.AvailableLength / stripSize);
      decimal sizeByLength = itemsByLengthStrip * spec.AvailableWidth;

      decimal maxSize = Math.Max(sizeByWidth, sizeByLength);
      var res = new SheetSpecSelection()
      {
        Spec = spec,
        AvailableSize = maxSize,
        Orientation = sizeByWidth > sizeByLength ? ECutOrientation.Length : ECutOrientation.Width,
        StripCount = sizeByWidth > sizeByLength ? itemsPerWidthStrip : itemsByLengthStrip
      };

      return res;
    }

    // --------------------------------------------------------------------------------------------------------------------------
    /// <summary>
    /// This will group parts by a common dimension so that sheets can more easily cut sheets
    /// into strips.
    /// </summary>
    private static Dictionary<decimal, List<PlywoodPart>> GroupByDimension(List<PlywoodPart> parts)
    {
      // Create the groups of parts based on their longest dimensions..
      var buckets = new Dictionary<decimal, List<PlywoodPart>>();
      foreach (var p in parts)
      {
        decimal maxdim = Math.Max(p.Width, p.Length);
        if (!buckets.TryGetValue(maxdim, out var partList))
        {
          partList = new List<PlywoodPart>();
          buckets[maxdim] = partList;
        }

        // We want to orient all of the parts so that length is the max
        // dimension.  This is to make downstream operations a bit easier
        // to deal with.
        if (p.Width > p.Length) { p.Rotate90(); }
        partList.Add(p);
      }

      return buckets;
    }

    // --------------------------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Returns a list of input parts where each part has a quantity of one.
    /// For example, if the input list contains "Part A, qty 2", it will return a list that has
    /// two instance of "Part A", each with a qty of 1.
    /// </summary>
    private static List<PlywoodPart> UngroupParts(List<PlywoodPart> parts)
    {
      var res = new List<PlywoodPart>();

      foreach (var p in parts)
      {
        for (int i = 0; i < p.Quantity; i++)
        {
          res.Add(new PlywoodPart(p.Name, p.Width, p.Length, 1));
        }
      }
      return res;
    }

    // --------------------------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Example of how we might represent some parts to be created from  2x4s.
    /// </summary>
    private static void ComputeAndPrint2x4CutList()
    {
      // The 'CutItems' listed here are 2x4.  It would be useful to be able to represent
      // that in the data somehow.
      // NOTE: 'CutItem' isn't really a great name....
      var cuts = new List<CutItem>()
      {
        CutItem.From2x4(58, 2),
        CutItem.From2x4(10, 2),
        CutItem.From2x4(16.5m, 6),
        CutItem.From2x4(60.5m, 2),
        CutItem.From2x4(41, 2),
        CutItem.From2x4(25, 2),
        CutItem.From2x4(11.5m, 2),
        CutItem.From2x4(10.5m, 2),
        CutItem.From2x4(8.5m, 2),
        CutItem.From2x4(12, 2),
        CutItem.From2x4(20.5m, 10)
      };

      int sum = (from x in cuts select x.Quantity).Sum();
      Console.WriteLine($"There are {sum} total pieces in the cut list!");

      CutList cutList = ComputeCutList(cuts);

      PrintCutList(cutList);
    }

    // --------------------------------------------------------------------------------------------------------------------------
    private static void PrintCutList(CutList cutList)
    {
      Console.WriteLine("Cut List:");
      Console.WriteLine("----------------------");
      Console.WriteLine();

      int index = 0;
      foreach (var spec in cutList.AllSpecs)
      {
        var useParts = ConsolidateParts(spec);

        if (index > 0)
        {
          Console.WriteLine("");
        }
        Console.WriteLine($"Spec {index}:");
        Console.WriteLine($"Name\tLength\tQty");
        ++index;

        foreach (var x in useParts.Cuts)
        {
          Console.WriteLine($"{x.Name}\t{x.Length}\t{x.Quantity}");
        }

        Console.WriteLine("");
        Console.WriteLine($"Cut at: " + FormatCutMeasurements(spec.GetCutMeasurements(KERF_WIDTH)));
        Console.WriteLine("");

        Console.WriteLine("Scrap:");
        Console.WriteLine(spec.AvailableLength);


      }
    }

    // --------------------------------------------------------------------------------------------------------------------------
    private static string FormatCutMeasurements(List<decimal> cutMeasurements)
    {
      string res = string.Join(", ", cutMeasurements);
      return res;
    }

    // --------------------------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Consolidate all parts that have the same name + length into a single part.
    /// </summary>
    private static CutSpec ConsolidateParts(CutSpec spec)
    {
      var res = new CutSpec()
      {
        Length = spec.Length,
      };
      var used = new HashSet<CutItem>();

      // We want to group all of the parts together....
      // NOTE: This algo isn't super efficient and loops over used items many, many times.
      foreach (var x in spec.Cuts)
      {
        if (used.Contains(x)) { continue; }
        used.Add(x);

        int useCount = x.Quantity;
        foreach (var y in spec.Cuts)
        {
          if (used.Contains(y)) { continue; }
          if (y.Name == x.Name && y.Length == x.Length)
          {
            used.Add(y);
            useCount += y.Quantity;
          }
        }

        res.Cuts.Add(new CutItem(x.Length, x.Width, x.Name, useCount)
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
      var toPlace = new List<CutItem>();
      foreach (var cut in cuts)
      {
        // Decompose....
        for (int i = 0; i < cut.Quantity; i++)
        {
          toPlace.Add(new CutItem(cut.Length, cut.Width));
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
            a.Cuts.Add(item);

            //            used.Add(item);
            break;
          }
        }

        if (!found)
        {
          // Add a new board!
          var board = new CutSpec();
          board.Cuts.Add(item);
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
    public static CutItem From2x4(decimal length, int qty, string? name = null)
    {
      var res = new CutItem(length, Program._2x4_WIDTH, name, qty);
      return res;
    }

    // --------------------------------------------------------------------------------------------------------------------------
    public CutItem(decimal length_, decimal width_, string? name_ = null, int qty_ = 1)
    {
      Length = length_;
      Width = width_;
      Name = name_;
      Quantity = qty_;
    }

    public string? Name { get; set; } = null;
    public int Quantity { get; set; } = 1;
    public decimal Length { get; set; }
    public decimal Width { get; set; }
  }

}
