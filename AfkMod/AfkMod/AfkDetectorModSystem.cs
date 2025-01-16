using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace AfkDetectorMod
{
    public class AfkDetectorModSystem : ModSystem
    {
        private ICoreServerAPI sapi;
        private Dictionary<string, PlayerAfkInfo> playerAfkData = new Dictionary<string, PlayerAfkInfo>();
        private float afkThresholdMinutes = 15f; // Default to 15 minutes

        public override void StartServerSide(ICoreServerAPI api)
        {
            sapi = api;

            sapi.Event.PlayerJoin += OnPlayerJoin;
            sapi.Event.PlayerLeave += OnPlayerLeave;

            sapi.Event.RegisterGameTickListener(OnPeriodicAfkCheck, 1000);

            sapi.ChatCommands
                .Create("afk")
                .WithDescription("Manage AFK mod settings")
                .RequiresPrivilege(Privilege.controlserver)
                .WithArgs(api.ChatCommands.Parsers.OptionalWord("subcommand"),
                          api.ChatCommands.Parsers.OptionalInt("int value"))
                .HandleWith(OnAfkModCommand);
        }

        private void OnPlayerJoin(IServerPlayer player)
        {
            playerAfkData[player.PlayerUID] = new PlayerAfkInfo
            {
                LastKnownPosition = player.Entity.Pos.AsBlockPos,
                LastActiveTime = sapi.World.ElapsedMilliseconds
            };
        }

        private void OnPlayerLeave(IServerPlayer player)
        {
            playerAfkData.Remove(player.PlayerUID);
        }

        private void OnPeriodicAfkCheck(float dt)
        {
            long currentTime = sapi.World.ElapsedMilliseconds;

            foreach (var playerEntry in sapi.Server.Players)
            {
                if (playerEntry == null || playerEntry.ConnectionState.CompareTo(EnumClientState.Playing) != 0) continue;

                string playerId = playerEntry.PlayerUID;
                var afkInfo = playerAfkData[playerId];

                BlockPos currentPos = playerEntry.Entity.Pos.AsBlockPos;
                if (!currentPos.Equals(afkInfo.LastKnownPosition))
                {
                    afkInfo.LastActiveTime = currentTime;
                    afkInfo.LastKnownPosition = currentPos;
                    afkInfo.IsAfk = false;
                    continue;
                }
                var afkTime = FloorToThreeZeros(currentTime - afkInfo.LastActiveTime);
                
                if (afkTime == FloorToThreeZeros(afkThresholdMinutes/2 * 60000))
                {
                    sapi.Logger.Notification("Sent AFK warning to player: " + playerEntry.PlayerName + "(" + playerId + ")");
                    sapi.SendMessage(playerEntry, GlobalConstants.GeneralChatGroup, $"You will be kicked in {(afkThresholdMinutes - (afkThresholdMinutes / 2))} minutes", EnumChatType.Notification);
                }

                if (afkTime > FloorToThreeZeros(afkThresholdMinutes * 60000))
                {
                    sapi.Logger.Notification($"Kicked AFK player: {playerEntry.PlayerName} ({playerId}");
                    // For some reason client receives "unknown" message. Server still has proper disconnect message.
                    playerEntry.Disconnect("Autokick for AFK");
                }
            }
        }

        private static int FloorToThreeZeros(float number)
        {
            return (int)(Math.Floor(number / 1000.0) * 1000);
        }
        private TextCommandResult OnAfkModCommand(TextCommandCallingArgs args)
        {
            string subcommand = args[0] as string;
            int? value = args[1] as int?;

            if (string.IsNullOrEmpty(subcommand) || !subcommand.Equals("setTimeout", System.StringComparison.OrdinalIgnoreCase))
            {
                return TextCommandResult.Error("Invalid command, use: /afk setTimeout <minutes>");
            }
            return OnSetTimeoutCommand(args, subcommand, value);
        }

        private TextCommandResult OnSetTimeoutCommand(TextCommandCallingArgs args, string subcommand, int? timeout)
        {
            if (timeout == null || timeout <= 0)
            {
                return TextCommandResult.Error("Invalid timeout value. Please provide a positive number of minutes.");
            }
            afkThresholdMinutes = timeout.Value;
            sapi.BroadcastMessageToAllGroups($"AFK timeout set to {timeout.Value} minutes by {args.Caller.GetName()}.", EnumChatType.Notification);

            return TextCommandResult.Success();
        }
    }

    // Helper class
    public class PlayerAfkInfo
    {
        public BlockPos LastKnownPosition { get; set; }
        public long LastActiveTime { get; set; }
        public bool IsAfk { get; set; } = false;
    }
}
