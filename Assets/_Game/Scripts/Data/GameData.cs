[System.Serializable]
public class GameData
{
    #region Basic Stats
    public int lives;
    public int coins;
    public int currentLevelIndex;
    public bool isZombieMode;
    #endregion

    #region Upgrades
    public int gunLevel;
    public int bootLevel;
    public int ammoLevel;
    #endregion

    #region Inventory
    public PowerUpType heldItemType;
    public bool hasHeldItem;
    #endregion

    #region Constructor
    public GameData()
    {
        lives = 3;
        coins = 0;
        currentLevelIndex = 1;

        gunLevel = 0;
        bootLevel = 0;
        ammoLevel = 0;

        hasHeldItem = false;
        heldItemType = PowerUpType.None;
    }
    #endregion
}