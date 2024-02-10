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
public static Player player = new(5, 5, ConsoleColor.White, "Player");
public static Sacrifice SACRIFICE = new(30, 1000, ConsoleColor.DarkMagenta, "ENIGMA");
public static Enigma ENIGMA = new(5, 5, ConsoleColor.DarkRed, "SACRIFICE"); // 42, 1300
public static Harvest HARVEST = new(55, 1000, ConsoleColor.DarkGreen, "HARVEST");
public static End END = new(50, 1000, ConsoleColor.Black, "END");
public static Chaos CHAOS = new(60, 1000, ConsoleColor.DarkMagenta, "CHAOS");
public static bool StageClear = false;
// Write the battle encounter system with interface that is similar to Undertale but in text-version. Whosoever's SPD is higher, they will go first. The options are: Attack, Inventory, Flee. When the player selects attack, an interface of possible attacks/skills will show, and the same goes for inventory. There should be numbers corresponding to the option to get player input (i.e., [1] Attack, [2] Inventory, etc.). 
// Room global variables
public static Tile[,] Room = new Tile[0, 0];
public static RoomGenerator RoomGen = new();    public static List<Enemy> enemies = [];
// UI variables
public static Random RNG = new();
public static Dictionary<int, string> Interface = [];
public class Deity(int tspeed = 35, int tduration = 450, ConsoleColor color = ConsoleColor.White, string name = "???") {
    public int tspeed = tspeed, tduration = tduration;
    public ConsoleColor color = color;
    public string name = name;
        public void Talk(string str) {
        Program.Print(str, tspeed, tduration, color, name);
    }
}
public class Sacrifice(int tspeed = 35, int tduration = 450, ConsoleColor color = ConsoleColor.White, string name = "???") : Deity(tspeed, tduration, color, name) {

}
public class Enigma(int tspeed = 35, int tduration = 450, ConsoleColor color = ConsoleColor.White, string name = "???") : Deity(tspeed, tduration, color, name) {
}
public class Harvest(int tspeed = 35, int tduration = 450, ConsoleColor color = ConsoleColor.White, string name = "???") : Deity(tspeed, tduration, color, name) {

}
public class End(int tspeed = 35, int tduration = 450, ConsoleColor color = ConsoleColor.White, string name = "???") : Deity(tspeed, tduration, color, name) {

}
public class Chaos(int tspeed = 35, int tduration = 450, ConsoleColor color = ConsoleColor.White, string name = "???") : Deity(tspeed, tduration, color, name) {
}
public class Deityless(int tspeed = 35, int tduration = 450, ConsoleColor color = ConsoleColor.White, string name = "???") : Deity(tspeed, tduration, color, name) {

}
public class Enemy(int x, int y)
    {
    public int X = x, Y = y, EXP = NextInt(17+(player.Stage*3), 25+(player.Stage*3));
    public int HP, ATK, DEF, INT, SPD, LCK, GLD;
    public DeityEnum Deity = DeityList[NextInt(DeityList.Count-1)];
    public string Name = "???", DeityName = "None";
    public bool IsDefeated = false;
    public void Initialize() {
        DeityName = "THE " + Deity.ToString().ToUpper();
        string[][] Names = [["Bloodbound Fiend", "Graveborn Revenant", "Painforged Emissary"], 
                            ["Mind Walker", "Twilight Herald", "Dream Specter"], 
                            ["Bleeding Orchardgeist", "Weeping Golem", "Fanged Treant"], 
                            ["Voidborn Wraith", "Oblivion Scourge", "Ancient Desolator"]];
        string[] DeityNames = Enum.GetNames(typeof(DeityEnum));
        
        // Checks the deity of the enemy and assigns a name for it based on their deity.
        for (int i = 0; i < DeityNames.Length; i++)
            if (Deity.ToString() == DeityNames[i])
                Name = Names[i][NextInt(Names.Length-1)];
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

public class Player {
    // Dialogue variables
    public int tspeed, tduration;
    public ConsoleColor color;
    // Attribute variables
    public string name, DeityName;
    public int HP, ATK, DEF, INT, SPD, LCK, GLD, EXP, MaxEXP, LVL, PTS, X, Y, Stage,  TotalKills, SacrificeKills, EnigmaKills, HarvestKills, EndKills, IntHealth, IntMaxHealth;
    public double Health, MaxHealth, Damage, Armor;
    public dynamic ChosenDeity;
    public List<dynamic> inventory;
    public DeityEnum Deity;
    // Map variables
    public int spawnX = NextInt(1, 20), spawnY = NextInt(1, 26);
    public Player(int tspeed = 35, int tduration = 450, ConsoleColor color = ConsoleColor.White, string name = "???") {
        // Dialogue variables
        this.X = spawnX;
        this.Y = spawnY;
        this.tspeed = tspeed;
        this.tduration = tduration;
        this.color = color;
        // Info variables
        this.name = name;
        this.Stage = 1;
        this.inventory = [];
        this.DeityName = "None";
        this.Deity = DeityEnum.None;
        this.ChosenDeity = new Deityless();
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
        this.Damage = ATK;
        this.Armor = Math.Round(DEF*1.5);
    // Methods
    }
    public void Talk(string str) {
        Program.Print(str, tspeed, tduration, color, name);
    }

    public void EvaluateEXP() {
        if (EXP >= MaxEXP) {
            PTS += 2;
            EXP -= MaxEXP;
            LVL++;
            UpdateStats();
        }
    }
    public void Think(string str) {
        Program.Print(string.Format("({0})", str), tspeed, tduration, ConsoleColor.DarkGray);
    }

    public void Narrate(string str) {
        Program.Print(string.Format("[{0}]", str), tspeed, tduration, ConsoleColor.White);
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
        List<string> strings = [string.Format("   Level: {0, -13} Gold: {1}", LVL, GLD), string.Format("   Health: {0}/{1, -8} EXP: {2}/{3}", Health, MaxHealth, EXP, MaxEXP), string.Format("   Armor: {0, -13} Deity: {1}", Armor, DeityName), "   --------------------------------------------", string.Format("   HP: {0, -16} DEF: {1}", HP, DEF), string.Format("   ATK: {0, -15} INT: {1}", ATK, INT), string.Format("   SPD: {0, -15} LCK: {1}", SPD, LCK), string.Format("   Kills: {0, -13} Room: {1}", TotalKills, SacrificeKills, EnigmaKills, HarvestKills, EndKills, Stage)];

        for (var i = 0; i < strings.Count; i++)
            StatsDict.Add(2+i, strings[i]);

        return StatsDict;
    }
    public void SetDeity(DeityEnum Deity) {
        if (Deity != DeityEnum.None) {
            DeityName = "THE " + Deity.ToString().ToUpper();
            DeityList.Remove(Deity);
        }
    }

    public void UpdateStats(bool updateHealth = false) {
        MaxEXP = 80 + LVL*20;
        MaxHealth = 20 + HP*8;
        Armor = Math.Round(DEF*1.5);
        if (updateHealth)
            Health = MaxHealth;
    }
    public bool ChooseDeity(DeityEnum chosen) {
    int choice = GetChoice("Enter the door.", "Go back.");
    if (choice == 1) {
        SetDeity(chosen);
        return false;
    } else
        return true;
}
}



public static void WandererRoute() {
    // Sacrifice.name = "???";
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
    SACRIFICE.name = "THE SACRIFICE";
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


public static void Print(string str, int speed = 5, int duration = 5, ConsoleColor color = ConsoleColor.White, string name = "") {
    Console.ForegroundColor = color;

    if (!string.IsNullOrEmpty(name))
        str = str.Insert(0, name + ": ");

    foreach (char c in str) {
        Console.Write(c);
        Sleep(speed); 
    }
    Console.WriteLine();
    Sleep(duration);
    
    Console.ResetColor();
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
            StageClear = false;
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
            InitializeWalls((xSize-2)*(ySize-2)/5);
            InitializeEnemies(15+(xSize-2)*(ySize-2)/70); 
            InitializeItems();
            // Randomizes and teleports player to random spawnpoint.
            player.spawnX = NextInt(1, 20);
            player.spawnY = NextInt(1, 26);
            player.X = player.spawnX;
            player.Y = player.spawnY;
    }

    public void InitializeEnemies(int maxEnemies) {
        while (enemies.Count < maxEnemies) {
            int x = NextInt(1, xSize-1);
            int y = NextInt(1, ySize-1);
            if (Room[x, y].Type != TileType.Wall) {
                enemies.Add(new Enemy(x, y));
                enemies[^1].Initialize();
            }
        }
    }
    public void InitializeItems() {
        int HealingPotions = NextInt(1, 6);
        int Golds = NextInt(1, 5); 
        int CurrentHealingPotions = 0, CurrentGolds = 0;
        while (CurrentHealingPotions != HealingPotions || CurrentGolds != Golds) {
            int x = NextInt(1, xSize - 1);
            int y = NextInt(1, ySize - 1);
            
            if (Room[x, y].Type == TileType.Empty && CurrentHealingPotions != HealingPotions)
                Room[x, y] = new Tile(TileType.HealingPotion);
            else if (Room[x, y].Type == TileType.Empty && CurrentGolds != Golds)
                Room[x, y] = new Tile(TileType.Gold);
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

            // Checks if player encounters enemy
            foreach (Enemy enemy in enemies)
                if (player.X == enemy.X && player.Y == enemy.Y) 
                    Encounter(player, enemy);

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
                    int HealAmount = NextInt(10, player.IntMaxHealth/5); // Determine the healing amount
                    player.Health += Math.Clamp(HealAmount, HealAmount, player.MaxHealth);
                    player.Narrate($"You found a Healing Potion! Restored {HealAmount} health.");
                    Room[player.X, player.Y] = new Tile(TileType.Empty);
                    break;
                case TileType.Gold:
                    int GoldAmount = NextInt(5, 5+(player.LCK*5/2));
                    player.GLD += GoldAmount;
                    player.Narrate($"You found Gold! Gained {GoldAmount} gold.");
                    Room[player.X, player.Y] = new Tile(TileType.Empty);
                    break;
            }
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
        Console.Write($" {enemy.Name}");
    
        enemy.Defeat();
        if (player.TotalKills % 5 == 0 && !StageClear) {
            Room[player.spawnX, player.spawnY] = new Tile(TileType.Portal);
            StageClear = true;
        }
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
                        WriteTile("p ", ConsoleColor.Red);
                        break;
                    case TileType.Gold:
                        WriteTile("o ", ConsoleColor.Yellow);
                        break;
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
        Console.ResetColor();
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
Console.ResetColor();
}

public static int GetChoice(params string[] Choices) {
    int maxChoices = Choices.Length;
    Console.ResetColor();
    Console.WriteLine();
    for (int i = 0; i < Choices.Length; i++) {
        Print(string.Format("[{0}] {1}", i+1, Choices[i].ToString()), 20, 100);
    }

    Console.Write("> ");
    bool ValidChoice = int.TryParse( Console.ReadKey().KeyChar + "", out int Choice);

    if (!ValidChoice || Choice < 1 || Choice > maxChoices) {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("\nInvalid Input. Please try again.");
        Sleep(800);
        Console.Clear();
        Choice = GetChoice(Choices);
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
public static int NextInt(params int[] n) {
    Random RNG = new();
    if (n.Length == 1)
        return RNG.Next(n[0]);
    else
        return RNG.Next(n[0], n[1]);
} 

}