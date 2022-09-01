using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using RiptideNetworking;
using RiptideNetworking.Utils;

namespace Mythrail_Game_Server
{
    public enum GameServerToClientId : ushort
    {
        matches = 100,
        createMatchSuccess,
    }

    public enum ClientToGameServerId : ushort
    {
        id = 100,
        updateUsername,
        requestMatches,
        createMatch,
    }
    
    internal class Program
    {
        public static Dictionary<int, ClientInfo> currentlyConnectedClients = new Dictionary<int, ClientInfo>();

        private static Program program;

        private static Server Server;

        private ushort port;
        private ushort maxClientCount = 10;

        private static List<MatchInfo> matches = new List<MatchInfo>();
        
        public static void Main()
        {   
            program = new Program();
            program.Start();
        }

        private void Start()
        {
            AppDomain.CurrentDomain.ProcessExit += StopServer;
            
            RiptideLogger.Initialize(Console.Write, Console.Write, Console.Write, Console.Write, false);
            
            port = FreeTcpPort();
            
            Console.WriteLine(port);

            Server = new Server();
            Server.Start(port, maxClientCount);
            Server.ClientDisconnected += ClientDisconnected;
            
            var ServerTickTimer = new Timer(ServerTick, null, 0, 25);
            
            if(Console.ReadLine()=="-stop")
                StopServer(null, null);

            Console.ReadKey();
        }

        private void StopServer(object sender, EventArgs e)
        {
            Server.Stop();
            Console.WriteLine("Server stopped");
            foreach (var match in matches)
            {
                match.process.Kill();
            }
        }

        private void ServerTick(object o)
        {
            Server.Tick();
        }
        
        private void ClientDisconnected(object sender, ClientDisconnectedEventArgs e)
        {
            if(currentlyConnectedClients.TryGetValue(e.Id, out ClientInfo clientInfo))
                currentlyConnectedClients.Remove(e.Id);
        }

        private void SendMatches(ushort clientId)
        {
            Message message = Message.Create(MessageSendMode.reliable, GameServerToClientId.matches);
            message.AddMatchInfos(matches.ToArray());
            
            Server.Send(message, clientId);
        }

        private static void SendMatchesToAll()
        {
            Message message = Message.Create(MessageSendMode.reliable, GameServerToClientId.matches);
            message.AddMatchInfos(matches.ToArray());

            Server.SendToAll(message);
        }
        
        static ushort FreeTcpPort()
        {
            TcpListener l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();
            ushort port = (ushort)((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
            return port;
        }

        [MessageHandler((ushort)ClientToGameServerId.id)]
        private static void ReceiveConnecrId(ushort fromClientId, Message message)
        {
            currentlyConnectedClients.Add(fromClientId, new ClientInfo(fromClientId, message.GetString()));
            Console.WriteLine(currentlyConnectedClients[fromClientId].username);
            Console.WriteLine("Client connected and we are receiving information");
        }

        [MessageHandler((ushort)ClientToGameServerId.createMatch)]
        private static void CreateMatchHandler(ushort fromClientId, Message message)
        {
            try
            {
                Process matchProcess = new Process();
                matchProcess.EnableRaisingEvents = true;
                matchProcess.StartInfo.FileName = @"C:\Users\Mr. Monster\Documents\Coding\Match\Mythrail Server.exe";
            
                ushort port = FreeTcpPort();
                ushort maxPlayers = message.GetUShort();
                ushort minPlayers = message.GetUShort();
                Console.WriteLine(minPlayers);
                matchProcess.StartInfo.Arguments = $"port:{port.ToString()} maxPlayers:{maxPlayers.ToString()} minPlayers:{minPlayers.ToString()}";

                matchProcess.Start();
            
                MatchInfo newMatch = new MatchInfo(message.GetString(), fromClientId, matchProcess, port);
                matches.Add(newMatch);
                matchProcess.Exited += (sender, eventArgs) =>
                {
                    MatchEnded(newMatch);
                };
            
                Console.WriteLine("Client requested a match to be created");
            
                program.SendMatches(fromClientId);
                
                program.SendMatchCreationConformation(fromClientId, port);
                
                SendMatchesToAll();
            }
            catch(Exception e)
            {
                Console.WriteLine("Match Creation Failure: " + e);
            }
        }

        private void SendMatchCreationConformation(ushort fromClientId, ushort port)
        {
            Message conformationMessage = Message.Create(MessageSendMode.reliable, GameServerToClientId.createMatchSuccess);
            conformationMessage.AddUShort(port);
            Server.Send(conformationMessage, fromClientId);
        }

        private static void MatchEnded(MatchInfo match)
        {
            matches.Remove(match);
            Console.WriteLine("Match Ended");
            SendMatchesToAll();
        }

        [MessageHandler((ushort)ClientToGameServerId.requestMatches)]
        private static void MatchesRequested(ushort fromClientId, Message message)
        {
            program.SendMatches(fromClientId);
            Console.WriteLine("Client requested a matches list");
        }

        [MessageHandler((ushort)ClientToGameServerId.updateUsername)]
        private static void UpdateUsername(ushort fromClientId, Message message)
        {
            if (currentlyConnectedClients.TryGetValue(fromClientId, out ClientInfo clientInfo))
            {
                clientInfo.username = message.GetString();
                Console.WriteLine("Updated username: " + clientInfo.username);
            }
        }
    }
    
    

    public class ClientInfo
    {
        public ushort id;
        public string username;

        public ClientInfo(ushort id, string username)
        {
            this.id = id;
            this.username = username;
        }
    }

    public class MatchInfo
    {
        public string name;
        public ushort creatorId;
        public Process process;
        public ushort port;

        public MatchInfo(string name, ushort creatorId, Process process, ushort port)
        {
            this.name = name;
            this.creatorId = creatorId;
            this.process = process;
            this.port = port;
        }
    }
}