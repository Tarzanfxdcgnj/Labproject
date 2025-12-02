/*
How to Play Haunted RGU Campus
-----------------------------

1. Run the program. You will be prompted to enter your name.
2. The game will describe your current room and any challenges present.
3. At the 'Command>' prompt, type commands to interact with the game. Available commands:

- go <direction>   : Move to another room (north, south, east, west)
- attack           : Attack an enemy in the room (if present)
- solve <answer>   : Attempt to solve a puzzle in the room (if present)
- look             : Re-describe the current room and its challenges
- status           : Show your health and progress
- help             : Show the list of available commands
- map              : Show a simple map of the campus
- quit             : End the game

4. You must complete all challenges in a room before you can move to the next one.
5. Defeat enemies by using 'attack'. Solve puzzles by using 'solve <your answer>'.
6. If your health drops to zero, the game ends.
7. Reach and complete the final room (Rooftop Tower) to win the game.

Tips:
- Type 'help' at any time to see the list of commands.
- Use 'look' if you want to re-read the room description.
- Use 'status' to check your health and progress.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using HauntedRgu.Gameplay;
using HauntedRgu.Gameplay.Challenges;
using HauntedRgu.Gameplay.Rooms;
using HauntedRgu.Gameplay.Commands;
using HauntedRgu.Gameplay.World;

namespace HauntedRgu.Gameplay.Challenges
{
    public interface IChallenge
    {
        bool IsComplete { get; }
        string Description { get; }

        void Start(Player player);
        void HandleInput(string input, Player player);
    }

    public class PuzzleChal : IChallenge
    {
        public bool IsComplete { get; private set; }
        public string Description { get; }

        private readonly string _correctAnswer;
        private int _attemptsLeft;

        public PuzzleChal(string description, string correctAnswer, int attempts)
        {
            Description = description;
            _correctAnswer = correctAnswer.ToLower();
            _attemptsLeft = attempts;
        }

        public void Start(Player player)
        {
            Console.WriteLine(Description);
            Console.WriteLine("Type your answer:");
        }

        public void HandleInput(string input, Player player)
        {
            if (IsComplete || _attemptsLeft <= 0)
                return;

            _attemptsLeft--;

            if (input.Trim().ToLower() == _correctAnswer)
            {
                IsComplete = true;
                Console.WriteLine("The puzzle glows… You solved it!");
            }
            else
            {
                Console.WriteLine("Wrong answer...");
                Console.WriteLine("Attempts left: " + _attemptsLeft);
            }
        }
    }

    public class EnemyChallenge : IChallenge
    {
        public bool IsComplete => Enemy.IsDefeated;
        public string Description => "An angry " + Enemy.Name + "blocks your way!";

        public Enemy Enemy { get; }

        public EnemyChallenge(Enemy enemy)
        {
            Enemy = enemy;
        }

        public void Start(Player player)
        {
            Console.WriteLine(Description);
            Console.WriteLine("Enemy health: " + Enemy.Health);
        }

        public void HandleInput(string input, Player player)
        {
            if (input.Trim().ToLower() == "attack")
            {
                Enemy.TakeDamage(player.AttackPower);

                if (!Enemy.IsDefeated)
                {
                    Console.WriteLine("You hit the " + Enemy.Name + "! Its health is now" + Enemy.Health);
                    player.TakeDamage(Enemy.Damage);
                    Console.WriteLine("The " + Enemy.Name + "hits back! Your health: " + player.Health);
                }
                else
                {
                    Console.WriteLine("You defeated the " + Enemy.Name);
                }
            }
        }
    }
}
namespace HauntedRgu.Gameplay
{
    public class Player
    {
        public string Name { get; }
        public int Health { get; private set; }
        public int AttackPower { get; private set; }

        public bool IsAlive => Health > 0;

        public Player(string name, int startingHealth, int attackPower)
        {
            Name = name;
            Health = startingHealth;
            AttackPower = attackPower;
        }

        public void TakeDamage(int damage)
        {
            Health = Math.Max(0, Health - damage);
        }
    }

    public class Enemy
    {
        public string Name { get; }
        public int Health { get; private set; }
        public int Damage { get; }

        public bool IsDefeated => Health <= 0;

        public Enemy(string name, int health, int damage)
        {
            Name = name;
            Health = health;
            Damage = damage;
        }

        public void TakeDamage(int amount)
        {
            Health = Math.Max(0, Health - amount);
        }
    }
}

namespace HauntedRgu.Gameplay.Rooms
{
    public interface IRoom
    {
        string Name { get; }
        string Description { get; }

        IRoom? North { get; set; }
        IRoom? South { get; set; }
        IRoom? East  { get; set; }
        IRoom? West  { get; set; }

        IReadOnlyCollection<IChallenge> Challenges { get; }
        bool IsComplete { get; }

        void Enter(Player player);
    }

    public abstract class BaseRoom : IRoom
    {
        private readonly List<IChallenge> _challenges = new();

        public string Name { get; }
        public string Description { get; protected set; }

        public IRoom? North { get; set; }
        public IRoom? South { get; set; }
        public IRoom? East  { get; set; }
        public IRoom? West  { get; set; }

        public IReadOnlyCollection<IChallenge> Challenges => _challenges.AsReadOnly();

        public bool IsComplete => _challenges.All(c => c.IsComplete);

        protected BaseRoom(string name, string description)
        {
            Name = name;
            Description = description;
        }

        protected void AddChallenge(IChallenge challenge)
        {
            _challenges.Add(challenge);
        }

        public virtual void Enter(Player player)
        {
            Console.WriteLine();
            Console.WriteLine("==" + Name + "==");
            Console.WriteLine(Description);

            foreach (var challenge in _challenges.Where(c => !c.IsComplete))
            {
                challenge.Start(player);
            }
        }
    }

    // enemy-only
    public class PhysicalRoom : BaseRoom
    {
        public PhysicalRoom(string name, string description, Enemy enemy)
            : base(name, description)
        {
            AddChallenge(new EnemyChallenge(enemy));
        }
    }

    // enemy + puzzle
    public class UltimateRoom : BaseRoom
    {
        public UltimateRoom(string name, string description, Enemy enemy, IChallenge puzzle)
            : base(name, description)
        {
            AddChallenge(new EnemyChallenge(enemy));
            AddChallenge(puzzle);
        }
    }

    // puzzle-only
    public class SkillRoom : BaseRoom
    {
        public SkillRoom(string name, string description, IChallenge puzzle)
            : base(name, description)
        {
            AddChallenge(puzzle);
        }
    }
}
namespace HauntedRgu.Gameplay.Commands
{
    public interface ICommand
    {
        string Name { get; } // e.g. "go", "attack", "solve"
        void Execute(Game game, string[] args);
    }

    public class CommandManager
    {
        private readonly Dictionary<string, ICommand> _commands = new();

        public void Register(ICommand command)
        {
            _commands[command.Name] = command;
        }

        public bool TryExecute(string input, Game game)
        {
            if (string.IsNullOrWhiteSpace(input))
                return false;

            var parts = input.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var commandName = parts[0].ToLowerInvariant();
            var args = parts.Skip(1).ToArray();

            try
            {
                if (_commands.TryGetValue(commandName, out var command))
                {
                    command.Execute(game, args);
                    return true;
                }
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine("Problem with your command arguments: " + ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unexpected error running command " + commandName + ":" + ex.Message);
            }

            return false;
        }
    }

    public class MoveCommand : ICommand
    {
        public string Name => "go";

        public void Execute(Game game, string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Go where? north/south/east/west");
                return;
            }

            var direction = args[0];

            var nextRoom = direction switch
            {
                "north" => game.CurrentRoom.North,
                "south" => game.CurrentRoom.South,
                "east"  => game.CurrentRoom.East,
                "west"  => game.CurrentRoom.West,
                _ => null
            };

            if (nextRoom == null)
            {
                Console.WriteLine("You can not go that way.");
                return;
            }

            if (!game.CurrentRoom.IsComplete)
            {
                Console.WriteLine("A mysterious force keeps you here… you must finish the challenges first.");
                return;
            }

            game.CurrentRoom = nextRoom;
            game.CurrentRoom.Enter(game.Player);
        }
    }

    public class AttackCommand : ICommand
    {
        public string Name => "attack";

        public void Execute(Game game, string[] args)
        {
            var enemyChallenge = game.CurrentRoom
                .Challenges
                .OfType<EnemyChallenge>()
                .FirstOrDefault(c => !c.IsComplete);

            if (enemyChallenge == null)
            {
                Console.WriteLine("There is nothing to attack here.");
                return;
            }

            enemyChallenge.HandleInput("attack", game.Player);

            if (!game.Player.IsAlive)
            {
                game.IsOver = true;
                Console.WriteLine("You collapse… the hauntings consume you. Game over.");
            }
        }
    }
}

namespace HauntedRgu.Gameplay
{
    public class Game
    {
        public Player Player { get; private set; }
        public IRoom CurrentRoom { get; set; }
        public bool IsOver { get; set; }

        private readonly CommandManager _commandManager;

        public Game(Player player, IRoom startingRoom, CommandManager commandManager)
        {
            Player = player;
            CurrentRoom = startingRoom;
            _commandManager = commandManager;
        }

        public void Run()
        {
            CurrentRoom.Enter(Player);

            while (!IsOver)
            {
                if (!Player.IsAlive)
                {
                    Console.WriteLine("You can no longer continue… the haunting consumes you.");
                    IsOver = true;
                    break;
                }

                if (HasPlayerWon())
                {
                    Console.WriteLine("A warm light breaks through the fog… you’ve cleansed the haunted campus. You win!");
                    IsOver = true;
                    break;
                }

                Console.WriteLine();
                Console.Write("Command> ");
                var input = Console.ReadLine() ?? string.Empty;

                try
                {
                    if (!_commandManager.TryExecute(input, this))
                    {
                        Console.WriteLine("I don't understand that command. Type 'help' to see options.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Unexpected error while processing command: {ex.Message}");
                }
            }
        }

        private bool HasPlayerWon()
        {
            
            return CurrentRoom.Name == "Rooftop Tower" && CurrentRoom.IsComplete;
        }
    }
}

namespace HauntedRgu.Gameplay.World
{
    public interface IRoomFactory
    {
        IRoom CreateWorld(); 
    }

    public class HauntedRguRoomFactory : IRoomFactory
    {
        public IRoom CreateWorld()
        {
            // 1. Create enemies
            var ghostGuard = new Enemy("Ghost Security Guard", health: 30, damage: 5);
            var shadowFigure = new Enemy("Shadow Figure", 25, 4);
            var labDemon = new Enemy("Corrupted Lab Demon", 40, 6);
            var rooftopBoss = new Enemy("Warden of the Rooftop", 60, 8);
            var possessedStudent = new Enemy("Possessed Student", 35, 5);

            // 2. Create puzzles
            var libraryPuzzle = new PuzzleChal(
                description: "A dusty book glows. On the page: 'What is 6 x 7?'",
                correctAnswer: "42",
                attempts: 3);

            var theatrePuzzle = new PuzzleChal(
                description: "On the board, a riddle about time: 'I have hands but cannot clap. What am I?'",
                correctAnswer: "clock",
                attempts: 3);

            var labPuzzle = new PuzzleChal(
                description: "The lab terminal asks: 'Binary for 5?'",
                correctAnswer: "101",
                attempts: 3);

            var rooftopPuzzle = new PuzzleChal(
                description: "The final sigil glows: 'Add the digits of 2025.'",
                correctAnswer: "9",
                attempts: 3);

            // 3. Create rooms

            var entrance = new PhysicalRoom(
                name: "Haunted Main Entrance",
                description: "Cold air rushes through the shattered doors of RGU. A spectral guard floats at the turnstiles.",
                enemy: ghostGuard);

            var corridor = new PhysicalRoom(
                name: "Dark Corridor",
                description: "The corridor lights flicker. A tall shadow lurches from the end of the hallway.",
                enemy: shadowFigure);

            var library = new SkillRoom(
                name: "Silent Library",
                description: "Rows of books watch you silently. One shelf is glowing with a strange symbol.",
                puzzle: libraryPuzzle);

            var theatre = new SkillRoom(
                name: "Cursed Lecture Theatre",
                description: "Desks are overturned. Chalk moves by itself across the board, scribbling riddles.",
                puzzle: theatrePuzzle);

            var lab = new UltimateRoom(
                name: "Abandoned IT Lab",
                description: "Monitors flash error codes. A corrupted demon crawls out from a broken PC tower.",
                enemy: labDemon,
                puzzle: labPuzzle);

            var stairwell = new PhysicalRoom(
                name: "Rooftop Stairwell",
                description: "Each step echoes unnaturally. A possessed student blocks your path upwards.",
                enemy: possessedStudent);

            var rooftop = new UltimateRoom(
                name: "Rooftop Tower",
                description: "Wind howls across the rooftop. The Warden of the Rooftop stands before a glowing seal.",
                enemy: rooftopBoss,
                puzzle: rooftopPuzzle);

            // 4. Link rooms (simple map)

            // Entrance -> Corridor -> Library -> Theatre -> Lab -> Stairwell -> Rooftop
            entrance.North = corridor;

            corridor.South = entrance;
            corridor.North = library;

            library.South = corridor;
            library.North = theatre;

            theatre.South = library;
            theatre.North = lab;

            lab.South = theatre;
            lab.North = stairwell;

            stairwell.South = lab;
            stairwell.North = rooftop;

            rooftop.South = stairwell;

            // Starting room:
            return entrance;
        }
    }
}

class Program
{
    static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        Console.Write("Enter your name, ghost hunter: ");
        var name = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(name))
        {
            name = "Unknown";
        }

        var player = new Player(name, startingHealth: 100, attackPower: 10);

        var roomFactory = new HauntedRguRoomFactory();
        var startingRoom = roomFactory.CreateWorld();

        var commandManager = new CommandManager();
        commandManager.Register(new MoveCommand());
        commandManager.Register(new AttackCommand());
        commandManager.Register(new SolveCommand());
        commandManager.Register(new HelpCommand());
        commandManager.Register(new LookCommand());
        commandManager.Register(new MapCommand());
        commandManager.Register(new QuitCommand()); // you implement

        var game = new Game(player, startingRoom, commandManager);

        Console.WriteLine();
        Console.WriteLine("Welcome to Haunted RGU Campus. Type 'help' if you feel lost.");
        game.Run();
    }
}

namespace HauntedRgu.Gameplay.Commands
{
    public class SolveCommand : ICommand
    {
        public string Name => "solve";

        public void Execute(Game game, string[] args)
        {
            var puzzle = game.CurrentRoom
                .Challenges
                .OfType<PuzzleChal>()
                .FirstOrDefault(c => !c.IsComplete);

            if (puzzle == null)
            {
                Console.WriteLine("There doesn’t seem to be any puzzle to solve here.");
                return;
            }

            if (args.Length == 0)
            {
                Console.WriteLine("You must provide an answer. Example: solve 42");
                return;
            }

            var answer = string.Join(" ", args);

            try
            {
                puzzle.HandleInput(answer, game.Player);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Something went wrong while solving the puzzle: " + ex.Message);
            }
        }
    }
}

namespace HauntedRgu.Gameplay.Commands
{
    public class HelpCommand : ICommand
    {
        public string Name => "help";

        public void Execute(Game game, string[] args)
        {
            Console.WriteLine("Available commands:");
            Console.WriteLine("- go <direction>   (north, south, east, west)");
            Console.WriteLine("- attack           (attack an enemy in the room)");
            Console.WriteLine("- solve <answer>   (answer a puzzle in the room)");
            Console.WriteLine("- look             (re-describe the current room)");
            Console.WriteLine("- status           (show your health and progress)");
            Console.WriteLine("- help             (show this help text)");
            Console.WriteLine("- quit             (end the game)");
        }
    }
}

namespace HauntedRgu.Gameplay.Commands
{
    public class LookCommand : ICommand
    {
        public string Name => "look";

        public void Execute(Game game, string[] args)
        {
            game.CurrentRoom.Enter(game.Player);
        }
    }
}

namespace HauntedRgu.Gameplay.Commands
{
    public class MapCommand : ICommand
    {
        public string Name => "map";

        public void Execute(Game game, string[] args)
        {
            // Very simple text map for the player
            Console.WriteLine("Rough map of the haunted campus:");
            Console.WriteLine("[Entrance] -> [Corridor] -> [Library] -> [Theatre] -> [Lab] -> [Stairwell] -> [Rooftop]");
            Console.WriteLine("You are currently in: " + game.CurrentRoom.Name);
        }
    }
}

public class QuitCommand : ICommand
{
    public string Name => "quit";
    public void Execute(Game game, string[] args)
    {
        Console.WriteLine("You feel a chill as you leave the haunted campus. Goodbye!");
        game.IsOver = true;
    }
}

