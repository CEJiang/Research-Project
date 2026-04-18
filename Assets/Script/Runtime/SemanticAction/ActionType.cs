public enum ActionType
{
    PlayerObject,        // 玩家與某個物品互動
    PlayerEnvironment,   // 玩家進入房間、移動區域
    ObjectObject,        // 物件之間的語意行為（鎖 -> 鑰匙）
    ObjectEnvironment,   // 物件與環境之間的語意行為（物品放置在某區域）
}