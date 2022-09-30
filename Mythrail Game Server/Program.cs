using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
        joinedPrivateMatch,
        privateMatchNotFound,
        invalidName,
    }

    public enum ClientToGameServerId : ushort
    {
        id = 100,
        updateUsername,
        requestMatches,
        createMatch,
        joinPrivateMatch,
    }
    
    internal class Program
    {
        public static Dictionary<int, ClientInfo> currentlyConnectedClients = new Dictionary<int, ClientInfo>();

        private static Program program;

        private static Server Server;

        private ushort port = 63231;
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
            
            List<MatchInfo> publicMatches = new List<MatchInfo>();
            foreach (var match in matches)
            {
                if (!match.isPrivate)
                {
                    publicMatches.Add(match);
                }
            }
            message.AddMatchInfos(publicMatches.ToArray());
            
            Server.Send(message, clientId);
        }

        private static void SendMatchesToAll()
        {
            Message message = Message.Create(MessageSendMode.reliable, GameServerToClientId.matches);
            List<MatchInfo> publicMatches = new List<MatchInfo>();
            foreach (var match in matches)
            {
                if (!match.isPrivate)
                {
                    publicMatches.Add(match);
                }
            }
            message.AddMatchInfos(publicMatches.ToArray());

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
                matchProcess.StartInfo.FileName = Directory.GetCurrentDirectory() + @"\Match Application\Mythrail Server.exe";
            
                ushort port = FreeTcpPort();
                ushort maxPlayers = message.GetUShort();
                ushort minPlayers = message.GetUShort();
                Random random = new Random();
                string code = "";
                for (int i = 0; i < 5; i++)
                {
                    int randValue = random.Next(0, 26);
                    char letter = Convert.ToChar(randValue + 65);
                    code = code + letter;
                }
                matchProcess.StartInfo.Arguments = $"port:{port.ToString()} maxPlayers:{maxPlayers.ToString()} minPlayers:{minPlayers.ToString()}";

                matchProcess.Start();

                string name = message.GetString();
                bool isPrivate = message.GetBool();
                MatchInfo newMatch = new MatchInfo(name, currentlyConnectedClients[fromClientId].username, matchProcess, port, isPrivate, code);
                matches.Add(newMatch);
                matchProcess.Exited += (sender, eventArgs) =>
                {
                    MatchEnded(newMatch);
                };
            
                Console.WriteLine("Client requested a match to be created");
            
                program.SendMatches(fromClientId);
                
                program.SendMatchCreationConformation(fromClientId, port, isPrivate, code);
                
                SendMatchesToAll();
            }
            catch(Exception e)
            {
                Console.WriteLine("Match Creation Failure: " + e);
            }
        }

        private void SendMatchCreationConformation(ushort fromClientId, ushort port, bool isPrivate, string code)
        {
            Message message = Message.Create(MessageSendMode.reliable, GameServerToClientId.createMatchSuccess);
            message.AddBool(isPrivate);
            message.AddString(code);
            message.AddUShort(port);
            Server.Send(message, fromClientId);
        }

        private static void MatchEnded(MatchInfo match)
        {
            matches.Remove(match);
            Console.WriteLine("Match Ended");
            SendMatchesToAll();
        }

        private void PrivateMatchFound(ushort toClientId, ushort port)
        {
            Message message = Message.Create(MessageSendMode.reliable, GameServerToClientId.joinedPrivateMatch);
            message.AddUShort(port);
            Server.Send(message, toClientId);
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
                string newUsername = message.GetString();
                foreach (var client in currentlyConnectedClients)
                {
                    if (client.Value.username == newUsername)
                    {
                        Message invalidUsernameMessage = Message.Create(MessageSendMode.reliable, GameServerToClientId.invalidName);
                        Server.Send(invalidUsernameMessage, fromClientId);
                        return;
                    }
                }
                clientInfo.username = newUsername;
                Console.WriteLine("Updated username: " + clientInfo.username);
            }
        }

        [MessageHandler((ushort)ClientToGameServerId.joinPrivateMatch)]
        private static void JoinPrivateMatch(ushort fromClientId, Message message)
        {
            string codeKey = message.GetString();
            Console.WriteLine(codeKey);
            foreach (MatchInfo match in matches)
            {
                if (match.isPrivate)
                {
                    if (match.code == codeKey)
                    {
                        program.PrivateMatchFound(fromClientId, match.port);
                        return;
                    }
                }
            }
            
            Message failedMessage = Message.Create(MessageSendMode.reliable, GameServerToClientId.privateMatchNotFound);
            Server.Send(failedMessage, fromClientId);
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
        public string creatorName;
        public Process process;
        public ushort port;
        public bool isPrivate;
        public string code;

        public MatchInfo(string name, string creatorName, Process process, ushort port, bool isPrivate, string code)
        {
            this.name = name;
            this.creatorName = creatorName;
            this.process = process;
            this.port = port;
            this.isPrivate = isPrivate;
            this.code = code;
        }
    }
}