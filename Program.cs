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
     public static Deity End = new(50, 1000, ConsoleColor.Black, "The End");
     public static Deity Wisened = new(30, 1000, ConsoleColor.DarkMagenta, "The Wisened");
     public static Deity Wanderer = new(5, 5, ConsoleColor.DarkRed, "The Wanderer"); // 42, 1300
     public static Deity Harvest = new(55, 1000, ConsoleColor.DarkGreen, "The Harvest");

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
        X += dx;
        Y += dy;
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
    Sleep(2000);
    Console.Clear();
    StartRun();
}
public static void StartRun() {
    int xSize = 13;
    int ySize = 13;
    Tile[,] map = new Tile[13, 13];
    for (int i = 0; i < xSize; i++) {
        for (int j = 0; j < ySize; j++)
        {
            map[i, j] = new Tile(TileType.Empty);
        }
    }
    
    map[3, 3] = new Tile(TileType.Wall);
    map[3, 4] = new Tile(TileType.Wall);
    map[4, 3] = new Tile(TileType.Wall);
    map[4, 4] = new Tile(TileType.Wall);

    bool running = true;
    while (running) {
                // Print the map
                for (int i = 0; i < xSize; i++) {
                    for (int j = 0; j < ySize; j++) {

                        if (player.X == i && player.Y == j) {
                            Console.Write("P ");
                        }
                        else
                        {
                            if (map[i, j].Type == TileType.Empty) {
                                Console.Write(". ");
                            }
                            else
                            {
                                Console.Write("# ");
                            }
                        }
                    }
                    Console.WriteLine();
                }

                // Get player input
                Console.WriteLine("Enter direction (WASD):");
                char input = Console.ReadKey().KeyChar;
                Console.WriteLine();

                // Move the player
                switch (input) {
                    case 'w':
                        if (player.X > 0 && map[player.X - 1, player.Y].Type != TileType.Wall)
                        {
                            player.Move(-1, 0);
                        }
                        break;
                    case 'a':
                        if (player.Y > 0 && map[player.X, player.Y - 1].Type != TileType.Wall)
                        {
                            player.Move(0, -1);
                        }
                        break;
                    case 's':
                        if (player.X < ySize-1 && map[player.X + 1, player.Y].Type != TileType.Wall)
                        {
                            player.Move(1, 0);
                        }
                        break;
                    case 'd':
                        if (player.Y < xSize-1 && map[player.X, player.Y + 1].Type != TileType.Wall)
                        {
                            player.Move(0, 1);
                        }
                        break;
                    case 'q':
                        running = false;
                        break;
                }

                // Clear the console
                Console.Clear();
    }
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

    if (!ValidChoice || Choice < 1 || Choice > maxChoices)
    {
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

