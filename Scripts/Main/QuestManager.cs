using Godot;
using System;
using System.Collections.Generic;

// Prosta klasa danych zadania
public class Quest
{
	public string Id;
	public string Title;
	public string CurrentObjective; // Np. "Znajdź klucz", "Wróć do szefa"
	public bool IsCompleted;

	public Quest(string id, string title, string objective)
	{
		Id = id;
		Title = title;
		CurrentObjective = objective;
		IsCompleted = false;
	}
}

public partial class QuestManager : Node
{
	public static QuestManager Instance { get; private set; }

	// Słownik: ID zadania -> Obiekt Quest
	private Dictionary<string, Quest> _quests = new Dictionary<string, Quest>();

	public event Action OnQuestsUpdated;
	
	public event Action<string, string> OnQuestNotification;

	public override void _Ready()
	{
		Instance = this;
	}

	// 1. Rozpoczęcie zadania
	public void StartQuest(string id, string title, string initialObjective)
	{
		if (!_quests.ContainsKey(id))
		{
			var newQuest = new Quest(id, title, initialObjective);
			_quests.Add(id, newQuest);
			GD.Print($"[QUEST] Rozpoczęto: {title}");
			OnQuestsUpdated?.Invoke();
			OnQuestNotification?.Invoke("NOWE ZADANIE", title);
		}
	}

	// 2. Aktualizacja celu (np. po naprawie maszyny)
	public void UpdateQuestObjective(string id, string newObjective)
	{
		if (_quests.ContainsKey(id) && !_quests[id].IsCompleted)
		{
			_quests[id].CurrentObjective = newObjective;
			GD.Print($"[QUEST] Aktualizacja celu: {newObjective}");
			OnQuestsUpdated?.Invoke();
			OnQuestNotification?.Invoke("ZAKTUALIZOWANO ZADANIE", _quests[id].Title);
		}
	}

	// 3. Całkowite zakończenie zadania
	public void FinishQuest(string id)
	{
		if (_quests.ContainsKey(id) && !_quests[id].IsCompleted)
		{
			_quests[id].IsCompleted = true;
			// Można tu dodać logikę usuwania z aktywnych, albo przenoszenia do historii
			_quests.Remove(id); // Usuwamy z listy aktywnych
			GD.Print($"[QUEST] Zakończono zadanie: {id}");
			OnQuestsUpdated?.Invoke();
			OnQuestNotification?.Invoke("ZADANIE UKOŃCZONE", _quests[id].Title);
		}
	}

	public List<Quest> GetActiveQuests()
	{
		// Zwracamy listę aktywnych questów
		return new List<Quest>(_quests.Values);
	}
}
