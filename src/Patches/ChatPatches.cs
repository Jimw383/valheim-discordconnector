﻿using HarmonyLib;
using UnityEngine;

namespace DiscordConnector.Patches
{
    internal class ChatPatches
    {

        [HarmonyPatch(typeof(Chat), nameof(Chat.OnNewChatMessage))]
        internal class OnNewChatMessage
        {
            private static void Prefix(ref GameObject go, ref long senderID, ref Vector3 pos, ref Talker.Type type, ref string user, ref string text, ref string senderNetworkUserId)
            {
                if (string.IsNullOrEmpty(user))
                {
                    Plugin.StaticLogger.LogInfo("Ignored shout from invalid user (null reference)");
                }
                if (Plugin.StaticConfig.MutedPlayers.IndexOf(user) >= 0 || Plugin.StaticConfig.MutedPlayersRegex.IsMatch(user))
                {
                    Plugin.StaticLogger.LogInfo($"Ignored shout from user on muted list. User: {user} Shout: {text}.");
                    return;
                }

                ZNetPeer peerInstance = ZNet.instance.GetPeerByPlayerName(user);

                if (peerInstance == null || peerInstance.m_socket == null)
                {
                    // Check if we allow non-player shouts
                    if (Plugin.StaticConfig.AllowNonPlayerShoutLogging)
                    {
                        // Guard against chats that aren't shouts by non-players
                        if (type != Talker.Type.Shout)
                        {
                            Plugin.StaticLogger.LogDebug($"Ignored ping/join/leave from non-player {user}");
                            return;
                        }

                        string nonplayerHostName = "";
                        Plugin.StaticLogger.LogDebug($"Sending shout from '{user}' to discord: '{text}'");

                        // Only if we are sending shouts per the config should we send the shout
                        if (Plugin.StaticConfig.ChatShoutEnabled)
                        {
                            string userCleaned = MessageTransformer.CleanCaretFormatting(user);
                            string message = MessageTransformer.FormatPlayerMessage(Plugin.StaticConfig.ShoutMessage, userCleaned, nonplayerHostName, text);

                            if (message.Contains("%POS%"))
                            {
                                message.Replace("%POS%", "");
                            }

                            DiscordApi.SendMessage(message);
                        }
                        // Exit the function since we sent the message
                        return;
                    }

                    Plugin.StaticLogger.LogInfo($"Ignored shout from {user} because they aren't a real player");
                    return;
                }

                // Get the player's hostname to use for record keeping and logging
                string playerHostName = peerInstance.m_socket.GetHostName();

                switch (type)
                {
                    case Talker.Type.Ping:
                        if (Plugin.StaticConfig.AnnouncePlayerFirstPingEnabled && Plugin.StaticDatabase.CountOfRecordsByName(Records.Categories.Ping, user) == 0)
                        {
                            DiscordApi.SendMessage(
                                MessageTransformer.FormatPlayerMessage(Plugin.StaticConfig.PlayerFirstPingMessage, user, playerHostName)
                            );
                        }
                        if (Plugin.StaticConfig.StatsPingEnabled)
                        {
                            Plugin.StaticDatabase.InsertSimpleStatRecord(Records.Categories.Ping, user, playerHostName, pos);
                        }
                        if (Plugin.StaticConfig.ChatPingEnabled)
                        {
                            string message = MessageTransformer.FormatPlayerMessage(Plugin.StaticConfig.PingMessage, user, playerHostName);
                            if (Plugin.StaticConfig.ChatPingPosEnabled)
                            {
                                if (Plugin.StaticConfig.DiscordEmbedsEnabled || !message.Contains("%POS%"))
                                {
                                    DiscordApi.SendMessage(
                                        message,
                                        pos
                                    );
                                    break;
                                }
                                message = MessageTransformer.FormatPlayerMessage(Plugin.StaticConfig.PingMessage, user, playerHostName, pos);
                            }
                            if (message.Contains("%POS%"))
                            {
                                message.Replace("%POS%", "");
                            }
                            DiscordApi.SendMessage(message);
                        }
                        break;
                    case Talker.Type.Shout:
                        if (text.Equals("I have arrived!"))
                        {
                            if (!Plugin.IsHeadless())
                            {
                                if (Plugin.StaticConfig.AnnouncePlayerFirstJoinEnabled && Plugin.StaticDatabase.CountOfRecordsByName(Records.Categories.Join, user) == 0)
                                {
                                    DiscordApi.SendMessage(
                                        MessageTransformer.FormatPlayerMessage(Plugin.StaticConfig.PlayerFirstJoinMessage, user, playerHostName)
                                    );
                                }
                                if (Plugin.StaticConfig.StatsJoinEnabled)
                                {
                                    Plugin.StaticDatabase.InsertSimpleStatRecord(Records.Categories.Join, user, playerHostName, pos);
                                }
                                if (Plugin.StaticConfig.PlayerJoinMessageEnabled)
                                {
                                    string message = MessageTransformer.FormatPlayerMessage(Plugin.StaticConfig.JoinMessage, user, playerHostName);
                                    if (Plugin.StaticConfig.PlayerJoinPosEnabled)
                                    {
                                        if (Plugin.StaticConfig.DiscordEmbedsEnabled || !message.Contains("%POS%"))
                                        {
                                            DiscordApi.SendMessage(
                                                message,
                                                pos
                                            );
                                            break;
                                        }
                                        message = MessageTransformer.FormatPlayerMessage(Plugin.StaticConfig.JoinMessage, user, playerHostName, pos);
                                    }
                                    if (message.Contains("%POS%"))
                                    {
                                        message.Replace("%POS%", "");
                                    }
                                    DiscordApi.SendMessage(message);
                                }
                            }
                        }
                        else
                        {
                            if (Plugin.StaticConfig.AnnouncePlayerFirstShoutEnabled && Plugin.StaticDatabase.CountOfRecordsByName(Records.Categories.Shout, user) == 0)
                            {
                                DiscordApi.SendMessage(
                                    MessageTransformer.FormatPlayerMessage(Plugin.StaticConfig.PlayerFirstShoutMessage, user, playerHostName, text)
                                );
                            }
                            if (Plugin.StaticConfig.StatsShoutEnabled)
                            {
                                Plugin.StaticDatabase.InsertSimpleStatRecord(Records.Categories.Shout, user, playerHostName, pos);
                            }
                            if (Plugin.StaticConfig.ChatShoutEnabled)
                            {
                                string message = MessageTransformer.FormatPlayerMessage(Plugin.StaticConfig.ShoutMessage, user, playerHostName, text);
                                if (Plugin.StaticConfig.ChatShoutPosEnabled)
                                {
                                    if (Plugin.StaticConfig.DiscordEmbedsEnabled || !message.Contains("%POS%"))
                                    {
                                        DiscordApi.SendMessage(
                                            message,
                                            pos
                                        );
                                        break;
                                    }
                                    message = MessageTransformer.FormatPlayerMessage(Plugin.StaticConfig.ShoutMessage, user, playerHostName, text, pos);
                                }
                                if (message.Contains("%POS%"))
                                {
                                    message.Replace("%POS%", "");
                                }
                                DiscordApi.SendMessage(message);
                            }
                        }
                        break;
                    default:
                        Plugin.StaticLogger.LogDebug(
                            $"Unmatched chat message. [{type}] {user}: {text} at {pos}"
                        );
                        break;
                }

            }
        }
    }
}
