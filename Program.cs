using System.Media;
using System.Reflection.Metadata;
using static System.Threading.Thread;

class Program {
    // Global variables
    public static Player player = new(35, 600, ConsoleColor.White, "Player");
     public static Deity End = new(50, 1000, ConsoleColor.DarkRed, "The End");
     public static Deity Wisened = new(30, 1000, ConsoleColor.DarkMagenta, "The Wisened");
     public static Deity Wanderer = new(42, 1300, ConsoleColor.Black, "The Wanderer");
     public static Deity Harvest = new(55, 1000, ConsoleColor.DarkGreen, "The Harvest");

public class Deity(int speed = 40, int duration = 600, ConsoleColor color = ConsoleColor.White, string name = "???") {
    // Dialogue properties
    public string Name = name;
    private int tSpeed = speed;
    private int tDuration = duration;
    private ConsoleColor Color = color;

    public void Talk(string str) {
        Program.Print(str, tSpeed, tDuration, Color, Name);
    }
}


public class Player(int speed = 35, int duration = 450, ConsoleColor color = ConsoleColor.White, string name = "???") {
    private string Name = name;
    private static dynamic Deity;
    private string DeityName = "None"; // Job equivalent
    // Attributes properties
    private int HP, ATK, DEF, INT, SPD, LCK, GOLD, EXP;
    // Specific stats
    private int Health, Damage, Armor;

    // Dialogue properties
    private int tSpeed = speed;
    private int tDuration = duration;
    private ConsoleColor Color = color;
    public void Talk(string str) {
        Program.Print(str, tSpeed, tDuration, Color, Name);
    }

    public void Think(string str) {
        Program.Print(String.Format("({0})", str), tSpeed-10, tDuration, ConsoleColor.DarkGray);
    }

    public void Narrate(string str) {
        Program.Print(String.Format("[{0}]", str), tSpeed-15, tDuration+60, ConsoleColor.White);
    }

    public void setDeity(string deity) {
        if (deity != "None")
            DeityName = "The" + deity.ToString();
    }

    // public void Narrate(string str) {
    //     Program.Print(String.Format("[{0}]", str), tSpeed-10, tDuration, ConsoleColor.White);
    // }
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
    Wanderer.Name = "???";
    player.Think("The door slammed shut as soon as I entered.");
    player.Think("I felt an ominous figure watching me.");
    Sleep(800);
    player.Think("I hear a boisterous laugh in the distance.");
    Sleep(600);
    player.Think("It's getting closer...");
    Sleep(600);
    Wanderer.Talk("BWAHA..!");
    Sleep(600);
    Wanderer.Talk("BWAHAHA!");
    player.Think("...");
    Sleep(800);
    Wanderer.Talk("BWAHAHAHAHAHA!");
    player.Think("A monstrous horned-figure wearing a devilish armor approached...");
    Wanderer.Name = "The Wanderer";
    Wanderer.Talk("BLEED FOR YOUR MASTER, FOOL!");
    player.Narrate("\nYou chose The Wanderer as your Deity.");
    player.Narrate("Experience the worst to become the best.");
    player.Narrate("Effects: ++ HP, + DEF, - GLD, - ATK, - SPD");
}



public static void Print(string str, int speed = 40, int duration = 600, ConsoleColor color = ConsoleColor.White, string name = "") {
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
    bool ValidChoice = int.TryParse(Console.ReadLine(), out int Choice);

    if (!ValidChoice || Choice < 1 || Choice > maxChoices)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("Invalid Input. Please try again.");
        Sleep(800);
        Console.Clear();
        Choice = GetChoice(Choices);
    }
    Console.Clear();
    return Choice;
}

    static void Main() {
    DisplayTitle();
    player.Think("A mind shattering pain pierced my head.");
    player.Think("I flinched from the pain and noticed something in front of me.");
    player.Think("What is this place..?");
    player.Think("Four giant doors guarded by statues loom ominously before me.");
    bool flag = true;
    while (flag) {
        player.Talk("Where do I go?");

        int choice = GetChoice("Blood-Horned Door", "Twin-Mask Door", "Thorn-Blooming Door", "Ankh-Ornated Door", "Simple Door");
        player.Think("I approached the statue near the door.");
        if (choice == 1) {
            player.Think("The plaque reads... \"protection for a price.\"");
            flag = ChooseDeity("The Wanderer"); // Turns false when the player enters the door.
            if (!flag)
                WandererRoute();
        } else if (choice == 2) {
            player.Think("The plaque reads... \"Seek forbidden knowledge.\"");
            flag = ChooseDeity("The Wisened");
            // if (!flag)
            //     WisenedRoute();
        } else if (choice == 3) {
            player.Think("The plaque reads... \"A bounty for the patient.\"");
            flag = ChooseDeity("The Harvest");
            // if (!flag)
            //     HarvestRoute();
        } else if (choice == 4) {
            player.Think("The plaque reads... \"Find solace in the inevitable.\"");
            flag = ChooseDeity("The End");
            // if (!flag)
            //     EndRoute();
        } else if (choice == 5) {
            Print("(Go Alone)");
            flag = ChooseDeity("None");
            // if (!flag)
            //     DeitylessRoute();
        }
    }
}
}

