using System.Media;
using System.Reflection.Metadata;
using static System.Threading.Thread;
public enum TileType { Empty, Wall }
public class Tile {
    public TileType Type { get; set; }

    public Tile(TileType type)
    {
        Type = type;
    }
}
public class Program {
    // Global variables
    public static Player player = new(5, 5, ConsoleColor.White, "Player");
    public static Enemy enemy = new(5, 5);
     public static Deity End = new(50, 1000, ConsoleColor.Black, "The End");
     public static Deity Wisened = new(30, 1000, ConsoleColor.DarkMagenta, "The Wisened");
     public static Deity Wanderer = new(5, 5, ConsoleColor.DarkRed, "The Wanderer"); // 42, 1300
     public static Deity Harvest = new(55, 1000, ConsoleColor.DarkGreen, "The Harvest");
     public static Tile[,] room = new Tile[xSize, ySize];
     public static Random rng = new();
     public static int xSize = 27, ySize = 33;
     public static MapGenerator? mapGenerator;
     public static List<Enemy> enemies = [];

public class Deity {
    public int tspeed, tduration;
    public ConsoleColor color;
    public string name;
    public Deity(int tspeed = 35, int tduration = 450, ConsoleColor color = ConsoleColor.White, string name = "???") {
        this.tspeed = tspeed;
        this.tduration = tduration;
        this.color = color;
        this.name = name;
    }
    // Dialogue properties

    public void Talk(string str) {
        Program.Print(str, tspeed, tduration, color, name);
    }
}
public class Enemy {
    public int X, Y;

    public Enemy(int x, int y) {
        X = x;
        Y = y;
    }

    // Method to randomly move the enemy
    public void MoveRandom() {
        int[] dx = { -1, 0, 1, 0 }; // Up, Right, Down, Left
        int[] dy = { 0, 1, 0, -1 };

        // Attempt to move in a random direction
        for (int attempts = 0; attempts < 4; attempts++) {
            int direction = rng.Next(4);
            int newX = X + dx[direction];
            int newY = Y + dy[direction];

            // Check if the new position is within bounds and not a wall
            if (newX >= 0 && newX < xSize && newY >= 0 && newY < ySize && room[newX, newY].Type != TileType.Wall)
            {
                X = newX;
                Y = newY;
                break; // Successfully moved
            }
        }
    }
}

public class Player {
    public int tspeed, tduration;
    public ConsoleColor color;
    public string name, DeityName;
    public int HP, ATK, DEF, INT, SPD, LCK, GLD, EXP, maxEXP, LVL, X, Y;
    public double Health, maxHealth, Damage, Armor;
    public Deity deity;
    public Player(int tspeed = 35, int tduration = 450, ConsoleColor color = ConsoleColor.White, string name = "???") {
        // Dialogue variables
        this.X = 1;
        this.Y = 7;
        this.tspeed = tspeed;
        this.tduration = tduration;
        this.color = color;
        this.name = name;
        this.DeityName = "None";
        // Attribute variables
        this.HP = 10;
        this.ATK = 10;
        this.DEF = 10;
        this.INT = 10;
        this.SPD = 10;
        this.LCK = 10;
        this.LVL = 1;
        // Stats variables
        this.GLD = 100;
        this.EXP = 0;
        this.maxEXP = 80 + LVL*20;
        this.Health = 20 + HP*8;
        this.maxHealth = Health;
        this.Damage = ATK;
        this.Armor = Math.Round(DEF*1.5);
        this.deity = new Deity(DeityName);
    }    public void Talk(string str) {
        Program.Print(str, tspeed, tduration, color, name);
    }

    public void Think(string str) {
        Program.Print(String.Format("({0})", str), tspeed, tduration, ConsoleColor.DarkGray);
    }

    public void Narrate(string str) {
        Program.Print(String.Format("[{0}]", str), tspeed, tduration, ConsoleColor.White);
    }

    public void Move(int dx, int dy) {
        int newX = X + dx;
        int newY = Y + dy;
        if (newX >= 0 && newX < xSize && newY >= 0 && newY < ySize && room[newX, newY].Type != TileType.Wall) {
            X = newX;
            Y = newY;
        }
    }

    public void setDeity(string deity) {
        if (deity != "None")
            this.DeityName = "The" + deity.ToString();
    }

    public void updateStats() {
        this.maxEXP = 80 + LVL*20;
        this.Health = 20 + HP*8;
        this.Armor = Math.Round(DEF*1.5);
    }
    public class Deity {
            private string name;
            public Deity(string name)
            {
                this.name = name;
            }
        }
}

public static bool ChooseDeity(string chosen) {
    int choice = GetChoice("Enter the door.", "Go back.");
    if (choice == 1) {
        player.setDeity(chosen);
        return false;
    } else
        return true;
        
}

public static void WandererRoute() {
    // Wanderer.name = "???";
    // player.Think("The door slammed shut as soon as I entered.");
    // player.Think("I feel an ominous figure watching me.");
    // Sleep(300);
    // player.Think("I hear a boisterous laugh in the distance.");
    // Sleep(300);
    // player.Think("It's getting closer...");
    // Sleep(300);
    // Wanderer.Talk("BWAHA..!");
    // Sleep(300);
    // Wanderer.Talk("BWAHAHA!");
    // player.Think("...");
    // Sleep(400);
    // Wanderer.Talk("BWAHAHAHAHAHA!");
    // player.Think("A monstrous horned-figure wearing devilish armor approached...");
    Wanderer.name = "THE WANDERER";
    // Wanderer.Talk("BLEED FOR YOUR MASTER, FOOL!");
    // player.Talk("...What are you?");
    // Wanderer.Talk("I AM THE WANDERER, MASTER OF BLOOD AND BLADE, DEITY OF THE ENDLESS FRAY!");
    // Wanderer.Talk("MY BLOOD SHALL FEED YOUR HUNGER FOR GLORY AND DOMINATION!");
    // Wanderer.Talk("DRINK MY BURNING BLOOD, SHALL YOU WISH TO DEFY DEATH HERSELF!");
    // Console.WriteLine();
    player.Narrate("You chose The Wanderer as your Deity.");
    player.Narrate("Experience the worst to become the best.");
    player.Narrate("Effects: ++ HP, + DEF, - GLD, - ATK, - SPD");
    player.HP += 5;
    player.DEF += 3;
    player.GLD -= 50;
    player.ATK -= 3;
    player.SPD -= 3;
    player.updateStats();
    Sleep(1000);
    Console.Clear();
    mapGenerator = new MapGenerator(xSize, ySize);
    mapGenerator.InitializeRoom();
    mapGenerator.DisplayRoom();
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
    
    Console.ForegroundColor = ConsoleColor.White;
}

public class MapGenerator {
    public MapGenerator(int xSize, int ySize) {
        room = new Tile[xSize, ySize];
        InitializeRoom();
    }

    public void InitializeRoom() {
        room = new Tile[xSize, ySize];
        // Set all tiles as empty tiles
        for (int x = 0; x < xSize; x++) {
            for (int y = 0; y < ySize; y++) {
                room[x, y] = new Tile(TileType.Empty);
            }
        }
        // room[3, 3] = new Tile(TileType.Wall);
        // room[3, 4] = new Tile(TileType.Wall);
        // room[4, 3] = new Tile(TileType.Wall);
        // room[4, 4] = new Tile(TileType.Wall);
        
        // Set boundaries as walls
        for (int x = 0; x < xSize; x++) {
            room[x, 0] = new Tile(TileType.Wall);
            room[x, ySize - 1] = new Tile(TileType.Wall);
        }

        for (int y = 0; y < ySize; y++) {
            room[0, y] = new Tile(TileType.Wall);
            room[xSize - 1, y] = new Tile(TileType.Wall);
        }
        GenerateRandomWalls((xSize-2)*(ySize-2)/6);

        int midX = xSize / 2;
        int midY = ySize / 2;
        for (int i = -1; i <= 1; i++) {
            room[midX + i, midY] = new Tile(TileType.Empty); // Clear horizontal middle
            room[midX, midY + i] = new Tile(TileType.Empty); // Clear vertical middle
        }

        InitializeEnemies(5);
    }
    public void InitializeEnemies(int maxEnemies) {
        while (enemies.Count < maxEnemies) {
            int x = rng.Next(xSize-2);
            int y = rng.Next(ySize-2);
            if (room[x, y].Type != TileType.Wall) {
                enemies.Add(new Enemy(x, y));
            }
        }
    }
    private void GenerateRandomWalls(int numOfWalls) {
        for (int i = 0; i < numOfWalls; i++) {
            int x = rng.Next(1, xSize - 1);
            int y = rng.Next(1, ySize - 1);
            room[x, y] = new Tile(TileType.Wall);

            if (rng.NextDouble() > 0.5) { // Randomly decide if we extend the wall horizontally or vertically
                int length = rng.Next(1, 4);
                for (int l = 0; l < length; l++) {
                    int nx = x + l < xSize ? x + l : x; // Ensure within bounds
                    room[nx, y] = new Tile(TileType.Wall);
                }
            }
            else {
                int length = rng.Next(1, 4); 
                for (int l = 0; l < length; l++) {
                    int ny = y + l < ySize ? y + l : y; // Ensure within bounds
                    room[x, ny] = new Tile(TileType.Wall);
                }
            }
        }
    }

    // Main method for displaying rooms
    public void DisplayRoom() {
        bool flag = true;
        while (flag) {
            Console.Clear();
            PrintRoom();
            if (player.X == enemy.X && player.Y == enemy.Y)
                Encounter(player, enemy);
            flag = ProcessInput();  
            if (rng.Next(3) == 0) 
                enemy.MoveRandom();  
        }
    }
    
    public void Encounter(dynamic player, dynamic enemy) {
        System.Environment.Exit(0);
    }
    public void PrintRoom() {
        for (int i = 0; i < xSize; i++) {
            for (int j = 0; j < ySize; j++) {
                if (player.X == i && player.Y == j)
                    WriteTile("Y ", ConsoleColor.White);
                else if (enemy.X == i &&  enemy.Y == j) {
                    WriteTile("? ", ConsoleColor.DarkRed);
                }
                else {
                    if (room[i, j].Type == TileType.Empty)
                        WriteTile(". ", ConsoleColor.Gray);
                    else
                       WriteTile("# ", ConsoleColor.Black);
                }
                PrintInterface(i, j);
            }
            Console.WriteLine();
        }
    }
    public void WriteTile(string str, ConsoleColor color) {
        Console.ForegroundColor = color;
        Console.Write(str);
        Console.ForegroundColor = ConsoleColor.White;
    }
    public void PrintInterface(int i, int j) {
        int[] rows = {2, 3, 4, 6};
        string[] texts = {$"   Health: {player.Health}/{player.maxHealth}", $"   EXP: {player.EXP}/{player.maxEXP}", $"   LVL: {player.LVL}", $"   Controls: Movement (WASD), Menu (M), Quit (Q)"};
        foreach (int row in rows) {
            int index = Array.IndexOf(rows, row);
            if (i == rows[index] && j == ySize-1) Console.Write(texts[index]);
        }
    }

     public bool ProcessInput() {
        Console.Write("> ");
        char input = Console.ReadKey().KeyChar;
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
        }
        return true;
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

public static int GetChoice(params string[] Choices) {
    int maxChoices = Choices.Length;
    Console.ForegroundColor = ConsoleColor.White;
    Console.WriteLine();
    for (int i = 0; i < Choices.Length; i++) {
        Print(String.Format("[{0}] {1}", i+1, Choices[i].ToString()), 20, 100);
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
    bool flag = true;
    while (flag) {
        // player.Talk("Where do I go?");

        int choice = GetChoice("Blood-Horned Door", "Twin-Mask Door", "Thorn-Blooming Door", "Ankh-Ornated Door", "Simple Door");
        player.Think("I approached the door.");
        if (choice == 1) {
            // player.Narrate("The door stood tall and imposing, adorned with twisted horns portruding from every corner, their tips stained crimson with the blood of the unfortunate, each curve and jagged edge instills a primal fear in those who dare approach.");
            // player.Think("The plaque below the statue reads... \"protection for a price.\"");
            flag = ChooseDeity("THE WANDERER"); // Turns false when the player enters the door.
            if (!flag)
                WandererRoute();
        } else if (choice == 2) {
            player.Narrate("The door stood tall and magical, adorned with twin masks, one serene and the other solemn, their intricate designs pulsating with an otherworldly glow, while delicate tendrils of shimmering mist curled around the edges, obscuring the threshold in a veil of enchantment, hinting at the mysteries that lie beyond.");
            player.Think("The plaque below the statue reads... \"Seek forbidden knowledge.\"");
            flag = ChooseDeity("The Wisened");
            // if (!flag)
            //     WisenedRoute();
        } else if (choice == 3) {
            player.Narrate("The door stood tall and weathered, adorned with gnarled vines that twisted and coiled around its frame, their thorns glistening with a malevolent gleam as if hungry for the touch of unwary hands, while a faint aroma of fresh earth and ripened fruit wafted from the intricate carvings depicting fields of golden grain and lush orchards thriving beneath the shadow of a towering treant, its branches outstretched in a gesture of both protection and expectation.");
            player.Think("The plaque below the statue reads... \"A bounty for the patient.\"");
            flag = ChooseDeity("The Harvest");
            // if (!flag)
            //     HarvestRoute();
        } else if (choice == 4) {
            player.Narrate("The door loomed ominously, its obsidian surface engulfed in swirling crimson tendrils, each etching of the ankh symbol, a silent promise of finality and inevitability, a gateway to the abyssal realm of shadows where every step may lead to the precipice of final embrace.");
            player.Think("The plaque below the statue reads... \"Find solace in the inevitable.\"");
            flag = ChooseDeity("The End");
            // if (!flag)
            //     EndRoute();
        } else if (choice == 5) {
            player.Narrate("Just a simple door.");
            flag = ChooseDeity("None");
            // if (!flag)
            //     DeitylessRoute();
        }
    }
}
}

