// Data definition POCOs mirroring JSON authoring shapes (simplified)
namespace FrontierAges.Sim {
    [System.Serializable] public class ResourceDef { public string id; public string displayName; public string description; }
    [System.Serializable] public class ResourceDefList { public ResourceDef[] resources; }

    [System.Serializable] public class UnitTypeJson { public string id; public int age; public int maxHP; public float moveSpeed; public string attackProfile; }
    [System.Serializable] public class UnitTypeJsonList { public UnitTypeJson[] units; }

    [System.Serializable] public class AttackJson { public string id; public float range; public int cooldownMs; public int damage; }
    [System.Serializable] public class AttackJsonList { public AttackJson[] attacks; }

    [System.Serializable] public class CostJson { public int food; public int wood; public int stone; public int metal; }
    [System.Serializable] public class BuildingJson { public string id; public int age; public int maxHP; public string[] train; public Footprint footprint; public int buildTimeMs; public CostJson cost; public int providesPopulation; }
    [System.Serializable] public class Footprint { public int w; public int h; }
    [System.Serializable] public class BuildingJsonList { public BuildingJson[] buildings; }

    // Registry holds imported data for reference (not yet fully used by simulation)
    public static class DataRegistry {
        public static ResourceDef[] Resources = new ResourceDef[0];
        public static UnitTypeJson[] Units = new UnitTypeJson[0];
    public static AttackJson[] Attacks = new AttackJson[0];
    public static BuildingJson[] Buildings = new BuildingJson[0];
    public static bool HasLoaded => Resources.Length > 0 || Units.Length > 0 || Attacks.Length > 0 || Buildings.Length > 0;
    }
}
