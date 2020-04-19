using CommandLine;

namespace BDExtractor
{
  public sealed class CmdOptions
  {
    [Option('p', "path", Required = true, HelpText = "The path to iso file")]
    public string Path { get; set; }

    [Option('o', "output", Required = true, HelpText = "The output folder")]
    public string Output { get; set; }
  }
}
