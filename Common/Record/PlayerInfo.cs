namespace KartLibrary.Record;

public struct PlayerInfo
{
    public string PlayerName { get; set; }

    public string ClubName { get; set; }

    public PlayerEquipment Equipment { get; set; }

    public PlayerInfo()
    {
        PlayerName = "KartRider";
        ClubName = "Default Club";
        Equipment = new PlayerEquipment();
    }
}