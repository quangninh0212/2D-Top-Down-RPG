// Every sound effect the game can ask for. Named rather than passed as clips so
// gameplay code never has to hold an AudioClip reference.
public enum GameSfx
{
    UiClick,
    SwordSwing,
    BowShot,
    StaffShot,
    Projectile,
    PlayerHurt,
    PlayerDeath,
    EnemyHurt,
    EnemyDeath,
    CoinPickup,
    HealthPickup,
    StaminaPickup,
    GateOpen,
    Dash,
    BossAttack,
    Victory,
    Purchase,
    Denied,

    // Added with the defence skills, the forbidden zone and the collision objects.
    Warning,
    ShieldUp,
    ShieldBlock,
    ShieldBreak,
    Stun,
    ChestOpen,
    TrapHit,
    SpeedUp,
    Explosion
}

public enum GameMusic
{
    None,
    Menu,
    Level,
    Boss,
    Victory
}
