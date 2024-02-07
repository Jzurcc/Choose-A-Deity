﻿using System.Media;
using System.Reflection.Metadata;
using static System.Threading.Thread;

class Program {
public class Deity(int speed = 40, int duration = 600, ConsoleColor color = ConsoleColor.White, string name = "???") {
    private string Name = name;
    private int tSpeed = speed;
    private int tDuration = duration;
    private ConsoleColor Color = color;

    public void Talk(string str) {
        Program.Print(str, tSpeed, tDuration, Color, Name);
    }
}

public class Player(int speed = 35, int duration = 450, ConsoleColor color = ConsoleColor.White, string name = "???") {
    private string Name = name;
    private string Job = "";
    private int tSpeed = speed;
    private int tDuration = duration;
    private ConsoleColor Color = color;
    private int HP, DEF, ATK, INT, SPD, EXP, LCK, GLD;

    public void Talk(string str) {
        Program.Print(str, tSpeed, tDuration, Color, Name);
    }

    public void Think(string str) {
        Program.Print(String.Format("({0})", str), tSpeed, tDuration, ConsoleColor.DarkGray);
    }
}

static void Main() {
    DisplayTitle();

    Player Player = new(30, 600, ConsoleColor.White, "Player");
    Deity End = new(50, 600, ConsoleColor.DarkRed, "The End");
    Deity Wisened = new(30, 1000, ConsoleColor.DarkMagenta, "The Wisened");
    Deity Wanderer = new(40, 600, ConsoleColor.Black, "The Wanderer");
    Deity Harvest = new(55, 650, ConsoleColor.DarkGreen, "The Harvest");
    

    Player.Think("A mind shattering pain pierced my head.");
    Player.Think("I flinched from the pain and noticed something in front of me.");
    Player.Think("Four giant doors guarded by statues loom ominously before me.");
    Player.Talk("Where do I go?");

    int choice = GetChoice("Blood-Horned Door", "Twin-Mask Door", "Thorn-Blooming Door", "Ankh-Ornated Door", "Simple Door");
    if (choice == 1) {
        
    } else if (choice == 2) {
        Wisened.Talk("Seek forbidden knowledge.");
    } else if (choice == 3) {
        Wanderer.Talk("Protection for a price.");
    } else if (choice == 4) {
        Harvest.Talk("A bounty for the patient.");
        End.Talk("Find solace in the inevitable.");
    } else if (choice == 5) {
        Print("(Go Alone)");
    }
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
}