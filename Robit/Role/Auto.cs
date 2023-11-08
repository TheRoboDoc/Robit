using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;
using static Robit.FileManager.AutoroleManager;

namespace Robit.Role
{
    public static class Auto
    {
        public static async Task GiveRole(DiscordGuild guild, DiscordMember member)
        {
            List<Autorole>? autoroles = ReadEntries(guild.Id.ToString());

            if (autoroles == null || !autoroles.Any())
            {
                return;
            }

            foreach (Autorole autorole in autoroles)
            {
                if (!ulong.TryParse(autorole.RoleID, out ulong id))
                {
                    break;
                }

                try
                {
                    await member.GrantRoleAsync(guild.GetRole(id), "Automatic role");
                }
                catch
                {
                    Program.BotClient?.Logger.LogWarning("Failed to add role in {guild} ID: {ID}", guild.Name, guild.Id);
                }
            }
        }
    }
}
