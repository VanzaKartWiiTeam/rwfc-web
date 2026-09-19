namespace RetroRewindWebsite.Models.DTOs.Player;

public record InGamePlayerDto(
    string Name,
    string FriendCode,
    int VR,
    int Rank,
    int PrestigeRank,
    string? MiiData
);
