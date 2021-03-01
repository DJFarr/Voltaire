using Discord.WebSocket;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Voltaire.Controllers.Messages;
using Voltaire.Controllers.Helpers;



namespace Voltaire.Utility
{
    public static class SlashCommandHandler
    {
      // TODO: This should be unified with SendToGuild.LookupAndSendAsync, but
      // SocketInteraction doesn't have a common ancestor with
      // ShardedCommandContext, so much of the code is duplicated while the
      // library is worked on.
      public static async Task Handle(SocketInteraction arg, DataBase db) {
          await Default(arg);

          var options = new List<SocketInteractionDataOption>(arg.Data.Options);
          var text = (String)options[0].Value;
          var replyable = false;
          if(arg.Data.Options.Count > 1) {
            replyable = (Boolean)options[1].Value;
          }
          var user = arg.Member;
          var guild = arg.Guild;

          var dbGuild = FindOrCreateGuild.Perform(guild, db);
          var userChannel = await user.GetOrCreateDMChannelAsync();

          if (!UserHasRole.Perform(guild, user, dbGuild))
          {

              await Send.SendErrorWithDeleteReaction(userChannel, "You do not have the role required to send messages to this server.");
              return;
          }

          if (PrefixHelper.UserBlocked(user.Id, dbGuild))
          {
              await Send.SendErrorWithDeleteReaction(userChannel, "It appears that you have been banned from using Voltaire on the targeted server. If you think this is an error, contact one of your admins.");
              return;
          }


          if(!IncrementAndCheckMessageLimit.Perform(dbGuild, db))
          {
              await Send.SendErrorWithDeleteReaction(userChannel, "This server has reached its limit of 50 messages for the month. To lift this limit, ask an admin or moderator to upgrade your server to Voltaire Pro. (This can be done via the `!volt pro` command.)");
              return;
          }

          var prefix = PrefixHelper.ComputePrefix(user, dbGuild);

          var messageFunction = Send.SendMessageToChannel(arg.Channel, replyable, user, userChannel, dbGuild.UseEmbed);
          await messageFunction(prefix, text);
      }

      public static async Task Default(SocketInteraction arg) {
        try {
          await arg.RespondAsync(Type:Discord.InteractionResponseType.Acknowledge);
        }
        catch (Exception e) {
          return;
        }
      }
    }
}