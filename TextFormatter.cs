using System.Text;

namespace drewCo.Tools
{
  // TODO: This class should go in the tools lib.
  // ============================================================================================================================
  /// <summary>
  /// This class is mainly used to format bits of text into a tabular format
  /// for consoles.
  /// </summary>
  public class TextFormatter
  {
    private List<List<string>> _Lines = new List<List<string>>();

    // --------------------------------------------------------------------------------------------------------------------------
    public void Clear()
    {
      _Lines.Clear();
    }

    // --------------------------------------------------------------------------------------------------------------------------
    public void AddLine(params object?[] cols)
    {
      var toAdd = new List<string>();
      foreach (var item in cols)
      {
        toAdd.Add(item?.ToString() ?? "");
      }
      AddLine(toAdd);
    }

    // --------------------------------------------------------------------------------------------------------------------------
    public void AddLine(params string?[] cols)
    {
      var toAdd = new List<string>();
      foreach (var item in cols)
      {
        toAdd.Add(item ?? "");
      }
      AddLine(toAdd);
    }
    // --------------------------------------------------------------------------------------------------------------------------
    public void AddLine(List<string> cols)
    {
      _Lines.Add(cols);
    }

    // --------------------------------------------------------------------------------------------------------------------------
    public string Print()
    {
      int maxCols = 0;
      foreach (var line in _Lines)
      {
        maxCols = Math.Max(maxCols, line.Count);
      }

      // Determine the max width for each of the cols.
      var colSizes = new List<int>(maxCols);
      for (int i = 0; i < maxCols; i++)
      {
        colSizes.Add(0);
      }

      foreach (var colSet in _Lines)
      {
        int size = colSet.Count;
        for (int i = 0; i < maxCols; i++)
        {
          if (i >= size) { break; }

          int textLen = colSet[i].Length;
          colSizes[i] = Math.Max(colSizes[i], textLen);
        }

      }

      // Now that we know the max size of each column, we can format the output appropriately.
      var sb = new StringBuilder();
      foreach (var line in _Lines)
      {
        int index = 0;
        foreach (var col in line)
        {
          int colSize = colSizes[index];

          // NOTE: We can use different module sizes per column, if assigned....
          int useColSize = NormalizeColSize(colSize, 5);

          string padded = StringTools_Local.PadString(col, useColSize);
          sb.Append(padded);

          ++index;
        }
        sb.Append(Environment.NewLine);
      }

      string res = sb.ToString();
      return res;
    }

    // --------------------------------------------------------------------------------------------------------------------------
    private static int NormalizeColSize(int inputSize, int module, bool allowZeroPadding = false)
    {
      int modCount = (inputSize / module );
      if (inputSize % module == 0)
      {
        modCount += 1;
      }

      int res = modCount * module;
      if (res == inputSize || !allowZeroPadding)
      {
        res += module;
      }

      return res;
    }

    // --------------------------------------------------------------------------------------------------------------------------
    public override string ToString()
    {
      return this.Print();
    }

  }

}
