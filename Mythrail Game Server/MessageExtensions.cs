using RiptideNetworking;

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
            
            for (int i = 0; i < value.Length; i++)
            {
                names[i] = value[i].name;
                creatorNames[i] = value[i].creatorName;
                ports[i] = value[i].port;
            }
            
            message.AddStrings(names);
            message.AddStrings(creatorNames);
            message.AddUShorts(ports);
            
            return message;
        }
    }
}