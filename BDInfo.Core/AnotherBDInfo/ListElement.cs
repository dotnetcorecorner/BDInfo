using System;

namespace AnotherBDInfo
{
  internal sealed class ListElement
  {
    private readonly int _position;
    private string _text;
    private int _value;

    public ListElement(int rowPosition)
    {
      _position = rowPosition;
      OnTextChanged = null;
      OnProgressChanged = null;
    }

    public string Text { get { return _text; } set { _text = value; OnTextChanged?.Invoke(_text, _position); } }

    public int Value { get { return _value; } set { _value = value; OnProgressChanged?.Invoke(_value, _position); } }

    public event Action<string, int> OnTextChanged;
    public event Action<int, int> OnProgressChanged;
  }
}
