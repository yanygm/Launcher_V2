namespace Launcher.Library.Record
{
    public struct PlayerInfo
    {
        public PlayerInfo()
        {

        }

        public string PlayerName { get; set; } = "KartRider";
        public string ClubName { get; set; } = "Default Club";
        public PlayerEquipment Equipment { get; set; } = new PlayerEquipment();//0x32 if version>=11 else 0x2C
    }
}
