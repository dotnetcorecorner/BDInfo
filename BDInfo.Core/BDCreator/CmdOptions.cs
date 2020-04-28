using CommandLine;

namespace BDCreator
{
  public sealed class CmdOptions
  {
    [Option('p', "path", Required = true, HelpText = "The path to bluray folder")]
    public string Path { get; set; }

    [Option('o', "output", Required = true, HelpText = "The output iso path")]
    public string Output { get; set; }

    [Option('t', "test", Required = false, Default = false, HelpText = "Require test ?")]
    public bool Test { get; set; }

    [Option('d', "deleteatfail", Required = false, Default = false, HelpText = "Delete at test fail")]
    public bool Delete { get; set; }
  }
}
