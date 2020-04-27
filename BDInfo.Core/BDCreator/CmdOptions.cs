using CommandLine;

namespace BDCreator
{
  public sealed class CmdOptions
  {
    [Option('p', "path", Required = true, HelpText = "The path to bluray folder")]
    public string Path { get; set; }

    [Option('o', "output", Required = true, HelpText = "The output iso path")]
    public string Output { get; set; }
  }
}
