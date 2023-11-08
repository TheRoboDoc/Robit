using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;
using static Robit.FileManager.AutoroleManager;

namespace Robit.Role
{
    /// <summary>
    ///     A set of methods to manage automatic role functions
    /// </summary>
    public static class Auto
    {
        /// <summary>
        ///     Give a role to a discord user
        /// </summary>
        /// 
        /// <param name="guild">
        ///     Guild the user is at
        /// </param>
        /// 
        /// <param name="member">
        ///     User to give the role to
        /// </param>
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
