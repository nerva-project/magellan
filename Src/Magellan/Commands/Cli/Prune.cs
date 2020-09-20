using AngryWasp.Cli;

namespace MagellanServer.Commands.Cli
{
    [ApplicationCommand("prune", "Prune the node map")]
    public class Prune : IApplicationCommand
    {
        public bool Handle(string command)
        {
            throw new System.NotImplementedException();
        }
    }
}