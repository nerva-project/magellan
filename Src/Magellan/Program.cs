using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using System.Xml.Linq;
using AngryWasp.Cli;
using AngryWasp.Cli.Args;
using AngryWasp.Serializer;
using AngryWasp.Serializer.Serializers;
using Log = AngryWasp.Logger.Log;

namespace MagellanServer
{
    public class MainClass
    {
        private const int DEFAULT_PORT = 15236;

        [STAThread]
        public static void Main(string[] rawArgs)
        {
            Log.CreateInstance(true);
            Serializer.Initialize();

            Arguments args = Arguments.Parse(rawArgs);

            NodeMapDataStore ds = new NodeMapDataStore();

            string dataDir = args.GetString("data-dir", Environment.CurrentDirectory);
            Log.Instance.Write($"Using data directory {dataDir}");

            if (File.Exists(Path.Combine(dataDir, "NodeMap.xml")))
            {
                Log.Instance.Write("Loading node map info");
                ds = new ObjectSerializer().Deserialize<NodeMapDataStore>(XDocument.Load(Path.Combine(dataDir, "NodeMap.xml")));
                Log.Instance.Write($"Node map loaded {ds.NodeMap.Count} items from file");

                if (!File.Exists("/var/www/html/nodemap.json"))
                    File.WriteAllText("/var/www/html/nodemap.json", $"{{\"status\":\"OK\",\"result\":{ds.FetchAll()}}}\r\n");
            }

            if (args["access-keys"] != null)
			{
                //todo: check file exists
                Log.Instance.Write("Access keys loaded from file");
				Config.AllowedKeys = File.ReadAllLines(args["access-keys"].Value).ToList();
			}    

            int port = args.GetInt("port", DEFAULT_PORT).Value;
            Log.Instance.Write($"Listening on port {port}");

            RpcListener r = new RpcListener();

            bool mapDataChanged = false;

            r.MapDataChanged += () => {
                mapDataChanged = true;
            };

            //run once every 5 minutes
            Timer t = new Timer(1000 * 60 * 5);
            t.Elapsed += (s, e) =>
            {
                if (mapDataChanged)
                {
                    Task.Run( () =>
                    {
                        new ObjectSerializer().Serialize(r.DataStore, Path.Combine(dataDir, "NodeMap.xml"));
                        Log.Instance.Write("Node map data saved");

                        Log.Instance.Write("Saving node map data to json");
                        File.WriteAllText("/var/www/html/nodemap.json", $"{{\"status\":\"OK\",\"result\":{r.DataStore.FetchAll()}}}\r\n");

                        mapDataChanged = false;
                    });
                }
            };

            t.Start();
            r.Start(ds, port);
            Log.Instance.Write("Ready...");
            Application.RegisterCommands();
            Application.Start();
        }
    }
}