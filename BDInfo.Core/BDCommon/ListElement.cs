using System;

namespace BDCommon
{
  internal sealed class ListElement
  {
    private readonly int _position;
    private string _text;
    private double _value;

    public ListElement(int rowPosition)
    {
      _position = rowPosition;
      OnTextChanged = null;
      OnProgressChanged = null;
    }

    public string Text { get { return _text; } set { _text = value; OnTextChanged?.Invoke(_text, _position); } }

    public double Value { get { return _value; } set { _value = value; OnProgressChanged?.Invoke(_value, _position); } }

    public event Action<string, int> OnTextChanged;
    public event Action<double, int> OnProgressChanged;
  }
}
