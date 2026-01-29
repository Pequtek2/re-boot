using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class DebugConsole : CanvasLayer
{
	[Export] public Control ConsolePanel; // Przypisz Panel
	[Export] public RichTextLabel LogOutput;
	[Export] public LineEdit InputLine;

	private bool _isOpen = false;

	public override void _Ready()
	{
		// Ukryj na starcie
		ConsolePanel.Visible = false;
		ProcessMode = ProcessModeEnum.Always; // Działa nawet gdy gra zapauzowana

		// Obsługa Entera
		InputLine.TextSubmitted += OnCommandSubmitted;
	}

	public override void _Input(InputEvent @event)
{
	if (@event is InputEventKey keyEvent && keyEvent.Pressed)
	{
		// 1. Obsługa klawisza Toggle (/)
		if (keyEvent.Keycode == Key.Slash)
		{
			ToggleConsole();
			
			// Ważne: Oznaczamy jako obsłużone, żeby "/" nie wpisało się do konsoli
			// ani nie wywołało akcji w grze.
			GetViewport().SetInputAsHandled(); 
		}
		// 2. Obsługa wyjścia przez ESC (tylko gdy konsola jest otwarta)
		else if (keyEvent.Keycode == Key.Escape)
		{
			if (_isOpen)
			{
				ToggleConsole(); // Zamknij konsolę
				
				// Ważne: Blokujemy propagację, żeby ESC nie otworzyło 
				// Menu Pauzy zaraz po zamknięciu konsoli.
				GetViewport().SetInputAsHandled(); 
			}
		}
	}
}
	private void ToggleConsole()
	{
		_isOpen = !_isOpen;
		ConsolePanel.Visible = _isOpen;
		GetTree().Paused = _isOpen; // Pauzuj grę jak konsola otwarta

		if (_isOpen)
		{
			InputLine.GrabFocus();
			InputLine.Clear(); // Czyść "/" które mogło się wpisać
		}
	}

	private void OnCommandSubmitted(string text)
	{
		if (string.IsNullOrWhiteSpace(text)) return;

		LogToConsole($"> {text}", Colors.Gray);
		InputLine.Clear();
		
		ProcessCommand(text);
	}

	private void LogToConsole(string text, Color color)
	{
		string hexColor = color.ToHtml();
		LogOutput.AppendText($"[color=#{hexColor}]{text}[/color]\n");
	}

	// --- PARSER KOMEND ---

	private void ProcessCommand(string input)
	{
		string[] parts = input.Split(' ');
		string command = parts[0].ToLower();
		string[] args = parts.Skip(1).ToArray(); // Reszta to argumenty

		try
		{
			switch (command)
			{
				case "help":
					LogToConsole("--- LISTA KOMEND ---", Colors.Yellow);
					LogToConsole("repair [id] - Naprawia maszynę (musi być odblokowana)", Colors.White);
					LogToConsole("unlock [id] - Odblokowuje maszynę", Colors.White);
					LogToConsole("quest_start [id] - Rozpoczyna zadanie", Colors.White);
					LogToConsole("quest_prog [id] [amount] - Progresuje zadanie", Colors.White);
					LogToConsole("quest_finish [id] - Kończy zadanie", Colors.White);
					LogToConsole("dialogue [npc_id] - Odpala rozmowę", Colors.White);
					LogToConsole("item [name] - Daje przedmiot (powiadomienie)", Colors.White);
					LogToConsole("list_machines - Wypisuje stany maszyn", Colors.White);
					break;

				case "repair":
					if (args.Length < 1) { LogToConsole("Błąd: Podaj ID maszyny.", Colors.Red); return; }
					string mId = args[0];
					
					// WARUNEK: Nie można naprawić zablokowanej!
					if (!MainGameManager.Instance.IsMachineUnlocked(mId))
					{
						LogToConsole($"BŁĄD: Maszyna '{mId}' jest ZABLOKOWANA! Najpierw użyj 'unlock'.", Colors.Red);
					}
					else
					{
						MainGameManager.Instance.SetMachineFixed(mId);
						LogToConsole($"Sukces: Maszyna '{mId}' naprawiona.", Colors.Green);
					}
					break;

				case "unlock":
					if (args.Length < 1) { LogToConsole("Błąd: Podaj ID maszyny.", Colors.Red); return; }
					MainGameManager.Instance.UnlockMachine(args[0]);
					LogToConsole($"Maszyna '{args[0]}' odblokowana.", Colors.Cyan);
					break;

				case "quest_start":
					if (args.Length < 1) { LogToConsole("Błąd: Podaj ID questa.", Colors.Red); return; }
					QuestManager.Instance.StartQuest(args[0]);
					LogToConsole($"Quest '{args[0]}' rozpoczęty.", Colors.Gold);
					break;

				case "quest_prog":
					if (args.Length < 1) { LogToConsole("Błąd: Podaj ID questa.", Colors.Red); return; }
					int amount = 1;
					if (args.Length > 1) int.TryParse(args[1], out amount);
					
					QuestManager.Instance.ProgressQuest(args[0], amount);
					LogToConsole($"Quest '{args[0]}' postęp +{amount}.", Colors.Gold);
					break;
				
				case "quest_finish":
					if (args.Length < 1) { LogToConsole("Błąd: Podaj ID questa.", Colors.Red); return; }
					QuestManager.Instance.ForceCompleteQuest(args[0]);
					LogToConsole($"Quest '{args[0]}' wymuszono zakończenie.", Colors.Green);
					break;

				case "dialogue":
					if (args.Length < 1) { LogToConsole("Błąd: Podaj ID NPC.", Colors.Red); return; }
					ToggleConsole(); // Zamknij konsolę żeby widzieć dialog
					DialogueManager.Instance.StartDialogue(args[0]);
					break;

				case "item":
					string itemName = string.Join(" ", args); // Łączy resztę słów w nazwę (np. Złoty Klucz)
					NotificationUI.Instance.AddNotification("OTRZYMANO PRZEDMIOT", itemName);
					LogToConsole($"Dodano przedmiot: {itemName}", Colors.Orange);
					break;
					
				case "add_tag":
					 if (args.Length < 1) return;
					 TagManager.Instance.AddTag(args[0]);
					 LogToConsole($"Tag '{args[0]}' dodany.", Colors.Magenta);
					 break;

				default:
					LogToConsole($"Nieznana komenda: {command}. Wpisz 'help'.", Colors.Red);
					break;
			}
		}
		catch (Exception e)
		{
			LogToConsole($"CRITICAL ERROR: {e.Message}", Colors.Red);
		}
	}
}
