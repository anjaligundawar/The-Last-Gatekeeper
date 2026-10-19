namespace Gatekeeper
{
    public enum EnemyType
    {
        Grunt = 0,
        Runner = 1,
        Shield = 2,
        Flyer = 3,
    }

    /// <summary>What an enemy attacks with. Only changes how the attack looks and when it lands.</summary>
    public enum EnemyWeapon
    {
        Sword = 0,
        Sphere = 1,
        Bow = 2,
    }

    public enum DamageType
    {
        Physical,
        Arrow,
        Explosive,
        Frost,
    }
}
