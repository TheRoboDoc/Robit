// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Usage", "CA2211:Non-constant fields should not be visible", Justification = "No reasoable way to change around this", Scope = "member", Target = "~F:Robit.Program.botClient")]
[assembly: SuppressMessage("Usage", "CA2211:Non-constant fields should not be visible", Justification = "No reasoable way to change around this", Scope = "member", Target = "~F:Robit.Program.giphyClient")]
[assembly: SuppressMessage("Usage", "CA2211:Non-constant fields should not be visible", Justification = "No reasoable way to change around this", Scope = "member", Target = "~F:Robit.Program.openAiService")]
[assembly: SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Cannot rename as a lot of recorded response entries exist and renaming would require rewriting of a lot of JSON entries", Scope = "member", Target = "~P:Robit.FileManager.ResponseManager.ResponseEntry.reactName")]
[assembly: SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Cannot rename as a lot of recorded response entries exist and renaming would require rewriting of a lot of JSON entries", Scope = "member", Target = "~P:Robit.FileManager.ResponseManager.ResponseEntry.content")]
[assembly: SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Cannot rename as a lot of recorded response entries exist and renaming would require rewriting of a lot of JSON entries", Scope = "member", Target = "~P:Robit.FileManager.ResponseManager.ResponseEntry.response")]
[assembly: SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Cannot rename as a lot of recorded response entries exist and renaming would require rewriting of a lot of JSON entries", Scope = "member", Target = "~P:Robit.FileManager.ChannelManager.Channel.autoResponse")]
[assembly: SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Cannot remove as would break events that trigger it", Scope = "member", Target = "~M:Robit.Response.Handler.Run(DSharpPlus.DiscordClient,DSharpPlus.EventArgs.MessageCreateEventArgs)~System.Threading.Tasks.Task")]
[assembly: SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Cannot rename as a lot of recorded response entries exist and renaming would require rewriting of a lot of JSON entries", Scope = "member", Target = "~P:Robit.FileManager.QuoteManager.QuoteEntry.author")]
[assembly: SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Cannot rename as a lot of recorded response entries exist and renaming would require rewriting of a lot of JSON entries", Scope = "member", Target = "~P:Robit.FileManager.QuoteManager.QuoteEntry.bookSource")]
[assembly: SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Cannot rename as a lot of recorded response entries exist and renaming would require rewriting of a lot of JSON entries", Scope = "member", Target = "~P:Robit.FileManager.QuoteManager.QuoteEntry.quote")]
[assembly: SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Breaks Commands if this is done", Scope = "member", Target = "~M:Robit.Command.Commands.Ping(DSharpPlus.CommandsNext.CommandContext)~System.Threading.Tasks.Task")]
[assembly: SuppressMessage("Usage", "CA2211:Non-constant fields should not be visible", Justification = "Breaks AI status check", Scope = "member", Target = "~F:Robit.Program.chosenStatus")]
