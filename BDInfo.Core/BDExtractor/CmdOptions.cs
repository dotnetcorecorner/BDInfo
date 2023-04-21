using CommandLine;

namespace BDExtractor
{
  public sealed class CmdOptions
  {
    [Option('p', "path", Required = true, HelpText = "The path to iso file")]
    public string Path { get; set; }

    [Option('o', "output", Required = false, HelpText = "The output folder. If not set then will extract in same folder as *.iso file")]
    public string Output { get; set; }
  }
}
