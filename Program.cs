using System.Linq.Expressions;
using static System.Threading.Thread;

public class Program {
public enum TileType { Empty, Wall, Portal, HealingPotion, ManaPotion, Gold }
public class Tile(TileType type) {
    public TileType Type { get; set; } = type;
}
public enum DeityEnum { Sacrifice, Enigma, Harvest, End, None }
public static DeityEnum DeityEnumeration = new();
public static List<DeityEnum> DeityList = Enum.GetValues(typeof(DeityEnum)).Cast<DeityEnum>().ToList();
// Deity Dialogue variables
public static Entity player = new(30, 750, ConsoleColor.White, "Player");
public static Deity Sacrifice = new(5, 5, ConsoleColor.DarkRed, "SACRIFICE"); // 42, 1300 Warrior equivalent
public static Deity Enigma = new(30, 750, ConsoleColor.DarkMagenta, "ENIGMA"); // Mage equivalent
public static Deity Harvest = new(45, 800, ConsoleColor.DarkGreen, "HARVEST"); // Archer equivalent
public static Deity End = new(28, 1100, ConsoleColor.Black, "END"); // Assassin equivalent
public static Deity Chaos = new(45, 700, ConsoleColor.White, "CHAOS"); // Hidden class
// Write the battle encounter system with interface that is similar to Undertale but in text-version. Whosoever's SPD is higher, they will go first. The options are: Attack, Inventory, Flee. When the player selects attack, an interface of possible attacks/skills will show, and the same goes for inventory. There should be numbers corresponding to the option to get player input (i.e., [1] Attack, [2] Inventory, etc.). 
// Room global variables
public static double GrowthAmount = 0;
public static Tile[,] Room = new Tile[0, 0];
public static RoomGenerator RoomGen = new();    public static List<Enemy> enemies = [];
public static bool RoomClear = false;
public static Random RNG = new();

public static Dictionary<int, string> Interface = [];

// Combat global variables
public static bool IsOver = false;
public static int Turn = 0;
public class Deity(int tspeed = 35, int tduration = 450, ConsoleColor color = ConsoleColor.White, string Name = "???") {
    public int tspeed = tspeed, tduration = tduration;
    public ConsoleColor color = color;
    public string Name = Name;
    public void Talk(string str) {
        Program.Print(str, tspeed, tduration, color, Name);
    }
}
public class Enemy(int x, int y) : Entity {
    public bool IsDefeated = false;
    public void Initialize() {
        X = x;
        Y = y;
        EXP = NextInt(17+(player.Stage*3), 25+(player.Stage*3));
        Deity = DeityList[NextInt(DeityList.Count-1)];
        DeityName = "THE " + Deity.ToString().ToUpper();
        HP = 6;
        ATK = 6;
        DEF = 6;
        INT = 6;
        SPD = 6;
        LCK = 6;
        LVL = player.Stage;
        PTS = 10+(LVL*2);
        GetDeityStats();
        DistributePTS();
        MaxEXP = 80 + LVL*20;
        MaxHealth = 20 + HP*4;
        Health = MaxHealth;
        Armor = Math.Clamp(Math.Round(DEF*0.02), 0, 0.5);
        TotalKills = NextInt(0, LVL*3);

        string[][] Names = [["Bloodbound Stalker", "Graveborn Revenant", "Painforged Emissary"], 
                            ["Mind Walker", "Twilight Herald", "Dream Specter"], 
                            ["Bleeding Orchardgeist", "Weeping Rosecutter", "Fiery Treant"], 
                            ["Voidborn Wraith", "Oblivion Scourge", "Ancient Desolator"]];
        string[] DeityNames = Enum.GetNames(typeof(DeityEnum));

        
        // Checks the deity of the enemy and assigns a Name for it based on their deity.
        for (int i = 0; i < DeityNames.Length; i++)
            if (Deity.ToString() == DeityNames[i])
                Name = Names[i][NextInt(Names.Length-1)];
    }
    public int SpendPTS(int Attempts = 0, double Chance = 0.5, bool SpendAll = false) {
        int Amount = 0;
        if (!SpendAll) {
            for (int i = 0; i < Attempts; i++)
                if(Chance > RNG.NextDouble() && PTS > 0) {
                    Amount++;
                    PTS--;
                }
            return Amount;
        } else if (PTS > 0) {
            PTS -= Attempts;
            return Attempts;
        }
        return 0;
        
    }
    public void GetDeityStats() {
        switch (Deity) {
            case DeityEnum.Sacrifice:
                HP += 5 + SpendPTS(PTS/2, 0.35);
                DEF += 3;
                GLD -= 50;
                ATK -= 3;
                SPD -= 3;
                UpdateStats();
                break;
            case DeityEnum.Enigma:
                INT += 5 + SpendPTS(PTS/2, 0.35);
                PTS += 3;
                HP -= 3;
                ATK -= 5;
                UpdateStats();
                break;
            case DeityEnum.Harvest:
                LCK += 5 + SpendPTS(PTS/2, 0.35);
                PTS -= 3;
                DEF -= 3;
                SPD -= 3;
                UpdateStats();
                break;
            case DeityEnum.End:
                ATK += 5 + SpendPTS(PTS/2, 0.35);
                SPD += 3;
                GLD -= 50;
                HP -= 3;
                DEF -= 3;
                UpdateStats();
                break;
        }
    }
    public void DistributePTS() {
        while (PTS > 0) {
            double chance = RNG.NextDouble();
            if (chance < 0.18)
                HP++;
            else if (chance < 0.36)
                ATK++;
            else if (chance < 0.52)
                DEF++;
            else if (chance < 0.68)
                INT++;
            else if (chance < 0.84)
                SPD++;
            else
                LCK++;
            PTS--;
        }
    }
    public void Defeat() {
        player.TotalKills++;
        player.EXP += EXP;
        player.EvaluateEXP();
        IsDefeated = true;
    }

    // Method to randomly move the enemy
    public void Move() {
        int[] dx = { -1, 0, 0, 1 };
        int[] dy = { -1, 0, 0, 1 };

        // Attempt to move in a random direction
        for (int attempts = 0; attempts < 4; attempts++) {
            int direction = NextInt(4);
            int newX = X, newY = Y;
            if (RNG.NextDouble() > 0.5)
                newX = X + dx[direction];
            else
                newY = Y + dy[direction];

            // Check if the new position is within bounds and not a wall
            if (newX >= 0 && newX < RoomGen.xSize && newY >= 0 && newY < RoomGen.ySize && Room[newX, newY].Type != TileType.Wall)
            {
                X = newX;
                Y = newY;
                break; // Successfully moved
            }
        }
    }
}

public class Entity {
    public DeityEnum Deity;
    public List<dynamic> inventory;
    // Dialogue variables
    public int tspeed, tduration;
    public ConsoleColor color;
    // Attribute variables
    public string Name, DeityName;
    public dynamic Skill1, Skill2, Skill3, Skill4, Skill5;
    public int Skill1Timer, Skill2Timer, Skill3Timer, Skill4Timer, Skill5Timer;
    public int HP, ATK, DEF, INT, SPD, LCK, GLD, EXP, MaxEXP, LVL, PTS, IntHealth, IntMaxHealth, ATKM;
    public double Health, MaxHealth, DMG, Armor, FinalDMG;
    public dynamic ChosenDeity;
    // Room variables
    public int X, Y, Stage;
    public int TotalKills, SacrificeKills, EnigmaKills, HarvestKills, EndKills;
    public int spawnX = NextInt(1, 20), spawnY = NextInt(1, 26);
    public Entity(int tspeed = 35, int tduration = 450, ConsoleColor color = ConsoleColor.White, string Name = "???") {
        // Dialogue variables
        this.X = spawnX;
        this.Y = spawnY;
        this.tspeed = tspeed;
        this.tduration = tduration;
        this.color = color;
        // Info variables
        this.Name = Name;
        this.Stage = 1;
        this.inventory = [];
        this.DeityName = "None";
        this.Deity = DeityEnum.None;
        this.ChosenDeity = new Deity();
        // Attribute variables
        this.HP = 10;
        this.ATK = 10;
        this.DEF = 10;
        this.INT = 10;
        this.SPD = 10;
        this.LCK = 10;
        this.LVL = 1;
        this.PTS = 0;
        // Stats variables
        this.GLD = 100;
        this.EXP = 0;
        this.MaxEXP = 80 + LVL*20;
        this.Health = 20 + HP*8;
        this.IntHealth = Convert.ToInt32(Health);
        this.MaxHealth = Health;
        this.IntMaxHealth = Convert.ToInt32(MaxHealth);
        this.DMG = ATK;
        this.FinalDMG = DMG;
        this.ATKM = ATK*3/4;
        this.Armor = Math.Clamp(Math.Round(DEF*0.02), 0, 0.5);
        // Skill variables
        this.Skill1 = "";
        this.Skill2 = "";
        this.Skill3 = "";
        this.Skill4 = "";
        this.Skill5 = "";
        this.Skill1Timer = 0;
        this.Skill2Timer = 0;
        this.Skill3Timer = 0;
        this.Skill4Timer = 0;
        this.Skill5Timer = 0;
    }

    // Methods
    public void Talk(string str) {
        Program.Print(str, tspeed, tduration, color, Name);
    }

    public void EvaluateEXP() {
        while (EXP >= MaxEXP) {
            PTS += 2;
            EXP -= MaxEXP;
            LVL++;
            UpdateStats();
        }
    }

    public void Think(string str) {
        Program.Print($"({str})", tspeed, tduration, ConsoleColor.DarkGray);
    }

    public void Narrate(string str, int speed = 3, int duration = 250, ConsoleColor color = ConsoleColor.White) {
        Program.Print($"[{str}]", speed, duration, color);
    }

    public void Move(int dx, int dy) {
        int newX = X + dx;
        int newY = Y + dy;
        if (newX >= 0 && newX < RoomGen.xSize && newY >= 0 && newY < RoomGen.ySize && Room[newX, newY].Type != TileType.Wall) {
            X = newX;
            Y = newY;
        }
    }

    public Dictionary<int, string> GetInventory() {
        Dictionary<int, string> inventoryDict = [];
        inventoryDict.Add(10, "   --------------------------------------------");
        for (var i = 0; i < 9; i++)
            if (i < inventory.Count)
                inventoryDict.Add(11+i, string.Format("   {0}. {1}", i+1, inventory[i]));    
            else
                inventoryDict.Add(11+i, $"   {1+i}. Empty");
        inventoryDict.Add(20, "   --------------------------------------------");
        return inventoryDict;
    }

    public Dictionary<int, string> GetStats() {
        Dictionary<int, string> StatsDict = [];
        StatsDict.Add(1, "   --------------------------------------------");
        List<string> strings = [string.Format("   Level: {0, -13} Gold: {1}", LVL, GLD), string.Format("   Health: {0}/{1, -8} EXP: {2}/{3}", Health, MaxHealth, EXP, MaxEXP), string.Format("   Armor: {0, -13} Deity: {1}", Armor*100+"%", DeityName), "   --------------------------------------------", string.Format("   HP: {0, -16} DEF: {1}", HP, DEF), string.Format("   ATK: {0, -15} INT: {1}", ATK, INT), string.Format("   SPD: {0, -15} LCK: {1}", SPD, LCK), string.Format("   Kills: {0, -13} Room: {1}", TotalKills, Stage)];

        for (var i = 0; i < strings.Count; i++)
            StatsDict.Add(2+i, strings[i]);

        return StatsDict;
    }

    public void WriteStats() {
        Console.WriteLine($"Name: {Name}");
        List<string> strings = ["--------------------------------------------", string.Format("Level: {0, -13} Gold: {1}", LVL, GLD), string.Format("Health: {0}/{1, -8} EXP: {2}/{3}", Health, MaxHealth, EXP, MaxEXP), string.Format("Armor: {0, -13}% Deity: {1}", Armor*100+"%", DeityName), "--------------------------------------------", string.Format("HP: {0, -16} DEF: {1}", HP, DEF), string.Format("ATK: {0, -15} INT: {1}", ATK, INT), string.Format("SPD: {0, -15} LCK: {1}", SPD, LCK)];
        for (var i = 0; i < strings.Count; i++)
            Console.WriteLine(strings[i]);
    }

    public void SetDeity(DeityEnum deity) {
        if (deity != DeityEnum.None) {
            Deity = deity;
            DeityName = "THE " + deity.ToString().ToUpper();
            DeityList.Remove(deity);
        }
    }

    public void UpdateStats(bool updateHealth = false) {
        MaxEXP = 80 + LVL*20;
        MaxHealth = 20 + HP*8;
        Armor = Math.Clamp(Math.Round(DEF*0.02), 0, 0.5);
        ATKM = ATK*3/4;
        if (updateHealth)
            Health = MaxHealth;
    }

    public bool ChooseDeity(DeityEnum chosen) {
    int choice = GetChoice(5, 100, "Enter the door.", "Go back.");
    if (choice == 1) {
        SetDeity(chosen);
        return false;
    } else
        return true;
    }

    // Returns a random number from the range of the attack
    public double GetDMG(dynamic DMG, dynamic enemy, bool PierceArmor = false, double MinMultiplier = 0.5, double MaxMultiplier = 1.5) {
        int MinDMG = Convert.ToInt32(DMG*MinMultiplier);
        int MaxDMG = Convert.ToInt32(DMG*MaxMultiplier);
        FinalDMG = NextInt(MinDMG, MaxDMG);
        if (!PierceArmor)
            FinalDMG -= FinalDMG*enemy.Armor;
        
        if (FinalDMG >= MaxDMG*0.7)
            Narrate("Critical Hit!");
        return FinalDMG;
    }

    // Checks if the DMG is evaded or not
    public bool CheckEvade(double DMG, dynamic enemy) {
        double chance = RNG.NextDouble();
        if (1.0-LCK*0.02 > chance) {
            enemy.Health = Math.Clamp(enemy.Health - DMG, 0, enemy.MaxHealth);
            return true;
        } else {
            Narrate($"{enemy.Name} evaded the attack!");
            return false;
        }
    }
    // Sacrifice Skills

    // Decreases Health slightly but increases ATK significantly for the attack.
    public void BloodStrike(dynamic enemy) {
            Health -= MaxHealth*0.05 - MaxHealth*0.05*Armor;
            ATK += 5;
            UpdateStats();
            Narrate($"{Name} used Blood Strike!");
            DMG = GetDMG(ATK*2, enemy);
            if (CheckEvade(DMG, enemy)) 
                Narrate($"{enemy.Name} got hit for {DMG} DMG in exchange of {Name}'s {MaxHealth*0.05} health!");
            ATK -= 5;
            UpdateStats();
            
    }
    // Deals a percentage of the user's missing health to the enemy.
    public void LifeDrain(dynamic enemy) {
        DMG = GetDMG((enemy.MaxHealth - enemy.Health)*0.25, enemy);
        Narrate($"{Name} used Life Drain!");
        if (CheckEvade(DMG, enemy)) 
            Narrate($"{enemy.Name} got hit for {DMG} DMG based on 25% of their missing health!");
    }
    // Reduces Health but increases ATK and SPD for several turns.
    public void SacrificialPower(dynamic enemy) {
        Narrate($"{Name} used Sacrifical Power!");
        if (Skill3Timer == 0) {
            Health -= MaxHealth*0.12 - MaxHealth*0.12*Armor;
            ATK += 3;
            SPD += 3;
            Skill3Timer += 2;
            Narrate("Gained +3 ATK and +3 SPD for two turns!");
            UpdateStats();
        } else
            Narrate($"Skill failed! This skill is already in play!");
    }
    // Lowers the enemy's DEF and Armor for several turns.
    public void WeakenResolve(dynamic enemy) {
        Narrate($"{Name} used Weaken Resolve!");
        if (Skill4Timer == 0) {
            enemy.DEF -= 6;
            enemy.UpdateStats();
            Skill4Timer = 2;
            Narrate($"{enemy.Name} gained -6 DEF for two turns!");
        } else
            Narrate($"Skill failed! This skill is already in play!");
    }
    // Significantly reduces health but deals a significant percentage of it to the enemy.
    public void UltimateSacrifice(dynamic enemy) {
        Narrate($"{Name} used Ultimate Sacrifice!");
        DMG = GetDMG(enemy.MaxHealth*0.45, enemy);
        Health -= Health*0.25 - DEF*0.5;
        if (CheckEvade(DMG, enemy)) 
            Narrate($"{enemy.Name} got hit {DMG} DMG for 45% of their max health in exchange of {Health*0.25 - DEF*0.5} of {Name}'s health!");
    }
    // Enigma Skills
    // Tinamad na ako
    public void SoulTrack(dynamic enemy) {
        DMG = INT*1.5;
        Narrate($"{Name} used Soul Track!");
        if (CheckEvade(GetDMG(DMG, enemy), enemy))
            Narrate($"{enemy.Name} got hit for {DMG} DMG ignoring their armor!");
    }
    public void Shadowflame(dynamic enemy) {
        DMG = INT*0.7;
        Narrate($"{Name} used Shadowflame!");
        int times = NextInt(1, 3);
        for (var i = 0; i <= times; i++) {
            FinalDMG = GetDMG(DMG, enemy);
            if (CheckEvade(FinalDMG, enemy)) {
                Narrate($"{enemy.Name} got hit for {FinalDMG} DMG!");
            }
        }
        Narrate($"Shadowflame hit {times} times!");
    }
    public void ManaVeil() {
        Narrate($"{Name} used Mana Veil!");
        if (Skill3Timer == 0) {
            DEF += 3;
            SPD += 3;
            UpdateStats();
            Narrate($"Gained +3 DEF for two turns!");
            Skill3Timer = 2;
        } else
            Narrate($"Skill failed! This skill is already in play!");
    }
    public void ConjureIllusions(dynamic enemy) {
        Narrate($"{Name} used Conjure Illusions!");
        if (Skill4Timer == 0) {
            enemy.ATK -= 3;
            enemy.SPD -= 3;
            Skill4Timer = 2;
            enemy.UpdateStats();
            Narrate($"{enemy.Name} gained -3 ATK and -3 SPD for two turns!");
        } else
            Narrate($"Skill failed! This skill is already in play!");
    }
    public void DimensionalRift(dynamic enemy) {
        Narrate($"{Name} used Dimensional Rift!");
        SPD += 5;
        DMG = GetDMG(INT*3, enemy);
        Skill5Timer = 2;
        
        if (CheckEvade(DMG, enemy)) 
            Narrate($"{enemy.Name} got hit for {DMG} DMG and {Name} gained +5 SPD for two turns!");
    }
    // Harvest Skills
    public void ThornedWrath(dynamic enemy) {
         Narrate($"{Name} used Thorned Wrath!");
         DMG = GetDMG(ATK*0.4+LCK*0.5, enemy);
        if (CheckEvade(DMG, enemy)) 
            Narrate($"{enemy.Name} got hit for {DMG} DMG!");

        double chance = RNG.NextDouble();
        if (LCK*0.04 > chance) {
            double HealedAmount = Math.Clamp(Health + DMG*0.5, 0, MaxHealth);
            Health = HealedAmount;
            Narrate($"{Name} gained {HealedAmount} health!");
        }
    }
    public void LuckyPunch(dynamic enemy) {
        Narrate($"{Name} used Lucky Punch!");
        DMG = GetDMG(Convert.ToInt32(ATK*1.5)+NextInt(LCK*2), enemy);
        if (CheckEvade(DMG, enemy)) 
            Narrate($"{enemy.Name} got hit for {DMG} DMG based on luck!");
    }
    public void Growth() {
        Narrate($"{Name} used Growth!");
        if (Skill3Timer == 0) {
            double HealedAmount = MaxHealth*0.2; 
            GrowthAmount = HealedAmount;
            MaxHealth += HealedAmount;
            Health += HealedAmount;
            Skill3Timer = 2;
            Narrate($"Gained {HealedAmount} max health for two turns!");
        } else
            Narrate($"Skill failed! This skill is already in play!");
    }
    public void Wither(dynamic enemy) {
        Narrate($"{Name} used Wither!");
        if (Skill4Timer == 0) {
            enemy.SPD -= 3;
            enemy.INT -= 3;
            Skill4Timer = 2;
            Narrate($"{enemy.Name} gained -3 SPD and -3 INT!");
        } else
            Narrate($"Skill failed! This skill is already in play!");
    }
    public void RootedRampage(dynamic enemy) {
        int times = NextInt(3, 6);
        Narrate($"{Name} used Rooted Rampage!");
        for (var i = 0; i < times; i++) {
            DMG = GetDMG(LCK*1.5, enemy);
            if (CheckEvade(DMG, enemy))
                Narrate($"{enemy.Name} got hit for {DMG} DMG based on luck!");
        }
        Narrate($"Rooted Rampage hit {times} times!");
    }

    // End Skills
    public void SoulBleed(dynamic enemy) {
        Narrate($"{Name} used Soul Bleed!");
        DMG = GetDMG(ATK*0.5, enemy, true);
        if (CheckEvade(DMG, enemy)) {
            Narrate($"{enemy.Name} got hit for {DMG} DMG ignoring armor!");
            for (int i = 0; i < 3; i++) {
                double Bleed = GetDMG(enemy.MaxHealth*0.05, enemy, true);
                if (CheckEvade(Bleed, enemy)) 
                    Narrate($"{enemy.Name} bled for {Bleed} DMG ignoring armor!");
        }
        }
    }
    public void VoidSlash(dynamic enemy) {
        Narrate($"{Name} used Void Slash!");
        int times = NextInt(1, 4);
        for (int i = 0; i < times; i++)
        {
            DMG = GetDMG(ATK*0.8, enemy);
            if (CheckEvade(DMG, enemy)) 
                Narrate($"{enemy.Name} got hit for {DMG} DMG!");
        }
        Narrate($"Void Slash hit {times} times!");
    }
    public void ShroudedMist() {
        Narrate($"{Name} used Shrouded Mist!");
        if (Skill3Timer == 0) {
            SPD += 3;
            LCK += 3;
            Skill3Timer = 2;
            Narrate($"Gained +3 SPD and +2 LCK!");
        } else
            Narrate($"Skill failed! This skill is already in play!");
    }

    public void StealStrength(dynamic enemy) {
        Narrate($"{Name} used Steal Strength!");
        if (Skill4Timer == 0) {
            enemy.ATK -= 4;
            ATK += 4;
            Narrate($"Gained +4 ATK and the enemy gained -4 ATK for two turns!");
        } else
            Narrate($"Skill failed! This skill is already in play!");
    }

    public void Annhilation(dynamic enemy) {
        Narrate($"{Name} used Annhilation!");
        DMG = GetDMG(ATK*1.3*(TotalKills+1), enemy);
        if (CheckEvade(DMG, enemy))
            Narrate($"{enemy.Name} got hit for {DMG} DMG based on {Name}'s kills!");
    }
    
}



public static void WandererRoute() {
    // Sacrifice.Name = "???";
    // player.Think("The door slammed shut as soon as I entered.");
    // player.Think("I feel an ominous figure watching me.");
    // Sleep(300);
    // player.Think("I hear a boisterous laugh in the distance.");
    // Sleep(300);
    // player.Think("It's getting closer...");
    // Sleep(300);
    // Sacrifice.Talk("BWAHA..!");
    // Sleep(300);
    // Sacrifice.Talk("BWAHAHA!");
    // player.Think("...");
    // Sleep(400);
    // Sacrifice.Talk("BWAHAHAHAHAHA!");
    // player.Think("A monstrous horned-figure wearing devilish armor approached...");
    // Sacrifice.Name = "THE SACRIFICE";
    // Sacrifice.Talk("BLEED FOR YOUR MASTER, FOOL!");
    // player.Talk("...What are you?");
    // Sacrifice.Talk("I AM THE SACRIFICE, MASTER OF BLOOD AND BLADE, DeityEnum OF THE ENDLESS FRAY!");
    // Sacrifice.Talk("MY BLOOD SHALL FEED YOUR HUNGER FOR GLORY AND DOMINATION!");
    // Sacrifice.Talk("DRINK MY BURNING BLOOD, SHALL YOU WISH TO DEFY DEATH HERSELF!");
    // Console.WriteLine();
    // player.Narrate("You chose The Sacrifice as your Deity.");
    // player.Narrate("Experience the worst to become the best.");
    // player.Narrate("Effects: ++ HP, + DEF, - GLD, - ATK, - SPD");
    // Sleep(500);
    // player.Narrate("You have angered the other deities.");
    // player.Narrate("They are coming for your soul.");
    // Sleep(1500);
    player.HP += 5;
    player.DEF += 3;
    player.GLD -= 50;
    player.ATK -= 3;
    player.SPD -= 3;
    player.inventory.Add("Sacrificial Dagger");
    player.UpdateStats(true);
    Console.Clear();
    RoomGen = new();
    RoomGen.InitializeRoom();
    RoomGen.DisplayRoom();
}


public static void Print(string str, int speed = 1, int duration = 5, ConsoleColor color = ConsoleColor.White, string Name = "") {
    Console.ForegroundColor = color;

    if (!string.IsNullOrEmpty(Name))
        str = str.Insert(0, Name + ": ");

    foreach (char c in str) {
        Console.Write(c);
        Sleep(speed); 
    }
    Console.WriteLine();
    Sleep(duration);
    
    Console.ForegroundColor = ConsoleColor.White;
}

public class RoomGenerator {
    public int xSize, ySize;
    public RoomGenerator() {
        // this.xSize = NextInt(25, 33);
        // this.ySize = NextInt(33, 55);
        this.xSize = NextInt(25, 35);
        this.ySize = NextInt(25, 40);
    }
        public void InitializeRoom() {
            RoomClear = false;
            Room = new Tile[xSize, ySize];
            // Set all tiles as empty tiles
            for (int x = 0; x < xSize; x++) {
                for (int y = 0; y < ySize; y++) {
                    Room[x, y] = new Tile(TileType.Empty);
                }
            }
            
            // Set boundaries as walls
            for (int x = 0; x < xSize; x++) {
                Room[x, 0] = new Tile(TileType.Wall);
                Room[x, ySize - 1] = new Tile(TileType.Wall);
            }

            for (int y = 0; y < ySize; y++) {
                Room[0, y] = new Tile(TileType.Wall);
                Room[xSize - 1, y] = new Tile(TileType.Wall);
            }
            
            // Initializes enemies and walls that scale with inner room area
            InitializeWalls((xSize-2)*(ySize-2)/6);
            InitializeEnemies(15+(xSize-2)*(ySize-2)/70); 
            InitializeItems(NextInt(1, 5), NextInt(5, 10));
            // Randomizes and teleports player to random spawnpoint.
            player.spawnX = NextInt(1, 20);
            player.spawnY = NextInt(1, 26);
            player.X = player.spawnX;
            player.Y = player.spawnY;
    }

    public void InitializeEnemies(int maxEnemies) {
        enemies.Clear();
        while (enemies.Count < maxEnemies) {
            int x = NextInt(1, xSize-1);
            int y = NextInt(1, ySize-1);
            if (Room[x, y].Type != TileType.Wall) {
                enemies.Add(new Enemy(x, y));
                enemies[^1].Initialize();
            }
        }
    }
    public void InitializeItems(int HealingPotions = 0, int Golds = 0) {
        int CurrentHealingPotions = 0;
        int CurrentGolds = 0;

        while (CurrentHealingPotions != HealingPotions || CurrentGolds != Golds) {
            int x = NextInt(1, xSize - 1);
            int y = NextInt(1, ySize - 1);
            
            if (Room[x, y].Type == TileType.Empty && CurrentHealingPotions != HealingPotions) {
                Room[x, y] = new Tile(TileType.HealingPotion);
                CurrentHealingPotions++;
            }
            else if (Room[x, y].Type == TileType.Empty && CurrentGolds != Golds) {
                Room[x, y] = new Tile(TileType.Gold);
                CurrentGolds++;
            }
        }

    }

    private void InitializeWalls(int numOfWalls) {
        for (int i = 0; i < numOfWalls; i++) {
            int x = NextInt(1, xSize - 1);
            int y = NextInt(1, ySize - 1);

            // if ((x == xSize / 2 && y >= ySize / 2 - 1 && y <= ySize / 2 + 1) || (y == ySize / 2 && x >= xSize / 2 - 1 && x <= xSize / 2 + 1)) {
            //     continue; 
            // }

            Room[x, y] = new Tile(TileType.Wall);

            if (RNG.NextDouble() >= 0.5) {
                int length = NextInt(1, 4);
                for (int j = 0; j < length; j++) {
                    int nx = x + j < xSize ? x + j : x; // Ensure within bounds 
                    Room[nx, y] = new Tile(TileType.Wall);
                }
            }
            else {
                int length = NextInt(1, 4); 
                for (int l = 0; l < length; l++) {
                    int ny = y + l < ySize ? y + l : y; // Ensure within bounds
                    Room[x, ny] = new Tile(TileType.Wall);
                }
            }
        }
    }

    // Main method for displaying Rooms
    public void DisplayRoom() {
        bool flag = true;
        while (flag) {
            Console.Clear();
            // Removes defeated enemies
            for (int i = 0; i < enemies.Count; i++) {
                if (enemies[i].IsDefeated)
                    enemies.RemoveAt(i);
            }

            PrintRoom();

            flag = ProcessInput(); // Asks for input and returns false if input is q

            // 80% chance for enemies to move randomly.
            if (RNG.NextDouble() < 0.8) {
                foreach (Enemy enemy in enemies)
                    if (!enemy.IsDefeated)
                        enemy.Move();
            }
            
            // Checks if player's tile is a portal
            switch (Room[player.X, player.Y].Type) {
                case TileType.Portal:
                    UsePortal();
                    break;
                case TileType.HealingPotion:
                    int HealAmount = NextInt(10, Convert.ToInt32(player.IntMaxHealth/5)); // Determine the healing amount
                    if (player.Health + HealAmount <= player.MaxHealth)
                        player.Health += HealAmount;
                    else
                        HealAmount = 0;
                    Console.WriteLine();
                    player.Narrate($"You found a Healing Potion! Restored {HealAmount} health.");
                    Room[player.X, player.Y] = new Tile(TileType.Empty);
                    break;
                case TileType.Gold:
                    int GoldAmount = NextInt(5, Convert.ToInt32(player.LCK*5/2)-15);
                    player.GLD += GoldAmount;
                    Console.WriteLine();
                    player.Narrate($"You found Gold! Gained {GoldAmount} gold.");
                    Room[player.X, player.Y] = new Tile(TileType.Empty);
                    break;
            }

            // Checks if player encounters enemy
            foreach (Enemy enemy in enemies)
                if (player.X == enemy.X && player.Y == enemy.Y) 
                    Encounter(player, enemy);
        }
    }
    public void UsePortal() {
        Console.Clear();
        player.Narrate("You entered the portal.");
        for (var i = 0; i < 3; i++) {
            Console.Write(". ");
            Sleep(450);
        }

        xSize = NextInt(25, 35);
        ySize = NextInt(25, 40);
        player.Stage++;
        enemies.Clear();
        InitializeRoom();
        DisplayRoom();
    }
    
    public static void Encounter(dynamic player, dynamic enemy) {
        Console.Clear();
        player.Narrate($"A wild {enemy.Name} appeared!", 5, 600);
        Console.Clear();
        PrintEnemy(enemy);
        IsOver = false;
        Turn = 0;
        do {
            Turn++;
            if (player.SPD >= enemy.SPD) {
                Print($"Turn {Turn}: The player goes first this round!");
                Sleep(450);
                PlayerAction(enemy);
                EnemyAction(enemy);
                Console.Clear();
            } else {
                Print($"Turn {Turn}: The enemy goes first this round!");
                Sleep(450);
                EnemyAction(enemy);
                PlayerAction(enemy);
            }
            CheckHealth(enemy);
        } while (!IsOver);

        

        // Checks if player has killed 5 enemies in the room and spawns a portal if so
        if (player.TotalKills % 5 == 0 && !RoomClear) {
            Room[player.spawnX, player.spawnY] = new Tile(TileType.Portal);
            RoomClear = true;
        }
    }

    public static void CheckHealth(Enemy enemy) {
        if (enemy.Health <= 0) {
            enemy.Defeat();
            IsOver = true;
        } else if (player.Health <= 0) {
            Console.WriteLine("Game Over.");
            Environment.Exit(0);
        } 
    }
    public static void Divider() {
        Console.WriteLine("--------------------------------------------");
    }
    public static void EnemyAction(Enemy enemy) {
        CheckHealth(enemy);
        if(!IsOver) {
            Divider();
            enemy.WriteStats();
            Console.WriteLine();
            Divider();
            double ChosenSkill = RNG.NextDouble();
            switch (enemy.Deity) {
                case DeityEnum.Sacrifice:
                    SacrificeSkills(ChosenSkill, enemy);
                    break;
                case DeityEnum.Enigma:
                    EnigmaSkills(ChosenSkill, enemy);
                    break;
                case DeityEnum.Harvest:
                    HarvestSkills(ChosenSkill, enemy);
                    break;
                case DeityEnum.End:
                    EndSkills(ChosenSkill, enemy);
                    break;
            }
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
            Console.Clear();
        }
    }
    public static void SacrificeSkills(double ChosenSkill, Enemy enemy) {
        if (ChosenSkill < 0.3)
            enemy.BloodStrike(player);
        else if (ChosenSkill < 0.6)
            enemy.LifeDrain(player);
        else if (ChosenSkill < 0.75)
            enemy.SacrificialPower(player);
        else if (ChosenSkill < 0.9)
            enemy.WeakenResolve(player);
        else
            enemy.UltimateSacrifice(player);
    }
    public static void EnigmaSkills(double ChosenSkill, Enemy enemy) {
        if (ChosenSkill < 0.3)
            enemy.SoulTrack(player);
        else if (ChosenSkill < 0.6)
            enemy.Shadowflame(player);
        else if (ChosenSkill < 0.75)
            enemy.ManaVeil();
        else if (ChosenSkill < 0.9)
            enemy.ConjureIllusions(player);
        else
            enemy.DimensionalRift(player);
    }
    public static void HarvestSkills(double ChosenSkill, Enemy enemy) {
        if (ChosenSkill < 0.3)
            enemy.ThornedWrath(player);
        else if (ChosenSkill < 0.6)
            enemy.LuckyPunch(player);
        else if (ChosenSkill < 0.75)
            enemy.Growth();
        else if (ChosenSkill < 0.9)
            enemy.Wither(player);
        else
            enemy.RootedRampage(player);
    }
    
    public static void EndSkills(double ChosenSkill, Enemy enemy) {
        if (ChosenSkill < 0.3)
            enemy.SoulBleed(player);
        else if (ChosenSkill < 0.6)
            enemy.VoidSlash(player);
        else if (ChosenSkill < 0.75)
            enemy.ShroudedMist();
        else if (ChosenSkill < 0.9)
            enemy.StealStrength(player);
        else
            enemy.Annhilation(player);
    }


    public static void PlayerAction(Enemy enemy) {
        CheckHealth(enemy);
        bool StayTurn = false;
        do {
        if (!IsOver) {
            StayTurn = false;
            foreach (KeyValuePair<dynamic, dynamic> kvp in new Dictionary<dynamic, dynamic>(){{player, enemy}, {enemy, player}}) {
                if (kvp.Key.Skill3Timer != 0 || kvp.Key.Skill4Timer != 0)
                    kvp.Key.Skill3Timer--;
                else {
                    switch(kvp.Key.Deity) {
                        case DeityEnum.Sacrifice:
                            if (kvp.Key.Skill3Timer != 0) {
                                kvp.Key.ATK -= 3;
                                kvp.Key.SPD -= 3;
                            } else if (kvp.Key.Skill4Timer != 0) {
                                kvp.Value.DEF += 6;
                            }
                            break;
                        case DeityEnum.Enigma:
                            if (kvp.Key.Skill3Timer != 0) {    
                                kvp.Key.DEF -= 3;
                                kvp.Key.SPD -= 3;
                            } else if (kvp.Key.Skill4Timer != 0) {
                                kvp.Value.ATK += 3;
                                kvp.Value.SPD += 3;
                            }
                            break;
                        case DeityEnum.Harvest:
                        if (kvp.Key.Skill3Timer != 0) {
                            kvp.Key.MaxHealth -= GrowthAmount;
                            kvp.Key.Health -= GrowthAmount;
                        } else if (kvp.Key.Skill4Timer != 0) {
                            kvp.Value.SPD += 3;
                            kvp.Value.INT += 3;
                        }
                            break;
                        case DeityEnum.End:
                            if (kvp.Key.Skill1Timer !=0) {
                                kvp.Key.SPD -= 3;
                                kvp.Key.LCK -= 3;
                            } else if (kvp.Key.Skill4Timer != 0) {
                                kvp.Value.ATK += 4;
                                kvp.Key.ATK -= 4;
                            }
                            break;
                    }
                }
            }
            }
        
            Divider();
            player.WriteStats();
            Console.WriteLine();
            Divider();
            switch (GetChoice(0, 0, "Attack", "Inventory", "Show Enemy", "Flee")) {
                case 1:
                    Console.Clear();
                    Attack(enemy);
                    break;
                case 2:
                    Console.Clear();
                    BattleInventory();
                    break;
                case 3:
                    PrintEnemy(enemy);
                    StayTurn = true;
                    break;
                case 4:
                    Console.Clear();
                    Flee(enemy);
                    IsOver = true;
                    break;
            }
            Console.Clear();
        } while(StayTurn);
    }

    public static void Attack(Enemy enemy) {
        int Choice, maxChoices;
        bool ValidChoice;
        do {
            Console.Clear();
            List<string> AttackList = [];
            player.UpdateStats();
            switch(player.Deity) {
                case DeityEnum.Sacrifice:
                    AttackList = [$"Blood Strike - Damage: {(player.ATK*2*0.7).ToString("0")}-{(player.ATK*2*1.3).ToString("0")} DMG, Cost: 5% Max Health", $"Life Drain - Damage: {((enemy.MaxHealth - enemy.Health)*0.25*2*0.7).ToString("0")}-{((enemy.MaxHealth - enemy.Health)*0.25*2*1.3).ToString("0")} DMG (Enemy Missing Health)", "Sacrificial Power - Effect: +3 ATK & +3 SPD", "Weaken Resolve - Effect: Enemy -6 DEF", $"Ultimate Sacrifice - Damage: {(enemy.MaxHealth*0.45*0.7).ToString("0")}-{(enemy.MaxHealth*0.45*1.3).ToString("0")} DMG, Cost: 25% Current Health"];
                    break;
                case DeityEnum.Enigma:
                    AttackList = [$"Soul Track - Damage: {(player.INT*1.5*0.7).ToString("0")}-{(player.INT*1.5*1.3).ToString("0")} DMG", $"Shadowflame - Damage: {(player.INT*0.7*0.7).ToString("0")}-{(player.INT*0.7*1.3).ToString("0")} DMG 1-3 times", $"Mana Veil - Effect: +3 DEF & +3 SPD", $"Conjure Illusions - Effect: Enemy -3 ATK & -3 SPD", $"Dimensional Rift - Damage: {(player.INT*3*0.7).ToString("0")}-{(player.INT*3*1.3).ToString("0")} DMG, Effect: +5 SPD"];
                    break;
                case DeityEnum.Harvest:
                    AttackList = [$"Thorned Wrath - Damage: {(player.ATK+player.LCK*0.5*0.7).ToString("0")}-{(player.ATK+player.LCK*0.5*1.3).ToString("0")} DMG, Effect: {player.LCK*0.04*100}% chance to heal {Math.Clamp(player.Health + player.DMG*0.5, 0, player.MaxHealth).ToString("0")} Health", $"Lucky Punch - Damage: {(player.ATK*1.5*0.7).ToString("0")}-{(player.ATK*1.5+player.LCK*2*1.3).ToString("0")} DMG", $"Growth - Effect: Gain {(player.MaxHealth*0.2).ToString("0")} health for two turns", $"Wither - Effect: Enemy -3 SPD & -3 INT", $"Rooted Rampage - Damage: {(player.LCK*1.5).ToString("0")} DMG 3-6 times"];
                    break;
                case DeityEnum.End:
                    AttackList = [$"Sould Bleed - Damage: {player.ATK*1.2*0.7}-{player.ATK*1.2*1.3} DMG & {enemy.MaxHealth*0.05*0.7}-{enemy.MaxHealth*0.05*1.3} DMG 0-3 times", $"Void Slash - Damage: {player.ATK*1.2} DMG 1-4 times", $"Shrouded Mist - Effect: +3 SPD & +3 LCK", $"Steal Strength - Effect: +4 ATK & Enemy -4 ATK", $"Annhilation - Damage: {player.ATKM*(player.TotalKills+1)} DMG"];
                    break;
            }
            AttackList.Add("Cancel");
            maxChoices = AttackList.Count;
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine();
            for (int i = 0; i < AttackList.Count; i++)
                Console.WriteLine(string.Format("[{0}] {1}", i+1, AttackList[i].ToString()));
            
            Console.Write("> ");
            ValidChoice = int.TryParse( Console.ReadKey().KeyChar + "", out Choice);

            if (!ValidChoice || Choice < 1 || Choice > maxChoices) {
                Console.Clear();
            }
        } while (!ValidChoice || Choice < 1 || Choice > maxChoices);

        Console.Clear();
        switch(player.Deity) {
            case DeityEnum.Sacrifice:
                switch (Choice) {
                    case 1:
                        player.BloodStrike(enemy);
                        break;
                    case 2:
                        player.LifeDrain(enemy);
                        break;
                    case 3:
                        player.SacrificialPower(enemy);
                        break;
                    case 4:
                        player.WeakenResolve(enemy);
                        break;
                    case 5:
                        player.UltimateSacrifice(enemy);
                        break;
                    case 6:
                        break;
                    case 7:
                        break;
                }
                break;
            case DeityEnum.Enigma:
                switch (Choice) {
                    case 1:
                        player.SoulTrack(enemy);
                        break;
                    case 2:
                        player.Shadowflame(enemy);
                        break;
                    case 3:
                        player.ManaVeil();
                        break;
                    case 4:
                        player.ConjureIllusions(enemy);
                        break;
                    case 5:
                        player.DimensionalRift(enemy);
                        break;
                    case 6:
                        break;
                }
                break;
            case DeityEnum.Harvest:
                switch (Choice) {
                    case 1:
                        player.ThornedWrath(enemy);
                        break;
                    case 2:
                        player.LuckyPunch(enemy);
                        break;
                    case 3:
                        player.Growth();
                        break;
                    case 4:
                        player.Wither(enemy);
                        break;
                    case 5:
                        player.RootedRampage(enemy);
                        break;
                    case 6:
                        break;
                }
                break;
            case DeityEnum.End:
                switch (Choice) {
                    case 1:
                        player.SoulBleed(enemy);
                        break;
                    case 2:
                        player.VoidSlash(enemy);
                        break;
                    case 3:
                        player.ShroudedMist();
                        break;
                    case 4:
                        player.StealStrength(enemy);
                        break;
                    case 5:
                        player.Annhilation(enemy);
                        break;
                    case 6:
                        break;
                }
                break;
        }
        Console.WriteLine("Press any key to continue...");
        Console.ReadKey();
        Console.Clear();

    }
    public static void BattleInventory() {

    }
    public static void Flee(Enemy enemy) {
        player.Skill3Timer = player.Skill4Timer = enemy.Skill3Timer = enemy.Skill4Timer = 0;
    }

    public static void PrintEnemy(Enemy enemy) {
        switch (enemy.Name) {
            case "Mind Walker":
                Console.WriteLine(@"
████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓███████████████
████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓█████████████
████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓███████████████
████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒░▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓███████████████████
████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓█████████████████████
█████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓██████████████████████
█████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▓▓▓▓▓▓▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓███████████████████████
█████▓█▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▓▓▓▓▓▓▓▓▓▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓██████████████████████
███████▓█████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▓▓██▓▓▓▓▓▓▓▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓██████████████████████
███████▓██████▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▓▓███▓█▓▓▓▓▓▓▓▓░░░░░░░░░░░░░░░░░░░░░░░░░░░▒░░▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓██████████████████████
███████████████▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▓██▓▓▒▒▒▒▓▓▓▓███░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▓▒▓▓▓▓▓▓▓▓▓▓████████████████████████
████████████████▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▓██▓▒▒░░░░▒▒▓▓▓███▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▒▒▓▓▓▓▓██████████████████████████
███████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▓███▓▒░░░▒░░░▒▓▓████▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▒▓▓▓▓▓██████████████████████▓▓▓▓
██████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓███▓▒▒░░▒▒▒░░▒▓█████▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▓▓▓▓▓██████████████████████▓▓▓▓▓
████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░▒▓█████▓▓▓▒▒░▒▒▓▓▓██████▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▓▓▓▓▓▓▓▓▓██████████████████▓▓▓▓▓▓▓
███████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░▓██████▓▓▓▒▒▒▒▓▓▓▓▓██████▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓████████████████▓▓▓▓▓▓▓
██████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░▒████████▓▓▓▓▒▒▓▓▓█████████░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓████████████████▓▓▓▓▓▓▓
██████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░▓█████████▓▓▒▒░▒▒███████████░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓███████████████▓▓▓▓▓▓▓
██████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░▓███████████▓▒▒▓███████████▓▒░░░░░▒░░░░░░▒░░░░░░░░░░░░░░░░░▒▓▓▓▓▓▓▓▓▓▓▓██████████████▓▓▓▓▓▓▓▓▓
██████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░▓█████████▓▓▓▓▓▓▓▓█████████▓▓▒░▒▒▒░▒▒░░▒▒░░░░░░░░░░░░░░░░░▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓███████████▓▓▓▓▓▓▓▓▓
███████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░▒▒▓███████▓▓▓▒▒▓▓▓▓██████████▓▓▒▒▒░░░▒▒▓▓▒▒░░░░░░░░░░░░░░░░▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓███████████▓▓▓▓▓▓▓▓▓
█████████████▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▓▓████████▓▒▒▒▒▒▒▓██████████▓▓▓▒▓▒▒▒▒▒▒▓▓██▒▒░░░░░░░░░░░░░░░▒▒▒▓▓▓▓▓▓▓▓▓▓▓███████████▓▓▓▓▓▓▓▓▓
█████████████▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░▒▒▒▓▒▒▓▓▓███████▓▓▒▒░░▒▒▓████████▓▓▓▓▓█▓▓▓▓▓▓▓▓▓█▓▓▓▒▒▒░░░░░░░░░░▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓███████████▓▓▓▓▓▓▓▓
█████████████▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░▒▒▒▓▓▓▓▓▓▓▓▓▓▓██▓▓▒▒▒░░░░░░▒▒▒▓██▓▓▓▓▓▒▒▓▓█▓█▓▓▓▓▓▓▓▓███▓░░░░░░░░░░░▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓████████████▓▓▓▓▓▓▓▓
████████▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒░░░░░░░░░░░░░▒▒▒▒▒▓▓▓██▓▓▓▓▓▓▓█▓▒▒▒░░░░░░░░░░░░▒▒▓██▓▓▓▓▓▓▓███████████████▓░░░░░░░░░▒▒▒▒▓▓▓▓▓▓▓▓▓▓███████████████▓▓▓▓▓▓▓▓
███████▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒░░░░░░░░░░░░▒▒▓▓▓▓█▓████▓▓█▓▓▓█▓▒▒░░░░░░░░░░░░░░░▒▒▓▓█▓▓▓▓█▓████████████████▒░░░░░░░░▒▒▒▒▓▓▓▓▓▓▓▓▓████████████████▓▓▓▓▓▓▓▓
███████▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒░░░░░░░░░░░░▒▓▓▓▓███████▓█▓▓▓█▓▒░░░░░░░░░░░░░░░░░░░░▒▓▓▓▓██████████▓████▓▓▒▒▒▒░░░░░▒▒▒▒▓▓▓▓▓▓▓▓▓▓█████████████████▓▓▓▓▓▓▓▓
███████▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒░░░░░░░░░░░░░▒█████████████▓▓██▒▒░░░░░░░░░░░░░░░░░░░░▒▒█▓████████████▓▓███▒░░░░░░░▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓█████████████████▓▓▓▓▓▓▓▓
███████▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒░░░░░░░░░░░░░▓▒▒███████████▓███▓▒░░░░░░░░░░░░░░░░░░░░▒▓██▓███████████████▓▓▒░░░░░░▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓█████████████████▓▓▓▓▓▓▓▓
████████▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒░░░░░░░░░░░░░░▓███████████▓███▓▒▒░░░░░░░░░░░░░░░░░░▒▒▓███████████████████▒░░░░░░░▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓██████████████▓▓▓▓▓▓▓▓
████████▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒░░░░░░░░░░░░░▒▓▓██████████████▓▒▒▒▒░░░░░░░░░░░░░░▒▓▓▓████████████████████▓░░░░░░░░░░░▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓███████████▓▓▓▓▓▓▓▓
█████████▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒░░░░░░░░░░░░░░▓███████████▓█████▓▓▒░░░░░░░░░░░░▒▓▓███████████████████████▒░░░░░░░░░░░░░░▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓████████▓▓▓▓▓▓▓▓
██████████▓▓▓▓▒▓▒▒▒▒▒▒▒▒▒▒▒▒▒░░░░░░░░░░░░░░██████████████████▓▓▓▒▒▒░░░░░░▒▒▓▓▓████████████▓█████████▓▓█▒░░░░░░░░░░░░░░▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓█████▓▓▓▓▓▓▓▓
███████████▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒░░░░░░░░░░░░░▒▓██████████████████████▓▒▒░░▒▓▓███████████████▓░░▒██████████▓▓░░░░░░▒▒░░░░░░░▒▒▒▒▒▒▒▒░░░░░▒▓▓▓▓████▓▓▓▓▓▓▓▓
███████████▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒░░░░░░░░░░░░░▓▓████████▓▒█████▓████▓▓▓▓▓▓▓▓▒▓▓▓█████████████░░░░░▓██████████▓▒░░░░▒▓▒░░░░░░░░░░░░▒▒░░░░░▒▓▓▓▓████▓▓▓▓▓▓▓▓
███████████▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒░░░░░░░░░░░░▓▓▓███████▓░░█████████▓██▓▒▒▓▒▒▓██▓▓███████████▒░░░░░░▒███████████▓░░▒▒█▓▒▒░░░░░░░░░░▒▒░░░░▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
█████████▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒░░░░░░░░░░░░░▓▓████████▒░░░▒████████▓▓▓▓▓▓█▓▓▓▓▓▓███████████▓░░░░░░░░░▓█████████▓▓░▒▓██▓▓░░░░░░░░░░▒▒░░░▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
██████▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒░░░░░░░░░░░░░░░▓▓████████░░░░░░▓███████▓██▓▒▒▓▒▓██▓▓███████████░░░░░░░░░░░▓█████████▓▓▒████▓░░░░░░░░░░▒▒░░▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
████▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒░░░░░░░░░░░░░░▒▓▓▓███████▒░░░░░░▓████████▓▓▓█▓█▓▓▓▓▓███████████▒░░░░░░░░░░░░██████▒▓▓▒▓▓█████▒░░░░░░░░░▒▒░▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒░░░░░░░░░░░▒▒▓██████████▓░░░░░░░▓████████▓█▓▒▒▓▒▓██▓███████████░░░░░░░░░░░░░░▒████▒▒▓▒██▓████▒░░░░░░░░░▒▒░░▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒░░░░░░░░░░░▒▓██▓████████░░░░░░░░█████████▓▓▓█▓█▓▓▓▓████████████▒░░░░░░░░░░░░▒░▒██▓█▓▓▓▓▓▓████▒░░░░░░░░▒▒▒░░▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒░░░░░░░░░░░▓███▓███████░░░░░░░░▓█████████▓██▓▓▓▓██▓████████████▒░░░░░░░░░░░░░▓████████▓█▓████▒░░░░░░▒▒▒▒▒▒▓▓▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒░░░░░░░░░▒███████████▓▒░░░░░░░█████████████████████████████████▒░░▓▓▒░░░░░░░░░▓██████▓▓█████▓░░░░░▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒░░░░░░░▓███████████▒░░░░░▒██████████████▓▓▓▓█████████████████▓▒░▓█▓░░░░░░░░░░░▓████▓▓▓▓▓██▓░░░░░▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
▓▓▒▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒░░░░░░▓█████████▓▒░░░░░░░█████████████▓▓▒▒▒▒▒▓▓█████████▓▓██▓▓██████▒░░░░░░░░░░▒▓███▓▓▓▓▓██▒░░░░░░▒▒▒▒░░▒░▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
▓▓▒▒▒▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒░░░░░░░░▒▓████████▒░░░░░░░░▓███████████▓█▓▓▒▓▒▒▒▓▓▓█▓█▓▓███████████████▓░░░░░░░░░░░░▒███▓▒▓▓▓▓▓░░░░░░▒▒▒░░░▒░▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒░░░░░░░░░▒▓████████▒░░░░░░░░▓██████████████▓▓▓▓▓▓▓▓▓██████████████████████▒░░░░░░░░░░░░░▓██▓▓▓▓▓▓▓░░░░░░░▒░░░▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
▓▓▒▒▒▒▒░▒▒▒▒░░░░░░░░░░░░░░░░░░▓███████▓░░░░░░░░░░████████████████████████████████████████████▓██░░░░░░░░░░░░░░▓█▓▓▓▓███▓░░░░░░░░░░▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
▓▒▒▒▒▒░░▒░░▒░░░░░░░░░░░░░░░░░▓███████▒░░░░░░░░░░▓█████████████████████████████████████████████▓█▒░░░░░░░░░░░░░░████████░░░░░░░░░░░▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
▒▒▒▒▒▒░░▒░░▒░░░░░░░░░░░░░░░░▒███████▒░░░░░░░░░░▒████▓████▓▓▓▓▓▓███████████████████████████████▓██░░░░░░░░░░░░░░▓███████░░░░░░░░░░░▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░▓██████▒░░░░░░░░░░░█▓▓█▓██▓▓▓▓▓▓▓▓▓██████████████████████▓█████████▓█▓░░░░░░░░░░░░▓▓███████░░░░░░░░░░░▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░▓███▓▓█▓░░░░░░░░░░░▒▓▓▓▓██▓▓▓▓▓▓████████████████████▓▓▓▓███████████▓███░░░░░░░░░░░░█████████▓░░░░░░░░░░▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
▓▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░▓████▓███░▒▓░░░░░░░▒▒▓▓███▓▓▓▓▓▓▓████████████████████▓▓▓▓▓█▓▓▓██████░░▒▓█▓▓▒░░░░░░░░██▒▒█████▓░░░░░░░▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
▒▒▒▒▒▒░▒░░░░░░░░░░░░░░░░░░▓█▓███████▓░░░░░░░▒▒▓▓███▓▓▓▓▓▓▓▓█████████████████████▓▓▓▓▓█▓▓▓▓▓▓███░░░░▒▒▓█▓░░░░░░█▓░░▓████▒░░░░░░░▒▒░▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
▓▓▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░▓▓█████▒▒░░░░░░░░▒▒▓▓████▓▓▓▓▓▓▓▓▓▓████████████████████▓▓▓▓▓█▓▓▓▓▓▓▓▓█▒░░░░░░░▒░░░░░▒░░░▓███▒░░░░░░░░░░░▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
▓▓▓▒▒▒▒▒▒░░░░░░░░░░░░░░░░▒▓▓█▒▓▒▓░░░░░░░░░▒▒▓▓█████▓▓▓▓▓▓▓████████████████████▓▓▓█▓▓▓▓██▓▓▓▓▓▓▓██▒░░░░░░░░░░░░░░░▒██▓░░░░░░░░░░░░░▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
▓▓▓▓▒▒▒▒▒▒▒░░░░▓▒░░░░░░░▒▓▓█▓░░░░░░░░░░░░▒▓▓▓████▓▓██▓▓▓▓▓▓▓█████████████████████▓▓▓▓▓▓█▓▓▓▓▓▓▓▓██▓░░░░░░░░░░░░░░▒█▒░░░░░░░░░░░░░░▒▒▒▒▒▒▒▒▒▒▓▓▒▒▒▓▓▓▓▓
▓▓▓▓▒▒▒▒▒▒▒░░░░░▓████▓█▓▓▓▓█▒░░░░░░░░░░░▒▓▓▓█████████▓▓▓▓█▓███████████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓████▒░░░░░░░░░░░░▒░░░░░░░░░░░░░░░░▒░▒▒▒▒▒▒▒▒▓▒▒▒▒▒▓▓▓▓
▓▓▓▒▒▒▒▒▒▒▒▒░░░░░░▒▒▓▓▓▓▓▓▓█▓░░░░░░░░░░▒▓▓▓████████████████████████████████████▓▓▓▓▓▓██▓▓▓▓▓▓▓███████▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▒▒▒▓▒▒▒▒▒▓▓▓▓
▒▒▒▒▒▒▒▒▒▒▒▒░░░░░░░░░▒█▓▓██████▓▓▒▒░░░▒▓▓▓███████████████████████████████████████▓▓▓▓▓█▓▓▓▓▓▓▓█████████░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▒▒▒▒▓▒▓▓▓▓▓▓▓▓
▒▒▒▒▒▒▒▒▒▒▒▒░░░░░░░░░░▓▓██▓▒▒▒▒▓▒▒░░░▓▓▓▓▓█████████████████████████████████████████▓▓▓██▓▓▓▓▓▓██████████▒░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▒▒
▒▒▒▒▒░▒▒▒▒▒▒▒░░░░░░░░▒▓██▒░░░░░░░░░░▓▓▓▓▓███████████████████████████████████████████▓▓▓██▓▓▓█████████████▓░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▒▒▒
▒▒▒▒▒░░░▒░░░░░░░░░░░░███▓░░░░░░░░░░▓▓▓▓▓████████████████████████████████████████████▓▓▓███▓▓▓██████████████▒░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓▒▒▒▒▒▒
▒▒▒▒▒░░░░░░░░░░░░░░░▒██▓░░░░░░░░░░▓▓▓▓▓███████████████████████████████████████████████▓████▓▓▓██████████████▓░░░░░░░░░░░░░░▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▒▒▒▒▒▒▒▒
▒▒▒▒░░▒▒░░░░░░░░░░░░▓██░░░░░░░░░░▓▓▓▓▓███████████████████████████████████████████████████████▓▓███████████████▓░░░░░░░░░░▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒
▒▒▒▒░░░░░░░░░░░░░░░▒██▒░░░░░░░░░▓███▓███████████████████████████████████████████████████████████████████████████▓░░░░░░░▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒
▒▒▒▒░░░░░░░░░░░░░░░▓██░░░░░░░░░▓██████████████████████████████████████████████████████████████████████████████████▓░░░░░▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒
▒▒▒▒░░░░░░░░░░░░░░▓▓█▒░░░░░░░░▒█████████████████████████████████████████████████████████████████████████████████████▓▒░░░░▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒
▒▒▒▒░░░▒░░░░░░░░░░███░░░░░░░░░█████████████████████████████████████████████████████████████████████████████████████████▓░░░░▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒
▒▒▒▒░░░▒░░░░░░░░░▓██▒░░░░░░░░▓██████████████████▒░▒▓███████████████████████████████████▒░░▒▓█████████████████████████████▓░░░░░▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒
▒▒▒▒░░░▒░░░░░░░░▒███░░░░░░░░▒██████████████████▓░░░▒▓██████████████████▒▒█████████████▒▒░░▒▓███████████████████████████████▓░░░░░░░░░▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒
▒▒▒▒░░░▒░░░░░░░░▓██▒░░░░░░░░███████████████████▓░░▓███████████████▓████▒░░▓███████████▓▓▒▒▒██████████████████░▒▓████████████▓▒░░░░░░░░▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒░
▒▒▒▒░░░▒░░░░░░░░██▓░░░░░░░░▒██████████████████████████████████▒░▒█▒▒███▓░░░░▓██▓█████████████████████████████▓░░░░▒▓██████████▓▒░░░░░░▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒░
▒▒▒░░░░░░░░░░░░▓██░░░░░░░░░███████████████████████████████████▒░░░░░░██▒░░░░░░▓░▓██████████████████████████████░░░░░░▒▓█████████▓░░░░░░░▒▒▒▒▒▒▒░░░░▒▒░
▒▒▒░░░░░░░░░░░░██▒░░░░░░░░░███████████████████████████████████▓░░░░░░░█░░░▓░░░░░░░██████████████████████████████▒░░░░░░░▒▓▓██████▓▒░░░░░▒▒▒░░░▒░▒░░░░░
▒░▒░░░░░░░░░░░▓██░░░░░░░░░▒███████████████████████████████████▓░░░░░░░░▒░░▓░░░░░░░░▒█████████████████████████████▓░░░░░░░░░▒▓▓█████▓▒░░░░░░░░░▒░░░░░░░
░░▒░░░░░░░░░░░██▒░░░░░░░░░▒█████████████▓░▓░▓█▓████████████████░░░░░░░░░░░░░░░░░░░░░█████████████████▒█████████████▒░░░░░░░░░░▒▒▓███▓▒░░░░░░░░░░░░░░░▓
░░▒░░░░░░░░░░▒██░░░░░░░░░░▓██▓░░░░▒█▒▓██▒░░░░▒▒████████████████▒░░░░░░░░░░░░░░░░░░░░█████████████████▓░█████████████▓░░░░░░░░░░░░▒▒▓▓█▓░░░░░░░░░░░░░▒█
░░▒░░░▒░░░░░░▓█▒░░░░░░░░░░▓██▓░░░░░░▒░▓██░░░░░█████████████████▓░░░░░░░░░░░░░░░░░░░░██████████████████░▒██▓▒██████████▒░░░░░░░░░░░░░░▒▓▓▒░░░░░░░░░░░██
░░▒░░░▒░░░░░░██░░░░░░░░░░░▒███░░░░░░░░░█▒▒░░░░░█████████████████░░░░░░░░▒░░░░░░░░░░░▓████████████████▓░░▒█▓▒░████▒▓████▓░░░░░░░░░░░░░░░▒▓▒░░░░░░░░░▓██
░░▒░░░▒░░░░░▒█▒░░░░░░░░░░░░▓███░░░░░░░░▒▓░░░░░▒████████████████▒░░░░░░░░▓░░░░░░░░░░░░█████████████████░░░▓▓░░░▓▒▒░▒▒▓█▓▓▓░░░░░░░░░░░░░░░░░▒░░░░░░░▒███
▒░▒░░░▒░░░░░▓█░░░░░░░░░░░░░░▓███▒░░░░░░░▒░░░░░▓███████████████▓▒░░░▓░░░░▓░░░░░░░░░░░░▓███████████████▓▒░░▒▒░░░▒░░░░░▒▓▓▓▓▒░░░░░░░░░░░░░░░░░░░░░░░░███▒
▒▒▒░░░▒░░░░░█▒░░░░░░░░░░░░░░░░▓██▓░░░░░░░░░░░▒▒▓▓████████████▓░░░░░░░░░░▓░░░░░▒░░░░░░▒░▓█████████████▓░░░░▒░░░▒░░░░░▒▒▓▒▒▓▒░░░░░░░░░░░░░░░░░░░░░░███▒░
▒▒▒░░░▒░░░░▒▓░░░░░░░░░░░░░░░░░░░░▓▓▓▒░░░░░░░░░░░░████████████░░░░░░░░░░░▒░░░░░░░░░░░░░░░▓████████████▒▒░░░░░░░▓░░░░▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░▒▒▒▓██▓▒▒
▒▒▒▒▒░▒░░░░▓▒░░░░░░░░░░░░░░░░░░░░░░░▒▒▒░░░░░░░░░░███████████▒░░░░░░░░░░░░░░░░░░░░░░░░░░░▒█████████████░░░░░░░░▓░░░░▒▒▒▒▒▒▒░░░░░░░░░▒▒░░░░░░░░█████████
▒▒▒▒▒▒▒░░░░▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓███████████░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒█████████████▓░░░░░░░▒░░░▒▒▒▒▒░░░░░░░░░░░░▓███▓▒▒░░▓█████████
▒▒▒▒▒▒▒░░░▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓▓██████████▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓▓▓██████████▒█▒░░░▒░░▒░░░░░░░░░░░░░░░░░░░░░░▓████████████████
█▓▒▒▒▒▒▒▒░▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒░░▓█████████░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒░▒██████████░░▓░░░░░░░░░░░░░░░░░░░░░░░▒░░░░░░▓███████████████
██▓▒▒▒▒▒▒▒▒░░▒▒░░▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒█████████░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒█████████░░░░░░░░░░░░░░░░░░░░░░░░░▓▓▒░▒░░░▒▓██████████████
███▓▒▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓████████▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒████████▒░░░░░░░░░░░░░░░░░░░░░░░░█▓▒░░░░░░░██████████████
██████▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░██████████░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░████████▓░░░░░░░░░░░░░░░░░░░░░░░▒█▒░░░░▒▓▓████▓██████████
███████▓▒▒▒▓▓▒▒▒▒▒▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░▒█████████▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓████████▒░░▒▒░░░░░░░░░░░░░░▒░░▒░▒░░░░▓██████▓▓██████████
████████▓▒▒▒██▓▒▒▒▒▒▒▒▒▒▒▒▓▓▓▒▒░░░░░░░░░░░░░░░▒▓██████████▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓█████████▒░░░░░░░░░░░░░░░░░▒▒░░░░▒█▓████████████████████
██████████▓▓▓██▓▒▒▒▒▒▒▒▒▒▒█████▓▓▒▒░░░░░░░░░░▒▓███████████▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒████████▓░░░░░░▒░░░░░▒░░░░▓███▓░▓▓▓█████████████████████
█████████████████▓▓▒▒▒▒▒▒██████████▓▒░░░░░░░░▒█████████████▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓████████▓░░░░░░░░░░░░▒░░▓██████▓████████████████████████
███████████████████▓▓█▓████████████████▓▒░░▒▓▓█████████████▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░███▓█████▒░░░░░░░░░░░░▒░▒▓██████████████████████████████
███████████████████████████████████████▓▓▓▓▓▓██████████████▓▒▒▒░░░░░▒░░░░░░░░░░░▒▒▒▒▒▒▒▒▒▒▒▒░▒▓▓▓▓▓▓███▓░░░░░░▒░░▒░▒▓▓████████████████████████████████
████████████████████████████████████████████████████████████▓▓▒▒▒▒▒▓▓▒▒▒▒▒▒▓▒▒▒▒▒▒▒▒▒▒▒▒▓▓▒▒▒▓███▓▓▓▓▓██▓▓▒░▒▓████████████████████████████████████████
█████████████████████████████████████████████████████████████████████████████▓▓▓▓▓▓▓██████▓▓▓████████████▓▒▓██████████████████████████████████████████
██████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████");
                break;
            case "Bloodbound Stalker":
                Console.WriteLine(@"
█████████████████████████████████████████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓█████████████████████████████████████████████
██████████████████████████████████████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓███████████████████████████████████████████
████████████████████████████████████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓█████████████████████████████████████████
██████████████████████████████████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓█████████████████████████████████████
████████████████████████████████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓███████████████████████████████████
██████████████████████████████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓█████████████████████████████████
█████████████████████████████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓████████████████████████████████
███████████████████████████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓██████████████████████████████
█████████████████████████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓█▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓████████████████████████████
████████████████████████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓█▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓███████████████████████████
██████████████████████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓█▒▒▒▒▒▒▒▒▒▒▒▓▓▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓█████████████████████████
█████████████████████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒█▒▒▒▒▒▒▒░▒▒▒▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓████████████████████████
████████████████████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒█▒▒░░░░░░░░░▓█▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓██████████████████████
███████████████████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▓▒░░░░░░░░▒▒▓█▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓█████████████████████
██████████████████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▓▒▓▒▒▒▒▒░▒▒▒▓███▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓████████████████████
█████████████████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓█▓█▓▓▓▓▓▓▓▓▓▓███▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓███████████████████
████████████████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒████▓▓███████████▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓██████████████████
███████████████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓█████▓████████████▒▒▒▒▒▒▒▒▒▒▒▒▓▒▒▒▒▒▒▓█▓▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓█████████████████
███████████████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓██████████████████▒▒▒▒▒▒▒▒▒▒▒▓▒▒▒▒▒▒▓▓▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓████████████████
██████████████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒███▓███▓██████████▒▒▒▒▒▒▒▒▒▒▒▒▓▒▒▒▒▒▓█▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓███████████████
█████████████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓█████████████████▒▓▒▒▒▒▒▒▒▒▒▓█▒▒▒▒▒█▓▓▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓███████████████
████████████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▓▒▒▒▒▒▓████████████████████▓▒▒▒▒▒▒▒▓█▓▒▒▒▓█▒▒▒▒▒▒▒▓▓▓▓▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓██████████████
████████████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▓▓▓▒▒▒▒▒▓▒▒▒▓▒▓█████████████████▓▓▓▒▒▒▒▒▒▒▓█▓▒▓██▓▒▒▒▒▒▓▓▒▒▒▒▓▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓█████████████
███████████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▓▒▒▒▓▓▒▒▒▓▒▒▓█▒▒█████████████████▓▒▒▒▒▒▒░▒▓▓▓▓▓▓██▒▓▒▒▒▓▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓█████████████
██████████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▓▒▒▒▒▓▒▒▒█▒▒▒▒█▒▓▓▒▓█████████████████▓▓▒▒▒▒▓▓██▓▓████▓▓▓▒▒▓▓▒▒▓▓█▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓████████████
█████████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▓█▓▒▒██▓▓▓▓▓▒▓███████████████████▓▓▓▓██▓█▓▓▓▓▓▓▒▓▓▓▓█▓▓█▓▓▓▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓███████████
█████████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓███▓▒▒▒▓▓▓▓██████████████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓████▓▓▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓██████████
████████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▒▓▓▓▒▒▒▒▒▒▒▓████████████████████████▓█▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓█▓███▓▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓██████████
████████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓██▓▓█▓▓▓▓▒▓▓█████████████████████▓█▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓███▓▓▓▓▓▓▓▓▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓█████████
███████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▓▒▓▓▓▒▓▓█▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓█████████████████████████▓▓▓▓▓▓▓▓█▓▓██████████▓▓▒▓▓▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓████████
██████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓██████▓▓▓▓▓▓▓████████████████████████▓▓▓▓▓▓▓▓▓▓▓▓▓███████████▓▓▓▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓████████
██████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓█████████▓████████████████████████████████▓▓▓▓▓▓█████████████▒▓▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓███████
█████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓█████████████████████████████▓███▓▓▒▓▓▓▓█████████████████████▓▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓██████
█████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓█████████████████████████████▓▓█▓▒▒▒▒▓▓▓▓█████████████████████▓▓▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓██████
████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒█▒▓█████████████████████████▓▓▓▓▓▓▓▓▓▓▓▓██████████████████████▓▒▓▓▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓█████
████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒█▒▒████████████████████████▓▓▓▓▓▓▓▓▓▓▓█████████████████████████▓▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓█████
████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▒▒▓████████████████████████▓▓▓██▓▓▓████████████████████████████▓▓▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓████
████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓████████████████████████████████████████████████████████████▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓███
███████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒███████████████████████████████████████████████████████████▓▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓███
███████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓████████████████████████████████████████████████████████████▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓███
███████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▒██████████████████████████████████████████████▒▓█████████████▓▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓███
███████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓██████████████████████████████████████████████▒▒█████████████▒▒▒▓▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓███
██████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒██████████████████████████████████████████████▓░░▒▓████████████▒▒█▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓███
██████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒██████████████████████████████████████████████▒░░░░▒▓██████████▓██▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓██
██████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓█████████████████████████████████████████████▓▒░░░░░░▒█████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓█
██████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓███████████████████████████████████████████▓▒█▒░░░░░░░░▒████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓█
██████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒██████████▒▒███████████████████████████████▒░░█░░░░░░░░▓▓████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓█
██████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▒▒▓██████████▓▒▒▓█████████████████████████████▓░░░░▒░░░░░░░░▒███████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓█
██████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓████████████▓▒░▓█████████████████████████████▓░░░░░░░░░░░░░░▓█████████████▓▓▓▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓█
██████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓████████████▓▒▒███████████▓▓█████████████████▓░░░░░░░░░░░░░░▓████████████▓▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓█
██████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▓█████████████▒░▓███████████▓███████████████████▓▓▓▒░░░░░░░▒▒▒█████████████▓▓▓▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓█
██████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▓████████████▓▒▓████████████████████▓▓███▓████████▒░░░░░░░░░░▒████████████▓▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓█
██████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▓█████████████▒▓█████████████▓▓██████▓▓▓████████████▒▒▒░░░░░░░▒▒███████████▓▓▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
██████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▓████████████▒███████████████████████████████████████▓░░░░░░░░░░▓█████████▓▓▓▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
██████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒██████████▓▒▒████████████████████████████████████████▓▒░░░░░░░░░██████████▓▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
██████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▓█████████▒▒▒▓██████████████████████████████████████████░░░░░░░░▒▒████████▓▓▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
███████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▓▒▓████████▓▒▒░▒███████████████████████████████████████████▒░░░░░░░░░▓███████▓▓▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
███████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▓████████▒▓░░░▒███████████████████████████████████████████▓░░░░░░░░░████████▒▓▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
███████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▓████████▒▒▒░░░▓████████████████████████████████████████████▒░░░░░░░░███████▓▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
███████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▓████████▓▓▒░░░▒██████████████████████████████████████████████░░░░░░▒████████▓▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
███████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▓███████▓▒▒▒▒░░░▒██████████████████████████████████████████████▒░░░░░▓████████▓▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓████████▓▒▒▒▒░░░▓███████████████████████████████████████████████░░░░▒▓███████▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓███████▒▓▓▓▓▓░░░████████████████████████████████████████████████▒░░▓█████████▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
███████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓████████▓█▓▒▒░░░▒███████████████████████████▓▓███████████████████▒▒███████████▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
███████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓██████████▒▒▒▒░░░▒██████████████████████████▓▓████████████████████▓▓███████████▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
███████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓███████████▓▒▒▒▒▒░▒██████████████████████████▓██▓▓█████████████████▓▒██▓████████▓▒▒▒▒▒▒▒▒▓▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
███████████▓▓▓▓▓▓▓▓▓▓▓█▓▓▓▓▓▓▓▓▓▓▓████████▓██▒▒▒▒░░▒▒████████████████████████████▓▓▓█████████████████▓▓██▒▓███████▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
███████████▓▓▓▓▓▓▓████████▓▓▓▓▓▓▓▓███████▓▒▓█▓▒▒▒░░▒▒███████████████████████████▓▓▓▓█████████████████▓▓██▒▓██████▓▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
██████████▓▓▓▓▓▓▓████████████▓▓▓▓█████████▓██▓▒▒▒▒░░▓███████████████████████████▓▓▓██████████████████▓▒▓▒▒██████▓▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
██████████▓▓▓▓▓▓▓█████████████▓█████████████▓▒▒▒▒▒░░▒████████████████████████████▓███████████████████▓░▒▓████▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
██████████▓▓▓▓▓▓▓▓███████████████▓▓██▓▓▓▓▒▒▒▒▒▒▒▒▒▒░▓█████████████████████████▓▓██████████████████████▒░▒▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
█████████▓▓▓▓▓▓▓▓▓██████████████▓▓▓█▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓██████████████████████████▓██████████████████████▒░░▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
█████████▓▓▓▓▓▓▓▓▓██████████████▓███▓▒▒▓▒▒▒▒▒▒▒▒▒▒▒▒▓█████████████████████████████████████████████████▓░▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
█████████▓▓▓▓▓▓▓▓▓██████████████▓▓███▒▓▒▒▒▒▒▒▒▒▒▒▒▒▒██████████████████████████████████████████████████▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
█████████▓▓▓▓▓▓▓▓███████████████▓▓███▓▒▒▒▒▒▒▒▒▒▒▒▒▒▓███████████████████████████████████████████████████▒▒▒░░▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
█████████▓▓▓▓▓▓▓▓▓███████████████▓▓█▓▓▒▒▒▒▒▒▒▒▒▒▒▒▓▓███████████████████████████████████████████████████▓▒░░░▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
█████████▓▓▓▓▓▓▓▓▓███████████████▓▓▓█▓▓▓▒▒▒▒▒▒▒▒▒▒▓▓███████████████████████████████████████████████████▓▒░░░▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
██████████▓▓▓▓▓▓▓▓████████████████▓▓█▓▒▓▒▒▒▒▒▒▒▒▒▒▓▓███████████████████████████████████████████████████▓▒░░░░▒▒▒▒▒▒▒▒▒▒▒▒▓▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
██████████▓▓▓▓▓▓▓▓▓███████████████████▓▒▒▒▒▒▒▒▒▒▒▒▓█████████████████████████████████████████████████████▓▒░░░▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
█████████▓▓▓▓▓▓▓▓▓▓▓██████████████████▓▒▒▒▒▒▒▒▒▒▒▒▓██████████████████████████████████████████████████████▓▒░▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
█████████▓▓▓▓▓▓▓▓▓▓▓▓█████████████████▓▒▒▒▒▒▒▒▒▒▒▒▓███████████████████████████████████████████████████████▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
██████████▓▓▓▓▓▓▓▓▓▓▓██████████████████▓▒▓▒▒▒▒▒▒▒▒▓███████████████████████████████████████████████████████▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
███████████▓▓▓▓▓▓▓▓▓▓▓▓██████████████████▓▒▒▒▒▒▒▒▒▓████████████████████████████████████████████████████████▓▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
███████████▓▓▓▓▓▓▓▓▓▓▓▓███████████▓▓▓██▓██▓▓▓▒▒▒▒▒▓█████████████████████████████████████████████████████████▓▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
█████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓███████▓▓▓▓▓▓▓██▓▓▒▒▒▒▒▓██████████████████████████████████████████████████████████▓▓▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
█████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓███▓▓▓▓▓▒▒▓█▓▓▒▒▒▒▒▓██████████████████████████████████████████████████████████▓▓▓▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
█████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓███▓▓▒▒▒▒▒▓▓▓▒▓▒▒▒███████████████████████████████████████████████████████████▓▓▓▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓███▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓██▓▓▓▒▒▒▒▓▓▓▒▒▒▒▓███████████████████████████████████████████████████████████▓▓▓▒▒▒▒▒▒▒▒▒▒▓▓▓████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓█▓▓▒▒▒▒▒▒▒▓▒▒▒▓████████████████████████████████████████████████████████████▓▓▓▓▒▒▒▒▒▒▒▒▒▓█▓██▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▓▒▒▓█████████████████████████████████████████████████████████████▓▓▓▓▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▓▓▓█████████████████████████████████████████████████████████████▓▓▓▓▓▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▓▓▒▒▒▒▒▒▒▓████████████████████████████████████████████████████████████████▓▓▓▓▓▒▓▓▓▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▓████████████████████████████████████████████████████████████████▓▓▓▓▓▓▓▓▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓");
                break;
            case "Graveborn Revenant":
                Console.WriteLine(@"
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓▒░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓▓░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▓▒░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓▓▓▒░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓▓▓▓░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓▓▓▓░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒░▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓▓▓▓▓░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░█▓▓▓▓░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒░░░░▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▓▓▓▓░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓▓▒▓▓▓░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▓░▒▒▓░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓▓▓░▒▒▓░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▓█▒▒▓▒░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▓▓▓▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▓▓▓▓▒▒░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓▓▓▓█▓▓░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▓▓▓█▓▒░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▒▒▓▓▓▓▓▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓█▒██▓▓░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▓█▓▓▓░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▒▓▓█▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▓▓▒▓▓▒▓░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒░▒▒▒▓▒▒▓▓▓▓▓██▓▓▓▓▓▓▓▓██░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒░▓▒▒▓░▓░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▓▓▓▓▓▓▓██████████▓███▓░░▒▒▒▒▒▒░▒▒░░░░░░░░░░░░░░░░░░░░░░░░▓▒▒▓▒▓░░▓░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▓▓▓▓▓███████████▓██████████████▓▓░░░░░░░░░░░░░░░░░░░░░░▓▓▓▒▓▓░░░▓░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒███████████████▓█████▓██▓▓▓█████▓░░░░░░░░░░░░░░░░░░░░▒▓▓▓▒▓▒░░░█░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▓█▓▓▓██████████████▓▓█▓██▓█▓▓███████▒░░░░░░░░░░▒▒▓▓▒▒░░░▓▓▓▓▓░▒░░░▓░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓███▓▓████████████████▓███▓██▓██████▓▓██▓▒░░▒▒░░▒███████▓▒▓▓▓▓▓░░▓░░░▓░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓█▓▓▓▓███████████████████▓▓▓██▓███▓▓█▓█████▓███▓████████▓▓▓▒▓▓░░░▓░░░▒░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓█▓▓▓▓█████████▓████▓███▓█▓▓▓██████▓██████▓███▓▓▓▓▓█████▓▓█▓▓▓▓░░▒░░▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒█▓█▓▓▓█████████▓███████████▓▓▓█████▓▓█████████▓██▓████████████▓░░░▒░▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▓██▓▓█████████▓█████▓███▓████████████████████████████████████▓░░░▒▓▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▓██▓▓▓██████████████▓▓██▓▓▓▓▓████████████▓█████████████████▓▓▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▓▓██▓▓████▓▓█████████▓▒▓▓▓▓▓▓▓████████████████████████▓█████▓▓▓▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▓▓▓█▓▓▓█▓█████████████▒▓▓▓▓▓▓███████░░░▓░▓░▒▒▓░▓█████████████▓▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▓▓█▓▓▓███████▓███████▒▓▓▓▓▓▓██████▒░░░▓░▒▒░░▒▒░▒▓███▓███▒██▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓▓▓█▓▓▓█▓▓▓▓██████████▓▓█▓▓▓▓██████░░░░▓░░▓░░░▒░░▓▒░▒▓█▓▓▓▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓█▓▓▓▓▓▓▓▓▓██████████▓█▓██▓▓█████▓▒░░▓▒░▓░░▒▒░░▒▒░░▓▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓▒▓█▓▓█▓▓▓█▓▓██████▓███▓█▓▓▓▓▓▓█▓███▓░░░▓░░░░░▒░░▒▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒█▒▒▓█▓▓▓▓▓█▓▓▓██▓██▓█████▓▓▓▓▓██████▒░░░░▓░░░▒░░░░▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓▓█▒▒▓███▓█▓▓▓▓█████▓████▓▓▓▓█▓▓█████▓░░░░░░▒▒▒▓░░░░▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓▓▓▒░░▒▓████▓▓▓▓▓▓██▓▓▓███████████▒▓██▓▓▓███▓▓░░░░░░░░░░░░░░▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▓▓███▓▓▓▓█████▒▓▓▓▓████████████████▓▓█▓▓▓█████▓░░░░░░░░░░░░░░▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▓████████▓▓███████▒▓▓▓▓██████████████████▓██████▓░░░░░░░░░░░░░░░▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒██▓▓█████████████████▓▓▓▓█████████████████████████▒░░░░▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▓▓██████████████████████▓▓█▓▓███▓██████████████████████▓░░░▒▓▓▓▒░░░░░░▒░░░░░░░░░░░░▒▓██░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓█▓█████████████████████▓░░░░▓▓███▓▓████████████████████████▓░░▒████▒░░░░▒▒░░░░░░▒▓▓▓█▓▓▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓▓██████████████████████▓░▓░░░▒▓████▓▓██▓███▓███████████████████▓███████░░░░▓█▓░▒▓████▓▓▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒█▓█████████████████████▒░▓░░▒██████▓███████▓█████████████████████████▓▓░░░▒▓███████▓▓░░▒░▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒█▓███████████████████▓░░▓░░▒███████▓██████▓███████████████████████▓░░▒▓▓█▓█████▒░░░▒░░▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓▓█████▓██▒▓███▓▒▒▓▓░░░▓▒░▒▓▓███████████████▓██████▓███████████▓▓██▓███████████▓░░░▒▒░▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓▒░▓▓▓░░░▓░░░░░░░░▒▒░░▒▓░▓███████████████████▓█████▓▓██████████████████████▓█░▓▓▒░░▒▓░▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓▒░▒▓▓░░░█░░░░░░░░░▒▓▒░▒▓█████▓█████████████████████▓███████████████████▒▒▒▓▒░▓░▒▒░░▒▒▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓░░▒▓▓░░░▒░░░░░░░░░░░░░▓███████▓████████████████████▓█▓██████████████████▓▒░░░▒▒░░░░▓▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓░░▒▒▓░░░▒░░░░░░░░░░░░▒▓███████████████████████████████████████████████████▒░░░▓▒▒▒▓▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓░░▒▒▓▒░░░░░░░░░░░░░░░█▓██████████████████████████████████▓▓████████████████▓░░░░░░░▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓░░▒▒▓▒░░░░░░░░░░░░░░▒███████████████████████████████████▓▒▓███████▓█████████▓░░░░░░▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓░░▒▒▒▒░░░░░░░░░░░░░░▓████████████████████████████████████▓▓▓██████████████████▒░░░░▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓▓▒░▒▒▒▒░░░░░░░░░░░░░▒██████████████████████████████████████▒▓▓██████████████████▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▓▓▓███▓▓▓░░░░░░░░░░░░░▒██████████████████████████████████████▓▒▓████████████████████▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░▒██▓▒▓██████▓░░░░░░░░░░░░░░▓▓█████████████████████████████████████▒▒▓▓█████████████████████▓▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░▓▓▓▓████████▓█░░░░░░░░░░░░░▓▓███████████████████████▓██████████████▒▓▓▓██████████████████████░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▓▓▓▓████████▒░░░░░░░░░░░▒▓████████████████████████▓██████████████▓▒▓▓███████████████████████▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░▓▓▓▓▓█████████▒░░░░░░░░░░▒▓▓████████████████████████████████████████▒▒▒▓████████████████████████▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░▒█████████▓▓▓▓███▓▒░░░░░░▓█▓█████████████████████████████████████████▒▒▓▓██████████████████▓██████▓░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓██▓▓▓▒░▒▓▓▓████▓▒░░░▒▓███████████████████████████████████████████▓▒▒▒█████████████████▓▓████████▒░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░▒██▓▓▒░▒▒▓▓▓████▓▓▒░░█▓▓███████████████████████████████████████▓▓██▓▓▓▓███████████████▓▓██████████▓░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░▒██▓▓▓▒▒▓▓▓█████▓▓▒▒▓▓▓██▓██████████▓▓▓██████████████████████████████▓▓▓▓████████████▓▓▓████████████░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░▓███▓▓▓▓████▓▓▓▓█▓▓▓▓▓████████████████████████████████████████████████▓▓▓███████████▓▓▓████████████▒░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░██████████▓▓▓▓▒▓▓███▓▓████████████████████████████████████████████████▓▓▓██████████▓▓▓████████████▒░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░▓▓██▓▓████▓▓▓▓▓▓███▓▓████████████████████████░░░░░▓▓███████████▒▓██████▓▓██████████▓█████████████▒░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▓▓▓▓▓▓████▓▓██████▓███████████████████████▓░░░░░▒▓███████████▓▒██████▓▓▓▓███████████████████▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▓▓▓▓▓███████████▓▒██████████████▓▓▓███████▓░░░░░▓▓███████████░░▓█████▓▓▓███████████████▓▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓███████████▓▓▒░░▒████████████████████████▓░░░░▒████████████▒░▓▓█████▓▓▓██████████▓▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓██████▓▒▒░░░░░░▓████████████████████▓▒░░░░░░█████████████░▒▓█████▓▓▓██████████▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓░░░░░░░░░░░░░░░▒▓███████████████▓██▒░░░░░░░▓▓███████████░░░▓▓████▓▓███████████▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓▒▒░░░░░░░░░░░░░░░░░█████████████████▒░░░░░░░▓█████████████▒░▒▓▓█████████████████░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▓▓███▓▒░░░░░░░░░░░░░▒▓▓████████████████░░░░░░░██████████▓▓▒▒░░░▓▓█▓█████████████▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓▓▒▓██████▓░░░░░░░░░░░░▒▓████████████████▓░░░░░░▒██████████░░░░░░░░███▓▓███████████▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒███▓██████▓▓░░░░░░░░░░░░░░░▓██████████▓░░░░░░░░▒███████████▒░░░░░░░░░░░▒███████████▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▓▓▓████▓▓▓▒▒░░░░░░░░░░░░░░▒▓█▓████████▓░░░░░░▓██▓█████████▓░░░░░░░░░░░░▒██████████▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▓██████▓▒░░░░░░░░░░░░░░░░░███████████░░░░░░░░░▒▒▓█▓▓█▓▓█▒▓▒░░░░░░░░░░░░▒█▓████████▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▓▓▓▒▒░░░░░░░░░░░░░░░░░░░▓█████████▓░░░░░░░░░░░░░▒░▒▒░▒░░░░░░░░░░░░░░░░▓█████████▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒█████████▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒█████████▓░░░▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓█████████▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓█████████▒░▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒░░░░░░░▒▒██████████▓▒▒▒▒▒▓▓▓▓▒▒▓▓▒▒▒▒▒▒▓▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▒▓██████████▒▒▒▒▒▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▓▓▓▓▓▓▓▓█▓▓██████████████▓▓▓▓▓▓▓██▓▓▓▓▓▓▓▓▓▓▓▓▓██▓▓▓█▓▓▓▓▓▓██▓▓██▓█████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▓▓▓██▓▓▓▓▓██▓██████████████████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓████▓▓▓█▓▓▓███████████████▓▓████▓▓▓▓▓▓▒░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓████████████████████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓█▓█████████▓▓▓▓▓▓▓▓▓▓▓█▓▓▓▓▓▓▓▒░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓██████████████████████████████████████▓▓▓▓▓▓▓▓█▓▓▓▓▓▓▓▓▓▓▓▓▓▓████████████████████████▓▓█▓▓████▓█▓▓▓▒▒░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▓▓▓▓▓▓▓██▓▓███████████████████████████▓▓▓████████▓▓▓▓▓▓████▓▓▓▓▓▓▓▓▓██▓███████████████████████████▓▓██▓▓▓▓▓▓▓▓▒▒░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓███████████▓▓█▓▓██▓██████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓█▓▓▓█▓▓▓▓▓▓▓▓▓▓██████████████████▓▓▓▓▓▓▓▓▓▒░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓█████▓▓▓▓▓▓▓▓▓▓▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒░▒░░░▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
");
                break;
            case "Twilight Herald":
                Console.WriteLine(@"
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒░░░▒▓█▓▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒░░░░▒▓██████▓▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒░░░░░▒███████▓███▓▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒░░░░▒█████████▓██████▓▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒░░░▒████████▓▓█▓███▓████▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒░░░▒█████▓▓▒▓▓▓▓▓▓███▓██████▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒░░░▓████▓▓▒▒▒▒▒▒▒▓▓▓▓▓▓▓██████▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒░▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒░░▒█████▓▓▒▒▒▒▒▒▒▓▓▓▓▓▓▒▓▓███████▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒░▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒░░▒█████▓▒▒▒▒▒▒▒▒▒▒▓▓▓▓▒▒▒▓▓███████▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒█████▓▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▒▒▒▓▓▓███████▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒█████▓▒▒▒▒▒▒▒▒▒▒▒▒▓▓█▓▒▒▒▒▓▓████████░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒█████▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▓▓▒▒▒▒▒▓▓████████▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒█████▓▓▓▓▓████▓▓▓▒▒▒▒▒▓▒▒▒▒▒▓▓▓████████▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▓█████▓▓▓▓▓▓▓▓████▓▒▒▒▒▒▓▓▓██████████████░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▓███████████▓██▓▓▓▓▓▓▒▒▒▓▓█████▓██████████▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▓██████████████▓██▓▓▓▒▒▒▓▓▓▓████████████████░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▓▓█████▓▒▓▓▓▓▓▓▓▓▓▓▒▓▓▒▒▒▒▓▓████▓████████████▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓█████▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒░▒▒▓▒▓▓▓▓▓████████████▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓████▓▓▒▒▒▒▒▒▒▒▒▒▒▒░▒▒▒░░▒▒▒▒▒▒▓▓▓▓▓██████████░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓█████▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒░▒▒▒▒▒▒▒▒▒▓▓██████████░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓█████▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓███████████▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓██████▓▓▓▓▓▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▒▒▒▓▓▓████████████░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓██████▓▓▓▓▓▓▒▒▒▒▒▒▒▒▓▓██▓▓▓▓▓▒▒▒▒▓▓█████████████░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓██████▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓▒▒▒▓▓██████████████▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓██████▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▒▓▓▓████████████████░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓███████▓▓▓▓▓▓▓▓▓▓▓▓▓██████████▓▓▓█████████████████▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓█████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓███▓▓███████████████████░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓███████████▓▓▓▓▓▓▓▒▒▓▓▓▓██▓▓▓▓▓▓████████████████████▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓█████████████▓▓▓▓▓▓▒▒▒▒▒▓▓▓▓▓▓▓██████████████████████▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓████████████████▓▓▓▓▓▒▒▒▒▒▒▓▓▓▓████████████████████████░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓████████████████████▓▓▓▓▓▓▓▓▓██████████████████████████▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓█████████████████████████████████████████████████████████▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓██████████████████████████████████████████████████████████░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓█████████████████████████████████████████████████▓▓▓███████▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓█████████████████████████████████████████████████▓▓▓▓▓██████░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓███████████████████████████████████████████████████▓▓▓▓▓▓▓▓█████░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░▒████████████▓▓▓▓▓▓▓▓█████████████████████████████████████████████████████████▓▓▓▓▓▓▓▓▓▓▓███▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░▓██████████████████████████████████████▓▓█████████████████████████████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░▒█████████████████████████████████████████▓▓██▓███████████████████████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░█████████████████████████████████████████▓▓▓▓▓▓██▓▓██▓███████████▓▓███████████▓▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▓▓▓▓▓▓█████████████████████████████████▓▓▓▓▒▓▓▓▓▓▓▓▓██▓▓███████▓▓▓█████████████▓▓▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▓▓▓▓▓▓▓████████████████████████████▓▓▓▓▓▓▒▒▓▓▓▓▓▓█▓▓▓▓▓██████▓▓██████████████▓▓▓▓▓▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░▒██████▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓█████████████████████▓▓▓▓▓▓▓▓▓▒▒▒▓▓▓▓▓███▓▓▓▓▓███▓▓▓██▓███████████▓▓▓▓▓▓▓▓▓▓▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░██████████▓▓▒░░░░░░░░░░░░▒▒▒▓▓▓▓▓▓██████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▓▓▓▓▓▓▓▓▓██████▓▓▓█▒██▓██████████▓▓▓▓▓▓▓▓█████▓▓▒░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░███████████████▓▒░░░░░░░░░░░░▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▓▓▓▓▒▓██████▓▓█▒▓█▓▓██████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░███████████████████▓▒░░░░░░░░░░▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▓▓▓▓▓▓▓▓▓▒░▒▒▒▒▓▓▓▓▓█████▓▓█▒▓▓▓▓███████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓█▒░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░███████████████████████▓▒░░░░░░░░░▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▓▓▓▒▒▒▒▒░░▒▒▒▓▓▓▓▓▓█▓██▓█▒▒▓▓▒▓███████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░███████████████████████████▓▒░░░░░░░▒▒▒▒▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒░░▒▒▒▓▓███▓███▓▓▒▒▓▒▒▓▓▓█████████▓▓▓▓▓▓▓▓▓▓▓▓▓░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░▓█████████████████████████████▓▒░░░░░░▒▒▒▒▒▓▓▒▒▒▒▒▒▒▒▒▒░░▒▒▒▒▒▒▒▒▒▒▒▒░░▒▒▒▓█▓██▓▓██▓▒░▒▓▒▓▓▓▓██████████▓▓▓▓▓▓▓▓▓▓▒░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░███████████████████████████████▓▒░░░░░░▒▒▒▒░▒▒▒▒▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒░░▒▒▓▓▓▓▓▓██▓▒░▒▒▒▒▓▓▓▓▓███████████████▓▓░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░█████████████████████████████████▓▒░░░░░░▒▒▒▒▒▒▒▒▒▒▓▒▓▓▓▓▒▒▒░░▒▒▒▒▒▒▒▒░░▒▒▓▓▒▒▓▓▓▓▒░░▒▒░▒▒▓▓▓▓▓▓▓█▓█████████▓▒░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░▓███████████▓▓▓▓███████████████████▓▒░░░░░░▒▒▒▒▒▒▓█▒▒██▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒░▒▒▒▒▒▓▓▓▓▒░░▒▒░▒▒▒▓▓▒▓▓▓▓▓▓▓████████▓░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░▓██████████▓▓▓▓▓▓████████████████████▓▒░░░░░░▒▒▒▒▒▓█▓███▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒░░▒▒▒▓▓▓▓▒▒░▒▒▒░▒▒▓▓▒▓▓▓▓▓▓▓▓▓▓▓▓████▓░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░▓██████████▓▓▓▓▓▓▓▓▓█████████████████████▒░░░░░▒▒▓▓▓▓▓███▓▓█▓▒░▒▒▒▒▒▒▒▒▒▒▒░░▒▒▓▓▓▓▓▓░▒▒▒░▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓████▓░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░▓██████████▓▓▓▓▓▓▓▓▓▓▓▓██▓▓█████████████████▒░░░░░▒▒▓▓▓▓▒▓▓▓▓▒▒▓▒▒▒▒▒▒▒▒▒▒▒▒░░▒▓▓▓▓▓▓░▒▒▒░▒▒▒▓▓▓▓▓▓▓▓▓▓▒▓▓▓▓▓███▓▒░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░▒▓▓███████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓██████████████████▒░░░▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▓▓▓▒░▒▓▓▓▓▓▒▒▒▒▒░▒▒▓▓▒▒▒▒▓▒▒▒▒▒▓▓▒▓███▓▓▒░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░▒▓▓▓███████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓███████████████████▓▒░▒░▒▒▒▒▒▒▒▒▒▓▓▓▓▓▒▒▒▒▒▒▓▓▓▓▓▒▒▓▓▓▓▓▒▒▒▒▒░▒▒▓▓▒▒▒▒▓▓▓▒▒▒▓▒▒▒▓██▓▓▓▒░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░▒▓▓▓▓██████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓█████████████████████▒░▒▒▒▒▒▒▒▒▒▒▒▒▒▓▒▒▒▒▒▒▒▒▓▓▓▓▓▒▒▓▓▓▓▒▒▒▒▒▒▒▓▓▒▓█▓▓███▒▓▒▒▒▓▒▓███▓▓▓░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░▒▓▓▓▓███████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓██▓▓▓▓█████████████████████▓▒░▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▒▒▓▒▓▓▓▓▓▓▓▒▒▓▓▓░▒▒▒▒▒▓▓▓███▓▓███▓▓▒▒▒▒▒▒▓██▓▓▓▓░░░░░░░░░░░░░░░░░░░
░░░░░░░░░▒▓▓▓▓▓███████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓██▓▓▓▓███████████████████████▓▒▒▒▒▒▒▓▒▒▒▒▒▒▓▒▓▓▓▓▒▓▓▓▓▓▓▓▒▓▓▓░▒▓▓▓▒▓▓███▓▓▓███▓▓▒▒▒▒▒▒▓██▓▓▓▓▒░░░░░░░░░░░░░░░░░░
░░░░░░░░▒▓▓▓▓▓████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓██▓▓▓█████████████████████████▓▒▒▒▒▒▓▒▒▓▓▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒░▓▓▓▓▓▓████▓██████▓▓▓▓▓▓▓▓██▓▓▓▓▓▒░░░░░░░░░░░░░░░░░
░░░░░░░▒▓▓▓▓▓██████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓██▓▓▓███████▓▓█████████████████▓▒▒▒▓▒▓▓▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓░▒▓▓▓▓▓▓████████▓█▓▓▓▓▓▓▓▓▓███▓▓▓▓▓▒░░░░░░░░░░░░░░░░
░░░░░░▒▓▓▓▓▓████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓██▓▓▓██████▓▓▓███████████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓█▓▓█▓▓▓▓▓▓▓▓░▒▓▓▓▓▓████████▓▓▓▓▓▓▓▓▓▓▓▓███▓▓▓▓▓▓░░░░░░░░░░░░░░░░
░░░░░▒▓▓▓▓▓████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓█▓▓▓██████▓▓█████████████████████▓▓▓▓▓▓█▓▓█▓▓▓██▓███▓▓▓██▓▒▓▓▓▓▓▓███████▓▓▓▓▓▓▓█▓█▓▓▓███▓▓▓█▓▓▓░░░░░░░░░░░░░░░
░░░░▓▓▓▓▓▓▓████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓█▓▓▓██████████████████████████████▓▓▓▓▓▓█▓▓██▓███████████▓▒▓▓▓▓▓▓█████▓▓▓▓▓▓▓████████████▓▓█▓▓▓▒░░░░░░░░░░░░░░
░░░▓▓▓▓▓▓██████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓██▓▓▓██████████████████████████████▓███▓▓████████████████▓▓▓▓▓▓▓▓██████▓██▓██████████████▓▓██▓▓▓▒░░░░░░░░░░░░░
░░▓▓▓▓▓▓███████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓██▓▓▓████████████████████████████████████████████████████▓█▓▓▓▓▓███████▓█████████████████▓▓▓█▓▓▓▓▒░░░░░░░░░░░░
░▓▓▓▓▓▓███████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓███▓▓███████████████████████████████████████████████████████▓▓▓▓██████████████████████████▓▓██▓▓▓▓▒░░░░░░░░░░░
▓▓▓▓▓▓████████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓████▓███████████████████████████████████████████████████████▓▓▓▓██████████████████████████▓▓██▓▓▓▓▓▒░░░░░░░░░░
▓▓▓▓██████████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓███████████████████████████████████████████████████████████▓▓▓▓████████████████████████████▓██▓▓▓▓▓▓▒░░░░░░░░░
▓▓████████████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓█████████████████████████████████████████████████████████████▓▓▓▓████████████████████████████▓██▓▓▓▓▓▓▓▒░░░░░░░░
▓█████████████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓███████████████████████████████████████████████████████████████▓▓▓█████████████████████████████▓██▓▓▓▓█▓▓▓▒░░░░░░░
██████████████████████▓▓▓▓▓▓▓▓▓▓▓▓▓███████████████████████████████████████████████████████████████▓▓▓▓████████████████████████████████▓▓▓████▓▓▒░░░░░░
██████████████████████▓▓▓▓▓▓▓▓▓▓██████████████████████████████████████████████████████████████████▓▓▓▓█████████████████████████████████▓▓█████▓▓░░░░░░
██████████████████████▓▓▓▓▓▓▓▓▓███████████████████████████████████████████████████████████████████▓▓▓███████████████████████████████████▓█████▓▓▓░░░░░
████████████████████████▓▓▓▓▓▓█████████████████████████████████████████████████████████████████████████████████████████████████████████████████▓▓▓░░░░
█████████████████████████▓▓▓█████████████████████████████████████████████████████████████▓▓▓▓▓▓▓▓▓▓▓▓▓█████████████████████████████████████████▓▓▓▓░░░
██████████████████████████▓███████████████████████████████████████████████████████████████▓▓▓▓▓▓▓▓▓▓▓██████████████████████████████████████████▓▓▓▓▒░░
█████████████████████████████████████████████████████████████████████████████████████████▓█▓█▓▓▓▓▓▓▓███████████████████████████████████████████▓▓▓▓▓▒░
████████████████████████████████████████████████████████████████████████████████████████████▓▓▓▓▓▓▓██████████████████████████████████████████████▓▓▓▓▒
█████████████████████████████████████████████████████████████████████████████████████████▓▓▓▓▓▓▓▓▓▓▓▓██████████████████████████████████████████████▓▓▓
██████████████████████████████████████████████████████████████████████████████████████▓▓▓▓▓▓▓▓██████▓▓▓▓█████████████████████████████████████████████▓
███████████████████████████████████████████████████████████████████████████████████▓██▓▓▓▓▓▓▓▓████████████████████████████████████████████████████████
████████████████████████████████████████████████████████████████████████████████████▓▓██████████▓█████████████████████████████████████████████████████");
                break;
            case "Dream Specter":
                Console.WriteLine(@"
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▓▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒░▒░░▒▓▓▓▓▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒░░▒███████▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒░░▓█████████▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒░▓▓▓█▓▓▓█████░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒░▒▒▓▓▒█▓██▒█████▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒░░▒██▒▒░░▓████████▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒███▓▒▒▓▓█████████░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒█████▒▒▓▓█████████▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓██████▓▒▒▓██████████▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒█████████▓███████████▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒████▒▓████████████████▓▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓████▓██████████▓██████▓██▓▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒██████████████████████▓█████▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▓███▓▓███████████████████████▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒░▒▒▒▓▒▒▓▒▓▓▓████████████████████████▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓░▒▓▒▓▒▓▒▒▓▓████████▓█████████████████▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░▒░░░░░▒░░░░░▒░░░░░░░░░░░░░░░░░░░░░▓▒▓▒▒▓▒▓▒▓▒▒▓█████████▓▓█████████████████▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░▒░▒▒░▒▒▒░░░░▒░░░░░░░░░░░░░░░░░░░░▒▓▒█▒▒▓▒█▒▓░▓▓█████████▓▓██████████████████▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▓▓▓▒░░░▒░░░░░░░░░░░░░░░░░░░░▒▓▒▓▓▒▓▓█▒▓░▓▒▓▓██▓████▓▓███████████████████░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░▒▒▒▒░▓█▓▒░░░░░░░░░░░░░░░░░░░░░░░░▓█▒▓█▒▓▓▓▒▒▒▓▓▒▒▓▓▓▓▓██▓██▓▓██▓█████████████▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░▒▒░░▓█▓▓░░░░░░░░░░░░░░░░░░░░░░░░▓█▒▓█▓▓▓▓▒▒▒█▓▒▒▓▓▓▓▓▓▓▓██▒▓██▓███████████▓██▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░▒▒█▓▒█▓▓▓▓▒▒▒█▓▒▓▓▒▓▓▓▓▓▓██▒▓██▓███████████▓███▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░▒░▒░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▓█▓▓▓▓▓▓▒▒▒█▓▒▓▓▒▓▓▓▓████▒▓██▓██████████▓▓████▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▓██▓▓▓▓▓▓▒▒█▓▒▓█▓▓▓██████▒▓██▓▓█████████▓█████▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒░▓███▓▓▓▓▓▒▒█▓▒▓▓▓▓▓██▓███▒▓██▓▓████████▓▓██████▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░▒▒▓▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░▒▓▒▒▓███▓▓▓▓▓▒▒█▓▒▓▓▓▓▓██▓███▒▓██▓▓████████▓▓▓▓▓▓▓▓▓▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░▒▒▓▓▓▒░░░░░░░░░░░░░░░░░░▒▒▒▓█▓▓███▓▓▓▓▓▒▒█▒▓▒▓▓▓▓▓█▓███▓▓██▓▓████████▓██▓▓▓▓▓▓▓▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░▓████▒░░░░░░░░░░░░░░▒▒▒░▓▓▓▓██▓▓██▓▓▓▓▓▒▒█▒▓▓▓▓▓█▓█▓▓██▓▓██▓▓█████████▓▓▓█▓████▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░▒███▓▒▒▒▒▒▒░░▒░░▒▒▒▒▓▓█▓▓▓█████▓██▓▓▓▓▓▒▒█▒▓▓▓▓▓█▓█▓▓██▓▓██▓▓████████▓▓▓▓▓█▓▓▓▓▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░▒▓███▓▓▓████▓▓▓▓▓▓▓▓▓▓▓███████████▓▓▓▓▓▒▒█▒▒▓▓▓▓██▓▓▓██▓████▒████████▓▓▓▓▒▒▒▓▓█▓▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░▒▓████▓▓██████████████████████████▓▓▓▓█▒▒█▒▒▓▓▓███▓▓███▓████▒▓███████▓▓▒▒▒▓▓▓▓▓▓▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░▓█████▓█████████████████████████▓▓▓▓█▒▓█▒▒▓▓▓██▓█▓███▓▓███▒▓███████▓▓▒▒▒▒▒▓▓▓▓▓▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░▓█▓▓██▓▓█████████████████████████▓▓█▒▓█▒▒██▓██▒█▓███▓▓███▓▓███████▓▒▒▒▒▓▓█████▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░▒█▒▒▓▓▓▓▓▓███████████████████████▓▓█▒▓█▒▒██▓████▓██▓▒▓███▓▓███████▒▒▒▒▒▒▓██████▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓▒▒▒▓▓▓▓▓███████████████████████▓▓█▒▓█▒▒██▓████▓██▓▒▓███▓▓██████▓▓▓▒▒▒▒▓▓██▓███░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒░▒▒▒▒▒▓▓▒▓███████████████████████▓██▒▓█▒▓██▓████▓██▓▒▓███▓▓███████▓▒▒▒▓▒▓▓██████░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒░░▒▒▒░▓▓▓▓▓█████████████████████████▒▓█▒▓██▓████▓██▓▒▓████▓███████▓▓▓▓▒▒▓▓██████░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▒░░▓▓▓▓█████████████████████████▓▓█▒▓██▓▓███▓██▓▒▓████▓███████▓▓█▒░░▓███████░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒░░▒▓▓▓▓████████████████████████▓▓█▒▓███▓███▓██▓▒▓████▓█████████▒▓▒░▒▓██████▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒░░░▒▓▓▓▓████████████████████████▓▓█▒▓███▓███▓██▓▒▓████▓████████▓▒██▒▓███████▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▒▒▓▓▓▓▓███████████████████████▓▓▓▒████▓███▓██▓▒▓█████████████▒▓██▒▓████████░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▒▒▒▒▓▓▓▓█▓█▓██████████████████████▓▓▓▒████▓███▓███▒▓████████████████▓▓█████████▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▓▒▒▓▓▓▓▓██▓██████████████████████▓▓▒████▓██████▓▒▓███████████████▓▓███████████▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▓▒▓▓▒▒▓▓▓▓██▓▓█████████████████████▓▒▓████▓██████▓▒▓████████████████████████████▓▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▓▒▓▓▓▒▓▓▓▓██▓▓████████████████████▓▒▓████▓██████▓▒▓████████████████████████████▓▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▒▒▓▓▓▒▒▓▓█████████████████████████▒████████████▓▓▓████████████████████████████▓▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▒▒▓▒▒▒▓▓████████████████████████▓▒████████████▓▓▓█████████████████████████████▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒░▒▓▒▓▓▓▒▒███████▓█████████████████▓▓█████▓█████▓▓▓▓█████████████████████████████▓▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒░▒▒▒▓▓▓▒██████▓▓▓████████████████▓▓█████▓█████▓▓▓▓█████████████████████████████▓▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒░░▒▒▓▓▓██████▓█████████████████▒████████████▓▓▓▓██████████████████████████████▓▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒░░░░▒▒▓███████████████████████▒▓████████▓██▓▓▓▓███████████████████████████████▓▒▒▒░░▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒░░░░░░░▓██████████████████████▒▓██████▓█▓██▓▓▒▓▓███████████████████████████████▓▓▒░░▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒██████████████████████▓▓█████▓▓█▓██▓▓▒▓▓█████████████████████████████████▓▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒█████████████████████▓▓██████▓█▓██▓▓▒▓███████████████████████████████████▓▒░░░░░░▒▒▒░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▓█████████████▓▓███▓▓▓██████▓████▓▓▒▓████████████████████████████████████▓▒▒░░░▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒░▒▒▒▓████████████▓▓███▓▓████████▓███▓▓▒▓█████████████████████████████████████▓▒░░░▒░▒▒▒░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▒▒░▒▒▒▓██████████▓▓▓███▓▓████████▓███▓▓▓▓███████████████████████████████████████▓▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓▓▒▒░▒▒▒▓██████▓▓▓▓▓▓███▓▓▓▓██████▓███▓▓▒▓█████████████████████████████████████████▓▒░░░▒▒░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▓▒▒▒░░▒▓█████▓▓▓▓▓▓███▓▓▓███████▓████▓▓▓███████████████████████████████████████████▓▒▓▒▒▒░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒░░░▒▓██▓▓█▓▓▓▓▓███▓▓▓███████▓████▓▓▓█████████████████████████████████████████████▓▓▓▓▒░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒░░░▒▓▓▓▓██▓▓▓▓███▓▓▓████████████▓▓▓▓██████████████████████████████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒░░░▒▓▓▓███▓▓▓███▓▓█████████████▓▓▓▓██████████████████████████████████████████▓▓▓▓▓▓▓▓▓▓▓▓▒░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓▓▓███▓▓▓███▓▓█████████████▓▓▓▓████████████████▓███████████████████████████████████▓▒░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓▓███▓▓▓███▓▓██████████████▓▓▓████████████████░█████████████████████▓▓▓▓████████▓▒░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓███▓▓▓███▓▓██████████████▓▓▓████████████████░▓████████████████████▒░░░░▓▓▓▒░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓▓██▓▓▓███▓▓██████████████▓▓▓████████████████▒░████████████████████▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▓██▓▓▓████████████████████▓▓████████████████▒░▒▒▓▒▒▓█████████████▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▓▓▓▓▓▓████████████████████▓█▓███████████████▓░░░░░░░▒███████████▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▓▓▓▓▓█████████████████████▓█▓███████████████▓░░░░░░░░▒▓▓▒▓▓██▓▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▓▓▓▓▓█████████████████████▓█▓███████████████▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▓▓▓▓▓█████████████████████▓█▓████████████████▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▓▓▓▓▓█████████████████████▓██▓███████████████▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▓▓▓▓▓▓██████████████████▓▓▓██▓██████████████▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▓▓█████▓▓████████▓▓▓▓▓█▓▓████████████▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▓▓▓▓░░░░▒░░░░░▒▒▓▓▓▓▓▓▓▓████████████▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓▓▓▓▓▓▓▓████████████▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▓▓▓▓▓▓███████████▓▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒████████████████▓▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░██████████████▓▓▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░█████████████▓▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░");
                break;
            case "Painforged Emissary":
                Console.WriteLine(@"
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▓▓▒▓▓▓▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓▓▓▓▓██████▓▓▓▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓▓██████████▓▓▓▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓████████████▓▓▓▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓████████████████▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓███████████████▓▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓████████████████▓▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓▓████████████████▓▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓█████████████████▓▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓██████████████████▓▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓████████████████████▓▓▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▓▓███████████████████████▓▓▓▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▓▓▓▓█████████████████████████▓▓▓▓▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░██▓▓▓▓▓█████████████████████████▓███▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░████████████████████████████████████▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒█████████████████▓▓█████████████████▓▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒████████▓█▓▓▓███████▓▓▓▓▓▓██████████▓▓▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▓█████████▓▓▓▓▓▓███▓▓▓▓▓▓▓▓██████████▒▒▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓▓▓▓██████████▓▓▓▓▓████▓▓▓▓▓▓▓▓▓████████▓▓▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▒▓██████████▓▓▓▓▓████████████▓█████████▓▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓▓░██████████████████████████████████████▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓█████████████████████████████████████████░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░█▓████████████████████████████████████████▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒█▓▓███████████████████████████████████████▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓▓██████████████████████████████████████████░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓███████████████████████████████████████████▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒████████████████████████████████████████████▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒███████████▓▓█████████████████████████████████▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░███████▓█▓▒█▓███████████████████████████████████▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓██████▓▒█▒▒▓█████████████████████████████████████░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓██████▒▒▓▒█▒██████████████████████████████████████▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒████▓▓▓░▓▓▒░▓█████████████████████████████████▓█████░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒████▒░▓█▒▓▓░░▓█████████████████████████████████▓▒████▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░███▓░░░▓█▓▓▓░░█████████████████████████████████▓█░░▓███▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓██▒░░░▓█▒▓▓▒░▒███████████████████████████████▒▒▒█░░▒▒▓██▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓██░░▒░▓▓░▒▓▒▒░████████████████████████████████▒░▒█░░░░░▒██▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓██░░░▒█▒░░░█░░▒██████████████████████████████▓█▓░░▓▓░░░░░▒██░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░███▒░░░██░░░▒▓░░▓█████████████████████████████████░░░█░▒░░░░▓██▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓███▒░▒▓░▓▒░░▓▒░░████████████████████████████████▓█▓░▒█▓▓░░░░▓▓██▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░█████▒░▒▒░▓█░░▓▓▒▒███████████████████████████████▓▒▒█▓▒▓▓▒░░░░▓▒███▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░██████░░░▒▒▓██▓▒▒▓████████████████████████████████▓▒▒▓▓░▓▓░░░░░▒▒████░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░███▓▒█▒░░▒▓░░▓█▒▒▒▓█████████████████████████████████▓▒░▓▒▓▒░░▒▒░░░█████░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░██▓░█▓░░░▒░░░▓▓░▒▒▓█████████████████████████████████▓▓▒█▒▒▒▒░░░░░░▒▓████░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒██▓▒▓░░░▒░░░░▓▓▒░░▓█████████████████████████████████▓█▓▓█▓▓░░░░░▒░░░█▓██▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓█▓▓▒░░░▒░░░░▓█▓░░██████████████████████████████████▓▓█████░░░░░▒░░░▒███▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░█▓▓▒░░░░▒▒░▒▓█▒▒▒███████████████████████████████████▓█████░░░░▒▒░░░░███▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒░░░░▒▒░░░░▓▓▓▒▓██████████████████████████████████▓▒▓██▒█▓▒▒▒▒▒▒░▒▓▓█▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▓▓███████████████████████████████████▒▓█▒▓▒▓▒░░░▒▒▒░░░▓▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▓████████████████████████████████████▓▓█░▓▒░░▒░░░░▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓██████████████████████████████████████▒▓▒▓▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒███████████████████████████████████████▒▒▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓███████████████████████████████████████▒▓▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒█████████████████████████████████████████▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒██████████████████████████████████████████▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒████████████████████████████████████████████░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒█████████████████████████████████████████████▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓██████████████████████████████████████████████▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒███████████████████████████████████████████████▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░█████████████████████████████████████████████████▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░███████████████████████████████████████████████████░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓████████████████████████████████████████████████████░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒██████████████████████████████████████████████████████▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░█████████████████████████████████████████████████████████░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░███████████████████████████████████████████████████████████▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░█████████████████████████████████████████████████████████████▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓███████████████████████████████████████████████████████████████████▓▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒████████████████████████████████████████████████████████████████████████████▓▓▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▓▓▓▓█████████████████████████████████████████████████████████████████████████████████████████▓▓▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░▒▒▒▓▓▓███████████████████████████████████████████████████████████████████████████████████████████████████████████████▓▒▒░░░░░░░░░░░░░░░░░░░░░
░░▒▓█████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████▓▓▓▓▒▒░░░░░░░░░░░
▓████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████▓▒░░░░░░░
████████████████████████████████████████████████████████████████████████████████████████████████████████████▓▒▒▒████████████████▓███████████████▓▒░░░░
████████████████████████████████████████████████████████████████████████████████████████████████████████████▓▒▒░▓██▒▓▓▒▓▓▓▓▒▓▓▓▓▒▒▓▓▓▓▓▓▒▒▓▓███████▓░░
████████████████████████████████████████████████████████████████████████████████████████████████████████████▒▓█▓▓████████▓██▓█▓████▓██▓█▓████████████▒
██████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████
██████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████
██████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████
█████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████▒");
                break;
            case "Bleeding Orchardgeist":
                Console.WriteLine(@"
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒░▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒███▓▓█▓▒▒▒▒▓▓▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓████▓▓▒▓▓▒▒▒▒▒▒▓▓▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓████▓▓▒▓█▓▓▒▒▒▓▓▓▓▓▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒████▓▓▓█▓████▓▒▓▓▓▓▓▓▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓███████████████▓▓██▓▓▓▓▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒███████▓▒▒▒▒▒▒▓▓████████▓▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒████████▓▒▒▒▒▒▒▒▓██▓██████▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░████████▓▒▒▒▒▒▒▒▒████▓▓▓██▓▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓███████████▓▓▓████▓█▓▓███▓▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓███████████▓▒▒▓▓▓▓▓█▓██████▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓████████▓▓▓▓▓▒▒▒▓▓▓██████████▓▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒██████████▓▓▓██▓█▓▓██████▓▓▓████▓▓▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▓███████████████▓▓▓████████▓▓▓▓▓▓▓█▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓███████████████████▓████▓████▓▒▒▒▓▓▓▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒███▓█████████████▓▓▓███▓▓████▓▒▓████▒▒▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓█████▓████████████████▓▓█▓▓█▓█████▓▓▓▓▒▒▒▒▒▒▒▒▓▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓█████████████████████████▓▓█▒▒▓▓███▓▓▓▓▓▓▒▓▒▒▒▒▒▒▓▓▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▓▓▓▓████████████████▓▓▓█████▓▓▓██▓▓██▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓▓▓▓▓▓▓▓▓███████████████▓▓▓██▓▓▓▓▓█▓█▓▓▓▓▓▓▓██▓▓█▓▓▓▓█▓▓██░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░█████▓▓█████████████████▓▓▓▓█▓▓▒▒██▓▓██▓▓▓▓▓▓▓▓▓█▓████████▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░██████████████████████▓▓▓▒▒▒▓▒▒▒▒▒▓█▓▓▓█▓▓▓▓▓▓▓███████████▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒█████████████████████████▓▒▒▒▒░░░▒▒▓▓▓██▓▓▓▓▓▓▓███████████▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒███████████████████████▓▓▒▒▒▒▒▒▒▓███▓███▓▓▓▓▓▓█████████████░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░████████████████████████▓▓▒▒▒▒▒▒▓█████████▓▓▓██████████████▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒█████████████████████████▓▓▒▓▓▒▓▓▓█████████▓██████████████▓▓▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒█████████████████████████▓▓▓▓▓▓▓██████████████████████████▓▓▓▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░█████████████████████████████▓▓███████████████████████████▓▓▓▓█░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░████████████████████████████████████████████████████▓██████▓▓▓▓██░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒███████████████████████████████████████████████████▒░▓███████▓▓██▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓███████████▓███████████████████████████████████████░░░▓███████████▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░███████████▒░▒█████████████████████████████████████▓░░░░▓██████████▓▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓██████████▓░░░█████████████████████████████████████░░░░░░▓████████▓▓█▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒██████████▓░░░░░███████████████████████████████████▓░░░░░░░▓██████▓▓▓▓█▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒██████████▓░░░░░░▓██████████████████████████████████░░░░░░░░░█████▓▓▓▓▓▓█░▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░███████████▒░░░░░░▒██████████████████████████████████▒░░░░░░░░▒███▓▓▓▓▓▓▓███░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓░███████████▒░░░░░░░▒██████████████████████████████████▓░░░░░░░░░███▓█▓▓▓▓▓███░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒█████████████░░░░░░░░▓███████████████████████████████████▒░░░░░░░░▓████▓▓▓▓▓███░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒████████████░░░░░░░░░█████████████████████████████████████░░░░░░░▒▓███▓▓▓▓▓▓███░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░███████████▓▒▒░░░░░░▓██████████████████████████████████████░░░░░░░░▓████▓▓▓▓▓██░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒███████████▓░░░░░░░▓████████████████████████████████████████░░░░░░░░░▓███▓▓▓▓██░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓█████████▒░░░░░░░░░█████████████████████████████████████████▒░░░░░░░░░▒██▓▓▓▓▓█░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░████████▓░░░░░░░░░░▓█████████████████████████████████████████▒░░░░░░░░░░░██▓▓▓▓█▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░███████▒░░░░░░░░░░░██████████████████████████████████████████▓░░░░░░░░░░░░██▓▓▓█▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒██████▒░░░░░░░░░░░▒███████████████████████████████████████████░░░░░░░░░░░░▓█▓▓▓█▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░██████▒░░░░░░░░░░░░█████████████████████████████████████████████░░░░░░░░░░░▒▓▓▓███░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒█████▓░░░░░░░░░░░░▒█████████████████████████████████████████████▓░░░░░░░░░▒▓▒▓▓▓██░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓█████░░░░░░░░░░░░░▓██████████████████████████████████████████████░░░░░░░░▓▓▓▓▓▓▓▓▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓█████▒░░░░░░░░░░░░███████████▓▓▓▓█████████████████████████████████░░░░░░▒▓▓▓▓▓▓▓▓▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓██████▒░░░░░░░░░░▓████████▓▓▓▓▓▓▓▓████████████████████████████████▓░░░░░▒▓▓██▓▓▓▓▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓███████░░░░░░░░░░██████████▓▓▓▓▓▓▓▓████████████████████████████████▓░░░░▒▓▓░░▓▓▓█▓▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓███████░░░░░░░░░▓█████████▓▓▓▓▓▓▓▓▓█████████████████████████████████▒░░░▒▓▒░░▓▓███▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░████▒▒██░░░░░░░░░███████████▓▓▓▓▓▓▓███████████████████████████████████▒░░░▒░░░▓▓██▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░███░░██▓░░░░░░░██████████████▓▓█▓▓████████████████████████████████████░░░░▒▓████▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒███▒▒▓▓░░░░░░▓████████████████▓███████████████████████████████████████░░▒███▓▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓███▓░░░░░░░██████████████▓▓▓▓▓▓▓▓███████████████████████████████████▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▓░░░░░░▓███████████████▓▓▓▓▓▓▓████████████████████████████████████▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░████████████████▓▓▓▓▓▓▓█████████████████████████████████████▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓█████████████████▓▓▓▓▓▓██████████████████████████████████████▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒████████████████████▓▓▓▓███████████████████████████████████████░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓███████████████████▓▓▓▓▓████▓▓██████████████████████████████████░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒██████████████████▓▓▓▓▓▓▓███▓▓▒▓██████████████████████████████████░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░████████████████████▓▓▓▓▓▓███▓▒▒▒███████████████████████████████████░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓███████████████████▓▓▓▓▓▓████▒░░░████████████████████████████████████░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░████████████████████████▓█████▒░░░████████████████████████████████████▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓██████████████████████████████▓░░░█████████████████████████████████████▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░███████████████████████████████▓░░░▓█████████████████████████████████████▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒███████████████████████████████░░░░░██████████████████████████████████████░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░███████████████████████████████▓░░░░░██████████████████████████████████████▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░███████████████████████████████▓░░░░▒█████████████████████▓▓████████████████░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒██████████▓█████████████████████░░░░░████████████▒░░░▒▓██░░░░▒▓██████▒▒▒████▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓█████████▒░▒█▓░░▒▓██████████████░░░░▓████████████░░░░░░░▓░░░░░░░███▓░░░░░▓███░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓█████████▒░░▓░░░░░██████████████▓░░░████████████▓░░░░░░░░░░░░░░░▓██░░░░░░░▓██░░░░░░░░░░░░░░░░░░░░░░░░░░░░░");
                break;
            case "Weeping Rosecutter":
                Console.WriteLine(@"
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒░░░▓▓▓▓▓▒░░░▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒░▒▒░▓█████████▓░▒░░▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒█████████████▓▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒░▓▒▒▒▒█████▓▓▓██████▓▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▒▓▒▓████▓▓▓▓███████▓▒▓▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒░▓▓▓▒▒▓████▓▓▓▓▓███████▓▓▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▓▓▓▒▒██████▓▓█████████▓█▓▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▓▓▓░▓██████▓██████████▓█▓▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▓▓▓▓░███████▓▓█████████▓██▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▓▓▓▒▒██████▓▒▓▓▓███████▓██▓▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▓▓▓▒▓██████▓▒▒▓████████▓██▓▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▓▓▓▓░▓████▓▓▓███▓▓███████▓█▓▓▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▓▓▓▓░████████████████████▓█▓▓▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▓▓▓▓▒▒███████▓████████████▓██▓▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▓▓▓▓▒▓████████▓███████████▓██▓▓▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒░▒▒▒▓▓▓▓▒▓████████████████████▓██▓▓▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▓▓▓▓▒▒█████████████████████▓███▓▓▒▒░▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒░▒▒▓▓▓▓▓▒▒█████████████████████▓▓██▓▓▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒░▒▒▓▓▓▓▓▒▓█████████████████████▓▓██▓▓▒▒▒░▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒░▒▓▓▓▓▓▓▓▓██████████████████████▓███▓▓▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒░▒▒▓▓▓▓▓▓▓▓██████████████████████▓███▓▓▒▓▓░▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▓░▒▓▓▓▓▓▓▓▓███████████████████████▓▓██▓▓▒▓▓▒░▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒░░▒▓▒▒▒▓▓▓▓▓▓▓▓███████████████████████▓▓██▓▓▒▓▓▓▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▓▓▒▒▒▓▓▓▓▓▓▓▓███████████▓▓███████████▓▓▓█▓▓▒▓▓▓▒▓▓▓▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▓▓░▒▓▓▓▓▓▓▓▓▓█████████▓▓▓▓▓▓█████████▓▓██▓▓▒▓▓▓▒▒▓▓▓▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒░▒▓▒▒▒▓▓▓▓▓▓▒▓██████████▓▓▓▓▓▓▓████████▓▓█▓▓▓▓▒▓▓▒▒▓▓▓▓▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒░▒▓▒▒▒▓▒▓▓▓▓▒▓██████████▓▓▓▒▒▒▓▓███████▓▓█▓▓▓▓▒▓▓▓▒▓▓▓▓▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▒▓▒▓▓▓▓▓▒▓██████████▓▓▓▒▒▒▒▒▓▓██████▓▓██▓▓▓▒▓▓▒▒▓▓▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▒▓▒▓▓▓▓▓▓███████████▓▓▓▒▒▒▒▒▒▓██████▓▓▓█▓▓▓▒▓▓▒▒▓▓▓▓▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓███████████▓▓▓▒▒▒▒▒▒▓▓█████▓▓▓▓▓▓▓▓▒▓▒▒▓▓▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒░▒▒▒▒▓▒▓▓▓▓▓▓▓█████████▓▓▓▒▒▒▒▒▒▒▒▒▓█████▓▓▓▓▓▓▓▓▒▓▓▒▒▓▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓████████▓▓▓▒▓▒▒▒▒▒▒▒▒▒▒▓█████▓▓▓▓▓▓▓▓▒▓▓▒▓▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▓▒▓▓▓▓▓▓▓▓██████▓▓▓▓▓▒▒▒▒▒▒▒▒▓▓▓▓█████▓▓▓▓▓▓▓▓▒▒▓▒▓▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▒███████▓▓▓▓▓▓▒▒▒▒▒▓▓▓▓▓▓█████▓▓▓▓▓▓▓▒▒▒▓▒▓▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒░▒▒▓▓▓▓▓▓▓▓█████▓▓▓▓▓▓▓▓▓▒▒▒▓▓▓▓▓▓▓██████▓▓▓▓▓▓▒▒▒▒▒▓▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓█████▓▓▓▓▓▓▓▓▒▒▓▓▓▓▓▓▓▓▓▓▓████▓▓▓▓▓▓▓▒▒▒▓▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓████▓▓▓▓▓▓▓▓▓▒▓▓▒▓▓▓▓▓▓▓▓▓▓███▓▓▓▓▓▓▓▒▒▒▓▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▓░▒▒▓▓▓▓▓▓▓▒▓███▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓██▓▒▓▓▓▓▓▒▒▒▒▒▒▓▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒░▒▒▓▓▓▓▓▓▓▒▓██▓▓▓▓▓▒▓▓▒▒▒▒▒▒▒▓▓▒▓▓▓▓▓▓▓▓▓▓█▓▓▓▓▓▓▒▒▒▒▒▒▓▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▒▓██▓▓▓▓▓▒▓▒▒▓▓▒▒▒▒▓▓▒▓▓▒▓▓▓▓▓▓▓█▓▒▒▓▓▓▒▒▒▒▒▒▓▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▓▓▓▓▓▓▓▒▒▓█▓▓▓▓▓▓▒▒▒▒▓▒▒▒▓▒▓▓▒▓▓▒▓▓▓▓▓▓▓▓▓▒▒▓▒▓▓▒▒▒▒▒▒▓▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒░▒▒▒▒▒▒▒▒▓▓▒▒▒▒▒▒▓█▓▓▓▓▓▓▓▒▒▒▓▒▒▓▓▒▓▓▒▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▓▓▓▒▒▓▒▒▓▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒░▒▒▒▒▒▒▒▒▓▒▒▒▒▒▒▒▓█▓▓▓▓▓▓▓▒▒▓▓▒▒▓▓▒▓▓▒▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▓▓▓▓▒▒▒▒▒▓▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒░▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▒▒▓▓▓▒▓▓▓▒▓▓▒▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▓▓▓▓▒▒▒▒▓▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒░▒▓▒▒▒▒▒▒▒▓▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▒▓▓▓▒▓▓▓▒▓▓▒▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▓▓▓▓▓▒▒▒▒▒▓▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▓▒▒▒▒▒▓▒▓▒▓▒▒▒▒▒▒▒▒▒▓▓▒▓▒▓▓▓▒▓▓▓▒▓▓▒▓▓▓▓▓▓▓▓▒▒▒▒▒▒▓▓▓▓▓▓▒▒▒▒▒▓▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▓▒▒▒▒▒▓▒▒▒▓▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▒▓▓▓▒▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▓▓▓▓▓▓▓▓▒▒▒▒▓▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▓▓▒▒▒▒▓▓▒▓▓▒▒▓▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▒▒▒▒▓▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▓▓▓▒▓▒▒▓▓▓▓▓▒▒▓▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▓▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▓▓▓▒▓▒▒▓▒▓▓▓▒▒▓▓▒▒▒▒▒▒▒▒▒▓█▓▓▓▓█▓▓▓▓▓▓▓▓█▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▓▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▓▓▓▒▒▒▒▓▓▓▓▓▒▓▓▓▒▒▒▒▒▒▒▒▒▓█▓▓▓▓█▓▓▓▓▓▓▓▓▓▓▓▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▓▓▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▓▓▒▒▒▒▓▓▓▓▓▒▓█▓▒░▒▓▓▓▓▓▓▓▓███▓█▓▓▓█▓▓▓▓▓▓▓████▓▓█▓▓▓▓▓▓▓▓▓▒▒▒▓▓▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▓▓▒▓▒▒▒▓▓▓▓▒▓██████████▓▓▓▓████▓▓▓█▓█▓▓▓██████████▓▓▓▓▓▓▓▓▒▒▒▓▓▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▓▓▒▓▒▒▒▓▓▓▓▒███████████████████▓▓▓██▓▓▓███████████▓▓▓▓▓▓▓▓▒▒▒▓▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▓▓▒▓▒▒▓▓▓▓▓▒███████████████████▓███▓▓█████████████▓▓▓▓▓▓▓▓▒▒▒▓▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▓▓▒▓▒▒▓▓▓▓▒▒████████████████████▓▓████████████████▓▓▓▓▓▓▓▓▓▒▒▓▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▓▓▒▓▒▒▓▓▓▓▒▓█▓██▓██▓▓████████████████████████████▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▓▓▒▓▒▒▓▓▓▒▒▓████▓█▓▓▓███████████████████████████▓█▓▓▓▓▓▓▓▓▓▓▒▒▓▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▓▓▒▒▒▓▓▓▓▒▒█▓██▓▓█▓▓▓███████████████████████████▓█▓▓▓▓▓▓▓▓▓▒▒▒▓▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▓▓▒▒▒▓▓▓▓▒▒█▓██▓▓█▓▓▓███████████████████████████▓█▓▓▓▓▓▓▓▓▓▒▓▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▓▒▒▓▒▒▓▒▒▓█▓██▓▓█▓▓▓████████▓█▓▓▓████████▓█▓███▓█▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▓▒▒▓▒▒▓▓▒▓█▓██▓▓█▓▓▓▓▓█▓▓█▓█▓▓▓▓▓████████▓▓▓▓█▓▓█▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▓▒▒▓▒▓▓▓▒▓█▓██▓▓▓▓▓▓▓▓▓▓▓▓▓█▓▓▓▓▓▓████▓██▓▓▓▓█▓▓█▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▓▒▒▓▒▓▓▒▒██▓██▓▓▓▓▓▓▓▓▓▓▓▓▓█▓▓▓▓▓▓█▓██▓▓█▓▓▓▓█▓▓█▓▓▓▓▓▓▓▒▓▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▓▒▒▓▒▓▓▒▒██▓██▓▓▓▓▓▓▓▓▓▓▓▓▓█▓▓▓▓▓▓█▓██▓▓█▓▓▓▓█▓▓█▓▓▓▓▓▓▓▒▓▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▒▓▒▒▓▓▓▓▒▓▓█▓██▓▓▓▓▓▓▓▓█▓▓▓▓▓▓▓▓▓▓▓█▓▓█▓▓█▓▓▓▓█▓▓█▓▓▓▓▓▓▓▒▓▓▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▒▓▒▒▓▓▓▒░▓██▓██▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓█▓▓█▓▓█▓▓▓▓█▓▓█▓▓▓▓▓▓▓▒▒▓▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▒▓▒▒▓▓▒▒▒▓██▓██▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓█▓▓█▓▓█▓▓▓▓█▓▓▓▓▓▓▒▓▓▓▒▒▓▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▒▓▓▒▓▓▒▒▒▓██▓██▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓█▓▓█▓▓█▓▓▓▓▓▓▓▓▓▓▓▒▒▓▓▓▒▓▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▒▓▓▒▓▓▒▒▒▓██▓██▓▓▓▓▓▓▓▓█▓▓▓▓▓▓▓▓▓▓▓█▓▓█▓▓█▓▓▓▓▓▓▓▓▓▓▓▓▒▓▓▓▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▒▓▓▒▓▓▒▒▓▓██▓██▓▓▓▓▓▓▓▓█▓▓▓▓▓▓▓▓▓▓▓█▓▓█▓▓█▓▓▓▓▓▓▓▓▓▓▓▓▒▒▓▓▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▒▓▓▒▓▓▒░▓▓██▓██▓▓▓▓▓▓▓▓█▓▓▓▓▓▓▓▓▒▓▓▓▓▓█▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▓▓▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▒▒▓▓▒▓▓▒▒▓▓██▓██▓▓▓▓▓▓▓▓█▓▓▓▓▓▓▓▓▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▓▒▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▒▓▓▓▓▒▒▒▒▓▓████▓▓▓▓▓▓▓▓▓█▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▒▒▒▓▓▓▒░▒▓▓▓█▓██▓▓▓▓▓▓▓▓▓█▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▒▒▓▓▓▒▒▒▓▓██▓▓▓▓▓▓▒▓▓▓▓▓█▓▓▓█▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▒▒▓▓▓▓▒▒▓▓██▓▓▓▓▓▓▒▓▓▓▓▓█▓▓▓█▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓██▓▓▓▓▓▓▒▓▓▓▓▓█▓▓▓█▓▓▓▓█▒▓▓▓▓▓▓▓█▓▓▓▓▒▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓██▓▓▓▓▓▓▒▓▓▓▓▓█▓▓▓█▓▓▓▓█▓▓▓▓▓▓▓▓██▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓█▓▓▓▓▓▓▒▓▓▓▓▓█▓▓▓█▓▓▓▓█▓▓▓▓▓█▓▓█▓▓▓▓▓▒▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓█▓▓▓▓▓▒▒▓▓▓▓▓█▓▓▓█▓▓▓▓█▓▓▓▓▓█▓▓█▓█▓▓▓▒▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▒▓▒▓▓▓▓▓▓▓▓▓▓██▓▓▓▓▒▒▓▓▓▓▓█▓▓▓█▓▓▓▓█▓▓▓▓▓▓▓▓█▓█▓▓▓▒▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓███▓▓▓▓▒▒▓▓▓▓▓█▓▓▓██▓▓▓█▓▓▒▓▓█▓▓█▓█▓▓▓▒▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▒▓▒▓▓▓▓▓▓▓▓▓▓██▓▓▓▓▒▒▓▓▓▓▓█▓▓▓██▓▓▓█▓▓▓▓▓▓▓▓█▓█▓▓▓▓▓▓▓▓▓▓▓▒▒▒▓▓▒▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒░▒▒▒▒▒▒▒▒▓▒▓▓▓▓▓▓▓▓████▓▓▓▓▒▒▓▓▓▓▓█▓▓▓███▓▓█▓▓▓▓▓▓▓▓█▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▓▓▒▒▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▒▓▒▓▓▓▓▓▓▓█▓███▓▓▓▓▒▒▓▓▓▓▓█▓▓▓███▓▓█▓█▓▓▒▓▓▓█▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▓▓▒▒▒▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▒▒▓▒▓▓▓▓▓▓▓█████▓▓▓▓▒▒▓▓▓▓▓█▓▓▓███▓▓█▓▓▓▒▒▓▓▓███▓▓▓▓▒▓▓▓▓▓▓▓▓▒▒▓▒▒▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▒▒▒▓▒▒▓▒▓▓▓▓█████▓▓▓▓▒▓▓▓▓▓▓█▓▓▓███▓▓██▓▓▓▒▓▓▓█▓█▓▓▓▓▒▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▒▓▒▓▒▒▒▓▓▓▓▓█████▓▓▓▓▒▓▓▓▓▓▓█▓▓▓██▓▓▓▓█▓▓▓▒▓▓▓█▓▓▓▓▓▓▒▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▒▒▒▓▒▒▒▓▓▓▓▓▓████▓▓▓▓▒▓▓▓▓▓▓▓▓▓▓▓██▓▓▓█▓▓▓▒▒▓▓█▓█▓▓▓▓▓▓▓▓▒▓▒▓▓▓▒▒▒▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒░▒▒▒▒▒▓▒▒▓▓▓▓▓▓▓███▓▓▓▓▓▒▓▓▓▓▓▓▓▓▓▓▓▓█▓▓▓█▓▓▓▓▒▓▓▓▓█▓▓▓▓▓▓▓▒▒▓▓▒▒▓▒▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▒▒▒▒▓▒▒▓▓▓▓▓▓▓▓▓█▓▓▓▓▓▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▓▓▓▓█▓▓▓▓▓▓▓▒▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒░░░▒▒▒▒▒▒▒▓▒▓▓▓▓▓▓▓▓█▓▓▓▓▓▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▓█▓▓█▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▒▒▓▓▓▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒░▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▒▒▒▓▓▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒░░▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░▒▒░▒░░░░░░░░░▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒░░▒▒▒▒░░░░░░░░░▒░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▒▒▒░▒░▒▒▒▒▒▒▒░░░▒▒▒▒▒▒▓▒▓▓▓▓▓▓▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒░░░░▒░░░░░░░░░░░▒░░░░░░▒░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▒░░░░░░▒▒░░░░░░░░▒▒▒▒▒▓▒▓▓▒▓▓▒▒▒▒▒▒▒▒▓▓▓▒▒▓▒▒▓▒▒▓▓▓▓▓▓▓▓▓▓▒▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒░░░░░░░░▒▒▒▒▒▒▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▒▓▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒░░▒▒▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒░░░░░▒▒▒░░░░▒▒▒▒░░▒░░▒▒░▒▒▒▒▓▓▒▒░░░░░▒▒▒▒▒▒▒░░░░░▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒░▒▒░░▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒░▒░░▒▒░░░░░░░▒▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░");
                break;
            case "Fiery Treant":
                Console.WriteLine(@"
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒░░░░░░░▒░░▒░░░▒░▒░░▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒░░░░░▒░░░▒░░▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓█▓░░▒▓░░░░▒▒░░▒░▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒░░░░░░░░▒▒▒░░░░░░░░░░░░░░░░░░░░██▒▒▓█▒░░▒░░░▓▓░░▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓░░░░░░░░▒░░▓▒▓▒░░░░░░░░░░░░░░░░█▓░░░██▓░░░░▓▓▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓░░░░░░░░▒░░░▒█▓▒░░░░░░░░░░░░▒██▒▒░░███▓▒▓██▓▓▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓░▒░░░░░░▒░░▒▓▓▓▓▓▒▒░░░░░░░▓▒░█░█▒▓█▓████▓▓█▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓▓▓▓░░░░░▒▒░▓█░▒░░▒▓▒░░░░▒▒░░▒▒▒▓██▓▓████▓██▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓███▓▒█░░▓█▓█▒░▒▒▓▓░░░░░▒░▓▓░▒▓▒██▓▒▒▒▓███▓▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓▒░▒▒███▓▓███▓▓██▓░░░░░░▒▓▓▓█████▒▒▒▒▓▓██▓▒▓▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒░░▒▒▒▓█▓▓▓▓░▓████▓█▓░░▒░░░░░░░░░░░████▓▓▓▓▒▓███████▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓░░░▒▓████▒▒▓███▒▒▓░░▒░░░░░░░░░░▒▒▓███▓▒▓▒▒████▓▓▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓░░░▓████████▓▓▒░░░▒░░░░░░░░░▒▒░░▒█▓█▓▓▓████▓▓▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓█▓▒▒░░▒▓███▓▓▒░░▒░░▒▒▓▓▓▒░░▒░░░██▓████████▓▓▓▓▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒█▒░░░░░░▒▒▓██▓█▒░▓▓▒▓█████▓▒█▒░▓▓████▓▓▒░░░░░░░▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▓▓▒▒░░░░░░░░▒▓██▓▓▓▓████▓▓▓████▓▓▓██▓▓▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▒░░░░▒░░░░░░░▒▓████████▓▓▓▓▓███▓▓██▓▓░░░░░░░░░░░░░▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒░░░░░░▓▒░▒░▒▓▒████████▓▓▒▒▓████████▓▒▒░▓▓▒▒░░░░░░▒░░░░░▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓░▒░░░░░░▒▓▓▓▓▓▓▓███████▓▒░▒▓▓████▓▒▒▒▓▓█▓▒░░░░░░░░▒▓▓▓▒▓▓▓▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓▒░▒░░░░▒▓░░░▒▓█████████▓▒░░░▒▓█████████▓▒▒░░░░░░░░░▒▓▓▓▓▓▓▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒░▓▒░▒▒░░░░░▒▓██████▓▒▒░░▒▒▒▓██████▓▒▒░░░░░░░░░░░▓▓▓▓▓▓▓░▒▓▓▒░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▓░░░░▒░░░░░░░░▒▒▒▓███▓▒▒▒▒▒▒▓███▓▓▓▒▒░░░░▒▓░░▒░░░░▓▓▒▒▓▒▓██▓▒░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓█▓▓▒░▒░░░░░░░░░▓▒▒▓████▓▒▒▓▓█████▒░▒▒░░░░▒▒░░░░░░░▓▓▓▓▓▒▓▓▓▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒█▓░░▒░░░░░░░░▒▓█▓███████▓████████▓▓▓▓▒▒▓▓▓░░░░░▒▒▒▒▓▓▓▓▓▓▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▒▓▒░▒▒▒▓▒▒▒▓█▓████▓█████████████▓██▓█▓█▓▓█▓▓▒▒░▒▒▒▒▓▓▒▒▒▓▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▓▓▓▒▓▒░░░░░▒▓▓▓█▓▓▓█████▓█████████████▓▓▓▓██████▓▓░░▒░░░░▓▓▒▒▒▓▓▓▓▓░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒░░░░░▓▓▓██████████▓██████████████▓▓████████▒░▓▓░▒░▒▒▒░░░▓▓▓▓▓▒░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓████████████▓█████████████▓███████████▓▓▒▒░░░░░░░▒▒▒▒▓▒░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▓▓▓▒▒▒▒▓▓▒░▒▓▒▒▓████████████▓█████████████▓▓███████████▒░░░░░░░░░░▒▒▒▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒░▒▒▒▒▓▓▒▓▓▒▒▓█▓▓███████████▓▓███████████████████████▒██▓▓▒░░░░░▒▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▓▓▓▓▓▓▓▓▒▒▓████▓▓██████████▓███████████▓████▓███▓█████▓▓▓▒▒░▒▓▓▒▒▒▒▓▒░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▓▒░░░░▒▓▓▓▓▓▓▓▓▓▓▓▓█▓██▓▓▓██████▓███▓██████▓████▓███████████████▓▓▓▒▒▓▒▒▓▒▒▒▓░▒▓▒░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▓▓▓▓▓▒▓▒▒▒▓▓▓▓▓▓▓█▓▓▓▓████▓▓██████████▓██████▓▓████▓██████████▓▓████▓▒░▓▓▓▓▒▒▓▒▓▓▓▓▒░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▓▓▒▓▓▓▓▓▓▓▒▓▓▓▓█▓█▓▓▓▓▓██████████████████████████████▓████▓█████████████▓█▓▒▓█▓▓▒▒▓▓░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓█▓▓▓▓███████████████████████████████▓▓▓▓▓▓██▓▓▓▓█████████▓████▓▓▓▓░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓▓▓▓▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓████████████████████████████████▓██▓▓▓▓███▓▓▓▓▒▒▒▓█████████▓█▓█▓▓▓░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓▓▓▒▒▓▓▒▒▓▓▓▓▓██▓▓▓▓███████████████████████████████████▓▓▓▓██████████▓▓████████▓███▓▓▓▓░░░░▒▒░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▓▒▒▒▒▒▓▓▓▓▓█████▓███▓██████████████████████████████▓▓▓▓██████████████▓▓▓▓▓▓▓▓███▓▓█▓▓▓▓▓▓▓▒░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓████▓▓███▓████████▓▓████████████████▓▓▓▓▓▓█████████████▓▓▓▓▓▓▓▓▓▓▓████▓▓█▓▓▓▓▓▒▒░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓███▓▓███████████████████████▓▓▓▓█▓████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓██▓▓█▓██████▓▒▒▒░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓███▓▓█████▓▓▓▓████▓█████████▓███████████▓███████████▓▓▓▓▓█▓▓▓▓▓▓▓█▓███▓▓▓▓▓████▓▓▓▒░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▓▓▓▓██████▓▓▓██████████▓▓▓████████████████▓▓████▓█▓██████████████▓▓██▓▓██▓▓▓▓▓▓█▓█████▓▓▓████▒░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▓▓▓▓▓██████████████████████▓████████████████████████████▓█████████████▓▓▓█▓██████▓███████████████▓▒░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓▓▓▓▓█▓███████████████▓█▓██▓▓▓▓████████▓▓▓▓▓████▓▓███████▓████████████▓▓▓█▓██████████████████████████▓▒░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓▓▓▓▓██████▓████▓▓███████▓▓▓▓▓▓▓▓▓█████▓██▓██████████████████▓▓█████▓▓▓▓██▓████████████████████████▓▓▓▓▓▒░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▓▓▓██████▓█████▓▓▓▓▓▓▓▓▓▓▓░░░░░░▒▒▒▓▓████▓████████████████▓███████▓▓▓████████████████████████████▓░░░░░▒░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▓▓▓▓████████▓▓█▓▓▓▓▓▓▒▒▒░░░░░░░░░░▒▒▒▓▓█▓▓████████▓█▓████████████████████████████████████▓▓█████▓░░░░░▒░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▓▓▓▓██████▓▓█▓▓▓▓▓▒▒▒▒░░░░░░░░░░▒▒▓▒▒▓█▓▓██▓▓▓█████████████████████████████████████████▒░░░░▓███▓▒░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓▒▒▒▓▓▓████████████▓▓▒▓▓▒░░░░░░░░░▓▒▒░░▒▓▒▒▒▓▓███████████████████▓▓██████████████████████████▒░░░▒▒▒▓▓▓▒░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▓▓▓███████████████▓▓▒▒░░░░░░░░░░▒▒▒▒▒▓▓▓▓▓████████████▓▓███████▓▓██████████████████████████▓▒▒░░▒░░░▓▓▒▒▒▒░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓▓████████▓█████████▓▒▓▓▒▒▒▒░▒▒▒▓▓▓▓▓██▓▓█████████████████████████████████████████████████████████▓▒░░▒▓░░░▒▒▒░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▓▓▓█▓▓▓▓████████████▓▓▓█▓▓▓▓▓▓▓▓▓▓████████████████████████████████████████████████████████████████▒░▒▒░░░░░░░░░▒░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓▓███▓▓▓▓▓▓▓▓▓▓▓█▓███████▓██▓███████████████████████████████████████████████████████▓██████████████▓░░░░░▒░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▓▓███▓▓▓▓▓▓▓▓▓▓▓▓▓██████████████████████████████████████████████████████████████████▓▓▓█████████████▒░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▓▓███▓█████▓▓▓▓▓▓▓▓████████████████████████▒▓▓███████████████████████████████████████▓▓▓▓███████████████░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▓████████▓▓▓▓▓█▓█▓████████████████████████▓▒▓█████████████████▓▓███████████████████▓▓▓▓▒▓████████████████▒░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▓▓█████████▓██████████████████████████████████████████████████▓▒▓▓██████████████████▓▓▒░░░▓█████████████████▒░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░▓▓▓▓▓▓▓█████████████████████████████████████████████████████████▓▒▒▓▓█████▓████████████▓▓░░▒▓▓▓█▓██████████████▓░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▓▓▓▓▓█████████████████████▓▓▓█████████████████████████████████▓▓▓▓█████████████████████▓▓▓▓▓▓██████████████████░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░▓▓▓▓▓███████████████████████▓▒░▒▓███████████████████████████████████▓██████████████████████████▓▓███████████░▒▓██▓░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░▒▓▓▓▓█████████▓▓▓▓▓▓█████████▓▒░░▓▓██████████████████████████████████████████████████████████████▓███████████▒░░░▓██▓░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░▒▓▓▓▓▓▓█▓▓██▓▓▓▓▓▓▓▓▓███▓▓███▓▓▒▒▓███████████████████▓███████▓▓▓█████████████████████████████████████▓▓███████░░░░░▓▓▓▓▒░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░▓█▓▓▓▓▓▓██▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓███▓▓▓▓███████████████████▓███████▓▓▓▓█████████████████████████████████████▓▓██████▓░░░░░░░░▒▓░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░▓▓▓▓▓███▓▓▓▓▓▓▓▓▓█▓▓▓▓▓▓▓██████████████████████████▓▓▓██████████████▓████████████████████████████████████████▓░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓███████████████████████████████████████████▓▓█████████████████████▓▓▓▓▓████████▓█████▒░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓██▓▓██████████████████████████████████▓▒▒▓████████████████████▓▓░░▓▓▓█████▓█████▓░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓█████▓▓▓▓█▓▓███████████████████████████▓▓▓▓████████████████████▓▓▒▒▒▓▓▓▓▓▓▓██████░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓█████████████▓▓▓▓████████████████████████████▓▓▓███████████████████▓▓▓▓▓▒▓▓▓▓███████▓░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓▓█▓▓▓█▓▓▓▓▓▓▓▓▓▓▓▓▓▓████████▓▓███████▓▓▓▓▓▓▓███████████████████████████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓█████▒░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▓▓▓▓▓▓▓▓▓▓██▓▓▓▓▓▓▓▓█████▓▓▓▓██████▓█████████████████████████████▓▓▓▓▓▓█████▓▓▓▓▓▓▓▓▓▓▓▓▓▓█████████▓░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓███████████▓▒▒▒▒▒░░▒░░░░░░░░░░░░░░░░░░░░▒▒▓▓▓▓▓▓▓▓▓████▓▓▓▓▓▓▓▓▓▓▓░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░███▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒░▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▓▓▓▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒████████████▓▓▓▓▓▓▓▓▓▓▓▓▒▓▒▒▒░▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░");
                break;
            case "Voidborn Wraith":
                Console.WriteLine(@"
▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓██████████████████████████████████████▓▓▓
▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▓▒▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▒▓▓▓▓▓▓▓████████████████████████████████████████████▓▓▓
▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓█████████████████████████████████████████████████▓▓
▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓▒▒▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓███████████████████████▓██████████████████████████▓▓▓
▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▓▓▓▒▒▒▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓██▓▓▓▓▓▓▓▓█▓▓▓▓▓█████████████████████████████████████▓▓▓▓
▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓████████████████████████████████▓▓▓▓▓
▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓████████████████████████████▓▓█▓▓▓▓
▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▓▒▒▒▒▒▒▒▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓████████████████████████▓▓▓▓▓▓▓▓
▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓█▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▓▓▒▒▒▒▒▒▒▒▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓██████████████████████▓▓▓▓▓▓▓▓
▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓█▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▓▓▒▒▒▒▒▒▒▒▓▓█▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▒▓█████████████████▓▓▓▓██▓▓▓▓▓▓▓
▓▓▓▓▓▓▓▓▓▓▓█▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▓▓▒▒▒▒▒▒▒▒▓███▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▒▒▒▒▓████████████████████▓▓▓▓██▓▓▓▓▓▓▓
▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▓▓▒▒▒▒▒▒▒▒▓▓██▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▒▒▒▓▓▒▒▒▒▒▒▒▒▓████████████████████▓▓▓▓▓█▓▓▓▓▓▓▓▓
▓▓▓▓▓▓▓▓▓████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▓▓▒▒▒▒▒▒▒▓▓▓█▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓██▓▒▒▒▒▓▓████████████▓▓███████▓▓▓▓█▓▓▓▓▓▓▓▓▓▓
▓▓▓▓▓▓▓▓▓▓███▓▓▓██▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▓▓▒▒▒▒▒▒▒▒▒▒▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓█████████████▓▓▓▓██████████▓▓▓▓▓▓▓▓▓▓▓▓
▓▓▓▓▓▓▓▓▓▓▓▓██▓▓▓███▓▓▓▓▓▓▓▓▓▓▓▒▒▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓██████████████▓▓███████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓
▓▓▓▓▓▓▓▓▓▓▓▓▓███▓▓███▓▓▓▓▓▓▓▓▒▒▒▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓██████████████████████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓
▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓█▓▓█████▓▓▓▓▓▒▒▓▓▓▓▒▒▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓██▓▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓████████████████████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓██▓████▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓█▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓██████▓▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓███████████████████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓██▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒███▓▓▓▒▒▒▒▒▒▓██▓▓▓▓▓▓▓███████▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓██▓▓█████████▓▓▓▓█████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓███████▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▓▒▒▒▒▒▒▒▒▒▒▒▒▒████▓▓▒▒▒▒███████▓▓██████████▒▒▓▓▓▓▓▓▓▓▓▓▓▓████████████████▓▓▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓████████▓▓█▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒█████▓▓▓▓▓███████████████████▓▓▓▓▓▓▓▓▓▓▓▓██████████▓▓▓▓▓▓█▓▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓███████▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▓▒▒▒▒▒▒▒▒▓█████▓▓▓▓████████████████████▓▓▓▓▓▓▓▓▓▓▓██▓▓▓▒▒▒▒█▓▒▒▒▒▒▓██▓▓▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▓▓████████▓▓██▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓█████▓▓▓█████████████████████▓▓▓▓▓▓▓▓▓▓▓█▓▓▒▒▒▒▓▓▓▒▒▒▒▒▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▓▓███████▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒██████▓███████████████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▓▒▒▒▒▒▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▓▓▓▓▓▓███████▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒██████████████████████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▓▓▒▒▒▒▒▒▓▓▓▓████▓▓▓▒▒▒▒▒▒▓▓▒▒▒▒▒▒▒▒▒▒▒██████████▓▓█████▓▓▓▓█████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▓▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▓▓▓▓▓█▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒██████████▓▓███▓▓▓▓▓██████████▓▒▒▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒███████████████▓██████████████▓▒▒▒▒▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▓▓▓▓▒▒▒▒▓▒▒▒▒▒▒▒▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒██████████████████████████████▓▒▒▒▒▒▒▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓
▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▓▓▓▓▓▓▓▓▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▓██████████████████████████████▓▒▒▒▒▒▓▓▓▓▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▓██████████████████████████████▓▒▒▒▒▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▒▒▒▓▓▓▓▒▒▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▓███████████████████████████████▒▒▒▒▒▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓█▓▓▓▓▓▓▓▓▓▓▓▓▓
▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▓▓███████████████████████████████▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▓▒▒▒▒▓▓▓▓▓▒▒▒▒▒▒▒▓▒▓▓▓▓▓▓▓▒▒▒▒▒▒▒▓▓▓██████████████████████████████▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▒▒▒▒▒▒▒▒▒▓▓▒▒▒▒▒▒▒▒▒▓▓▓▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓
▓▓▒▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▒▒▒▒▒▓▒▒▒▒▒▒▒▓▒▒▒▒▒▓▓▒▓████████████████████████████████▓▓▒░░░▒░░▒▒▒▒▒▒▒▒▒▒▓▒▒▒▒▒▒▒▒▒▒▓▒▒▒▒▒▒▒▒▓▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓
▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▒▒▒▒▒▓▒▒▒▒▒▒▓▓▓▓▓███████████████████████████████████▓░░░░▒▓▒▒▒▒▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓
▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▒▓▓▒▓▓▓▒▒▓▓████████████████████████████████████▓▓▒▒▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▒▒▒▒▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓
▒▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▒▒▒▒▒▒▒▒▓▓███████████████████████████████████████▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓
▒▒▒▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▒▒▒▒▒▒▒▒▒▒▒▒▒▓▒▒▒▒▒▓▒▓▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓███████████████████████████████████████▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▓█▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒
▒▒▒▒▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▒▒▒▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓████████████████████████████████████▓▓▓▓▓▓▓▓▓▓▓▓▒▓▓▒▒▒▒███▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒
▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓████████████████████████████████████▓▓▓▓▓▓▓▓▓██▓██▓▓██████▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒
▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓▒▒▓▓▓▒▒▒▓▓▓▓▒▒▒▒▒▒▓▓▓▓▓█████████████████████████████████▓▓▓▓▓▓▓▓███▓▓█████████▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒
▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓█▓▓▓▓▓▓▓▓▓▒▒▒▒▒▓▓▓▓▓▓▓▒▓▓▓▓▓███████████████████████████████▓▓▓▓▓▓████████████▓▓▒▒▒▒▒▒▒▒▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒
▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▓▒▒▓█▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▓▓▓▓█▓▓▓▓▓▓█████████████████████████████▓▓▓███▓███████████▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒
▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓█▓▓▓██▓▒▒▒▒▒▒▓▓▓▓███▓▓▓▓█▓▓▓▓▓▓▓▓▓▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓████████████████████████████▓▓▓▓███████████▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒
▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓███▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓█▓▓▓▓▓▓▓▓█▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓█████████████████████▓▓▓▓▓█████████████▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒
▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓█████████▓█▓▓▒▒▓▓▓▓██████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▓▓▓████████████████████████████████████████▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒
▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓██████████▓██████▓▓▓▓▓▓▓████▓▓▓█▓▓▓▓▓▓███▓▓▓▒▓▓▓▓███▓▓▓▓███████████████████████████████████▓▒▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒
▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓██▓▓▓▓▓▓▓▓▓▒▒▓███████████████████▓▓▓▓▓▓█▓▓▓▓▓██████▓▓▓▓▒░▒▓▓▓███████████████████████████████████▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒
▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓███▓▓▒▒▒▒▒▓▓▓▓▒▓██████████████████████████▓▓▓▓██▓▓▓▓▓▓▒▒▒▒░░░▓▓▓▓▓████████████████████████████████▓▒▓█▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒
▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓██▓▓▒▒▒▒▒▒▒▒▒▒▓████████████████████████████████▓▓▒▒▓██▓▓▓▓▓▓▓▓▓▓▓▓█████████████████████████████████████▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒
▒▒▒▒▒▒▒▒▒▒▒▒▒▓██▓▒▒▒▒▒▒▒▒▒▒▒▓▓██████████████████████████████▓▒▓▒▒▒▓▓▓▓▓▓▓███▓▓████████████████████████████████████████▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒
▒▒▒▒▒▒▒▒▒▒▒▓▓▓█▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓███████████▓███████████████▓▓▓▓▓▒▓███▓▓▓▓▓▓█▓█████████████████████████████████████████▓▒▒▒▒▒▒▒▒▒▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒
▒▒▒▒▒▒▒▒▒▒▓▒▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓█▓██████▓▓▓▓▓▓█████████████▓▓▓▓▓▓▓▓▒▓▓▓▓▓▓▓███▓████████████████████████████████████████████▓▓▓▓▓██▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒
▒▒▒▒▒▒▒▒▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒█████▓▓▓▓▓▓▓███████████▓▓▓▓▓▓▓▓█████████▓▓▓█████████████████████████████████████████████████████▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒
▒▒▒▒▒▒▒▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓█████▓▓███▓██████████▓▓▓▓▓▓███▓█▓▓▓▓▓▓▓▓▓█████████████████████████████████████████████▓▒▒▒▒▒▒▒▒▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒
▒▒▒▒▒▒▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒██▓███████▓▓▓▓█████▓▓▓▓▓▓▓██████████▓▓▓▓▓▓▓█████████████████████████████████████████████▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒
▒▒▒▒▒▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓██▓██████▓▓▓██████▓▓▓████████████████████████████████████████████████████████████████████▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒
▒▒▒▒▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒████▓▓▓▓▓▓▓▓▓▓████▓▓█████████████████████████████████████████████████████▓██████████████████▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒
▒▒▒▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓██▓▓▓▓▓▓▓▓██▓▓▓▓▓▓████████████████████████████████████████████████████▒▒▒░▒▓████████████████▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▒▒▒▒▒▒▒▒
▒▒▒▓▓▓▓▓▒▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓█▓▓▓▓███████████████████████████████████████████████████████▓▒░░░░░░▓██████████████████▒▒▒▒▓▓▒▒▒▒▒▒▒▒▒▒▒▒▓▓▒▒▒▒▒▒▒
▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓█▓██▓███████████████████████████████████████████████████████████░░░░░░░░░▓▓████████████████▓▒▓██▓▒▒▒▒▒▒▒▒▒▒▒▒▓▓▒▒▒▒▒▒▒
▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▓████▓██▓▓██████████████████▓▒█████████████████████████████████████████▒░░░░░░░▒▒▒░▒█████████████████████▓▒▒▒▒▒▒▒▒▒▒▒▓▓▓▒▒▒▒▒▒
▒▒▒▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒██████████████████████████▓░░░▒████████████████████████████████████████▒░░░░░░░░▒▒▓▓██████████████████████▒▒▒▒▒▒▒▒▒▒▒▓▓▒▒▒▒▒▒▒
▒▒▒▓▓██▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒██████████████████████████▒░░▒▒▒████████████████████████████████████████▒░░░░░░░░░▓████████████████████████▓▓▒▒▒▒▒▒▓▓▓▒▒▒▒▒▒▒▒
▒▒▒▓▓█▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▓█████████████████████████▒░░░░█▓▒██████████████████████████████████████████▒░░░░░░░░▒▓███████████▓████████████████▓▓▒▒▒▒▒▒▒▒▒▒▒
▒▒▒▒▓▓▓▓█▓▒▒▒▒▒▒▒▒▒▒▒▒▓░▒███████████████████████▒░░░░▓██████████████████████████████████████████████▓░░░░░░░▒██████████████████████████▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒
▒▒▒▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▓░░▒░░░▒▓████████████████████▓░░░▒▓███████████████████████████████████████████████▓░░░░░░░▒███████████████████████▒▒▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒
▒▒▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▓▒░░▒░▒░░▒▓▓██████████████▓▒▒░░░░░▒█████████████████████████████████████████████████░░░░░▒▓████████████████████████▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒
▒▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒░░░░▓░░░░░▒██████████▓▒░░░░░░░░▒▓█████████████████████████████████████████▓████████▒░░░░▓████████████████████████▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒
▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒░░▒▒░░▒▒░░░░░▒▓▓██▓▓▓▒▒░░░░▒░░░░▓████████████████████████████████████████████████████▓░░░▓██▓▓▒▒████████████████████▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒
▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒░▒░░░░░▒░░░░░░▒▓▓▓▓░░░░░░░░░░░▒▓▓█████████████████████████████████████████████████████▓▓▓▓▓▒░░░▒▓▓▓█████████████████▓▓█▒▒▒▒▒▒▒▒▒▒▒▒▒▒
▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒░░░░░░░▒░░░░░░▒▒▒▒▒░░░░░░░░░░████████████████████████████▓▓███████▓███████████████████▒▒▒░░░░░▒▒░░▓████████████████▒░▒▓▒▒▒▒▒▒▒▒▒▒▒▒▒
▒▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒░░░░░░░░░░░░░░░▒░░▒░░░░░░░░░░▓██████████████████████▓▓▓▓▓▓▓▓▓▓▓▓▓█████████████████████▒░░░░░░░░░░░░▒███████████████▓░░░▒▒▒▒▒▒▒▒▒▒▒▒▒
▒▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒░░░░░░░░░░░░░░▒▒░▒▒░░░░░░░░░█████████████████████████████████▓████████████████████████▒░░░░░░▒░░░░░▓▓▓█████████▓▓▓█▒░░░▒▒▒▒▒▒▒▒▒▒▒▒
▒▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒░░░░░░░░░░░░░░░▒▒▓▒░░░░░░░▒▓██████████████████████████████████████████████████████████▒░░░░░▒▒░░░░░▒▒▒▓▓▓██████▓▓▓▓▓░░░░░▒▒▒▒▒▒▒▒▒▒
▒▒▓▓▓▓▓▓▓▓▒▒▒▒▒░▒▒▒▒░░░░░░░░░░░░░░░░▓▓▓▒░░▒▒░▓████████████████████████████████████████████████████████████▒░░░░░░▒░░░░▒░░░▒▒▒▓████▓▓▓▓█▒▒░▒▒▒▒▒▒▒▒▒▒▒▒
▒▒▒▒▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒░░░░░▓▒░░░░░░░░░░▒▒▓▓▓▓▓▓██████████████████████████████████████████████████████████████▒▒░░░░░░░░░░░░▒▒▒▒▒▒▓███▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒
▒▒▒▒▒▒▓▓▓▓█▓▒▒▒▒▒▒▒▒░░░░▓▓░░░░░░░░░░░░▓▓█████████████████████████████████████████████████████████████████████▓░░░░░░░░░░▒▒▒▒▒▒▒▒▓███▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒
▒▒▒▒▒▒▒▓▓▓▓█▓▓▒▒▒▒▒▒▒░░░░░░░░░░░░░░▒▓████████████████████████████████████████████████████████████████████████▓▒░░░░░░░░▒▒▒▒▒▒▒▒▒▒▓███▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒
▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▒▒▒▒▒▒▒░░░░░░░░░░░▒████████████████████████████████████████████████████████████████████████████▓▒░░░░░▒▒▒▒▒▒▒▒▒▒▒▒▒██▓▓▓███▓▒▒▒▒▒▒▒▒▒▒▒
▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▒▒▒▒▒▒░░░░░░░░▒▓██████████████████████████████████████████████████████████████████████████████▓▒░░▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓██▓▓████▓▒▒▒▒▒▒▒▒▒▒▒
▒▒▒▒▒▒▒▒▒▒▒▓▓▓█▓▓▓▓▒▒▒▒▒▒░░░░▒▓████████▓▒▓████████████████████████████████████████████████████████████████████▓▓█▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓███▓▒▒▒▒▒▒▒▒▒▒▒
▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▒▓▓▓▓▒▒▒▒▒▓▓████████▓▒░▓█████████████████████████████████████████████████████████████████████▒▒▒▒▒▒▒▓▓▓▓▓▓▒▒▒▒▒▓▓▓▓▓▓▓▓██▓▒▒▒▒▒▒▒▒▒▒▒
▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒███▓▓▓▓██████▓▓▒░░░▓██████████████████████████████████████████████████████████████████████▓▒▒▒▒▒▒▒▓▓▓▓▓▒▒▒▒▒▓▓▓▓▓█▓▓▓██▒▒▒▒▒▒▒▒▒▒▒
░▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓█▓▓██▓▓▓▒▒▒░░░░░░▓████████████████▓▓██▓▓▓█████████████████████████████████████████████████▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓███▓▓▓▓█▒▒▒▒▒▒▒▒▒▒▒
░▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓▒▒▒▒░▒▒░░░░░▒▓██████████████▓▓▓█▓▓▓▓███████████████████████████████████████████████████▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓████▓▓▓█▓▒▒▒▒▒▒▒▒▒▒
░░▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒░░░▒▒▒░░░░░░▒▓██████████████▓▓██▓▓█████████████████▓█████▓███████████▓▓▓█████████████████▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓▒▒▓█▓▓▓██▒▒▒▒▒▒▒▒▒▒
░░░▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒░░░░░░░░░░▒▓█████████████████████████████████████▓▓████▓▓████████▓███▓▓▓█████████████████▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓██▓▓█▓▒▒▒▒▒▒▒▒▒▒
▒░░▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒░░░░░░░▒▓▓██████████████████████████████████████▓▓████▓▓██████████▓▓▓▓▓▓▓███████████████▓▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓█████▓▓▓▒▒▒▒▒▒▒▒▒▒▒
░░▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒░▒▒▒▒▒▒▒░░░░░▒▒▓██████████████████▓████████████████████▓█▓███▓▓███████████▓▓▓▓▓▓▓▓█████████████▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓████▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒
░░░▒▒▒▒▒▒▒▒▒▒▒▒▒▒█▓▓▓▒▒▒▒░░░░▒▒░▓██████████████████████████████████████████▓███▓▓█████████████▓▓▓▓▓▓▓█████████████▓▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓██▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒
▒░░░▒▒▒▒▒▒▒▒▒▒▒▒▓███▓▒▒▒▒░░░▒▒▒▓███████████████████████████████████████████████▓▓█████████████████▓▓▓██████████████▓▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒
▒▒░░░▒▒▒▒▒▒▒▒▒▒▒▓███▒▒▒▒▒░░▒▒▒▓███████████████████████████████▓████████████████▓▓██████████████████▓▓▓██████████████▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒
▒▒▒▒░░░░▒▒▒▒▒▒▒▒▓▓█▓▒▒▒▒▒░▒▓▓████████████████████████████████████████████████████████████████████████▓▓▓█████████████▓▒▒▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒
▒░▒▒▒▒▒░░▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓███████████████████████████████████████████████████████████████████████████▓▓▓▓▓████▓▓▓▓▓█▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒
░░░░▒▒░░░▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓██████████▓▓▓▓▓█████████████████████████████████████▓▓▓███████████████████████▓▓▓▓████▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒
▒░░░░▒▒▒▒░░░░▒▒▒░░▒░▒▒▒▓▓██████████████████████████████████████████████████████▓▓▓████████████████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒
░▒░░░░▒░░░░░░░░░░░░░░▒▓▓███████████████████████████████████████████████████████████▓███████████████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒
░░░░░░▒▒░░░░░░░░░░░░▒▓▓▓▓▓▓▓▓███████████████████████████████████████████████████████████████████████████████████▓███▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒
░░░░░░░░░░░░░░░░░░▒▓▓▓▓▓▓▓▓▓█████████████████████████████████████████████████████████████████████████████████▓▓▓█████▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▒▒▒▒▒▒▒▒
░░░░░░░░▒░░░░░░░░▒▓▓▓▓▓▓▓▓▓██████████████████████████████████████████████████████████████████████████████████▓▓▓▓▓▓██▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒
░░░░░░░░░░░░░░░▒▓▓▓▓▓▓▓▓▓▓▓██████████▓▓███████████████████████████████████████▓███▓▓█▓▓▓▓▓▓▓▓███████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒
▒░░░░░░░░░░░▒▒▓▓▓▓▓▓▓▓▓▓▓▓███████▓▓▓▓▓▓▓▓▓███████████████████████████▓██▓█▓▓█▓▓██▓▓▓▓▓██▓█▓▓▓▓▓▓▓██████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒░▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒
▓▓▒▒░░░░░▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓██▓▓▓▓▓▓▓▓▓▓▓▓▓███████████████████████▓▓████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓█████▓▓▓▓▓▓▓▓█▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒░░░░▒▒▒▒▒▒▒▒▒▒▒▒
▓▓▓▓▓▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓███████████████████▓▓▓▓███▓▓▓▓▓▓▓▓▓▓▒▒▒▒▓▓▓▓▓██████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒░░░▒░░░▒▒▒▒▒▒▒▒▒
▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓██████████████████▓▓▓▓▓▓▓█▓▓▓▓▓▓▓▓▓▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓██████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒░░░▒▒▒░░▒▒▒▒▒▒
▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓█████████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒░░░░▒▒▓▓▓▓▓▓▓▓▓▓▓▓█▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒░▒▒▒░░▒▒▒▒▒▒
▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▓▓▓▓▓████████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒░▒▒░▒░▒░░░░▓▓▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒
▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▓▓███████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒░░▒░░░▒░░░░▒▒░░▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒
▒▒▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒░░▒▒▒▒▒▓███████████████▓▓▓▓▓▓▓▓▒▓▓▓▓▓▓░░░░░░░░░░░░░▒░░▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒░░▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒
░░▒▒▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓████████████▓█▓▓▓▓▓▓▓▓▓▓▒▓▓▒▓▓▒░▒░░░░░░░░░░░▒░░░░▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒░░░░▒▓▓▒▒▒▒▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒░░░▒▒▒▒▒▒▒▒");
                break;
            case "Oblivion Scourge":
                Console.WriteLine(@"
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒░░░░░░░░░░░░░░░░░▒▓▒░░░░░░░░░░░░░░░░░░░░░░░▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░▓▓░░░░░░░░░░░░░░░░░░▒▓▓▒░░░░░░░░░░░░░░░░░░░░░▓░░░░▒▒░░░░░░░░░░░░░░░░░░░▒▒▓▓▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░▒█▒░░░░░░░░░░░░░░░░░░░░▓▓▓▒▒▒░░░░░░░░░░░░░░░░█▒░░░▒▓░░░░░░░░░░░░░░░░▒▓▓██▓▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓█▓░░░░░░░░░░░░░░░░░░░░░░▒▒▓▓██████▒▒▒░░░░░░▓█▒░░▓▒░░░░░░░░░░░░▒▓▓███▓▒░░░░░░░░░░░░░░░░░░░░░░░░░▓▒░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒███▒░▒░░░░░░░░░░░░░░░░░░░░░░░░░▒▓█████▒▒░░░▒▓█▓▓█▓▒▒░░░░▒▓▓██████▓░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓████▒░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓███▓▒░░▒▓█▓████▓▒░▓▓████▓▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒█▒░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓███▓▒░░▒░░░░░░░░░░░░░▒▒▓▓▓▒░░░░▒▓████▓▒▒▓█▓████▓▓█████▒░░░░░░▒▒▒▒░░░░░░░░░░░░░░░░▒▓▒▓███▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓███████▓▒░░░░░░▒░░░░░░░░▒▓█▒░▒▓▓██████▓▓█▓█████████▓░░░░░▒▓▓▒▒░░░░░░░░░░░░░▒████████▓▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▓█████████▓▓▓▓█▒░░░░░░░░░▒█▓▒▒▓▓██████████████████▒░░░▒█▒▒░░░░░░░░▒▒▒▒▓▓▓▓███████▓▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓██████████████▓▒▓▒░░░░▒██▓▓▓███████▓██████████▓░░▒▒█▓▓█▓▓▒▓▓▓▓████████████▓▓▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▓▒▒▓█████████████▓▓▓▓████▓▓▓██▓██▓██▓▓████████▓██████████████████████▓▓▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▓██████████████████████▓█▓▓███████████▓████████████████████▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░████████████████████████▓█████████████████████████▓▒░░▒██▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒█████████████████████████▓▓▓▓████████████████████▒░░░░░▒▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒█████████████████████████████████████████████████▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒░░░░▓████████████████████████████████████████████▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓██████████████████████████████████████▓▒▒░░▒▒▒▒░░░░░░░▒▓█▓▓▓██▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▓▓▓██▓▓▓███████████▓▓████████████████▓██▒░░░░░░░░░░░░░▓▓░░░░░░░▓█▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒██▓████████████▓███████████████▓▓▒▓▓▓▓░░░░░░░░░▒█▓░░░░░░░░░░██░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓██████▓▓▒░░░░░░░░░░░▓▓▓▓▓██████████▓█▓▓█▓█████████████▓▓▓▓▓▓▓▓░░░░▒░░██░░░░░░░░░░░░██░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓██▓▓▒░░▒▒▓▓█▓█░░░░░░▒▒▒▒▓▓▓███████████▓▓████████████████████████▓▓▓▓██████▓░░░░░░░░░░░▓█▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓█▓░░░░░░░░░▒▓███▓▒▓▓▓▓▓██████▓████████▓█▓█████████████████████████████▓█████▒▒░▒█▒░░░░░▒█▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓█▒░░░░░░▒▒░░▓▓▓███▓██████▓█████████████▓█▓███████████████████████████████████████▓█▓▓█▒░▓██▒░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒██▒░░░░░▒▓▓▓▒█████████████████████▓████████████████████████████████████████████████████▓▓▓██▒░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒██▒▒▓▓▓▒▓███▓█▓▓▓█████▓▓▓▓▓█▓█████████▓████▓█▓▓█████████████████████████████████████████████▒░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒██▓▒▓██▓▓▓▓▓▓▓▓▓████████████████████▓██████▓▓▓▓█████████████████████████████████████████████▒░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓██▓▓▓▓▓▓██████▓▓████▓█▓▓▓█▓▓▓█▓▓▓▓███▓████▓▓███████████████████████████████████████████████▒░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░▓▒▒▓█████████▓▓▓▓█████████████████████▓▓█████▓▓█████████████████████████████████████████████████▓░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▓▓█████▓▓▓██████████████▓▓▓████▓▓█████████▓▓▓█▓▓███████████████████████████████████████████████▓▓▒░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░▒▓▓███▓▓▓▓▓████████▓▓▓█▓██████████▓▓██████████▓███████████████████████████████████████████████████████▓░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░▒▓█▓▓▓▓▓███▓▓█████▓▓███▓███████████▓█████████▓▓█████████████████████████████████████████████████████████▒░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░▒▒▓▒▒▓▓▓█████▓▓▓████████████████████▓███████████████▓█████████████████████████████████████████████████████▓▒░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░▒▒▓███▓▓█▓▓▓████████████████████████▓████████████████▓██████████████████████████████████████████████████████▒░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░▓▓▓█▓▒▓██████▓▓██▓█████████████████▓███▓██████████████▓█████████████████████████████████████████████████████▒░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░▓▒▓▓▓▓▓█████████████▓███████████████▓███▓▓██████████████████████████████████████████████████████████████████▓░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░▒▓▓████████████████████████████████████▓▓▓▓▓████████████████████████████████████████████████████████████████▓░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░▒▒▓▓▓▓██████████████████████▓███████████████████████████▓████████████████████████████████████████████████████▒░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░▒▓███▓▓██████████████████████████▓▓████████████████████████████████████████████████████████████████████████▓▒░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░▓▓▓██▓▓███████████████████████████████████████████████████████████████████████████████████████████████████░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░▒▓▓███▓██████████████████████████████████████████████▓███████████████████████████████████████████████████░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░▓███▓████████████████████████████████████████████▓███████████████████████████████████████████████████▒░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░▒▓▓▓▓▓████████████████████████████████████████████████████████████████████████████████████████████████▒░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░▒▓▓▓▓▓▓███████████████████████████████████████████████████████████████████▓▓▓▓▒▒▒█████████████████████▒░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░▓▓▓███▓▓████████████▓▒▓████████████████████████████████████████████████▓▓▒▒▓▓▒▒▒▒▓▓██████████████████▓░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░▒▓█████▓▓██████████▒▒▒░▒██████████████████████████████████████████████▒▒▒▒▒▓▒▒▒▒▒▒▒▒▓██████████████████▓▒░░░▒▒░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░▒▒▓▒▓█████▓▓████████▓▒▒▒▒▒▒▒▓███████████████████████████████████████████▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒████████████████████▓▒▓▓█░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░▒▒░▒▒▓▓▓▓████▓▓▓████████▓▒▒▒▒▒▒▒▒▓██████████████████████████████████████▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒█▒▒▒▒▒████████████████████████▓▒░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░▒▒▓▓▓▓▓▓▓██▓▓▓████████▓▓▒▒▒▒▒▒▓▒▓▓████████████████████████████████████▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▒█▓█▓▓███████████████████████▓▒░░░░░░░░░░░░░░░
░░░░░░░░░░░░▒▒░░░░░▒▓▓▓▓▓█▓▓▓▓▓██▓▓▓▓▓█▓▓█▓▒▒▒▒▓▒▒▓▓▓▓██████████████████████████████████▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓███████████████████████████▓▒▒░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░▒▓▓▓▓▓▓▓▓▓▓█▓██████▓████▓▓▓▓▓▓▓▓▒▒▒▒▓▓▓▓▓██████████████████████████████████▒▒▒▒▒▒▒▒▒▓▓▒▒▒▒▒▓████████████████████████▓███▓▒░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░▒▒▓▓▓▓▓▓▓█▓██████████▓▓██▓▓▓▒▒▒▒▒▒▒▒▓▓▓██████████████████████████████████▓▒▓▓▒▒▒▒▒█▓▒▒▒▒▒▓█████████████████████████████▒░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░▒▓▓▓▓▓▓██▓██████████▓▓▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓█▓████████▓█████████████████████████▓▓▓▓▒▓██▒▒▒▒▒▒▓▓█████████████████████████▓▒░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░▒▓▓▓▓▓██▓████████▓▓▓▓▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓█▓████████▓███████████████████████████▒▓█▓██▒▒▒▒▓▒▒▓████████████████████████░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░▒░▒░▒▓▓▓▓██▓▓███▓███▓▓▓▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓████████▓███████████████████████████▓█▓▓██▓▓▒▒▒▒▒▒▓█████████████████████▓▒░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░▒▓▓▒▓▓▓█▓█▓▓▓██▓▓█▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓█▓▓██████▓█████████████████████████████▓█▓██▓▓▒▒▓▒▓▓▓████████████████████▒░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░▒▓▓▓▓▓▓▓▓▓▓▓▓██▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓████▓██████████████████████████████▓▓▓██▓▓▒▒▓▓▓▓▓██████████████████▒░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓█▓▓▓▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓█▓████▓█████████████████████████████▓██████▓▓▓▓▒▓▓▓▓██████████████████▓░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▓▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓██▓████████████████████████████████▓▓██▓▓▓▓▓▓▓▓████████████████████▒░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓██▓▓████████████████████████████████▓██▓▓▓▓▓▓▓▓████████████████████▓░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓██▓▓████████████████████████████████▓█▓▓▓██████████████████████████▓▓░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░▒▒▒▒▓▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓█▓▓██▓█████████████████████████████▓▓▓██████████████████████████▓▓▓▓▒░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓█▓▓██▓█████████████████▓▓█████▓▓▓▓▓▓█▓▓█▓▓█████████████████████▓▓▓▓▓░░░░░░░░░░░░░░▓▓
░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▒▒▒▓▒▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓█▓▓▓██▓████████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓████████████████████▓▓▓▓▓▓▓▓▒░░░░░░░░░░░░▓▓
░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▒▒▒▒▒▒▓▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓█████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓██████████████████████▓▓▓▓▓▓▓▓▓▓▒░░░░░░░░░░▒█▓
░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▒▒▒▒▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓██████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓██▓█████████████████▓▓▓▓▓▓▓▓▓▓▓▓▒░░░░░▒▓▓▓███
░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▒▒▒▒▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓█████████████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▓███████
░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▒▒▒▒█▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓█████▓▓▓█████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓█████████████
░░░░░░░░░░░░░░░░░░░░▓▒▒▒▒▒▒▒▒▒█▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▒▓▒▓▓▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓█▓▓▓▓▓██▓▓▓▓▓▓███▓▓▓▓██▓▓█▓▓▓▓▓▓▓▓▓▓███████████████████
░░░░░░░░░░░░░░░░░░░░▒▓▒▓▓▒▒▒▒▒▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒█▒▓▓▓▓▓▓▓▒▓▓▓█▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓█▓▓▓▓▓▓▓█▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓██▓▓▓▓▓▓██▓▓▓███████████████
░░░░░░░░░░░░░░░░░░░░░▒▓▓▓▒▒░░░░▒▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓█▓▓▓▓▓▓▒▓▓▓▓▓█▓█▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓█▓██▓█▓▓▓▓▓▓▓█▓▓▓▓▓██▓▓▓▓▓▓▓▓▓▓▓███▓▓▓█▓█████████████▓██████
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓▒▒▒▒▒▒▒▒▒▒▓▓▓▓▒▓▓▓▓▓█▓▓▓▓▓▓▓▓██████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓█▓▓▓▓▓█████▓▓▓█████▓▓▓▓███▓▓▓▓▓▓▓▓▓████▓███▓████████████████████
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▓▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓██▓▓████▓█████████████▓█▓█▓██▓███▓██████████████▓█████▓▓███▓▓▓█▓███▓█████████████████████████████
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓███████████████████████████████████████████████████████▓▓█████████████████████████████████████████");
                break;
            case "Ancient Desolator":
                Console.WriteLine(@"
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▓▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▓▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▓▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▓▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▓▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▓▒▒░░░░▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▓▓▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▓▓▒▒░░▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒░░░▓▓▓▓▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▓▓▓▓▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▓▓█▓▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓▓██▓▓▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓▒▓▓▓▓▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▓▓██▓▓▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓█▓▓▓▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▓▓▓█▓▒░▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▓▓▓▓▓▒▒▒▒▒░▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▒▓▓▓▓█▓▓▒░▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓▓█▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▓▓▓▓▓▓▓▓██▓▓▒░░░▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▓▓▓▓▓▓███▓███▓▓▓▓▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒░░░░░░░░░░░░░▒▒▒▒▒░▒▒▓▓██▓▓▓▓▓▒▒▒▒▒▒▒░░▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▓██▓▒▒▒▒▒▒▒▒░░░░░░░░░░▒▒▒▒▒▒▒▓██▓▓▒▒░░░▒▒▒░▒░░░░░░░▒▒▒▒░░▒░▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▓▓▓▒▒▒▒▒▒▒▒░░░░░░░▒▒▒▒▒▒▒▓█▓▒░░░░░░░▒░░░░░▒░░░░░▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒█▓█▓▓▒▒▒▒░░░░░░░░░░▒▒▒▓▓███▓▒▒░░░░░▒▒░░░░░░░░░▒▒▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▒▒▒▒▓▓█▓███▓▓▒░░░░░░░░░░▒▒▒▓█████▓▓▓▒▒▒▓▓▒▒▒░░░░░░░░░░▒▒░░▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒░░░░░░░░░▒▒▓▓▓▓▓▓▓▓▓▓▓▓▒░░▒▒░░░░░░░░░░▒▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▓▓███▓█▓▒▒░░░░░░░░░▒▓█▓██▓▒▒▒▒▒▒░░░▒░░░░░░░░░░░░▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▓▓▓▓▓█████▒░░░░░░░▒░░▓█████▓▓▓▒▒▒░░░░░░░░░░░░░░░░░░▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▓▒▒▒▓▓▓███▓▓▓▒░▒░░░░░░▒▓▓███▓▒░▒░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▓▓▓▓▒▓▓▓▓█████▓▓█▓▒▒▒▒▓▓▓▓███▓▒▒░░░░░░░░░░░░▒▒░░░░░░░░░░▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒░░▒░░░▓▓▓████▓▓▒░░▒▓▓█████▒▒▒▒░░░░░░░░░░▒▒▒▒▒▒░░░░▒░▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▒▒░░░░░░░▒▒▓▓████▓▓▓▒▒▓▓▓██▓▓▓▓▒▒▒▒░░░░░░░░░▒▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒░░▒▓▒▒▒░░░░░░░▒▒▒▓▓▓██▓▓▓▓▓▓▓▓██▓▓▓▓▓▓▒▓▒▒░░░░░▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒░░░░░░░░░░░░▒▒▒▒▒░▒▓▓▒▒▒░░░▒▒▒▒▓▓▓▓▓███▓▓▓▓▓▓███▓▒▒▒▓██▓▓▒▒▒▒▒▒▒▒░░▒▒▒▒▒▒▒▒░▒▓▒▒▒▒░░░░░░░░░▒░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒░░░░░░░░░▒░░░░░░▒▒▒▒▒▒▒▒▓▓▓▒▒▒▒▒▒▒▓▓▓▒▒▓██▓██▓▓▓▓█▓██▓▒▒▓▓▓██▓▒▒░░▒▒░░░░░▒▒▒▒▒▓▒▒▒▒▓▒▒▒░░░░░▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒░░░░░░░▒░▒▒▒░░▒▒▒▒▒▒▒▓▓▒▓▒▒▓▓▓▒▓▓██▓▓▒▒▓▓▓▓██▓▓▓▓█▓█▓▓▒▓▓████▒░░░░░░▒░░░░▒▒▓▒▒▒▒▓▓▓▓▒▓▒░░░░▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒░░░░░░░░▒▒▒▒▒▒▒▒▒░░░▒▒▒▒▒▒▒▒░░░▒▓███▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓████▒░░░░░░▒░░░▒▒▓▓▓▓▒▒▒▓▓▓▒▓▓░░▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒░░░░░░░░▒▒▒▒▒▒▒▒░░░▒▒▒▒▒▒▒░░░░▒▓██▓▓▓▓▓▓▒▓▓▓▓▓▓▓▓▓▓▓▒▓▓████▓▒▒░░░▒▒▒░░░▒▒▓▓▓▓▓▓▓▓▓▒▒▒▒▓▓▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒░░░░░░░▒▒▒▒▒▒▒▒░▒▒▓▓▓▒▒▒░░░░░▒▓███▓▓▓▓▓▓▓█▓▓▓▓▓▓▓▓▓▓▓▓█▓███▓▒▒░▒▒▒▒░░░▒▒▒▒▓▓▓▓▓▓▓▓▒▒▓▓▓▓▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒░▒░░░░░░▒▒▒▒▓▓▒▓▒▓▓▒░▒░░░░░▒▒█████████▓▓█▓▓▓▓███▓▓▓▓▓█▓█████▓▓▓▓▓▒░░░▒░░░░▒▓▓▓▓▓▓▒▒▒▒▓▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▓▒▒▒▒▒▒░░░▒▒▒▒▓▓▓▓▓▓▓░░░░▒░░▒▓████████▓▓▓▓▓▓▓▓▓▓▒▓▓▓▓▓▓▓█████████▒▒░░░░░░▒░░▒▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▓▓░░▒░░▒▒▓▓▒▓▓▓▓▓▓█▓▓▒░░░░▒▒▓▓██████▓█▓█▓▓▓▓▓▓▓▓▓▓▒▓▓▓▓█████████▓░░░░░░░▒▒▒░░▒▒▓▓▒▒░▓▒▒▒▒▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░▒▒▒░░░░░░▒▒▒▒▒▒▒▓▒░░▒▓▓▓▓▓▓▓█▓█▓▒▓▒░░░▒▒▓▓██████▓███▓▓▓▓▓█▓▓▓▓▓▓▓▓█████████▒░░░░░░▒▒▒░▒▒▒▓▓▒▒▒▒▒▒▒▒▓▓▒▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░▒▒▒▒▒░▒▒▒▓▒▒▒▒▓▓▓▒▒▓▓▒▓▒▓▓▓███▓▒▓▒▒▒░░▒▒▒▓███████████▓▓▓▓▓▓█▓▓▓▓▓█████████▓░░░░░▒▒▒▒▒▒▒▒▒▒░▒▒░░░░░▒▓▓▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░▒▓▒▒▒▓▓▓▓▓▓▓▒▓▓▓▓▓▒▓▒▒▒▒█▓▓███▓▒▒▒░░░░░░░░▓██████████▓▓▓▓▓██▓▓▓▓██▓▓████▓▓▒░░░░░░▒▓▓▓▓▒░░▒▒▒▒▒░░░░░░▒▒▒▒▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░▓▓▓▒▓▓▒▓▓▓▓▓▓▒▒▒▓▓▓▓▓▓▓▓▓█████▒▓▒▒░▒░░░░░▒███████▓▓█▓▓▓▓▓██▓▓▓▓▓█▓█▓▓▓▓▓░░░░░▒▒▓▓▓▓▓▒▒▒▒░░░░░░░░░░░░▒▓▓▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░▓▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓██████▓▒▒▒▒░░░░░░▒▓▓▓▓▓▓▓▓▓██▓▓▓▓▓▓▓▓████▓▓▓▓▒░░░▒▒▓▓▓▓▓██▓▒▒▒▒▒░░░░░░░░░░▒▒▓▓▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░▒░░░▒▓▓▓▓▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓██████▓▓▒░▒░░░░░░░▒▒▒▒▒▒▒▓▓▓███▓▓▓▓▓▓▓▓█▓▓▒▒░░▒▒▒▒▒▒▓▓██▓▓▓▒▒▒▓▒▒▒░▒░░░░▒▒▒▓▓▒▒▓▓░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░▓▓▓▓▒▓▓▓▓▓▓▓▓▓▓▓▓███████▓▓▓▓▒░▒░░░░░▒▒▒▒▒▒▒▒▒▓▓▓▓███▓██▓▓▓▓▒░▒▒▒▒▒▒▒▓▓████▓▓▓▓▒▒▒▓▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓▒▓▒░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓███████▓▓▓▓▒▒▒▒▒░▒▒▒▓▓▒▒▒▓▓▓▓▓▓▓▓▓▓█▓▓▓▓▓▒▒▒▒▒▒▓▓▓▓▓█████▓▓▓▓▓▓▒▒▓▓▒▒▒▒▒▒▒▒▒▓▓▓▓▓▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░▒▓███▓▓▓██▓▓▓▓██▓██████▓▒▒▒██▓▓▒▒▒▒▒▒▓▓▒▓▓▓▓▓▓▓▓▓▓▒▒▓▓▓▒▒▒▒▒▒▒▒▓▓▓▓▓▓█████▓▓██▓▓▓▒▒▓▓▓▒▒▒▒▒▓▓▒▓▓▓▓▓▒░░░░▒▒░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░▓▒█▓▓▓██▓▓▓█▓████████▓█▓▓▓▓█▓▓▓▒▒▒▒▒▓▓▓▓▓▓▓▓▒▓▓▓▓▓▓▓▒▓▒▒▒▒▓▒▒▓▓▓█▓▓██████▓███▓▓▓▓▓▓▓▓▒▒▒▓▓▓▒▒▓▓▓▓▓▒░░░░▒░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░▒▓█▓▓▓▓▓▓▓▓▓███████████▓▓▒▓██▓▓▒▒▒▒▒▓█▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓█▓▓███████▓██▓▓▓▓▓▓▓▓▓▒▓▓▓▓▒▓▒▓▓▓▓▓▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░▒█▓▓▓▓▓▓▓▓▓███████████▓▓▓▓▓██▓▓▒▒▒▒▓██▓▓▓▓▓▓▓▓▓▓▓█▓▓▓▓▓█▓▓▓▓████████████▓█▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓█▓▓▓▓▓▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░▒▒▒░░░░░░▒▓▓█████▓▓███████████████▓███▓▓▒▓▒▒▒▓████▓████▓▓▓▓▓█▓▓█▓▓██▓█████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▒▓▓▓▓██▓▓▓▓▓▓▓▓▓▓▒░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░▒▓▒▓▓▓▓▓▒░░░░▓▓▓████▓▓████████████████████▓▓▓▓▒▒▒████████████▓███▓█▓▓▓█▓██████▓████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓██▓▓▓▓▓▓▓▓▓▒▒░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░▒▒▒▒▓▓▓▓▓▓▒▒▒▓▓▓███▓▓▓▓██▓▓████████████████▓▓▓▒▓▒▒▒▓████████████████▓█▓▓▓▓████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓██▓▓▓▓▓▓▓░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░▒▓▓▒▒▒▓▓▓▓▓█▓██▓▓▓▓▓▓▓▓▓▓▓█▓▓██████████████████▓▓▓▒▒▒▒███████████████████████████████▓▓███▓▓▓▓▓█▓▓▓▓▓▓▓▓▓██▓▓██▓▓▓░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░▒▒▓▓▓▓▓▓▓▓▓▓▓█▓██▓▓▓▓▓▒▓▓▓▓▓█▓▓▓█████████████████▓▓▓▓▒▒▒▓██████████████████████████▓▓█████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓███████▓▓▒░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░▓▓▓▓▓▓▓▓▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓██████████████████████▓▓▓▓▓▒▓▒████████████▓▓▓▓██▓█▓██▓█▓▓█████▓▓█▓▓▓▓▓▓▓▓▓▓▓█████████▓▒▒░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░▒▓▓▓▓▓▓▓▓▓▓▒▒▒▓▓▓▓▓▓▓▓▓▓█▓▓▓██████████████████▓▓▓▓▒▓▓▓▓▓███████████████████▓▓██████████▓▓█▓▓▓▓▓▓▓▓▓█▓█████▓███▓▓▒▒░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░▒▓█▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓██████████████████▓▓▓█▓▓▓▓▓████████████████████████████████▓▓█▓▓███▓▓█████████▓▓██▓▓▓▓▓▒░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░▒▓█▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▓▓▓▓▓█████████████████▓█▓█▓▓█▓▓████████████████████████████████▓▓▓▓▓▓████████████▓▓▓██▓▓███▓▓▒▒░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░▒▓░▒▓█████▓▓▓▓▓▓▓▓▓▓▓▓▓▓████████████████▓█▓██▓█▓▓███████████████████████████████▓▓▓▓▓▓▓███████████▓▓▓███████▓▓▓▒▒░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░▒███████▓▓▓▓██▓▓▓▓▓██████████████████████▓█▓█████████████████████████████▓▓▓▓▓▓▓▓█████████████▓█████████▓▒░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░▒▒▓██▓▓████▓▓▓██▓▓▓██████████████████████▓█▓██▓▓████████████████▓▓██████▓▓▓▓▓▓▓▓▓██████████████████████▓▒░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▓▓█████▓▓█▓█▓▓████████████████████████████▓█████▓███████▓▓▓▓███████▓▓▓▓▓▓▓██████████████████████▓▒░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▓██████▓▓███▓███████████████████████████▓▓██▓███████▓▓▓████████▓▓▓▓▓▓▓▓▓████████████████████▓▓▒░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▓▓███████████████████████████████████▓▓████████▓▓█████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓██████████████████▓▓▓░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒██▓▓▓███████████████████████████▓▓▓▓███████▓▓██████▓▓▓▓▓▓▓▓▓▓▓▓▓▓██▓▓▓███████████████▓▓▓▒▒░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓░░░░▓████████████████████████▓▓▓▓▓██████▓▓▓▓▓███▓▓████▓▓▓▓▓▓▓▓██████████████████████▓▓▓▓▒░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒█████████████████████▓▓▓▓▓▓▓███▓▓▓▓▓▓▓▓▓▓▓█████▓▓▓▓▓▓▓▓▓████████████▓▓▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░███████████████████▓▓▓▓▓▓▓▓▓▓▓█▓▓███▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓███▓███████▓▓▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▓▓██████████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓█▓▓▓█████▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▓▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░");
                break;
            default:
                Console.WriteLine(@"
▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▓▓▓▒▒▒▓▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▓▒▒▒▓▓▓▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
▓▓▓▓▓▓▓▓▓▓▓▓▒▓▒▓▒▒▒▒▒▓▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▓▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
▓▓▓▓▓▓▓▓▓▓▓▓▒▓▒▒▒▓▒▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▓▓▓▓▓▒▒▒▒░░░░░▒▒▓░░░░░░▒▒░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▓█▓▒░░░░░░░░░░░░░░░░▒▓▓▓░░░░░░▒░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▓█▓░░░░░░░░░░░░░░░░░░░░░▒▓▓▒▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▒▒▓▓▓▓▓▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
▓▓▓▓▓▓▓▓▓▓▓▓▓▒▓▓▓▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒██▒▒░░░░░░░░░░░░░░░░░░░░░░▓██▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▒▓▓▓▓▓▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒░░▓█▒░░░░░░░░░░▓░░░░░░░░░░░░░▒▓█▓▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓▓██▒░░░░░░░░░▒█▓░░░░░░░░░░░░░▓██▓░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓██▒░░░░░░░░░▒███░░░░░░░░░░░░▒▒██▒░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▓▒▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒██▒░░░░░░░░░▓███▓░░░░░░░░░░░░▓██░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒░░▒░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▓▓███▒░░░▒░░░▓░████▓▒▓░░░░░░░░░░▓██░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▒▒▓▓▓▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒░▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░███▒░░▓░░▒▒██████▓█░░▓░░▒▒░▒▓██▓▒░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▓▓▓▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░███▓▓▓░▒▓▒███████▓▓▒█▒░██████▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▓▓▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
▓▓▓▓▓▓▓▒▒▒▓▒▒▒▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓█████▓█▓████████▓▓██▓███▓▒░░░░░░▒░▒░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▓▓▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓░░░░░░░░░▒█████████████████▓██▓▓▒▒▒▒▒▒▒▓▓▓▓▒░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▓▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▓▓▒░░░░▓▒▒▒▓██▓███████████████▓█████▓█▓▓▒░▒▒░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒░░░░░░░░▓▓█▓▓▓▓██████████████████████▓▓▓▓▓▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░█▓▒░░░░░▒▓█████████████████████████████▓▓▓▓▓▒▒▒▒▓▒░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
▓▓▓▓▓▓▓▓▒▓▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░▒██▓▓▒▓▓▓█▓████████████████████████████▓▓▓█████▓▓▓▒▒▓░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
▓▓▓▓▒▓▓▓▒▓▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░▒▒░░░▓████████████████████████████████████████████████▓▒▓▒▒▒░░░░▒░░░░░░░░░░░░░░░░▒▒▒░░▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
▓▓▓▓▒▓▓▒▒▓▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░▒██▓██████████████████████████████████████████████████████▓▓▓▓▓▓▓▓██▓▒░░░░░░░░░░░░░░░░░░░░▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░▒▓░░▒███████████████████████████████████████████████████████▓████▓▒▒▓█▓░░░░░░░░░░░░░░░░░░░▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░▒░▒▓█████████████████████████████████████████████████████████▓░░▒░░▒░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░░░░░░░▓███████████████████████████████████████████████████████▒░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░▒░░░░░▒████████████████████████████████████████████████████████▒▒░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░▓█▓▓█████████████████████████████████████████████████████████▓█▓▓▒▒░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░░░░░▓███████████████████████████████████████████████████████████████▓▓▒▒░░░░░░░░░░░▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░░▒▒▓████████████████████████████████████████████████████████████████▒▓▒░░░░░░░░░░░▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
▓▓▓▓▓▓▓▒▓▓▓▓▒▒▒▒▒▒▒▒▒▒░░░░░░░░░░░░░▒▓▒░░░░▒▓███████████▓████████████████████████████████████████████████████▓▓▓▒░░░░░▒▒░░░▒░▒▒▒▒▒▒▓▓▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓
▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒░░░░░░░░░░░░░▒████████████████▓░░▓████████████████████████████████████▓▓███████████████▓▓▓▒░░▒▒░░░▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓
▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒░░░░░░░░░░░░░░░▒▓█████████████▓░░░████████████████████████████████████▓░░▓▒░▓██████████████▓▓▓▓░▒░▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓
▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒░░░░░░░░░░░░░░░░▒████████████▓▒▒▒░▓███████████████████████████████████▓█▒░▒░░▒░░▒▓████████████▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓
▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒░░░░░░░▒▒▒░░▒▒▓▓███████████▓▒▒░░░░▓███████████████████████████████████▓██░░░░░░░▒▓███████████████▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓
▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒░░░░░░░░░░░▓▓▓█████████████▒▒▒▒░░░▓████████████████████████████████████▓▓█░░░░░░▒▓██████████████▓███▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓
▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒░░░░░░░░░░░░▒██████████████████▒░░░▒██████████████████████████████████████▓▓▒░░░░░▓█▓▓██████████████████▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓
▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒░░░░░░░░▒███████████████▓▒▒░░░░░▒███████████████████████████████████████▓▓░░░░▒▓░░░▒████████████████████▓▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓
▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒░░░▒▒▓▓██████████▓▓▒░▒░░░░░░░░▓███████████████████████████████████████▓▓▓░░░▒░░░░▒█▓▓▒▒▓█████████████▓▓▓▓▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓
▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒░▒▓▓▓██████████▓▓░░▒░░░░░░░░░░░▒█████████████████████████████████████████▓▓▒░░░░░░░▓▓░░░░▓▒▒▓█████████████▓▓▒▒▒▓▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓
▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒░▒▓████████▓▒░░░░░░░░░░░░░░░░▒▒▓███████████████████████████████████████████▓▒░░░░░░░▓░░░▒░░░▒▒▒▒▒▓████████████▓▓▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓
▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▓▒▒▒████████▒░░░░░░░░░░░░░░░░▒██▓█████████████████████████████████████████████▓▒░░░░░░░▒░░░░░▒▒▒▒▒▒▒▒▓███████████▓▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓
▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▓█▓████████▓▒░░░░░░░░░░░░▒▓████▓▓█████████████████████████████████████████████▓░░░░░░░░░░░░░░▒▒▒▒▒▒▒▒▒▓██████████▓▓▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓
▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▓█████▓███▓▓▒░░░░░░░▒▒▓███████████████████████████████████████████████████████▒░░░░░░░░░░░░░▒▒▒▒▒▒▒▒▒▒▒▒▓█████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▓█████▒▓███▓▒▒░▒▓▓████████████████████████████████████████████████████████████▓▒░░░░░░▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓████████▓▓▓▓▓▓▓▓▓▓▓▓▓
▓▓▓▓▓▓▓▓▓▓▓▒▒▓▒▒▒▒▒▒▒▓████▓░░▓██▓▓▒▒▒▓██████████████████████████████████████████████████████████████▓▒░░░░░░░░▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒███████▓▓▓▓▓▓▓▓▓▓▓▓▓
▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▓█████▒░▒▓███▓▒▒▓█████████▓▓▓███████████████████████████████████████████████████▓▒░░▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓█████████▓▓▓▓▓▓▓▓▓
▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▓████▓▒░░▓█████████████▓▒▒▓█████████████████████████████████████████████████████▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓████████▓▓▓▓▓▓▓▓▓
▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▓█████▓▓█████████████▓▒▒▒▓██████████████████████████████████████████████████████▓▓▓▒▒▒▒▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓██████▓▓▓▓▓▓▓▓▓▓
▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▓█████████████████▓▒▒▒▒▒▓███████████████████████████████████████████████████████▓▓▓▒▒▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓██████▓▓▓▓▓▓▓▓▓▓
▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▓▒▒▓██▓███████████▓▓▓▒▒▒▒▓█████████████████████████████████████████████████████████▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒██████▓▓▓▓▓▓▓▓▓▓
▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒███████████████▓▓▒░▒▒▒▒▒▓████████████████████████████████████████████████████████████▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓█████▓▓▓▓▓▓▓▓▓▓▓
▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▓▓▓▓████████████▓▓▓▒▒▒▒▒▒▒▒▒▒█████████████████████████████████████████████████████████████████▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓█████▓▓▓▓▓▓▓▓▓▓▓▓
▓▓▓▓▓▓▓▓▓▓▓▒▓▒▒▒▓▓███████████▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓███████████████████████████████████████████████████████████████████▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▓▒▓█████▓▓▓▓▓▓▓▓▓▓▓▓▓
▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓█████████▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓█████████████████████████████████████████████████████████████████████▓▓▓▓▒▒▒▒▒▒▒▒▓▒▓██████▓▓▓▓▓▓▓▓▓▓▓▓▓▓
▓▓▓▓▓▓▓▓▓▓▓▓▓███████▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓████████████████████████████████████████████████████████████████████▓▓▓▓█▓▒▒▒▒▒▒▒▒▓███████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
▓▓▓▓▓▓▓▓▓▓▓█████▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓████████████████████████████████████████████████████████████████████████▓▓▓▓▓▓▓▓▓▓▓████▓▓▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓
▓▓▓▓▓▓▓▓▓█████▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓█▓▓██████████████████████████████████████████████████████████████████████████▓▓▓▓▓▓▓▓███▓▓▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓
▓▓▓▓▓▓▓▓████▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓█▓███████████████████████████████████████████████████████████████████████████▓▓▓▓▓▓▓▓██▓▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓
▓▓▓▓▓▓▓███▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓█▓██████████████████████████████████████████████████████████████████████████████████▓██▒▓▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓
▓▓▓▓▓▓███▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓██████████████████████████████████████████████████████████████████████████████████▓▓██▓█▓▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓
▓▓▓▓▓██▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓███████▓▓▓█████████████████████████████████████████████████████████████████████████████████▓▓▒▒▒▓▒▒▓▓▓▓▓▓▓▓▓▓▓
▓▓▓▓██▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓██▓▓▓████████████████████████████████████████████████████████████████████████████████████▓▓▒▒▓▒▒▒▓▓▓▓▓▓▓▓▓▓
▓▓▓▓█▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓█████▓▓▓█████████████████████████████████████████████████████████████████████████████████▓█▓▓▓▓▓▓▒▒▓▓▓▓▓▓▓▓▓▓
▓▓▓█▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒█████████▓▓▓██████████████████████████████████████████████████████████████████████████████████████▓▓▓▒▒▒▒▓▓▓▓▓▓▓▓▓▓
▓▓▓█▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓████████████▓████████████████████████████████████████████████████████████████████████▓███████████████▓▓▒▒▒▓▓▓▓▓▓▓▓▓▓
▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓████████████▓▓▓█████████████████████████████████████████████████████████████████████▓▒▓▓▒▒▓▓▓█████████████▓▓▓▓▓▓▓▓▓▓▓▓▓
▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓███████████████████████████████████████████████████████████████████████████████▓▒▓▓▒▒▒▒▒▒▓█████████████▓▓▓▓▓▓▓▓▓▓▓
▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓██████▓▓████████████████████████████████████████████████████████████████████████▒▒▒▒▒▒▒▒▒▒▒▓██████████████▓▓▓▓▓▓▓▓
▓▓▓▓▓▓▓▓▓▓▒▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓████████████████████████████████████▓███████████████████████████████████████████▓▒▒▒▒▒▒▒▒▒▒▒▓▓██████████████▓▓▓▓▓▓
▓▓▓▓▓▓▓▓▓▓▒▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒██████████████████████████████████████▓███████████████████████████████████████████▓▓▒▒▒▒▒▒▒▒▒▒▒▓▓███████████████▓▓▓
▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓█████████████████████████████████████████████████████████████████████████████████████▓▒▒▒▒▒▒▒▒▒▒▓▓▓█████████████████
▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓████▓▓▓█████████████████████████████████████████████████████████████████████████████████▓▒▒▒▒▒▓▓▒▒▓▓▓▓▓███████████████
▓▓▓▓▓▓▓▓▓▒▓▓▓▓▓▒▓▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▒▒▒▓██████████████████████████████████████████████████████████████████████████████████▓▒▒▓▓▒▓▓▒▓▓▓▒▒▓██████████████
▓▓▓▓▓▓▓▓▓▒▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒█████████████████████████████████████████████████████▓███████████████████████████████▓▒▓▓▒▒▓▓▓▓▓▒▒▓▓▓████████████
▓▓▓▓▓▓▓▓▓▒▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓███████████▓██████████████████████████████████████████▓██████████████████████████████▓▓▓▓▓▒▓▓▓▓▓▒▒▓▓▓▓▓██████████
▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓███████████▓▒▒███████████████████████████████████████████▓██████████████████████████████▓▓▓▓▒▓▓▓▓▓▓▒▓▓▓▓▓▓▓████████
▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓███████████▓▒▒▒▓███████████████████████████████████████████▓▓▓████████████████████████████▓▓▓▓▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓████████
▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▓▒▒▒▒▒▒▒▒▒▒▒▓███████████▓▒▒▒▒▒▓████████████████████████████████████████████▓▓▓▓▓███▓▓██▓▓████████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓████
▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▓▒▒▒▒▒▒▒▒▒▓▓████████▓▓▒▓▒▒▒▒▒▓█████████████████████████████████████████████▓▓▓▓▒▒▓█▓▒▓▓▒▒▓▓▓▓█████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓██
▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒▒▒▓▓▓██████████▒▒▒▒▒▒▒▒▒▓███████████████████████████████████████████████▓▓▓▒▒▒▓▓▒▒▓▒▒▓▒▒▓█▓███████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓█
▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▓▓▒▒▓██████████▓▒▒▒▒▒▒▒▒▒▓███▓▓▓▓██████████████████████████████████████████▓▓▒▒▒▒▒▒▒▒▓▒▓▓▓▓▓▓▓▓███████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▒▓██▓███████████▒▒▒▒▒▒▒▒▒▒▓███▓▓▒▓███████████████████████████████████████████▓▓▓▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓█▓█████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓█▓███████████████▓▒▒▒▒▒▒▒▒▒▓▓██▓▒▒▒▓████████████████████████████████████████████▓▓▓▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓█████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
▓▓▓▓▓▓▓▓▓▓▓▓██████████████████████▓▓▒▒▒▒▒▒▓▓▓▓▓▓▒▒▒▓███████████████████████████████████████████▓▓▓▓▓▒▒▒▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓██████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
▓▓▓▓▓▓▓▓▓▓███████████████████████████▓▒▒▓▓▓▓▒▓▓▒▒▒▓██████████████████████████████▓█████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓█████████████▓▓▓▓▓▓▓▓▓▓▓▓▓
▓▓▓▓▓▓▓▓▓█████████████████████████████▓▓▓▓▒▒▒▒▒▒▒▓▓██████████████████████████████████████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓███████████▓▓▓▓▓▓▓▓▓▓▓▓
▓▓▓▓▓▓▓██████▓█████████████▓████████████▓▓▒▒▒▒▒▒▓▓███████████████████████████████████████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓█████████████▓▓▓▓▓▓▓▓▓
▓▓▓▓▓▓██████▓▓▓▓██████████▓▓█▓▓██████████▓▒▒▒▒▒▓▓█████████████████████████████████████████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓████████████▓▓▓▓▓▓▓▓
▓▓▓▓▓████▓▓▓▓▓▓▓██████████▓▒▓▓▓▓▓▓▓▒▓▓▓███▓▓▒▒▒▓▓█████████████████████████████████████████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓██████████▓▓▓▓▓▓
▓▓▓▓▓████▓▓▓▓▓▓█████▓▓████▓▓▓▓▒▒▓▓▓▒▒▒▒▒▒▓▓█▓▓▓▓▓██████████████████████████████████████████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓██████████▓▓▓▓▓
▓▓▓▓███▓▓▓▓▓▓▓▓████▓▓▓▓███▓▓▓▓▓▓▓▓▓▒▓▒▒▒▒▒▒▓▓▓▓▓▓███████████████████████████████████████████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓█████████▓▓▓▓
▓▓▓███▓▓▓▓▓▓▓▓▓███▓▓▓▓▓▓██▓▓▓▓▓▓▓▓▓▓▓▓▓▒▒▒▓▓▓▓▓▓█████████████████████████████████████████████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓████████▓▓▓
▓▓███▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓█▓▓▓▓▓▓▓▓▓▓▓▓▒▒▓▓▓▓▓█████████████████████████████████████████████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓████████▓▓▓
▓███▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓██▓▓▓▓▓▓▓▓▓▓▓▒▓▓▓▓▓▓██████████████████████████████████████████████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓███████▓▓▓▓
▓██▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓█▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓████████████████████████████████████████████████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓████████▓▓▓▓
██▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓█▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓██████████████████████████████████████████████████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓█████████████████
█▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓█▓▓▓▓▓▓▓▓▓▓▓▓▓▓█████████████████████████████████████████████████████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓██████████████████████████
▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓███████████████████████████████████████████████████████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓█████████████████████████████
▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓█████████████████████████████████████████████████████████████████▓▓▓▓▓▓▓▓▓▓▓████████████████████████████████
▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓███████████████████████████████████████████████████████████████████▓▓▓▓▓▓▓▓▓█████████████████████████████████
▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓████████████████████████████████████▓████████████████████████████████▓▓▓▓▓▓▓▓▓█████████████████████████████████
▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓███████████████████████████████████████▓██████████████████████████████▓▓▓▓▓▓▓▓▓█████████████████████████████████
▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓██████████████████████████████████████████████████████████████████████████▓▓▓▓▓▓▓▓▓███████████████████████████████
█▓▓▓██▓██▓▓▓▓█▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓███████▓████████████████████████████████████████████████████████████████████▓▓▓▓▓▓▓▓▓▓█████████████████████████████
████████████████████▓█▓▓▓▓▓▓▓▓▓▓██▓████████████████████████████████████████████████████████████████████████████▓▓▓▓▓▓▓▓▓▓▓▓▓██████████████████████████
████████████████████████▓▓▓█▓███████████████████████████████████████████████████████████████████████████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓████████████████████████
████████████████████████████████████████████████████████████████████████████████████████████████████████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓█████████████████████
████████████████████████████████████████████████████████████████████████████████████████████████████████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓██▓▓▓███████████████
█████████████████████████████████████████████████████████████████████████████████████████████████████████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓█▓█████████████
█████████████████████████████████████████████████████████████████████████████████████████████████████████████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓█████████████
██████████████████████████████████████████████████████████████████████████████████████████████████████████▓████▓▓██▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓█▓███████████
█████████████████████████████████████████████████████████████████████████████████████████████████████▓▓▓█████▓▓▓▓███████▓▓▓▓▓▓▓▓▓▓▓███████████████████
███████████████████████████████████████████████████████████████████████████████████████████████████████▓▓▓▓▓████████████████▓▓▓▓▓▓▓███████████████████");
                break;
        }
        Console.WriteLine();
        enemy.WriteStats();
        Console.Write("\nPress any key to continue... ");
        Console.ReadKey();
        Console.Clear();
        Console.WriteLine("\n");
        
    }

    public void PrintRoom() {
        Console.WriteLine("\n\n");
        for (int i = 0; i < xSize; i++) {
            for (int j = 0; j < ySize; j++) {
                // Writs the player and each of the enemies
                if (player.X == i && player.Y == j)
                    WriteTile("Y ", ConsoleColor.Cyan);
                else if (enemies.Any(enemy => enemy.X == i && enemy.Y == j))
                    foreach (Enemy enemy in enemies) {
                        WriteEnemies(enemy, i, j);
                    }
                else {
                    // Writes the tiles according to type.
                    switch (Room[i, j].Type) {
                        case TileType.Empty:
                            WriteTile(". ", ConsoleColor.Black);
                            break;
                        case TileType.Wall:
                            WriteTile("# ", ConsoleColor.Black);
                            break;
                        case TileType.Portal:
                            WriteTile("O ", ConsoleColor.Blue);
                            break;
                        case TileType.HealingPotion:
                            WriteTile("o ", ConsoleColor.Red);
                            break;
                        case TileType.Gold:
                            WriteTile("o ", ConsoleColor.Yellow);
                            break;
                    }
                }

                // Prints interface
                Interface.Clear();
                player.UpdateStats();
                UpdateInterface(new(){{0, $"   Controls: Movement (WASD), Inventory (1-9), Quit (Q)"}});
                UpdateInterface(player.GetInventory());
                UpdateInterface(player.GetStats());

                foreach (KeyValuePair<int, string> kvp in Interface)
                    if (i == kvp.Key && j == ySize-1) Console.Write(kvp.Value);
            }
            Console.WriteLine();
        }

    }

    public static void WriteEnemies(Enemy enemy, int i, int j) {
        if (enemy.X == i && enemy.Y == j && enemy.Deity == DeityEnum.Sacrifice)
            WriteTile("! ", ConsoleColor.DarkRed);
        else if (enemy.X == i && enemy.Y == j && enemy.Deity == DeityEnum.Enigma)
            WriteTile("! ", ConsoleColor.DarkMagenta);
        else if (enemy.X == i && enemy.Y == j && enemy.Deity == DeityEnum.Harvest)
            WriteTile("! ", ConsoleColor.DarkGreen);
        else if (enemy.X == i && enemy.Y == j && enemy.Deity == DeityEnum.End)
            WriteTile("! ", ConsoleColor.White);
    }
    public static void WriteTile(string str, ConsoleColor color) {
        Console.ForegroundColor = color;
        Console.Write(str);
        Console.ForegroundColor = ConsoleColor.White;
    }
     public static bool ProcessInput() {
        Console.Write("> ");
        char input = char.ToLower(Console.ReadKey().KeyChar);
        Console.WriteLine();

        switch (input) {
            case 'w':
                player.Move(-1, 0);
                break;
            case 'a':
                player.Move(0, -1);
                break;
            case 's':
                player.Move(1, 0);
                break;
            case 'd':
                player.Move(0, 1);
                break;
            case 'q':
                return false;
            case 'r':
                player.X += 2;
                player.Y += 2;
                break;
        }
        return true;
    }
    public static void UpdateInterface(Dictionary<int, string> Dict) {
    foreach (KeyValuePair<int, string> kvp in Dict)
        try {Interface.Add(kvp.Key, kvp.Value);} catch {}
    }
}

public static void DisplayTitle() {
    Console.ForegroundColor = ConsoleColor.DarkRed;
    Console.WriteLine(@"
 ▄████████    ▄█    █▄     ▄██████▄   ▄██████▄     ▄████████    ▄████████         ▄████████      ████████▄     ▄████████  ▄█      ███     ▄██   ▄   
███    ███   ███    ███   ███    ███ ███    ███   ███    ███   ███    ███        ███    ███      ███   ▀███   ███    ███ ███  ▀█████████▄ ███   ██▄ 
███    █▀    ███    ███   ███    ███ ███    ███   ███    █▀    ███    █▀         ███    ███      ███    ███   ███    █▀  ███▌    ▀███▀▀██ ███▄▄▄███ 
███         ▄███▄▄▄▄███▄▄ ███    ███ ███    ███   ███         ▄███▄▄▄            ███    ███      ███    ███  ▄███▄▄▄     ███▌     ███   ▀ ▀▀▀▀▀▀███ 
███        ▀▀███▀▀▀▀███▀  ███    ███ ███    ███ ▀███████████ ▀▀███▀▀▀          ▀███████████      ███    ███ ▀▀███▀▀▀     ███▌     ███     ▄██   ███ 
███    █▄    ███    ███   ███    ███ ███    ███          ███   ███    █▄         ███    ███      ███    ███   ███    █▄  ███      ███     ███   ███ 
███    ███   ███    ███   ███    ███ ███    ███    ▄█    ███   ███    ███        ███    ███      ███   ▄███   ███    ███ ███      ███     ███   ███ 
████████▀    ███    █▀     ▀██████▀   ▀██████▀   ▄████████▀    ██████████        ███    █▀       ████████▀    ██████████ █▀      ▄████▀    ▀█████▀ 

");
Console.ForegroundColor = ConsoleColor.White;
}

public static int GetChoice(int speed = 0, int duration = 100, params string[] Choices) {
    int maxChoices = Choices.Length;
    Console.ForegroundColor = ConsoleColor.White;
    Console.WriteLine();
    for (int i = 0; i < Choices.Length; i++) {
        Print(string.Format("[{0}] {1}", i+1, Choices[i].ToString()), speed, duration);
    }

    Console.Write("> ");
    bool ValidChoice = int.TryParse( Console.ReadKey().KeyChar + "", out int Choice);

    if (!ValidChoice || Choice < 1 || Choice > maxChoices) {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("\nInvalid Input. Please try again.");
        Sleep(800);
        Console.Clear();
        Choice = GetChoice(speed, duration, Choices);
    }
    Console.Clear();
    return Choice;
}

    static void Main() {
    // DisplayTitle();
    // player.Think("A mind shattering pain pierced my head.");
    // player.Think("I flinched from the pain and noticed something in front of me.");
    // player.Think("What is this place..?");
    // player.Think("Four giant doors guarded by statues loom ominously before me.");
    // bool flag = true;
    // while (flag) {
    //     // player.Talk("Where do I go?");

    //     int choice = GetChoice("Blood-Horned Door", "Twin-Mask Door", "Thorn-Blooming Door", "Ankh-Ornated Door", "Simple Door");
    //     player.Think("I approached the door.");
    //     if (choice == 1) {
    //         // player.Narrate("The door stood tall and imposing, adorned with twisted horns portruding from every corner, their tips stained crimson with the blood of the unfortunate, each curve and jagged edge instills a primal fear in those who dare approach.");
    //         // player.Think("The plaque below the statue reads... \"protection for a price.\"");
                // player.ChooseDeity(DeityEnum.Sacrifice); // Turns false when the player enters the door.
    //         if (!flag)
        player.SetDeity(DeityEnum.Sacrifice);
        WandererRoute();
    //     } else if (choice == 2) {
    //         player.Narrate("The door stood tall and magical, adorned with twin masks, one serene and the other solemn, their intricate designs pulsating with an otherworldly glow, while delicate tendrils of shimmering mist curled around the edges, obscuring the threshold in a veil of enchantment, hinting at the mysteries that lie beyond.");
    //         player.Think("The plaque below the statue reads... \"Seek forbidden knowledge.\"");
    //         flag = ChooseDeity("The Enigma");
    //         // if (!flag)
    //         //     WisenedRoute();
    //     } else if (choice == 3) {
    //         player.Narrate("The door stood tall and weathered, adorned with gnarled vines that twisted and coiled around its frame, their thorns glistening with a malevolent gleam as if hungry for the touch of unwary hands, while a faint aroma of fresh earth and ripened fruit wafted from the intricate carvings depicting fields of golden grain and lush orchards thriving beneath the shadow of a towering treant, its branches outstretched in a gesture of both protection and expectation.");
    //         player.Think("The plaque below the statue reads... \"A bounty for the patient.\"");
    //         flag = ChooseDeity("The Harvest");
    //         // if (!flag)
    //         //     HarvestRoute();
    //     } else if (choice == 4) {
    //         player.Narrate("The door loomed ominously, its obsidian surface engulfed in swirling crimson tendrils, each etching of the ankh symbol, a silent promise of finality and inevitability, a gateway to the abyssal realm of shadows where every step may lead to the precipice of final embrace.");
    //         player.Think("The plaque below the statue reads... \"Find solace in the inevitable.\"");
    //         flag = ChooseDeity("The End");
    //         // if (!flag)
    //         //     EndRoute();
    //     } else if (choice == 5) {
    //         player.Narrate("Just a simple door.");
    //         flag = ChooseDeity("None");
    //         // if (!flag)
    //         //     DeitylessRoute();
    //     }
    // }
}

// Optimized RNG.Next()
public static int NextInt(params dynamic[] n) {
    Random RNG = new();
    if (n.Length == 1)
        return RNG.Next(n[0]);
    else
        return RNG.Next(n[0], n[1]);
} 

}