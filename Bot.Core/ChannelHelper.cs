using Bot.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot.Core
{
    class ChannelHelper
    {

        public static async Task<IChannelCategory?> GetOrCreateCategory(IGuild guild, string name)
        {
            var cat = guild.ChannelCategories.Where(x => x.Name == name).FirstOrDefault();

            if(cat == null)
            {
                Console.WriteLine($"ChannelHelper: Creating category '{name}' in guild {guild.Id}");
                cat = await guild.CreateCategoryAsync(name);
                Console.WriteLine($"ChannelHelper: Category created. Name='{name}', Id={cat?.Id}");
            }
            else
            {
                Console.WriteLine($"ChannelHelper: Category already exists. Name='{name}', Id={cat.Id}");
            }

            return cat;
        }

        public static async Task<IChannel?> GetOrCreateVoiceChannel(IGuild guild, IChannelCategory parent, string name)
        {
            var chan = parent.Channels.Where(x => x.Name == name).FirstOrDefault();

            if(chan == null)
            {
                Console.WriteLine($"ChannelHelper: Creating voice channel '{name}' in category '{parent.Name}' (Id={parent.Id})");
                chan = await guild.CreateVoiceChannelAsync(name, parent);
                Console.WriteLine($"ChannelHelper: Voice channel created. Name='{name}', Id={chan?.Id}, ParentId={parent.Id}");
            }
            else
            {
                Console.WriteLine($"ChannelHelper: Voice channel already exists. Name='{name}', Id={chan.Id}");
            }

            return chan;
        }

        public static string MakeTextChannelName(string inName)
        {
            return inName.ToLower().Replace(' ', '-');
        }

        public static async Task<IChannel?> GetOrCreateTextChannel(IGuild guild, IChannelCategory parent, string name)
        {
            var textName = MakeTextChannelName(name);
            var chan = parent.Channels.Where(x => x.Name == textName).FirstOrDefault();

            if(chan == null)
            {
                Console.WriteLine($"ChannelHelper: Creating text channel '{textName}' (from '{name}') in category '{parent.Name}' (Id={parent.Id})");
                chan = await guild.CreateTextChannelAsync(name, parent);
                Console.WriteLine($"ChannelHelper: Text channel created. Name='{chan?.Name}', Id={chan?.Id}, ParentId={parent.Id}");
            }
            else
            {
                Console.WriteLine($"ChannelHelper: Text channel already exists. Name='{chan.Name}', Id={chan.Id}");
            }

            return chan;
        }
    }
}
