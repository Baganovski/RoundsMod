namespace RWF.GameModes
{
    public class TeamDeathmatchHandler : RWFGameModeHandler<GM_TeamDeathmatch>
    {
        internal const string GameModeName = "Team Deathmatch";
        internal const string GameModeID = "Team Deathmatch";
        public TeamDeathmatchHandler() : base(
            name: GameModeName,
            gameModeId: GameModeID,
            allowTeams: true,
            pointsToWinRound: 2,
            roundsToWinGame: RWFMod.instance != null ? RWFMod.instance.RoundsToWinGame : 5,
            // null values mean RWF's instance values
            playersRequiredToStartGame: null,
            maxPlayers: null,
            maxTeams: null,
            maxClients: null,
            description: "Team Deathmatch. Last team standing wins.",
            videoURL: "https://github.com/Baganovski/RoundsMod/raw/main/Media/TeamDeathmatch.mp4"
            )
        {

        }
    }
}