using System.Media;
using static System.Threading.Thread;

class Program {
public class Deity {
    private string Name;
    private int tSpeed;
    private int tDuration;
    private ConsoleColor Color;

    public Deity(int speed = 40, int duration = 600, ConsoleColor color = ConsoleColor.White, string name = "???") {
        this.tSpeed = speed;
        this.tDuration = duration;
        this.Color = color;
        this.Name = name;
    }

    public void Talk(string str) {
        Program.Print(str, tSpeed, tDuration, Color, Name);
    }
}

public class Player {
    private string Name;
    private string Job;
    private int tSpeed;
    private int tDuration;
    private ConsoleColor Color;
    private int HP, DEF, ATK, INT, SPD, EXP, LCK, GLD;
    public Player(int speed = 40, int duration = 600, ConsoleColor color = ConsoleColor.White, string name = "???") {
        this.tSpeed = speed;
        this.tDuration = duration;
        this.Color = color;
        this.Name = name;
    }
    public void Talk(string str) {
        Program.Print(str, tSpeed, tDuration, Color, Name);
    }
}

static void Main() {
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
    Player Player = new Player(45, 600, ConsoleColor.White, "Player");
    Deity End = new Deity(50, 600, ConsoleColor.DarkRed, "The End");
    Deity Wisened = new Deity(30, 1000, ConsoleColor.DarkYellow, "The Wisened");
    Deity Wanderer = new Deity(40, 600, ConsoleColor.Black, "The Wanderer");

    Player.Talk("I must choose a deity.");
    End.Talk("Find solace in the inevitable.");
    Wisened.Talk("Seek forbidden knowledge.");
    Wanderer.Talk("Protection for a price.");
    Print("(Go Alone)");
}

public static void Print(string str, int speed = 40, int duration = 600, ConsoleColor color = ConsoleColor.White, string name = "") {
    Console.ForegroundColor = color;
    if (!string.IsNullOrEmpty(name))
        Console.Write(name + ": ");

    foreach (char c in str) {
        Console.Write(c);
        Sleep(speed); 
    }
    Console.WriteLine();
    Sleep(duration);
    Console.ForegroundColor = ConsoleColor.White;
}

}