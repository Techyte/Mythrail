using Riptide;

namespace Mythrail_Game_Server
{
    public static class MessageExtensions
    {
        public static Message AddMatchInfos(this Message message, MatchInfo[] value) => Add(message, value);
        
        public static Message Add(this Message message, MatchInfo[] value)
        {
            string[] names = new string[value.Length];
            string[] creatorNames = new string[value.Length];
            ushort[] ports = new ushort[value.Length];
            string[] codes = new string[value.Length];
            
            for (int i = 0; i < value.Length; i++)
            {
                names[i] = value[i].name;
                creatorNames[i] = value[i].creatorName;
                ports[i] = value[i].port;
                codes[i] = value[i].code;
            }
            
            message.AddStrings(names);
            message.AddStrings(creatorNames);
            message.AddUShorts(ports);
            message.AddStrings(codes);
            
            return message;
        }
        
        public static Message AddClientInfos(this Message message, ClientInfo[] value) => Add(message, value);
        
        public static Message Add(this Message message, ClientInfo[] value)
        {
            ushort[] ids = new ushort[value.Length];
            string[] usernames = new string[value.Length];
            
            for (int i = 0; i < value.Length; i++)
            {
                ids[i] = value[i].id;
                usernames[i] = value[i].username;
            }
            
            message.AddUShorts(ids);
            message.AddStrings(usernames);
            
            return message;
        }

        public static ClientInfo[] GetClientInfos(this Message message)
        {
            ushort[] ids = message.GetUShorts();
            string[] usernames = message.GetStrings();
            
            ClientInfo[] infos = new ClientInfo[ids.Length];

            for (int i = 0; i < ids.Length; i++)
            {
                infos[i] = new ClientInfo(ids[i], usernames[i]);
            }

            return infos;
        }
    }
}