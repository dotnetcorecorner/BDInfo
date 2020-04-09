using System;

namespace AnotherBDInfo
{
  internal sealed class ListElement
  {
    private string _text;
    private int _value;

    public string Text { get { return _text; } set { _text = value; OnTextChanged?.Invoke(_text); } }

    public int Value { get { return _value; } set { _value = value; OnProgressChanged?.Invoke(_value); } }

    public event Action<string> OnTextChanged;
    public event Action<int> OnProgressChanged;
  }
}
